using System.Threading.Channels;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggerProvider : ILoggerProvider
{
    private readonly ChannelWriter<Log> _logWriter;

    public GoogleCloudLoggerProvider(ChannelWriter<Log> logWriter)
    {
        _logWriter = logWriter;
    }

    public ILogger CreateLogger(string categoryName) => new GoogleCloudLogger(categoryName, _logWriter);

    public void Dispose() => _logWriter.Complete();
}
