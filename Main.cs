using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        public static SpriteFont Consolas10 = null!;

        public static Texture2D? Noise;
        public static Texture2D? EffectColors;
        public static Texture2D Pixel = null!;

        public static Region? Region;
        public static Matrix Projection;

        public static BasicEffect PixelEffect = null!;

        public static GameTime TimeCache = null!;

        public static MouseState OldMouseState;
        public static MouseState MouseState;
        public static KeyboardState OldKeyboardState;
        public static KeyboardState KeyboardState;

        static Stopwatch DrawTineWatch = new();
        static int FPS;
        static int FPSCounter;
        static Stopwatch FPSWatcher = new();

        static HashSet<Room> SelectedRooms = new();
        static Vector2 OldDragPos;
        static bool Selecting;
        static bool Dragging;
        static Thread MainThread = null!;
        static Queue<Action> MainQueue = new();

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
        }

        protected override void LoadContent()
        {
            SpriteBatch = new(GraphicsDevice);

            RoomColor = Content.Load<Effect>("RoomColor");
            WaterColor = Content.Load<Effect>("WaterColor");
            WaterSurface = Content.Load<Effect>("WaterSurface");
            Consolas10 = Content.Load<SpriteFont>("Consolas10");

            Pixel = new(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });

            PixelEffect = new(GraphicsDevice);
            PixelEffect.Texture = Pixel;
            PixelEffect.TextureEnabled = true;

            RoomColor.Parameters["_cloudsSpeed"].SetValue(1);

            Thread dirSelect = new(() =>
            {
                System.Windows.Forms.FolderBrowserDialog fd = new();
                fd.Description = "Select Rain World region";
                if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                    Exit();

                LoadRegion(fd.SelectedPath);
            });
            dirSelect.SetApartmentState(ApartmentState.STA);
            dirSelect.Start();
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

            RoomColor?.Parameters["_RAIN"].SetValue(shaderTime);
            WaterColor?.Parameters["_RAIN"].SetValue(shaderTime);
            PixelEffect.Projection = Projection;

            Region?.Update();
            Vector2 worldPos = PanNZoom.ScreenToWorld(MouseState.Position.ToVector2());

            bool drag = IsActive && MouseState.LeftButton == ButtonState.Pressed;
            bool oldDrag = OldMouseState.LeftButton == ButtonState.Pressed;

            if (drag && !oldDrag)
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
                PanNZoom.Update();

            if (KeyboardState.IsKeyDown(Keys.F7) && OldKeyboardState.IsKeyUp(Keys.F7))
            {
                foreach (Room room in SelectedRooms)
                    room.UpdateScreenSize();
            }

            if (Region is not null)
            {
                Window.Title = $"RainMap " +
                    $"DT: {DrawTineWatch.Elapsed.TotalMilliseconds.ToString("0.00", CultureInfo.InvariantCulture)}ms " +
                    $"FPS: {FPS}" +
                    $"{(TextureLoader.QueueLength == 0 ? "" : $" Loading {TextureLoader.QueueLength} textures")}";
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            Viewport vp = GraphicsDevice.Viewport;
            Projection = Matrix.CreateOrthographicOffCenter(0, vp.Width, vp.Height, 0, 0, 1);

            //if (Region is not null && Region.TryGetRoom("FS_C04", out Room? test) && test.CameraScreens.All(a => a is null || a.Loaded))
            //{
            //    CaptureManager.RenderTarget ??= new(GraphicsDevice, 1400, 800);
            //
            //    GraphicsDevice.SetRenderTarget(CaptureManager.RenderTarget);
            //    test.PrepareDraw();
            //    test.DrawScreen(true, 1);
            //    GraphicsDevice.SetRenderTarget(null);
            //
            //    SpriteBatch.Begin();
            //    SpriteBatch.Draw(CaptureManager.RenderTarget, new Rectangle(0, 0, 700, 400), Color.White);
            //    SpriteBatch.End();
            //    return;
            //}

            bool capture = IsActive && KeyboardState.IsKeyDown(Keys.F8);
            bool oldCapture = IsActive && OldKeyboardState.IsKeyDown(Keys.F8);
            if (capture && !oldCapture && Region is not null)
            {
                string? renderFile = null;
                Thread thd = new(() =>
                {
                    System.Windows.Forms.SaveFileDialog sfd = new();
                    sfd.Title = "Select render save file";
                    sfd.Filter = "TIFF Image|*.tiff";
                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        renderFile = sfd.FileName;
                });
                thd.SetApartmentState(ApartmentState.STA);
                thd.Start();
                thd.Join();

                if (renderFile is not null)
                {
                    var capResult = CaptureManager.CaptureRegion(Region);

                    using FileStream fs = File.Create(renderFile);
                    Window.Title = "Saving region capture";
                    capResult.Save(fs, new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder()
                    {
                        Compression = SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Deflate,
                    });
                    Window.Title = "Freeing region capture";
                    capResult.Dispose();
                    GC.Collect();
                }
                
            }

            DrawTineWatch.Restart();
            GraphicsDevice.Clear(Region?.BackgroundColor ?? Color.CornflowerBlue);

            SpriteBatch.Begin();
            foreach (Room r in SelectedRooms)
            {
                for (int i = 0; i < r.CameraScreens.Length; i++)
                {
                    TextureAsset? screenTex = r.CameraScreens[i];
                    if (screenTex?.Loaded is null or false)
                        continue;

                    Vector2 pos = PanNZoom.WorldToScreen(r.WorldPos + r.CameraPositions[i]);
                    pos -= new Vector2(2);
                    Vector2 size = screenTex.Texture.Size() * PanNZoom.Zoom;
                    size += new Vector2(4);

                    SpriteBatch.DrawRect(pos, size, Color.White * 0.4f);
                }
            }
            SpriteBatch.End();

            Region?.Draw();

            if (SelectedRooms.Count == 1)
            {
                SpriteBatch.Begin();

                Room r = SelectedRooms.First();

                SpriteBatch.DrawRect(PanNZoom.WorldToScreen(r.WorldPos), r.Size.ToVector2() * 20 * PanNZoom.Zoom, null, Color.Lime * 0.3f);
                SpriteBatch.DrawRect(PanNZoom.WorldToScreen(r.WorldPos + r.ScreenStart), r.ScreenSize * PanNZoom.Zoom, null, Color.Red * 0.3f);
                SpriteBatch.End();
            }

            if (Selecting)
            {
                SpriteBatch.Begin();
                Vector2 a = PanNZoom.WorldToScreen(OldDragPos);
                Vector2 b = Mouse.GetState().Position.ToVector2();

                Vector2 tl = new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Vector2 br = new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));

                SpriteBatch.Draw(Pixel, tl, null, Color.LightBlue * 0.2f, 0f, Vector2.Zero, br - tl, SpriteEffects.None, 0);
                SpriteBatch.End();
            }

            base.Draw(gameTime);
            DrawTineWatch.Stop();
            FPSCounter++;
            if (FPSWatcher.Elapsed.TotalSeconds > 1)
            {
                FPSWatcher.Restart();
                FPS = FPSCounter;
                FPSCounter = 0;
            }
        }

        public void LoadRegion(string path)
        {
            Region = null;
            try
            {
                string? rwpath = path;
                while (true)
                {
                    if (rwpath is null)
                        throw new Exception("Could not find RainWorld.exe");

                    if (File.Exists(Path.Combine(rwpath, "RainWorld.exe")))
                        break;

                    rwpath = Path.GetDirectoryName(rwpath);
                }

                Noise = Texture2D.FromFile(GraphicsDevice, Path.Combine(rwpath, "Assets\\Futile\\Resources\\Palettes\\noise.png"));
                EffectColors = Texture2D.FromFile(GraphicsDevice, Path.Combine(rwpath, "Assets\\Futile\\Resources\\Palettes\\effectColors.png"));
                Palettes.SetPalettePath(Path.Combine(rwpath, "Assets\\Futile\\Resources\\Palettes"));

                RoomColor.Parameters["NoiseTex"].SetValue(Noise);
                WaterColor.Parameters["NoiseTex"].SetValue(Noise);
                RoomColor.Parameters["EffectColors"].SetValue(EffectColors);

                ThreadPool.QueueUserWorkItem((_) =>
                {
                    try
                    {
                        Region = Region.Load(path);
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