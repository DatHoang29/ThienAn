using System.Globalization;
using System.Text.RegularExpressions;

namespace Shared.QuestHa.Infrastructure.Services.Export.Rules;

/// <summary>
/// Author: KienLT
/// Description: Luật thuần của bản xuất theo ngày: ngày giờ server [00:00, 24:00) ⇒ khoảng UTC của cột designated timestamp (dữ liệu lưu UTC),
/// chia lượt theo GIỜ (giữ RAM QuestDB thấp ở ngày đỉnh), đường dẫn thư mục/tệp, câu truy vấn <c>/exp</c> và đếm dòng cùng điều kiện.
/// Created date: 02/11/2026
/// </summary>
internal static partial class QuestExportDayRules
{
    /// <summary>Tên file manifest — ghi SAU CÙNG; thư mục không có manifest = bản dở.</summary>
    public const string ManifestFile = "manifest.json";

    /// <summary>Kết quả <c>SHOW CREATE TABLE</c> để tạo lại bảng đúng DDL (timestamp/partition/WAL/DEDUP) khi khôi phục.</summary>
    public const string SchemaFile = "schema.sql";

    /// <summary>Đuôi file tạm khi stream/ghi; dọn ở đầu mỗi lần thử.</summary>
    public const string TempSuffix = ".tmp";

    /// <summary>Tiến độ của phiên bản nháp (giờ đã xong) để lần thử sau dùng lại; xoá khi công bố.</summary>
    public const string ProgressFile = "progress.json";

    /// <summary>Thư mục phiên bản <c>{thư mục ngày}/vNNNN</c>.</summary>
    public static string VersionFolder(string dayFolder, int version) =>
        Path.Combine(dayFolder, "v" + version.ToString("0000", CultureInfo.InvariantCulture));

    /// <summary>Tên thư mục là phiên bản <c>vNNNN</c> (4–9 chữ số, ≥ 1).</summary>
    public static bool TryParseVersion(string name, out int version)
    {
        version = 0;
        return VersionName().IsMatch(name) && int.TryParse(name.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out version) && version > 0;
    }

    /// <summary>Tên file do bản xuất tạo (chứng từ sở hữu khi xoá): <c>HH.parquet</c>, manifest, schema, tiến độ và bản tạm của chúng.</summary>
    public static bool IsOwnedFileName(string name)
    {
        var bare = name.EndsWith(TempSuffix, StringComparison.Ordinal) ? name[..^TempSuffix.Length] : name;
        return bare is ManifestFile or SchemaFile or ProgressFile || HourFileName().IsMatch(bare);
    }

    /// <summary>
    /// Khoảng UTC của ngày <paramref name="day"/> theo múi giờ <paramref name="zone"/>: [00:00 ngày đó, 00:00 ngày sau) giờ server.
    /// 00:00 rơi vào khoảng nhảy giờ (DST) ⇒ thời điểm hợp lệ đầu tiên sau đó; 00:00 lặp hai lần ⇒ lần đầu (xem <see cref="StartUtc"/>).
    /// </summary>
    public static (DateTime FromUtc, DateTime ToUtc) DayRange(DateOnly day, TimeZoneInfo zone) =>
        (StartUtc(day, zone), StartUtc(day.AddDays(1), zone));

