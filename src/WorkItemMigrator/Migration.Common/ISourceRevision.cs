namespace Migration.Common
{
    public interface ISourceRevision
    {
        string OriginId { get; }
        string Type { get; }

        string GetFieldValue(string fieldName);

    }
}