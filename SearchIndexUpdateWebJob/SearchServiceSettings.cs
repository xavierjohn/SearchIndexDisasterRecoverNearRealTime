namespace SearchIndexUpdateWebJob;

public abstract class SearchServiceSettings
{
    public Uri? ServiceUri { get; set; }
    public string Key { get; set; } = string.Empty;
    public abstract string IndexName { get; set; }
}
