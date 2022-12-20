using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RainMap
{
    public static class TextureLoader
    {
        public static int QueueLength => LoadQueue.Count;

        static Queue<TextureAsset> LoadQueue = new();
        static Thread? LoadThread;
        static AutoResetEvent LoadTrigger = new(false);
        static bool Waiting = false;

        public static TextureAsset Load(string path)
        {
            if (LoadThread is null)
            {
                LoadThread = new(LoadMethod);
                LoadThread.Start();
            }

            TextureAsset asset = new(path);
            LoadQueue.Enqueue(asset);
            LoadTrigger.Set();
            return asset;
        }

        static void LoadMethod()
        {
            Waiting = true;
            LoadTrigger.WaitOne();
            Waiting = false;

            while (LoadQueue.TryDequeue(out TextureAsset? asset))
            {
                asset.Texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, asset.Path);
                asset.CompletionAction?.Invoke();
            }
        }

        public static void AwakeIfZombie()
        {
            if (Waiting && QueueLength > 0)
                LoadTrigger.Set();
        }
    }

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
