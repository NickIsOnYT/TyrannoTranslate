using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TyrannoTranslate.Helpers;

namespace TyrannoTranslate.Dialogs;

public partial class DarkMessageBoxWindow : Window
{
    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public DarkMessageBoxWindow(
        Window? owner,
        string message,
        string caption,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        WindowChromeTheme.Attach(this);
        InitializeComponent();

        if (owner != null)
            Owner = owner;
        else
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

        TitleText.Text = caption;
        MessageText.Text = message;
        SetIcon(image);
        ConfigureButtons(buttons);
    }

    private void SetIcon(MessageBoxImage image)
    {
        (IconText.Text, IconText.Foreground) = image switch
        {
            MessageBoxImage.Error or MessageBoxImage.Hand or MessageBoxImage.Stop =>
                ("×", new SolidColorBrush(Color.FromRgb(255, 107, 107))),
            MessageBoxImage.Warning or MessageBoxImage.Exclamation =>
                ("!", new SolidColorBrush(Color.FromRgb(255, 193, 7))),
            MessageBoxImage.Question =>
                ("?", new SolidColorBrush(Color.FromRgb(124, 158, 255))),
            MessageBoxImage.Information or MessageBoxImage.Asterisk =>
                ("i", new SolidColorBrush(Color.FromRgb(124, 158, 255))),
            _ => (string.Empty, Brushes.Transparent)
        };
    }

    private void ConfigureButtons(MessageBoxButton buttons)
    {
        var yesNo = buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel;
        YesButton.Visibility = yesNo ? Visibility.Visible : Visibility.Collapsed;
        NoButton.Visibility = yesNo ? Visibility.Visible : Visibility.Collapsed;
        OkButton.Visibility = yesNo ? Visibility.Collapsed : Visibility.Visible;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e) => CloseWithResult(MessageBoxResult.Yes);

    private void NoButton_Click(object sender, RoutedEventArgs e) => CloseWithResult(MessageBoxResult.No);

    private void OkButton_Click(object sender, RoutedEventArgs e) => CloseWithResult(MessageBoxResult.OK);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseWithResult(MessageBoxResult.Cancel);

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseWithResult(MessageBoxResult.Cancel);
            e.Handled = true;
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseWithResult(MessageBoxResult result)
    {
        Result = result;
        Close();
    }
}

public static class DarkMessageBox
{
    public static MessageBoxResult Show(
        Window? owner,
        string message,
        string caption,
        MessageBoxButton buttons,
        MessageBoxImage image)
    {
        var dialog = new DarkMessageBoxWindow(owner, message, caption, buttons, image);
        dialog.ShowDialog();
        return dialog.Result;
    }
}
