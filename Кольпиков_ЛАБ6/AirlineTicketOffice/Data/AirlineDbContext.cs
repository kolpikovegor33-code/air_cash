using System;
using System.IO;
using AirlineTicketOffice.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AirlineTicketOffice.Data;

public class AirlineDbContext : DbContext
{
    public DbSet<Airport> Airports => Set<Airport>();
    public DbSet<Aircraft> Aircrafts => Set<Aircraft>();
    public DbSet<Airline> Airlines => Set<Airline>();
    public DbSet<ServiceClass> ServiceClasses => Set<ServiceClass>();
    public DbSet<BaggageTariff> BaggageTariffs => Set<BaggageTariff>();
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Passenger> Passengers => Set<Passenger>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    private readonly string? _customConnectionString;
    private readonly bool _useSqliteOnly;

    public AirlineDbContext(string? customConnectionString = null, bool useSqliteOnly = false)
    {
        _customConnectionString = customConnectionString;
        _useSqliteOnly = useSqliteOnly;

        try
        {
            Database.EnsureCreated();
            SeedDataIfEmpty();
        }
        catch
        {
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        if (_useSqliteOnly)
        {
            string localDb = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AirlineTicketOffice.db");
            optionsBuilder.UseSqlite($"Data Source={localDb}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_customConnectionString))
        {
            if (_customConnectionString.Contains(".db") || _customConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                string sqliteConn = _customConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase)
                    ? _customConnectionString
                    : $"Data Source={_customConnectionString}";
                optionsBuilder.UseSqlite(sqliteConn);
            }
            else
            {
                optionsBuilder.UseSqlServer(_customConnectionString);
            }
            return;
        }

        string connStr = DbSettings.ConnectionString;
        bool sqlConnected = false;

        try
        {
            using var testConn = new SqlConnection($"Server={DbSettings.ServerName};Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2;");
            testConn.Open();
            sqlConnected = true;
        }
        catch
        {
            try
            {
                using var testLocal = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=2;");
                testLocal.Open();
                connStr = DbSettings.LocalDbConnectionString;
                sqlConnected = true;
            }
            catch
            {
                sqlConnected = false;
            }
        }

        if (sqlConnected)
        {
            optionsBuilder.UseSqlServer(connStr);
        }
        else
        {
            string fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AirlineTicketOffice.db");
            optionsBuilder.UseSqlite($"Data Source={fallbackPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BaggageTariff>().Property(b => b.FreeWeightKg).HasPrecision(18, 2);
        modelBuilder.Entity<BaggageTariff>().Property(b => b.Price).HasPrecision(18, 2);

        modelBuilder.Entity<Flight>().Property(f => f.BasePrice).HasPrecision(18, 2);
        modelBuilder.Entity<Flight>().Property(f => f.BusinessPrice).HasPrecision(18, 2);

        modelBuilder.Entity<Booking>().Property(b => b.Price).HasPrecision(18, 2);

        modelBuilder.Entity<Ticket>().Property(t => t.BaggageWeight).HasPrecision(18, 2);
        modelBuilder.Entity<Ticket>().Property(t => t.BaggagePrice).HasPrecision(18, 2);
        modelBuilder.Entity<Ticket>().Property(t => t.Price).HasPrecision(18, 2);

        modelBuilder.Entity<Flight>()
            .HasOne(f => f.DepartureAirport)
            .WithMany()
            .HasForeignKey(f => f.DepartureAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Flight>()
            .HasOne(f => f.ArrivalAirport)
            .WithMany()
            .HasForeignKey(f => f.ArrivalAirportId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Flight)
            .WithMany()
            .HasForeignKey(b => b.FlightId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Flight)
            .WithMany()
            .HasForeignKey(t => t.FlightId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    public void SeedDataIfEmpty()
    {
        if (Airports.Any()) return;

        var svo = new Airport { Title = "Шереметьево", City = "Москва", Code = "SVO" };
        var dme = new Airport { Title = "Домодедово", City = "Москва", Code = "DME" };
        var vko = new Airport { Title = "Внуково", City = "Москва", Code = "VKO" };
        var aer = new Airport { Title = "Сочи (Адлер)", City = "Сочи", Code = "AER" };
        var led = new Airport { Title = "Пулково", City = "Санкт-Петербург", Code = "LED" };
        var kzn = new Airport { Title = "Казань", City = "Казань", Code = "KZN" };
        Airports.AddRange(svo, dme, vko, aer, led, kzn);

        var aeroflot = new Airline { Title = "ПАО «Аэрофлот»" };
        var s7 = new Airline { Title = "АО «Авиакомпания Сибирь» (S7)" };
        var pobeda = new Airline { Title = "ООО «Авиакомпания Победа»" };
        Airlines.AddRange(aeroflot, s7, pobeda);

        var a320 = new Aircraft { Model = "Airbus A320", SeatCount = 180 };
        var b737 = new Aircraft { Model = "Boeing 737-800", SeatCount = 189 };
        var a321 = new Aircraft { Model = "Airbus A321", SeatCount = 220 };
        Aircrafts.AddRange(a320, b737, a321);

        var classEconomy = new ServiceClass { Title = "Эконом" };
        var classBusiness = new ServiceClass { Title = "Бизнес" };
        ServiceClasses.AddRange(classEconomy, classBusiness);

        var bagHand = new BaggageTariff { Title = "Ручная кладь (до 10 кг)", FreeWeightKg = 10, Price = 0 };
        var bagStd = new BaggageTariff { Title = "Стандарт (до 23 кг)", FreeWeightKg = 23, Price = 0 };
        var bagExtra = new BaggageTariff { Title = "Увеличенный (до 32 кг)", FreeWeightKg = 32, Price = 2500 };
        BaggageTariffs.AddRange(bagHand, bagStd, bagExtra);

        var cashier = new User
        {
            Login = "cashier",
            Password = "123",
            LastName = "Иванов",
            FirstName = "И.И.",
            Role = "Кассир"
        };
        var admin = new User
        {
            Login = "admin",
            Password = "admin",
            LastName = "Петров",
            FirstName = "П.П.",
            Role = "Администратор"
        };
        Users.AddRange(cashier, admin);

        var p1 = new Passenger
        {
            LastName = "Иванов",
            FirstName = "Алексей",
            MiddleName = "Петрович",
            BirthDate = new DateTime(1988, 7, 14),
            DocumentNumber = "4514 892341",
            Phone = "+7 (915) 772-14-55",
            Email = "alexey.ivanov@mail.ru"
        };
        var p2 = new Passenger
        {
            LastName = "Иванова",
            FirstName = "Мария",
            MiddleName = "Сергеевна",
            BirthDate = new DateTime(1992, 3, 22),
            DocumentNumber = "4518 102934",
            Phone = "+7 (920) 901-22-33",
            Email = "m.ivanova@list.ru"
        };
        var p3 = new Passenger
        {
            LastName = "Петров",
            FirstName = "Дмитрий",
            MiddleName = "Игоревич",
            BirthDate = new DateTime(1979, 11, 5),
            DocumentNumber = "1708 654321",
            Phone = "+7 (903) 123-45-67",
            Email = "d.petrov@yandex.ru"
        };
        var p4 = new Passenger
        {
            LastName = "Петрова",
            FirstName = "Елена",
            MiddleName = "Викторовна",
            BirthDate = new DateTime(2001, 9, 30),
            DocumentNumber = "4520 771234",
            Phone = "+7 (916) 443-89-01",
            Email = "elena.petrova@gmail.com"
        };
        Passengers.AddRange(p1, p2, p3, p4);

        var baseDate = DateTime.Today.AddDays(1);
        var f1 = new Flight
        {
            FlightNumber = "SU-1124",
            Airline = aeroflot,
            Aircraft = a320,
            DepartureAirport = svo,
            ArrivalAirport = aer,
            DepartureDateTime = baseDate.AddHours(8).AddMinutes(30),
            ArrivalDateTime = baseDate.AddHours(12).AddMinutes(15),
            FreeSeats = 42,
            BasePrice = 6450,
            BusinessPrice = 18900,
            Status = "По расписанию"
        };
        var f2 = new Flight
        {
            FlightNumber = "S7-2048",
            Airline = s7,
            Aircraft = b737,
            DepartureAirport = dme,
            ArrivalAirport = aer,
            DepartureDateTime = baseDate.AddHours(11).AddMinutes(10),
            ArrivalDateTime = baseDate.AddHours(14).AddMinutes(55),
            FreeSeats = 18,
            BasePrice = 7100,
            BusinessPrice = 19500,
            Status = "По расписанию"
        };
        var f3 = new Flight
        {
            FlightNumber = "DP-144",
            Airline = pobeda,
            Aircraft = b737,
            DepartureAirport = vko,
            ArrivalAirport = aer,
            DepartureDateTime = baseDate.AddHours(14).AddMinutes(20),
            ArrivalDateTime = baseDate.AddHours(18).AddMinutes(0),
            FreeSeats = 5,
            BasePrice = 4990,
            BusinessPrice = 12000,
            Status = "По расписанию"
        };
        var f4 = new Flight
        {
            FlightNumber = "SU-1130",
            Airline = aeroflot,
            Aircraft = a321,
            DepartureAirport = svo,
            ArrivalAirport = aer,
            DepartureDateTime = baseDate.AddHours(19).AddMinutes(40),
            ArrivalDateTime = baseDate.AddHours(23).AddMinutes(25),
            FreeSeats = 76,
            BasePrice = 6800,
            BusinessPrice = 19800,
            Status = "Задержка до 20:10"
        };
        var f5 = new Flight
        {
            FlightNumber = "SU-022",
            Airline = aeroflot,
            Aircraft = a320,
            DepartureAirport = svo,
            ArrivalAirport = led,
            DepartureDateTime = baseDate.AddHours(9).AddMinutes(0),
            ArrivalDateTime = baseDate.AddHours(10).AddMinutes(35),
            FreeSeats = 64,
            BasePrice = 3200,
            BusinessPrice = 9800,
            Status = "По расписанию"
        };
        Flights.AddRange(f1, f2, f3, f4, f5);

        var b1 = new Booking
        {
            BookingNumber = "PNR-48912",
            Flight = f1,
            Passenger = p2,
            User = cashier,
            BookingDate = DateTime.Now.AddHours(-5),
            ExpireDate = DateTime.Now.AddHours(43),
            SeatNumber = "11C",
            ClassTitle = "Эконом",
            Price = 6450,
            Status = "Создано"
        };
        Bookings.Add(b1);

        var t1 = new Ticket
        {
            TicketNumber = "555-2418901234",
            Flight = f1,
            Passenger = p1,
            User = cashier,
            BaggageTariff = bagStd,
            PurchaseDate = DateTime.Now.AddDays(-1),
            SeatNumber = "12A",
            ClassTitle = "Эконом",
            BaggageWeight = 20,
            BaggagePrice = 0,
            Price = 6450,
            PaymentMethod = "Банковская карта (Эквайринг)",
            Status = "Оплачен"
        };
        var t2 = new Ticket
        {
            TicketNumber = "555-2418901235",
            Flight = f2,
            Passenger = p3,
            User = cashier,
            BaggageTariff = bagExtra,
            PurchaseDate = DateTime.Now.AddDays(-2),
            SeatNumber = "10F",
            ClassTitle = "Эконом",
            BaggageWeight = 28,
            BaggagePrice = 2500,
            Price = 9600,
            PaymentMethod = "Наличные",
            Status = "Оплачен"
        };
        var t3 = new Ticket
        {
            TicketNumber = "555-2418901236",
            Flight = f2,
            Passenger = p4,
            User = cashier,
            BaggageTariff = bagStd,
            PurchaseDate = DateTime.Now.AddDays(-3),
            SeatNumber = "14B",
            ClassTitle = "Эконом",
            BaggageWeight = 18,
            BaggagePrice = 0,
            Price = 12300,
            PaymentMethod = "Банковская карта (Эквайринг)",
            Status = "Возвращён"
        };
        Tickets.AddRange(t1, t2, t3);

        SaveChanges();
    }
}
