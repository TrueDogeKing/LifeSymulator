namespace LifeSymulator.Model
{
    public class Rules
    {
        // R - plant is born when it has this many plant neighbours
        public HashSet<int> R = new HashSet<int> { 1, 2, 3, 4 };
        // W - plant survives
        public HashSet<int> W = new HashSet<int> { 1, 2, 3, 4, 5, 6 };
        // B - animal is born when it has this many animal neighbours
        public HashSet<int> B = new HashSet<int> { 2, 3 };
        // P - ...and this many plant neighbours
        public HashSet<int> P = new HashSet<int> { 0, 1 };
        // Z - animal survives
        public HashSet<int> Z = new HashSet<int> { 0, 1, 2, 3 };

        public int M = 2;   // how far animal can move
        public int L = 10;  // how many years animal lives

        // "1,2,3" or "1 2 3" or "123" -> set
        public static HashSet<int> ParseSet(string text)
        {
            var result = new HashSet<int>();
            text = text.Trim();
            if (text.Contains(',') || text.Contains(' '))
            {
                foreach (var s in text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    result.Add(int.Parse(s));
                }
            }
            else
            {
                // every digit is one number (neighbours are 0..8 anyway)
                foreach (char c in text)
                {
                    if (char.IsDigit(c))
                        result.Add(c - '0');
                }
            }
            return result;
        }

        public static string SetToString(HashSet<int> set)
        {
            return string.Join(",", set.OrderBy(x => x));
        }
    }
}
