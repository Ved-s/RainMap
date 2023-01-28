using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RainMap.Renderers;
using RainMap.Structures;
using RWAPI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace RainMap
{
    public class Main : Game
    {
        public static Main Instance = null!;

        public GraphicsDeviceManager GraphicsManager;

        public static SpriteBatch SpriteBatch = null!;

        public static Effect RoomColor = null!;
        public static Effect WaterColor = null!;
        public static Effect WaterSurface = null!;
        public static Effect LightSource = null!;
        public static SpriteFont Consolas10 = null!;

        public static Texture2D? Noise;
        public static Texture2D? EffectColors;
        public static Texture2D Pixel = null!;
        public static Texture2D Transparent = null!;

        public static Region? Region;
        public static Matrix Projection;

        public static BasicEffect PixelEffect = null!;

        public static GameTime TimeCache = null!;

        public static MouseState OldMouseState;
        public static MouseState MouseState;
        public static KeyboardState OldKeyboardState;
        public static KeyboardState KeyboardState;

        public static CameraRenderer WorldCamera = null!;

        public static bool RenderConnections = true;
        public static bool RenderRoomLevel = true;
        public static bool RenderRoomTiles = false;
        public static bool DrawObjectNames = false;
        public static bool DrawObjectIcons = false;
        public static bool DrawInfo = true;
        public static bool UseParallax = false;

        public static float PlacedObjectIconsScale = 1f;

        public static bool RenderTilesWithPalette = false;

        public static TimeLogger<MainDrawTime> MainTimeLogger = new();
        public static TimeLogger<RoomDrawTime> RoomTimeLogger = new();
        private static int FPS;
        private static int FPSCounter;
        private static Stopwatch FPSWatcher = new();
        private static HashSet<Room> SelectedRooms = new();
        private static Vector2 OldDragPos;
        private static bool Selecting;
        private static bool Dragging;
        private static Thread MainThread = null!;
        private static Queue<Action> MainQueue = new();

        public Main()
        {
            Instance = this;
            MainThread = Thread.CurrentThread;
            GraphicsManager = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
            FPSWatcher.Start();
            GraphicsDevice.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
            PlacedObjects.PlacedObject.RegisterAll();
        }

        protected override void LoadContent()
        {
            SpriteBatch = new(GraphicsDevice);
            WorldCamera = new(SpriteBatch);

            RoomColor = Content.Load<Effect>("RoomColor");
            WaterColor = Content.Load<Effect>("WaterColor");
            WaterSurface = Content.Load<Effect>("WaterSurface");
            LightSource = Content.Load<Effect>("LightSource");
            Consolas10 = Content.Load<SpriteFont>("Consolas10");

            Pixel = new(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Microsoft.Xna.Framework.Color.White });

            UI.GraphicExtensions.Pixel = Pixel;

            Transparent = new(GraphicsDevice, 1, 1);
            Transparent.SetData(new[] { Microsoft.Xna.Framework.Color.Transparent });

            GameAssets.LoadContent(Content);

            PixelEffect = new(GraphicsDevice);
            PixelEffect.Texture = Pixel;
            PixelEffect.TextureEnabled = true;

            RoomColor.Parameters["_cloudsSpeed"]?.SetValue(1);
            RoomColor.Parameters["GrabTex"]?.SetValue(Transparent);
            WaterColor.Parameters["GrabTexture"]?.SetValue(Transparent);

            if (!RainWorldAPI.SearchRainWorld())
            {
                Thread rwSelect = new(() =>
                {
                    System.Windows.Forms.OpenFileDialog fd = new();
                    fd.Filter = "Executable (.exe)|*.exe";
                    fd.Title = "Select Rain World main executable.";
                    if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        Exit();

                    RainWorldAPI.SetRainWorldRoot(Path.GetDirectoryName(fd.FileName)!);
                });
                rwSelect.SetApartmentState(ApartmentState.STA);
                rwSelect.Start();
                rwSelect.Join();
            }

            Interface.Init();
        }

        protected override void Update(GameTime gameTime)
        {
            TimeCache = gameTime;
            base.Update(gameTime);
            TextureLoader.AwakeIfZombie();

            OldMouseState = MouseState;
            OldKeyboardState = KeyboardState;

            MouseState = Mouse.GetState();
            KeyboardState = Keyboard.GetState();

            while (MainQueue.TryDequeue(out Action? invoker))
                invoker();

            float shaderTime = (float)gameTime.TotalGameTime.TotalSeconds / 5;

            RoomColor?.Parameters["_RAIN"]?.SetValue(shaderTime);
            WaterColor?.Parameters["_RAIN"]?.SetValue(shaderTime);
            PixelEffect.Projection = Projection;

            Region?.Update();
            Vector2 worldPos = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());

            bool drag = IsActive && MouseState.LeftButton == ButtonState.Pressed;
            bool oldDrag = OldMouseState.LeftButton == ButtonState.Pressed;

            if (drag && !oldDrag && !Interface.Hovered)
            {
                Room? room = Region?.Rooms.LastOrDefault(r => worldPos.X > r.WorldPos.X + r.ScreenStart.X
                                                                  && worldPos.Y > r.WorldPos.Y + r.ScreenStart.Y
                                                                  && worldPos.X < r.WorldPos.X + r.ScreenStart.X + r.ScreenSize.X
                                                                  && worldPos.Y < r.WorldPos.Y + r.ScreenStart.Y + r.ScreenSize.Y);
                OldDragPos = worldPos;

                if (room is not null)
                {
                    if (!SelectedRooms.Contains(room))
                    {
                        Region!.Rooms.Remove(room);
                        Region!.Rooms.Add(room);
                        SelectedRooms.Clear();
                        SelectedRooms.Add(room);
                    }
                    Dragging = true;
                }
                else if (room is null)
                {
                    SelectedRooms.Clear();
                    Selecting = true;
                }
            }

            if (drag && Dragging)
            {
                Vector2 diff = worldPos - OldDragPos;
                if (diff != Vector2.Zero)
                    foreach (Room r in SelectedRooms)
                        r.WorldPos += diff;
                OldDragPos = worldPos;
            }

            if (!drag && oldDrag)
            {
                if (Selecting)
                {
                    Vector2 tl = new(Math.Min(OldDragPos.X, worldPos.X), Math.Min(OldDragPos.Y, worldPos.Y));
                    Vector2 br = new(Math.Max(OldDragPos.X, worldPos.X), Math.Max(OldDragPos.Y, worldPos.Y));

                    SelectedRooms.Clear();
                    if (Region is not null)
                        SelectedRooms.UnionWith(Region.Rooms.Where(r => r.IntersectsWith(tl, br)));
                }

                Dragging = false;
                Selecting = false;
            }

            if (IsActive)
            {
                WorldCamera.Update();

                if (KeyboardState.IsKeyDown(Keys.F9) && OldKeyboardState.IsKeyUp(Keys.F9))
                    DrawInfo = !DrawInfo;
            }

            if (Region is not null)
            {
                Window.Title = $"RainMap " +
                    $"FPS: {FPS}" +
                    $"{(TextureLoader.QueueLength == 0 ? "" : $" Loading {TextureLoader.QueueLength} textures")}";
            }

            Interface.Update();
        }

        protected override void Draw(GameTime gameTime)
        {
            Viewport vp = GraphicsDevice.Viewport;
            GraphicsDevice.ScissorRectangle = new(0, 0, vp.Width, vp.Height);
            Projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);

			GraphicsDevice.Clear(Region?.BackgroundColor ?? Microsoft.Xna.Framework.Color.CornflowerBlue);

			DrawRoomSelection(SelectedRooms);

			MainTimeLogger.StartWatch(MainDrawTime.Region);

            Region?.Draw(WorldCamera, RenderConnections);

            MainTimeLogger.StartWatch(MainDrawTime.Selection);

            if (SelectedRooms.Count == 1)
            {
                SpriteBatch.Begin();

                Room r = SelectedRooms.First();

                WorldCamera.DrawRect(r.WorldPos, r.Size.ToVector2() * 20, null, Microsoft.Xna.Framework.Color.Lime * 0.3f);
                WorldCamera.DrawRect(r.WorldPos + r.ScreenStart, r.ScreenSize, null, Microsoft.Xna.Framework.Color.Red * 0.3f);
                SpriteBatch.End();
            }

            if (Selecting)
            {
                SpriteBatch.Begin();
                Vector2 a = OldDragPos;
                Vector2 b = WorldCamera.InverseTransformVector(MouseState.Position.ToVector2());

                Vector2 tl = new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Vector2 br = new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

                WorldCamera.DrawRect(tl, br - tl, Microsoft.Xna.Framework.Color.LightBlue * 0.2f);
                SpriteBatch.End();
            }

            Interface.Draw();

            if (DrawInfo)
                DrawTimingInfo();
        }

        private void DrawRoomSelection(HashSet<Room> SelectedRooms)
        {
            SpriteBatch.Begin();

            foreach (Room r in SelectedRooms)
            {
                for (int i = 0; i < r.CameraScreens.Length; i++)
                {
                    TextureAsset? screenTex = r.CameraScreens[i];
                    if (screenTex?.Loaded is null or false)
                        continue;

                    WorldCamera.DrawRect(r.WorldPos + r.CameraPositions[i] - new Vector2(2) / WorldCamera.Scale, screenTex.Texture.Size() + new Vector2(4) / WorldCamera.Scale, Microsoft.Xna.Framework.Color.White * 0.4f);
                }
            }

            SpriteBatch.End();
        }

        protected void DrawTimingInfo()
        {
            List<string> lines = new()
            {
                $"World Camera Scale: {WorldCamera.Scale:0.##}",
                $"Main rendering time",
            };

            foreach (var kvp in MainTimeLogger.Times)
                lines.Add($"{kvp.Key}: {kvp.Value.TotalMilliseconds:0.00}ms");

            lines.Add($"Room rendering times");
            foreach (var kvp in RoomTimeLogger.Times)
                lines.Add($"{kvp.Key}: {kvp.Value.TotalMilliseconds:0.00}ms");

            float x = 10;
            float y = 10;
            SpriteBatch.Begin();
            foreach (var line in lines)
            {
                SpriteBatch.DrawStringShaded(Consolas10, line, new(x, y), Microsoft.Xna.Framework.Color.White);
                y += Consolas10.LineSpacing;
            }
            SpriteBatch.End();
        }

        protected override void EndDraw()
        {
            MainTimeLogger.StartWatch(MainDrawTime.Present);
            base.EndDraw();

            FPSCounter++;
            if (FPSWatcher.Elapsed.TotalSeconds > 1)
            {
                FPSWatcher.Restart();
                FPS = FPSCounter;
                FPSCounter = 0;
            }

            MainTimeLogger.FinishWatch();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);
            TextureLoader.Finish();
        }

        public void LoadRegion(MultiDirectory path, string id)
        {
            Region = null;
            try
            {
                Noise = RainWorldAPI.Assets?.FindTexture("palettes/noise.png");
                EffectColors = RainWorldAPI.Assets?.FindTexture("palettes/effectColors.png");

                RoomColor.Parameters["NoiseTex"]?.SetValue(Noise);
                WaterColor.Parameters["NoiseTex"]?.SetValue(Noise);
                RoomColor.Parameters["EffectColors"]?.SetValue(EffectColors);

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        Region = Region.Load(path, id);
                    }
                    catch (Exception ex)
                    {
                        Window.Title = $"Caught an exception while loading region: {ex.Message}";
                    }
                });
            }
            catch (Exception ex)
            {
                Window.Title = $"Caught an exception while loading assets: {ex.Message}";
            }
        }

        public static void InvokeMainThread(Action callback, bool wait = true)
        {
            bool onMainThread = MainThread == Thread.CurrentThread;
            if (onMainThread)
            {
                callback();
                return;
            }

            if (!wait)
            {
                MainQueue.Enqueue(callback);
                return;
            }

            AutoResetEvent trigger = new(false);
            Action wrapper = () =>
            {
                callback();
                trigger.Set();
            };
            MainQueue.Enqueue(wrapper);

            trigger.WaitOne();
        }
    }
}