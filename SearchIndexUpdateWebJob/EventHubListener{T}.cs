namespace SearchIndexUpdateWebJob;

using System;
using System.Collections;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;

public class EventHubListener<T> : IAsyncDisposable
{
    public IObservable<T> Events => _events.AsObservable();
    public Action<T>? EventCallback = null;
    public EventHubListenerSettings<T> Settings { get; }

    private long _saveCheckPoint = 0;
    public bool SaveCheckPoint
    {
        get
        {
            /* Interlocked.Read() is only available for int64,
             * so we have to represent the bool as a long with 0's and 1's
             */
            return Interlocked.Read(ref _saveCheckPoint) == 1;
        }
        set
        {
            Interlocked.Exchange(ref _saveCheckPoint, Convert.ToInt64(value));
        }
    }

    private IDisposable? _timer = null;
    private readonly Subject<T> _events = new();
    private readonly ILogger<EventHubListener<T>> _logger;
    private EventProcessorClient? _processor;
    private DateTimeOffset _lastTimeProcessErrorHandlerLogged = DateTimeOffset.Now.AddMinutes(-1);

    public EventHubListener(EventHubListenerSettings<T> eventHubSettings, ILogger<EventHubListener<T>> logger)
    {
        Settings = eventHubSettings;
        _logger = logger;
    }

    public async Task StartProcessing(CancellationToken cancellationToken)
    {
        var a = Settings;
        var storageUri = new Uri(a.BlobStorageCheckpointUri);
        var storage = new BlobContainerClient(storageUri, new DefaultAzureCredential());
        await storage.CreateIfNotExistsAsync();

        // Create an event processor client to process events in the event hub
        _processor = new EventProcessorClient(storage, a.EventHubConsumerGroup, a.EventHubNamespace, a.EventHubName, new DefaultAzureCredential());

        // Register handlers for processing events and handling errors
        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;

        // Start the processing
        _logger.LogInformation("StartProcessingAsync {EventHubNamespace}, {EventHubName}, {EventHubConsumerGroup} ", a.EventHubNamespace, a.EventHubName, a.EventHubConsumerGroup);
        await _processor.StartProcessingAsync(cancellationToken);

        _timer = Observable
            .Interval(a.SaveCheckPointInterval)
            .Subscribe(x => SaveCheckPoint = true);

    }

    public async Task StopProcessing(CancellationToken cancellationToken)
    {
        if (_processor == null) return;
        if (_timer != null) _timer.Dispose();
        _timer = null;

        _logger.LogInformation("StopProcessingAsync stopping...");
        await _processor.StopProcessingAsync(cancellationToken);
        _logger.LogInformation("StopProcessingAsync stopped. {EventHubNamespace}, {EventHubName}, {EventHubConsumerGroup} ", Settings.EventHubNamespace, Settings.EventHubName, Settings.EventHubConsumerGroup);

        // Register handlers for processing events and handling errors
        _processor.ProcessEventAsync -= ProcessEventHandler;
        _processor.ProcessErrorAsync -= ProcessErrorHandler;
        _processor = null;
    }

    // https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.eventhubs.processor.ieventprocessor.processerrorasync?view=azure-dotnet
    // Called when the underlying client experiences an error while receiving. EventProcessorHost will take care of recovering from the error and continuing to pump messages,
    // so no action is required from your code. This method is provided for informational purposes.
    private Task ProcessErrorHandler(ProcessErrorEventArgs arg)
    {
        if (_lastTimeProcessErrorHandlerLogged.AddMinutes(1) < DateTimeOffset.Now)
        {
            _lastTimeProcessErrorHandlerLogged = DateTimeOffset.Now;
            _logger.LogError(arg.Exception, "ProcessErrorHandler error '{Operation}'", arg.Operation);
            _events.OnError(arg.Exception);
        }

        return Task.CompletedTask;
    }

    private async Task ProcessEventHandler(ProcessEventArgs eventArgs)
    {
        var item = Settings.ConvertEventCallBack(eventArgs);
        if (item.HasValue)
        {
            if (item.Value is ICollection)
            {
                foreach (var @event in item.ToList())
                {
                    ProcessEventHandler(@event);
                }
            }
            else ProcessEventHandler(item.Value);
        }

        void ProcessEventHandler(T item)
        {
            _events.OnNext(item);
            try
            {
                if (EventCallback != default) EventCallback(item);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "EventCallback threw an exception for value {value}", item);
            }
        }

        // Even if a subscriber throws, the following code will get called
        // so the processing will keep moving forward.
        if (SaveCheckPoint || Settings.UpdateCheckpointWithEveryMessage)
        {
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
            SaveCheckPoint = false;
        }
    }

    public async ValueTask DisposeAsync() => await StopProcessing(CancellationToken.None);
}
