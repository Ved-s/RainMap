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
        public static PlacedObject LoadObject(string data)
        {
            string[] split = data.Split("><", 4);

            PlacedObject @object;
            if (!split.TryGet(0, out string id) || !TryCreateObject(id, out @object!))
                @object = new UnloadedObject { Id = id };

            if (split.TryGet(1, out string posxstr) && float.TryParse(posxstr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posx))
                @object.Position.X = posx;

            if (split.TryGet(2, out string posystr) && float.TryParse(posystr, NumberStyles.Float, CultureInfo.InvariantCulture, out float posy))
                @object.Position.Y = posy;

            if (split.TryGet(3, out string extra))
            {
                PlacedObject? result = @object;
                @object.LoadData(extra, ref result);
                if (result is null)
                    result = new UnloadedObject { Data = extra, Id = id };
                
                @object = result;
            }

            @object.Initialize();

            return @object;
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
