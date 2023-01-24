using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    public class BlueToken : PlacedObject
    {
        public string? SandboxToken;

        public override void LoadData(string data, ref PlacedObject? resultObject)
        {
            string[] split = data.Split('~');

            if (split.TryGet(5, out string token))
                SandboxToken = token;
        }
    }
}
