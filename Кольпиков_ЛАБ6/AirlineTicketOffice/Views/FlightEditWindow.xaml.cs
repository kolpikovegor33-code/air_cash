using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;

namespace AirlineTicketOffice.Views;

public partial class FlightEditWindow : Window
{
    private readonly AirlineDbContext _db;
    private readonly FlightService _flightService;
    private readonly Flight? _editingFlight;

    public bool IsSaved { get; private set; }
    public Flight? ResultFlight { get; private set; }

    public FlightEditWindow(AirlineDbContext db, Flight? flightToEdit = null, Window? owner = null)
    {
        InitializeComponent();
        if (owner != null) Owner = owner;

        _db = db;
        _flightService = new FlightService(_db);
        _editingFlight = flightToEdit;

        LoadFormData();

        if (_editingFlight != null)
        {
            HeaderTextBlock.Text = $"Редактирование рейса {_editingFlight.FlightNumber}";
            Title = $"Редактирование рейса {_editingFlight.FlightNumber}";
            PopulateFlightData(_editingFlight);
        }
        else
        {
            HeaderTextBlock.Text = "Добавление нового авиарейса в расписание";
            Title = "Добавление нового рейса";
            DepartureDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            ArrivalDatePicker.SelectedDate = DateTime.Today.AddDays(1);
        }
    }

    private void LoadFormData()
    {
        AirlineComboBox.ItemsSource = _flightService.GetAirlines();
        if (AirlineComboBox.Items.Count > 0) AirlineComboBox.SelectedIndex = 0;

        var airports = _flightService.GetAirports();
        DepartureAirportComboBox.ItemsSource = airports;
        ArrivalAirportComboBox.ItemsSource = airports.ToList();

        if (airports.Count > 1)
        {
            DepartureAirportComboBox.SelectedIndex = 0;
            ArrivalAirportComboBox.SelectedIndex = 1;
        }

        AircraftComboBox.ItemsSource = _flightService.GetAircrafts();
        if (AircraftComboBox.Items.Count > 0) AircraftComboBox.SelectedIndex = 0;
    }

    private void PopulateFlightData(Flight f)
    {
        FlightNumberTextBox.Text = f.FlightNumber;

        var airlines = AirlineComboBox.ItemsSource as System.Collections.Generic.List<Airline>;
        if (airlines != null)
        {
            AirlineComboBox.SelectedItem = airlines.FirstOrDefault(a => a.Id == f.AirlineId);
        }

        var departures = DepartureAirportComboBox.ItemsSource as System.Collections.Generic.List<Airport>;
        if (departures != null)
        {
            DepartureAirportComboBox.SelectedItem = departures.FirstOrDefault(a => a.Id == f.DepartureAirportId);
        }

        var arrivals = ArrivalAirportComboBox.ItemsSource as System.Collections.Generic.List<Airport>;
        if (arrivals != null)
        {
            ArrivalAirportComboBox.SelectedItem = arrivals.FirstOrDefault(a => a.Id == f.ArrivalAirportId);
        }

        var aircrafts = AircraftComboBox.ItemsSource as System.Collections.Generic.List<Aircraft>;
        if (aircrafts != null)
        {
            AircraftComboBox.SelectedItem = aircrafts.FirstOrDefault(a => a.Id == f.AircraftId);
        }

        DepartureDatePicker.SelectedDate = f.DepartureDateTime.Date;
        DepartureTimeTextBox.Text = f.DepartureDateTime.ToString("HH:mm");

        ArrivalDatePicker.SelectedDate = f.ArrivalDateTime.Date;
        ArrivalTimeTextBox.Text = f.ArrivalDateTime.ToString("HH:mm");

        BasePriceTextBox.Text = f.BasePrice.ToString("0");
        BusinessPriceTextBox.Text = f.BusinessPrice.ToString("0");
        FreeSeatsTextBox.Text = f.FreeSeats.ToString();

        foreach (ComboBoxItem item in StatusComboBox.Items)
        {
            if (item.Content.ToString() == f.Status)
            {
                StatusComboBox.SelectedItem = item;
                break;
            }
        }
    }

