namespace Tests.ShareData.Common;

/// <summary>
/// Description: Cung cấp câu truy vấn mẫu chuẩn của các gói tin 101-111 đọc từ script seed 02,
///              dùng chung cho các bài test thay cho mẫu viết cứng trong mã.
/// </summary>
internal static class PacketTemplateSeed
{
    /// <summary>
    /// Thông tin mẫu chuẩn của một gói tin.
    /// </summary>
    public sealed record PacketTemplateInfo(
        string Code,
        string ExtractSql,
        BaseEnums.SyncMode SyncMode,
        string? CursorTimeColumn,
        int? MaxLookBackDays);

    /// <summary>
    /// Danh sách mẫu chuẩn theo thứ tự mã gói.
    /// </summary>
    public static IReadOnlyList<PacketTemplateInfo> All { get; } = SeedScriptReader.ReadAll().Values
        .OrderBy(x => x.Code, StringComparer.Ordinal)
        .Select(x => new PacketTemplateInfo(
            x.Code,
            x.ExtractSql,
            (BaseEnums.SyncMode)x.SyncMode,
            x.CursorTimeColumn,
            x.MaxLookBackDays))
        .ToList();

    /// <summary>
    /// Lấy mẫu theo mã gói dạng "NNN" hoặc "NNN_xxx".
    /// </summary>
    public static bool TryGet(string packetCode, out PacketTemplateInfo info)
    {
        var prefix = packetCode.Length >= 3 ? packetCode[..3] : packetCode;
        var found = All.FirstOrDefault(x => x.Code == prefix);
        info = found!;
        return found != null && (packetCode.Length == 3 || packetCode[3] == '_');
    }
}

