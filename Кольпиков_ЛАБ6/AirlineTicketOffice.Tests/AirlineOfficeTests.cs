using System;
using System.IO;
using System.Linq;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Services;
using Xunit;

namespace AirlineTicketOffice.Tests;

public class AirlineOfficeTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly AirlineDbContext _db;
    private readonly AuthService _authService;
    private readonly FlightService _flightService;
    private readonly PassengerService _passengerService;
    private readonly TicketBookingService _bookingService;
    private readonly AnalyticsService _analyticsService;

    public AirlineOfficeTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"TestAir_{Guid.NewGuid():N}.db");
        _db = new AirlineDbContext(_testDbPath);
        _authService = new AuthService(_db);
        _flightService = new FlightService(_db);
        _passengerService = new PassengerService(_db);
        _bookingService = new TicketBookingService(_db);
        _analyticsService = new AnalyticsService(_db);
    }

    [Fact]
    public void Login_WithValidCashierCredentials_ReturnsSuccess()
    {
        var (success, user, message) = _authService.Login("cashier", "123");

        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal("Кассир", user.Role);
        Assert.Equal("Иванов", user.LastName);
    }

    [Fact]
    public void Login_WithValidAdminCredentials_ReturnsSuccessAndAdminRole()
    {
        var (success, user, message) = _authService.Login("admin", "admin");

        Assert.True(success);
        Assert.NotNull(user);
        Assert.Equal("Администратор", user.Role);
        Assert.Equal("Петров", user.LastName);
    }

    [Theory]
    [InlineData("", "123")]
    [InlineData("cashier", "")]
    [InlineData(null, null)]
    public void Login_WithEmptyCredentials_ReturnsValidationError(string? login, string? password)
    {
        var (success, user, message) = _authService.Login(login!, password!);

        Assert.False(success);
        Assert.Null(user);
        Assert.Contains("обязательные поля", message);
    }

    [Fact]
    public void Login_WithInvalidPassword_ReturnsAuthenticationError()
    {
        var (success, user, message) = _authService.Login("cashier_kolpikov", "wrong_pass_999");

        Assert.False(success);
        Assert.Null(user);
        Assert.Contains("Неверный логин или пароль", message);
    }

    [Fact]
    public void ValidatePassenger_WithValidData_ReturnsSuccess()
    {
        var p = new Passenger
        {
            LastName = "Смирнов",
            FirstName = "Игорь",
            BirthDate = new DateTime(1990, 5, 12),
            DocumentNumber = "4510 889900"
        };

        var val = _passengerService.Validate(p);

        Assert.True(val.IsValid);
        Assert.Empty(val.Errors);
    }

    [Theory]
    [InlineData("", "Иван", "4514 123456")]
    [InlineData("Иванов", "", "4514 123456")]
    public void ValidatePassenger_EmptyName_ReturnsError(string lName, string fName, string doc)
    {
        var p = new Passenger
        {
            LastName = lName,
            FirstName = fName,
            BirthDate = new DateTime(1995, 1, 1),
            DocumentNumber = doc
        };

        var val = _passengerService.Validate(p);

        Assert.False(val.IsValid);
        Assert.Contains(val.Errors, e => e.Contains("обязательно для заполнения"));
    }

    [Fact]
    public void ValidatePassenger_ShortDocumentNumber_ReturnsError()
    {
        var p = new Passenger
        {
            LastName = "Сидоров",
            FirstName = "Олег",
            BirthDate = new DateTime(1995, 1, 1),
            DocumentNumber = "123"
        };

        var val = _passengerService.Validate(p);

        Assert.False(val.IsValid);
        Assert.Contains(val.Errors, e => e.Contains("1234 876678") || e.Contains("паспорта"));
    }

    [Fact]
    public void ValidatePassenger_BirthDateInFuture_ReturnsError()
    {
        var p = new Passenger
        {
            LastName = "Петров",
            FirstName = "Антон",
            BirthDate = DateTime.Today.AddDays(10),
            DocumentNumber = "4514 889900"
        };

        var val = _passengerService.Validate(p);

        Assert.False(val.IsValid);
        Assert.Contains(val.Errors, e => e.Contains("будущем"));
    }

    [Fact]
    public void SavePassenger_ValidNewPassenger_PersistsToDatabase()
    {
        var p = new Passenger
        {
            LastName = "Федоров",
            FirstName = "Никита",
            MiddleName = "Сергеевич",
            BirthDate = new DateTime(1998, 4, 15),
            DocumentNumber = "4519 998877",
            Phone = "+7 (999) 111-22-33",
            Email = "fedorov@test.ru"
        };

        var (success, msg) = _passengerService.SavePassenger(p);

        Assert.True(success);
        Assert.True(p.Id > 0);

        var retrieved = _db.Passengers.Find(p.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("Федоров", retrieved.LastName);
    }

    [Fact]
    public void SavePassenger_ExistingPassenger_UpdatesProperties()
    {
        var existing = _db.Passengers.First();
        existing.Phone = "+7 (900) 000-00-00";

        var (success, msg) = _passengerService.SavePassenger(existing);

        Assert.True(success);
        var updated = _db.Passengers.Find(existing.Id);
        Assert.NotNull(updated);
        Assert.Equal("+7 (900) 000-00-00", updated.Phone);
    }

    [Fact]
    public void DeletePassenger_WithTickets_BlocksDeletion()
    {
        var ticket = _db.Tickets.First();
        var (success, msg) = _passengerService.DeletePassenger(ticket.PassengerId);

        Assert.False(success);
        Assert.Contains("оформлены билеты", msg);
    }

    [Fact]
    public void GetAllFlights_ReturnsSeededFlightsWithAirportsAndAirlines()
    {
        var flights = _flightService.GetAllFlights();

        Assert.NotEmpty(flights);
        Assert.All(flights, f =>
        {
            Assert.NotNull(f.DepartureAirport);
            Assert.NotNull(f.ArrivalAirport);
            Assert.NotNull(f.Airline);
            Assert.NotNull(f.Aircraft);
            Assert.True(f.BasePrice > 0);
        });
    }

    [Fact]
    public void SearchFlights_ByArrivalCity_FiltersCorrectly()
    {
        var flights = _flightService.SearchFlights(null, "Сочи", null, null);

        Assert.NotEmpty(flights);
        Assert.All(flights, f => Assert.Equal("Сочи", f.ArrivalAirport?.City));
    }

    [Fact]
    public void CreateBooking_ValidData_GeneratesPnrAndDecrementsFreeSeats()
    {
        var flight = _db.Flights.First(f => f.FlightNumber == "SU-1124");
        int initialSeats = flight.FreeSeats;
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();

        var (success, booking, msg) = _bookingService.CreateBooking(
            flight.Id, passenger.Id, user.Id, "13A", "Эконом", flight.BasePrice);

        Assert.True(success);
        Assert.NotNull(booking);
        Assert.StartsWith("PNR-", booking.BookingNumber);
        Assert.Equal("13A", booking.SeatNumber);
        Assert.Equal("Создано", booking.Status);

        var refreshedFlight = _db.Flights.Find(flight.Id);
        Assert.NotNull(refreshedFlight);
        Assert.Equal(initialSeats - 1, refreshedFlight.FreeSeats);
    }

    [Fact]
    public void CreateBooking_SeatAlreadyOccupied_ReturnsError()
    {
        var flight = _db.Flights.First();
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();

        _bookingService.CreateBooking(flight.Id, passenger.Id, user.Id, "13B", "Эконом", flight.BasePrice);
        var (success, booking, msg) = _bookingService.CreateBooking(flight.Id, passenger.Id, user.Id, "13B", "Эконом", flight.BasePrice);

        Assert.False(success);
        Assert.Null(booking);
        Assert.Contains("уже занято", msg);
    }

    [Fact]
    public void IssueTicket_ValidPayment_Generates13DigitTicketAndDecrementsSeats()
    {
        var flight = _db.Flights.First(f => f.FlightNumber == "S7-2048");
        int initialSeats = flight.FreeSeats;
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();
        var baggage = _db.BaggageTariffs.First();

        var (success, ticket, msg) = _bookingService.IssueTicket(
            flight.Id, passenger.Id, user.Id, null, baggage.Id, "13E", "Эконом",
            baggage.FreeWeightKg, baggage.Price, flight.BasePrice, "Банковская карта (Эквайринг)");

        Assert.True(success);
        Assert.NotNull(ticket);
        Assert.StartsWith("555-", ticket.TicketNumber);
        Assert.Equal("Оплачен", ticket.Status);

        var refreshedFlight = _db.Flights.Find(flight.Id);
        Assert.NotNull(refreshedFlight);
        Assert.Equal(initialSeats - 1, refreshedFlight.FreeSeats);
    }

    [Fact]
    public void IssueTicket_WithExtraBaggage_AddsBaggagePriceToTotal()
    {
        var flight = _db.Flights.First();
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();
        var extraBag = _db.BaggageTariffs.First(b => b.Price > 0);

        decimal expectedTotal = flight.BasePrice + extraBag.Price;

        var (success, ticket, msg) = _bookingService.IssueTicket(
            flight.Id, passenger.Id, user.Id, null, extraBag.Id, "13C", "Эконом",
            extraBag.FreeWeightKg, extraBag.Price, expectedTotal, "Наличные");

        Assert.True(success);
        Assert.NotNull(ticket);
        Assert.Equal(expectedTotal, ticket.Price);
        Assert.Equal(extraBag.Price, ticket.BaggagePrice);
    }

    [Fact]
    public void CancelBooking_ExistingBooking_SetsCancelledStatusAndRestoresSeat()
    {
        var flight = _db.Flights.First();
        int initialSeats = flight.FreeSeats;
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();

        var (_, booking, _) = _bookingService.CreateBooking(
            flight.Id, passenger.Id, user.Id, "10E", "Эконом", flight.BasePrice);

        Assert.NotNull(booking);
        var (success, msg) = _bookingService.CancelBooking(booking.Id);

        Assert.True(success);
        var refreshedBooking = _db.Bookings.Find(booking.Id);
        Assert.NotNull(refreshedBooking);
        Assert.Equal("Аннулирован", refreshedBooking.Status);

        var refreshedFlight = _db.Flights.Find(flight.Id);
        Assert.NotNull(refreshedFlight);
        Assert.Equal(initialSeats, refreshedFlight.FreeSeats);
    }

    [Fact]
    public void RefundTicket_ExistingPaidTicket_DeductsServiceFeeAndRestoresSeat()
    {
        var flight = _db.Flights.First();
        int initialSeats = flight.FreeSeats;
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();
        var bag = _db.BaggageTariffs.First();

        var (_, ticket, _) = _bookingService.IssueTicket(
            flight.Id, passenger.Id, user.Id, null, bag.Id, "12C", "Эконом",
            bag.FreeWeightKg, bag.Price, flight.BasePrice, "Банковская карта (Эквайринг)");

        Assert.NotNull(ticket);
        var (success, refundAmount, msg) = _bookingService.RefundTicket(ticket.Id);

        Assert.True(success);
        decimal expectedRefund = flight.BasePrice - TicketBookingService.ServiceFee;
        Assert.Equal(expectedRefund, refundAmount);

        var refreshedTicket = _db.Tickets.Find(ticket.Id);
        Assert.NotNull(refreshedTicket);
        Assert.Equal("Возвращён", refreshedTicket.Status);

        var refreshedFlight = _db.Flights.Find(flight.Id);
        Assert.NotNull(refreshedFlight);
        Assert.Equal(initialSeats, refreshedFlight.FreeSeats);
    }

    [Fact]
    public void GetAnalytics_CalculatesCorrectKpiMetricsAndBreakdown()
    {
        var (kpi, breakdown) = _analyticsService.GetAnalytics();

        Assert.True(kpi.TotalSoldTickets > 0);
        Assert.True(kpi.TotalRevenue > 0);
        Assert.True(kpi.AverageLoadFactorPercent > 0);
        Assert.NotEmpty(breakdown);
        Assert.Equal(3, breakdown.Count);
    }

    [Fact]
    public void AddFlight_ValidFlight_PersistsAndAppearsInFlightList()
    {
        var dep = _db.Airports.First(a => a.Code == "SVO");
        var arr = _db.Airports.First(a => a.Code == "LED");
        var airline = _db.Airlines.First();
        var aircraft = _db.Aircrafts.First();

        var newFlight = new Flight
        {
            FlightNumber = "SU-9999",
            DepartureAirportId = dep.Id,
            ArrivalAirportId = arr.Id,
            AirlineId = airline.Id,
            AircraftId = aircraft.Id,
            DepartureDateTime = DateTime.Today.AddDays(2).AddHours(10),
            ArrivalDateTime = DateTime.Today.AddDays(2).AddHours(12),
            FreeSeats = 150,
            BasePrice = 5500,
            BusinessPrice = 16000,
            Status = "По расписанию"
        };

        var (success, _, msg) = _flightService.AddFlight(newFlight);

        Assert.True(success);
        Assert.True(newFlight.Id > 0);

        var retrieved = _db.Flights.Find(newFlight.Id);
        Assert.NotNull(retrieved);
        Assert.Equal("SU-9999", retrieved.FlightNumber);
    }

    [Fact]
    public void UpdateFlight_ExistingFlight_ModifiesProperties()
    {
        var flight = _db.Flights.First();
        flight.Status = "Задерживается до 14:00";
        flight.BasePrice = 8800;

        var (success, _, msg) = _flightService.UpdateFlight(flight);

        Assert.True(success);
        var updated = _db.Flights.Find(flight.Id);
        Assert.NotNull(updated);
        Assert.Equal("Задерживается до 14:00", updated.Status);
        Assert.Equal(8800, updated.BasePrice);
    }

    [Fact]
    public void RefundTicket_ReleasesSeatInOccupiedSeatsMap()
    {
        var flight = _db.Flights.First();
        var passenger = _db.Passengers.First();
        var user = _db.Users.First();
        var bag = _db.BaggageTariffs.First();
        string testSeat = "11A";

        var (_, ticket, _) = _bookingService.IssueTicket(
            flight.Id, passenger.Id, user.Id, null, bag.Id, testSeat, "Эконом",
            bag.FreeWeightKg, bag.Price, flight.BasePrice, "Банковская карта");

        Assert.NotNull(ticket);
        var occupiedBeforeRefund = _bookingService.GetOccupiedSeats(flight.Id);
        Assert.Contains(testSeat, occupiedBeforeRefund);

        var (success, refundAmount, _) = _bookingService.RefundTicket(ticket.Id);
        Assert.True(success);

        var occupiedAfterRefund = _bookingService.GetOccupiedSeats(flight.Id);
        Assert.DoesNotContain(testSeat, occupiedAfterRefund);
    }

    [Fact]
    public void DynamicAnalytics_ReportsReturnValidItems()
    {
        var loadReport = _analyticsService.GetFlightLoadReport();
        Assert.NotEmpty(loadReport);
        Assert.All(loadReport, r =>
        {
            Assert.NotEmpty(r.FlightNumber);
            Assert.True(r.TotalSeats > 0);
        });

        var refundsReport = _analyticsService.GetRefundsReport();
        Assert.NotEmpty(refundsReport);
        Assert.All(refundsReport, r =>
        {
            Assert.NotEmpty(r.TicketNumber);
            Assert.Equal("Возвращён", r.Status);
        });
    }

    [Fact]
    public void ExcelExport_GeneratesValidOpenXmlWorkbook()
    {
        string tempXlsx = Path.Combine(Path.GetTempPath(), $"TestReport_{Guid.NewGuid():N}.xlsx");
        try
        {
            var (kpi, breakdown) = _analyticsService.GetAnalytics();
            var headers = new List<string> { "Авиакомпания", "Рейсы", "Места", "Выручка" };
            var rows = breakdown.Select(b => new List<object?>
            {
                b.AirlineTitle,
                b.FlightsCount,
                b.SeatsSold,
                b.TotalRevenue
            }).ToList();

            ExcelExportService.ExportToExcel(
                filePath: tempXlsx,
                reportTitle: "Тестовый финансовый отчет",
                periodText: "За весь период",
                filterText: "Все авиакомпании",
                kpi: kpi,
                headers: headers,
                rows: rows);

            Assert.True(File.Exists(tempXlsx));
            Assert.True(new FileInfo(tempXlsx).Length > 100);

            using (var zip = System.IO.Compression.ZipFile.OpenRead(tempXlsx))
            {
                Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
                Assert.NotNull(zip.GetEntry("xl/workbook.xml"));
                Assert.NotNull(zip.GetEntry("xl/worksheets/sheet1.xml"));
            }
        }
        finally
        {
            if (File.Exists(tempXlsx))
            {
                try { File.Delete(tempXlsx); } catch { }
            }
        }
    }

    public void Dispose()
    {
        _db.Dispose();
        if (File.Exists(_testDbPath))
        {
            try { File.Delete(_testDbPath); } catch { }
        }
    }
}
