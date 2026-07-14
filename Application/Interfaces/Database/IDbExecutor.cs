using Dapper;

namespace Application.Interfaces.Database;

public interface IDbExecutor
{
    Task<int> ExecuteAsync(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default);

    Task<T?> ExecuteScalarAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default);

    Task<T?> QuerySingleAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<T>> QueryListAsync<T>(
        string storedProcedure,
        DynamicParameters? parameters = null,
        CancellationToken ct = default);

    Task<T> QueryMultipleAsync<T>(
        string storedProcedure,
        Func<SqlMapper.GridReader, Task<T>> map,
        DynamicParameters? parameters = null,
        CancellationToken ct = default);
}
