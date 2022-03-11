namespace SearchIndexUpdateWebJob;

using Azure;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

public class WeatherSearchIndexWriterWorker : BackgroundService
{
    private readonly ILogger<WeatherSearchIndexWriterWorker> _logger;
    private readonly WeatherSearchRepository _searchRepository;
    private readonly EventHubListener<WeatherDto> _eventHub;
    private readonly WeatherSearchSettings _weatherSearchSettings;
    private readonly Channel<WeatherDto> _channel;
    private const int BufferSize = 20_000;
    private const float WarningPercentage = 80;
    private const int WarningAt = (int)(BufferSize * WarningPercentage / 100);
    private const int ThrottleAt = (int)(BufferSize * 0.70);
    private readonly TimeSpan _throttleTime;
    private CancellationToken _cancellationToken;

    public WeatherSearchIndexWriterWorker(
        ILogger<WeatherSearchIndexWriterWorker> logger,
        WeatherSearchRepository searchRepository,
        EventHubListener<WeatherDto> eventHub,
        IOptions<WeatherSearchSettings> weatherSearchSettings)
    {
        _logger = logger;
        _searchRepository = searchRepository;
        _eventHub = eventHub;
        _weatherSearchSettings = weatherSearchSettings.Value;
        var options = new BoundedChannelOptions(BufferSize)
        {
            SingleWriter = false,
            SingleReader = true
        };
        _channel = Channel.CreateBounded<WeatherDto>(options);
        _throttleTime = TimeSpan.FromSeconds(20);
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await CreateIndex(cancellationToken);
        _cancellationToken = cancellationToken;
        _eventHub.EventCallback = QueueWorkItem;
        try
        {
            await ProcessQueue(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(" _channel.Reader.ReadAllAsync canceled.");
        }
        catch (Exception e)
        {
            _logger.LogCritical(e, "ExecuteAsync. An unhandled exception was thrown.");
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }

    private async Task ProcessQueue(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = (await _channel.Reader.ReadMultipleAsync(maxBatchSize: 1000, cancellationToken)).ToArray();
            for (var i = 0; i < batch.Length; i++) batch[i].uploadTime = DateTimeOffset.UtcNow;
            await _searchRepository.Store(batch, CancellationToken.None);
        }
    }

    private void QueueWorkItem(WeatherDto item)
    {
        if (!_channel.Writer.TryWrite(item) && !_cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Queue is full. Length {length}.", _channel.Reader.Count);
            Thread.Sleep(TimeSpan.FromMinutes(1));
        }

        var length = _channel.Reader.Count;
        if (length > ThrottleAt) Thread.Sleep(_throttleTime);
        if (length > WarningAt) _logger.LogWarning($"Queue over {WarningPercentage}% full. Length {length}", length);
    }

    private async Task CreateIndex(CancellationToken cancellationToken)
    {
        var credential = new AzureKeyCredential(_weatherSearchSettings.Key);
        SearchIndexClient adminClient = new(_weatherSearchSettings.ServiceUri, credential);
        FieldBuilder fieldBuilder = new();
        var searchFields = fieldBuilder.Build(typeof(WeatherDto));

        var definition = new SearchIndex(_weatherSearchSettings.IndexName, searchFields);
        await adminClient.CreateOrUpdateIndexAsync(definition, cancellationToken: cancellationToken);
    }
}
