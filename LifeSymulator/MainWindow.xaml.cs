using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;
using LifeSymulator.Model;

namespace LifeSymulator;

public partial class MainWindow : Window
{
    World world;

    public MainWindow()
    {
        InitializeComponent();
        world = new World(50, 50);
        board.LoadColorsFromResources();
        FillColorLists();
        board.ViewChanged += (s, e) => UpdateScrollBars();
        CreateBoard();
        Loaded += (s, e) => FitBoard(false);
        ShowRules();
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += Timer_Tick;
    }

    // called when the world object changes (new size / loaded from file)
    void CreateBoard()
    {
        board.World = world;
        board.ScrollTo(0, 0);
        RefreshBoard();
    }

    // ---------- zoom ----------

    // zoom levels for the +/- buttons
    double[] zoomLevels = { 0.5, 1, 2, 4, 8, 16, 32 };

    Point ViewCenter()
    {
        return new Point(board.ActualWidth / 2, board.ActualHeight / 2);
    }

    void ZoomTo(double size, bool animate)
    {
        size = Math.Max(BoardView.MinCellSize, Math.Min(BoardView.MaxCellSize, size));
        if (!animate)
        {
            // stop running animation first, otherwise it overrides the value
            board.BeginAnimation(BoardView.CellSizeProperty, null);
            board.CellSize = size;
            return;
        }
        var anim = new DoubleAnimation(size, TimeSpan.FromMilliseconds(300));
        anim.EasingFunction = new QuadraticEase();
        anim.FillBehavior = FillBehavior.Stop;
        // after animation the real value has to be set, otherwise it jumps back
        anim.Completed += (s, e) => board.CellSize = size;
        board.BeginAnimation(BoardView.CellSizeProperty, anim);
    }

    void FitBoard(bool animate)
    {
        if (board.ActualWidth <= 0) return;
        double size = Math.Min(board.ActualWidth / world.Width, board.ActualHeight / world.Height);
        board.ZoomAnchor = ViewCenter();
        ZoomTo(Math.Min(size, 16), animate);
    }

    private void btnZoomIn_Click(object sender, RoutedEventArgs e)
    {
        board.ZoomAnchor = ViewCenter();
        foreach (double z in zoomLevels)
        {
            if (z > board.CellSize + 0.01) { ZoomTo(z, true); return; }
        }
    }

    private void btnZoomOut_Click(object sender, RoutedEventArgs e)
    {
        board.ZoomAnchor = ViewCenter();
        for (int i = zoomLevels.Length - 1; i >= 0; i--)
        {
            if (zoomLevels[i] < board.CellSize - 0.01) { ZoomTo(zoomLevels[i], true); return; }
        }
    }

    private void btnFit_Click(object sender, RoutedEventArgs e)
    {
        FitBoard(true);
    }

