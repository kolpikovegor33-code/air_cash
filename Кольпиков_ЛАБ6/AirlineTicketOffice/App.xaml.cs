using System;
using System.Windows;
using AirlineTicketOffice.Views;

namespace AirlineTicketOffice;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var loginWindow = new LoginWindow();
        bool? result = loginWindow.ShowDialog();

        if (result == true && loginWindow.AuthenticatedUser != null)
        {
            var mainWindow = new MainWindow(loginWindow.AuthenticatedUser, loginWindow.Db);
            MainWindow = mainWindow;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        else
        {
            Shutdown();
        }
    }
}
