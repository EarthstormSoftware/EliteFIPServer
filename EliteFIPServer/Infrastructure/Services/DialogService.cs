using System.Windows;

namespace EliteFIPServer.Infrastructure.Services
{
    /// <summary>
    /// Service for displaying modern dialogs/confirmations
    /// Replaces legacy MessageBox.Show() calls
    /// </summary>
    public interface IDialogService
    {
        MessageBoxResult ShowConfirmation(string title, string message);
        void ShowInformation(string title, string message);
        void ShowError(string title, string message);
    }

    public class DialogService : IDialogService
    {
        public MessageBoxResult ShowConfirmation(string title, string message)
        {
            return MessageBox.Show(
                message,
                title,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No
            );
        }

        public void ShowInformation(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        public void ShowError(string title, string message)
        {
            MessageBox.Show(
                message,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
