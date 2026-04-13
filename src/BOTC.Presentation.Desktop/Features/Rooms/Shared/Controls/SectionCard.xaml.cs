using System.Windows;
using System.Windows.Controls;

namespace BOTC.Presentation.Desktop.Features.Rooms.Shared.Controls;

public partial class SectionCard : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(SectionCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(SectionCard),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty HeaderActionContentProperty = DependencyProperty.Register(
        nameof(HeaderActionContent),
        typeof(object),
        typeof(SectionCard),
        new PropertyMetadata(null));

    public static readonly DependencyProperty BodyContentProperty = DependencyProperty.Register(
        nameof(BodyContent),
        typeof(object),
        typeof(SectionCard),
        new PropertyMetadata(null));

    public SectionCard()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? HeaderActionContent
    {
        get => GetValue(HeaderActionContentProperty);
        set => SetValue(HeaderActionContentProperty, value);
    }

    public object? BodyContent
    {
        get => GetValue(BodyContentProperty);
        set => SetValue(BodyContentProperty, value);
    }
}
