using RainMap.PlacedObjects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap
{
    public class RoomSettings
    {
        public RoomSettings? Parent;

        public string? Template { get => template; set => template = value; }
        public int? Palette { get => Parent?.Palette ?? palette; set => palette = value; }
        public int? FadePalette { get => Parent?.FadePalette ?? fadePalette; set => fadePalette = value; }
        public float[]? FadePaletteValues { get => Parent?.FadePaletteValues ?? fadePaletteValues; set => fadePaletteValues = value; }
        public int? EffectColorA { get => Parent?.EffectColorA ?? effectColorA; set => effectColorA = value; }
        public int? EffectColorB { get => Parent?.EffectColorB ?? effectColorB; set => effectColorB = value; }
        public float? Grime { get => Parent?.Grime ?? grime; set => grime = value; }
        public float? Clouds { get => Parent?.Clouds ?? clouds; set => clouds = value; }
        public float? WaveSpeed { get => Parent?.WaveSpeed ?? waveSpeed; set => waveSpeed = value; }
        public float? WaveLength { get => Parent?.WaveLength ?? waveLength; set => waveLength = value; }
        public float? WaveAmplitude { get => Parent?.WaveAmplitude ?? waveAmplitude; set => waveAmplitude = value; }
        public float? SecondWaveLength { get => Parent?.SecondWaveLength ?? secondWaveLength; set => secondWaveLength = value; }
        public float? SecondWaveAmplitude { get => Parent?.SecondWaveAmplitude ?? secondWaveAmplitude; set => secondWaveAmplitude = value; }

        string? template;
        int? palette;
        int? fadePalette;
        float[]? fadePaletteValues;
        int? effectColorA;
        int? effectColorB;
        float? grime;
        float? clouds;
        float? waveSpeed;
        float? waveLength;
        float? waveAmplitude;
        float? secondWaveLength;
        float? secondWaveAmplitude;

        public List<PlacedObject> PlacedObjects = new();

        public static RoomSettings Load(string filePath, RoomSettings? parent = null)
        {
            RoomSettings settings = new();
            settings.Parent = parent;

            string[] lines = File.ReadAllLines(filePath);

            for (int i = 0; i < lines.Length; i++)
            {
                if (!lines[i].Contains(':'))
                    continue;

                string[] split = lines[i].Split(new char[] { ':' }, 2);

                for (int j = 0; j < split.Length; j++)
                    split[j] = split[j].Trim();

                switch (split[0])
                {
                    case "Template":
                        settings.Template = split[1];
                        break;

                    case "Palette" when int.TryParse(split[1], out int palette):
                        settings.Palette = palette;
                        break;

                    case "FadePalette":
                        string[] fp = split[1].Split(',');
                        if (int.TryParse(fp[0].Trim(), out int fadePalette))
                            settings.FadePalette = fadePalette;
                        settings.fadePaletteValues = new float[fp.Length - 1];
                        for (int j = 1; j < fp.Length; j++)
                            if (float.TryParse(fp[j].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float fade))
                                settings.fadePaletteValues[j - 1] = fade;
                        
                        break;

                    case "EffectColorA" when int.TryParse(split[1], out int effectColorA):
                        settings.EffectColorA = effectColorA;
                        break;

                    case "EffectColorB" when int.TryParse(split[1], out int effectColorB):
                        settings.EffectColorB = effectColorB;
                        break;

                    case "Grime" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture,out float grime):
                        settings.Grime = grime;
                        break;

                    case "Clouds" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float clouds):
                        settings.Clouds = clouds;
                        break;

                    case "WaveSpeed" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float waveSpeed):
                        settings.WaveSpeed = waveSpeed;
                        break;

                    case "WaveLength" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float waveLength):
                        settings.WaveLength = waveLength;
                        break;

                    case "WaveAmplitude" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float waveAmplitude):
                        settings.WaveAmplitude = waveAmplitude;
                        break;

                    case "SecondWaveLength" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float secondWaveSpeeed):
                        settings.SecondWaveLength = secondWaveSpeeed;
                        break;

                    case "SecondWaveAmplitude" when float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float secondWaveAmplitude):
                        settings.SecondWaveAmplitude = secondWaveAmplitude;
                        break;

                    case "PlacedObjects":
                        settings.PlacedObjects.Clear();
                        foreach (string podata in split[1].Split(',', StringSplitOptions.TrimEntries))
                             settings.PlacedObjects.Add(PlacedObject.LoadObject(podata));
                        break;
                }
            }

            return settings;
        }
    }
}
