namespace SearchIndexUpdateWebJob;

using System.Text;
using System.Text.Json;
using Azure.Messaging.EventHubs.Processor;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

public class ConvertEventHubWeatherToSearchIndex
{
    private readonly ILogger<ConvertEventHubWeatherToSearchIndex> _logger;

    public ConvertEventHubWeatherToSearchIndex(ILogger<ConvertEventHubWeatherToSearchIndex> logger) => _logger = logger;
    public Maybe<WeatherDto> ConvertSupportQueueMetricTimeSeriesToSearchIndex(ProcessEventArgs arg)
    {
        try
        {
            return ConvertToDto(arg);
        }
        catch (Exception e)
        {
            var json = Encoding.UTF8.GetString(arg.Data.Body.ToArray());
            _logger.LogError(e, "Deserialize exception. {json}", json);
            return Maybe<WeatherDto>.None;
        }
    }

    private Maybe<WeatherDto> ConvertToDto(ProcessEventArgs arg)
    {
        var dto = JsonSerializer.Deserialize<WeatherDto>(arg.Data.Body.Span);
        if (dto == null)
        {
            var json = Encoding.UTF8.GetString(arg.Data.Body.ToArray());
            _logger.LogError("Deserialize failed. {json}", json);
            return Maybe<WeatherDto>.None;
        }
        dto.enqueuedTime = arg.Data.EnqueuedTime.ToUniversalTime();
        dto.receivedTime = DateTimeOffset.UtcNow;
        return Maybe.From(dto);
    }

}
