namespace LifeSymulator.Model
{
    // one field of the board - can have a plant and one animal at the same time
    public class Cell
    {
        public bool HasPlant;
        public bool HasAnimal;
        public int AnimalAge;

        public Cell Copy()
        {
            return new Cell { HasPlant = HasPlant, HasAnimal = HasAnimal, AnimalAge = AnimalAge };
        }
    }
}
