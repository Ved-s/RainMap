using Microsoft.Xna.Framework;
using RainMap.Renderers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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
                        renderer.Size = room.ScreenSize;

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
    }
    
}
