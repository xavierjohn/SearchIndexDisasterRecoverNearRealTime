namespace SearchIndexUpdateWebJob;
using System.Threading.Channels;
using System.Threading.Tasks;

public static class ChannelReaderExtensions
{
    public static async Task<List<T>> ReadMultipleAsync<T>(this ChannelReader<T> reader, int maxBatchSize, CancellationToken cancellationToken)
    {
        await reader.WaitToReadAsync(cancellationToken);

        var batch = new List<T>(maxBatchSize);
        while (batch.Count < maxBatchSize && reader.TryRead(out var message))
            if (message != null) batch.Add(message);

        return batch;
    }
}
