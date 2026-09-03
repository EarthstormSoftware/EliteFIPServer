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
}
