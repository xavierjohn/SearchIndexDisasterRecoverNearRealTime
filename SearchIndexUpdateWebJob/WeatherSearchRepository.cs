namespace SearchIndexUpdateWebJob;

using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Search.Documents;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class WeatherSearchRepository : IDisposable
{
    private readonly SearchClient _searchClient;
    private readonly ILogger<WeatherSearchRepository> _logger;
    private bool _disposedValue;
    public WeatherSearchSettings Settings { get; }
    private readonly MD5 _checksumGenerator;

    public WeatherSearchRepository(IOptions<WeatherSearchSettings> opSearchSettings, ILogger<WeatherSearchRepository> logger)
    {
        Settings = opSearchSettings.Value;
        var credential = new AzureKeyCredential(Settings.Key);
        var searchClient = new SearchClient(Settings.ServiceUri, Settings.IndexName, credential);
        _searchClient = searchClient;
        _logger = logger;
#pragma warning disable CA5351 // Do not use insecure cryptographic algorithm MD5.
        _checksumGenerator = MD5.Create();
#pragma warning restore CA5351 // Do not use insecure cryptographic algorithm MD5.
    }

    public async Task Store(WeatherDto[] dtos, CancellationToken cancellationToken)
    {
        try
        {
            for (var i = 0; i < dtos.Length; i++)
            {
                var dto = dtos[i];
                dto.id = GenerateId(dto);
            }
            await _searchClient.UploadDocumentsAsync(dtos);
        }
        catch (Exception ex)
        {
            StringBuilder sb = new();
            var limitNumberOfIdsInLog = 5;
            for (var i = 0; i < dtos.Length && i < limitNumberOfIdsInLog; i++)
            {
                sb.Append(dtos[i] + ",");
            }
            if (dtos.Length > limitNumberOfIdsInLog) sb.Append("...");
            _logger.LogError(ex, "Search index {indexname} store failed for ids {ids}.", _searchClient.IndexName, sb.ToString());
            throw;
        }
    }
    private string GenerateId(WeatherDto dto)
    {
        StringBuilder sb = new();
        var hashValue = _checksumGenerator.ComputeHash(Encoding.UTF8.GetBytes(dto.city));
        for (var i = 0; i < hashValue.Length; i++) sb.Append(hashValue[i].ToString("x2"));
        return sb.ToString();
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _checksumGenerator.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
