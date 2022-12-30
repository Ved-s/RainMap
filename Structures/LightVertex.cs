using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace RainMap.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LightVertex : IVertexType
    {
        public Vector2 Position;
        public Vector2 LightTextureCoord;
        public Vector2 LevelTextureCoord;
        public Color Color;

        public VertexDeclaration VertexDeclaration => Declaration;
        static VertexDeclaration Declaration = new(new VertexElement[]
        {
                new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new(16, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 1),
                new(24, VertexElementFormat.Color, VertexElementUsage.Color, 0),
        });
    }
}
