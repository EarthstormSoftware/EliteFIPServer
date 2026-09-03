using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EliteFIPServer;

public sealed partial class MatricSettingsPage : Page
{
    public event EventHandler BackRequested;

    public MatricSettingsPage()
    {
        InitializeComponent();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ResetMatricButtonTextConfigs();
        }
    }
}
