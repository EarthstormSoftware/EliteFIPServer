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

    private void MatricRestartInfoBar_CloseButtonClick(InfoBar sender, object e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.MatricRestartRequired = false;
        }
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ResetMatricButtonTextConfigs();
        }
    }

    private void ResetPageSwitches_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ResetMatricPageSwitchConfigs();
        }
    }

    private void AddClientProfile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.AddMatricClientProfile();
        }
    }

    private void RemoveClientProfile_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel && sender is Button button && button.DataContext is MatricClientProfileViewModel profile)
        {
            viewModel.RemoveMatricClientProfile(profile);
        }
    }
}
