using System.Threading.Channels;
using Microsoft.Extensions.Logging.Console;
using sisyphish.GoogleCloud.Logging;
using sisyphish.Tools;

namespace sisyphish.Extensions;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder, out Channel<Log> logChannel)
    {
        logChannel = Channel.CreateUnbounded<Log>();

        var logReader = logChannel.Reader;
        var logWriter = logChannel.Writer;

        var logProvider = new GoogleCloudLoggerProvider(logWriter);

        builder.Logging
            .ClearProviders()
            .AddProvider(logProvider)
                .AddFilter<GoogleCloudLoggerProvider>("", LogLevel.Information)
            .AddJsonConsole()
                .AddFilter<ConsoleLoggerProvider>("", LogLevel.None)
                .AddFilter<ConsoleLoggerProvider>("Microsoft.Hosting.Lifetime", LogLevel.Warning)
                .AddFilter<ConsoleLoggerProvider>("Microsoft.AspNetCore.Diagnostics", LogLevel.Warning);

        builder.Services
            .AddSingleton(logReader)
            .AddSingleton<GoogleCloudLoggingBackgroundService>();

        return builder;
    }
}
