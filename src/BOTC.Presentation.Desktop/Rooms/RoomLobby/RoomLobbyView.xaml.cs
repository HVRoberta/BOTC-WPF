using System.Windows;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public partial class RoomLobbyView
{
    private bool _isInitialized;

    public RoomLobbyView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RoomLobbyViewModel viewModel)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                await viewModel.LoadAsync(CancellationToken.None);
            }

            await viewModel.ActivateAsync(CancellationToken.None);
        }
    }

    private async void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is RoomLobbyViewModel viewModel)
        {
            await viewModel.DeactivateAsync(CancellationToken.None);
        }
    }
}
