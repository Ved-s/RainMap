using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Color = Microsoft.Xna.Framework.Color;
using SLColor = SixLabors.ImageSharp.Color;

namespace RainMap
{
    public static class CaptureManager
    {
        public static Image<Rgba32> CaptureRegion(Region region)
        {
            Main.Instance.Window.Title = "Starting region capture";

            Vector2 min = new(float.MaxValue);
            Vector2 max = Vector2.Zero;

            foreach (Room room in region.Rooms)
            {
                min.X = Math.Min(min.X, room.WorldPos.X);
                min.Y = Math.Min(min.Y, room.WorldPos.Y);

                max.X = Math.Max(max.X, room.WorldPos.X + room.ScreenSize.X);
                max.Y = Math.Max(max.Y, room.WorldPos.Y + room.ScreenSize.Y);
            }

            Rgba32 bg = region.BackgroundColor is null ? new(0) : new(region.BackgroundColor.Value.PackedValue);
            Image<Rgba32> image = new((int)(max.X - min.X), (int)(max.Y - min.Y), bg);

            CaptureRenderer renderer = new(image)
            {
                Position = min,
                Size = max - min
            };

            for (int k = 0; k < region.Rooms.Count; k++)
            {
                Room room = region.Rooms[k];
                Main.Instance.Window.Title = $"Rendering room {room.Name} ({k}/{region.Rooms.Count})";

                int maxWidth = 0;
                int maxHeight = 0;

                foreach (TextureAsset? screen in room.CameraScreens)
                    if (screen is not null)
                    {
                        maxWidth = Math.Max(maxWidth, screen.Texture.Width);
                        maxHeight = Math.Max(maxHeight, screen.Texture.Height);
                    }
                renderer.EnsureRenderSize(maxWidth, maxHeight);

                Vector2 pos = renderer.Position;
                for (int i = 0; i < room.CameraScreens.Length; i++)
                {
                    TextureAsset? screen = room.CameraScreens[i];
                    if (screen is not null)
                    {
                        renderer.BeginCapture(screen.Texture.Width, screen.Texture.Height);
                        renderer.Position = room.WorldPos + room.CameraPositions[i];
                        room.DrawScreen(renderer, i);
                        renderer.Position = pos;
                        renderer.EndCapture(room.WorldPos + room.CameraPositions[i], screen.Texture.Width, screen.Texture.Height);
                    }
                }
                room.Rendered = true;
            }

            Main.Instance.Window.Title = "Drawing region connections";
            region.DrawConnections(renderer);
            renderer.Dispose();
            return image;
        }

        public class CaptureRenderer : Renderer, IDisposable
        {
            RenderTarget2D? RenderTarget = null;
            Rgba32[] Colors = Array.Empty<Rgba32>();
            Image<Rgba32>? ScreenImage = null;

            Image<Rgba32> Image;
            PointF[] LinePoints = new PointF[2];
            bool Capturing = false;

            public override Matrix Projection => Matrix.CreateOrthographicOffCenter(0, RenderTarget!.Width, RenderTarget!.Height, 0, 0, 1);

            public CaptureRenderer(Image<Rgba32> image)
            {
                Image = image;
            }

            public void BeginCapture(int width, int height)
            {
                EnsureRenderSize(width, height);

                Main.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
                Main.Instance.GraphicsDevice.Clear(Color.Transparent);
                Capturing = true;
            }

            public void EndCapture(Vector2 worldPos, int width, int height)
            {
                Main.Instance.GraphicsDevice.SetRenderTarget(null);

                RenderTarget!.GetData(Colors);

                for (int j = 0; j < height; j++)
                {
                    int index = j * width;
                    Colors.AsSpan(index, width)
                        .CopyTo(ScreenImage.DangerousGetPixelRowMemory(j).Span);
                }
                Vector2 drawPos = TransformVector(worldPos);
                Image.Mutate(f => f.DrawImage(ScreenImage, new SixLabors.ImageSharp.Point((int)drawPos.X, (int)drawPos.Y), 1));
                Capturing = false;
            }

            public void EnsureRenderSize(int width, int height)
            {
                if (RenderTarget is null || RenderTarget.Width < width || RenderTarget.Height < height)
                {
                    RenderTarget?.Dispose();
                    RenderTarget = new(Main.Instance.GraphicsDevice, width, height);
                }

                if (ScreenImage is null || ScreenImage.Width < width || ScreenImage.Height < height)
                {
                    ScreenImage?.Dispose();
                    ScreenImage = new(width, height);
                }

                if (Colors.Length != RenderTarget.Width * RenderTarget.Height)
                    Colors = new Rgba32[RenderTarget.Width * RenderTarget.Height];
            }

            public override void DrawTexture(Texture2D texture, Vector2 worldPos, Microsoft.Xna.Framework.Rectangle? source = null, Vector2? worldSize = null, Color? color = null)
            {
                int texWidth = source?.Width ?? texture.Width;
                int texHeight = source?.Height ?? texture.Height;
                bool cap = Capturing;

                if (!cap)
                    BeginCapture(texWidth, texHeight);

                Main.SpriteBatch.Draw(texture, Vector2.Zero, source, color ?? Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);

                if (!cap)
                    EndCapture(worldPos, texWidth, texHeight);
            }

            public override void DrawRect(Vector2 worldPos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
            {
                worldPos = TransformVector(worldPos);
                size *= Scale;

                if (border.HasValue)
                {
                    SLColor color = new(new Rgba32() { PackedValue = border.Value.PackedValue });   
                    Image.Mutate(f => f.Draw(color, thickness, new RectangleF(worldPos.X, worldPos.Y, size.X, size.Y)));
                }
                if (fill.HasValue)
                {
                    SLColor color = new(new Rgba32() { PackedValue = fill.Value.PackedValue });
                    Image.Mutate(f => f.Fill(color, new RectangleF(worldPos.X, worldPos.Y, size.X, size.Y)));
                }
            }

            public override void DrawLine(Vector2 worldPosA, Vector2 worldPosB, Color color, float thickness = 1)
            {
                SLColor slcolor = new(new Rgba32() { PackedValue = color.PackedValue });

                worldPosA = TransformVector(worldPosA);
                worldPosB = TransformVector(worldPosB);

                LinePoints[0] = new(worldPosA.X, worldPosA.Y);
                LinePoints[1] = new(worldPosB.X, worldPosB.Y);

                Image.Mutate(f => f.DrawLines(slcolor, thickness, LinePoints));
            }

            public void Dispose()
            {
                RenderTarget?.Dispose();
                ScreenImage?.Dispose();
            }
        }
    }
}
