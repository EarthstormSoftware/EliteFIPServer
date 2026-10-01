using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace EliteFIPServer;

public enum HelpTopic
{
    Status,
    Settings,
    MatricSettings,
    Clients,
    Activity
}

public sealed partial class HelpPane : UserControl
{
    private readonly Expander[] allSections;

    public event EventHandler<string> DashboardRequested;
    public event EventHandler WelcomeRequested;
    public event EventHandler CloseRequested;

    public HelpPane()
    {
        InitializeComponent();
        allSections = SectionHolder.Children.OfType<Expander>().ToArray();
        SectionHolder.Children.Clear();
        ShowTopic(HelpTopic.Status);
    }

    public void ShowTopic(HelpTopic topic)
    {
        (string context, Expander[] relevant) = topic switch
        {
            HelpTopic.Settings => ("Settings", new[] { SettingsSection, TabletSection, CustomPanelsSection }),
            HelpTopic.MatricSettings => ("Matric integration settings", new[] { MatricSection }),
            HelpTopic.Clients => ("Clients", new[] { ClientsSection }),
            HelpTopic.Activity => ("Activity", new[] { TroubleshootingSection }),
            _ => ("Status", new[] { GettingStartedSection, DashboardSection })
        };

        ContextText.Text = $"About the {context} screen";
        ContextSections.Children.Clear();
        OtherSections.Children.Clear();

        foreach (Expander section in relevant)
        {
            section.IsExpanded = true;
            ContextSections.Children.Add(section);
        }

        foreach (Expander section in allSections.Except(relevant))
        {
            section.IsExpanded = false;
            OtherSections.Children.Add(section);
        }

        HelpScroller.ChangeView(null, 0, null, true);
    }

    private void DashboardLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string path })
        {
            DashboardRequested?.Invoke(this, path);
        }
    }

    private void ShowWelcome_Click(object sender, RoutedEventArgs e)
    {
        WelcomeRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
