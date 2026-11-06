using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;

namespace AirlineTicketOffice.Views;

public class SeatItemViewModel
{
    public string Code { get; set; } = string.Empty;
    public Brush Background { get; set; } = Brushes.White;
    public Brush Foreground { get; set; } = Brushes.Black;
    public Brush BorderBrush { get; set; } = (Brush)new BrushConverter().ConvertFrom("#94A3B8")!;
    public bool IsEnabled { get; set; } = true;
}

public class SeatRowViewModel
{
    public int RowNumber { get; set; }
    public SeatItemViewModel SeatA { get; set; } = new();
    public SeatItemViewModel SeatB { get; set; } = new();
    public SeatItemViewModel SeatC { get; set; } = new();
    public SeatItemViewModel SeatD { get; set; } = new();
    public SeatItemViewModel SeatE { get; set; } = new();
    public SeatItemViewModel SeatF { get; set; } = new();
}

public partial class BookingTicketWindow : Window
{
    private readonly AirlineDbContext _db;
    private readonly TicketBookingService _bookingService;
    private readonly PassengerService _passengerService;
    private readonly Flight _flight;
    private readonly User _currentUser;

    private string _selectedSeat = "12A";
    private readonly List<SeatRowViewModel> _rows = [];

    public BookingTicketWindow(Flight flight, User user, AirlineDbContext db, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;

        _flight = flight;
        _currentUser = user;
        _db = db;
        _bookingService = new TicketBookingService(_db);
        _passengerService = new PassengerService(_db);

        TitleTextBlock.Text = $"Оформление авиабилета на рейс {_flight.FlightNumber} ({_flight.DepartureAirport?.City} {_flight.DepartureAirport?.Code} → {_flight.ArrivalAirport?.City} {_flight.ArrivalAirport?.Code})";

        LoadData();
        BuildSeatsMatrix();
        UpdateCalculations();
    }

    private void LoadData()
    {
        var passengers = _passengerService.GetAllPassengers();
        PassengerComboBox.ItemsSource = passengers;
        if (passengers.Count > 0)
        {
            PassengerComboBox.SelectedIndex = 0;
        }

        var tariffs = _bookingService.GetBaggageTariffs();
        BaggageTariffComboBox.ItemsSource = tariffs;
        if (tariffs.Count > 1)
        {
            BaggageTariffComboBox.SelectedIndex = 1;
        }
    }

    private void BuildSeatsMatrix()
    {
        _rows.Clear();
        var occupied = _bookingService.GetOccupiedSeats(_flight.Id);

        if (occupied.Contains(_selectedSeat))
        {
            for (int r = 10; r <= 13; r++)
            {
                foreach (var col in new[] { "A", "B", "C", "D", "E", "F" })
                {
                    string seat = $"{r}{col}";
                    if (!occupied.Contains(seat))
                    {
                        _selectedSeat = seat;
                        break;
                    }
                }
                if (!occupied.Contains(_selectedSeat)) break;
            }
        }

        for (int r = 10; r <= 13; r++)
        {
            var row = new SeatRowViewModel
            {
                RowNumber = r,
                SeatA = CreateSeat($"{r}A", occupied),
                SeatB = CreateSeat($"{r}B", occupied),
                SeatC = CreateSeat($"{r}C", occupied),
                SeatD = CreateSeat($"{r}D", occupied),
                SeatE = CreateSeat($"{r}E", occupied),
                SeatF = CreateSeat($"{r}F", occupied)
            };
            _rows.Add(row);
        }

        SeatsMatrixItemsControl.ItemsSource = null;
        SeatsMatrixItemsControl.ItemsSource = _rows;
        UpdateSeatVisuals();
    }

    private SeatItemViewModel CreateSeat(string code, HashSet<string> occupied)
    {
        bool isOcc = occupied.Contains(code);
        return new SeatItemViewModel
        {
            Code = code,
            IsEnabled = !isOcc,
            Background = isOcc ? (Brush)new BrushConverter().ConvertFrom("#CBD5E1")! : Brushes.White,
            Foreground = isOcc ? (Brush)new BrushConverter().ConvertFrom("#64748B")! : (Brush)new BrushConverter().ConvertFrom("#1E293B")!,
            BorderBrush = isOcc ? (Brush)new BrushConverter().ConvertFrom("#CBD5E1")! : (Brush)new BrushConverter().ConvertFrom("#94A3B8")!
        };
    }

