using System.Windows;

namespace AspectUtils.Desktop.Dialogs;

public partial class ConfirmDialog : AspectUtils.Desktop.Theme.ThemedWindow
{
    public ConfirmDialog(string title, string message, string confirmText, bool danger)
    {
        InitializeComponent();

        Title = title;
        DialogTitle.Text = title;
        DialogMessage.Text = message;
        ConfirmButton.Content = confirmText;

        if (danger)
        {
            ConfirmButton.Style = (Style)FindResource("DangerButton");
            ConfirmButton.Foreground = (System.Windows.Media.Brush)FindResource("DangerBrush");
        }
    }

    public static bool Show(
        Window? owner,
        string title,
        string message,
        string confirmText,
        bool danger = false)
    {
        var dialog = new ConfirmDialog(title, message, confirmText, danger);
        if (owner is { IsVisible: true })
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true;
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
