using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LifeSymulator.Model;

namespace LifeSymulator
{
    // draws the whole board as one bitmap (1 pixel = 1 cell) and scales it up,
    // ItemsControl with 1 element per cell was way too slow for big boards
    public class BoardView : FrameworkElement
    {
        public World? World;
        public double CellSize = 10;

        public Color EmptyColor = Colors.White;
        public Color PlantColor = Colors.Green;
        public Color AnimalColor = Colors.Red;
        public Color BothColor = Colors.Orange;

        WriteableBitmap? bmp;
        int[] pixels = new int[0];

        public BoardView()
        {
            // no blur when scaling up
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        }

        public void LoadColorsFromResources()
        {
            EmptyColor = GetColor("EmptyBrush", EmptyColor);
            PlantColor = GetColor("PlantBrush", PlantColor);
            AnimalColor = GetColor("AnimalBrush", AnimalColor);
            BothColor = GetColor("BothBrush", BothColor);
        }

        Color GetColor(string key, Color def)
        {
            if (TryFindResource(key) is SolidColorBrush b) return b.Color;
            return def;
        }

        static int ToInt(Color c)
        {
            return (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;
        }

        // call after every change of the world
        public void Redraw()
        {
            if (World == null) return;
            int w = World.Width, h = World.Height;
            if (bmp == null || bmp.PixelWidth != w || bmp.PixelHeight != h)
            {
                bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                pixels = new int[w * h];
                InvalidateMeasure();
            }

            int empty = ToInt(EmptyColor), plant = ToInt(PlantColor), animal = ToInt(AnimalColor), both = ToInt(BothColor);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Cell c = World.Cells[x, y];
                    int col = empty;
                    if (c.HasPlant && c.HasAnimal) col = both;
                    else if (c.HasAnimal) col = animal;
                    else if (c.HasPlant) col = plant;
                    pixels[y * w + x] = col;
                }
            }
            bmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);
            InvalidateVisual();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (World == null) return new Size(0, 0);
            return new Size(World.Width * CellSize, World.Height * CellSize);
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (World == null || bmp == null) return;
            dc.DrawImage(bmp, new Rect(0, 0, World.Width * CellSize, World.Height * CellSize));
        }

        // position in pixels -> cell, false if outside
        public bool GetCell(Point p, out int x, out int y)
        {
            x = (int)(p.X / CellSize);
            y = (int)(p.Y / CellSize);
            return World != null && p.X >= 0 && p.Y >= 0 && x < World.Width && y < World.Height;
        }
    }
}
