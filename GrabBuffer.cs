using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RainMap.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public static class GrabBuffer
    {
        public static RenderTarget2D? Target { get; private set; }

        public static Matrix Projection => Matrix.CreateOrthographicOffCenter(0, Target?.Width ?? 0, Target?.Height ?? 0, 0, 0, 1);
        public static Vector2 Translation { get; private set; } = Vector2.Zero;
        public static float Scale { get; private set; } = 1.0f;

        public static Rectangle CurrentSource { get; private set; }
        public static Vector2 CurrentSize { get; private set; }
        public static bool DrawnTo { get; private set; }
        public static Vector2 SourceScaleCorrection => CurrentSize / CurrentSource.Size.ToVector2();

        public static readonly GrabRenderer Renderer = new();

        public static void Clear()
        {
            DrawnTo = false;
            CurrentSize = Vector2.Zero;
        }

        public static void Begin(Vector2 levelSize, float scale, Vector2 translation)
        {
            Translation = translation;
            Scale = scale;

            CurrentSize = levelSize * scale;
            int width = (int)Math.Max(1, Math.Ceiling(CurrentSize.X));
            int height = (int)Math.Max(1, Math.Ceiling(CurrentSize.Y));
            CurrentSource = new(0, 0, width, height);

            if (Target is null || Target.Width < width || Target.Height < height)
            {
                Target?.Dispose();
                Target = new(Main.Instance.GraphicsDevice, width, height);
            }

            Main.Instance.GraphicsDevice.SetRenderTarget(Target);
            Main.Instance.GraphicsDevice.Clear(Color.Transparent);
        }

        public static void End()
        {
            Main.Instance.GraphicsDevice.SetRenderTarget(null);
            DrawnTo = true;
        }

        public static void ApplyToShader(Effect effect)
        {
            if (Target is null || !DrawnTo)
            {
                effect.Parameters["GrabTexture"]?.SetValue((Texture2D)null!);
                return;
            }
               
            effect.Parameters["GrabTexture"]?.SetValue(Target);
            effect.Parameters["GrabScale"]?.SetValue(CurrentSize / Target.Size());

            Main.Instance.GraphicsDevice.SamplerStates[1] = SamplerState.PointClamp;
        }

        public class GrabRenderer : ScreenRenderer
        {
            public override Vector2 Position => GrabBuffer.Translation;
            public override float Scale => GrabBuffer.Scale;

            public GrabRenderer() : base(Main.SpriteBatch)
            {
                
            }
        }
    }
}
