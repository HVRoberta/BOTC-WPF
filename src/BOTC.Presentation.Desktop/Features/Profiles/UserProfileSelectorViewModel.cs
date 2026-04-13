using System.Collections.ObjectModel;
using System.Windows.Input;
using BOTC.Presentation.Desktop.Features.Profiles.CreateUserProfile;
using BOTC.Presentation.Desktop.Infrastructure.Commands;
using BOTC.Presentation.Desktop.Infrastructure.Mvvm;

namespace BOTC.Presentation.Desktop.Features.Profiles;

public sealed class UserProfileSelectorViewModel: ViewModelBase
{
    private UserProfileItemViewModel? _selectedProfile;
    private bool _isCreateProfileVisible;
    private CreateUserProfileViewModel? _createProfileViewModel;

    public ObservableCollection<UserProfileItemViewModel> Profiles { get; } = new();

    public UserProfileItemViewModel? SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (_selectedProfile == value) return;
            _selectedProfile = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsCreateProfileVisible
    {
        get => _isCreateProfileVisible;
        set => SetProperty(ref _isCreateProfileVisible, value);
    }

    public CreateUserProfileViewModel? CreateProfileViewModel
    {
        get => _createProfileViewModel;
        set => SetProperty(ref _createProfileViewModel, value);
    }

    public ICommand SelectProfileCommand { get; }
    public ICommand CreateProfileCommand { get; }
    public ICommand BackCommand { get; }

    public UserProfileSelectorViewModel()
    {
        Profiles.Add(new UserProfileItemViewModel
        {
            Id = Guid.NewGuid(),
            DisplayName = "Roberta",
            Subtitle = "Last used yesterday",
            IsLastUsed = true
        });

        Profiles.Add(new UserProfileItemViewModel
        {
            Id = Guid.NewGuid(),
            DisplayName = "David"
        });

        Profiles.Add(new UserProfileItemViewModel
        {
            Id = Guid.NewGuid(),
            DisplayName = "Guest"
        });

        SelectProfileCommand = new RelayCommand(SelectProfile, CanSelectProfile);
        CreateProfileCommand = new RelayCommand(CreateProfile);
        BackCommand = new RelayCommand(GoBack);
    }

    private bool CanSelectProfile(object? parameter)
        => SelectedProfile is not null;

    private void SelectProfile(object? parameter)
    {
        if (SelectedProfile is null)
            return;

        // TODO:
        // 1. mentsd el current sessionbe
        // 2. navigálj az EntryView-re
    }

    private void CreateProfile(object? parameter)
    {
        var vm = new CreateUserProfileViewModel();

        // vm.SaveRequested += OnCreateProfileSaveRequested;
        // vm.CancelRequested += OnCreateProfileCancelRequested;

        CreateProfileViewModel = vm;
        IsCreateProfileVisible = true;
    }

    private void GoBack(object? parameter)
    {
        // TODO: ha kell visszanavigálás
    }
}