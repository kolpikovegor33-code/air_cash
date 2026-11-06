using System.IO;
using Xunit;

namespace AirlineTicketOffice.Tests;

public class ScreenshotTests
{
    [Fact]
    public void GenerateAllScreenshots_ForReport()
    {
        string outDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        ScreenshotGenerator.GenerateAll(outDir);

        Assert.True(File.Exists(Path.Combine(outDir, "screenshot_login.png")));
        Assert.True(File.Exists(Path.Combine(outDir, "screenshot_main_cashier.png")));
        Assert.True(File.Exists(Path.Combine(outDir, "screenshot_booking.png")));
        Assert.True(File.Exists(Path.Combine(outDir, "screenshot_passengers.png")));
        Assert.True(File.Exists(Path.Combine(outDir, "screenshot_analytics.png")));
    }
}
