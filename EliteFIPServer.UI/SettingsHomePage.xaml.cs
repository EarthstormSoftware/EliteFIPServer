using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EliteFIPServer;

public sealed partial class SettingsHomePage : Page
{
    public event EventHandler MatricSettingsRequested;

    public SettingsHomePage()
    {
        InitializeComponent();
    }

    private void MatricSettings_Click(object sender, RoutedEventArgs e)
    {
        MatricSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PanelRestartInfoBar_CloseButtonClick(InfoBar sender, object e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.PanelRestartRequired = false;
        }
    }
}
