using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RainMap
{
    public static class GameAssets
    {
        public static TextureAsset LightMask0 = new("");

        static Dictionary<string, TextureAsset> Assets = new();

        internal static void LoadContent(ContentManager content)
        {
            LightMask0.Load(content.Load<Texture2D>("LightMask0"));
        }

        //internal static void LoadAssets(string assetsPath)
        //{
        //    Assets.Clear();

        //    foreach (FieldInfo field in typeof(GameAssets).GetFields())
        //    {
        //        if (!field.IsStatic || field.FieldType != typeof(TextureAsset))
        //            continue;

        //        TextureAsset asset = (TextureAsset)field.GetValue(null)!;
        //        Assets[field.Name] = asset;
        //    }

        //    ThreadPool.QueueUserWorkItem((_) =>
        //    {
        //        foreach (string file in Directory.EnumerateFiles(assetsPath, "*.png"))
        //        {
        //            string name = Path.GetFileNameWithoutExtension(file);
        //            if (Assets.TryGetValue(name, out TextureAsset? asset) && !asset.Loaded)
        //            {
        //                asset.Texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, file);
        //                asset.CompletionAction?.Invoke();
        //            }

        //            string atlasData = Path.ChangeExtension(file, ".txt");
        //            if (File.Exists(atlasData))
        //            {
        //                JsonNode json;
        //                using (FileStream fs = File.OpenRead(atlasData))
        //                {
        //                    json = JsonNode.Parse(fs)!;
        //                }

        //                Image<Rgba32>? atlas = null;

        //                if (json["frames"] is JsonObject frames)
        //                    foreach (var (assetName, assetFrameData) in frames)
        //                        if (assetName is not null && assetFrameData is not null && Assets.TryGetValue(Path.ChangeExtension(assetName, null), out asset) && !asset.Loaded)
        //                        {
        //                            try
        //                            {
        //                                JsonObject? frame = assetFrameData["frame"] as JsonObject;

        //                                if (frame is null)
        //                                    continue;

        //                                int x = (int)frame["x"]!;
        //                                int y = (int)frame["y"]!;
        //                                int w = (int)frame["w"]!;
        //                                int h = (int)frame["h"]!;

        //                                if (atlas is null)
        //                                    atlas = Image.Load<Rgba32>(file);

        //                                Texture2D texture = new(Main.Instance.GraphicsDevice, w, h);
        //                                Rgba32[] colors = ArrayPool<Rgba32>.Shared.Rent(w * h);

        //                                for (int i = 0; i < h; i++)
        //                                {
        //                                    Span<Rgba32> source = atlas.DangerousGetPixelRowMemory(i + y).Span.Slice(x, w);
        //                                    Span<Rgba32> destination = colors.AsSpan(i * w, w);
        //                                    source.CopyTo(destination);
        //                                }

        //                                texture.SetData(colors, 0, w * h);

        //                                asset.Texture = texture;
        //                                asset.CompletionAction?.Invoke();
        //                            }
        //                            catch { }
        //                        }
        //            }
        //        }
        //    });
        //}
    }
}
