namespace SearchIndexUpdateWebJob;

using Azure.Search.Documents.Indexes;
#pragma warning disable IDE1006 // Naming Styles

public class WeatherDto
{
    [SimpleField(IsKey = true)]
    public string id { get; set; } = string.Empty;

    [SimpleField(IsFilterable = true, IsSortable = true, IsFacetable = true)]
    public string city { get; set; } = string.Empty;

    [SimpleField]
    public double temperature { get; set; }

    [SimpleField(IsFilterable = true)]
    public DateTimeOffset enqueuedTime { get; set; }

    [SimpleField(IsFilterable = true)]
    public DateTimeOffset receivedTime { get; set; }

    [SimpleField(IsFilterable = true)]
    public DateTimeOffset uploadTime { get; set; }
}
