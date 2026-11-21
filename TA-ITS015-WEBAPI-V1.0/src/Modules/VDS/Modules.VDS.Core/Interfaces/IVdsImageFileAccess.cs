namespace Modules.VDS.Core.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Chạy thao tác file ảnh VDS dưới quyền truy cập share đã cấu hình.
/// Created date: 02/10/2026
/// </summary>
public interface IVdsImageFileAccess
{
    /// <summary>The root must come from host/tenant storage configuration, never a request path or image name.</summary>
    Task<T> RunAsync<T>(string configuredRoot, Func<Task<T>> operation, CancellationToken ct);
}