    private void UpdateSeatVisuals()
    {
        foreach (var r in _rows)
        {
            CheckAndSet(r.SeatA);
            CheckAndSet(r.SeatB);
            CheckAndSet(r.SeatC);
            CheckAndSet(r.SeatD);
            CheckAndSet(r.SeatE);
            CheckAndSet(r.SeatF);
        }

        SeatsMatrixItemsControl.Items.Refresh();
        SelectedSeatTextBlock.Text = $"Выбранное место: {_selectedSeat} (у окна, эконом-класс)";
    }

    private void CheckAndSet(SeatItemViewModel s)
    {
        if (s.Code == _selectedSeat)
        {
            s.Background = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
            s.Foreground = Brushes.White;
            s.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
        }
    }

    private void Seat_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string code)
        {
            _selectedSeat = code;
            BuildSeatsMatrix();
        }
    }

    private void Passenger_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PassengerDetailsTextBlock == null || PassengerDocPhoneTextBlock == null) return;
        if (PassengerComboBox?.SelectedItem is Passenger p)
        {
            PassengerDetailsTextBlock.Text = $"Пассажир: {p.FullName}, Дата рожд.: {p.BirthDate:dd.MM.yyyy}";
            PassengerDocPhoneTextBlock.Text = $"Документ: {p.DocumentNumber} | Тел.: {p.Phone ?? "не указан"}";
        }
    }

    private void ClassOrBaggage_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateCalculations();
    }

    private void UpdateCalculations()
    {
        if (_flight == null) return;
        bool isBusiness = ServiceClassComboBox?.SelectedIndex == 1;
        decimal basePrice = isBusiness ? _flight.BusinessPrice : _flight.BasePrice;

        decimal baggagePrice = 0;
        decimal weight = 23;
        if (BaggageTariffComboBox?.SelectedItem is BaggageTariff b)
        {
            baggagePrice = b.Price;
            weight = b.FreeWeightKg;
        }

        decimal total = basePrice + baggagePrice;

        if (BasePriceTextBlock != null) BasePriceTextBlock.Text = $"{basePrice:N2} ₽";
        if (BaggageFeeLabelTextBlock != null) BaggageFeeLabelTextBlock.Text = $"Багажный сбор ({weight:0} кг):";
        if (BaggageFeeTextBlock != null) BaggageFeeTextBlock.Text = $"{baggagePrice:N2} ₽";
        if (TotalPriceTextBlock != null) TotalPriceTextBlock.Text = $"{total:N2} ₽";
    }

    private void CreateBooking_Click(object sender, RoutedEventArgs e)
    {
        if (PassengerComboBox.SelectedItem is not Passenger p)
        {
            new ValidationMessageDialog("Пожалуйста, выберите пассажира из списка.", this).ShowDialog();
            return;
        }

        string sClass = ServiceClassComboBox.SelectedIndex == 1 ? "Бизнес" : "Эконом";
        decimal price = ServiceClassComboBox.SelectedIndex == 1 ? _flight.BusinessPrice : _flight.BasePrice;

        var (success, booking, msg) = _bookingService.CreateBooking(
            _flight.Id, p.Id, _currentUser.Id, _selectedSeat, sClass, price);

        if (success)
        {
            MessageBox.Show(msg, "Бронирование создано", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            new ValidationMessageDialog(msg, this).ShowDialog();
        }
    }

    private void IssueTicket_Click(object sender, RoutedEventArgs e)
    {
        if (PassengerComboBox.SelectedItem is not Passenger p)
        {
            new ValidationMessageDialog("Пожалуйста, выберите пассажира из списка.", this).ShowDialog();
            return;
        }

        var baggage = BaggageTariffComboBox.SelectedItem as BaggageTariff ?? _bookingService.GetBaggageTariffs().First();
        string sClass = ServiceClassComboBox.SelectedIndex == 1 ? "Бизнес" : "Эконом";
        decimal basePrice = ServiceClassComboBox.SelectedIndex == 1 ? _flight.BusinessPrice : _flight.BasePrice;
        decimal total = basePrice + baggage.Price;
        string payMethod = (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Банковская карта (Эквайринг)";

        var (success, ticket, msg) = _bookingService.PurchaseTicket(
            _flight.Id, p.Id, _currentUser.Id, null, baggage.Id,
            _selectedSeat, sClass, baggage.FreeWeightKg, baggage.Price, total, payMethod);

        if (success && ticket != null)
        {
            var dlg = new SuccessTicketDialog(ticket.TicketNumber, this);
            dlg.ShowDialog();
            DialogResult = true;
            Close();
        }
        else
        {
            new ValidationMessageDialog(msg, this).ShowDialog();
        }
    }

    private void NewPassenger_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Для регистрации нового пассажира перейдите на вкладку «Пассажиры» главного окна.", "Регистрация пассажира", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void Close_Click(object sender, MouseButtonEventArgs e) => Close();
}
