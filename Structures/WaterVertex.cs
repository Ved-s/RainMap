using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace RainMap.Structures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct WaterVertex : IVertexType
    {
        public Vector2 Position;
        public Vector2 TextureCoord;
        public float Depth;

        public VertexDeclaration VertexDeclaration => Declaration;
        static VertexDeclaration Declaration = new(new VertexElement[]
        {
                new(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new(16, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1),
        });
    }
}
