namespace LifeSymulator.Model
{
    public class World
    {
        public int Width;
        public int Height;
        public Cell[,] Cells;
        public Rules Rules = new Rules();

        Random rnd = new Random();
        Cell[,]? spare; // buffer for the next generation

        // statistics (last step + total)
        public int PlantBirths, PlantDeaths, AnimalBirths, AnimalDeaths;
        public int TotalPlantBirths, TotalPlantDeaths, TotalAnimalBirths, TotalAnimalDeaths;

        public int PlantCount()
        {
            int n = 0;
            foreach (var c in Cells) if (c.HasPlant) n++;
            return n;
        }

        public int AnimalCount()
        {
            int n = 0;
            foreach (var c in Cells) if (c.HasAnimal) n++;
            return n;
        }

        public void ResetStats()
        {
            PlantBirths = PlantDeaths = AnimalBirths = AnimalDeaths = 0;
            TotalPlantBirths = TotalPlantDeaths = TotalAnimalBirths = TotalAnimalDeaths = 0;
        }

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

        // HashSet.Contains was slow in the main loop, so the rules are turned into bool tables (0..8 neighbours)
        static bool[] ToTable(HashSet<int> set)
        {
            var t = new bool[9];
            foreach (int v in set)
                if (v >= 0 && v <= 8) t[v] = true;
            return t;
        }

        int Wrap(int v, int max)
        {
            return (v % max + max) % max;
        }

        public void Step()
        {
            Cell[,] old = Cells;
            // second buffer is reused, creating 1M new objects every step was too slow
            if (spare == null || spare.GetLength(0) != Width || spare.GetLength(1) != Height || spare == old)
            {
                spare = new Cell[Width, Height];
                for (int x = 0; x < Width; x++)
                    for (int y = 0; y < Height; y++)
                        spare[x, y] = new Cell();
            }
            Cell[,] next = spare;
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Cell o = old[x, y], n = next[x, y];
                    n.HasPlant = o.HasPlant;
                    n.HasAnimal = o.HasAnimal;
                    n.AnimalAge = o.AnimalAge;
                }
            }

            bool[] r = ToTable(Rules.R), wt = ToTable(Rules.W), b = ToTable(Rules.B), pt = ToTable(Rules.P), z = ToTable(Rules.Z);

            PlantBirths = PlantDeaths = AnimalBirths = AnimalDeaths = 0;

            // 1. plants and animals birth/death (computed from old state)
            for (int x = 0; x < Width; x++)
            {
                int xl = x == 0 ? Width - 1 : x - 1;
                int xr = x == Width - 1 ? 0 : x + 1;
                for (int y = 0; y < Height; y++)
                {
                    Cell o = old[x, y];
                    Cell n = next[x, y];
                    int yu = y == 0 ? Height - 1 : y - 1;
                    int yd = y == Height - 1 ? 0 : y + 1;

                    // count plant and animal neighbours (8 around, board wraps around the edges)
                    int p = 0, a = 0;
                    Cell c;
                    c = old[xl, yu]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[x, yu]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[xr, yu]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[xl, y]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[xr, y]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[xl, yd]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[x, yd]; if (c.HasPlant) p++; if (c.HasAnimal) a++;
                    c = old[xr, yd]; if (c.HasPlant) p++; if (c.HasAnimal) a++;

                    if (!o.HasPlant && !o.HasAnimal)
                    {
                        if (r[p])
                        {
                            n.HasPlant = true;
                            PlantBirths++;
                        }
                    }
                    else if (o.HasPlant)
                    {
                        if (!wt[p])
                        {
                            n.HasPlant = false;
                            PlantDeaths++;
                        }
                    }

                    if (o.HasAnimal)
                    {
                        n.AnimalAge++;
                        if (n.AnimalAge > Rules.L || !z[a])
                        {
                            n.HasAnimal = false;
                            n.AnimalAge = 0;
                            AnimalDeaths++;
                        }
                    }
                    else
                    {
                        if (b[a] && pt[p])
                        {
                            n.HasAnimal = true;
                            n.AnimalAge = 0;
                            AnimalBirths++;
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
            for (int i = animals.Count - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (animals[i], animals[j]) = (animals[j], animals[i]);
            }

            foreach (var (x, y) in animals)
            {
                if (next[x, y].HasPlant) continue; // has food, stays
                MoveAnimal(next, x, y);
            }

            // 3. animals eat plants on their field
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (next[x, y].HasAnimal && next[x, y].HasPlant)
                    {
                        next[x, y].HasPlant = false;
                        PlantDeaths++; // eaten
                    }

            TotalPlantBirths += PlantBirths;
            TotalPlantDeaths += PlantDeaths;
            TotalAnimalBirths += AnimalBirths;
            TotalAnimalDeaths += AnimalDeaths;

            spare = old;
            Cells = next;
        }

        // checks the new grid so two animals never end up on the same field
        void MoveAnimal(Cell[,] next, int x, int y)
        {
            // a) plant right next to it
            var options = new List<(int, int)>();
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = Wrap(x + dx, Width), ny = Wrap(y + dy, Height);
                    if (next[nx, ny].HasPlant && !next[nx, ny].HasAnimal)
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
                    if (next[nx, ny].HasAnimal) break;
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
                    if (!next[nx, ny].HasAnimal)
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
