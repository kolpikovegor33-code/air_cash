using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AirlineTicketOffice.Data;
using AirlineTicketOffice.Models;
using AirlineTicketOffice.Views;

namespace AirlineTicketOffice.Tests;

public static class ScreenshotGenerator
{
    public static void GenerateAll(string outputDir)
    {
        Directory.CreateDirectory(outputDir);

        var thread = new Thread(() =>
        {
            var app = Application.Current ?? new Application();

            string testDb = Path.Combine(outputDir, "screen_airline.db");
            if (File.Exists(testDb)) File.Delete(testDb);

            using var db = new AirlineDbContext(testDb);
            var cashier = db.Users.First(u => u.Role == "Кассир");
            var admin = db.Users.First(u => u.Role == "Администратор");
            var flight = db.Flights.First();
            var passenger = db.Passengers.First();
            var ticket = db.Tickets.FirstOrDefault() ?? new Ticket
            {
                TicketNumber = "ET-10293847",
                PassengerId = passenger.Id,
                Passenger = passenger,
                FlightId = flight.Id,
                Flight = flight,
                SeatNumber = "12A",
                ClassTitle = "Эконом",
                Price = 8500m,
                Status = "Выписан",
                PurchaseDate = DateTime.Now
            };

            var loginWin = new LoginWindow();
            SaveWindowScreenshot(loginWin, Path.Combine(outputDir, "screenshot_login.png"), 550, 530);

            var mainWin = new MainWindow(cashier, db);
            SaveWindowScreenshot(mainWin, Path.Combine(outputDir, "screenshot_main_cashier.png"), 1180, 780);

            mainWin.SwitchTab(1);
            SaveWindowScreenshot(mainWin, Path.Combine(outputDir, "screenshot_passengers.png"), 1180, 780);

            mainWin.SwitchTab(4);
            SaveWindowScreenshot(mainWin, Path.Combine(outputDir, "screenshot_analytics.png"), 1180, 780);

            var bookWin = new BookingTicketWindow(flight, cashier, db);
            SaveWindowScreenshot(bookWin, Path.Combine(outputDir, "screenshot_booking.png"), 880, 680);

            var flightEditWin = new FlightEditWindow(db, flight);
            SaveWindowScreenshot(flightEditWin, Path.Combine(outputDir, "screenshot_flight_edit.png"), 660, 620);

            var refundDlg = new ConfirmRefundDialog(ticket);
            SaveWindowScreenshot(refundDlg, Path.Combine(outputDir, "screenshot_refund_dialog.png"), 520, 360);

            var successDlg = new SuccessTicketDialog(ticket.TicketNumber);
            SaveWindowScreenshot(successDlg, Path.Combine(outputDir, "screenshot_success_dialog.png"), 500, 420);

            var validDlg = new ValidationMessageDialog("Пожалуйста, проверьте правильность заполнения паспортных данных пассажира. Номер документа должен содержать не менее 6 цифр.");
            SaveWindowScreenshot(validDlg, Path.Combine(outputDir, "screenshot_validation_dialog.png"), 480, 320);
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
    }

    private static void SaveWindowScreenshot(Window window, string filePath, int width, int height)
    {
        var content = window.Content as UIElement;
        if (content != null)
        {
            window.Content = null;
            
            var container = new Border
            {
                Width = width,
                Height = height,
                Background = window.Background ?? Brushes.White,
                Child = content
            };

            container.Measure(new Size(width, height));
            container.Arrange(new Rect(0, 0, width, height));
            container.UpdateLayout();

            var rtb = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(container);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            using var fs = File.Create(filePath);
            encoder.Save(fs);

            container.Child = null;
            window.Content = content;
        }
    }
}
