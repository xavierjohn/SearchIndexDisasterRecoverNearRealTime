namespace SearchIndexUpdateWebJob;

using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class WeatherEventHubWorker : BackgroundService
{
    private readonly ILogger<WeatherEventHubWorker> _logger;
    private readonly EventHubListener<WeatherDto> _eventHubMetricListener;

    public WeatherEventHubWorker(ILogger<WeatherEventHubWorker> logger, EventHubListener<WeatherDto> eventHubListener)
    {
        _logger = logger;
        _eventHubMetricListener = eventHubListener;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _eventHubMetricListener.StartProcessing(stoppingToken);
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "EventHubListener {eventhub} failed to start.", _eventHubMetricListener.Settings.EventHubName);
        }
        Activity.Current?.AddEvent(new ActivityEvent("Started"));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _eventHubMetricListener.StopProcessing(cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "EventHubListener {eventhub} failed to stop.", _eventHubMetricListener.Settings.EventHubName);
        }

        await base.StopAsync(cancellationToken);
    }
}
