namespace BOTC.Presentation.Desktop.Configuration;

/// <summary>
/// SignalR endpoint options for the desktop application.
/// </summary>
public sealed class SignalROptions
{
    /// <summary>
    /// Gets or sets the absolute hub URL.
    /// </summary>
    public required string HubUrl { get; set; }
}

