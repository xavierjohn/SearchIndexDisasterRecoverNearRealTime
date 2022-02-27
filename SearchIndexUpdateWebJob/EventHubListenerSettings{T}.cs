namespace SearchIndexUpdateWebJob;

using System;
using Azure.Messaging.EventHubs.Processor;
using CSharpFunctionalExtensions;

public abstract class EventHubListenerSettings<T>
{
    public string EventHubNamespace { get; set; } = string.Empty;

    public abstract string EventHubName { get; set; }

    public abstract string EventHubConsumerGroup { get; set; }

    // When set to true, it will use a storage account local to this data center
    // Otherwise it will use a storage account that is common to all regions.
    public abstract bool RegionSpecific { get; set; }
    public string RegionSpecificBlobStorageUri { get; set; } = string.Empty;
    public string CommonBlobStorageUri { get; set; } = string.Empty;

    //https://docs.microsoft.com/en-us/rest/api/storageservices/Naming-and-Referencing-Containers--Blobs--and-Metadata
    public string BlobStorageCheckpointUri => (RegionSpecific ? RegionSpecificBlobStorageUri : CommonBlobStorageUri) + "checkpoint-" + EventHubName + "-" + EventHubConsumerGroup;

    public TimeSpan SaveCheckPointInterval { get; set; } = TimeSpan.FromMinutes(1);

    public bool UpdateCheckpointWithEveryMessage { get; set; } = false;

    public Func<ProcessEventArgs, Maybe<T>> ConvertEventCallBack { get; init; } = (e) => Maybe<T>.None;

}
