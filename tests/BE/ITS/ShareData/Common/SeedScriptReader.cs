namespace Tests.ShareData.Common;

using System.Text.RegularExpressions;
using Module.ShareData.Core.Entities;

/// <summary>
/// Description: Đọc và phân tích cú pháp tệp seed SQL '02-sharedata-seed-goi-tin.sql'
///              để trích xuất chuỗi SQL cấu hình và SyncMode của các gói tin ShareData.
/// </summary>
internal static class SeedScriptReader
{
    private static readonly Lazy<Dictionary<string, SeedPacketInfo>> Cache = new(LoadAllInternal);

    /// <summary>
    /// Thông tin gói tin được trích xuất từ kịch bản seed SQL.
    /// </summary>
    public sealed record SeedPacketInfo(
        string Code,
        string ExtractSql,
        int SyncMode,
        string? CursorTimeColumn = null,
        int? MaxLookBackDays = null);

    /// <summary>
    /// Thông tin bản ghi INSERT gói tin khởi tạo từ kịch bản seed SQL.
    /// </summary>
    public sealed record SeedInsertedPacketInfo(
        string Id,
        string Code,
        string Name,
        string PacketVersion,
        string Description,
        int OrderNo,
        int Status,
        string? Remark);

    /// <summary>
    /// Description: Lấy chuỗi SQL trích xuất của gói tin tương ứng (101, 102...).
    ///              Ném ngoại lệ InvalidOperationException nếu không tìm thấy.
    /// </summary>
    public static string GetExtractSql(string packetCode)
    {
        if (string.IsNullOrWhiteSpace(packetCode))
        {
            throw new ArgumentException("Mã gói tin không được để trống.", nameof(packetCode));
        }

        var normalizedCode = packetCode.Trim();
        var all = Cache.Value;
        if (!all.TryGetValue(normalizedCode, out var info))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy cấu hình trích xuất của gói tin '{normalizedCode}' trong kịch bản seed 02-sharedata-seed-goi-tin.sql.");
        }

