using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;

namespace AirlineTicketOffice.Views;

public partial class MainWindow : Window
{
    private readonly User _currentUser;
    private readonly AirlineDbContext _db;
    private readonly FlightService _flightService;
    private readonly PassengerService _passengerService;
    private readonly TicketBookingService _bookingService;
    private readonly AnalyticsService _analyticsService;

    private Flight? _selectedFlight;
    private Passenger? _selectedPassenger;
    private Ticket? _foundRefundTicket;

    private readonly DispatcherTimer _clockTimer;
    private bool _isInitializingFilters = true;
    private bool _isFormattingPassport = false;

    public MainWindow(User user, AirlineDbContext db)
    {
        InitializeComponent();
        _currentUser = user;
        _db = db;
        _flightService = new FlightService(_db);
        _passengerService = new PassengerService(_db);
        _bookingService = new TicketBookingService(_db);
        _analyticsService = new AnalyticsService(_db);

        CurrentUserNameTextBlock.Text = $"Пользователь: {_currentUser.FullName}";
        CurrentUserRoleTextBlock.Text = $"({_currentUser.Role})";
        WindowHeaderTextBlock.Text = $"АИС «Авиакасса» — Главное рабочее место ({_currentUser.Role}: {_currentUser.FullName})";

        AdminFlightsPanel.Visibility = _currentUser.Role == "Администратор" ? Visibility.Visible : Visibility.Collapsed;

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) => ClockTextBlock.Text = $"Текущее время: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
        _clockTimer.Start();
        ClockTextBlock.Text = $"Текущее время: {DateTime.Now:dd.MM.yyyy HH:mm}";

