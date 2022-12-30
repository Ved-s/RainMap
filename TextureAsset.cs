using Microsoft.Xna.Framework.Graphics;
using System;

namespace RainMap
{
    public class TextureAsset
    {
        public Texture2D Texture { get => AssetValue ?? Main.Pixel; internal set => AssetValue = value; }

        public bool Loaded => AssetValue is not null;
        public string Path { get; }

        Texture2D? AssetValue;
        internal Action? CompletionAction;

        public TextureAsset(string path)
        {
            Path = path;
        }

        public void OnLoaded(Action callback)
        {
            if (Loaded)
            {
                callback();
                return;
            }
            CompletionAction += callback;
        }
    }
}
