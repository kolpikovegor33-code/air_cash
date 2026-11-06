using System;
using System.Collections.Generic;

namespace AirlineTicketOffice.Models;

public class Airport
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public string DisplayName => $"{City} ({Title} / {Code})";
}

public class Aircraft
{
    public int Id { get; set; }
    public string Model { get; set; } = string.Empty;
    public int SeatCount { get; set; }
}

public class Airline
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class ServiceClass
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class BaggageTariff
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal FreeWeightKg { get; set; }
    public decimal Price { get; set; }

    public string DisplayName => $"{Title} — {Price:N0} ₽";
}

public class Flight
{
    public int Id { get; set; }
    public string FlightNumber { get; set; } = string.Empty;

    public int DepartureAirportId { get; set; }
    public Airport? DepartureAirport { get; set; }

    public int ArrivalAirportId { get; set; }
    public Airport? ArrivalAirport { get; set; }

    public int AirlineId { get; set; }
    public Airline? Airline { get; set; }

    public int AircraftId { get; set; }
    public Aircraft? Aircraft { get; set; }

    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }

    public int FreeSeats { get; set; }
    public decimal BasePrice { get; set; }
    public decimal BusinessPrice { get; set; }
    public string Status { get; set; } = "По расписанию";

    public string RouteDisplay => $"{DepartureAirport?.City} ({DepartureAirport?.Code}) → {ArrivalAirport?.City} ({ArrivalAirport?.Code})";
    public string DepartureTimeDisplay => $"{DepartureDateTime:dd.MM.yyyy HH:mm}";
    public string ArrivalTimeDisplay => $"{ArrivalDateTime:dd.MM.yyyy HH:mm}";
    public string BasePriceDisplay => $"от {BasePrice:N0} ₽";
    public string FreeSeatsDisplay => $"{FreeSeats} мест";
}

public class Passenger
{
    public int Id { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateTime BirthDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{LastName} {FirstName}"
        : $"{LastName} {FirstName} {MiddleName}";

    public string ShortInfo => $"{FullName}, Паспорт: {DocumentNumber}";
}

public class User
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string Role { get; set; } = "Кассир";

    public string FullName => $"{LastName} {FirstName}";
}

public class Booking
{
    public int Id { get; set; }
    public string BookingNumber { get; set; } = string.Empty;

    public int FlightId { get; set; }
    public Flight? Flight { get; set; }

    public int PassengerId { get; set; }
    public Passenger? Passenger { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime BookingDate { get; set; }
    public DateTime ExpireDate { get; set; }

    public string SeatNumber { get; set; } = string.Empty;
    public string ClassTitle { get; set; } = "Эконом";
    public decimal Price { get; set; }
    public string Status { get; set; } = "Создано";
}

public class Ticket
{
    public int Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;

    public int FlightId { get; set; }
    public Flight? Flight { get; set; }

    public int PassengerId { get; set; }
    public Passenger? Passenger { get; set; }

    public int? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int BaggageTariffId { get; set; }
    public BaggageTariff? BaggageTariff { get; set; }

    public DateTime PurchaseDate { get; set; }
    public string SeatNumber { get; set; } = string.Empty;
    public string ClassTitle { get; set; } = "Эконом";

    public decimal BaggageWeight { get; set; }
    public decimal BaggagePrice { get; set; }
    public decimal Price { get; set; }
    public string PaymentMethod { get; set; } = "Банковская карта (Эквайринг)";
    public string Status { get; set; } = "Оплачен";
}
