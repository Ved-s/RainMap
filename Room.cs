using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RainMap.PlacedObjects;
using RainMap.Renderers;
using RainMap.Structures;
using RWAPI;
using SixLabors.Fonts;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace RainMap
{
    public class Room
    {
        static Point[] Directions = new Point[] { new Point(0, -1), new Point(1, 0), new Point(0, 1), new Point(-1, 0) };
        static RasterizerState Scissors = new() { ScissorTestEnable = true };

        public string Name = null!;
        public Point Size;

        public Vector2 WorldPos;

        public float WaterHeight;
        public int WaterLevel = -1;
        public bool WaterInFrontOfTerrain;
        public Vector2 LightAngle;
        public Vector2[] CameraPositions = null!;
        public TextureAsset?[] CameraScreens = null!;

        public Tile[,] Tiles = null!;

        public RoomConnection[]? Connections;

        public Point[] RoomExits = null!;
        public RoomShortcut[] RoomShortcuts = null!;

        public Vector2 ScreenStart;
        public Vector2 ScreenSize;

        public RoomSettings? Settings;
        public WaterData? Water;
        public bool Rendered;

        public Texture2D? TileMap;

        Texture2D? FadePosValCache = null;
        Vector2[] FixedCameraPositions = null!;
        bool DoneFullScreenUpdate = false;

        static LightVertex[] LightVertices = new LightVertex[4]
        {
            new() { LightTextureCoord = new(0, 0) },
            new() { LightTextureCoord = new(1, 0) },
            new() { LightTextureCoord = new(0, 1) },
            new() { LightTextureCoord = new(1, 1) },
        };

        public string FilePath = null!;

        public static Room CurrentRoom = null!;

        public static Room Load(MultiDirectory dir, string name)
        {
            Room room = new();

            string roomPath = dir.FindFile($"{name}.txt")!;

            room.FilePath = roomPath;

            string[] lines = File.ReadAllLines(roomPath);

            if (lines.TryGet(0, out string displayname))
                room.Name = displayname;

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
                    room.WaterLevel = waterLevel;
                    room.WaterHeight = waterLevel * 20;
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
                room.FixCameraPositions();
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
                            case "1": tile.Attributes |= Tile.TileAttributes.VerticalBeam; break;
                            case "2": tile.Attributes |= Tile.TileAttributes.HorizontalBeam; break;

                            case "3" when tile.Shortcut == Tile.ShortcutType.None:
                                tile.Shortcut = Tile.ShortcutType.Normal;
                                break;

                            case "4": tile.Shortcut = Tile.ShortcutType.RoomExit; break;
                            case "5": tile.Shortcut = Tile.ShortcutType.CreatureHole; break;
                            case "6": tile.Attributes |= Tile.TileAttributes.WallBehind; break;
                            case "7": tile.Attributes |= Tile.TileAttributes.Hive; break;
                            case "8": tile.Attributes |= Tile.TileAttributes.Waterfall; break;
                            case "9": tile.Shortcut = Tile.ShortcutType.NPCTransportation; break;
                            case "10": tile.Attributes |= Tile.TileAttributes.GarbageHole; break;
                            case "11": tile.Attributes |= Tile.TileAttributes.WormGrass; break;
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

                        if (tile.Terrain == Tile.TerrainType.ShortcutEntrance)
                            shortcuts.Add(new Point(i, j));

                        if (tile.Shortcut == Tile.ShortcutType.RoomExit)
                            exits.Add(new Point(i, j));
                    }

                Point[] exitEntrances = new Point[exits.Count];

                for (int i = 0; i < exits.Count; i++)
                {
                    exitEntrances[i] = room.TraceShotrcut(exits[i]);
                }

                List<RoomShortcut> tracedShortcuts = new();

                foreach (Point shortcutIn in shortcuts)
                {
                    Point target = room.TraceShotrcut(shortcutIn);
                    Tile targetTile = room.GetTile(target);
                    tracedShortcuts.Add(new(shortcutIn, target, targetTile.Shortcut));
                }

                room.RoomShortcuts = tracedShortcuts.ToArray();
                room.RoomExits = exitEntrances;
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
                string? screenFile = dir.FindFile($"{roomName}.png");
                if (screenFile is not null)
                    room.CameraScreens[0] = TextureLoader.Load(screenFile);

            }
            for (int i = 0; i < room.CameraPositions.Length; i++)
            {
                string? screenFile = dir.FindFile($"{roomName}_{i + 1}.png");
                if (screenFile is not null)
                    room.CameraScreens[i] = TextureLoader.Load(screenFile);
            }

            for (int i = 0; i < room.CameraPositions.Length; i++)
                room.CameraScreens[i]?.OnLoaded(room.UpdateScreenSize);

            room.UpdateScreenSize();

            if (room.WaterHeight > 0)
                room.Water = new WaterData(room.ScreenSize.X, room.CameraScreens.Max(s => s?.Texture.Width ?? 0));

            return room;
        }

        public Point TraceShotrcut(Point pos)
        {
            Point lastPos = pos;
            int? dir = null;
            bool foundDir = false;

            while (true)
            {
                if (dir is not null)
                {
                    Point dirVal = Directions[dir.Value];

                    Point testTilePos = pos + dirVal;

                    if (testTilePos.X >= 0 && testTilePos.Y >= 0 && testTilePos.X < Size.X && testTilePos.Y < Size.Y)
                    {
                        Tile tile = Tiles[testTilePos.X, testTilePos.Y];
                        if (tile.Shortcut == Tile.ShortcutType.Normal)
                        {
                            lastPos = pos;
                            pos = testTilePos;
                            continue;
                        }
                    }
                }
                foundDir = false;
                for (int j = 0; j < 4; j++)
                {
                    Point dirVal = Directions[j];
                    Point testTilePos = pos + dirVal;

                    if (testTilePos == lastPos || testTilePos.X < 0 || testTilePos.Y < 0 || testTilePos.X >= Size.X || testTilePos.Y >= Size.Y)
                        continue;

                    Tile tile = Tiles[testTilePos.X, testTilePos.Y];
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

            return pos;
        }

        public bool IntersectsWith(Vector2 tl, Vector2 br)
        {
            return WorldPos.X + ScreenStart.X < br.X
                && tl.X < WorldPos.X + ScreenSize.X + ScreenStart.X
                && WorldPos.Y + ScreenStart.Y < br.Y
                && tl.Y < WorldPos.Y + ScreenSize.Y + ScreenStart.Y;
        }
        public bool ScreenIntersectsWith(int screen, Vector2 tl, Vector2 br)
        {
            if (CameraScreens[screen] is null)
                return false;

            Vector2 screenTL = CameraPositions[screen] + WorldPos;
            Vector2 screenBR = screenTL + CameraScreens[screen]!.Texture.Size();

            return screenTL.X < br.X
                && tl.X < screenBR.X
                && screenTL.Y < br.Y
                && tl.Y < screenBR.Y;
        }

        public void Update()
        {
            CurrentRoom = this;
            if (!DoneFullScreenUpdate && CameraScreens.All(s => s is null || s.Loaded))
            {
                UpdateScreenSize();
                DoneFullScreenUpdate = true;
            }

            if (Settings is not null)
                foreach (PlacedObject obj in Settings.PlacedObjects)
                    obj.Update();

            UpdateWater();
        }
        private void UpdateWater()
        {
            if (Water is null || Main.KeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.P))
                return;

            try
            {
                float r = 1 * 0.0045f / (0.0005f * WaterData.TriangleSize);
                float waveAmp = MathHelper.Lerp(1f, 40f, Settings?.WaveAmplitude ?? 0);
                float waveLength = MathHelper.Lerp(50f, 750f, Settings?.WaveLength ?? 0);
                float waveSpeed = MathHelper.Lerp(-0.033333335f, 0.033333335f, Settings?.WaveSpeed ?? 0);
                float rollBackLength = MathHelper.Lerp(2f, 0f, Settings?.SecondWaveLength ?? 0);
                float rollBackAmp = Settings?.SecondWaveAmplitude ?? 0;

                Water.SinCounter -= waveSpeed * .5f;

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
                    for (int num9 = 0; num9 < 2; num9++)
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
            // It throws nullref sometimes and I idk why
            catch { }
        }

        public void Draw(Renderer renderer)
        {
            CurrentRoom = this;
            Rendered = false;

            Vector2 rendererTL = renderer.InverseTransformVector(Vector2.Zero);
            Vector2 rendererBR = renderer.InverseTransformVector(renderer.Size);

            if (!IntersectsWith(rendererTL, rendererBR))
                return;

            if (Main.MappingMode)
            {
                if (TileMap is null)
                    ResetTileMap();
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                renderer.DrawTexture(TileMap!, WorldPos, null, Size.ToVector2() * 20);
                Main.SpriteBatch.End();
            }
            else
            {
                PrepareDraw();

                for (int i = 0; i < CameraScreens.Length; i++)
                    if (ScreenIntersectsWith(i, rendererTL, rendererBR))
                        DrawScreen(renderer, i);
            }
            Rendered = true;
        }
        public void PrepareDraw()
        {
            Vector4 lightDirAndPixelSize = new(LightAngle.X, LightAngle.Y, 0.0007142857f, 0.00125f);
            Main.RoomColor?.Parameters["_lightDirAndPixelSize"]?.SetValue(lightDirAndPixelSize);
        }
        public void DrawScreen(Renderer renderer, int index)
        {
            CurrentRoom = this;

            if (Main.RenderRoomLevel)
                DrawLevel(renderer, index);

            if (Main.RenderRoomTiles)
                DrawTileOverlay(renderer, index, Main.RenderTilesWithPalette);

            if (Main.DrawObjectNames || Main.DrawObjectIcons)
            {
                Rectangle oldScissors = Main.Instance.GraphicsDevice.ScissorRectangle;
                Vector2 tl = renderer.TransformVector(WorldPos + CameraPositions[index]);
                Vector2 br = renderer.TransformVector(WorldPos + CameraPositions[index] + CameraScreens[index]?.Texture.Size() ?? Vector2.One);
                Main.Instance.GraphicsDevice.ScissorRectangle = new((int)tl.X, (int)tl.Y, (int)(br.X - tl.X), (int)(br.Y - tl.Y));
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, rasterizerState: Scissors);

                if (Main.DrawObjectNames)
                    DrawObjectNames(renderer);

                if (Main.DrawObjectIcons)
                    DrawObjectIcons(renderer);

                Main.SpriteBatch.End();
                Main.Instance.GraphicsDevice.ScissorRectangle = oldScissors;
            }

            Main.RoomTimeLogger.FinishWatch();
        }

        private void DrawLevel(Renderer renderer, int index)
        {
            TextureAsset? texture = CameraScreens[index];

            if (texture is null)
                return;

            Main.RoomTimeLogger.StartWatch(RoomDrawTime.RoomLevel, true);

            float grabScale1 = Math.Min(1, renderer.Scale);
            float grabScale2 = Math.Max(1, renderer.Scale);

            Main.SpriteBatch.Begin(SpriteSortMode.Immediate, samplerState: SamplerState.PointClamp);

            if (Main.RoomColor is not null)
            {
                Main.RoomColor.Parameters["Projection"]?.SetValue(renderer.Projection);
                ApplyPaletteToShader(Main.RoomColor, index);
                ApplyLevelToShader(Main.RoomColor, index, renderer);
                //GrabBuffer.ApplyToShader(Main.RoomColor);

                if (Settings?.EffectColorA is not null)
                    Main.RoomColor.Parameters["EffectColorA"]?.SetValue(Settings.EffectColorA.Value);

                if (Settings?.EffectColorB is not null)
                    Main.RoomColor.Parameters["EffectColorB"]?.SetValue(Settings.EffectColorB.Value);

                Main.RoomColor.Parameters["_light"]?.SetValue(MathHelper.Lerp(1, -1, Settings?.Clouds ?? 0));
                Main.RoomColor.Parameters["_Grime"]?.SetValue(Settings?.Grime ?? 0.5f);
                Main.RoomColor.CurrentTechnique.Passes[0].Apply();
            }
            renderer.DrawTexture(texture.Texture, WorldPos + CameraPositions[index], null, CameraScreens[index]?.Texture.Size() ?? Vector2.Zero);
            Main.SpriteBatch.End();

            Main.RoomTimeLogger.StartWatch(RoomDrawTime.ObjectLights, true);
            DrawObjectLights(renderer, index);

            Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            renderer.DrawTexture(GrabBuffer.Target!, CameraPositions[index] + WorldPos, GrabBuffer.CurrentSource, texture.Texture.Size(), Color.White, Vector2.One * grabScale2);
            Main.SpriteBatch.End();

            Main.RoomTimeLogger.StartWatch(RoomDrawTime.Water, true);
            DrawWater(renderer, index);

            if (Connections is not null)
            {
                Main.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
                foreach (RoomConnection c in Connections)
                {
                    Point p = RoomExits[c.Exit];

                    Vector2 tileCenter = renderer.TransformVector(WorldPos + new Vector2(p.X, p.Y) * 20 + new Vector2(7));

                    bool gate = c.Target.Name.StartsWith("GATE");

                    Vector2 dir = gate ? Vector2.Zero : GetShortcutDirection(p.X, p.Y);
                    float rotation = dir == Vector2.Zero ? 0f : MathF.Atan2(dir.Y, dir.X);

                    if (!gate)
                        rotation += MathF.PI / 2;

                    Rectangle source = gate ? new(28, 0, 14, 14) : new(0, 0, 14, 14);

                    Main.SpriteBatch.Draw(GameAssets.Shortcuts.Texture, tileCenter, source, Color.White, rotation, new(7), renderer.Scale, SpriteEffects.None, 0);
                }
                Main.SpriteBatch.End();
            }
        }
        private void DrawObjectLights(Renderer renderer, int screenIndex)
        {
            if (Settings is null)
                return;

            foreach (PlacedObject obj in Settings.PlacedObjects)
                if (obj is ILightObject lightObj && lightObj.Lights is not null)
                    foreach (ILightObject.LightData light in lightObj.Lights)
                        if (light.Enabled)
                        {
                            Vector2 pos = light.RoomPos;
                            float rad = light.Radius / 8;
                            pos.Y = Size.Y * 20 - pos.Y;
                            DrawLight(renderer, light.Texture?.Texture, pos, rad, light.Color, screenIndex);

                            //Main.SpriteBatch.Begin();
                            ////renderer.DrawRect(pos + WorldPos - new Vector2(rad), new(rad * 2), light.Color * 0.1f);
                            //renderer.DrawRect(pos + WorldPos - new Vector2(rad), new(rad * 2), null, light.Color);
                            //renderer.DrawRect(pos + WorldPos - new Vector2(1), new(3), null, Color.White);
                            //Main.SpriteBatch.End();
                        }
        }

        private void DrawTileOverlay(Renderer renderer, int screenIndex, bool applyPalette = false)
        {
            Main.RoomTimeLogger.StartWatch(RoomDrawTime.Tiles, true);

            Main.SpriteBatch.Begin();
            float bgAlpha = Main.RenderRoomLevel ? .4f : 1f;
            Color bgColor = applyPalette ? SamplePalette(0, 7, screenIndex) : Color.Gray;
            renderer.DrawTexture(Main.Pixel, WorldPos + CameraPositions[screenIndex], null, CameraScreens[screenIndex]?.Texture.Size() ?? Vector2.Zero, bgColor * bgAlpha);
            Main.SpriteBatch.End();

            float solidAlpha = 1f;
            float waterAlpha = .1f;
            float water2Alpha = .2f;
            float wallAlpha = .4f;

            Color solidColor = applyPalette ? SamplePalette(0, 3, screenIndex) : Color.Black;
            Color waterColor = applyPalette ? SamplePalette(4, 7, screenIndex) : Color.Blue;
            Color water2Color = applyPalette ? SamplePalette(5, 7, screenIndex) : Color.Blue;
            Color wallColor = applyPalette ? SamplePalette(10, 3, screenIndex) : Color.Black;

            Matrix matrix = Matrix.Multiply(Matrix.CreateScale(20), Matrix.CreateTranslation(new(WorldPos, 0)));
            matrix = Matrix.Multiply(matrix, renderer.Transform);
            matrix = Matrix.Multiply(matrix, renderer.Projection);

            TriangleDrawing.Clear();
            TriangleDrawing.UseClamp = true;

            TriangleDrawing.ClampTL = CameraPositions[screenIndex] / 20;
            TriangleDrawing.ClampBR = (CameraPositions[screenIndex] + (CameraScreens[screenIndex]?.Texture.Size() ?? Vector2.Zero)) / 20;

            int xstart = (int)(CameraPositions[screenIndex].X / 20) - 1;
            int ystart = (int)(CameraPositions[screenIndex].Y / 20) - 1;
            int xend = (int)((CameraPositions[screenIndex].X + CameraScreens[screenIndex]?.Texture.Width ?? 0) / 20) + 1;
            int yend = (int)((CameraPositions[screenIndex].Y + CameraScreens[screenIndex]?.Texture.Height ?? 0) / 20) + 1;

            for (int y = ystart; y < yend; y++)
            {
                bool water = WaterLevel > 0 && !WaterInFrontOfTerrain && y >= Size.Y - WaterLevel;

                for (int x = xstart; x < xend; x++)
                {
                    Tile tile = GetTile(x, y);
                    bool oob = x < 0 || y < 0 || x >= Size.X || y >= Size.Y;

                    if (tile.Attributes.HasFlag(Tile.TileAttributes.WallBehind))
                        TriangleDrawing.AddQuad(new(x, y), new(x + 1, y), new(x, y + 1), new(x + 1, y + 1), wallColor * wallAlpha);

                    switch (tile.Terrain)
                    {
                        case Tile.TerrainType.Solid:
                            if (x > xstart && GetTile(x - 1, y).Terrain == Tile.TerrainType.Solid)
                                break;
                            int width = 1;
                            while (x + width < xend && GetTile(x + width, y).Terrain == Tile.TerrainType.Solid)
                                width++;

                            TriangleDrawing.AddQuad(new(x, y), new(x + width, y), new(x, y + 1), new(x + width, y + 1), solidColor * solidAlpha);
                            break;

                        case Tile.TerrainType.Floor:
                            TriangleDrawing.AddQuad(new(x, y), new(x + 1, y), new(x, y + .5f), new(x + 1, y + .5f), solidColor * solidAlpha);
                            break;

                        case Tile.TerrainType.ShortcutEntrance:
                            TriangleDrawing.AddQuad(new(x + .2f, y + .5f), new(x + .5f, y + .2f), new(x + .5f, y + .8f), new(x + .8f, y + .5f), Color.White * solidAlpha);
                            break;

                        case Tile.TerrainType.Slope:
                            if (water)
                                TriangleDrawing.AddQuad(new(x, y), new(x + 1, y), new(x, y + 1), new(x + 1, y + 1), water2Color * water2Alpha);

                            for (int i = 0; i < 4; i++)
                            {
                                Point dirR = Directions[i];
                                Point dirL = Directions[(i + 1) % 4];

                                Tile right = GetTile(new Point(x, y) + dirR);
                                Tile left = GetTile(new Point(x, y) + dirL);

                                if (right.Terrain != Tile.TerrainType.Solid || left.Terrain != Tile.TerrainType.Solid)
                                    continue;

                                Vector2 center = new(x + .5f, y + .5f);
                                Vector2 a = (dirR + dirL).ToVector2() / 2;
                                Vector2 b = new(-a.Y, a.X);
                                Vector2 c = -b;

                                TriangleDrawing.AddTriangle(center + a, center + b, center + c, solidColor * solidAlpha);
                            }
                            break;
                    }

                    if (tile.Terrain != Tile.TerrainType.ShortcutEntrance && !oob && renderer.Scale > 0.1f)
                        switch (tile.Shortcut)
                        {
                            case Tile.ShortcutType.Normal:
                                TriangleDrawing.AddQuad(new(x + .4f, y + .4f), new(x + .6f, y + .4f), new(x + .4f, y + .6f), new(x + .6f, y + .6f), Color.White * 0.8f);
                                break;

                            case Tile.ShortcutType.RoomExit:
                                TriangleDrawing.AddQuad(new(x + .2f, y + .5f), new(x + .5f, y + .2f), new(x + .5f, y + .8f), new(x + .8f, y + .5f), Color.Yellow * 0.8f);
                                break;
                            case Tile.ShortcutType.CreatureHole:
                                TriangleDrawing.AddQuad(new(x + .2f, y + .5f), new(x + .5f, y + .2f), new(x + .5f, y + .8f), new(x + .8f, y + .5f), Color.Magenta * 0.8f);
                                break;
                            case Tile.ShortcutType.NPCTransportation:
                                TriangleDrawing.AddQuad(new(x + .2f, y + .5f), new(x + .5f, y + .2f), new(x + .5f, y + .8f), new(x + .8f, y + .5f), Color.Lime * 0.8f);
                                break;
                            case Tile.ShortcutType.RegionTransportation:
                                TriangleDrawing.AddQuad(new(x + .2f, y + .5f), new(x + .5f, y + .2f), new(x + .5f, y + .8f), new(x + .8f, y + .5f), Color.Red * 0.8f);
                                break;
                        }

                    for (int i = 0; i < 7; i++)
                    {
                        Tile.TileAttributes feature = (Tile.TileAttributes)(1 << i);
                        if ((tile.Attributes & feature) == 0)
                            continue;

                        switch (feature)
                        {
                            case Tile.TileAttributes.VerticalBeam:
                                TriangleDrawing.AddQuad(new(x + .4f, y), new(x + .6f, y), new(x + .4f, y + 1), new(x + .6f, y + 1), solidColor * solidAlpha);
                                break;

                            case Tile.TileAttributes.HorizontalBeam:
                                TriangleDrawing.AddQuad(new(x, y + .4f), new(x + 1, y + .4f), new(x, y + .6f), new(x + 1, y + .6f), solidColor * solidAlpha);
                                break;

                            case Tile.TileAttributes.Hive:
                                TriangleDrawing.AddTriangle(
                                    new(x + .05f, y + 1), solidColor * solidAlpha,
                                    new(x + .275f, y + .3f), Color.White,
                                    new(x + .5f, y + 1), solidColor * solidAlpha);
                                TriangleDrawing.AddTriangle(
                                    new(x + .5f, y + 1), solidColor * solidAlpha,
                                    new(x + .725f, y + .6f), Color.Gray,
                                    new(x + 1f, y + 1), solidColor * solidAlpha);
                                break;
                            case Tile.TileAttributes.Waterfall:
                                break;
                            case Tile.TileAttributes.GarbageHole:
                                break;
                            case Tile.TileAttributes.WormGrass:
                                for (int j = 0; j < 4; j++)
                                {
                                    float height = (Math.Abs(x * 98436511 * y * 873569822 * j * 875983916) % 1024) / 1024f * 0.5f + 0.4f;

                                    float top = y + 1 - height;
                                    float bottom = y + 1;
                                    float left = x + (j * 0.25f);
                                    float right = left + 0.25f;

                                    Color topColor = new Color(.2f, .2f, .2f) * solidAlpha;
                                    Color bottomColor = solidColor * solidAlpha;

                                    TriangleDrawing.AddQuad(
                                        new(left, top), topColor,
                                        new(right, top), topColor,
                                        new(left, bottom), bottomColor,
                                        new(right, bottom), bottomColor);

                                    bottom = top + 0.05f;
                                    top -= 0.10f;
                                    left += 0.05f;
                                    right -= 0.05f;

                                    TriangleDrawing.AddQuad(
                                        new(left, top),
                                        new(right, top),
                                        new(left, bottom),
                                        new(right, bottom), Color.Blue);
                                }
                                break;
                        }
                    }

                    if (water && (tile.Terrain is not Tile.TerrainType.Solid and not Tile.TerrainType.Slope && (x == xend - 1 || GetTile(x + 1, y).Terrain is Tile.TerrainType.Solid or Tile.TerrainType.Slope)))
                    {
                        int width = 1;
                        while (x - width >= xstart && GetTile(x - width, y).Terrain is not Tile.TerrainType.Solid and not Tile.TerrainType.Slope)
                            width++;

                        TriangleDrawing.AddQuad(new(x - width + 1, y), new(x + 1, y), new(x - width + 1, y + 1), new(x + 1, y + 1), water2Color * water2Alpha);
                    }
                }
            }

            if (WaterInFrontOfTerrain)
            {
                int y = Size.Y - WaterLevel;
                TriangleDrawing.AddQuad(new(xstart, y), new(xend, y), new(xstart, yend), new(xend, yend), waterColor * waterAlpha);
            }

            Main.PixelEffect.Projection = matrix;
            Main.PixelEffect.VertexColorEnabled = true;
            TriangleDrawing.Draw(Main.PixelEffect);
        }

        private void DrawObjectNames(Renderer renderer)
        {
            if (Settings is null) 
                return;

            foreach (PlacedObject po in Settings.PlacedObjects)
            {
                Vector2 fixedPos = po.Position;
                fixedPos.Y = Size.Y * 20 - fixedPos.Y;

                string? name = po is UnloadedObject unloaded ? unloaded.Id : po.GetType().Name;

                Vector2 pos = renderer.TransformVector(WorldPos + fixedPos);

                if (!string.IsNullOrEmpty(name))
                {
                    string text = name;

                    if (po is BlueToken token && token.SandboxToken is not null)
                        text = $"{text} ({token.SandboxToken})";

                    Main.SpriteBatch.DrawStringAligned(Main.Consolas10, text, pos, Color.White, new(.5f));
                }
            }
        }
        private void DrawObjectIcons(Renderer renderer)
        {
            if (Settings is null)
                return;

            foreach (PlacedObject po in Settings.PlacedObjects)
            {
                string? name = po is UnloadedObject unloaded ? unloaded.Id : po.GetType().Name;
                if (name is null)
                    continue;

                Rectangle? source = GameAssets.GetObjectIconSource(name);
                if (source is null)
                    continue;

                Vector2 fixedPos = po.Position;
                fixedPos.Y = Size.Y * 20 - fixedPos.Y;
                Vector2 pos = WorldPos + fixedPos;

                Color? color = null;

                if (po is DataPearl pearl)
                    color = pearl.GetPearlColor();

                Vector2 size = source.Value.Size.ToVector2() * Main.PlacedObjectIconsScale;
                renderer.DrawTexture(GameAssets.Objects.Texture, pos - size/2, source.Value, size, color);

                if (po is BlueToken token && token.SandboxToken is not null)
                {
                    Rectangle? tokenSource = GameAssets.GetObjectIconSource(token.SandboxToken);

                    if (tokenSource is not null)
                    {
                        Vector2 tokenSize = tokenSource.Value.Size.ToVector2() * Main.PlacedObjectIconsScale;
                        Vector2 tokenPos = pos + new Vector2(size.X / 2 - tokenSize.X / 2, size.Y);

                        renderer.DrawTexture(GameAssets.Objects.Texture, tokenPos, tokenSource.Value, tokenSize);
                    }
                }
            }
        }

        private void DrawWater(Renderer renderer, int screenIndex)
        {
            if (Water is null || CameraScreens[screenIndex] is null)
                return;

            Vector2 parallaxCenter = renderer.InverseTransformVector(renderer.Size / 2) - WorldPos;
            float parallaxDistance = 10 / renderer.Size.X * renderer.Scale;

            Vector2 screenpos = CameraPositions[screenIndex];

            Matrix roomMatrix = Matrix.CreateTranslation(new(WorldPos, 0));
            roomMatrix = Matrix.Multiply(roomMatrix, renderer.Transform);
            roomMatrix = Matrix.Multiply(roomMatrix, renderer.Projection);

            Vector2 screensize = new(CameraScreens[screenIndex]!.Texture.Width, CameraScreens[screenIndex]!.Texture.Height);

            int start = (int)((screenpos.X - ScreenStart.X) / WaterData.TriangleSize);
            int end = (int)((screenpos.X + screensize.X - ScreenStart.X) / WaterData.TriangleSize) + 2;

            if (start > 0)
                start--;

            if (start == end)
                return;

            // skip {vertSkip} vertices when camera is too far
            float vertSkip = 0;
            float vertSkipCounter = 0;

            float pixLoss = 1 / renderer.Scale;
            if (pixLoss > 1)
                vertSkip = pixLoss / WaterData.TriangleSize;

            int vertexIndex = 0;

            for (int j = Math.Max(0, start - 10); j < end + 10; j++)
            {
                if (j < end - 1 && j > start && vertSkip > 0)
                {
                    vertSkipCounter += 1;
                    if (vertSkipCounter >= vertSkip)
                        vertSkipCounter -= vertSkip;
                    else
                        continue;
                }

                if (Water.Surface.GetLength(0) <= j)
                    continue;

                if (vertexIndex > Water.Vertices.Length - 1)
                    continue;

                float closePos = Water.Surface[j, 0].pos;
                float farPos = Water.Surface[j, 1].pos;

                Vector2 close = new()
                {
                    Y = Size.Y * 20 - (WaterHeight + closePos),
                    X = j * WaterData.TriangleSize + ScreenStart.X
                };

                Vector2 far = new()
                {
                    Y = Size.Y * 20 - (WaterHeight + farPos),
                    X = j * WaterData.TriangleSize + ScreenStart.X
                };

                if (Main.UseParallax)
                {
                    Vector2 parallaxDir = parallaxCenter - far;
                    parallaxDir *= parallaxDistance;
                    far += parallaxDir * 30;
                }
                else
                {
                    far = ApplyDepthOnVector(far, ScreenSize * new Vector2(.5f, 0.5f), 30);
                }

                if (far.Y > close.Y)
                    far.Y = close.Y;

                if (j == start || j == end - 1)
                    far.X = j * WaterData.TriangleSize + ScreenStart.X;

                Vector2 closeTexPos = (close - screenpos) / screensize;
                Vector2 farTexPos = (far - screenpos) / screensize;

                Water.Vertices[vertexIndex + 1].Position = far;
                Water.Vertices[vertexIndex + 1].TextureCoord = farTexPos;
                Water.Vertices[vertexIndex + 1].Depth = 1;

                Water.Vertices[vertexIndex].Position = close;
                Water.Vertices[vertexIndex].TextureCoord = closeTexPos;
                Water.Vertices[vertexIndex].Depth = 0;

                vertexIndex += 2;
            }

            if (vertexIndex > 3)
            {
                ApplyPaletteToShader(Main.WaterSurface, screenIndex);
                ApplyLevelToShader(Main.WaterSurface, screenIndex, renderer);
                GrabBuffer.ApplyToShader(Main.WaterSurface);
                Main.WaterSurface.Parameters["Projection"].SetValue(roomMatrix);
                Main.WaterSurface.Parameters["_waterDepth"].SetValue(WaterInFrontOfTerrain ? 0 : 1f / 30);
                
                Main.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
                Main.WaterSurface.CurrentTechnique.Passes[0].Apply();
                Main.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Water.Vertices, 0, vertexIndex - 2);
            }

            vertSkipCounter = 0;
            vertexIndex = 0;

            for (int j = start; j < end; j++)
            {
                if (j < end - 1 && j > start && vertSkip > 0)
                {
                    vertSkipCounter += 1;
                    if (vertSkipCounter >= vertSkip)
                        vertSkipCounter -= vertSkip;
                    else
                        continue;
                }

                if (Water.Surface.GetLength(0) <= j)
                    continue;

                if (vertexIndex > Water.Vertices.Length - 1)
                    continue;

                WaterData.SurfacePoint point = Water.Surface[j, 0];

                Vector2 vertPos = new()
                {
                    Y = Size.Y * 20 - (WaterHeight + point.pos),
                    X = j * WaterData.TriangleSize + ScreenStart.X
                };

                Vector2 texPos = (vertPos - screenpos) / screensize;

                // position in room
                // texture uv
                // water depth (0 - bottom)

                Water.Vertices[vertexIndex + 1].Position = vertPos;
                Water.Vertices[vertexIndex + 1].TextureCoord = texPos;
                Water.Vertices[vertexIndex + 1].Depth = 1;

                Water.Vertices[vertexIndex].Position = new Vector2(vertPos.X, screensize.Y + screenpos.Y);
                Water.Vertices[vertexIndex].TextureCoord = new Vector2(texPos.X, 1);
                Water.Vertices[vertexIndex].Depth = 1 - (screenpos.Y + screensize.Y) / ScreenSize.Y;

                vertexIndex += 2;
            }

            if (vertexIndex > 3)
            {
                ApplyPaletteToShader(Main.WaterColor, screenIndex);
                ApplyLevelToShader(Main.WaterColor, screenIndex, renderer);
                GrabBuffer.ApplyToShader(Main.WaterColor);

                Main.WaterColor.Parameters["Projection"].SetValue(roomMatrix);

                Main.WaterColor.Parameters["_screenOff"].SetValue(CameraPositions[screenIndex] / (Main.Noise?.Bounds.Size.ToVector2() ?? Vector2.One));
                Main.WaterColor.Parameters["_screenSize"].SetValue(screensize);
                Main.WaterColor.Parameters["_waterDepth"].SetValue(WaterInFrontOfTerrain ? 0 : 1f / 30);

                Main.WaterColor.CurrentTechnique.Passes[0].Apply();

                Main.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, Water.Vertices, 0, vertexIndex - 2);
            }
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

            if (FadePosValCache is null || FadePosValCache.Width != CameraPositions.Length)
                FadePosValCache = new(Main.Instance.GraphicsDevice, CameraPositions.Length, 1);

            Color[] fadePosValColors = ArrayPool<Color>.Shared.Rent(CameraPositions.Length);
            for (int i = 0; i < CameraScreens.Length; i++)
            {
                float texWidth = CameraScreens[i]?.Texture.Width ?? 0;
                float texHeight = CameraScreens[i]?.Texture.Height ?? 0;
                //max.X = Math.Max(max.X, CameraPositions[i].X + texWidth);
                //max.Y = Math.Max(max.Y, CameraPositions[i].Y + texHeight);
                fadePosValColors[i] = new
                (
                    (FixedCameraPositions[i].X + texWidth / 2 - ScreenStart.X) / ScreenSize.X,
                    (FixedCameraPositions[i].Y + texHeight / 2 - ScreenStart.Y) / ScreenSize.Y,
                    i < Settings?.FadePaletteValues?.Length ? Settings?.FadePaletteValues?[i] ?? 0 : 0
                );
            }
            FadePosValCache.SetData(fadePosValColors, 0, CameraPositions.Length);
            ArrayPool<Color>.Shared.Return(fadePosValColors);
        }

        public void ResetTileMap()
        {
            Color[] colors = new Microsoft.Xna.Framework.Color[Size.X * Size.Y];

            for (int j = 0; j < Size.Y; j++)
                for (int i = 0; i < Size.X; i++)
                {
                    Tile tile = GetTile(i, j);

                    float gray = 1;

                    bool solid = tile.Terrain == Tile.TerrainType.Solid;

                    if (solid)
                        gray = 0;

                    else if (tile.Terrain == Tile.TerrainType.Floor)
                        gray = 0.35f;

                    else if (tile.Terrain == Tile.TerrainType.Slope)
                        gray = .4f;

                    else if (Interface.RenderTileWalls && tile.Attributes.HasFlag(Tile.TileAttributes.WallBehind))
                        gray = 0.75f;

                    if (tile.Attributes.HasFlag(Tile.TileAttributes.VerticalBeam) || tile.Attributes.HasFlag(Tile.TileAttributes.HorizontalBeam))
                        gray = 0.35f;

                    byte b = (byte)(gray * 255);

                    Color color = new(b, b, b);

                    if ((WaterInFrontOfTerrain || !solid) && j >= Size.Y - WaterLevel)
                    {
                        Color waterColor = new(0, 0, 200);

                        color = Color.Lerp(color, waterColor, 0.4f);
                    }

                    colors[i + j * Size.X] = color;
                }

            foreach (Point p in RoomExits)
                colors[p.X + p.Y * Size.X] = new(255, 0, 0);

            TileMap ??= new(Main.Instance.GraphicsDevice, Size.X, Size.Y);
            TileMap.SetData(colors);
        }

        public Vector2 GetExitDirection(int index)
        {
            Point entrance = RoomExits[index];

            return GetShortcutDirection(entrance.X, entrance.Y);
        }
        public Vector2 GetShortcutDirection(int x, int y)
        {
            Vector2 direction = Vector2.Zero;

            for (int i = 0; i < 4; i++)
            {
                Point dir = Directions[i];
                Point tilePos = new Point(x, y) + dir;

                if (tilePos.X < 0 || tilePos.Y < 0 || tilePos.X >= Size.X || tilePos.Y >= Size.Y)
                    continue;

                Tile tile = Tiles[tilePos.X, tilePos.Y];
                if (tile.Terrain != Tile.TerrainType.Solid)
                    direction -= dir.ToVector2();
            }

            return direction;
        }

        public Tile GetTile(Point pos) => GetTile(pos.X, pos.Y);

        public Tile GetTile(int x, int y)
        {
            x = Math.Clamp(x, 0, Size.X - 1);
            y = Math.Clamp(y, 0, Size.Y - 1);
            return Tiles[x, y];
        }

        public void ApplyPaletteToShader(Effect effect, int screenIndex)
        {
            effect.Parameters["PaletteTex"]?.SetValue(Palettes.GetPalette(Settings?.Palette ?? 0));
            effect.Parameters["FadePaletteTex"]?.SetValue(Palettes.GetPalette(Settings?.FadePalette ?? 0));
            if (FadePosValCache is not null)
                effect.Parameters["FadePosValTex"]?.SetValue(FadePosValCache);
            effect.Parameters["FadeSize"]?.SetValue(CameraPositions.Length);
            effect.Parameters["FadeRect"]?.SetValue(new Vector4()
            {
                X = (CameraPositions[screenIndex].X - ScreenStart.X) / ScreenSize.X,
                Y = (CameraPositions[screenIndex].X + (CameraScreens[screenIndex]?.Texture.Width ?? 0) - ScreenStart.X) / ScreenSize.X,
                Z = (CameraPositions[screenIndex].Y - ScreenStart.Y) / ScreenSize.Y,
                W = (CameraPositions[screenIndex].Y + (CameraScreens[screenIndex]?.Texture.Height ?? 0) - ScreenStart.Y) / ScreenSize.Y,
            });

        }
        public void ApplyLevelToShader(Effect effect, int screenIndex, Renderer renderer)
        {
            if (Main.UseParallax)
            {
                Vector2 worldScreenCenter = renderer is CaptureRenderer
                ? renderer.Size / 2 + WorldPos + CameraPositions[screenIndex]
                : renderer.InverseTransformVector(renderer.Size / 2);
                Vector2 parallax = (worldScreenCenter - (WorldPos + CameraPositions[screenIndex])) / (CameraScreens[screenIndex]?.Texture.Size() ?? Vector2.One);

                effect.Parameters["ParallaxDistance"]?.SetValue(10 / renderer.Size.X * renderer.Scale);
                effect.Parameters["ParallaxCenter"]?.SetValue(parallax);
            }
            else
            {
                effect.Parameters["ParallaxDistance"]?.SetValue(0);
            }

            if (CameraScreens[screenIndex] is not null)
                effect.Parameters["LevelTex"]?.SetValue(CameraScreens[screenIndex]!.Texture);
        }

        void DrawLight(Renderer renderer, Texture2D? texture, Vector2 pos, float rad, Color color, int screen)
        {
            LightVertices[0].Position = pos + new Vector2(-rad, -rad);
            LightVertices[1].Position = pos + new Vector2(rad, -rad);
            LightVertices[2].Position = pos + new Vector2(-rad, rad);
            LightVertices[3].Position = pos + new Vector2(rad, rad);

            LightVertices[0].Color = color;
            LightVertices[1].Color = color;
            LightVertices[2].Color = color;
            LightVertices[3].Color = color;

            Vector2 screenPos = pos - CameraPositions[screen];
            Vector2 screenSize = CameraScreens[screen]?.Texture.Size() ?? Vector2.One;

            Vector2 topLeftUV = (screenPos - new Vector2(rad)) / screenSize;
            Vector2 bottomRightUV = (screenPos + new Vector2(rad)) / screenSize;

            if (topLeftUV.X > 1 || topLeftUV.Y > 1 || bottomRightUV.X < 0 || bottomRightUV.Y < 0)
                return;

            LightVertices[0].LevelTextureCoord = topLeftUV;
            LightVertices[1].LevelTextureCoord = new(bottomRightUV.X, topLeftUV.Y);
            LightVertices[2].LevelTextureCoord = new(topLeftUV.X, bottomRightUV.Y);
            LightVertices[3].LevelTextureCoord = bottomRightUV;

            Matrix matrix = Matrix.CreateTranslation(new(WorldPos, 0));
            matrix = Matrix.Multiply(matrix, renderer.Transform);
            matrix = Matrix.Multiply(matrix, renderer.Projection);

            ApplyLevelToShader(Main.LightSource, screen, renderer);

            Main.LightSource.Parameters["MainTex"]?.SetValue(texture ?? Main.Pixel);
            Main.LightSource.Parameters["Projection"]?.SetValue(matrix);
            Main.Instance.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            Main.Instance.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            Main.Instance.GraphicsDevice.SamplerStates[2] = SamplerState.PointClamp;
            Main.LightSource.CurrentTechnique.Passes[0].Apply();

            Main.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, LightVertices, 0, 2);
        }

        void FixCameraPositions()
        {
            if (FixedCameraPositions is null || FixedCameraPositions.Length != CameraPositions.Length)
            {
                FixedCameraPositions = new Vector2[CameraPositions.Length];
            }

            float quantizationSize = 20;

            for (int i = 0; i < CameraPositions.Length; i++)
            {
                Vector2 vec = CameraPositions[i];

                vec.X = MathF.Floor(vec.X / quantizationSize) * quantizationSize + quantizationSize / 2;
                vec.Y = MathF.Floor(vec.Y / quantizationSize) * quantizationSize + quantizationSize / 2;
                FixedCameraPositions[i] = vec;
            }

        }

        public Color PixelColorAtCoordinate(Vector2 coord)
        {
            Texture2D? texture = null;
            Vector2 texturePos = default;
            int screenId = 0;

            for (int i = 0; i < CameraScreens.Length; i++)
            {
                TextureAsset? screen = CameraScreens[i];
                if (screen is not null)
                {
                    Point pos = CameraPositions[i].ToPoint();
                    Point size = new(screen.Texture.Width, screen.Texture.Height);
                    Rectangle rect = new(pos, size);
                    if (rect.Contains(coord.ToPoint()))
                    {
                        texture = screen.Texture;
                        texturePos = CameraPositions[i];
                        screenId = i;
                        break;
                    }
                }
            }

            if (texture is null)
                return Color.Transparent;

            coord -= texturePos;

            Color pixel = texture.GetPixel((int)coord.X, (int)coord.Y);

            if (pixel.R == 255 && pixel.G == 255 && pixel.B == 255)
            {
                return SamplePalette(0, 7, screenId);
            }
            int red = pixel.R;
            float t = 0f;
            if (red > 90)
            {
                red -= 90;
            }
            else
            {
                t = 1f;
            }
            int paletteColor = red / 30;
            red = (red - 1) % 30;
            Color a = Color.Lerp(SamplePalette(red, paletteColor + 3, screenId), SamplePalette(red, paletteColor, screenId), t);
            return Color.Lerp(a, SamplePalette(1, 7, screenId), red * (1 - SamplePalette(9, 7, screenId).R / 255f) / 30f);
        }

        public Color SamplePalette(int x, int y, int screen)
        {
            Texture2D? palette = Palettes.GetPalette(Settings?.Palette ?? 0);
            Texture2D? fadePalette = Palettes.GetPalette(Settings?.FadePalette ?? 0);

            if (palette is null)
                return Color.Transparent;

            y = 7 - y;

            float fade = Settings?.FadePaletteValues?[screen] ?? 0;

            if (fadePalette is null || fade == 0)
                return palette.GetPixel(x, y);

            return Color.Lerp(palette.GetPixel(x, y), fadePalette.GetPixel(x, y), fade);
        }

        // I have no idea what that does
        static Vector2 ApplyDepthOnVector(Vector2 v, Vector2 depthPoint, float d)
        {
            d *= -0.025f;
            v -= depthPoint;
            d = (10f - d) * 0.1f;
            v /= d;
            v += depthPoint;
            return v;
        }

        public override string ToString()
        {
            return Name;
        }

        public class WaterData
        {
            public const float TriangleSize = 10;

            public SurfacePoint[,] Surface = null!;
            public WaterVertex[] Vertices = null!;
            public float SinCounter = 0;

            public WaterData(float roomWidth, float maxScreenWidth)
            {
                UpdateRoomSizes(roomWidth, maxScreenWidth);
            }

            public void UpdateRoomSizes(float roomWidth, float maxScreenWidth)
            {
                Surface = new SurfacePoint[(int)(roomWidth / TriangleSize + 10), 2];
                Vertices = new WaterVertex[(int)(maxScreenWidth / TriangleSize + 20) * 2];

                for (int i = 0; i < Surface.GetLength(0); i++)
                {
                    Surface[i, 0] = new SurfacePoint();
                    Surface[i, 1] = new SurfacePoint();
                }
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

    public record RoomConnection(Room Target, int Exit, int TargetExit);
    public record RoomShortcut(Point entrance, Point target, Tile.ShortcutType type);
}
