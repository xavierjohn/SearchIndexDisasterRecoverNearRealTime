namespace SearchIndexUpdateWebJob;

public class WeatherEventHubSettings : EventHubListenerSettings<WeatherDto>
{
    public WeatherEventHubSettings(ConvertEventHubWeatherToSearchIndex supportQueueMetricsToSearchIndex, string region)
    {
        EventHubConsumerGroup = $"index-worker-{region}";
        ConvertEventCallBack = supportQueueMetricsToSearchIndex.ConvertSupportQueueMetricTimeSeriesToSearchIndex;
    }

    public override string EventHubName { get; set; } = "city-temperature";
    public override string EventHubConsumerGroup { get; set; }

    public override bool RegionSpecific { get; set; } = true;
}
