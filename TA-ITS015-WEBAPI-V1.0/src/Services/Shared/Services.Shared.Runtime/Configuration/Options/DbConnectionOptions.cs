namespace Services.Shared.Configuration;

/// <summary>
/// Author: TAC Team
/// Description: Cấu hình kết nối CSDL (section "DbConnection" trong Database.json). Khớp cấu trúc
/// chuẩn của solution để dùng chung Database.json giữa WebAPI và các service.
/// Created date: 17/07/2026
/// </summary>
public sealed class DbConnectionOptions
{
    /// <summary>Bật log SQL ra console.</summary>
    public bool EnableConsoleSql { get; set; }

    /// <summary>ConfigId của kết nối chứa bảng cấu hình runtime (vd SysOpConfig). Mặc định "Default".</summary>
    public string ConfigConnectionId { get; set; } = "Default";

    /// <summary>Danh sách kết nối CSDL do SqlSugar quản lý.</summary>
    public List<DbConnectionConfig> ConnectionConfigs { get; set; } = [];
}

/// <summary>
/// Author: TAC Team
/// Description: Một kết nối CSDL trong Database.json (ConfigId / DbType / ConnectionString + settings).
/// Created date: 17/07/2026
/// </summary>
public sealed class DbConnectionConfig
{
    public string ConfigId { get; set; } = string.Empty;

    public string DbType { get; set; } = "SqlServer";

    public string ConnectionString { get; set; } = string.Empty;

    public DbSettings? DbSettings { get; set; }

    public TableSettings? TableSettings { get; set; }

    public SeedSettings? SeedSettings { get; set; }

    public List<SlaveSettings>? SlaveSettings { get; set; }
}

public sealed class DbSettings
{
    public bool EnableInitDb { get; set; }

    public bool EnableDiffLog { get; set; }

    public bool EnableUnderLine { get; set; }
}

public sealed class TableSettings
{
    public bool EnableInitTable { get; set; }

    public bool EnableIncreTable { get; set; }
}

public sealed class SeedSettings
{
    public bool EnableInitSeed { get; set; }

    public bool EnableIncreSeed { get; set; }
}

public sealed class SlaveSettings
{
    public int HitRate { get; set; }

    public string ConnectionString { get; set; } = string.Empty;
}
