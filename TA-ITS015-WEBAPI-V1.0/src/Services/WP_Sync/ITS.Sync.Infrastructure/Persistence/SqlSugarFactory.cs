using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SqlSugar;

namespace ITS.Sync.Infrastructure.Persistence;

/// <summary>
/// Tạo SqlSugar client chứa nhiều kết nối (Source + Target), đọc từ <see cref="DbConnectionOptions"/>.
/// </summary>
public sealed class SqlSugarFactory
{
    private readonly DbConnectionOptions _options;

    public SqlSugarFactory(DbConnectionOptions options) => _options = options;

    public ISqlSugarClient Create()
    {
        if (_options.ConnectionConfigs.Count == 0)
            throw new InvalidOperationException(
                "Chưa khai báo ConnectionConfigs trong section 'DbConnection'.");

        var configs = _options.ConnectionConfigs.Select(c => new ConnectionConfig
        {
            ConfigId = c.ConfigId,
            DbType = Enum.Parse<SqlSugar.DbType>(c.DbType, ignoreCase: true),
            ConnectionString = c.ConnectionString,
            IsAutoCloseConnection = true,

            // === Công cụ đồng bộ phải MIRROR trung thực CreateTime/UpdateTime từ nguồn ===
            // Base class EntityTenant (Shared.Core.dll) gắn các cờ SqlSugar:
            //   CreateTime : InsertServerTime = true   -> INSERT tự ghi GETDATE()
            //   UpdateTime : UpdateServerTime = true   -> UPDATE tự ghi GETDATE()
            //                IsOnlyIgnoreInsert = true -> bị LOẠI khỏi INSERT (nên bản ghi mới = null)
            // => Nếu không xử lý, đích luôn nhận thời gian chạy sync thay vì thời gian thật của nguồn.
            // EntityService dưới đây gỡ các cờ đó (đã kiểm chứng bằng ToSqlString trên đúng đường ghi
            // AsInsertable/AsUpdateable của app). KHÔNG đụng IsOnlyIgnoreUpdate để CreateTime vẫn
            // không bị ghi đè mỗi lần UPDATE.
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityService = (property, column) =>
                {
                    if (property.Name is "CreateTime" or "UpdateTime")
                    {
                        column.InsertServerTime = false;
                        column.UpdateServerTime = false;
                        column.IsOnlyIgnoreInsert = false;
                    }
                }
            }
        }).ToList();

        var enableLog = _options.EnableConsoleSql;
        var client = new SqlSugarScope(configs, scope =>
        {
            if (!enableLog) return;
            foreach (var cfg in configs)
            {
                var provider = scope.GetConnectionScope(cfg.ConfigId);
                provider.Aop.OnLogExecuting = (sql, pars) => Console.WriteLine($"[{cfg.ConfigId}] {sql}");
            }
        });

        return client;
    }

    /// <summary>Thử mở kết nối tới một DB theo ConfigId để kiểm tra cấu hình.</summary>
    public async Task<(bool Ok, string? Error)> TestConnectionAsync(string configId, CancellationToken ct = default)
    {
        try
        {
            var client = Create();
            var provider = client.AsTenant().GetConnectionScope(configId);
            await Task.Run(() =>
            {
                provider.Ado.Connection.Open();
                provider.Ado.Connection.Close();
            }, ct);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
