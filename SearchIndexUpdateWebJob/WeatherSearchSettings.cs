namespace SearchIndexUpdateWebJob;

public class WeatherSearchSettings : SearchServiceSettings
{
    public override string IndexName { get; set; } = "benchmark";
}
