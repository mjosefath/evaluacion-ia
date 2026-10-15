using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Inventario.Application.Data;

/// <summary>
/// Ejecuta procedimientos con result set + parámetros OUTPUT sobre la conexión del DbContext.
/// </summary>
internal static class StoredProcedureExecutor
{
    public static async Task<(IReadOnlyList<T> Items, int TotalRows)> QueryPagedAsync<T>(
        DbContext db,
        string procedureName,
        IReadOnlyList<SqlParameter> parameters,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var totalParam = new SqlParameter("@TotalRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(totalParam);

            var items = new List<T>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(map(reader));
                }
            }

            var totalRows = totalParam.Value is DBNull or null
                ? 0
                : Convert.ToInt32(totalParam.Value);

            return (items, totalRows);
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    /// <summary>
    /// Listado paginado con @TotalRows OUTPUT y @GrandTotal OUTPUT (reporte / PG5).
    /// </summary>
    public static async Task<(IReadOnlyList<T> Items, int TotalRows, decimal GrandTotal)> QueryReportPagedAsync<T>(
        DbContext db,
        string procedureName,
        IReadOnlyList<SqlParameter> parameters,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedureName;
            command.CommandType = CommandType.StoredProcedure;
            command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var totalParam = new SqlParameter("@TotalRows", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            var grandTotalParam = new SqlParameter("@GrandTotal", SqlDbType.Decimal)
            {
                Direction = ParameterDirection.Output,
                Precision = 18,
                Scale = 2
            };
            command.Parameters.Add(totalParam);
            command.Parameters.Add(grandTotalParam);

            var items = new List<T>();
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(map(reader));
                }
            }

            var totalRows = totalParam.Value is DBNull or null
                ? 0
                : Convert.ToInt32(totalParam.Value);
            var grandTotal = grandTotalParam.Value is DBNull or null
                ? 0m
                : Convert.ToDecimal(grandTotalParam.Value);

            return (items, totalRows, grandTotal);
        }
        finally
        {
            if (shouldClose)
            {
                await db.Database.CloseConnectionAsync();
            }
        }
    }

    public static SqlParameter In(string name, object? value, SqlDbType dbType) =>
        new(name, dbType) { Value = value ?? DBNull.Value };
}
