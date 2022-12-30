using System;

namespace RainMap.Structures
{
    public struct Tile
    {
        public TerrainType Terrain;
        public ShortcutType Shortcut;
        public TileAttributes Attributes;

        [Flags]
        public enum TileAttributes
        {
            None = 0,
            VerticalBeam = 1,
            HorizontalBeam = 2,
            WallBehind = 4,
            Hive = 8,
            Waterfall = 16,
            GarbageHole = 32,
            WormGrass = 64
        }

        public enum TerrainType
        {
            Air,
            Solid,
            Slope,
            Floor,
            ShortcutEntrance
        }

        public enum ShortcutType
        {
            None,
            Normal,
            RoomExit,
            CreatureHole,
            NPCTransportation,
            RegionTransportation,
        }
    }
}
