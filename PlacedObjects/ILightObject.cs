using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    public interface ILightObject
    {
        public LightData[] Lights { get; }

        record class LightData
        {
            public Vector2 RoomPos;
            public float Radius;
            public Color Color;
            public TextureAsset? Texture;
            public bool Enabled = true;

            public LightData() { }
            public LightData(Color color, TextureAsset? texture = null) 
            {
                Color = color;
                Texture = texture;
            }
        }
    }
}
