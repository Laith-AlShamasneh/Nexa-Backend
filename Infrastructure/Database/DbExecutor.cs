using Application.Interfaces.Database;
using Dapper;
using System.Data;

namespace Infrastructure.Database;

internal sealed class DbExecutor(ISqlConnectionFactory connectionFactory) : IDbExecutor
{
    public async Task<int> ExecuteAsync(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(ct);

        return await connection.ExecuteAsync(
            new CommandDefinition(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task<T?> ExecuteScalarAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(ct);

        return await connection.ExecuteScalarAsync<T>(
            new CommandDefinition(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task<T?> QuerySingleAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(ct);

        return await connection.QueryFirstOrDefaultAsync<T>(
            new CommandDefinition(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));
    }

    public async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(ct);

        var result = await connection.QueryAsync<T>(
            new CommandDefinition(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return result.AsList();
    }

    public async Task<T> QueryMultipleAsync<T>(
        string storedProcedure,
        Func<SqlMapper.GridReader, Task<T>> map,
        DynamicParameters? parameters = null,
        CancellationToken ct = default)
    {
        using var connection = await connectionFactory.CreateConnectionAsync(ct);

        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: ct));

        return await map(multi);
    }
}
