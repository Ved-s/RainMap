using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace RainMap
{
	public static class Palettes
	{
		private static List<Texture2D?> Pals = new();
		private static string? Path;

		public static Texture2D? GetPalette(int index)
		{
			if (Path is null)
				return null;

			if (index >= Pals.Count || Pals[index] is null)
			{
				Pals.EnsureCapacity(index + 1);
				while (Pals.Count <= index)
					Pals.Add(null);

				Pals[index] = Texture2D.FromFile(Main.Instance.GraphicsDevice, string.Format(Path, index));
			}

			return Pals[index];
		}

		public static void SetPalettePath(string path)
		{
			Path = System.IO.Path.Combine(path, "palette{0}.png");
			Pals.Clear();
		}
	}
}
