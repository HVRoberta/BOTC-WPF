using System.Windows;
using System.Windows.Controls;

namespace BOTC.Presentation.Desktop.Rooms.Shared.Controls;

public partial class BusyIndicator : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(BusyIndicator),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy),
        typeof(bool),
        typeof(BusyIndicator),
        new PropertyMetadata(false));

    public BusyIndicator()
    {
        InitializeComponent();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }
}

