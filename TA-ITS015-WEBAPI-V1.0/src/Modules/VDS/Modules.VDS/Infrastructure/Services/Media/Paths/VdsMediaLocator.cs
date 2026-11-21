using Modules.VDS.Core.Constants;
using Shared.Utility.Utilities;
using Shared.Utility.Counting;

namespace Modules.VDS.Infrastructure.Services.Media.Paths;

/// <summary>
/// Author: KienLT
/// Description: Dựng lại đường dẫn file media mà worker đã ghi. Không có bảng media nào: đường dẫn là TẤT
/// ĐỊNH theo mốc UTC + camera + loại bản tin, nên bên đọc chỉ việc ghép đúng chuỗi.
///
/// <para>Bộ mã nhánh media và hậu tố tên file nằm ở <see cref="VdsMediaKindConst"/> — cùng một nguồn với danh
/// sách nhánh mà API chi tiết sự kiện trả ra.</para>
/// Created date: 21/09/2026
/// </summary>
public static class VdsMediaLocator
{
    public static readonly IReadOnlyDictionary<string, string> Extensions = new Dictionary<string, string> {
        [".jpeg"] = "image/jpeg", [".jpg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp",
        [".bmp"] = "image/bmp", [".mp4"] = "video/mp4", [".mpeg"] = "video/mpeg", [".avi"] = "video/x-msvideo", [".mov"] = "video/quicktime"
    };

    /// <summary>Hậu tố tên file của một nhánh media; ném <c>ArgumentException</c> nếu nhánh không hợp lệ.</summary>
    public static string Suffix(string kind) => VdsMediaKindConst.SuffixOf(kind);

    public static string CameraFolder(string? camera)
    {
        return Modules.VDS.Core.Helper.VdsBaseHelper.MediaPathHp.CameraFolder(camera);
    }

    public static string? Find(string root, DateTime time, string? camera, string? eventType, string kind)
    {
        var utc = BaseUtil.TimeServer.ToUtc(time); var suffix = Suffix(kind);
        var branch = Modules.VDS.Core.Helper.VdsBaseHelper.MediaPathHp.Branch(eventType);
        var hour = Path.Combine(Path.GetFullPath(root), branch, utc.ToString("yyyyMM"), utc.ToString("ddHH"));
        var stem = $"{utc:yyyyMMddHHmmssfff}_{suffix}";
        // Never search another camera: equal millisecond timestamps are valid across cameras.
        foreach (var directory in new[] { Path.Combine(hour, CameraFolder(camera)), hour })
            foreach (var extension in Extensions.Keys.Where(x => suffix == VdsMediaKindConst.ClipSuffix ? Extensions[x].StartsWith("video/") : Extensions[x].StartsWith("image/")))
            {
                var path = Path.Combine(directory, stem + extension);
                if (File.Exists(path)) return path;
            }
        return null;
    }
}
