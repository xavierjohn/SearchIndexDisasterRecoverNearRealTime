
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Diagnostics;
using System.Text.Json;
using WeatherStation;

double temperature = 1.0;
var batchSize = 20_000;
var eventHubNamespace = "weathereh.servicebus.windows.net";
var eventHubName = "city-temperature";
Console.WriteLine($"Connection to EventHubNamespace {eventHubNamespace} eventHubName {eventHubName}");
await using var producerClient = new EventHubProducerClient(eventHubNamespace, eventHubName, new DefaultAzureCredential());
while (true)
{
    EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
    Console.WriteLine("Setting cities temperation to " + temperature.ToString());
    var currentBatchSize = 0;

    var stopwatch = Stopwatch.StartNew();
    for (var i = 0; i < 100_000; i++)
    {
        if (currentBatchSize == batchSize)
        {
            await producerClient.SendAsync(eventBatch);
            currentBatchSize = 0;
            eventBatch.Dispose();
            eventBatch = await producerClient.CreateBatchAsync();
        }
        var cityTemp = new WeatherDto() { city = "city" + i.ToString(), temperature = temperature };
        if (eventBatch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(cityTemp))) == false) {
            Console.WriteLine("Failed to add.");
        };
        currentBatchSize++;
    }
    await producerClient.SendAsync(eventBatch);
    stopwatch.Stop();
    Console.WriteLine($"Elapsed Time to sent to Event Hub Processing {stopwatch.ElapsedMilliseconds}");

    Console.WriteLine("Enter to increase temperature. Ctrl + C to exit");
    Console.ReadLine();
    temperature += 0.5;
}
