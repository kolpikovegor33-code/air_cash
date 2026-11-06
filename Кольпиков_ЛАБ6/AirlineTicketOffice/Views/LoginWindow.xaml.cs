using System;
using System.Windows;
using System.Windows.Input;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;

namespace AirlineTicketOffice.Views;

public partial class LoginWindow : Window
{
    private readonly AuthService _authService;
    private readonly AirlineDbContext _db;

    public User? AuthenticatedUser { get; private set; }
    public AirlineDbContext Db => _db;

    public LoginWindow()
    {
        InitializeComponent();
        _db = new AirlineDbContext();
        _authService = new AuthService(_db);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        PasswordBox.Focus();
    }

    private void LoginTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PasswordBox.Focus();
        }
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Login_Click(sender, e);
        }
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginTextBox.Text?.Trim() ?? string.Empty;
        string password = PasswordBox.Password?.Trim() ?? string.Empty;

        var (success, user, message) = _authService.Login(login, password);

        if (!success)
        {
            MessageBox.Show(message, "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
            PasswordBox.Focus();
            PasswordBox.SelectAll();
            return;
        }

        AuthenticatedUser = user;

        try
        {
            DialogResult = true;
        }
        catch (InvalidOperationException)
        {
            var mainWindow = new MainWindow(user!, _db);
            Application.Current.MainWindow = mainWindow;
            mainWindow.Show();
            Close();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DialogResult = false;
        }
        catch (InvalidOperationException)
        {
            Close();
        }
    }
}
