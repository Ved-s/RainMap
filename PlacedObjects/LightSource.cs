using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    public class LightSource : PlacedObject, ILightObject
    {
        public ILightObject.LightData[] Lights { get; set; } = new ILightObject.LightData[] { new() };

        public Vector2 PanelPos = new Vector2(0f, 100f);
        public Vector2 HandlePos = new Vector2(MathF.Sin(30 * 0.017453292f), MathF.Cos(30 * 0.017453292f)) * 100f;
        public float Strength = 1;
        public bool FadeWithSun = true;
        public bool Flat = false;
        public ColorType Type = ColorType.Environment;

        Color? CachedColor;

        public override void LoadData(string data, ref PlacedObject? resultObject)
        {
            string[] split = data.Split('~');

            if (split.TryGet(0, out string str) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                Strength = f;

            if (split.TryGet(1, out str) && Enum.TryParse(str, out ColorType type))
                Type = type;

            if (split.TryGet(2, out str) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                HandlePos.X = f;

            if (split.TryGet(3, out str) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                HandlePos.Y = f;

            if (split.TryGet(4, out str) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                PanelPos.X = f;

            if (split.TryGet(5, out str) && float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
                PanelPos.Y = f;

            if (split.TryGet(6, out str) && int.TryParse(str, out int i))
                FadeWithSun = i > 0;

            if (split.TryGet(7, out str) && int.TryParse(str, out i))
                Flat = i > 0;
        }

        public override void Update()
        {
            ILightObject.LightData light = Lights[0];

            if (CachedColor is null || Main.KeyboardState.IsKeyDown(Keys.F7) && Main.OldKeyboardState.IsKeyUp(Keys.F7))
            {

                switch (Type)
                {
                    case ColorType.Environment:
                        Vector2 pos = Position;
                        pos.Y = Room.CurrentRoom.Size.Y * 20 - pos.Y;
                        CachedColor = Room.CurrentRoom.PixelColorAtCoordinate(pos);
                        break;

                    case ColorType.White:
                        CachedColor = Color.White;
                        break;

                    case ColorType.EffectColor1:
                    case ColorType.EffectColor2:
                        int currentColor = (Type == ColorType.EffectColor1 ? Room.CurrentRoom.Settings?.EffectColorA : Room.CurrentRoom.Settings?.EffectColorB) ?? 0;
                        CachedColor = Main.EffectColors!.GetPixel(currentColor * 2, 0);
                        break;
                }
            }
            light.RoomPos = Position;
            light.Radius = HandlePos.Length() * 10;
            light.Color = CachedColor ?? Color.White;
            light.Color.SetAlpha(Strength);
        }

        public enum ColorType 
        {
            Environment,
            White,
            EffectColor1,
            EffectColor2
        }
    }
}
