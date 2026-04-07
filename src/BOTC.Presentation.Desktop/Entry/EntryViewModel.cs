using BOTC.Presentation.Desktop.Navigation;
using BOTC.Presentation.Desktop.Session;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BOTC.Presentation.Desktop.Entry;

public partial class EntryViewModel(
    INavigationService navigationService,
    IClientSessionService clientSessionService) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    [NotifyCanExecuteChangedFor(nameof(HostGameCommand))]
    [NotifyCanExecuteChangedFor(nameof(JoinGameCommand))]
    private string _userName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    partial void OnUserNameChanged(string value)
    {
        ValidateUserName(value);
        StatusMessage = string.Empty;
    }

    private bool CanStartFlow() => IsValidUserName(UserName);

    [RelayCommand(CanExecute = nameof(CanStartFlow))]
    private void HostGame()
    {
        if (!TryPrepareUserName(out var normalizedUserName))
        {
            return;
        }

        clientSessionService.SetDisplayName(normalizedUserName);
        StatusMessage = "Starting host flow...";
        navigationService.NavigateToCreateRoom();
    }

    [RelayCommand(CanExecute = nameof(CanStartFlow))]
    private void JoinGame()
    {
        if (!TryPrepareUserName(out var normalizedUserName))
        {
            return;
        }

        clientSessionService.SetDisplayName(normalizedUserName);
        StatusMessage = "Starting join flow...";
        navigationService.NavigateToJoinRoom();
    }

    private bool TryPrepareUserName(out string normalizedUserName)
    {
        normalizedUserName = UserName.Trim();
        if (!IsValidUserName(normalizedUserName))
        {
            ValidateUserName(normalizedUserName);
            return false;
        }

        ValidationMessage = string.Empty;
        return true;
    }

    private void ValidateUserName(string userName)
    {
        var normalizedUserName = userName.Trim();

        if (normalizedUserName.Length == 0)
        {
            ValidationMessage = "User name is required.";
            return;
        }

        if (normalizedUserName.Length < 2)
        {
            ValidationMessage = "User name must be at least 2 characters.";
            return;
        }

        ValidationMessage = string.Empty;
    }

    private static bool IsValidUserName(string userName)
    {
        return !string.IsNullOrWhiteSpace(userName) && userName.Trim().Length >= 2;
    }
}