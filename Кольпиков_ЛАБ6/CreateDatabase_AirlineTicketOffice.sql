
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'AirlineTicketOffice')
BEGIN
    CREATE DATABASE [AirlineTicketOffice];
END
GO

USE [AirlineTicketOffice];
GO

DROP TABLE IF EXISTS [Tickets];
DROP TABLE IF EXISTS [Bookings];
DROP TABLE IF EXISTS [Flights];
DROP TABLE IF EXISTS [BaggageTariffs];
DROP TABLE IF EXISTS [ServiceClasses];
DROP TABLE IF EXISTS [Aircrafts];
DROP TABLE IF EXISTS [Airlines];
DROP TABLE IF EXISTS [Airports];
DROP TABLE IF EXISTS [Passengers];
DROP TABLE IF EXISTS [Users];
GO

CREATE TABLE [Users] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Login] NVARCHAR(50) NOT NULL UNIQUE,
    [Password] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(50) NOT NULL DEFAULT N'Кассир'
);
GO

CREATE TABLE [Passengers] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [LastName] NVARCHAR(100) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [MiddleName] NVARCHAR(100) NULL,
    [BirthDate] DATETIME2 NOT NULL,
    [DocumentNumber] NVARCHAR(50) NOT NULL,
    [Phone] NVARCHAR(50) NULL,
    [Email] NVARCHAR(100) NULL
);
GO

CREATE TABLE [Airports] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Title] NVARCHAR(100) NOT NULL,
    [City] NVARCHAR(100) NOT NULL,
    [Code] NVARCHAR(10) NOT NULL UNIQUE
);
GO

CREATE TABLE [Airlines] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Title] NVARCHAR(150) NOT NULL
);
GO

CREATE TABLE [Aircrafts] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Model] NVARCHAR(100) NOT NULL,
    [SeatCount] INT NOT NULL
);
GO

CREATE TABLE [ServiceClasses] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Title] NVARCHAR(50) NOT NULL
);
GO

CREATE TABLE [BaggageTariffs] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Title] NVARCHAR(100) NOT NULL,
    [FreeWeightKg] DECIMAL(18,2) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL
);
GO

CREATE TABLE [Flights] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [FlightNumber] NVARCHAR(20) NOT NULL,
    [DepartureAirportId] INT NOT NULL FOREIGN KEY REFERENCES [Airports]([Id]),
    [ArrivalAirportId] INT NOT NULL FOREIGN KEY REFERENCES [Airports]([Id]),
    [AirlineId] INT NOT NULL FOREIGN KEY REFERENCES [Airlines]([Id]),
    [AircraftId] INT NOT NULL FOREIGN KEY REFERENCES [Aircrafts]([Id]),
    [DepartureDateTime] DATETIME2 NOT NULL,
    [ArrivalDateTime] DATETIME2 NOT NULL,
    [FreeSeats] INT NOT NULL,
    [BasePrice] DECIMAL(18,2) NOT NULL,
    [BusinessPrice] DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT N'По расписанию'
);
GO

CREATE TABLE [Bookings] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [BookingNumber] NVARCHAR(50) NOT NULL UNIQUE,
    [FlightId] INT NOT NULL FOREIGN KEY REFERENCES [Flights]([Id]),
    [PassengerId] INT NOT NULL FOREIGN KEY REFERENCES [Passengers]([Id]),
    [UserId] INT NOT NULL FOREIGN KEY REFERENCES [Users]([Id]),
    [BookingDate] DATETIME2 NOT NULL,
    [ExpireDate] DATETIME2 NOT NULL,
    [SeatNumber] NVARCHAR(10) NOT NULL,
    [ClassTitle] NVARCHAR(50) NOT NULL DEFAULT N'Эконом',
    [Price] DECIMAL(18,2) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT N'Создано'
);
GO

CREATE TABLE [Tickets] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [TicketNumber] NVARCHAR(50) NOT NULL UNIQUE,
    [FlightId] INT NOT NULL FOREIGN KEY REFERENCES [Flights]([Id]),
    [PassengerId] INT NOT NULL FOREIGN KEY REFERENCES [Passengers]([Id]),
    [BookingId] INT NULL FOREIGN KEY REFERENCES [Bookings]([Id]),
    [UserId] INT NOT NULL FOREIGN KEY REFERENCES [Users]([Id]),
    [BaggageTariffId] INT NOT NULL FOREIGN KEY REFERENCES [BaggageTariffs]([Id]),
    [PurchaseDate] DATETIME2 NOT NULL,
    [SeatNumber] NVARCHAR(10) NOT NULL,
    [ClassTitle] NVARCHAR(50) NOT NULL DEFAULT N'Эконом',
    [BaggageWeight] DECIMAL(18,2) NOT NULL,
    [BaggagePrice] DECIMAL(18,2) NOT NULL,
    [Price] DECIMAL(18,2) NOT NULL,
    [PaymentMethod] NVARCHAR(100) NOT NULL,
    [Status] NVARCHAR(50) NOT NULL DEFAULT N'Оплачен'
);
GO

INSERT INTO [Users] ([Login], [Password], [LastName], [FirstName], [Role]) VALUES
(N'cashier', N'123', N'Иванов', N'И.И.', N'Кассир'),
(N'admin', N'admin', N'Петров', N'П.П.', N'Администратор');

