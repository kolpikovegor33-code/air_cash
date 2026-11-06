using System.Windows;

namespace AirlineTicketOffice.Views;

public partial class ValidationMessageDialog : Window
{
    public ValidationMessageDialog(string message, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;
        MessageTextBlock.Text = message;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
