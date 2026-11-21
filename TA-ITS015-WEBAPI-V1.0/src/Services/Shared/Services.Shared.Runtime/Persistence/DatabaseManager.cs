using Microsoft.Extensions.Options;
using Services.Shared.Configuration;
using SqlSugar;

namespace Services.Shared.Persistence;

/// <summary>
/// Author: TAC Team
/// Description: Quản lý SqlSugarScope hỗ trợ nhiều kết nối CSDL. Đọc từ DbConnectionOptions
/// (section "DbConnection" trong Database.json). Truy cập connection qua ConfigId.
/// Created date: 16/07/2026
/// </summary>
public sealed class DatabaseManager : IDisposable
{
    private readonly HashSet<string> _connectionIds;
    private readonly SqlSugarScope _scope;

    public DatabaseManager(IOptions<DbConnectionOptions> options)
    {
        var connections = options.Value.ConnectionConfigs;
        if (connections.Count == 0)
            throw new InvalidOperationException("DbConnection:ConnectionConfigs is required.");

        var duplicate = connections
            .GroupBy(x => x.ConfigId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Count() > 1);

        if (duplicate is not null)
            throw new InvalidOperationException($"Duplicate database connection ConfigId: {duplicate.Key}");

        foreach (var connection in connections)
        {
            if (string.IsNullOrWhiteSpace(connection.ConfigId))
                throw new InvalidOperationException("Database connection ConfigId is required.");

            if (string.IsNullOrWhiteSpace(connection.ConnectionString))
                throw new InvalidOperationException($"Connection string is required for '{connection.ConfigId}'.");
        }

        var configs = connections.Select(item => new ConnectionConfig
        {
            ConfigId = item.ConfigId,
            ConnectionString = item.ConnectionString,
            DbType = ParseDbType(item.DbType),
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        }).ToList();

        _connectionIds = connections
            .Select(x => x.ConfigId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        _scope = new SqlSugarScope(configs);
    }

    public ISqlSugarClient Get(string connectionId)
    {
        if (!Contains(connectionId))
            throw new KeyNotFoundException($"Database connection '{connectionId}' is not configured.");

        return _scope.GetConnection(connectionId);
    }

    public bool Contains(string connectionId) =>
        !string.IsNullOrWhiteSpace(connectionId) && _connectionIds.Contains(connectionId);

    public void Dispose() => _scope.Dispose();

    private static SqlSugar.DbType ParseDbType(string value)
    {
        if (Enum.TryParse<SqlSugar.DbType>(value, true, out var dbType))
            return dbType;

        throw new InvalidOperationException($"Unsupported SqlSugar DbType: {value}");
    }
}
