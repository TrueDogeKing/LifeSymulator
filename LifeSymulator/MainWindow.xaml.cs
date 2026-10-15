using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using LifeSymulator.Model;

namespace LifeSymulator;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    World world;
    ObservableCollection<CellView> cells = new ObservableCollection<CellView>();

    private int boardWidth;
    public int BoardWidth
    {
        get { return boardWidth; }
        set { boardWidth = value; OnPropertyChanged(nameof(BoardWidth)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string name)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public MainWindow()
    {
        InitializeComponent();
        world = new World(50, 50);
        CreateBoard();
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += Timer_Tick;
    }

    // builds the CellView list from the world (only when size changes)
    void CreateBoard()
    {
        BoardWidth = world.Width;
        cells = new ObservableCollection<CellView>();
        // UniformGrid fills row by row so y is outer loop
        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
                cells.Add(new CellView { X = x, Y = y });
        board.ItemsSource = cells;
        RefreshBoard();
    }

    // copies state from model to views
    void RefreshBoard()
    {
        foreach (var cv in cells)
        {
            Cell c = world.Cells[cv.X, cv.Y];
            if (c.HasPlant && c.HasAnimal) cv.State = CellState.Both;
            else if (c.HasAnimal) cv.State = CellState.Animal;
            else if (c.HasPlant) cv.State = CellState.Plant;
            else cv.State = CellState.Empty;
        }
        UpdateStats();
    }

    void UpdateStats()
    {
        txtGeneration.Text = "Generation: " + generation;
        txtPlants.Text = "Plants: " + world.PlantCount();
        txtAnimals.Text = "Animals: " + world.AnimalCount();
        // last step (total)
        txtBirths.Text = $"Births: plants {world.PlantBirths} ({world.TotalPlantBirths}), animals {world.AnimalBirths} ({world.TotalAnimalBirths})";
        txtDeaths.Text = $"Deaths: plants {world.PlantDeaths} ({world.TotalPlantDeaths}), animals {world.AnimalDeaths} ({world.TotalAnimalDeaths})";
    }

    private void Cell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (running) return;
        var cv = (CellView)((FrameworkElement)sender).DataContext;
        Cell c = world.Cells[cv.X, cv.Y];
        // cycle empty -> plant -> animal -> both -> empty
        switch (cv.State)
        {
            case CellState.Empty:
                c.HasPlant = true; c.HasAnimal = false;
                break;
            case CellState.Plant:
                c.HasPlant = false; c.HasAnimal = true; c.AnimalAge = 0;
                break;
            case CellState.Animal:
                c.HasPlant = true; c.HasAnimal = true;
                break;
            case CellState.Both:
                c.HasPlant = false; c.HasAnimal = false;
                break;
        }
        RefreshBoard();
    }

    private void btnNew_Click(object sender, RoutedEventArgs e)
    {
        int w = int.Parse(txtWidth.Text);
        int h = int.Parse(txtHeight.Text);
        var rules = world.Rules;
        world = new World(w, h);
        world.Rules = rules;
        generation = 0;
        CreateBoard();
    }

    private void btnClear_Click(object sender, RoutedEventArgs e)
    {
        world.Clear();
        world.ResetStats();
        generation = 0;
        RefreshBoard();
    }

    private void btnRandom_Click(object sender, RoutedEventArgs e)
    {
        world.Randomize();
        world.ResetStats();
        generation = 0;
        RefreshBoard();
    }

    int generation = 0;

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
        generation++;
        RefreshBoard();
    }
}
