using System.Windows;

namespace AirlineTicketOffice.Views;

public partial class SuccessTicketDialog : Window
{
    public SuccessTicketDialog(string ticketNumber, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;
        TicketNumberTextBlock.Text = $"Выписан электронный билет № {ticketNumber}.";
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
