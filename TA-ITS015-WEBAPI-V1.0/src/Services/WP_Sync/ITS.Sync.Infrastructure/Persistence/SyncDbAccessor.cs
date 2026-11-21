using SqlSugar;

namespace ITS.Sync.Infrastructure.Persistence;

/// <summary>
/// Cấp repository và client gắn đúng DB nguồn/đích (ẩn việc chọn ConfigId).
/// </summary>
public sealed class SyncDbAccessor
{
    private readonly ISqlSugarClient _root;

    public SyncDbAccessor(ISqlSugarClient root) => _root = root;

    /// <summary>Client (scope) tới DB NGUỒN - dùng khi cần transaction/thao tác trực tiếp.</summary>
    public ISqlSugarClient SourceDb => _root.AsTenant().GetConnectionScope(DbConstants.SourceDb);

    /// <summary>Client (scope) tới DB ĐÍCH - dùng khi cần transaction/thao tác trực tiếp.</summary>
    public ISqlSugarClient TargetDb => _root.AsTenant().GetConnectionScope(DbConstants.TargetDb);

    /// <summary>Repository trỏ tới DB NGUỒN (Dev_ITS10).</summary>
    public BaseRepository<T> Source<T>() where T : class, new()
        => new(SourceDb);

    /// <summary>Repository trỏ tới DB ĐÍCH (DEV_ITS015_WP).</summary>
    public BaseRepository<T> Target<T>() where T : class, new()
        => new(TargetDb);
}
