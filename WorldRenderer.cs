using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public abstract class Renderer
    {
        public virtual Vector2 Position { get; set; } = Vector2.Zero;
        public virtual float Scale { get; set; } = 1.0f;
        public virtual Vector2 Size { get; set; } = Vector2.One;

        public virtual Matrix Transform => Matrix.Multiply(Matrix.CreateTranslation(-Position.X, -Position.Y, 0), Matrix.CreateScale(Scale));
        public virtual Matrix InverseTransform => Matrix.Multiply(Matrix.CreateScale(1/ Scale), Matrix.CreateTranslation(Position.X, Position.Y, 0));
        public virtual Matrix Projection => Matrix.CreateOrthographicOffCenter(0, Size.X, Size.Y, 0, 0, 1);

        public virtual Vector2 TransformVector(Vector2 vec)
        {
            return (vec - Position) * Scale;
        }
        public virtual Vector2 InverseTransformVector(Vector2 vec)
        {
            return vec / Scale + Position;
        }

        public abstract void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source = null, Vector2? worldSize = null, Color? color = null);

        public abstract void DrawRect(Vector2 worldPos, Vector2 size, Color? fill, Color? border = null, float thickness = 1);
        public abstract void DrawLine(Vector2 worldPosA, Vector2 worldPosB, Color color, float thickness = 1);
    }

    public abstract class ScreenRenderer : Renderer
    {
        public SpriteBatch SpriteBatch { get; }

        public override Vector2 Size => SpriteBatch.GraphicsDevice.Viewport.Bounds.Size.ToVector2();

        public ScreenRenderer(SpriteBatch spriteBatch) 
        {
            SpriteBatch = spriteBatch;
        }

        public override void DrawTexture(Texture2D texture, Vector2 worldPos, Rectangle? source, Vector2? worldSize, Color? color)
        {
            Vector2 texSize = source?.Size.ToVector2() ?? texture.Size();
            Vector2 texScale = worldSize.HasValue ? worldSize.Value / texSize : Vector2.One;

            SpriteBatch.Draw(texture, TransformVector(worldPos), source, color ?? Color.White, 0f, Vector2.Zero, texScale * Scale, SpriteEffects.None, 0);
        }

        public override void DrawRect(Vector2 worldPos, Vector2 size, Color? fill, Color? border = null, float thickness = 1)
        {
            SpriteBatch.DrawRect(TransformVector(worldPos), size * Scale, fill, border, thickness);
        }

        public override void DrawLine(Vector2 worldPosA, Vector2 worldPosB, Color color, float thickness = 1)
        {
            SpriteBatch.DrawLine(TransformVector(worldPosA), TransformVector(worldPosB), color, thickness);
        }
    }
}
