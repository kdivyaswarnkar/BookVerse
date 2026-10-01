using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace BookStore.Data;

public class DbConnectionFactory(IConfiguration cfg)
{
    private readonly string _cs = cfg.GetConnectionString("Default")!;
    public SqlConnection Create() => new(_cs);
}