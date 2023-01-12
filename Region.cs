using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RainMap.Renderers;
using RainMap.Structures;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RainMap
{
    public class Region
    {
        public List<Room> Rooms = new();

        HashSet<string> DrawnRoomConnections = new();
        RegionProperties? Properties;

        public Color? BackgroundColor;

        public static Region Load(string path)
        {
            string regionId = Path.GetFileName(path);

            Main.Instance.Window.Title = $"Loading region {regionId}";

            Region region = new();

            string[] worldLines = File.ReadAllLines(Path.Combine(path, $"world_{regionId}.txt"));

            Dictionary<string, string?[]> connections = new();

            string roomsDir = Path.Combine(path, "Rooms");

            for (int i = 0; i < worldLines.Length; i++) 
            {
                string line = worldLines[i];

                if (line == "ROOMS")
                {
                    for (; i < worldLines.Length; i++)
                    { 
                        line = worldLines[i];
                        if (line == "END ROOMS")
                            break;

                        if (line.StartsWith("//"))
                            continue;

                        string[] split = line.Split(':', StringSplitOptions.TrimEntries);
                        if (split.Length < 1)
                            continue;

                        string roomName = split[0];

                        if (split.Length > 1)
                        {
                            string?[] roomConns = split[1].Split(',', StringSplitOptions.TrimEntries);
                            for (int j = 0; j < roomConns.Length; j++)
                                if (roomConns[j] == "DISCONNECTED")
                                    roomConns[j] = null;
                            connections[roomName] = roomConns;
                        }
                    }
                }
            }

            int roomIndex = 0;
            foreach (string roomName in connections.Keys)
            {
                string roomPath;

                if (roomName.StartsWith("GATE"))
                    roomPath = Path.Combine(path, $"../../Gates/{roomName}.txt");

                else
                    roomPath = Path.Combine(roomsDir, $"{roomName}.txt");

                if (!File.Exists(roomPath))
                    continue;

                Main.Instance.Window.Title = $"Loading room {roomName} ({roomIndex}/{connections.Count})";

                Room room = Room.Load(roomPath);
                region.Rooms.Add(room);
                roomIndex++;
            }

            List<(Room, int, int)> roomConnections = new();
            foreach (var kvp in connections)
            {
                if (!region.TryGetRoom(kvp.Key, out Room? source))
                    continue;

                roomConnections.Clear();

                for (int i = 0; i < kvp.Value.Length; i++)
                {
                    string? connection = kvp.Value[i];
                    if (connection is null)
                        continue;

                    if (!region.TryGetRoom(connection, out Room? destination)
                     || !connections.TryGetValue(connection, out string?[]? destinationConnections)) 
                        continue;

                    int index = Array.IndexOf(destinationConnections, kvp.Key);
                    if (index < 0)
                        continue;

                    roomConnections.Add((destination, i, index));
                }

                source.Connections = roomConnections.ToArray();
            }

            string properties = Path.Combine(path, "Properties.txt");
            if (File.Exists(properties))
            {
                region.Properties = RegionProperties.Load(properties);

                if (region.Properties.DefaultPalette.HasValue)
                {
                    Texture2D? palette = Palettes.GetPalette(region.Properties.DefaultPalette.Value);
                    if (palette is not null)
                        region.BackgroundColor = palette.GetPixel(0, 4);
                }
            }

            Dictionary<string, RoomSettings> templates = new();

            foreach (Room room in region.Rooms)
            {
                string roomSettingsPath = Path.Combine(roomsDir, $"{room.Name}_Settings.txt");
                RoomSettings? roomSettings = null;

                if (File.Exists(roomSettingsPath))
                {
                    RoomSettings settings = RoomSettings.Load(roomSettingsPath);
                    string? template = settings.Template;
                    if (template is null && region.Properties?.DefaultTemplate is not null)
                        template = $"{regionId}_SettingsTemplate_{region.Properties.DefaultTemplate}";

                    if (template is not null && template != "NONE")
                    {
                        if (!templates.TryGetValue(template, out RoomSettings? templateSettings))
                        {
                            string templatePath = Path.Combine(path, $"{template}.txt");
                            if (File.Exists(templatePath))
                                templateSettings = RoomSettings.Load(templatePath);
                            else templateSettings = new();
                            templates[template] = templateSettings;
                        }
                        settings.Parent = templateSettings;
                    }
                    roomSettings = settings;
                }

                room.Settings = roomSettings;
            }

            string mapFile = Path.Combine(path, $"map_{regionId}.txt");
            if (File.Exists(mapFile))
            {
                string[] mapLines = File.ReadAllLines(mapFile);

                foreach (string line in mapLines)
                {
                    if (!line.Contains(':'))
                        continue;

                    string[] split = line.Split(':', 2, StringSplitOptions.TrimEntries);
                    if (!region.TryGetRoom(split[0], out Room? room))
                        continue;

                    string[] data = split[1].Split(',', StringSplitOptions.TrimEntries);

                    if (data.Length > 3)
                    {
                        if (float.TryParse(data[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
                            room.WorldPos.X = x * 10;
                        if (float.TryParse(data[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                            room.WorldPos.Y = -y * 10;
                    }
                }
            }

            return region;
        }

        public void Update()
        {
            foreach (Room room in Rooms)
                room.Update();
        }

        public void Draw(Renderer renderer)
        {
            Main.RoomTimeLogger.ResetWatches();

            foreach (Room room in Rooms)
                room.Draw(renderer);
            
            Main.RoomTimeLogger.StartWatch(RoomDrawTime.RegionConnections);
            DrawConnections(renderer);
            Main.RoomTimeLogger.FinishWatch();
        }

        public bool TryGetRoom(string name, [NotNullWhen(true)] out Room? room)
        {
            room = Rooms.FirstOrDefault(r => r.Name == name);
            return room is not null;
        }

        public void DrawConnections(Renderer renderer)
        {
            if (!Main.RenderConnections)
                return;

            DrawnRoomConnections.Clear();

            Main.SpriteBatch.Begin();

            for (int i = 0; i < Rooms.Count; i++)
            {
                Room room = Rooms[i];
                if (room.Connections is null)
                    continue;

                foreach (var connection in room.Connections)
                {
                    if (DrawnRoomConnections.Contains(connection.Target.Name) || !room.Rendered && !connection.Target.Rendered)
                        continue;

                    Vector2 start = room.RoomExitEntrances[connection.Exit].ToVector2() * 20 + new Vector2(10) + room.WorldPos;
                    Vector2 end = connection.Target.RoomExitEntrances[connection.TargetExit].ToVector2() * 20 + new Vector2(10) + connection.Target.WorldPos;

                    float maxDist = (start - end).Length() / 2;

                    Vector2 startDir = room.GetExitDirection(connection.Exit) * Math.Min(maxDist, 1000);
                    Vector2 endDir = connection.Target.GetExitDirection(connection.TargetExit) * Math.Min(maxDist, 1000);

                    if (startDir == Vector2.Zero)
                        startDir = (end - start) / 5;

                    if (endDir == Vector2.Zero)
                        endDir = (start - end) / 5;

                    DrawConnection(start, startDir, end, endDir, renderer);
                }
                DrawnRoomConnections.Add(room.Name);
            }
            Main.SpriteBatch.End();
        }

        void DrawConnection(Vector2 start, Vector2 startDir, Vector2 end, Vector2 endDir, Renderer renderer)
        {
            renderer.DrawRect(start - new Vector2(2) / renderer.Scale, new Vector2(4) / renderer.Scale, Color.White);
            renderer.DrawRect(end - new Vector2(2) / renderer.Scale, new Vector2(4) / renderer.Scale, Color.White);

            float lineLength = 30 * renderer.Scale;
            float tFac = 1f / (100 * renderer.Scale);

            tFac = MathHelper.Clamp(tFac, 0.01f, 0.3f);

            Vector2 a = start;
            Vector2 b = start + startDir;
            Vector2 c = end + endDir;
            Vector2 d = end;

            float lengthOff = (float)Main.TimeCache.TotalGameTime.TotalSeconds % 1 / 1 * lineLength * 2;
            bool alternator = false;
            float? tLastOff = null;

            float t = 0;

            while (t < 1)
            {
                Vector2 velocity = Bezier.CalcCubicBezier1(a, b, c, d, t) * tFac;
                float bezLength = velocity.Length() * renderer.Scale;

                if (bezLength == 0)
                {
                    t += 0.01f;
                    continue;
                }

                float tOff = tFac * (lengthOff - lineLength) / bezLength;

                float lenFac = lineLength / bezLength;

                if (!tLastOff.HasValue)
                    tLastOff = tOff;

                alternator = !alternator;
                float tNext = t + tFac * lenFac;

                if (alternator)
                {
                    // HACK: write better bezier code
                    Vector2 lineA = Bezier.CalcCubicBezier0(a, b, c, d, MathHelper.Clamp(t + MathHelper.Lerp(tLastOff.Value, tOff, 0.75f), 0, 1));
                    Vector2 lineB = Bezier.CalcCubicBezier0(a, b, c, d, MathHelper.Clamp(tNext + tOff, 0, 1));

                    renderer.DrawLine(lineA, lineB, Color.White, 2);
                }
                t = tNext;
                tLastOff = tOff;
            }
        }
    }
}
