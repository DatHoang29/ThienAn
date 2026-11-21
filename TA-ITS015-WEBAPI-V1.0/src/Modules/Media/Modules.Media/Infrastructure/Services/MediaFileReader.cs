using Microsoft.AspNetCore.Hosting;
using Modules.Media.Infrastructure.Helpers;
using Shared.Utility.Helpers;

namespace Modules.Media.Infrastructure.Services;

/// <summary>
/// Description: [ITS] Đọc file ảnh trong wwwroot theo đường dẫn lưu trong DB
/// (BaseHelper.FileHp.FileMimeHelper: TryGetPathInRoot chặn thoát khỏi wwwroot, IsImage/GetByExtension lấy content-type).
/// URL tuyệt đối (http/https) trả Status = External để FE dùng thẳng.
/// Created date: 08/10/2026
/// </summary>
public sealed class MediaFileReader
{
    private const long MaxFileBytes = 10 * 1024 * 1024;

    private readonly IWebHostEnvironment _env;

    public MediaFileReader(IWebHostEnvironment env) => _env = env;

    /// <summary>Đọc file vào <paramref name="output"/> (Exists/Status/ContentType/Base64/Url).</summary>
    public async Task<T> ReadAsync<T>(string? rawPath, T output, CancellationToken ct)
        where T : MediaBaseOutput
    {
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            output.Status = MediaConst.Status.MissingSource;
            return output;
        }

        var raw = rawPath.Trim();
        if (MediaHelper.IsExternal(raw))
        {
            output.Status = MediaConst.Status.External;
            output.Url = raw;
            return output;
        }

        var webRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        if (!BaseHelper.FileHp.FileMimeHelper.IsImage(Path.GetExtension(raw))
            || !BaseHelper.FileHp.FileMimeHelper.TryGetPathInRoot(webRoot, raw, out var fullPath))
        {
            output.Status = MediaConst.Status.NotFound;
            return output;
        }

        var file = new FileInfo(fullPath);
        if (!file.Exists || file.Length == 0 || file.Length > MaxFileBytes)
        {
            output.Status = MediaConst.Status.MissingSource;
            return output;
        }

        output.Exists = true;
        output.Status = MediaConst.Status.Ready;
        output.ContentType = BaseHelper.FileHp.FileMimeHelper.GetByExtension(file.Extension);
        output.Base64 = Convert.ToBase64String(await File.ReadAllBytesAsync(file.FullName, ct));
        return output;
    }
}
