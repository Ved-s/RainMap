namespace RWAPI
{
    public class MultiDirectory
    {
        public string[] Directories { get; init; }

        public MultiDirectory(string[] directories)
        {
            Directories = directories;
        }

        // for example palettes/palette1.png
        public string? FindFile(string path)
        {
            foreach (string assetsDir in Directories)
            {
                string fullPath = Path.Combine(assetsDir, path);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }

        public MultiDirectory? FindDirectory(string path)
        {
            List<string> paths = new();
            foreach (string assetsDir in Directories)
            {
                string fullPath = Path.Combine(assetsDir, path);
                if (Directory.Exists(fullPath))
                    paths.Add(fullPath);
            }

            return paths.Count == 0 ? null : new(paths.ToArray());
        }

        public IEnumerable<KeyValuePair<string, MultiDirectory>> EnumerateSubDirectories()
        {
            if (Directories.Length == 0)
                yield break;

            Dictionary<string, List<string>> dirs = new();

            foreach (string path in Directories) 
            {
                foreach (string subdir in Directory.EnumerateDirectories(path))
                {
                    string name = Path.GetFileName(subdir);

                    if (!dirs.TryGetValue(name, out List<string>? subdirs))
                    {
                        subdirs = new();
                        dirs[name] = subdirs;
                    }

                    subdirs.Add(subdir);
                }
            }

            foreach (var (name, paths) in dirs)
                yield return new(name, new(paths.ToArray()));
        }
    }
}