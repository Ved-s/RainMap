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

        public static void Init()
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
                                        Name = "Settings",
                                        Element = new UIPanel
                                        {
                                            BackColor = new(30, 30, 30),
                                            BorderColor = new(100, 100, 100),

                                            Padding = new(5),

                                            Elements =
                                            {
                                                new UIList
                                                {
                                                    Height = new(-30, 1),

                                                    ElementSpacing = 5,
                                                    Elements =
                                                    {
                                                        new UIButton
                                                        {
                                                            Height = 18,

                                                            Selectable = true,
                                                            Selected = Main.RenderRoomLevel,
                                                            Text = "Room level layer",

                                                            SelectedBackColor = Color.White,
                                                            SelectedTextColor = Color.Black,

                                                        }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderRoomLevel = btn.Selected),

                                                        new UIButton
                                                        {
                                                            Height = 18,

                                                            Selectable = true,
                                                            Selected = Main.RenderRoomTiles,
                                                            Text = "Room tile layer",

                                                            SelectedBackColor = Color.White,
                                                            SelectedTextColor = Color.Black,

                                                        }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderRoomTiles = btn.Selected),

                                                        new UIButton
                                                        {
                                                            Height = 18,

                                                            Selectable = true,
                                                            Selected = Main.RenderConnections,
                                                            Text = "Room connections",

                                                            SelectedBackColor = Color.White,
                                                            SelectedTextColor = Color.Black,

                                                        }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.RenderConnections = btn.Selected),

                                                        new UIButton
                                                        {
                                                            Height = 18,

                                                            Selectable = true,
                                                            Selected = Main.UseParallax,
                                                            Text = "Room parallax effect",

                                                            SelectedBackColor = Color.White,
                                                            SelectedTextColor = Color.Black,

                                                        }.OnEvent(UIElement.ClickEvent, (btn, _) => Main.UseParallax = btn.Selected)
                                                    }
                                                },
                                                new UIButton
                                                {
                                                    Height = 25,

                                                    Top = new(-25, 1),
                                                    TextAlign = new(.5f),
                                                    Text = "Render region",

                                                }.OnEvent(UIElement.ClickEvent, (_, _) =>
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
                                                        var capResult = CaptureManager.CaptureRegion(Main.Region);

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
                                                })
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }.Assign(out SidePanel)
                }
            };
            Root.Recalculate();

            Main.Instance.Window.ClientSizeChanged += (_, _) => Root.Recalculate();
        }

        public static void Update()
        {
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
