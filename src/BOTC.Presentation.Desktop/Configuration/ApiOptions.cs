namespace BOTC.Presentation.Desktop.Configuration;

/// <summary>
/// API client configuration options for the desktop application.
/// </summary>
public sealed class ApiOptions
{
    /// <summary>
    /// Gets or sets the base URL for the API server.
    /// </summary>
    public required string BaseUrl { get; set; }
}

