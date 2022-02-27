
using Azure.Identity;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using System.Text.Json;
using WeatherStation;

double temperature = 1.0;

var eventHubNamespace = "weathereh.servicebus.windows.net";
var eventHubName = "city-temperature";
Console.WriteLine($"Connection to EventHubNamespace {eventHubNamespace} eventHubName {eventHubName}");
await using var producerClient = new EventHubProducerClient(eventHubNamespace, eventHubName, new DefaultAzureCredential());
var cities = Cities.GetCities();
while (true)
{
    using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
    Console.WriteLine("Setting cities temperation to " + temperature.ToString());
    foreach (var city in cities)
    {
        var cityTemp = new WeatherDto() { city = city, temperature = temperature };
        eventBatch.TryAdd(new EventData(JsonSerializer.SerializeToUtf8Bytes(cityTemp)));
    }
    await producerClient.SendAsync(eventBatch);
    Console.WriteLine("Enter to increase temperature. Ctrl + C to exit");
    Console.ReadLine();
    temperature += 0.5;
}
