using System.Windows;
using System.Windows.Controls;

namespace BOTC.Presentation.Desktop.Rooms.Shared.Controls;

public partial class ActionBar : UserControl
{
    public static readonly DependencyProperty LeadingContentProperty = DependencyProperty.Register(
        nameof(LeadingContent),
        typeof(object),
        typeof(ActionBar),
        new PropertyMetadata(null));

    public static readonly DependencyProperty TrailingContentProperty = DependencyProperty.Register(
        nameof(TrailingContent),
        typeof(object),
        typeof(ActionBar),
        new PropertyMetadata(null));

    public ActionBar()
    {
        InitializeComponent();
    }

    public object? LeadingContent
    {
        get => GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }
}

