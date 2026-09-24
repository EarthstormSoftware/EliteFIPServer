using H.NotifyIcon;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Windows.Input;

namespace EliteFIPServer;

public sealed class TrayIconService : IDisposable
{
    private readonly TaskbarIcon taskbarIcon;
    private bool created;
    private bool disposed;

    public TrayIconService()
    {
        taskbarIcon = new TaskbarIcon
        {
            ToolTipText = "EliteFIPServer",
            Visibility = Visibility.Collapsed
        };

        string iconPath = Path.Combine(AppContext.BaseDirectory, "EliteFIPServerIcon256.ico");
        if (File.Exists(iconPath))
        {
            taskbarIcon.Icon = new System.Drawing.Icon(iconPath);
        }

        // TaskbarIcon's default ContextMenuMode (PopupMenu) renders the menu with a native
        // Win32 popup and invokes each item's Command, not its Click event.
        var openItem = new MenuFlyoutItem { Text = "Open", Command = new RelayCommand(() => OpenRequested?.Invoke(this, EventArgs.Empty)) };
        var exitItem = new MenuFlyoutItem { Text = "Exit", Command = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty)) };

        var menu = new MenuFlyout();
        menu.Items.Add(openItem);
        menu.Items.Add(exitItem);
        taskbarIcon.ContextFlyout = menu;

        taskbarIcon.DoubleClickCommand = new RelayCommand(() => OpenRequested?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler OpenRequested;
    public event EventHandler ExitRequested;

    public void Show()
    {
        if (!created)
        {
            taskbarIcon.ForceCreate(enablesEfficiencyMode: false);
            created = true;
        }

        taskbarIcon.Visibility = Visibility.Visible;
    }

    public void Hide()
    {
        taskbarIcon.Visibility = Visibility.Collapsed;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        taskbarIcon.Dispose();
    }
}

internal sealed class RelayCommand : ICommand
{
    private readonly Action execute;

    public RelayCommand(Action execute)
    {
        this.execute = execute;
    }

    // CanExecute is always true and never changes, so this command never needs to raise the event -
    // explicit no-op accessors avoid a field-backed event that's declared but never invoked.
    public event EventHandler CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object parameter) => true;

    public void Execute(object parameter) => execute();
}
