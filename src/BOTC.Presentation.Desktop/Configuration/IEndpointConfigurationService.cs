namespace BOTC.Presentation.Desktop.Configuration;

public interface IEndpointConfigurationService
{
    ApiOptions Api { get; }

    SignalROptions SignalR { get; }

    Uri GetApiBaseUri();

    Uri GetSignalRHubUri();
}

