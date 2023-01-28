using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RainMap.UI;
using RainMap.UI.Elements;
using RainMap.UI.Structures;
using System;
using System.IO;
using System.Threading;

#nullable disable

namespace RainMap
{
    public static class Interface
    {
        public static bool Hovered => Root.Hover is not null;

        static UIRoot Root;
        static UIResizeablePanel SidePanel;

        static float CaptureScale = 1f;

        public static void Init()
        {
            Build();
            Root.Recalculate();

            Main.Instance.Window.ClientSizeChanged += RootSizeChanged;
        }

        private static void Build()
        {
            Root = new(Main.Instance)
            {
                Font = Main.Consolas10,
                Elements =
                {
                    new UIResizeablePanel()
                    {
                        Left = new(0, 1, -1),

                        Width = 200,
                        Height = new(0, 1),
                        MaxWidth = new(0, 1f),
                        MinWidth = new(80, 0),

                        Margin = 5,

                        BackColor = Color.Transparent,
                        BorderColor = Color.Transparent,

                        CanGrabTop = false,
                        CanGrabRight = false,
                        CanGrabBottom = false,
                        SizingChangesPosition = false,

                        Elements =
                        {
                            new TabContainer
                            {
                                Tabs =
                                {
                                    new()
                                    {
                                        Name = "Controls",
                                        Element = BuildControls()
                                    },
                                    new()
                                    {
                                        Name = "Settings",
                                        Element = BuildSettings()
                                    }
                                }
                            }
                        }
                    }.Assign(out SidePanel)
                }
            };
        }

