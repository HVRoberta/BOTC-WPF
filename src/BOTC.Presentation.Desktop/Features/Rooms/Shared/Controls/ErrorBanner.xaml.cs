using System.Windows;
using System.Windows.Controls;
using BOTC.Presentation.Desktop.Features.Rooms.Shared;

namespace BOTC.Presentation.Desktop.Features.Rooms.Shared.Controls;

public partial class ErrorBanner : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(ErrorBanner),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(ScreenMessageKind),
        typeof(ErrorBanner),
        new PropertyMetadata(ScreenMessageKind.None));

    public static readonly DependencyProperty ShowProperty = DependencyProperty.Register(
        nameof(Show),
        typeof(bool),
        typeof(ErrorBanner),
        new PropertyMetadata(false));

    public ErrorBanner()
    {
        InitializeComponent();
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public ScreenMessageKind Kind
    {
        get => (ScreenMessageKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    public bool Show
    {
        get => (bool)GetValue(ShowProperty);
        set => SetValue(ShowProperty, value);
    }
}
