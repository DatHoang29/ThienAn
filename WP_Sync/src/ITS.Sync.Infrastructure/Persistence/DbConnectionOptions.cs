using System.Collections.Generic;

namespace ITS.Sync.Infrastructure.Persistence;

/// <summary>
/// Ánh xạ mục "DbConnection" trong Database.json. Mỗi phần tử ConnectionConfigs là một DB (theo ConfigId).
/// </summary>
public sealed class DbConnectionOptions
{
    public const string SectionName = "DbConnection";

    public bool EnableConsoleSql { get; set; }

    public List<DbConnectionItem> ConnectionConfigs { get; set; } = new();
}

/// <summary>Thông tin một kết nối DB.</summary>
public sealed class DbConnectionItem
{
    public string ConfigId { get; set; } = string.Empty;
    public string DbType { get; set; } = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
}
