using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using StockFlow.Core.Configuration;

namespace StockFlow.Core.Data;

public class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    public SqlConnection Create() => new(_connectionString);
}