        InitFilters();
        ApplyFlightFilters();
        LoadPassengers();
        LoadTickets();
        LoadAnalytics();
    }

    private void InitFilters()
    {
        _isInitializingFilters = true;

        var airports = _flightService.GetAirports();
        var airportNames = new List<string> { "Все аэропорты" };
        airportNames.AddRange(airports.Select(a => a.DisplayName));

        FilterFromComboBox.ItemsSource = airportNames;
        FilterFromComboBox.SelectedIndex = 0;

        FilterToComboBox.ItemsSource = airportNames.ToList();
        FilterToComboBox.SelectedIndex = 0;

        FilterDatePicker.SelectedDate = null;

        var airlines = _flightService.GetAirlines();
        var airlineNames = new List<string> { "Все авиакомпании" };
        airlineNames.AddRange(airlines.Select(a => a.Title));

        FilterAirlineComboBox.ItemsSource = airlineNames;
        FilterAirlineComboBox.SelectedIndex = 0;

        AnalyticsAirlineComboBox.ItemsSource = airlineNames.ToList();
        AnalyticsAirlineComboBox.SelectedIndex = 0;

        AnalyticsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        AnalyticsEndDatePicker.SelectedDate = DateTime.Today.AddDays(7);

        _isInitializingFilters = false;
    }

    private void NavTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            SwitchTab(tabIndex);
        }
    }

    public void SwitchTab(int index)
    {
        TabFlights.Visibility = index == 0 ? Visibility.Visible : Visibility.Collapsed;
        TabPassengers.Visibility = index == 1 ? Visibility.Visible : Visibility.Collapsed;
        TabTickets.Visibility = index == 2 ? Visibility.Visible : Visibility.Collapsed;
        TabRefund.Visibility = index == 3 ? Visibility.Visible : Visibility.Collapsed;
        TabAnalytics.Visibility = index == 4 ? Visibility.Visible : Visibility.Collapsed;

        UpdateButtonActive(NavFlightsBtn, index == 0);
        UpdateButtonActive(NavPassengersBtn, index == 1);
        UpdateButtonActive(NavTicketsBtn, index == 2);
        UpdateButtonActive(NavRefundBtn, index == 3);
        UpdateButtonActive(NavAnalyticsBtn, index == 4);

        if (index == 0) ApplyFlightFilters();
        if (index == 2) LoadTickets();
        if (index == 4) LoadAnalytics();
    }

    private void UpdateButtonActive(Button btn, bool isActive)
    {
        if (isActive)
        {
            btn.Background = (Brush)new BrushConverter().ConvertFrom("#0284C7")!;
            btn.Foreground = Brushes.White;
            btn.BorderThickness = new Thickness(0);
            btn.FontWeight = FontWeights.Bold;
        }
        else
        {
            btn.Background = (Brush)new BrushConverter().ConvertFrom("#F1F5F9")!;
            btn.Foreground = (Brush)new BrushConverter().ConvertFrom("#334155")!;
            btn.BorderBrush = (Brush)new BrushConverter().ConvertFrom("#CBD5E1")!;
            btn.BorderThickness = new Thickness(1);
            btn.FontWeight = FontWeights.SemiBold;
        }
    }

    #region --- Вкладка 1: Рейсы ---
    private void AirportFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFlightFilters();
    }

    private void AirportFilter_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFlightFilters();
    }

    private void SearchFlights_Click(object sender, RoutedEventArgs e)
    {
        ApplyFlightFilters();
    }

    private void ApplyFlightFilters()
    {
        if (_isInitializingFilters || FilterFromComboBox == null || FilterToComboBox == null) return;

        string? from = FilterFromComboBox.SelectedItem?.ToString();
        string? to = FilterToComboBox.SelectedItem?.ToString();
        DateTime? date = FilterDatePicker.SelectedDate;
        string? airline = FilterAirlineComboBox.SelectedItem?.ToString();

        var results = _flightService.SearchFlights(from, to, date, airline);
        FlightsDataGrid.ItemsSource = results;
        FoundFlightsCountTextBlock.Text = $"Доступные рейсы (найдено: {results.Count} рейса):";

        if (results.Count > 0)
        {
            FlightsDataGrid.SelectedIndex = 0;
        }
        else
        {
            _selectedFlight = null;
            SelectedFlightSummaryTextBlock.Text = "Рейсы по выбранным критериям не найдены. Выберите другие аэропорты или очистите фильтры.";
        }
    }

    private void ClearFilters_Click(object sender, MouseButtonEventArgs e)
    {
        _isInitializingFilters = true;
        FilterFromComboBox.SelectedIndex = 0;
        FilterToComboBox.SelectedIndex = 0;
        FilterDatePicker.SelectedDate = null;
        FilterAirlineComboBox.SelectedIndex = 0;
        _isInitializingFilters = false;
        ApplyFlightFilters();
    }

    private void FlightsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FlightsDataGrid.SelectedItem is Flight f)
        {
            _selectedFlight = f;
            SelectedFlightSummaryTextBlock.Text = $"Выбран рейс: {f.FlightNumber} ({f.RouteDisplay}) | Тариф эконом: {f.BasePrice:N0} ₽ | Бизнес: {f.BusinessPrice:N0} ₽";
        }
    }

    private void OpenBookingTicket_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFlight == null)
        {
            new ValidationMessageDialog("Пожалуйста, выберите рейс из таблицы.", this).ShowDialog();
            return;
        }

        var w = new BookingTicketWindow(_selectedFlight, _currentUser, _db, this);
        if (w.ShowDialog() == true)
        {
            ApplyFlightFilters();
            LoadTickets();
            LoadAnalytics();
        }
    }

    private void TariffsInfo_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFlight == null) return;
        string msg = $"Подробные сведения о тарифах для рейса {_selectedFlight.FlightNumber}:\n\n" +
                     $"• Эконом-класс: {_selectedFlight.BasePrice:N0} ₽ (включена ручная кладь до 10 кг)\n" +
                     $"• Бизнес-класс: {_selectedFlight.BusinessPrice:N0} ₽ (приоритетная посадка, увеличенное кресло)\n" +
                     $"• Сверхнормативный багаж (до 32 кг): доплата 2 500 ₽\n" +
                     $"• Воздушное судно: {_selectedFlight.Aircraft?.Model} (всего мест: {_selectedFlight.Aircraft?.SeatCount})";

        MessageBox.Show(msg, "Справка по тарифам", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AddFlight_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new FlightEditWindow(_db, null, this);
        if (dlg.ShowDialog() == true)
        {
            ApplyFlightFilters();
            LoadAnalytics();
            MessageBox.Show("Новый авиарейс успешно добавлен в расписание авиакассы.", "Рейс добавлен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void EditFlight_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFlight == null)
        {
            new ValidationMessageDialog("Пожалуйста, выберите рейс из таблицы для редактирования.", this).ShowDialog();
            return;
        }

        var dlg = new FlightEditWindow(_db, _selectedFlight, this);
        if (dlg.ShowDialog() == true)
        {
            ApplyFlightFilters();
            LoadAnalytics();
            MessageBox.Show($"Параметры рейса {_selectedFlight.FlightNumber} успешно обновлены.", "Рейс изменен", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void DeleteFlight_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFlight == null)
        {
            new ValidationMessageDialog("Пожалуйста, выберите рейс из таблицы для удаления.", this).ShowDialog();
            return;
        }

        var res = MessageBox.Show($"Вы действительно хотите удалить рейс {_selectedFlight.FlightNumber} ({_selectedFlight.RouteDisplay}) из системы?",
            "Подтверждение удаления рейса", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            var (success, msg) = _flightService.DeleteFlight(_selectedFlight.Id);
            if (success)
            {
                MessageBox.Show(msg, "Рейс удален", MessageBoxButton.OK, MessageBoxImage.Information);
                ApplyFlightFilters();
                LoadAnalytics();
            }
            else
            {
                new ValidationMessageDialog(msg, this).ShowDialog();
            }
        }
    }
    #endregion

    #region --- Вкладка 2: Пассажиры ---
    private void LoadPassengers(string? query = null)
    {
        var passengers = string.IsNullOrWhiteSpace(query)
            ? _passengerService.GetAllPassengers()
            : _passengerService.SearchPassengers(query);

        PassengersDataGrid.ItemsSource = passengers;
        TotalPassengersCountTextBlock.Text = $"Всего зарегистрировано пассажиров в базе: {passengers.Count}";

        if (passengers.Count > 0)
        {
            PassengersDataGrid.SelectedIndex = 0;
        }
        else
        {
            ClearPassengerForm();
        }
    }

    private void PassengerSearch_Click(object sender, RoutedEventArgs e)
    {
        LoadPassengers(PassengerSearchTextBox.Text);
    }

    private void PassengerSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            LoadPassengers(PassengerSearchTextBox.Text);
        }
    }

    private void PassengerReset_Click(object sender, RoutedEventArgs e)
    {
        PassengerSearchTextBox.Text = string.Empty;
        LoadPassengers();
    }

    private void PassengersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PassengersDataGrid.SelectedItem is Passenger p)
        {
            _selectedPassenger = p;
            PassLastNameTextBox.Text = p.LastName;
            PassFirstNameTextBox.Text = p.FirstName;
            PassMiddleNameTextBox.Text = p.MiddleName ?? string.Empty;
            PassBirthDatePicker.SelectedDate = p.BirthDate;
            PassDocTextBox.Text = p.DocumentNumber;
            PassPhoneTextBox.Text = p.Phone ?? string.Empty;
            PassEmailTextBox.Text = p.Email ?? string.Empty;
        }
    }

    private void PassDocTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isFormattingPassport) return;
        _isFormattingPassport = true;

        int caret = PassDocTextBox.CaretIndex;
        string raw = new string(PassDocTextBox.Text.Where(char.IsDigit).Take(10).ToArray());
        string formatted = raw;
        if (raw.Length > 4)
        {
            formatted = $"{raw.Substring(0, 4)} {raw.Substring(4)}";
        }

        PassDocTextBox.Text = formatted;
        PassDocTextBox.CaretIndex = Math.Min(formatted.Length, Math.Max(0, caret <= 4 ? caret : caret + 1));
        _isFormattingPassport = false;
    }

    private void PassengerNew_Click(object sender, RoutedEventArgs e)
    {
        _selectedPassenger = null;
        PassengersDataGrid.SelectedItem = null;
        ClearPassengerForm();
        PassLastNameTextBox.Focus();
    }

    private void ClearPassengerForm()
    {
        PassLastNameTextBox.Text = string.Empty;
        PassFirstNameTextBox.Text = string.Empty;
        PassMiddleNameTextBox.Text = string.Empty;
        PassBirthDatePicker.SelectedDate = new DateTime(1995, 1, 1);
        PassDocTextBox.Text = string.Empty;
        PassPhoneTextBox.Text = string.Empty;
        PassEmailTextBox.Text = string.Empty;
    }

    private void PassengerSave_Click(object sender, RoutedEventArgs e)
    {
        var p = _selectedPassenger ?? new Passenger();
        p.LastName = PassLastNameTextBox.Text;
        p.FirstName = PassFirstNameTextBox.Text;
        p.MiddleName = PassMiddleNameTextBox.Text;
        p.BirthDate = PassBirthDatePicker.SelectedDate ?? DateTime.Today.AddYears(-20);
        p.DocumentNumber = PassDocTextBox.Text;
        p.Phone = PassPhoneTextBox.Text;
        p.Email = PassEmailTextBox.Text;

        var (success, msg) = _passengerService.SavePassenger(p);
        if (success)
        {
            MessageBox.Show(msg, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadPassengers();
        }
        else
        {
            new ValidationMessageDialog(msg, this).ShowDialog();
        }
    }

    private void PassengerDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPassenger == null)
        {
            new ValidationMessageDialog("Пожалуйста, выберите пассажира для удаления.", this).ShowDialog();
            return;
        }

        var res = MessageBox.Show($"Удалить пассажира {_selectedPassenger.FullName} из базы данных?",
            "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            var (success, msg) = _passengerService.DeletePassenger(_selectedPassenger.Id);
            if (success)
            {
                MessageBox.Show(msg, "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadPassengers();
            }
            else
            {
                new ValidationMessageDialog(msg, this).ShowDialog();
            }
        }
    }
    #endregion

    #region --- Вкладка 3: Билеты и бронирования ---
    private void LoadTickets()
    {
        TicketsDataGrid.ItemsSource = _bookingService.GetAllTickets();
    }

    private void RefreshTickets_Click(object sender, RoutedEventArgs e)
    {
        LoadTickets();
    }
    #endregion

    #region --- Вкладка 4: Возврат билета ---
    private void RefundSearch_Click(object sender, RoutedEventArgs e)
    {
        string q = RefundSearchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            new ValidationMessageDialog("Введите номер билета для поиска.", this).ShowDialog();
            return;
        }

        var tickets = _bookingService.GetAllTickets();
        var ticket = tickets.FirstOrDefault(t => t.TicketNumber.Contains(q) || (t.Booking != null && t.Booking.BookingNumber.Contains(q)));

        if (ticket == null)
        {
            new ValidationMessageDialog($"Билет или бронь с номером «{q}» не найдены в базе данных.", this).ShowDialog();
            _foundRefundTicket = null;
            FoundTicketCardBorder.Visibility = Visibility.Collapsed;
            ProceedRefundBtn.IsEnabled = false;
            return;
        }

        _foundRefundTicket = ticket;
        FoundTicketCardBorder.Visibility = Visibility.Visible;
        ProceedRefundBtn.IsEnabled = ticket.Status == "Оплачен";

        RefundTicketTitle.Text = $"Билет № {ticket.TicketNumber}";
        RefundTicketStatusText.Text = ticket.Status;
        RefundPassengerText.Text = $"Пассажир: {ticket.Passenger?.FullName} | Документ: {ticket.Passenger?.DocumentNumber}";
        RefundFlightText.Text = $"Рейс: {ticket.Flight?.FlightNumber} ({ticket.Flight?.RouteDisplay}), Вылет: {ticket.Flight?.DepartureTimeDisplay}";
        RefundSeatPriceText.Text = $"Место: {ticket.SeatNumber} ({ticket.ClassTitle}) | Полная стоимость билета: {ticket.Price:N2} ₽";

        if (ticket.Status == "Возвращён")
        {
            RefundTicketStatusBadge.Background = (Brush)new BrushConverter().ConvertFrom("#FEE2E2")!;
            RefundTicketStatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#DC2626")!;
        }
        else
        {
            RefundTicketStatusBadge.Background = (Brush)new BrushConverter().ConvertFrom("#DCFCE7")!;
            RefundTicketStatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#15803D")!;
        }
    }

    private void ProceedRefund_Click(object sender, RoutedEventArgs e)
    {
        if (_foundRefundTicket == null) return;

        var dlg = new ConfirmRefundDialog(_foundRefundTicket, this);
        if (dlg.ShowDialog() == true && dlg.Confirmed)
        {
            var (success, refundAmount, msg) = _bookingService.RefundTicket(_foundRefundTicket.Id);
            if (success)
            {
                MessageBox.Show(msg, "Возврат средств выполнен", MessageBoxButton.OK, MessageBoxImage.Information);
                RefundSearch_Click(sender, e);
                ApplyFlightFilters();
                LoadTickets();
                LoadAnalytics();
            }
            else
            {
                new ValidationMessageDialog(msg, this).ShowDialog();
            }
        }
    }
    #endregion

    #region --- Вкладка 5: Аналитика руководителя ---
    private void AnalyticsForm_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadAnalytics();
    }

    private void AnalyticsFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadAnalytics();
    }

    private void AnalyticsFilter_DateChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadAnalytics();
    }

    private void LoadAnalytics()
    {
        if (_isInitializingFilters || AnalyticsFormComboBox == null || AnalyticsDataGrid == null) return;

        int formIndex = AnalyticsFormComboBox.SelectedIndex;
        if (formIndex < 0) formIndex = 0;

        DateTime? startDate = AnalyticsStartDatePicker?.SelectedDate;
        DateTime? endDate = AnalyticsEndDatePicker?.SelectedDate;
        string? airline = AnalyticsAirlineComboBox?.SelectedItem?.ToString();

        var (kpi, breakdown) = _analyticsService.GetAnalytics(startDate, endDate, airline);
        KpiSoldTicketsTextBlock.Text = $"{kpi.TotalSoldTickets} шт.";
        KpiRevenueTextBlock.Text = $"{kpi.TotalRevenue:N0} ₽";
        KpiLoadFactorTextBlock.Text = $"{kpi.AverageLoadFactorPercent:0.0}%";
        KpiRefundsTextBlock.Text = $"{kpi.TotalRefunds:N0} ₽";

        SetupAnalyticsColumns(formIndex);

        if (formIndex == 0)
        {
            AnalyticsDataGrid.ItemsSource = breakdown;
        }
        else if (formIndex == 1)
        {
            var loadReport = _analyticsService.GetFlightLoadReport(airline);
            AnalyticsDataGrid.ItemsSource = loadReport;
        }
        else if (formIndex == 2)
        {
            var refundsReport = _analyticsService.GetRefundsReport(startDate, endDate);
            AnalyticsDataGrid.ItemsSource = refundsReport;
        }
    }

    private void SetupAnalyticsColumns(int formIndex)
    {
        AnalyticsDataGrid.Columns.Clear();

        if (formIndex == 0)
        {
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Авиакомпания",
                Binding = new Binding("AirlineTitle"),
                Width = new DataGridLength(240),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Кол-во рейсов",
                Binding = new Binding("FlightsCount"),
                Width = new DataGridLength(120)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Продано мест",
                Binding = new Binding("SeatsSold"),
                Width = new DataGridLength(120)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Сумма тарифов",
                Binding = new Binding("TariffsSum") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(150)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Сбор за багаж",
                Binding = new Binding("BaggageSum") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(140)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Итоговая выручка",
                Binding = new Binding("TotalRevenue") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(160),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Доля продаж",
                Binding = new Binding("SalesSharePercent") { StringFormat = "{0:0.0}%" },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
        else if (formIndex == 1)
        {
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "№ Рейса",
                Binding = new Binding("FlightNumber"),
                Width = new DataGridLength(100),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Маршрут следования",
                Binding = new Binding("Route"),
                Width = new DataGridLength(220)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Воздушное судно",
                Binding = new Binding("AircraftModel"),
                Width = new DataGridLength(140)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Всего мест",
                Binding = new Binding("TotalSeats"),
                Width = new DataGridLength(100)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Занято мест",
                Binding = new Binding("SoldSeats"),
                Width = new DataGridLength(100)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Свободно мест",
                Binding = new Binding("FreeSeats"),
                Width = new DataGridLength(110)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Загрузка борта",
                Binding = new Binding("LoadFactorPercent") { StringFormat = "{0:0.0}%" },
                Width = new DataGridLength(120),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Выручка рейса",
                Binding = new Binding("Revenue") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
        else if (formIndex == 2)
        {
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "№ Билета",
                Binding = new Binding("TicketNumber"),
                Width = new DataGridLength(150),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "ФИО Пассажира",
                Binding = new Binding("PassengerName"),
                Width = new DataGridLength(200)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Рейс",
                Binding = new Binding("FlightNumber"),
                Width = new DataGridLength(90)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Дата оформления",
                Binding = new Binding("PurchaseDate") { StringFormat = "{0:dd.MM.yyyy HH:mm}" },
                Width = new DataGridLength(135)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Стоимость",
                Binding = new Binding("TicketPrice") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(110)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Сервисный сбор",
                Binding = new Binding("RetainedFee") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(120)
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Сумма возврата",
                Binding = new Binding("RefundAmount") { StringFormat = "{0:N0} ₽" },
                Width = new DataGridLength(130),
                FontWeight = FontWeights.Bold
            });
            AnalyticsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Статус операции",
                Binding = new Binding("Status"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }
    }

    private void RefreshAnalytics_Click(object sender, RoutedEventArgs e)
    {
        LoadAnalytics();
        MessageBox.Show("Аналитический отчёт успешно сформирован по актуальным данным продаж.", "Отчёт сформирован", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void KpiRefundsCard_Click(object sender, MouseButtonEventArgs e)
    {
        if (AnalyticsFormComboBox != null)
        {
            AnalyticsFormComboBox.SelectedIndex = 2;
        }
    }

    private void ExportExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int formIndex = AnalyticsFormComboBox?.SelectedIndex ?? 0;
            string reportTitle = formIndex switch
            {
                1 => "Отчёт о загрузке рейсов и пассажиропотоке",
                2 => "Реестр возвратов и аннулирований",
                _ => "Сводный финансовый отчёт по авиакомпаниям"
            };

            DateTime? startDate = AnalyticsStartDatePicker?.SelectedDate;
            DateTime? endDate = AnalyticsEndDatePicker?.SelectedDate;
            string periodText = (startDate, endDate) switch
            {
                ({ } s, { } end) => $"{s:dd.MM.yyyy} — {end:dd.MM.yyyy}",
                ({ } s, null) => $"с {s:dd.MM.yyyy}",
                (null, { } end) => $"по {end:dd.MM.yyyy}",
                _ => "За весь период"
            };

            string filterText = AnalyticsAirlineComboBox?.SelectedItem?.ToString() ?? "Все авиакомпании";
            var (kpi, breakdown) = _analyticsService.GetAnalytics(startDate, endDate, filterText);

            var headers = new List<string>();
            var rows = new List<List<object?>>();

            if (formIndex == 0)
            {
                headers.AddRange(new[] { "Авиакомпания", "Кол-во рейсов", "Продано мест", "Сумма тарифов (₽)", "Сбор за багаж (₽)", "Итоговая выручка (₽)", "Доля продаж (%)" });
                foreach (var item in breakdown)
                {
                    rows.Add(new List<object?>
                    {
                        item.AirlineTitle,
                        item.FlightsCount,
                        item.SeatsSold,
                        item.TariffsSum,
                        item.BaggageSum,
                        item.TotalRevenue,
                        item.SalesSharePercent
                    });
                }
            }
            else if (formIndex == 1)
            {
                headers.AddRange(new[] { "Номер рейса", "Маршрут", "Воздушное судно", "Всего мест", "Занято мест", "Свободно мест", "Загрузка рейса (%)", "Выручка (₽)" });
                var loadReport = _analyticsService.GetFlightLoadReport(filterText);
                foreach (var item in loadReport)
                {
                    rows.Add(new List<object?>
                    {
                        item.FlightNumber,
                        item.Route,
                        item.AircraftModel,
                        item.TotalSeats,
                        item.SoldSeats,
                        item.FreeSeats,
                        item.LoadFactorPercent,
                        item.Revenue
                    });
                }
            }
            else
            {
                headers.AddRange(new[] { "№ Билета", "ФИО Пассажира", "Рейс", "Дата оформления", "Стоимость (₽)", "Сервисный сбор (₽)", "Сумма возврата (₽)", "Статус операции" });
                var refundsReport = _analyticsService.GetRefundsReport(startDate, endDate);
                foreach (var item in refundsReport)
                {
                    rows.Add(new List<object?>
                    {
                        item.TicketNumber,
                        item.PassengerName,
                        item.FlightNumber,
                        item.PurchaseDate.ToString("dd.MM.yyyy HH:mm"),
                        item.TicketPrice,
                        item.RetainedFee,
                        item.RefundAmount,
                        item.Status
                    });
                }
            }

            string safeTitle = reportTitle.Replace(" ", "_").Replace("«", "").Replace("»", "").Replace("/", "_");
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Сохранение отчёта в Excel",
                Filter = "Книга Excel (*.xlsx)|*.xlsx",
                FileName = $"Отчет_{safeTitle}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (dialog.ShowDialog() == true)
            {
                ExcelExportService.ExportToExcel(dialog.FileName, reportTitle, periodText, filterText, kpi, headers, rows);

                var res = MessageBox.Show(
                    $"Отчёт успешно выгружен в Excel:\n{dialog.FileName}\n\nОткрыть созданный файл сейчас?",
                    "Выгрузка в Excel завершена",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialog.FileName)
                    {
                        UseShellExecute = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при выгрузке в Excel: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    #endregion

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        _clockTimer.Stop();
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }

    private void ExitApp_Click(object sender, MouseButtonEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
