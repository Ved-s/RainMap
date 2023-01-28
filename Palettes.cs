using Microsoft.Xna.Framework.Graphics;
using RWAPI;
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

            string palName = $"palettes/palette{index}.png";

            pal = RainWorldAPI.Assets?.FindTexture(palName);

            Pals[index] = pal;
            return pal;
        }
    }
}
