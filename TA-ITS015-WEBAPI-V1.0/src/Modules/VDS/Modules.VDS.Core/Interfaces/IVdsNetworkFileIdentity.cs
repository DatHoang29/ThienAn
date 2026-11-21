using Modules.VDS.Core.Options;

namespace Modules.VDS.Core.Interfaces;

/// <summary>
/// Author: KienLT
/// Description: Chạy thao tác file dưới danh tính mạng (SMB) riêng theo cấu hình share VDS.
/// Created date: 02/10/2026
/// </summary>
public interface IVdsNetworkFileIdentity
{
    Task<T> RunAsync<T>(string configuredRoot, VdsImageShareOptions settings, Func<Task<T>> operation, CancellationToken ct);
}
