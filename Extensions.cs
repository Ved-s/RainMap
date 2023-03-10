using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RWAPI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public static class Extensions
    {
        public static void SetAlpha(ref this Color color, float alpha)
        {
            color.A = (byte)(Math.Clamp(alpha, 0, 1) * 255);
        }

        public static bool TryGet<T>(this T[] array, int index, out T value)
        {
            if (index < array.Length)
            {
                value = array[index];
                return true;
            }
            value = default!;
            return false;
        }

        public static Color GetPixel(this Texture2D texture, int x, int y)
        {
            Color c = default;

            Main.InvokeMainThread(() =>
            {
                Color[] color = ArrayPool<Color>.Shared.Rent(1);

                Rectangle rect = new(x, y, 1, 1);
                texture.GetData(0, rect, color, 0, 1);

                c = color[0];
                ArrayPool<Color>.Shared.Return(color);
            });

            return c;
        }

        public static Vector2 Size(this Texture2D texture)
        {
            return new(texture.Width, texture.Height);
        }

        public static void DrawLine(this SpriteBatch spriteBatch, Vector2 a, Vector2 b, Color color, float thickness = 1)
        {
            Vector2 diff = b - a;
            float angle = MathF.Atan2(diff.Y, diff.X);
            spriteBatch.Draw(Main.Pixel, a, null, color, angle, new Vector2(0, .5f), new Vector2(diff.Length(), thickness), SpriteEffects.None, 0);
        }

        public static void DrawRect(this SpriteBatch spriteBatch, Vector2 pos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            if (fill.HasValue)
            {
                spriteBatch.Draw(Main.Pixel, pos, null, fill.Value, 0f, Vector2.Zero, size, SpriteEffects.None, 0);
            }
            if (border.HasValue)
            {
                spriteBatch.DrawRect(new(pos.X + thickness, pos.Y), new(size.X - thickness, thickness), border.Value);
                spriteBatch.DrawRect(pos, new(thickness, size.Y - thickness), border.Value);

                if (size.Y > thickness)
                    spriteBatch.DrawRect(new(pos.X, (pos.Y + size.Y) - thickness), new(Math.Max(thickness, size.X - thickness), thickness), border.Value);

                if (size.X > thickness)
                    spriteBatch.DrawRect(new((pos.X + size.X) - thickness, pos.Y + thickness), new(thickness, Math.Max(thickness, size.Y - thickness)), border.Value);
            }
        }

        public static void DrawStringAligned(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Vector2 align)
        {
            Vector2 size = spriteFont.MeasureString(text);
            spriteBatch.DrawString(spriteFont, text, position - size * align, color);
        }

        public static void DrawStringShaded(this SpriteBatch spriteBatch, SpriteFont spriteFont, string text, Vector2 position, Color color, Color? shadeColor = null)
        {
            shadeColor ??= Color.Black;

            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, -1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(0, 1), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(-1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position + new Vector2(1, 0), shadeColor.Value);
            spriteBatch.DrawString(spriteFont, text, position, color);
        }

        public static void DrawPoint(this SpriteBatch spriteBatch, Vector2 pos, float size, Color color)
        {
            spriteBatch.DrawRect(pos - new Vector2(size/2), new Vector2(size), color);
        }

        public static Texture2D? FindTexture(this MultiDirectory dir, string path)
        {
            string? fullPath = dir.FindFile(path);
            if (fullPath is null)
                return null;

            return Texture2D.FromFile(Main.Instance.GraphicsDevice, fullPath);
        }
    }
}
