namespace Modules.TMS.Core.Constants;

/// <summary>
/// Author: KienLT
/// Description: Luật "nguồn phát hiện nào được tự động" của <c>TmsIncidentAutomationConfig.AllowedSources</c> — NƠI DUY NHẤT.
///
/// <para>Trước đây cùng một luật được viết lại ở ba chỗ (lập lịch tự động, tự kết thúc sự kiện không ai nhận,
/// kiểm tra khi lưu cấu hình), và từ 17/10/2026 VDS service cũng dùng luật này để suy danh sách loại sự kiện cần
/// gửi sang TMS. Viết riêng từng nơi thì sớm muộn lệch nhau.</para>
///
/// <para>Luật (giữ đúng thiết kế ban đầu của SonTH, 24/07/2026):
/// <list type="bullet">
/// <item><c>AllowedSources</c> rỗng ⇒ mọi nguồn.</item>
/// <item>Có giá trị ⇒ chỉ nguồn nằm trong danh sách (mã <c>tms_source_detect</c>, cách nhau dấu phẩy, không phân biệt
/// hoa thường, bỏ khoảng trắng).</item>
/// <item>Sự cố không rõ nguồn ⇒ chặn (fail-safe) khi danh sách có giá trị.</item>
/// </list></para>
/// Created date: 17/10/2026
/// </summary>
public static class IncidentAutomationSourceConst
{
    public const string Vds = "vds";
    public const string User = "user";

    public static bool IsAllowed(string? allowedSources, string? incidentSource)
    {
        if (string.IsNullOrWhiteSpace(allowedSources))
            return true;

        if (string.IsNullOrWhiteSpace(incidentSource))
            return false;

        var source = incidentSource.Trim();
        return allowedSources
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(x => string.Equals(x, source, StringComparison.OrdinalIgnoreCase));
    }
}
