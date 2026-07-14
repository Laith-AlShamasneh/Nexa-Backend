using System.Data;

namespace Infrastructure.Database;

public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(CancellationToken ct = default);
}
