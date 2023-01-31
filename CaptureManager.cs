using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RainMap.Renderers;
using RainMap.Structures;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Normalization;
using System;
using System.Buffers;
using System.IO;
using Point = Microsoft.Xna.Framework.Point;

namespace RainMap
{
    public static class CaptureManager
    {
        public static Image<Rgba32> CaptureEntireRegion(Region region, float scale)
        {
            Main.Instance.Window.Title = "Starting region capture";

            if (Main.MappingMode)
                return CaptureRegionTileMap(region);

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
            Image<Rgba32> image = new((int)((max.X - min.X) * scale), (int)((max.Y - min.Y) * scale), bg);

            CaptureRenderer renderer = new(image)
            {
                Scale = scale,
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
                renderer.EnsureRenderSize((int)(maxWidth * scale), (int)(maxHeight * 1));

                Vector2 pos = renderer.Position;
                for (int i = 0; i < room.CameraScreens.Length; i++)
                {
                    TextureAsset? screen = room.CameraScreens[i];
                    if (screen is not null)
                    {
                        renderer.Size = room.ScreenSize * scale;
                        renderer.BeginCapture((int)(screen.Texture.Width * scale), (int)(screen.Texture.Height * scale));
                        renderer.Position = room.WorldPos + room.CameraPositions[i];
                        room.DrawScreen(renderer, i);
                        renderer.Position = pos;
                        renderer.EndCapture(room.WorldPos + room.CameraPositions[i], (int)(screen.Texture.Width * scale), (int)(screen.Texture.Height * scale));
                    }
                }
                room.Rendered = true;
            }

            Main.Instance.Window.Title = "Drawing region connections";
            region.DrawConnections(renderer);
            renderer.Dispose();
            return image;
        }

        static Image<Rgba32> CaptureRegionTileMap(Region region)
        {
            Point tl = new Point(0, 0);
            Point br = new Point(0, 0);

            foreach (Room room in region.Rooms)
            {
                Point roomtl = (room.WorldPos / 20).ToPoint();
                
                tl.X = Math.Min(tl.X, roomtl.X);
                tl.Y = Math.Min(tl.Y, roomtl.Y);

                br.X = Math.Max(br.X, roomtl.X + room.Size.X);
                br.Y = Math.Max(br.Y, roomtl.Y + room.Size.Y);
            }

            Image<Rgba32> image = new(br.X - tl.X, br.Y - tl.Y);

            foreach (Room room in region.Rooms)
            {
                if (room.TileMap is null)
                    room.ResetTileMap();

                Rgba32[] colors = ArrayPool<Rgba32>.Shared.Rent(room.Size.X * room.Size.Y);

                room.TileMap!.GetData(colors, 0, room.Size.X * room.Size.Y);

                int drawPosX = (int)room.WorldPos.X / 20 - tl.X;
                int drawPosY = (int)room.WorldPos.Y / 20 - tl.Y;

                for (int j = 0; j < room.Size.Y; j++)
                {
                    Span<Rgba32> src = colors.AsSpan(j * room.Size.X, room.Size.X);
                    Span<Rgba32> dst = image.DangerousGetPixelRowMemory(drawPosY + j).Span.Slice(drawPosX, room.Size.X);
                    src.CopyTo(dst);
                }
            }

            return image;
        }

        public static void CaptureRegionRooms(Region region, float scale)
        {
            string dir = Main.MappingMode ? $"RegionRender_{region.Id}_tiles" : $"RegionRender_{region.Id}";

            Directory.CreateDirectory(dir);

            Rgba32 bg = region.BackgroundColor is null ? new(0) : new(region.BackgroundColor.Value.PackedValue);
            for (int k = 0; k < region.Rooms.Count; k++)
            {
                Room room = region.Rooms[k];
                Main.Instance.Window.Title = $"Rendering room {room.Name} ({k}/{region.Rooms.Count})";
                Image<Rgba32> capturedRoom = CaptureRoom(room, bg, scale);

                string fileName = $"{room.Name}{(Main.RenderRoomTiles && !Main.MappingMode ? "_tiles" : "")}.png";
                string filePath = Path.Combine(dir, fileName);

                using FileStream outputStream = File.Create(filePath);
                capturedRoom.Save(outputStream, new PngEncoder());
                capturedRoom.Dispose();
                GC.Collect();
            }
        }

        public static Image<Rgba32> CaptureRoom(Room room, Rgba32 bg, float scale)
        {
            if (Main.MappingMode)
            {
                if (room.TileMap is null)
                    room.ResetTileMap();

                Rgba32[] buffer = ArrayPool<Rgba32>.Shared.Rent(room.Size.X * room.Size.Y);
                room.TileMap!.GetData(buffer, 0, room.Size.X * room.Size.Y);

                Image<Rgba32> map = new(room.Size.X, room.Size.Y);

                for (int i = 0; i < room.Size.Y; i++)
                {
                    Span<Rgba32> src = buffer.AsSpan()
                        .Slice(i * room.Size.X, room.Size.X);

                    Span<Rgba32> dst = map.DangerousGetPixelRowMemory(i).Span;
                    src.CopyTo(dst);
                }

                return map;
            }

            Image<Rgba32> image = new((int)(room.ScreenSize.X * scale), (int)(room.ScreenSize.Y * scale), bg);
            CaptureRenderer renderer = new(image)
            {
                Scale = scale,
                Position = room.WorldPos + room.ScreenStart
            };

            for (int i = 0; i < room.CameraScreens.Length; i++)
            {
                TextureAsset? screen = room.CameraScreens[i];
                if (screen is not null && screen.Loaded)
                {
                    renderer.Size = room.ScreenSize * scale;
                    renderer.Position = room.WorldPos + room.CameraPositions[i];
                    renderer.BeginCapture((int)(screen.Texture.Width * scale), (int)(screen.Texture.Height * scale));
                    room.DrawScreen(renderer, i);
                    renderer.Position = room.WorldPos + room.ScreenStart;
                    renderer.EndCapture(room.WorldPos + room.CameraPositions[i], (int)(screen.Texture.Width * scale), (int)(screen.Texture.Height * scale));
                }
            }
            room.Rendered = true;

            renderer.Dispose();
            return image;
        }
    }

}
