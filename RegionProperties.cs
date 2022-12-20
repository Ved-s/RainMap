using System;
using System.IO;

namespace RainMap
{
    public class RegionProperties
    {
        public string? DefaultTemplate { get => defaultTemplate; set => defaultTemplate = value; }
        public int? DefaultPalette { get => defaultPalette; set => defaultPalette = value; }

        string? defaultTemplate;
        int? defaultPalette;

        public static RegionProperties Load(string filePath)
        {
            RegionProperties props = new();

            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(':'))
                    continue;

                string[] split = lines[i].Split(':', 2, StringSplitOptions.TrimEntries);

                switch (split[0])
                {
                    case "Room Setting Templates":
                        props.DefaultTemplate = split[1].Split(',')[0].Trim();
                        break;

                    case "Palette" when int.TryParse(split[1], out int palette):
                        props.DefaultPalette = palette;
                        break;
                }
            }

            return props;
        }
    }
}
