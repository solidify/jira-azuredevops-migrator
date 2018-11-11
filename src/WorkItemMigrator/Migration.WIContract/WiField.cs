namespace Migration.WIContract
{
    public class WiField
    {
        public string ReferenceName { get; set; }
        public object Value { get; set; }

        public override string ToString()
        {
            return $"[{ReferenceName}]={Value}";
        }
    }
}