    private void sliderZoom_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        board.ZoomAnchor = ViewCenter();
        board.BeginAnimation(BoardView.CellSizeProperty, null);
    }

    private void Board_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        board.ZoomAnchor = e.GetPosition(board);
        double factor = Math.Pow(1.15, e.Delta / 120.0);
        ZoomTo(board.CellSize * factor, false);
    }

    void UpdateScrollBars()
    {
        hScroll.Maximum = Math.Max(0, world.Width - board.VisibleCols);
        hScroll.ViewportSize = board.VisibleCols;
        hScroll.LargeChange = board.VisibleCols;
        hScroll.Value = board.OffsetX;
        vScroll.Maximum = Math.Max(0, world.Height - board.VisibleRows);
        vScroll.ViewportSize = board.VisibleRows;
        vScroll.LargeChange = board.VisibleRows;
        vScroll.Value = board.OffsetY;
    }

    private void ScrollBar_Scroll(object sender, System.Windows.Controls.Primitives.ScrollEventArgs e)
    {
        board.ScrollTo(hScroll.Value, vScroll.Value);
    }

    // moving the board with right mouse button
    bool dragging = false;
    Point dragStart;
    double dragStartX, dragStartY;

    private void Board_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        dragging = true;
        dragStart = e.GetPosition(board);
        dragStartX = board.OffsetX;
        dragStartY = board.OffsetY;
        board.CaptureMouse();
        board.Cursor = Cursors.SizeAll;
    }

    private void Board_MouseMove(object sender, MouseEventArgs e)
    {
        if (!dragging) return;
        Point p = e.GetPosition(board);
        board.ScrollTo(dragStartX - (p.X - dragStart.X) / board.CellSize,
                       dragStartY - (p.Y - dragStart.Y) / board.CellSize);
    }

    private void Board_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        dragging = false;
        board.ReleaseMouseCapture();
        board.Cursor = null;
    }

    // ---------- look of plants / animals ----------

    public class ColorItem
    {
        public string Name { get; set; } = "";
        public Color Color { get; set; }
        public SolidColorBrush Brush { get; set; } = Brushes.Transparent;
    }

    List<ColorItem> MakeColorList(Color def)
    {
        var list = new List<ColorItem>();
        list.Add(new ColorItem { Name = "Default", Color = def, Brush = new SolidColorBrush(def) });
        // all named colors from System.Windows.Media.Colors
        foreach (var prop in typeof(Colors).GetProperties())
        {
            var c = (Color)prop.GetValue(null)!;
            if (c.A == 0) continue; // Transparent
            list.Add(new ColorItem { Name = prop.Name, Color = c, Brush = new SolidColorBrush(c) });
        }
        return list;
    }

    Color defaultBothColor;

    void FillColorLists()
    {
        defaultBothColor = board.BothColor;
        cmbPlantColor.ItemsSource = MakeColorList(board.PlantColor);
        cmbPlantColor.SelectedIndex = 0;
        cmbAnimalColor.ItemsSource = MakeColorList(board.AnimalColor);
        cmbAnimalColor.SelectedIndex = 0;
        imgPlant.Source = board.PlantImage;
        imgAnimal.Source = board.AnimalImage;
    }

    private void cmbColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbPlantColor.SelectedItem is ColorItem p) board.PlantColor = p.Color;
        if (cmbAnimalColor.SelectedItem is ColorItem a) board.AnimalColor = a.Color;
        // animal on a plant = mix of both colors (or the one from Colors.xaml if nothing was changed)
        Color pc = board.PlantColor, ac = board.AnimalColor;
        if (cmbPlantColor.SelectedIndex <= 0 && cmbAnimalColor.SelectedIndex <= 0)
            board.BothColor = defaultBothColor;
        else
            board.BothColor = Color.FromRgb((byte)((pc.R + ac.R) / 2), (byte)((pc.G + ac.G) / 2), (byte)((pc.B + ac.B) / 2));
        board.Redraw();
    }

    private void cmbLook_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (board == null) return; // called during InitializeComponent
        board.UseImages = cmbLook.SelectedIndex == 1;
        if (board.UseImages && board.CellSize < BoardView.ImageMinCellSize)
        {
            // zoom in a bit, otherwise images are not visible
            board.ZoomAnchor = ViewCenter();
            ZoomTo(16, true);
        }
        board.Redraw();
    }

    ImageSource? PickImage()
    {
        var dlg = new OpenFileDialog();
        dlg.Filter = "Images (*.png;*.jpg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif";
        if (dlg.ShowDialog() != true) return null;
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(dlg.FileName);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.DecodePixelWidth = 64; // no need for big pictures
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not load image: " + ex.Message);
            return null;
        }
    }

    private void btnPlantImage_Click(object sender, RoutedEventArgs e)
    {
        var img = PickImage();
        if (img == null) return;
        board.PlantImage = img;
        imgPlant.Source = img;
        board.Redraw();
    }

    private void btnAnimalImage_Click(object sender, RoutedEventArgs e)
    {
        var img = PickImage();
        if (img == null) return;
        board.AnimalImage = img;
        imgAnimal.Source = img;
        board.Redraw();
    }

    private void btnDefaultImages_Click(object sender, RoutedEventArgs e)
    {
        board.PlantImage = TryFindResource("PlantImage") as ImageSource;
        board.AnimalImage = TryFindResource("AnimalImage") as ImageSource;
        imgPlant.Source = board.PlantImage;
        imgAnimal.Source = board.AnimalImage;
        board.Redraw();
    }

    void RefreshBoard()
    {
        board.Redraw();
        UpdateStats();
    }

    void UpdateStats()
    {
        txtGeneration.Text = "Generation: " + world.Generation;
        txtPlants.Text = "Plants: " + world.PlantCount();
        txtAnimals.Text = "Animals: " + world.AnimalCount();
        // last step (total)
        txtBirths.Text = $"Births: plants {world.PlantBirths} ({world.TotalPlantBirths}), animals {world.AnimalBirths} ({world.TotalAnimalBirths})";
        txtDeaths.Text = $"Deaths: plants {world.PlantDeaths} ({world.TotalPlantDeaths}), animals {world.AnimalDeaths} ({world.TotalAnimalDeaths})";
    }

    // clicked somewhere on the board, find which cell it was
    private void Board_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (running) return;
        if (!board.GetCell(e.GetPosition(board), out int x, out int y)) return;
        Cell c = world.Cells[x, y];
        // cycle empty -> plant -> animal -> both -> empty
        if (!c.HasPlant && !c.HasAnimal)
        {
            c.HasPlant = true;
        }
        else if (c.HasPlant && !c.HasAnimal)
        {
            c.HasPlant = false; c.HasAnimal = true; c.AnimalAge = 0;
        }
        else if (!c.HasPlant && c.HasAnimal)
        {
            c.HasPlant = true;
        }
        else
        {
            c.HasPlant = false; c.HasAnimal = false;
        }
        RefreshBoard();
    }

    private void btnNew_Click(object sender, RoutedEventArgs e)
    {
        int w, h;
        if (!int.TryParse(txtWidth.Text, out w) || !int.TryParse(txtHeight.Text, out h))
        {
            MessageBox.Show("Size must be a number");
            return;
        }
        if (w < 50 || h < 50 || w > 1000 || h > 1000)
        {
            MessageBox.Show("Size must be between 50 and 1000");
            return;
        }
        var rules = world.Rules;
        world = new World(w, h);
        world.Rules = rules;
        CreateBoard();
        FitBoard(true);
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        world.Clear();
        world.ResetStats();
        RefreshBoard();
    }

    private void btnRandom_Click(object sender, RoutedEventArgs e)
    {
        world.Randomize();
        world.ResetStats();
        RefreshBoard();
    }

    private void btnStep_Click(object sender, RoutedEventArgs e)
    {
        DoStep();
    }

    DispatcherTimer timer = new DispatcherTimer();
    bool running = false;

    private void btnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!running)
        {
            running = true;
            btnStart.Content = "Stop";
            timer.Start();
        }
        else
        {
            running = false;
            btnStart.Content = "Start";
            timer.Stop();
        }
        // no editing while animation is running
        btnNew.IsEnabled = !running;
        btnClear.IsEnabled = !running;
        btnRandom.IsEnabled = !running;
        btnStep.IsEnabled = !running;
        btnLoad.IsEnabled = !running;
        btnApply.IsEnabled = !running;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        DoStep();
    }

    private void sliderSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // value = steps per second
        timer.Interval = TimeSpan.FromMilliseconds(1000.0 / e.NewValue);
    }

    void DoStep()
    {
        world.Step();
        RefreshBoard();
    }

    void ShowRules()
    {
        txtR.Text = Rules.SetToString(world.Rules.R);
        txtW.Text = Rules.SetToString(world.Rules.W);
        txtB.Text = Rules.SetToString(world.Rules.B);
        txtP.Text = Rules.SetToString(world.Rules.P);
        txtZ.Text = Rules.SetToString(world.Rules.Z);
        txtM.Text = world.Rules.M.ToString();
        txtL.Text = world.Rules.L.ToString();
    }

    private void btnApply_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var r = new Rules();
            r.R = Rules.ParseSet(txtR.Text);
            r.W = Rules.ParseSet(txtW.Text);
            r.B = Rules.ParseSet(txtB.Text);
            r.P = Rules.ParseSet(txtP.Text);
            r.Z = Rules.ParseSet(txtZ.Text);
            r.M = int.Parse(txtM.Text);
            r.L = int.Parse(txtL.Text);
            world.Rules = r;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Wrong rules: " + ex.Message);
        }
    }

    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog();
        dlg.Filter = "Text files (*.txt)|*.txt";
        dlg.FileName = "board.txt";
        if (dlg.ShowDialog() == true)
        {
            SaveLoad.Save(world, dlg.FileName);
        }
    }

    private void btnLoad_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog();
        dlg.Filter = "Text files (*.txt)|*.txt|All files|*.*";
        if (dlg.ShowDialog() == true)
        {
            try
            {
                world = SaveLoad.Load(dlg.FileName);
                        txtWidth.Text = world.Width.ToString();
                txtHeight.Text = world.Height.ToString();
                CreateBoard();
                ShowRules();
                FitBoard(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load file: " + ex.Message);
            }
        }
    }
}
