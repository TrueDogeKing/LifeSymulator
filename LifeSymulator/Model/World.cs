namespace LifeSymulator.Model
{
    public class World
    {
        public int Width;
        public int Height;
        public Cell[,] Cells;
        public Rules Rules = new Rules();

        Random rnd = new Random();

        public World(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Cells[x, y] = new Cell();
        }

        public void Clear()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    Cells[x, y] = new Cell();
        }

        public void Randomize()
        {
            Clear();
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (rnd.NextDouble() < 0.3)
                        Cells[x, y].HasPlant = true;
                    if (rnd.NextDouble() < 0.08)
                    {
                        Cells[x, y].HasAnimal = true;
                        Cells[x, y].AnimalAge = rnd.Next(Rules.L);
                    }
                }
            }
        }

        // counts neighbours (8 around), board wraps around the edges
        int CountNeighbours(Cell[,] grid, int x, int y, bool plants)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Cell c = grid[Wrap(x + dx, Width), Wrap(y + dy, Height)];
                    if (plants && c.HasPlant) count++;
                    if (!plants && c.HasAnimal) count++;
                }
            }
            return count;
        }

        int Wrap(int v, int max)
        {
            return (v % max + max) % max;
        }

        public void Step()
        {
            Cell[,] old = Cells;
            Cell[,] next = new Cell[Width, Height];
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    next[x, y] = old[x, y].Copy();

            // 1. plants and animals birth/death (computed from old state)
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cell o = old[x, y];
                    Cell n = next[x, y];
                    int p = CountNeighbours(old, x, y, true);
                    int a = CountNeighbours(old, x, y, false);

                    if (!o.HasPlant && !o.HasAnimal)
                    {
                        if (Rules.R.Contains(p))
                            n.HasPlant = true;
                    }
                    else if (o.HasPlant)
                    {
                        if (!Rules.W.Contains(p))
                            n.HasPlant = false;
                    }

                    if (o.HasAnimal)
                    {
                        n.AnimalAge++;
                        if (n.AnimalAge > Rules.L || !Rules.Z.Contains(a))
                        {
                            n.HasAnimal = false;
                            n.AnimalAge = 0;
                        }
                    }
                    else
                    {
                        if (Rules.B.Contains(a) && Rules.P.Contains(p))
                        {
                            n.HasAnimal = true;
                            n.AnimalAge = 0;
                        }
                    }
                }
            }

            // 2. movement - random order so animals in the top left corner dont always go first
            var animals = new List<(int x, int y)>();
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (next[x, y].HasAnimal)
                        animals.Add((x, y));
            animals = animals.OrderBy(t => rnd.Next()).ToList();

            foreach (var (x, y) in animals)
            {
                if (next[x, y].HasPlant) continue; // has food, stays
                MoveAnimal(old, next, x, y);
            }

            // 3. animals eat plants on their field
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (next[x, y].HasAnimal && next[x, y].HasPlant)
                        next[x, y].HasPlant = false;

            Cells = next;
        }

        void MoveAnimal(Cell[,] old, Cell[,] next, int x, int y)
        {
            // a) plant right next to it
            var options = new List<(int, int)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = Wrap(x + dx, Width), ny = Wrap(y + dy, Height);
                    if (next[nx, ny].HasPlant && !old[nx, ny].HasAnimal)
                        options.Add((nx, ny));
                }
            }
            if (options.Count > 0)
            {
                var t = options[rnd.Next(options.Count)];
                Move(next, x, y, t.Item1, t.Item2);
                return;
            }

            // b) look for the closest plant in range M and go M fields in that direction
            int bestDist = int.MaxValue, bdx = 0, bdy = 0;
            for (int dx = -Rules.M; dx <= Rules.M; dx++)
            {
                for (int dy = -Rules.M; dy <= Rules.M; dy++)
                {
                    int nx = Wrap(x + dx, Width), ny = Wrap(y + dy, Height);
                    if (next[nx, ny].HasPlant)
                    {
                        int d = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        if (d < bestDist)
                        {
                            bestDist = d; bdx = dx; bdy = dy;
                        }
                    }
                }
            }
            if (bestDist != int.MaxValue)
            {
                int cx = x, cy = y;
                for (int i = 0; i < Rules.M; i++)
                {
                    if (bdx == 0 && bdy == 0) break;
                    int sx = Math.Sign(bdx), sy = Math.Sign(bdy);
                    int nx = Wrap(cx + sx, Width), ny = Wrap(cy + sy, Height);
                    if (old[nx, ny].HasAnimal) break;
                    cx = nx; cy = ny;
                    bdx -= sx; bdy -= sy;
                }
                if (cx != x || cy != y)
                {
                    Move(next, x, y, cx, cy);
                    return;
                }
            }

            // c) random free neighbour
            options.Clear();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = Wrap(x + dx, Width), ny = Wrap(y + dy, Height);
                    if (!old[nx, ny].HasAnimal)
                        options.Add((nx, ny));
                }
            }
            if (options.Count > 0)
            {
                var t = options[rnd.Next(options.Count)];
                Move(next, x, y, t.Item1, t.Item2);
            }
        }

        void Move(Cell[,] grid, int x, int y, int nx, int ny)
        {
            grid[nx, ny].HasAnimal = true;
            grid[nx, ny].AnimalAge = grid[x, y].AnimalAge;
            grid[x, y].HasAnimal = false;
            grid[x, y].AnimalAge = 0;
        }
    }
}
