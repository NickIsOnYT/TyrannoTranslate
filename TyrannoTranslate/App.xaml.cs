using System.Windows;
using TyrannoTranslate.Helpers;
using TyrannoTranslate.Services;

namespace TyrannoTranslate;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var settings = AppSettings.Load();
        ThemeManager.Apply(settings.SelectedTheme);

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }
}
