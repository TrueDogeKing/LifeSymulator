using System.IO;
using System.Text;

namespace LifeSymulator.Model
{
    // text file format:
    // SIZE w h
    // R 1 2 3 4   (same for W B P Z)
    // M 2
    // L 10
    // BOARD
    // then h lines with w chars: . empty, P plant, A animal, X plant + animal
    // NOTE: animal age is not saved, after loading all animals have age 0
    public static class SaveLoad
    {
        public static void Save(World world, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("SIZE " + world.Width + " " + world.Height);
            sb.AppendLine("R " + string.Join(" ", world.Rules.R.OrderBy(x => x)));
            sb.AppendLine("W " + string.Join(" ", world.Rules.W.OrderBy(x => x)));
            sb.AppendLine("B " + string.Join(" ", world.Rules.B.OrderBy(x => x)));
            sb.AppendLine("P " + string.Join(" ", world.Rules.P.OrderBy(x => x)));
            sb.AppendLine("Z " + string.Join(" ", world.Rules.Z.OrderBy(x => x)));
            sb.AppendLine("M " + world.Rules.M);
            sb.AppendLine("L " + world.Rules.L);
            sb.AppendLine("BOARD");
            for (int y = 0; y < world.Height; y++)
            {
                for (int x = 0; x < world.Width; x++)
                {
                    Cell c = world.Cells[x, y];
                    if (c.HasPlant && c.HasAnimal) sb.Append('X');
                    else if (c.HasAnimal) sb.Append('A');
                    else if (c.HasPlant) sb.Append('P');
                    else sb.Append('.');
                }
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString());
        }

        public static World Load(string path)
        {
            string[] lines = File.ReadAllLines(path);
            World? world = null;
            Rules rules = new Rules();
            int i = 0;
            for (; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == "") continue;
                if (line == "BOARD") { i++; break; }

                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                string rest = string.Join(" ", parts.Skip(1));
                switch (parts[0])
                {
                    case "SIZE":
                        world = new World(int.Parse(parts[1]), int.Parse(parts[2]));
                        break;
                    case "R": rules.R = Rules.ParseSet(rest); break;
                    case "W": rules.W = Rules.ParseSet(rest); break;
                    case "B": rules.B = Rules.ParseSet(rest); break;
                    case "P": rules.P = Rules.ParseSet(rest); break;
                    case "Z": rules.Z = Rules.ParseSet(rest); break;
                    case "M": rules.M = int.Parse(parts[1]); break;
                    case "L": rules.L = int.Parse(parts[1]); break;
                }
            }

            if (world == null)
                throw new Exception("No SIZE in file");
            world.Rules = rules;

            for (int y = 0; y < world.Height && i + y < lines.Length; y++)
            {
                string row = lines[i + y];
                for (int x = 0; x < world.Width && x < row.Length; x++)
                {
                    char ch = row[x];
                    world.Cells[x, y].HasPlant = ch == 'P' || ch == 'X';
                    world.Cells[x, y].HasAnimal = ch == 'A' || ch == 'X';
                }
            }
            return world;
        }
    }
}
