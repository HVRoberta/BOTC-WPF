using System.Windows;
using System.Windows.Controls;
using BOTC.Presentation.Desktop.Rooms.Shared;

namespace BOTC.Presentation.Desktop.Rooms.Shared.Controls;

public partial class RoomFormLayout : UserControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(RoomFormLayout),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle),
        typeof(string),
        typeof(RoomFormLayout),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty FormContentProperty = DependencyProperty.Register(
        nameof(FormContent),
        typeof(object),
        typeof(RoomFormLayout),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent),
        typeof(object),
        typeof(RoomFormLayout),
        new PropertyMetadata(null));

    public static readonly DependencyProperty MessageTextProperty = DependencyProperty.Register(
        nameof(MessageText),
        typeof(string),
        typeof(RoomFormLayout),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MessageKindProperty = DependencyProperty.Register(
        nameof(MessageKind),
        typeof(ScreenMessageKind),
        typeof(RoomFormLayout),
        new PropertyMetadata(ScreenMessageKind.None));

    public static readonly DependencyProperty HasMessageProperty = DependencyProperty.Register(
        nameof(HasMessage),
        typeof(bool),
        typeof(RoomFormLayout),
        new PropertyMetadata(false));

    public static readonly DependencyProperty BusyTextProperty = DependencyProperty.Register(
        nameof(BusyText),
        typeof(string),
        typeof(RoomFormLayout),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IsBusyProperty = DependencyProperty.Register(
        nameof(IsBusy),
        typeof(bool),
        typeof(RoomFormLayout),
        new PropertyMetadata(false));

    public RoomFormLayout()
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

    public object? FormContent
    {
        get => GetValue(FormContentProperty);
        set => SetValue(FormContentProperty, value);
    }

    public object? ActionContent
    {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    public string MessageText
    {
        get => (string)GetValue(MessageTextProperty);
        set => SetValue(MessageTextProperty, value);
    }

    public ScreenMessageKind MessageKind
    {
        get => (ScreenMessageKind)GetValue(MessageKindProperty);
        set => SetValue(MessageKindProperty, value);
    }

    public bool HasMessage
    {
        get => (bool)GetValue(HasMessageProperty);
        set => SetValue(HasMessageProperty, value);
    }

    public string BusyText
    {
        get => (string)GetValue(BusyTextProperty);
        set => SetValue(BusyTextProperty, value);
    }

    public bool IsBusy
    {
        get => (bool)GetValue(IsBusyProperty);
        set => SetValue(IsBusyProperty, value);
    }
}

