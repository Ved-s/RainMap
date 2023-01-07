using Microsoft.Xna.Framework;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMap.PlacedObjects
{
    public abstract class PlacedObject
    {
        static Dictionary<string, Type> RegisteredObjects = new();

        public Vector2 Position;

        public static void RegisterAll()
        {
            RegisteredObjects.Clear();

            foreach (Type t in typeof(PlacedObject).Assembly.GetExportedTypes())
            {
                if (t.IsInterface || t.IsAbstract)
                    continue;

                if (t.IsAssignableTo(typeof(PlacedObject)))
                    RegisteredObjects.Add(t.Name, t);
            }
        }
        public static bool TryLoadObject<T>(string data, [NotNullWhen(true)] out T? @object) where T : PlacedObject
        {
            string[] split = data.Split("><", 4);

            @object = null;
            if (!split.TryGet(0, out string id))
                return false;

            if (!TryCreateObject(id, out @object))
                return false;

            if (split.TryGet(1, out string posxstr) && float.TryParse(posxstr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posx))
                @object.Position.X = posx;

            if (split.TryGet(2, out string posystr) && float.TryParse(posystr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posy))
                @object.Position.Y = posy;

            if (split.TryGet(3, out string extra))
            {
                PlacedObject? result = @object;
                @object.LoadData(extra, ref result);
                if (result is not T)
                    return false;
                @object = (T)result;
            }

            @object?.Initialize();

            return @object is not null;
        }
        public static bool TryCreateObject<T>(string name, [NotNullWhen(true)] out T? @object) where T : PlacedObject
        {
            @object = null;

            if (!RegisteredObjects.TryGetValue(name, out Type? poType))
                return false;

            if (!poType.IsAssignableTo(typeof(T)))
                return false;

            @object = (T?)Activator.CreateInstance(poType);
            return @object is not null;
        }

        public virtual void Initialize() { }
        public virtual void Update() { }
        public virtual void LoadData(string data, ref PlacedObject? resultObject) { }
    }
}
