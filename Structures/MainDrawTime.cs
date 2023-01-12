using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.Structures
{
    public enum MainDrawTime
    {
        Region,
        Selection,
        Present
    }

    public enum RoomDrawTime
    {
        RoomLevel,
        ObjectLights,
        Water,
        Tiles,
        RegionConnections
    }
}
