using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    public class DataPearl : PlacedObject
    {
        string? Type;

        public override void LoadData(string data, ref PlacedObject? resultObject)
        {
            string[] split = data.Split('~');

            if (split.TryGet(4, out string type))
                Type = type;
        }

        public Color GetPearlColor()
        {
            return Type switch
            {
                "CC" => new Color(0.9f, 0.6f, 0.1f),
                "SI_west" => new Color(0.01f, 0.01f, 0.01f),
                "SI_top" => new Color(0.01f, 0.01f, 0.01f),
                "LF_west" => new Color(1f, 0f, 0.3f),
                "LF_bottom" => new Color(1f, 0.1f, 0.1f),
                "HI" => new Color(0.007843138f, 0.19607843f, 1f),
                "SH" => new Color(0.2f, 0f, 0.1f),
                "DS" => new Color(0f, 0.7f, 0.1f),
                "SB_filtration" => new Color(0.1f, 0.5f, 0.5f),
                "SB_ravine" => new Color(0.01f, 0.01f, 0.01f),
                "GW" => new Color(0f, 0.7f, 0.5f),
                "SL_bridge" => new Color(0.4f, 0.1f, 0.9f),
                "SL_moon" => new Color(0.9f, 0.95f, 0.2f),
                "SU" => new Color(0.5f, 0.6f, 0.9f),
                "UW" => new Color(0.4f, 0.6f, 0.4f),
                "SL_chimney" => new Color(1f, 0f, 0.55f),
                "Red_stomach" => new Color(0.6f, 1f, 0.9f),
                _ => new Color(0.7f, 0.7f, 0.7f),
            };
        }

    }

    public class UniqueDataPearl : DataPearl { }
}