    /// <summary>Ngày giờ server chứa thời điểm UTC <paramref name="utc"/>.</summary>
    public static DateOnly DayOf(DateTime utc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone));

    /// <summary>
    /// Các lượt giờ (chỉ số 0.., khoảng UTC) phủ kín [<paramref name="fromUtc"/>, <paramref name="toUtc"/>). Ngày thường 24 lượt;
    /// ngày đổi giờ 23/25 lượt; lượt cuối có thể ngắn hơn 1 giờ khi múi giờ lệch nửa giờ.
    /// </summary>
    public static IReadOnlyList<(int Hour, DateTime FromUtc, DateTime ToUtc)> Hours(DateTime fromUtc, DateTime toUtc)
    {
        if (toUtc <= fromUtc) throw new ArgumentException("Khoảng ngày rỗng.");
        var result = new List<(int, DateTime, DateTime)>();
        var i = 0;
        for (var start = fromUtc; start < toUtc; start = start.AddHours(1), i++)
            result.Add((i, start, start.AddHours(1) < toUtc ? start.AddHours(1) : toUtc));
        return result;
    }

    /// <summary>
    /// Thư mục ngày <c>{root}/{bảng}/{yyyy}/{MM}/{yyyy-MM-dd}</c> (đường dẫn tuyệt đối đã chuẩn hoá); dữ liệu nằm trong các phiên bản
    /// <c>vNNNN</c> bên dưới (xem <see cref="VersionFolder"/>).
    /// </summary>
    public static string Folder(string root, string table, DateOnly day)
    {
        if (!IsSafeTableName(table)) throw new ArgumentException("Tên bảng không hợp lệ cho thư mục xuất.", nameof(table));
        return Path.GetFullPath(Path.Combine(root, table, day.ToString("yyyy", CultureInfo.InvariantCulture),
            day.ToString("MM", CultureInfo.InvariantCulture), day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }

    /// <summary>Tên file giờ <c>HH.parquet</c> — HH là thứ tự giờ kể từ 00:00 giờ server (trùng giờ server khi không đổi giờ).</summary>
    public static string HourFile(int hour) => hour.ToString("00", CultureInfo.InvariantCulture) + ".parquet";

    /// <summary>Mốc thời gian dạng literal QuestDB (UTC, micro giây).</summary>
    public static string QuestTs(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'", CultureInfo.InvariantCulture);

    /// <summary>Điều kiện khoảng trên cột designated: dùng CHUNG cho <c>/exp</c> và <c>SELECT count()</c> để hai bên đếm cùng tập dòng.</summary>
    public static string RangeWhere(string designated, DateTime fromUtc, DateTime toUtc) =>
        $"{Quote(designated)} >= '{QuestTs(fromUtc)}' AND {Quote(designated)} < '{QuestTs(toUtc)}'";

    /// <summary>Câu SELECT của một lượt giờ cho <c>/exp</c>.</summary>
    public static string ExportQuery(string table, string designated, DateTime fromUtc, DateTime toUtc) =>
        $"SELECT * FROM {Quote(table)} WHERE {RangeWhere(designated, fromUtc, toUtc)}";

    /// <summary>Câu đếm cùng điều kiện với <see cref="ExportQuery"/>.</summary>
    public static string CountQuery(string table, string designated, DateTime fromUtc, DateTime toUtc) =>
        $"SELECT count() FROM {Quote(table)} WHERE {RangeWhere(designated, fromUtc, toUtc)}";

    /// <summary>Tên bảng/cột chỉ gồm chữ, số, gạch dưới (không nhận chuỗi tuỳ ý từ manifest/CLI vào SQL hay đường dẫn).</summary>
    public static bool IsSafeTableName(string? name) => name is { Length: > 0 and <= 127 } && SafeName().IsMatch(name);

    /// <summary>Định danh QuestDB trong dấu nháy kép.</summary>
    public static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    /// <summary>Độ lệch UTC của múi giờ tại đầu ngày, dạng <c>+07:00</c> (ghi vào manifest).</summary>
    public static string Offset(DateTime fromUtc, TimeZoneInfo zone)
    {
        var offset = zone.GetUtcOffset(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc));
        return (offset < TimeSpan.Zero ? "-" : "+") + offset.ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Mốc UTC SỚM NHẤT của 00:00 ngày <paramref name="day"/>: 00:00 không tồn tại (DST nhảy tới) ⇒ thời điểm hợp lệ đầu tiên sau đó
    /// (= lúc chuyển giờ); 00:00 xảy ra hai lần (DST lùi lại) ⇒ lần đầu (offset lớn nhất). Dùng chung cho đầu ngày và cuối ngày trước
    /// nên các ngày nối liền, không bỏ sót dòng nào giữa hai ngày.
    /// </summary>
    internal static DateTime StartUtc(DateOnly day, TimeZoneInfo zone)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        while (zone.IsInvalidTime(local)) local = local.AddMinutes(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Max() : zone.GetUtcOffset(local);
        return DateTime.SpecifyKind(local - offset, DateTimeKind.Utc);
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex SafeName();

    [GeneratedRegex("^v[0-9]{4,9}$")]
    private static partial Regex VersionName();

    [GeneratedRegex("^[0-9]{2}\\.parquet$")]
    private static partial Regex HourFileName();
}
