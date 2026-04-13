namespace BOTC.Presentation.Desktop.Features.Profiles;

public class UserProfileItemViewModel
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public bool IsLastUsed { get; init; }
    public string? AvatarPath { get; init; }
}