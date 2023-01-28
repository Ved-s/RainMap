using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RainMap
{
    public static class TextureLoader
    {
        public static int QueueLength => LoadQueue.Count;

        static Queue<TextureAsset> LoadQueue = new();
        static Thread? LoadThread;
        static AutoResetEvent LoadTrigger = new(false);
        static bool Waiting = false;
        static object Lock = new();

        public static TextureAsset Load(string path)
        {
            lock (Lock)
            {
                if (LoadThread is null)
                {
                    LoadThread = new(LoadMethod)
                    {
                        Name = "Async texture loader"
                    };
                    LoadThread.Start();
                }

                TextureAsset asset = new(path);
                LoadQueue.Enqueue(asset);
                LoadTrigger.Set();

                return asset;
            }
        }

        static void LoadMethod()
        {
            while (true)
            {
                try
                {
                    TextureAsset? asset;
                    while (true)
                    {
                        bool deq;
                        lock (Lock)
                        {
                            deq = LoadQueue.TryDequeue(out asset);
                        }
                        if (!deq)
                            break;

                        asset!.Texture = Texture2D.FromFile(Main.Instance.GraphicsDevice, asset.Path);
                        asset.CompletionAction?.Invoke();
                    }

                    Waiting = true;
                    LoadTrigger.WaitOne();
                    Waiting = false;
                }
                catch (ThreadInterruptedException)
                {
                    return;
                }
                catch (Exception) { }
            }
        }

        public static void AwakeIfZombie()
        {
            if (Waiting && QueueLength > 0)
                LoadTrigger.Set();
        }

        public static void Finish()
        {
            LoadThread?.Interrupt();
        }
    }
}
