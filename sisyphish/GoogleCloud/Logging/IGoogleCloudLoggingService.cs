using sisyphish.Tools;

namespace sisyphish.GoogleCloud.Logging;

public interface IGoogleCloudLoggingService
{
    Task Log(Log log);
    Task LogBatch(List<Log> batch);
}
