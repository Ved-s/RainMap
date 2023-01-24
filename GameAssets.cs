using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace RainMap
{
    public static class GameAssets
    {
        public static TextureAsset LightMask0 = new("");
        public static TextureAsset Shortcuts = new("");
        public static TextureAsset Objects = new("");

        internal static void LoadContent(ContentManager content)
        {
            LightMask0.Load(content.Load<Texture2D>("LightMask0"));
            Shortcuts.Load(content.Load<Texture2D>("Shortcuts"));
            Objects.Load(content.Load<Texture2D>("Objects"));
        }

        public static Rectangle? GetObjectIconSource(string name)
        {
            return name switch
            {
                "BubbleGrass"       => new(0, 0, 20, 26),
                "DangleFruit"       => new(21, 0, 15, 22),
                "FirecrackerPlant"  => new(38, 0, 20, 29),
                "FlareBomb"         => new(62, 0, 14, 17),
                "FlyLure"           => new(0, 30, 22, 28),
                "Hazer"             => new(23, 23, 15, 22),
                "JellyFish"         => new(39, 30, 22, 24),
                "KarmaFlower"       => new(0, 60, 23, 23),
                "Mushroom"          => new(24, 46, 12, 19),
                "NeedleEgg"         => new(62, 18, 16, 21),
                "PuffBall"          => new(37, 55, 21, 26),
                "SlimeMold"         => new(62, 40, 24, 23),
                "SporePlant"        => new(79, 0, 23, 23),
                "VultureGrub"       => new(59, 64, 20, 17),
                "WaterNut"          => new(79, 24, 13, 13),
                "SeedCob"           => new(105, 0, 35, 38),
                "GhostSpot"         => new(87, 39, 38, 48),
                "BlueToken"         => new(126, 39, 10, 20),
                "GoldToken"         => new(126, 60, 10, 20),
                "RedToken"          => new(24, 66, 10, 20),
                "DataPearl"         => new(93, 24, 11, 10),
                "UniqueDataPearl"   => new(93, 24, 11, 10),
                
                _ => null,
            };
        }
    }
}
