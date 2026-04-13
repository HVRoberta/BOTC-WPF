using System.Windows.Input;
using BOTC.Presentation.Desktop.Infrastructure.Commands;
using BOTC.Presentation.Desktop.Infrastructure.Mvvm;

namespace BOTC.Presentation.Desktop.Features.Profiles.CreateUserProfile;

public class CreateUserProfileViewModel : ViewModelBase
{
    private string _displayName = string.Empty;
    private string? _validationMessage;
    
    public string DisplayName
    {
        get => _displayName;
        set
        {
            if (SetProperty(ref _displayName, value))
            {
                ValidationMessage = null;
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string? ValidationMessage
    {
        get => _validationMessage;
        private set => SetProperty(ref _validationMessage, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<string>? SaveRequested;
    public event Action? CancelRequested;

    public CreateUserProfileViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => Cancel());
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(DisplayName);
    }

    private void Save()
    {
        var trimmedName = DisplayName.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ValidationMessage = "Profile name is required.";
            return;
        }

        if (trimmedName.Length < 3)
        {
            ValidationMessage = "Profile name must be at least 3 characters.";
            return;
        }

        if (trimmedName.Length > 20)
        {
            ValidationMessage = "Profile name must be 20 characters or less.";
            return;
        }

        SaveRequested?.Invoke(trimmedName);
    }

    private void Cancel()
    {
        CancelRequested?.Invoke();
    }
}