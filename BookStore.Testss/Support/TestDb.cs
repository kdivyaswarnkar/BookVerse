using BookStore.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace BookStore.Tests.Support;

// Connection to the SEPARATE test database (BookStore_Test). Never point this at your real BookStore database.
public static class TestDb
{
    // EDIT if your SQL Server has another name (copy the Server=... part from your appsettings.json),
    // or set the environment variable BOOKSTORE_TEST_CONNECTION instead.
    private const string DefaultConnection =
        "Server=localhost;Database=BookStore_Test;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=5";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("BOOKSTORE_TEST_CONNECTION") is { Length: > 0 } c ? c : DefaultConnection;

    // true only if the connection works AND the database name ends with _Test (safety guard)
    public static readonly Lazy<bool> Available = new(() =>
    {
        try
        {
            var name = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
            if (!name.EndsWith("_Test", StringComparison.OrdinalIgnoreCase)) return false;

            using var connection = new SqlConnection(ConnectionString);
            connection.Open();
            return true;
        }
        catch
        {
            return false;
        }
    });

    public static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);
}

// [SqlServerFact] = a normal [Fact] that is SKIPPED (not failed) when the test database is not reachable.
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!TestDb.Available.Value)
            Skip = "Test database not reachable. Run 010_create_test_database.sql and check TestDb.cs (see BE-16 steps).";
    }
}
