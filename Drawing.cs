using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public static class Drawing
    {
        public static Vector2 CalcCubicBezier0(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            return t * t * t * (-a + 3 * b - 3 * c + d) +
                   3 * t * t * (a - 2 * b + c) +
                   3 * t * (-a + b) +
                   a;
        }

        public static Vector2 CalcCubicBezier1(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            return 3 * t * t * (-a + 3 * b - 3 * c + d) +
                   6 * t * (a - 2 * b + c) +
                   3 * (-a + b);
        }

        public static Vector2 CalcCubicBezier2(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            return 6 * t * (-a + 3 * b - 3 * c + d) +
                   6 * (a - 2 * b + c);
        }
    }
}
