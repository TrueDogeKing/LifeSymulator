using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        CreateBoard();
        ShowRules();
        timer.Interval = TimeSpan.FromMilliseconds(200);
        timer.Tick += Timer_Tick;
    }

    // called when the world object changes (new size / loaded from file)
    void CreateBoard()
    {
        board.World = world;
        RefreshBoard();
    }

    void RefreshBoard()
    {
        board.Redraw();
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
        generation++;
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
                generation = 0;
                txtWidth.Text = world.Width.ToString();
                txtHeight.Text = world.Height.ToString();
                CreateBoard();
                ShowRules();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load file: " + ex.Message);
            }
        }
    }
}
