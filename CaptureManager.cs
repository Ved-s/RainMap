using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Runtime.InteropServices;
using Color = Microsoft.Xna.Framework.Color;

namespace RainMap
{
    public static class CaptureManager
    {
        public static RenderTarget2D RenderTarget = null!;

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

            Rgba32[] colors = Array.Empty<Rgba32>();
            Image<Rgba32>? screenImage = null;

            for (int k = 0; k < region.Rooms.Count; k++)
            {
                Room room = region.Rooms[k];
                room.PrepareDraw();

                for (int i = 0; i < room.CameraScreens.Length; i++)
                {
                    TextureAsset? screen = room.CameraScreens[i];
                    if (screen is null)
                        continue;

                    Main.Instance.Window.Title = $"Rendering room {room.Name} ({k}/{region.Rooms.Count}), screen {i}/{room.CameraScreens.Length}";

                    Vector2 screenPos = -min + room.WorldPos + room.CameraPositions[i];

                    if (RenderTarget is null || RenderTarget.Width < screen.Texture.Width || RenderTarget.Height < screen.Texture.Height)
                    {
                        RenderTarget?.Dispose();
                        RenderTarget = new(Main.Instance.GraphicsDevice, screen.Texture.Width, screen.Texture.Height);
                    }

                    if (screenImage is null || screenImage.Width < screen.Texture.Width || screenImage.Height < screen.Texture.Height)
                    {
                        screenImage?.Dispose();
                        screenImage = new(screen.Texture.Width, screen.Texture.Height);
                    }

                    Main.Instance.GraphicsDevice.SetRenderTarget(RenderTarget);
                    Main.Instance.GraphicsDevice.Clear(Color.Transparent);

                    room.DrawScreen(true, i);

                    Main.Instance.GraphicsDevice.SetRenderTarget(null);

                    if (colors.Length != RenderTarget.Width * RenderTarget.Height)
                        colors = new Rgba32[RenderTarget.Width * RenderTarget.Height];

                    RenderTarget.GetData(colors);

                    for (int j = 0; j < screen.Texture.Height; j++)
                    {
                        int index = j * screen.Texture.Width;
                        colors.AsSpan(index, screen.Texture.Width)
                            .CopyTo(screenImage.DangerousGetPixelRowMemory(j).Span);
                    }
                    image.Mutate(f => f.DrawImage(screenImage, new SixLabors.ImageSharp.Point((int)screenPos.X, (int)screenPos.Y), 1));
                }

            }

            Main.Instance.Window.Title = "Drawing region connections";
            image.Mutate(f => region.DrawConnections(f, -min));
            return image;
        }
    }
}