        private static UIElement BuildControls()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UILabel()
                    {
                        Text =
                        "Left mouse click and drag - room selection and moving\n\n" +
                        "Right mouse click and drag - move camear around\n\n" +
                        "Mouse scroll - zoom\n\n"
                    }
                }
            };
        }
        private static UIElement BuildSettings()
        {
            return new UIPanel
            {
                BackColor = new(30, 30, 30),
                BorderColor = new(100, 100, 100),

                Padding = new(5),

                Elements =
                {
                    new UIList
                    {
                        Height = new(-200, 1),

                        ElementSpacing = 5,
                        Elements =
                        {
                            new UILabel
                            {
                                Height = 20,
                                Text = "Render layers",
                                TextAlign = new(.5f),
                                Margin = new(5, 0, 0, 0)
                            },

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.RenderRoomLevel,
                                Text = "Room level",

                                HoverText = "Render rooms close to in-game view",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderRoomLevel = btn.Selected),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.RenderRoomTiles,
                                Text = "Room tiles",

                                HoverText = "Render room tile geometry",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderRoomTiles = btn.Selected),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.DrawObjectNames,
                                Text = "Placed object names",

                                HoverText = "Render object names in rooms",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.DrawObjectNames = btn.Selected),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.DrawObjectIcons,
                                Text = "Placed object icons",

                                HoverText = "Render object icons in rooms",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.DrawObjectIcons = btn.Selected),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.RenderConnections,
                                Text = "Room connections",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderConnections = btn.Selected),

                            new UIElement
                            {
                                Height = 20,
                            },

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.RenderTilesWithPalette,
                                Text = "Render tiles with palette",

                                HoverText = "Render tile layer using room's palette",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderTilesWithPalette = btn.Selected),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.UseParallax,
                                Text = "Room parallax effect",

                                HoverText = "Experimental room effect",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.UseParallax = btn.Selected),
                        }
                    },

                    new UILabel
                    {
                        Height = 18,

                        Top = new(-142, 1),

                        Text = $"Object icons scale: {Main.PlacedObjectIconsScale:0.00}",
                        TextAlign = new(0, .5f)
                    }.Assign(out UILabel objScaleLabel),
                    new UIScrollBar
                    {
                        Height = 9,
                        Top = new(-124, 1),

                        BarPadding = new(-3, 0),
                        BarSize = 9,
                        BarSizeAbsolute = true,
                        Horizontal = true,
                        ScrollMin = 0.1f,
                        ScrollMax = 4,

                        BackColor = new(40, 40, 40),
                    }.OnEvent(UIScrollBar.ScrollChanged, (_, scroll) =>
                    {
                        Main.PlacedObjectIconsScale = scroll;
                        objScaleLabel.Text = $"Object icons scale: {Main.PlacedObjectIconsScale:0.00}";
                    }),

                    new UILabel
                    {
                        Height = 18,
                        Width = new(-40, 1),

                        Top = new(-108, 1),

                        Text = $"Render scale: {CaptureScale:0.00}",
                        TextAlign = new(0, .5f)

                    }.Assign(out UILabel scale),

                    new UIButton
                    {
                        Height = 18,
                        Width = 18,
                        Top = new(-108, 1),
                        Left = new(-23, 1, -1),

                        Text = "+",
                        TextAlign = new(.5f),

                    }.OnEvent(UIElement.ClickEvent, (btn, _) => { CaptureScale *= 2; scale.Text = $"Render scale: {CaptureScale:0.00}"; }),

                    new UIButton
                    {
                        Height = 18,
                        Width = 18,
                        Top = new(-108, 1),
                        Left = new(0, 1, -1),

                        Text = "-",
                        TextAlign = new(.5f),

                    }.OnEvent(UIElement.ClickEvent, (btn, _) => { CaptureScale *= .5f; scale.Text = $"Render scale: {CaptureScale:0.00}"; }),

                    new UIButton
                    {
                        Height = 25,

                        Top = new(-85, 1),
                        TextAlign = new(.5f),
                        Text = "Render region rooms",
                        HoverText = "Render rooms separately into\nRegionRooms_XX folder\n(using selected render scale)",

                    }.OnEvent(UIElement.ClickEvent, RenderRegionRoomsClicked),
                    new UIButton
                    {
                        Height = 25,

                        Top = new(-55, 1),
                        TextAlign = new(.5f),
                        Text = "Render region room tiles",
                        HoverText = "Render room tiles separately\ninto RegionRooms_XX_tiles)",

                    }.OnEvent(UIElement.ClickEvent, RenderRegionRoomTilesClicked),
                    new UIButton
                    {
                        Height = 25,

                        Top = new(-25, 1),
                        TextAlign = new(.5f),
                        Text = "Render entire region",
                        HoverText = "Render entire region into one\nimage file using selected render scale",

                    }.OnEvent(UIElement.ClickEvent, RenderEntireRegionClicked)
                }
            };
        }

        private static void RenderRegionRoomsClicked(UIButton button, Empty _)
        {
            if (Main.Region is not null)
                CaptureManager.CaptureRegionRooms(Main.Region, CaptureScale, false);
        }
        private static void RenderRegionRoomTilesClicked(UIButton button, Empty _)
        {
            if (Main.Region is not null)
                CaptureManager.CaptureRegionRooms(Main.Region, CaptureScale, true);
        }
        private static void RenderEntireRegionClicked(UIButton button, Empty _)
        {
            string renderFile = null;
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
                var capResult = CaptureManager.CaptureEntireRegion(Main.Region, CaptureScale);

                using FileStream fs = File.Create(renderFile);
                Main.Instance.Window.Title = "Saving region capture";
                capResult.Save(fs, new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder()
                {
                    Compression = SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Deflate,
                });
                Main.Instance.Window.Title = "Freeing region capture";
                capResult.Dispose();
                GC.Collect();
            }
        }

        private static void RootSizeChanged(object sender, EventArgs e)
        {
            Root.Recalculate();
        }

        public static void Update()
        {
            if (Main.OldKeyboardState.IsKeyUp(Keys.F12) && Main.KeyboardState.IsKeyDown(Keys.F12))
            {
                Build();
                Root.Recalculate();
            }

            Root.Update();

            if (Root.GetKeyState(Keys.F1) == KeybindState.JustPressed)
                SidePanel.Visible = !SidePanel.Visible;
        }

        public static void Draw()
        {
            Main.SpriteBatch.Begin();
            Root.Draw(Main.SpriteBatch);
            Main.SpriteBatch.End();
        }
    }
}