        return info.ExtractSql;
    }

    /// <summary>
    /// Description: Lấy SyncMode (1 = Snapshot, 2 = Incremental) của gói tin tương ứng.
    ///              Ném ngoại lệ InvalidOperationException nếu không tìm thấy.
    /// </summary>
    public static int GetSyncMode(string packetCode)
    {
        if (string.IsNullOrWhiteSpace(packetCode))
        {
            throw new ArgumentException("Mã gói tin không được để trống.", nameof(packetCode));
        }

        var normalizedCode = packetCode.Trim();
        var all = Cache.Value;
        if (!all.TryGetValue(normalizedCode, out var info))
        {
            throw new InvalidOperationException(
                $"Không tìm thấy SyncMode của gói tin '{normalizedCode}' trong kịch bản seed 02-sharedata-seed-goi-tin.sql.");
        }

        return info.SyncMode;
    }

    /// <summary>
    /// Description: Đọc toàn bộ danh sách gói tin được cấu hình trong kịch bản seed.
    /// </summary>
    public static IReadOnlyDictionary<string, SeedPacketInfo> ReadAll()
    {
        return Cache.Value;
    }

    /// <summary>
    /// Description: Đọc danh sách 11 gói tin khởi tạo từ các câu lệnh INSERT trong tệp seed 02.
    /// </summary>
    public static IReadOnlyList<SeedInsertedPacketInfo> ReadInsertedPackets()
    {
        var scriptPath = ResolveSeedScriptPath();
        var content = File.ReadAllText(scriptPath);

        var regex = new Regex(
            @"VALUES\s*\(\s*(?:LOWER\(CONVERT\(NVARCHAR\(64\),\s*NEWID\(\)\)\)|N?'(?<id>[^']+)?'),\s*N'(?<code>[^']+)',\s*N'(?<name>[^']+)',\s*N'(?<version>[^']+)',\s*N'(?<desc>[^']+)',\s*(?<order>\d+),\s*(?<status>\d+),\s*N'(?<remark>[^']*)'",
            RegexOptions.Singleline);

        var matches = regex.Matches(content);
        var list = new List<SeedInsertedPacketInfo>();
        foreach (Match m in matches)
        {
            list.Add(new SeedInsertedPacketInfo(
                m.Groups["id"].Success ? m.Groups["id"].Value : Guid.NewGuid().ToString("N"),
                m.Groups["code"].Value,
                m.Groups["name"].Value,
                m.Groups["version"].Value,
                m.Groups["desc"].Value,
                int.Parse(m.Groups["order"].Value),
                int.Parse(m.Groups["status"].Value),
                m.Groups["remark"].Value));
        }

        return list;
    }

    private static Dictionary<string, SeedPacketInfo> LoadAllInternal()
    {
        var scriptPath = ResolveSeedScriptPath();
        var content = File.ReadAllText(scriptPath);

        var regex = new Regex(
            @"SET \[TemplateSql\] = N'(?<sql>.*?)'\s*WHERE \(\[Code\] = '(?<code>\d{3})'.*?SET \[ExtractSql\] = \[TemplateSql\],\s*\[SyncMode\]\s*=\s*(?<sync>\d+),\s*\[CursorTimeColumn\]\s*=\s*(?<cursor>NULL|N'[^']*'),\s*\[MaxLookBackDays\]\s*=\s*(?<lookback>NULL|\d+)",
            RegexOptions.Singleline);

        var matches = regex.Matches(content);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy bất kỳ khối UPDATE gói tin nào phù hợp trong tệp seed '{scriptPath}'.");
        }

        var dict = new Dictionary<string, SeedPacketInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in matches)
        {
            var rawSql = match.Groups["sql"].Value;
            var syncModeStr = match.Groups["sync"].Value;
            var cursorRaw = match.Groups["cursor"].Value;
            var lookbackRaw = match.Groups["lookback"].Value;
            var code = match.Groups["code"].Value;

            var extractSql = rawSql.Replace("''", "'");
            var syncMode = int.Parse(syncModeStr);

            string? cursor = cursorRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                ? null
                : cursorRaw.Trim().TrimStart('N').Trim('\'');

            int? lookback = lookbackRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                ? null
                : int.Parse(lookbackRaw);

            dict[code] = new SeedPacketInfo(code, extractSql, syncMode, cursor, lookback);
        }

        return dict;
    }

    private static string ResolveSeedScriptPath()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var targetFile = Path.Combine(
                currentDir.FullName,
                "DocBusinessThienAn",
                "HữuNghị-ChiLăng",
                "ShareData",
                "sql",
                "02-sharedata-seed-goi-tin.sql");

            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            var altFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "02-sharedata-seed-goi-tin.sql");

            if (File.Exists(altFile))
            {
                return altFile;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            $"Không tìm thấy tệp seed 02-sharedata-seed-goi-tin.sql khi dò từ thư mục gốc '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Đọc danh mục 15 bảng nguồn từ script 03-sharedata-seed-cau-hinh.sql (mục 3.4).
    /// </summary>
    public static List<ShareDataSourceTable> ReadSourceTables()
    {
        var scriptPath = ResolveScript03Path();
        var content = File.ReadAllText(scriptPath);

        var regex = new Regex(
            @"\(\s*(?:'(?<id>[^']+)',\s*)?N'(?<tableName>[^']+)',\s*N'(?<name>[^']+)',\s*N'(?<packetCodes>[^']*)',\s*(?<order>\d+),\s*(?<tracked>\d+),\s*(?<queryable>\d+),\s*N'(?<alias>[^']*)',\s*N'(?<remark>(?:''|[^'])*)'\s*\)",
            RegexOptions.Singleline);

        var matches = regex.Matches(content);
        var list = new List<ShareDataSourceTable>();
        foreach (Match m in matches)
        {
            list.Add(new ShareDataSourceTable
            {
                ID = m.Groups["id"].Success ? m.Groups["id"].Value : Guid.NewGuid().ToString("N"),
                TableName = m.Groups["tableName"].Value,
                Name = m.Groups["name"].Value,
                PacketCodes = m.Groups["packetCodes"].Value,
                OrderNo = int.Parse(m.Groups["order"].Value),
                IsTracked = m.Groups["tracked"].Value == "1",
                IsQueryable = m.Groups["queryable"].Value == "1",
                DefaultAlias = m.Groups["alias"].Value,
                Remark = m.Groups["remark"].Value.Replace("''", "'")
            });
        }

        return list;
    }

    private static string ResolveScript03Path()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var targetFile = Path.Combine(
                currentDir.FullName,
                "DocBusinessThienAn",
                "HữuNghị-ChiLăng",
                "ShareData",
                "sql",
                "03-sharedata-seed-cau-hinh.sql");

            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            var altFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "03-sharedata-seed-cau-hinh.sql");

            if (File.Exists(altFile))
            {
                return altFile;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            $"Không tìm thấy tệp seed 03-sharedata-seed-cau-hinh.sql khi dò từ thư mục gốc '{AppContext.BaseDirectory}'.");
    }
}
