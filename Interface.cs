using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RainMap.UI;
using RainMap.UI.Elements;
using RainMap.UI.Structures;
using RWAPI;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.IO;
using System.Threading;

namespace RainMap
{
    public static class Interface
    {
        public static bool Hovered => Root.Hover is not null;

        static UIRoot Root = null!;
        static UIResizeablePanel SidePanel = null!;

        public static float CaptureScale = 1f;
        public static bool RenderTileWalls = true;

        public static InterfacePage Page = InterfacePage.RegionSelect;

        public static void Init()
        {
            Build();
            Main.Instance.Window.ClientSizeChanged += RootSizeChanged;
        }

        private static void Build()
        {
            switch (Page)
            {
                case InterfacePage.RegionSelect:
                    BuildRegionSelect();
                    break;

                case InterfacePage.Main:
                    BuildMain();
                    break;
            }
            Root.Recalculate();
        }
        private static void BuildRegionSelect()
        {
            Root = new(Main.Instance)
            {
                Font = Main.Consolas10,
                Elements =
                {
                    new UIPanel()
                    {
                        Top = new(0, .5f, -.5f),
                        Left = new(0, .5f, -.5f),

                        Width = 300,
                        Height = new(0, .9f),

                        Margin = 5,
                        Padding = new(5, 40),

                        Elements =
                        {
                            new UILabel()
                            {
                                Top = 10,
                                Height = 20,

                                Text = "Select region",
                                TextAlign = new(.5f)
                            },
                            new UIList()
                            {
                                Top = 40,
                                Height = new(-100, 1),
                                ElementSpacing = 5,

                            }.Execute(list =>
                            {
                                foreach (RegionData region in RainWorldAPI.EnumerateRegions())
                                {
                                    string? name = region.DisplayName ?? region.Id;
                                    if (name is null)
                                        continue;

                                    list.Elements.Add(new UIButton
                                    {
                                        Text = name,
                                        Height = 20,
                                        TextAlign = new(.5f)
                                    }.OnEvent(UIElement.ClickEvent, (_, _) =>
                                    {
                                        Page = InterfacePage.Main;
                                        Build();
                                        ThreadPool.QueueUserWorkItem((_) => Main.Instance.LoadRegion(region.Path, region.Id!));
                                    }));
                                }

                                list.Recalculate();
                            }),

                            new UIButton ()
                            {
                                Top = new(-50, 1),

                                Height = 20,
                                Text = "Manual select",
                                TextAlign = new(.5f)

                            }.OnEvent(UIElement.ClickEvent, (_, _) =>
                            {
                                Thread dirSelect = new(() =>
                                {
                                    System.Windows.Forms.FolderBrowserDialog fd = new();
                                    fd.UseDescriptionForTitle = true;
                                    fd.Description = "Select Rain World region folder. For example RainWorld_Data/StreamingAssets/world/su";
                                    if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                                        return;

                                    string id = Path.GetFileName(fd.SelectedPath);
                                    MultiDirectory dir = new(new[] { fd.SelectedPath });

                                    Page = InterfacePage.Main;
                                    Build();
                                    ThreadPool.QueueUserWorkItem((_) => Main.Instance.LoadRegion(dir, id));
                                });
                                dirSelect.SetApartmentState(ApartmentState.STA);
                                dirSelect.Start();
                                dirSelect.Join();
                            })
                        }
                    }
                }
            };
        }
        private static void BuildMain()
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
                        "Right mouse click and drag - move camera around\n\n" +
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
                        Height = new(-150, 1),

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

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = Main.MappingMode,
                                Text = "Mapping mode",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.Instance.SetMappingMode(btn.Selected)),

                            new UIButton
                            {
                                Height = 18,

                                Selectable = true,
                                Selected = RenderTileWalls,
                                Text = "Render tile walls",

                                HoverText = "Render room tiles with walls",

                                SelectedBackColor = Color.White,
                                SelectedTextColor = Color.Black,

                            }.OnEvent(UIElement.ClickEvent, (btn, _) =>
                            {
                                RenderTileWalls = btn.Selected;
                                Main.Instance.SetMappingMode(Main.MappingMode);
                            }),
                        }
                    },

                    new UILabel
                    {
                        Height = 18,

                        Top = new(-112, 1),

                        Text = $"Object icons scale: {Main.PlacedObjectIconsScale:0.00}",
                        TextAlign = new(0, .5f)
                    }.Assign(out UILabel objScaleLabel),
                    new UIScrollBar
                    {
                        Height = 9,
                        Top = new(-94, 1),

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

                        Top = new(-78, 1),

                        Text = $"Render scale: {CaptureScale:0.00}",
                        TextAlign = new(0, .5f)

                    }.Assign(out UILabel scale),

                    new UIButton
                    {
                        Height = 18,
                        Width = 18,
                        Top = new(-78, 1),
                        Left = new(-23, 1, -1),

                        Text = "+",
                        TextAlign = new(.5f),

                    }.OnEvent(UIElement.ClickEvent, (btn, _) => { CaptureScale *= 2; scale.Text = $"Render scale: {CaptureScale:0.00}"; }),

                    new UIButton
                    {
                        Height = 18,
                        Width = 18,
                        Top = new(-78, 1),
                        Left = new(0, 1, -1),

                        Text = "-",
                        TextAlign = new(.5f),

                    }.OnEvent(UIElement.ClickEvent, (btn, _) => { CaptureScale *= .5f; scale.Text = $"Render scale: {CaptureScale:0.00}"; }),

                    new UIButton
                    {
                        Height = 25,

                        Top = new(-55, 1),
                        TextAlign = new(.5f),
                        Text = "Render region rooms",
                        HoverText = "Render rooms separately into\nRegionRooms_XX folder\n(using selected render scale)",

                    }.OnEvent(UIElement.ClickEvent, RenderRegionRoomsClicked),
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
                CaptureManager.CaptureRegionRooms(Main.Region, CaptureScale);
        }
        private static void RenderEntireRegionClicked(UIButton button, Empty _)
        {
            if (Main.Region is null)
                return;

            string renderFile = null;
            Thread thd = new(() =>
            {
                System.Windows.Forms.SaveFileDialog sfd = new();
                sfd.Title = "Select render save file";
                sfd.Filter = Main.MappingMode ? "PNG Image|*.png" : "TIFF Image|*.tiff";
                if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    renderFile = sfd.FileName;
            });
            thd.SetApartmentState(ApartmentState.STA);
            thd.Start();
            thd.Join();

            if (renderFile is not null)
            {
                var capResult = CaptureManager.CaptureEntireRegion(Main.Region, CaptureScale);

                IImageEncoder encoder = Main.MappingMode ? new PngEncoder() : new SixLabors.ImageSharp.Formats.Tiff.TiffEncoder()
                {
                    Compression = SixLabors.ImageSharp.Formats.Tiff.Constants.TiffCompression.Deflate,
                };

                using FileStream fs = File.Create(renderFile);
                Main.Instance.Window.Title = "Saving region capture";
                capResult.Save(fs, encoder);
                Main.Instance.Window.Title = "Freeing region capture";
                capResult.Dispose();
                GC.Collect();
            }
        }

        private static void RootSizeChanged(object? sender, EventArgs e)
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

        public enum InterfacePage
        {
            RegionSelect,
            Main
        }
    }
}