INSERT INTO [Airports] ([Title], [City], [Code]) VALUES
(N'Шереметьево', N'Москва', N'SVO'),
(N'Домодедово', N'Москва', N'DME'),
(N'Внуково', N'Москва', N'VKO'),
(N'Сочи (Адлер)', N'Сочи', N'AER'),
(N'Пулково', N'Санкт-Петербург', N'LED'),
(N'Казань', N'Казань', N'KZN');

INSERT INTO [Airlines] ([Title]) VALUES
(N'ПАО «Аэрофлот»'),
(N'АО «Авиакомпания Сибирь» (S7)'),
(N'ООО «Авиакомпания Победа»');

INSERT INTO [Aircrafts] ([Model], [SeatCount]) VALUES
(N'Airbus A320', 180),
(N'Boeing 737-800', 189),
(N'Airbus A321', 220);

INSERT INTO [ServiceClasses] ([Title]) VALUES
(N'Эконом'),
(N'Бизнес');

INSERT INTO [BaggageTariffs] ([Title], [FreeWeightKg], [Price]) VALUES
(N'Ручная кладь (до 10 кг)', 10.0, 0.0),
(N'Стандарт (до 23 кг)', 23.0, 0.0),
(N'Увеличенный (до 32 кг)', 32.0, 2500.0);

INSERT INTO [Passengers] ([LastName], [FirstName], [MiddleName], [BirthDate], [DocumentNumber], [Phone], [Email]) VALUES
(N'Иванов', N'Алексей', N'Петрович', '1988-07-14', N'4514 892341', N'+7 (915) 772-14-55', N'alexey.ivanov@mail.ru'),
(N'Иванова', N'Мария', N'Сергеевна', '1992-03-22', N'4518 102934', N'+7 (920) 901-22-33', N'm.ivanova@list.ru'),
(N'Петров', N'Дмитрий', N'Игоревич', '1979-11-05', N'1708 654321', N'+7 (903) 123-45-67', N'd.petrov@yandex.ru'),
(N'Петрова', N'Елена', N'Викторовна', '2001-09-30', N'4520 771234', N'+7 (916) 443-89-01', N'elena.petrova@gmail.com');

INSERT INTO [Flights] ([FlightNumber], [DepartureAirportId], [ArrivalAirportId], [AirlineId], [AircraftId], [DepartureDateTime], [ArrivalDateTime], [FreeSeats], [BasePrice], [BusinessPrice], [Status]) VALUES
(N'SU-1124', 1, 4, 1, 1, DATEADD(hour, 8, DATEADD(minute, 30, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), DATEADD(hour, 12, DATEADD(minute, 15, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), 42, 6450.0, 18900.0, N'По расписанию'),
(N'S7-2048', 2, 4, 2, 2, DATEADD(hour, 11, DATEADD(minute, 10, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), DATEADD(hour, 14, DATEADD(minute, 55, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), 18, 7100.0, 19500.0, N'По расписанию'),
(N'DP-144',  3, 4, 3, 2, DATEADD(hour, 14, DATEADD(minute, 20, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), DATEADD(hour, 18, DATEADD(minute, 0, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), 5, 4990.0, 12000.0, N'По расписанию'),
(N'SU-1130', 1, 4, 1, 3, DATEADD(hour, 19, DATEADD(minute, 40, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), DATEADD(hour, 23, DATEADD(minute, 25, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), 76, 6800.0, 19800.0, N'Задержка до 20:10'),
(N'SU-022',  1, 5, 1, 1, DATEADD(hour, 9, DATEADD(minute, 0, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), DATEADD(hour, 10, DATEADD(minute, 35, CAST(CAST(GETDATE()+1 AS DATE) AS DATETIME2))), 64, 3200.0, 9800.0, N'По расписанию');

INSERT INTO [Bookings] ([BookingNumber], [FlightId], [PassengerId], [UserId], [BookingDate], [ExpireDate], [SeatNumber], [ClassTitle], [Price], [Status]) VALUES
(N'PNR-48912', 1, 2, 1, DATEADD(hour, -5, GETDATE()), DATEADD(hour, 43, GETDATE()), N'11C', N'Эконом', 6450.0, N'Создано');

INSERT INTO [Tickets] ([TicketNumber], [FlightId], [PassengerId], [BookingId], [UserId], [BaggageTariffId], [PurchaseDate], [SeatNumber], [ClassTitle], [BaggageWeight], [BaggagePrice], [Price], [PaymentMethod], [Status]) VALUES
(N'555-2418901234', 1, 1, NULL, 1, 2, DATEADD(day, -1, GETDATE()), N'12A', N'Эконом', 20.0, 0.0, 6450.0, N'Банковская карта (Эквайринг)', N'Оплачен'),
(N'555-2418901235', 2, 3, NULL, 1, 3, DATEADD(day, -2, GETDATE()), N'10F', N'Эконом', 28.0, 2500.0, 9600.0, N'Наличные', N'Оплачен');
GO

PRINT 'База данных AirlineTicketOffice успешно создана и заполнена данными!';
GO
