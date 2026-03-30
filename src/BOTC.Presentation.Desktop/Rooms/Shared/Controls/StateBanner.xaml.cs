using System.Windows;
using System.Windows.Controls;
using BOTC.Presentation.Desktop.Rooms.Shared;

namespace BOTC.Presentation.Desktop.Rooms.Shared.Controls;

public partial class StateBanner : UserControl
{
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message),
        typeof(string),
        typeof(StateBanner),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(ScreenMessageKind),
        typeof(StateBanner),
        new PropertyMetadata(ScreenMessageKind.None));

    public static readonly DependencyProperty ShowProperty = DependencyProperty.Register(
        nameof(Show),
        typeof(bool),
        typeof(StateBanner),
        new PropertyMetadata(false));

    public StateBanner()
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


