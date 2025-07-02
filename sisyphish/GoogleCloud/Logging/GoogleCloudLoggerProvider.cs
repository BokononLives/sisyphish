using System.Threading.Channels;
using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public class GoogleCloudLoggerProvider : ILoggerProvider
{
    private readonly Channel<Log> _logChannel;

    public GoogleCloudLoggerProvider(Channel<Log> logChannel)
    {
        _logChannel = logChannel;
    }

    public ILogger CreateLogger(string categoryName) => new GoogleCloudLogger(categoryName, _logChannel.Writer);

    public void Dispose() => _logChannel.Writer.Complete();
}
