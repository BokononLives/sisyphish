using System.Diagnostics.CodeAnalysis;
using sisyphish.Controllers;
using sisyphish.Discord;
using sisyphish.Filters;
using sisyphish.GoogleCloud.Authentication;
using sisyphish.GoogleCloud.CloudTasks;
using sisyphish.GoogleCloud.Firestore;
using sisyphish.GoogleCloud.Logging;
using sisyphish.Sisyphish.Processors;
using sisyphish.Sisyphish.Services;
using sisyphish.Tools;

namespace sisyphish.Extensions;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddRequiredServices(this IServiceCollection services)
    {
        return services
            .AddTransient<GoogleCloudAuthenticationHandler>()

            .AddSingleton<RequestTracker>()

            .AddScoped<DiscordFilter>()
            .AddScoped<IFisherService, FirestoreDbFisherService>()
            .AddScoped<IPromptService, FirestoreDbPromptService>()

            .AddScoped<ICommandProcessor, FishCommandProcessor>()
                .AddScoped<IFishCommandProcessor, FishCommandProcessor>()
            .AddScoped<ICommandProcessor, MessageComponentCommandProcessor>()
                .AddScoped<IMessageComponentCommandProcessor, MessageComponentCommandProcessor>()
            .AddScoped<ICommandProcessor, ResetCommandProcessor>()
                .AddScoped<IResetCommandProcessor, ResetCommandProcessor>()

            .AddScoped<HelloWorldController>()
            .AddScoped<DiscordInteractionController>()
            .AddScoped<FishController>()
            .AddScoped<EventController>()
            .AddScoped<ResetController>()

            .AddWithKeyedHttpClient<GoogleCloudFilter>(
                baseAddress: Config.GoogleCertsBaseUrl)

            .AddWithTypedHttpClient<IDiscordService, DiscordService>(
                baseAddress: Config.DiscordBaseUrl)
            .AddWithTypedHttpClient<IGoogleCloudAuthenticationService, GoogleCloudAuthenticationService>(
                baseAddress: Config.GoogleMetadataBaseUrl,
                headers: new KeyValuePair<string, string>(Config.GoogleMetadataFlavorKey, Config.GoogleMetadataFlavorValue))

            .AddWithTypedHttpClientWithCustomHandler<ICloudTasksService, CloudTasksService, GoogleCloudAuthenticationHandler>(
                baseAddress: Config.GoogleTasksBaseUrl)
            .AddWithTypedHttpClientWithCustomHandler<IFirestoreService, FirestoreService, GoogleCloudAuthenticationHandler>(
                baseAddress: Config.GoogleFirestoreBaseUrl)
            .AddWithTypedHttpClientWithCustomHandler<IGoogleCloudLoggingService, GoogleCloudLoggingService, GoogleCloudAuthenticationHandler>(
                baseAddress: Config.GoogleLoggingBaseUrl,
                enableHttpLogging: false);
    }

    private static IServiceCollection AddWithTypedHttpClient<TInterface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        string baseAddress,
        bool enableHttpLogging = true,
        params KeyValuePair<string, string>[] headers)
            where TInterface : class
            where TImplementation : class, TInterface
    {
        var httpClientBuilder = services
            .AddWithTypedHttpClientInternal<TInterface, TImplementation>(baseAddress, headers);

        if (!enableHttpLogging)
        {
            httpClientBuilder.RemoveAllLoggers();
        }

        return services;
    }

    private static IServiceCollection AddWithTypedHttpClientWithCustomHandler<TInterface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation, THandler>(
        this IServiceCollection services,
        string baseAddress,
        bool enableHttpLogging = true,
        params KeyValuePair<string, string>[] headers)
            where TInterface : class
            where TImplementation : class, TInterface
            where THandler : DelegatingHandler
    {
        var httpClientBuilder = services
            .AddWithTypedHttpClientInternal<TInterface, TImplementation>(baseAddress, headers)
            .AddHttpMessageHandler<THandler>();

        if (!enableHttpLogging)
        {
            httpClientBuilder.RemoveAllLoggers();
        }

        return services;
    }

    private static IServiceCollection AddWithKeyedHttpClient<TImplementation>(
        this IServiceCollection services,
        string baseAddress,
        bool enableHttpLogging = true,
        params KeyValuePair<string, string>[] headers)
            where TImplementation : IKeyedService
    {
        var httpClientBuilder = services
            .AddWithKeyedHttpClientInternal<TImplementation>(baseAddress, headers);

        if (!enableHttpLogging)
        {
            httpClientBuilder.RemoveAllLoggers();
        }

        return services;
    }

    private static IServiceCollection AddWithKeyedHttpClient<TImplementation, THandler>(
        this IServiceCollection services,
        string baseAddress,
        bool enableHttpLogging = true,
        params KeyValuePair<string, string>[] headers)
            where TImplementation : IKeyedService
            where THandler : DelegatingHandler
    {
        var httpClientBuilder = services
            .AddWithKeyedHttpClientInternal<TImplementation>(baseAddress, headers)
            .AddHttpMessageHandler<THandler>();

        if (!enableHttpLogging)
        {
            httpClientBuilder.RemoveAllLoggers();
        }

        return services;
    }

    private static IHttpClientBuilder AddWithTypedHttpClientInternal<TInterface, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TImplementation>(
        this IServiceCollection services,
        string baseAddress,
        params KeyValuePair<string, string>[] headers)
            where TInterface : class
            where TImplementation : class, TInterface
                => services.AddHttpClient<TInterface, TImplementation>(client => ConfigureHttpClient(client, baseAddress, headers));

    private static IHttpClientBuilder AddWithKeyedHttpClientInternal<T>(
        this IServiceCollection services,
        string baseAddress,
        params KeyValuePair<string, string>[] headers)
            where T : IKeyedService
            => services.AddHttpClient(T.KeyName, client => ConfigureHttpClient(client, baseAddress, headers));

    private static void ConfigureHttpClient(HttpClient client, string baseAddress, IEnumerable<KeyValuePair<string, string>> headers)
    {
        client.BaseAddress = new Uri(baseAddress);

        foreach (var header in headers)
        {
            client.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }
}
