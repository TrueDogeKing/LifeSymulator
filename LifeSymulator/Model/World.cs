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
    }
}
