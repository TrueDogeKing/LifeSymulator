using System.ComponentModel;

namespace LifeSymulator
{
    public enum CellState
    {
        Empty,
        Plant,
        Animal,
        Both // animal standing on a plant
    }

    // this is what the ItemsControl displays (one per field)
    public class CellView : INotifyPropertyChanged
    {
        public int X { get; set; }
        public int Y { get; set; }

        private CellState state;
        public CellState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(State)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
