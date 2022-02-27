using Azure.Core;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using SearchIndexUpdateWebJob;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, builder) => AddKeyVault(context, builder))
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<WeatherSearchSettings>(c =>
        {
            c.ServiceUri = new Uri(hostContext.Configuration["searchServiceSettings:ServiceUri"]);
            c.Key = hostContext.Configuration["SearchServiceKey"];
        });
        EnvironmentSettings _environmentSettings = new();
        hostContext.Configuration.Bind(nameof(EnvironmentSettings), _environmentSettings);
        services.AddSingleton<ConvertEventHubWeatherToSearchIndex>();
        services.AddSingleton<EventHubListenerSettings<WeatherDto>>((container) =>
        {
            var logger = container.GetRequiredService<ILogger<ConvertEventHubWeatherToSearchIndex>>();
            ConvertEventHubWeatherToSearchIndex processEvents = new(logger);
            WeatherEventHubSettings eventHubSettings = new(processEvents, _environmentSettings.Region);
            hostContext.Configuration.Bind("eventHubSettings", eventHubSettings);
            return eventHubSettings;
        });
        services.AddSingleton<EventHubListener<WeatherDto>>();
        services.AddSingleton<WeatherSearchRepository>();

        services.AddHostedService<WeatherEventHubWorker>();
        services.AddHostedService<WeatherSearchIndexWriterWorker>();
        services.Configure<HostOptions>(option =>
        {
            option.ShutdownTimeout = TimeSpan.FromMinutes(1);
        });
    })
    .Build();

await host.RunAsync();

static void AddKeyVault(HostBuilderContext context, IConfigurationBuilder builder)
{
    var keyVaultName = Environment.GetEnvironmentVariable("keyVaultSettings:keyVaultName");
    if (string.IsNullOrEmpty(keyVaultName)) throw new ArgumentException("Environment variable keyVaultSettings:keyVaultName is not set");
    var keyVaultUri = $"https://{keyVaultName}.vault.azure.net/";
    Console.WriteLine($"Key Vault Name {keyVaultName} : Uri {keyVaultUri}");
    if (keyVaultName != null)
    {
        TokenCredential tokenCredential;
        if (context.HostingEnvironment.IsDevelopment())
        {
            tokenCredential = new AzureCliCredential();
        }
        else
        {
            var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            Console.WriteLine($"clientId {clientId}", clientId);
            tokenCredential = new ManagedIdentityCredential(clientId);
        }

        var secretClient = new SecretClient(new Uri(keyVaultUri), tokenCredential);
        AzureKeyVaultConfigurationOptions azureKeyVaultConfigurationOptions = new()
        {
            ReloadInterval = TimeSpan.FromHours(1),
        };
        builder.AddAzureKeyVault(secretClient, azureKeyVaultConfigurationOptions);
    }
}