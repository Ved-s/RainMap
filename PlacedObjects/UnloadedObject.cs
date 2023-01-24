using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    internal class UnloadedObject : PlacedObject
    {
        public string? Id;
        public string? Data;

        public override void LoadData(string data, ref PlacedObject? resultObject)
        {
            Data = data;
        }
    }
}
