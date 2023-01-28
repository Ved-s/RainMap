using Microsoft.Win32;

namespace RWAPI
{
    public static class RainWorldAPI
    {
        public static string? RootDir { get; private set; }

        public static MultiDirectory? Assets { get; private set; }

        public static bool SearchRainWorld()
        {
            object? steampathobj =
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Valve\\Steam", "InstallPath", null) ??
                    Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Wow6432Node\\Valve\\Steam", "InstallPath", null);

            if (steampathobj is string steampath)
            {
                string rwpath = Path.Combine(steampath, "steamapps/common/Rain World");
                if (Directory.Exists(rwpath))
                {
                    SetRainWorldRoot(rwpath);
                    return true;
                }
            }

            return false;
        }

        public static void SetRainWorldRoot(string rootDir)
        {
            RootDir = rootDir;

            string assetsDir = Path.Combine(rootDir, "RainWorld_Data/StreamingAssets");
            if (!Directory.Exists(assetsDir))
                return;

            List<string> assets = new();
            assets.Add(assetsDir);

            string modsDir = Path.Combine(assetsDir, "mods");

            if (Directory.Exists(modsDir))
                foreach (string mod in Directory.EnumerateDirectories(modsDir))
                    assets.Add(mod);

            Assets = new(assets.ToArray());
        }

        public static IEnumerable<RegionData> EnumerateRegions()
        {
            MultiDirectory? world = Assets?.FindDirectory("world");

            if (world is null)
                yield break;

            foreach (var (name, dir) in world.EnumerateSubDirectories())
            {
                if (dir.FindFile("properties.txt") is null)
                    continue;

                yield return new(dir, name);
            }
        }
    }
}