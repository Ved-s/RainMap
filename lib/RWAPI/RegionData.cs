namespace RWAPI
{
    public struct RegionData 
    {
        public MultiDirectory Path { get; private set; }

        public string? DisplayName { get; private set; }
        public string? Id { get; private set; }

        public RegionData(MultiDirectory path, string id)
        {
            Path = path;
            Id = id;

            DisplayName = null;
            string? displayname = path.FindFile("displayname.txt");
            if (displayname is not null)
                DisplayName = File.ReadAllText(displayname);
        }

        public override string ToString()
        {
            return DisplayName ?? Id ?? base.ToString();
        }
    }
}