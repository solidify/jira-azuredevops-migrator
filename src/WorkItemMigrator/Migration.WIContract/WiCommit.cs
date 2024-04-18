namespace Migration.WIContract
{
    public class WiDevelopmentLink
    {
        public string Id { get; set; }
        public string Repository { get; set; }
        public string Type { get; set; }

        public override string ToString()
        {
            return $"[{Repository}]{Id}";
        }
    }
}