    private void AircraftComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editingFlight == null && AircraftComboBox.SelectedItem is Aircraft ac)
        {
            FreeSeatsTextBox.Text = ac.SeatCount.ToString();
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        string flightNum = FlightNumberTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(flightNum))
        {
            MessageBox.Show("Укажите номер рейса (например, SU-1124).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AirlineComboBox.SelectedItem is not Airline airline)
        {
            MessageBox.Show("Выберите авиакомпанию-перевозчика.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DepartureAirportComboBox.SelectedItem is not Airport depAirport)
        {
            MessageBox.Show("Выберите аэропорт вылета.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ArrivalAirportComboBox.SelectedItem is not Airport arrAirport)
        {
            MessageBox.Show("Выберите аэропорт прилёта.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (depAirport.Id == arrAirport.Id)
        {
            MessageBox.Show("Аэропорт вылета и аэропорт прилёта не могут совпадать.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (AircraftComboBox.SelectedItem is not Aircraft aircraft)
        {
            MessageBox.Show("Выберите тип воздушного судна.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DepartureDatePicker.SelectedDate.HasValue || !ArrivalDatePicker.SelectedDate.HasValue)
        {
            MessageBox.Show("Укажите даты вылета и прилёта.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TimeSpan.TryParse(DepartureTimeTextBox.Text.Trim(), out var depTime))
        {
            MessageBox.Show("Укажите корректное время вылета в формате ЧЧ:ММ (например, 08:30).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TimeSpan.TryParse(ArrivalTimeTextBox.Text.Trim(), out var arrTime))
        {
            MessageBox.Show("Укажите корректное время прилёта в формате ЧЧ:ММ (например, 12:15).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var depDateTime = DepartureDatePicker.SelectedDate.Value.Date.Add(depTime);
        var arrDateTime = ArrivalDatePicker.SelectedDate.Value.Date.Add(arrTime);

        if (arrDateTime <= depDateTime)
        {
            MessageBox.Show("Дата и время прилёта должны быть позже времени вылета.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(BasePriceTextBox.Text.Trim(), out var basePrice) || basePrice <= 0)
        {
            MessageBox.Show("Укажите корректную стоимость тарифа эконом (больше 0).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(BusinessPriceTextBox.Text.Trim(), out var bizPrice) || bizPrice < basePrice)
        {
            MessageBox.Show("Стоимость бизнес-класса должна быть не меньше тарифа эконом-класса.", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(FreeSeatsTextBox.Text.Trim(), out var freeSeats) || freeSeats < 0)
        {
            MessageBox.Show("Укажите корректное количество свободных мест (неотрицательное число).", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "По расписанию";

        if (_editingFlight != null)
        {
            _editingFlight.FlightNumber = flightNum;
            _editingFlight.AirlineId = airline.Id;
            _editingFlight.DepartureAirportId = depAirport.Id;
            _editingFlight.ArrivalAirportId = arrAirport.Id;
            _editingFlight.AircraftId = aircraft.Id;
            _editingFlight.DepartureDateTime = depDateTime;
            _editingFlight.ArrivalDateTime = arrDateTime;
            _editingFlight.BasePrice = basePrice;
            _editingFlight.BusinessPrice = bizPrice;
            _editingFlight.FreeSeats = freeSeats;
            _editingFlight.Status = status;

            var (success, flight, msg) = _flightService.UpdateFlight(_editingFlight);
            if (!success)
            {
                MessageBox.Show(msg, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ResultFlight = flight;
            IsSaved = true;
            MessageBox.Show(msg, "Рейс обновлен", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        else
        {
            var newFlight = new Flight
            {
                FlightNumber = flightNum,
                AirlineId = airline.Id,
                DepartureAirportId = depAirport.Id,
                ArrivalAirportId = arrAirport.Id,
                AircraftId = aircraft.Id,
                DepartureDateTime = depDateTime,
                ArrivalDateTime = arrDateTime,
                BasePrice = basePrice,
                BusinessPrice = bizPrice,
                FreeSeats = freeSeats,
                Status = status
            };

            var (success, flight, msg) = _flightService.AddFlight(newFlight);
            if (!success)
            {
                MessageBox.Show(msg, "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ResultFlight = flight;
            IsSaved = true;
            MessageBox.Show(msg, "Рейс добавлен", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
