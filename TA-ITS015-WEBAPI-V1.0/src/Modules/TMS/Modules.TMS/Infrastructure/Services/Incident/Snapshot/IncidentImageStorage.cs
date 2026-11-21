using Modules.VDS.Core.Services.Media;
using Microsoft.AspNetCore.Hosting;

namespace Modules.TMS.Infrastructure.Services.Incident.Snapshot;

/// <summary>
/// Author: KienLT
/// Description: Nơi DUY NHẤT biết ảnh sự cố nằm ở đâu: <c>{Root}/{yyyyMM}/{ddHH}/{incident}_{equipment}_{1|2|3}.jpeg</c>.
///
/// <para>Trước 14/10/2026 ảnh nằm trong <c>wwwroot/tms/incident</c> và ba nơi tự ghép đường dẫn
/// (IncidentSnapshotDispatcher, TmsVdsRegistrationImageService, IncidentImageQueryHandler). Nay dùng chung lớp này,
/// thư mục gốc lấy từ cấu hình để đưa ảnh lên share dùng chung với ảnh VDS.</para>
///
/// <para><b>Quyền share.</b> Mọi thao tác file phải chạy trong <see cref="RunAsync{T}"/>: với đường dẫn UNC cùng share
/// với <c>vds_media_storage_directory</c>, <see cref="VdsImageShareAccess"/> đăng nhập bằng tài khoản trong VdsConfig;
/// đường dẫn local thì chạy thẳng bằng quyền tiến trình. KHÔNG gọi <see cref="RunAsync{T}"/> lồng trong một lần
/// truy cập share khác và KHÔNG bọc lời gọi camera/HTTP — chỉ bọc đúng đoạn đọc/ghi file.</para>
///
/// <para><b>Không còn URL tĩnh.</b> Share không được phục vụ như file tĩnh của site, nên ảnh chỉ trả qua Base64.</para>
/// Created date: 14/10/2026
/// </summary>
public sealed class IncidentImageStorage
{
    private readonly VdsImageShareAccess _shareAccess;

    public IncidentImageStorage(IncidentImageOptions options, IWebHostEnvironment environment, VdsImageShareAccess shareAccess)
    {
        _shareAccess = shareAccess;
        Root = string.IsNullOrWhiteSpace(options.RootDirectory)
            ? Path.Combine(WebRoot(environment), "tms", "incident")
            : options.RootDirectory.Trim();
    }

    /// <summary>Thư mục gốc đã chuẩn hoá (cấu hình hoặc <c>wwwroot/tms/incident</c>).</summary>
    public string Root { get; }

    /// <summary>Thư mục chứa ảnh của một mốc chụp.</summary>
    public string Directory(DateTime captureTime)
        => Path.Combine(Root, captureTime.ToString("yyyyMM"), captureTime.ToString("ddHH"));

    /// <summary>Chạy thao tác file với quyền truy cập thư mục gốc (đăng nhập share nếu là UNC có cấu hình tài khoản).</summary>
    public Task<T> RunAsync<T>(Func<Task<T>> operation, CancellationToken ct)
        => _shareAccess.RunAsync(Root, operation, ct);

    /// <inheritdoc cref="RunAsync{T}"/>
    public Task RunAsync(Func<Task> operation, CancellationToken ct)
        => _shareAccess.RunAsync(Root, async () => { await operation(); return true; }, ct);

    /// <summary>Hai đường dẫn trùng nhau hoặc nằm lồng trong nhau (so theo cả dấu phân cách, không so tiền tố trần).</summary>
    public static bool Overlaps(string first, string second)
    {
        var a = Normalize(first);
        var b = Normalize(second);
        return a.Equals(b, StringComparison.OrdinalIgnoreCase)
            || a.StartsWith(b + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || b.StartsWith(a + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string path)
        => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    internal static string WebRoot(IWebHostEnvironment environment)
        => string.IsNullOrWhiteSpace(environment.WebRootPath)
            ? Path.Combine(AppContext.BaseDirectory, "wwwroot")
            : environment.WebRootPath;
}
