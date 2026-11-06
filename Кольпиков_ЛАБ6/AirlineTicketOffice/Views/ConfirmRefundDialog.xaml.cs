using System.Windows;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;

namespace AirlineTicketOffice.Views;

public partial class ConfirmRefundDialog : Window
{
    public bool Confirmed { get; private set; }

    public ConfirmRefundDialog(Ticket ticket, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;

        decimal fee = TicketBookingService.ServiceFee;
        decimal refund = System.Math.Max(0, ticket.Price - fee);

        HeaderQuestionTextBlock.Text = $"Вы действительно хотите оформить возврат билета № {ticket.TicketNumber}?";
        DetailsTextBlock.Text = $"Пассажир: {ticket.Passenger?.FullName ?? "-"} | Рейс: {ticket.Flight?.FlightNumber} ({ticket.Flight?.RouteDisplay}) | Место: {ticket.SeatNumber}";
        RefundCalcTextBlock.Text = $"К возврату клиенту: {refund:N2} ₽ (удержан сервисный сбор авиакомпании {fee:N2} ₽).";
        SeatReleaseTextBlock.Text = $"После подтверждения место {ticket.SeatNumber} снова станет доступным для продажи.";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = true;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Confirmed = false;
        DialogResult = false;
        Close();
    }
}
