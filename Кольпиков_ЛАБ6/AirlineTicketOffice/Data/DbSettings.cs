using System;

namespace AirlineTicketOffice.Data;

public static class DbSettings
{
    public static string ServerName { get; set; } = @".\SQLEXPRESS";

    public static string DatabaseName { get; set; } = "AirlineTicketOffice";

    public static string ConnectionString =>
        $"Server={ServerName};Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";

    public static string LocalDbConnectionString =>
        $"Server=(localdb)\\MSSQLLocalDB;Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
}
