using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public static class TriangleDrawing
    {
        static VertexPositionColor[] Vertices = new VertexPositionColor[256];
        static int Triangles = 0;

        public static Vector2 ClampTL;
        public static Vector2 ClampBR;
        public static bool UseClamp;

        static void EnsureVertSize(int size)
        {
            if (Vertices.Length < size)
                Array.Resize(ref Vertices, (int)(Vertices.Length * 1.5));
        }

        static Vector2 Clamp(Vector2 vec)
        {
            if (!UseClamp)
                return vec;

            if (vec.X < ClampTL.X) vec.X = ClampTL.X;
            if (vec.Y < ClampTL.Y) vec.Y = ClampTL.Y;
            if (vec.X > ClampBR.X) vec.X = ClampBR.X;
            if (vec.Y > ClampBR.Y) vec.Y = ClampBR.Y;

            return vec;
        }

        public static void Clear()
        {
            UseClamp = false;
            Triangles = 0;
        }

        public static void Draw(Effect effect)
        {
            if (Triangles <= 0)
                return;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                Main.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, Vertices, 0, Triangles);
            }
        }

        public static void AddTriangle(Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            EnsureVertSize(Triangles * 3 + 3);

            int vert = Triangles * 3;
            Vertices[vert].Position = new(Clamp(a), 0);
            Vertices[vert + 1].Position = new(Clamp(b), 0);
            Vertices[vert + 2].Position = new(Clamp(c), 0);

            Vertices[vert].Color = color;
            Vertices[vert + 1].Color = color;
            Vertices[vert + 2].Color = color;

            Triangles++;
        }

        public static void AddTriangle(Vector2 a, Color colorA, Vector2 b, Color colorB, Vector2 c, Color colorC)
        {
            EnsureVertSize(Triangles * 3 + 3);

            int vert = Triangles * 3;
            Vertices[vert].Position = new(Clamp(a), 0);
            Vertices[vert + 1].Position = new(Clamp(b), 0);
            Vertices[vert + 2].Position = new(Clamp(c), 0);

            Vertices[vert].Color = colorA;
            Vertices[vert + 1].Color = colorB;
            Vertices[vert + 2].Color = colorC;

            Triangles++;
        }

        public static void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color color)
        {
            EnsureVertSize(Triangles * 3 + 6);
            AddTriangle(a, b, c, color);
            AddTriangle(c, b, d, color);
        }
    }
}
