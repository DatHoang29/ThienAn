using SqlSugar;

namespace Services.Shared.Persistence;

/// <summary>
/// Author: TAC Team
/// Description: Base mỏng cho các repository cụ thể — chỉ lo phần chọn connection và expose
/// <see cref="Client"/> (SqlSugar). Không chứa CRUD generic: mỗi repository tự viết query domain
/// đặt tên theo nghiệp vụ, SqlSugar được giấu sau method đó. Consumer không thấy ISqlSugarClient.
/// Inspired by ACS.ACS10.Database.DeviceMonitor Management/*DbManager nhưng có DI, không static.
/// Created date: 17/07/2026
/// </summary>
public abstract class SqlSugarAccess
{
    private readonly DatabaseManager _databaseManager;
    private readonly Func<string> _resolveConnectionId;

    /// <param name="connectionIdResolver">Hàm trả về ConfigId của connection cần dùng (có thể đổi theo runtime config).</param>
    protected SqlSugarAccess(DatabaseManager databaseManager, Func<string> connectionIdResolver)
    {
        _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        _resolveConnectionId = connectionIdResolver ?? throw new ArgumentNullException(nameof(connectionIdResolver));
    }

    /// <param name="connectionId">ConfigId cố định (vd connection bootstrap cho bảng cấu hình).</param>
    protected SqlSugarAccess(DatabaseManager databaseManager, string connectionId)
        : this(databaseManager, () => connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new ArgumentException("Connection id is required.", nameof(connectionId));
    }

    /// <summary>SqlSugar client cho connection hiện tại. Dùng trong subclass để viết query domain.</summary>
    protected ISqlSugarClient Client => _databaseManager.Get(ResolveConnectionId());

    private string ResolveConnectionId()
    {
        var connectionId = _resolveConnectionId()
            ?? throw new InvalidOperationException("Resolved database connection id is null.");
        if (string.IsNullOrWhiteSpace(connectionId))
            throw new InvalidOperationException("Resolved database connection id is empty.");

        return connectionId;
    }
}
