using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Infrastructure.Database;

internal sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _connectionString =
        configuration.GetConnectionString("SqlConnection")
        ?? throw new InvalidOperationException("SqlConnection string is missing from configuration.");

    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken ct = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
