using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BOTC.Presentation.Desktop.Features.Entry.Shared.Controls;

public partial class WaxSealButton : UserControl
{
    public WaxSealButton()
    {
        InitializeComponent();
        
        Width = 220;
        Height = 220;
    }
    
    public static readonly DependencyProperty TopTextProperty = 
        DependencyProperty.Register(
            nameof(TopText), 
            typeof(string), 
            typeof(WaxSealButton),
            new PropertyMetadata(string.Empty));
    
    public string TopText
    {
        get => (string)GetValue(TopTextProperty);
        set => SetValue(TopTextProperty, value);
    }
    
    public static readonly DependencyProperty BottomTextProperty =
        DependencyProperty.Register(
            nameof(BottomText),
            typeof(string),
            typeof(WaxSealButton),
            new PropertyMetadata(string.Empty));

    public string BottomText
    {
        get => (string)GetValue(BottomTextProperty);
        set => SetValue(BottomTextProperty, value);
    }
    
    public static readonly DependencyProperty BackgroundImageProperty =
        DependencyProperty.Register(
            nameof(BackgroundImage),
            typeof(ImageSource),
            typeof(WaxSealButton),
            new PropertyMetadata(null));

    public ImageSource? BackgroundImage
    {
        get => (ImageSource?)GetValue(BackgroundImageProperty);
        set => SetValue(BackgroundImageProperty, value);
    }

    public static readonly DependencyProperty CenterImageProperty =
        DependencyProperty.Register(
            nameof(CenterImage),
            typeof(ImageSource),
            typeof(WaxSealButton),
            new PropertyMetadata(null));

    public ImageSource? CenterImage
    {
        get => (ImageSource?)GetValue(CenterImageProperty);
        set => SetValue(CenterImageProperty, value);
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(
            nameof(Command),
            typeof(ICommand),
            typeof(WaxSealButton),
            new PropertyMetadata(null));

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(
            nameof(CommandParameter),
            typeof(object),
            typeof(WaxSealButton),
            new PropertyMetadata(null));

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly DependencyProperty GlowBrushProperty =
        DependencyProperty.Register(
            nameof(GlowBrush),
            typeof(Brush),
            typeof(WaxSealButton),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(140, 255, 80, 80))));

    public Brush GlowBrush
    {
        get => (Brush)GetValue(GlowBrushProperty);
        set => SetValue(GlowBrushProperty, value);
    }

    public static readonly DependencyProperty ForegroundBrushProperty =
        DependencyProperty.Register(
            nameof(ForegroundBrush),
            typeof(Brush),
            typeof(WaxSealButton),
            new PropertyMetadata(Brushes.Gold));

    public Brush ForegroundBrush
    {
        get => (Brush)GetValue(ForegroundBrushProperty);
        set => SetValue(ForegroundBrushProperty, value);
    }

    public static readonly DependencyProperty BorderBrushColorProperty =
        DependencyProperty.Register(
            nameof(BorderBrushColor),
            typeof(Brush),
            typeof(WaxSealButton),
            new PropertyMetadata(Brushes.Goldenrod));

    public Brush BorderBrushColor
    {
        get => (Brush)GetValue(BorderBrushColorProperty);
        set => SetValue(BorderBrushColorProperty, value);
    }

    public static readonly DependencyProperty FallbackFillProperty =
        DependencyProperty.Register(
            nameof(FallbackFill),
            typeof(Brush),
            typeof(WaxSealButton),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(90, 20, 20))));

    public Brush FallbackFill
    {
        get => (Brush)GetValue(FallbackFillProperty);
        set => SetValue(FallbackFillProperty, value);
    }
}