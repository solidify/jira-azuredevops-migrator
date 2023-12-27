namespace Migration.WIContract
{
    public class WiCommit
    {
        public string Id { get; set; }
        public string Repository { get; set; }

        public override string ToString()
        {
            return $"[{Repository}]{Id}";
        }
    }
}
