using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainMap
{
    public class Room
    {
        static Point[] Directions = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };

        public string Name = null!;
        public Point Size;

        public Vector2 WorldPos;
        

        public float WaterLevel;
        public bool WaterInFrontOfTerrain;
        public Vector2 LightAngle;
        public Vector2[] CameraPositions = null!;

        public TextureAsset?[] CameraScreens = null!;

        public Tile[,] Tiles = null!;

        public (Room Target, int Exit, int TargetExit)[]? Connections;

        public Point[] RoomExitEntrances = null!;
        public Point[] RoomExits = null!;
        public Point[] RoomShortcuts = null!;

        public Vector2 ScreenStart;
        public Vector2 ScreenSize;

        public RoomSettings? Settings;
        public WaterData? Water;
        public bool Rendered;

        public string FilePath = null!;

        public static Room Load(string roomPath)
        {
            Room room = new();
            room.FilePath = roomPath;

            string[] lines = File.ReadAllLines(roomPath);

            if (lines.TryGet(0, out string name))
                room.Name = name;

            if (lines.TryGet(1, out string sizeWater))
            {
                string[] swArray = sizeWater.Split('|');
                if (swArray.TryGet(0, out string size))
                {
                    string[] sArray = size.Split('*');
                    if (sArray.TryGet(0, out string widthStr) && int.TryParse(widthStr, out int width))
                        room.Size.X = width;
                    if (sArray.TryGet(1, out string heightStr) && int.TryParse(heightStr, out int height))
                        room.Size.Y = height;
                }
                if (swArray.TryGet(1, out string waterLevelStr) && int.TryParse(waterLevelStr, out int waterLevel))
                {
                    room.WaterLevel = waterLevel * 20 + 10;
                }
                if (swArray.TryGet(2, out string waterInFrontStr))
                {
                    room.WaterInFrontOfTerrain = waterInFrontStr == "1";
                }
            }

            if (lines.TryGet(2, out string lightDirX))
            {
                string[] ldxArray = lightDirX.Split('|');
                if (ldxArray.TryGet(0, out string lineDir))
                {
                    string[] ldArray = lineDir.Split('*');
                    if (ldArray.TryGet(0, out string xStr) && float.TryParse(xStr, out float x))
                        room.LightAngle.X = x;
                    if (ldArray.TryGet(1, out string yStr) && float.TryParse(yStr, out float y))
                        room.LightAngle.Y = y;
                }
            }

            if (lines.TryGet(3, out string cameraPositions))
            {
                string[] cpArray = cameraPositions.Split('|');
                List<Vector2> positions = new();

                for (int i = 0; i < cpArray.Length; i++)
                {
                    Vector2 pos = new();
                    string[] posArray = cpArray[i].Split(',');
                    if (posArray.TryGet(0, out string xStr) && float.TryParse(xStr, out float x))
                        pos.X = x;
                    if (posArray.TryGet(1, out string yStr) && float.TryParse(yStr, out float y))
                        pos.Y = y;
                    positions.Add(pos);
                }

                for (int i = positions.Count - 1; i >= 1; i--)
                {
                    float length = (positions[i] - positions[0]).Length();
                    if (length < 30000)
                        break;

                    positions.RemoveAt(i);
                }

                room.CameraPositions = positions.ToArray();
            }

            if (lines.TryGet(11, out string tiles))
            {
                room.Tiles = new Tile[room.Size.X, room.Size.Y];

                string[] tilesArray = tiles.Split('|');

                int x = 0, y = 0;
                for (int i = 0; i < tilesArray.Length; i++)
                {
                    if (tilesArray[i].Length == 0 || x < 0 || y < 0 || x >= room.Tiles.GetLength(0) || y >= room.Tiles.GetLength(1))
                        continue;

                    string[] tileArray = tilesArray[i].Split(',');
                    Tile tile = new();

                    for (int j = 0; j < tileArray.Length; j++)
                    {
                        if (j == 0)
                        {
                            if (!int.TryParse(tileArray[j], out int terrain))
                                continue;

                            tile.Terrain = (Tile.TerrainType)terrain;
                            continue;
                        }

                        switch (tileArray[j])
                        {
                            case "3" when tile.Shortcut == Tile.ShortcutType.None:
                                tile.Shortcut = Tile.ShortcutType.Normal;
                                break;

                            case "4": tile.Shortcut = Tile.ShortcutType.RoomExit; break;
                            case "5": tile.Shortcut = Tile.ShortcutType.CreatureHole; break;
                            case "9": tile.Shortcut = Tile.ShortcutType.NPCTransportation; break;
                            case "12": tile.Shortcut = Tile.ShortcutType.RegionTransportation; break;
                        }
                    }

                    room.Tiles[x, y] = tile;

                    y++;
                    if (y >= room.Size.Y)
                    {
                        x++;
                        y = 0;
                    }
                }

                List<Point> exits = new();
                List<Point> shortcuts = new();

                for (int j = 0; j < room.Size.Y; j++)
                    for (int i = 0; i < room.Size.X; i++)
                    {
                        Tile tile = room.Tiles[i, j];

                        if (tile.Shortcut == Tile.ShortcutType.Normal)
                            shortcuts.Add(new Point(i, j));

                        if (tile.Shortcut == Tile.ShortcutType.RoomExit)
                            exits.Add(new Point(i, j));
                    }

                Point[] exitEntrances = new Point[exits.Count];

                for (int i = 0; i < exits.Count; i++)
                {
                    Point exit = exits[i];
                    Point lastPos = exit;
                    Point pos = exit;
                    int? dir = null;
                    bool foundDir = false;

                    while (true)
                    {
                        if (dir is not null)
                        {
                            Point dirVal = Directions[dir.Value];

                            Point testTilePos = pos.Add(dirVal);

                            if (testTilePos.X < 0 || testTilePos.Y < 0 || testTilePos.X >= room.Size.X || testTilePos.Y >= room.Size.Y)
                            {
                                pos = exit;
                                break;
                            }

                            Tile tile = room.Tiles[testTilePos.X, testTilePos.Y];
                            if (tile.Shortcut == Tile.ShortcutType.Normal)
                            {
                                lastPos = pos;
                                pos = testTilePos;
                                continue;
                            }
                        }
                        foundDir = false;
                        for (int j = 0; j < 4; j++)
                        {
                            Point dirVal = Directions[j];
                            Point testTilePos = pos.Add(dirVal);

                            if (testTilePos == lastPos || testTilePos.X < 0 || testTilePos.Y < 0 || testTilePos.X >= room.Size.X || testTilePos.Y >= room.Size.Y)
                                continue;

                            Tile tile = room.Tiles[testTilePos.X, testTilePos.Y];
                            if (tile.Shortcut == Tile.ShortcutType.Normal)
                            {
                                dir = j;
                                foundDir = true;
                                break;
                            }
                        }
                        if (!foundDir)
                            break;
                    }

                    exitEntrances[i] = pos;
                }

                room.RoomShortcuts = shortcuts.ToArray();
                room.RoomExits = exits.ToArray();
                room.RoomExitEntrances = exitEntrances;
            }

            Vector2 min = new(float.MaxValue, float.MaxValue);

            for (int i = 0; i < room.CameraPositions.Length; i++)
            {
                min.X = Math.Min(min.X, room.CameraPositions[i].X);
                min.Y = Math.Min(min.Y, room.CameraPositions[i].Y);
            }
            room.ScreenStart = min;

            string roomDir = Path.GetDirectoryName(roomPath)!;
            string roomName = Path.GetFileNameWithoutExtension(roomPath);

            room.CameraScreens = new TextureAsset[room.CameraPositions.Length];
            if (room.CameraScreens.Length == 1)
            {
                string screenFile = Path.Combine(roomDir, $"{roomName}.png");
                if (File.Exists(screenFile))
                    room.CameraScreens[0] = TextureLoader.Load(screenFile);

            }
            for (int i = 0; i < room.CameraPositions.Length; i++)
            {
                string screenFile = Path.Combine(roomDir, $"{roomName}_{i + 1}.png");
                if (File.Exists(screenFile))
                    room.CameraScreens[i] = TextureLoader.Load(screenFile);
            }

            for (int i = 0; i < room.CameraPositions.Length; i++)
                room.CameraScreens[i]?.OnLoaded(room.UpdateScreenSize);

            room.UpdateScreenSize();

            if (room.WaterLevel > 0)
                room.Water = new WaterData(room.ScreenSize.X, room.CameraScreens.Max(s => s?.Texture.Width ?? 0));

            return room;
        }

        public bool IntersectsWith(Vector2 tl, Vector2 br)
        {
            return WorldPos.X + ScreenStart.X < br.X
                && tl.X < WorldPos.X + ScreenSize.X + ScreenStart.X
                && WorldPos.Y + ScreenStart.Y < br.Y
                && tl.Y < WorldPos.Y + ScreenSize.Y + ScreenStart.Y;
        }

        public void Update()
        {
            if (Water is null)
                return;

            float r = 1 * 0.0045f / (0.0005f * WaterData.TriangleSize);
            float waveAmp = MathHelper.Lerp(1f, 40f, Settings?.WaveAmplitude ?? 0);
            float waveLength = MathHelper.Lerp(50f, 750f, Settings?.WaveLength ?? 0);
            float waveSpeed = MathHelper.Lerp(-0.033333335f, 0.033333335f, Settings?.WaveSpeed ?? 0);
            float rollBackLength = MathHelper.Lerp(2f, 0f, Settings?.SecondWaveLength ?? 0);
            float rollBackAmp = Settings?.SecondWaveAmplitude ?? 0;

            Water.SinCounter -= waveSpeed;

            float num7 = 0f;
            for (int num8 = 0; num8 < Water.Surface.GetLength(0); num8++)
            {
                WaterData.SurfacePoint point = Water.Surface[num8, 0];
                if (num8 == 0)
                {
                    point.nextHeight = (2f * point.height + (r - 1f) * point.lastHeight + 2f * (float)Math.Pow(r, 2f) * (Water.Surface[num8 + 1, 0].height - point.height)) / (1f + r);
                }
                else if (num8 == Water.Surface.GetLength(0) - 1)
                {
                    point.nextHeight = (2f * point.height + (r - 1f) * point.lastHeight + 2f * (float)Math.Pow(r, 2f) * (Water.Surface[num8 - 1, 0].height - point.height)) / (1f + r);
                }
                else
                {
                    point.nextHeight = (float)Math.Pow(r, 2f) * (Water.Surface[num8 - 1, 0].height + Water.Surface[num8 + 1, 0].height) + 2f * (1f - (float)Math.Pow(r, 2f)) * point.height - point.lastHeight;
                    //if (this.room.GetTile(point.defaultPos + new Vector2(0f, point.height)).Terrain == Room.Tile.TerrainType.Solid)
                    //{
                    //    point.nextHeight *= ((!this.room.waterInFrontOfTerrain) ? 0.75f : 0.95f);
                    //}
                }
                point.nextHeight += MathHelper.Lerp(-waveAmp, waveAmp, (float)Random.Shared.NextDouble()) * 0.005f;
                point.nextHeight *= 0.99f;
                //if (this.room.roomSettings.DangerType != RoomRain.DangerType.None)
                //{
                //    point.nextHeight += (float)Math.Lerp(-1f, 1f, UnityEngine.Random.value) * this.room.world.rainCycle.ScreenShake * this.room.roomSettings.RumbleIntensity;
                //}
                num7 += Water.Surface[num8, 0].height;
                for (int num9 = 0; num9 < 1; num9++)
                {
                    float num10 = -(float)num8 * WaterData.TriangleSize / waveLength;
                    Water.Surface[num8, num9].lastPos = Water.Surface[num8, num9].pos;
                    float num11 = Water.Surface[num8, num9].height * 3f;
                    float num12 = 3f;
                    for (int num13 = -1; num13 < 2; num13 += 2)
                    {
                        if (num8 + num13 * 2 > 0 && num8 + num13 * 2 < Water.Surface.GetLength(0) && (float)Math.Abs(Water.Surface[num8, num9].height - Water.Surface[num8 + num13, num9].height) > (float)Math.Abs(Water.Surface[num8, num9].height - Water.Surface[num8 + num13 * 2, num9].height))
                        {
                            num11 += Water.Surface[num8 + num13, num9].height;
                            num12 += 1f;
                        }
                    }
                    Water.Surface[num8, num9].pos = num11 / num12;
                    Water.Surface[num8, num9].pos += (float)Math.Cos((num10 + Water.SinCounter * ((num9 != 1) ? -1f : 1f)) * MathF.Tau) * waveAmp;
                    //if (this.room.roomSettings.DangerType != RoomRain.DangerType.None)
                    //{
                    //    Water.Surface[num8, num9].pos += Custom.DegToVec(UnityEngine.Random.value * 360f) * this.room.world.rainCycle.MicroScreenShake * 4f * this.room.roomSettings.RumbleIntensity;
                    //}
                    Water.Surface[num8, num9].pos += (float)Math.Cos((num10 + Water.SinCounter * ((num9 != 1) ? 1f : -1f)) * MathF.Tau * rollBackLength) * waveAmp * rollBackAmp;
                }
            }
            num7 /= Water.Surface.GetLength(0) * 1.5f;
            for (int num14 = 0; num14 < Water.Surface.GetLength(0); num14++)
            {
                Water.Surface[num14, 0].lastHeight = Water.Surface[num14, 0].height;
                float num15 = Water.Surface[num14, 0].nextHeight - num7;
                if (num14 > 0 && num14 < Water.Surface.GetLength(0) - 1)
                {
                    num15 = MathHelper.Lerp(num15, MathHelper.Lerp(Water.Surface[num14 - 1, 0].nextHeight, Water.Surface[num14 + 1, 0].nextHeight, 0.5f), 0.01f);
                }
                Water.Surface[num14, 0].height = MathHelper.Clamp(num15, -40f, 40f);
            }
        }

        public void Draw()
        {
            Rendered = false;

            if (!IntersectsWith(PanNZoom.ScreenToWorld(Vector2.Zero), PanNZoom.ScreenToWorld(Main.Instance.GraphicsDevice.Viewport.Bounds.Size.ToVector2())))
                return;

            PrepareDraw();

            for (int i = 0; i < CameraScreens.Length; i++)
                DrawScreen(false, i);

            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            Vector2 nameSize = Main.Consolas10.MeasureString(Name);
            Main.SpriteBatch.DrawString(Main.Consolas10, Name, PanNZoom.WorldToScreen(WorldPos + new Vector2(ScreenSize.X / 2, 0)) - new Vector2(nameSize.X / 2, 0), Color.Yellow);
            Main.SpriteBatch.End();

            Rendered = true;
        }

        public void PrepareDraw()
        {
            Vector4 lightDirAndPixelSize = new(LightAngle.X, LightAngle.Y, 0.0007142857f, 0.00125f);
            Main.RoomLevel?.Parameters["_lightDirAndPixelSize"].SetValue(lightDirAndPixelSize);
        }

        public void DrawScreen(bool forCapture, int index)
        {
            TextureAsset? texture = CameraScreens[index];
            Vector2 pos = forCapture ? Vector2.Zero : PanNZoom.WorldToScreen(CameraPositions[index] + WorldPos);

            if (texture is null)
                return;

            Main.SpriteBatch.Begin(SpriteSortMode.Immediate, samplerState: SamplerState.PointClamp);

            if (Main.RoomLevel is not null)
            {
                Main.RoomLevel.Parameters["PaletteTex"].SetValue(Palettes.GetPalette(Settings?.Palette ?? 0));
                Main.RoomLevel.Parameters["FadePaletteTex"].SetValue(Palettes.GetPalette(Settings?.FadePalette ?? 0));
                Main.RoomLevel.Parameters["FadePalette"].SetValue(1 - Settings?.FadePaletteValues?[index] ?? 1);

                if (Settings?.EffectColorA is not null)
                    Main.RoomLevel.Parameters["EffectColorA"].SetValue(Settings.EffectColorA.Value);

                if (Settings?.EffectColorB is not null)
                    Main.RoomLevel.Parameters["EffectColorB"].SetValue(Settings.EffectColorB.Value);

                Main.RoomLevel.Parameters["_light"].SetValue(MathHelper.Lerp(1, -1, Settings?.Clouds ?? 0));
                Main.RoomLevel.Parameters["_Grime"].SetValue(Settings?.Grime ?? 0.5f);
                Main.RoomLevel.CurrentTechnique.Passes[0].Apply();
            }
            Main.SpriteBatch.Draw(texture.Texture, pos, null, Color.White, 0f, Vector2.Zero, forCapture ? 1 : PanNZoom.Zoom, SpriteEffects.None, 0f);

            Main.SpriteBatch.End();

            DrawWater(forCapture, index);
        }

        void DrawWater(bool forCapture, int screenIndex)
        {
            //if (Name == "SL_D06" && screenIndex == 3)
            //    Debugger.Break();

            if (Water is null)
                return;

            if (CameraScreens[screenIndex] is null)
                return;

            Vector2 screenpos = CameraPositions[screenIndex];

            Matrix roomMatrix;
            if (!forCapture)
            {
                roomMatrix = Matrix.CreateTranslation(new Vector3(WorldPos, 0));
                roomMatrix = Matrix.Multiply(roomMatrix, PanNZoom.WorldToScreenTransform);
                roomMatrix = Matrix.Multiply(roomMatrix, Main.Projection);
            }
            else
            {
                roomMatrix = Matrix.CreateTranslation(new(-screenpos, 0));
                roomMatrix = Matrix.Multiply(roomMatrix, Matrix.CreateOrthographicOffCenter(0, CaptureManager.RenderTarget.Width, CaptureManager.RenderTarget.Height, 0, 0, 1));
            }

            Vector2 screensize = new(CameraScreens[screenIndex]!.Texture.Width, CameraScreens[screenIndex]!.Texture.Height);

            int start = (int)((screenpos.X - ScreenStart.X) / WaterData.TriangleSize);
            int end = (int)((screenpos.X + screensize.X - ScreenStart.X) / WaterData.TriangleSize) + 2;

            if (start > 0)
                start--;

            if (start == end)
                return;

            int verts = 0;

            for (int j = start; j < end; j++)
            {
                if (Water.Surface.GetLength(0) <= j)
                    continue;

                int vi = (j - start) * 2;

                if (vi < 0 || vi > Water.Vertices.Length - 1)
                    continue;

                WaterData.SurfacePoint point = Water.Surface[j, 0];

                Vector2 vertPos = new();
                vertPos.Y = point.pos + Size.Y * 20 - WaterLevel;
                vertPos.X = j * WaterData.TriangleSize + ScreenStart.X;

                Vector2 texPos = (vertPos - screenpos) / screensize;

                // position in room
                // texture uv
                // water depth (0 - bottom)

                Water.Vertices[vi + 1].Position = vertPos;
                Water.Vertices[vi + 1].TextureCoord = texPos;
                Water.Vertices[vi + 1].Depth = 1;

                Water.Vertices[vi].Position = new Vector2(vertPos.X, screensize.Y + screenpos.Y);
                Water.Vertices[vi].TextureCoord = new Vector2(texPos.X, 1);
                Water.Vertices[vi].Depth = 1 - (screenpos.Y + screensize.Y) / ScreenSize.Y;

                verts += 2;
            }

            if (verts < 3)
                return;

            Main.DeepWater.Parameters["LevelTex"].SetValue(CameraScreens[screenIndex]!.Texture);
            Main.DeepWater.Parameters["Projection"].SetValue(roomMatrix);
            Main.DeepWater.Parameters["PaletteTex"].SetValue(Palettes.GetPalette(Settings?.Palette ?? 0));
            Main.DeepWater.Parameters["FadePaletteTex"].SetValue(Palettes.GetPalette(Settings?.FadePalette ?? 0));
            Main.DeepWater.Parameters["FadePalette"].SetValue(1 - Settings?.FadePaletteValues?[screenIndex] ?? 1);

            Vector2 spriteSize = screensize * PanNZoom.Zoom;

            // For some reason, on-screen sprite position is flipped in shader here if not capturing
            if (!forCapture)
                spriteSize *= new Vector2(1, -1);

            Main.DeepWater.Parameters["_screenOff"].SetValue(CameraPositions[screenIndex] / (Main.Noise?.Bounds.Size.ToVector2() ?? Vector2.One));
            Main.DeepWater.Parameters["_screenSize"].SetValue(forCapture ? screensize : spriteSize);
            Main.DeepWater.Parameters["_waterDepth"].SetValue(WaterInFrontOfTerrain ? 0 : 1f / 30);

            Main.DeepWater.CurrentTechnique.Passes[0].Apply();

            Main.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Water.Vertices, 0, verts - 2);
        }

        public void UpdateScreenSize()
        {
            Vector2 max = new(0, 0);
            for (int i = 0; i < CameraScreens.Length; i++)
            {
                max.X = Math.Max(max.X, CameraPositions[i].X + (CameraScreens[i]?.Texture.Width ?? 0));
                max.Y = Math.Max(max.Y, CameraPositions[i].Y + (CameraScreens[i]?.Texture.Height ?? 0));
            }

            ScreenSize = max - ScreenStart;
            Water?.UpdateRoomSizes(ScreenSize.X, CameraScreens.Max(s => s?.Texture.Width ?? 0));
        }

        public Vector2 GetExitDirection(int index)
        {
            Point entrance = RoomExitEntrances[index];

            Vector2 direction = Vector2.Zero;

            for (int i = 0; i < 4; i++)
            {
                Point dir = Directions[i];
                Point tilePos = entrance.Add(dir);

                if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= Size.X || tilePos.Y >= Size.Y)
                    continue;

                Tile tile = Tiles[tilePos.X, tilePos.Y];
                if (tile.Terrain != Tile.TerrainType.Solid)
                    direction -= dir.ToVector2();
            }

            //if (direction == Vector2.Zero)
            //    Debugger.Break();

            return direction;
        }

        public override string ToString()
        {
            return Name;
        }

        public struct Tile
        {
            public TerrainType Terrain;
            public ShortcutType Shortcut;

            public enum TerrainType
            {
                Air,
                Solid,
                Slope,
                Floor,
                ShortcutEntrance
            }

            public enum ShortcutType
            {
                None,
                Normal,
                RoomExit,
                CreatureHole,
                NPCTransportation,
                RegionTransportation,
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WaterVertex : IVertexType
        {
            public Vector2 Position;
            public Vector2 TextureCoord;
            public float Depth;

            public VertexDeclaration VertexDeclaration => Declaration;
            static VertexDeclaration Declaration = new(new VertexElement[] 
            {
                new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new(16, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
            });
        }

        public class WaterData
        {
            public const float TriangleSize = 20;

            public SurfacePoint[,] Surface = null!;
            public WaterVertex[] Vertices = null!;
            public float SinCounter = 0;

            public WaterData(float roomWidth, float maxScreenWidth)
            {
                UpdateRoomSizes(roomWidth, maxScreenWidth);
            }

            public void UpdateRoomSizes(float roomWidth, float maxScreenWidth)
            {
                Surface = new SurfacePoint[(int)(roomWidth / TriangleSize + 2), 1];
                Vertices = new WaterVertex[(int)(maxScreenWidth / TriangleSize + 4) * 2];

                for (int i = 0; i < Surface.GetLength(0); i++)
                    Surface[i, 0] = new SurfacePoint();
            }

            public class SurfacePoint
            {
                public float lastPos;
                public float pos;

                public float height;
                public float lastHeight;
                public float nextHeight;
            }
        }
    }
}
