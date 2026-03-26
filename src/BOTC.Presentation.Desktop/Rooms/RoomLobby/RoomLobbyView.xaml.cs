using System.Windows;
using System.Windows.Controls;

namespace BOTC.Presentation.Desktop.Rooms.RoomLobby;

public partial class RoomLobbyView
{
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
