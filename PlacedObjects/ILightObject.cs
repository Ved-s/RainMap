using Microsoft.Xna.Framework;
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
            public bool Enabled = true;

            public LightData() { }
            public LightData(Color color) 
            {
                Color = color;
            }
        }
    }
}
