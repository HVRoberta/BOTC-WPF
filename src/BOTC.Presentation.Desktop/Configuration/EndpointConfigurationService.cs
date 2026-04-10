using Microsoft.Extensions.Options;

namespace BOTC.Presentation.Desktop.Configuration;

public sealed class EndpointConfigurationService : IEndpointConfigurationService
{
    public EndpointConfigurationService(
        IOptions<ApiOptions> apiOptions,
        IOptions<SignalROptions> signalROptions)
    {
        ArgumentNullException.ThrowIfNull(apiOptions);
        ArgumentNullException.ThrowIfNull(signalROptions);

        Api = apiOptions.Value;
        SignalR = signalROptions.Value;

        _apiBaseUri = ParseRequiredAbsoluteUri(Api.BaseUrl, "Api:BaseUrl");
        _signalRHubUri = ParseRequiredAbsoluteUri(SignalR.HubUrl, "SignalR:HubUrl");
    }

    private readonly Uri _apiBaseUri;
    private readonly Uri _signalRHubUri;

    public ApiOptions Api { get; }

    public SignalROptions SignalR { get; }

    public Uri GetApiBaseUri() => _apiBaseUri;

    public Uri GetSignalRHubUri() => _signalRHubUri;

    private static Uri ParseRequiredAbsoluteUri(string? value, string configurationPath)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Configuration value '{configurationPath}' is required.");
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"Configuration value '{configurationPath}' must be an absolute URI. Got: '{value}'.");
        }

        return uri;
    }
}

