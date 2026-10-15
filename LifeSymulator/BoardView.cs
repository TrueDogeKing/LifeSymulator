using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LifeSymulator.Model;

namespace LifeSymulator
{
    // Shows the board. Only the visible part is rendered (virtualization),
    // the whole World is still simulated - this class only looks at it.
    public class BoardView : FrameworkElement
    {
        public World? World;

        // size of one cell on screen, dependency property so it can be bound and animated (smooth zoom)
        public static readonly DependencyProperty CellSizeProperty = DependencyProperty.Register(
            "CellSize", typeof(double), typeof(BoardView),
            new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender, OnCellSizeChanged),
            v => (double)v >= MinCellSize && (double)v <= MaxCellSize);

        public double CellSize
        {
            get { return (double)GetValue(CellSizeProperty); }
            set { SetValue(CellSizeProperty, value); }
        }

        public const double MinCellSize = 0.5;
        public const double MaxCellSize = 40;

        // top left visible cell (can be fractional)
        public double OffsetX, OffsetY;

        // point on the screen that stays in place when zooming (mouse position or center)
        public Point ZoomAnchor;

        // fired when zoom / offset changes so the window can update scrollbars
        public event EventHandler? ViewChanged;

        public Color EmptyColor = Colors.White;
        public Color PlantColor = Colors.Green;
        public Color AnimalColor = Colors.Red;
        public Color BothColor = Colors.Orange;

        // bitmap only as big as the visible part of the board
        WriteableBitmap? bmp;
        int[] pixels = new int[0];
        Pen gridPen = new Pen(new SolidColorBrush(Color.FromArgb(60, 0, 0, 0)), 1);

        public BoardView()
        {
            gridPen.Freeze();
            // no blur when scaling up
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            ClipToBounds = true;
            SizeChanged += (s, e) => { ClampOffsets(); ViewChanged?.Invoke(this, EventArgs.Empty); };
        }

        static void OnCellSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var bv = (BoardView)d;
            double oldCs = (double)e.OldValue, newCs = (double)e.NewValue;
            // keep the cell under ZoomAnchor in the same place
            bv.OffsetX += bv.ZoomAnchor.X / oldCs - bv.ZoomAnchor.X / newCs;
            bv.OffsetY += bv.ZoomAnchor.Y / oldCs - bv.ZoomAnchor.Y / newCs;
            bv.ClampOffsets();
            bv.ViewChanged?.Invoke(bv, EventArgs.Empty);
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

        // how many cells fit on the screen
        public double VisibleCols { get { return ActualWidth / CellSize; } }
        public double VisibleRows { get { return ActualHeight / CellSize; } }

        public void ClampOffsets()
        {
            if (World == null) return;
            OffsetX = Math.Max(0, Math.Min(OffsetX, World.Width - VisibleCols));
            OffsetY = Math.Max(0, Math.Min(OffsetY, World.Height - VisibleRows));
        }

        public void ScrollTo(double x, double y)
        {
            OffsetX = x;
            OffsetY = y;
            ClampOffsets();
            InvalidateVisual();
            ViewChanged?.Invoke(this, EventArgs.Empty);
        }

        // when the board is smaller than the screen it is drawn in the middle
        Point Origin()
        {
            if (World == null) return new Point();
            double cs = CellSize;
            return new Point(Math.Max(0, (ActualWidth - World.Width * cs) / 2),
                             Math.Max(0, (ActualHeight - World.Height * cs) / 2));
        }

        // call after every change of the world
        public void Redraw()
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            // transparent background so mouse events work everywhere
            dc.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, ActualWidth, ActualHeight));
            if (World == null) return;
            double cs = CellSize;
            Point o = Origin();

            // range of visible cells
            int x0 = (int)Math.Floor(OffsetX);
            int y0 = (int)Math.Floor(OffsetY);
            int x1 = Math.Min(World.Width, (int)Math.Ceiling(OffsetX + VisibleCols));
            int y1 = Math.Min(World.Height, (int)Math.Ceiling(OffsetY + VisibleRows));
            int w = x1 - x0, h = y1 - y0;
            if (w <= 0 || h <= 0) return;

            if (bmp == null || bmp.PixelWidth != w || bmp.PixelHeight != h)
            {
                bmp = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null);
                pixels = new int[w * h];
            }

            int empty = ToInt(EmptyColor), plant = ToInt(PlantColor), animal = ToInt(AnimalColor), both = ToInt(BothColor);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Cell c = World.Cells[x0 + x, y0 + y];
                    int col = empty;
                    if (c.HasPlant && c.HasAnimal) col = both;
                    else if (c.HasAnimal) col = animal;
                    else if (c.HasPlant) col = plant;
                    pixels[y * w + x] = col;
                }
            }
            bmp.WritePixels(new Int32Rect(0, 0, w, h), pixels, w * 4, 0);

            // screen position of cell x0,y0
            double sx = o.X + (x0 - OffsetX) * cs;
            double sy = o.Y + (y0 - OffsetY) * cs;
            dc.DrawImage(bmp, new Rect(sx, sy, w * cs, h * cs));

            // grid lines only when zoomed in enough
            if (cs >= 6)
            {
                for (int x = 0; x <= w; x++)
                    dc.DrawLine(gridPen, new Point(sx + x * cs, sy), new Point(sx + x * cs, sy + h * cs));
                for (int y = 0; y <= h; y++)
                    dc.DrawLine(gridPen, new Point(sx, sy + y * cs), new Point(sx + w * cs, sy + y * cs));
            }
        }

        // position on screen -> cell, false if outside
        public bool GetCell(Point p, out int x, out int y)
        {
            Point o = Origin();
            x = (int)Math.Floor(OffsetX + (p.X - o.X) / CellSize);
            y = (int)Math.Floor(OffsetY + (p.Y - o.Y) / CellSize);
            return World != null && x >= 0 && y >= 0 && x < World.Width && y < World.Height;
        }
    }
}
