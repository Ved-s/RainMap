using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RainMap.PlacedObjects
{
    public class LightFixture : PlacedObject, ILightObject
    {
        public Vector2 DevPanelPos;
        public int RandomSeed;

        public ILightObject.LightData[] Lights { get; set; }

        public override void LoadData(string data, ref PlacedObject? resultObject)
        {
            string[] split = data.Split('~', 4);

            resultObject = null;

            if (!split.TryGet(0, out string id))
                return;

            if (!TryCreateObject(id, out LightFixture? light))
                return;

            resultObject = light;

            resultObject.Position = Position;

            if (split.TryGet(1, out string posxstr) && float.TryParse(posxstr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posx))
                light.DevPanelPos.X = posx;

            if (split.TryGet(2, out string posystr) && float.TryParse(posystr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posy))
                light.DevPanelPos.X = posy;

            if (split.TryGet(3, out string seedstr) && int.TryParse(seedstr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int seed))
                light.RandomSeed = seed;
        }
    }
}
