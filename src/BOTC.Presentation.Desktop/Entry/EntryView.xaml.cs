using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BOTC.Presentation.Desktop.Entry;

public partial class EntryView : UserControl
{
    public EntryView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }
    
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NameTextBox.Focus();
        Keyboard.Focus(NameTextBox);
    }
}