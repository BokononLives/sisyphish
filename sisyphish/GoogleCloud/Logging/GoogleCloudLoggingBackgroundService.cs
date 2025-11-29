using System.Text.Json;
using System.Threading.Channels;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggingBackgroundService
{
    private const int MaxBatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(2);

    private readonly ChannelReader<Log> _logReader;
    private readonly IGoogleCloudLoggingService _logService;

    public GoogleCloudLoggingBackgroundService(ChannelReader<Log> logReader, IGoogleCloudLoggingService logService)
    {
        _logReader = logReader;
        _logService = logService;
    }

    public async Task ProcessLogs(CancellationToken cancellationToken = default)
    {
        var batch = new List<Log>(MaxBatchSize);
        var flushTimer = new PeriodicTimer(FlushInterval);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (batch.Count < MaxBatchSize && _logReader.TryRead(out var log))
                {
                    batch.Add(log);
                }

                if (batch.Count >= MaxBatchSize || (batch.Count > 0 && await flushTimer.WaitForNextTickAsync(cancellationToken)))
                {
                    await FlushBatch(batch);
                    batch.Clear();
                }

                if (batch.Count == 0)
                {
                    var log = await _logReader.ReadAsync(cancellationToken);
                    batch.Add(log);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (batch.Count > 0)
            {
                await FlushBatch(batch);
            }
        }
    }

    private async Task FlushBatch(List<Log> batch)
    {
        try
        {
            await _logService.LogBatch(batch);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to write logs to Google Cloud Logging Api - {ex.Message}");

            var errorJson = JsonSerializer.Serialize(new FallbackErrorLog { Error = ex.ToString().Replace("\r", "").Replace("\n", "\\n") }, CamelCaseJsonContext.Default.FallbackErrorLog);
            Console.Error.WriteLine(errorJson);

            foreach (var log in batch)
            {
                Console.WriteLine(log.Text);
            }
        }
    }
}
