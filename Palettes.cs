using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public static class Palettes
    {
        static List<Texture2D?> Pals = new();

        static string[] Paths = Array.Empty<string>();

        public static Texture2D? GetPalette(int index)
        {
            Texture2D? pal = null;

            if (index < Pals.Count)
                pal = Pals[index];

            if (pal is not null)
                return pal;

            Pals.EnsureCapacity(index + 1);
            while (Pals.Count <= index)
                Pals.Add(null);

            string palName = $"palette{index}.png";

            foreach (string path in Paths)
            {
                string fullPath = Path.Combine(path, palName);
                if (File.Exists(fullPath))
                {
                    pal = Texture2D.FromFile(Main.Instance.GraphicsDevice, fullPath);
                    break;
                }
            }

            Pals[index] = pal;
            return pal;
        }

        public static void SearchPalettes(string path)
        {
            List<string> paths = new();

            string basePals = Path.Combine(path, "palettes");
            if (Directory.Exists(basePals))
                paths.Add(basePals);

            string mods = Path.Combine(path, "mods");

            if (Directory.Exists(mods))
                foreach (string mod in Directory.EnumerateDirectories(mods))
                {
                    string modPalettes = Path.Combine(mod, "palettes");
                    if (Directory.Exists(modPalettes))
                        paths.Add(modPalettes);
                }

            Paths = paths.ToArray();
        }
    }
}
