namespace Tests.ShareData.Common;

using System.Text.RegularExpressions;
using Module.ShareData.Core.Entities;

/// <summary>
/// Description: Đọc và phân tích cú pháp tệp seed SQL 'sharedata-seed-goi-tin.sql'
///              để trích xuất chuỗi SQL cấu hình của các gói tin ShareData.
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
    /// Thông tin câu ghi chiều nhận trích xuất từ kịch bản seed SQL.
    /// </summary>
    public sealed record SeedWriteSqlInfo(
        string PacketCode,
        string TableName,
        int OrderNo,
        string WriteSql);

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
                $"Không tìm thấy cấu hình trích xuất của gói tin '{normalizedCode}' trong kịch bản seed sharedata-seed-goi-tin.sql.");
        }

        return info.ExtractSql;
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
            @"SET \[TemplateSql\] = N'(?<sql>.*?)'\s*WHERE \(\[Code\] = '(?<code>\d{3})'.*?SET \[ExtractSql\] = \[TemplateSql\],\s*\[CursorTimeColumn\]\s*=\s*(?<cursor>NULL|N'[^']*'),\s*\[MaxLookBackDays\]\s*=\s*(?<lookback>NULL|\d+)",
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
            var cursorRaw = match.Groups["cursor"].Value;
            var lookbackRaw = match.Groups["lookback"].Value;
            var code = match.Groups["code"].Value;

            var extractSql = rawSql.Replace("''", "'");

            string? cursor = cursorRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                ? null
                : cursorRaw.Trim().TrimStart('N').Trim('\'');

            int? lookback = lookbackRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                ? null
                : int.Parse(lookbackRaw);

            dict[code] = new SeedPacketInfo(code, extractSql, cursor, lookback);
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
                "sharedata-seed-goi-tin.sql");

            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            var legacyFile = Path.Combine(
                currentDir.FullName,
                "DocBusinessThienAn",
                "HữuNghị-ChiLăng",
                "ShareData",
                "sql",
                "02-sharedata-seed-goi-tin.sql");

            if (File.Exists(legacyFile))
            {
                return legacyFile;
            }

            var altFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "sharedata-seed-goi-tin.sql");

            if (File.Exists(altFile))
            {
                return altFile;
            }

            var altLegacyFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "02-sharedata-seed-goi-tin.sql");

            if (File.Exists(altLegacyFile))
            {
                return altLegacyFile;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            $"Không tìm thấy tệp seed sharedata-seed-goi-tin.sql khi dò từ thư mục gốc '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Đọc danh mục 15 bảng nguồn từ script sharedata-seed-cau-hinh.sql (mục 3.4).
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
                "sharedata-seed-cau-hinh.sql");

            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            var legacyFile = Path.Combine(
                currentDir.FullName,
                "DocBusinessThienAn",
                "HữuNghị-ChiLăng",
                "ShareData",
                "sql",
                "03-sharedata-seed-cau-hinh.sql");

            if (File.Exists(legacyFile))
            {
                return legacyFile;
            }

            var altFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "sharedata-seed-cau-hinh.sql");

            if (File.Exists(altFile))
            {
                return altFile;
            }

            var altLegacyFile = Path.Combine(
                currentDir.FullName,
                "ShareData",
                "sql",
                "03-sharedata-seed-cau-hinh.sql");

            if (File.Exists(altLegacyFile))
            {
                return altLegacyFile;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            $"Không tìm thấy tệp seed sharedata-seed-cau-hinh.sql khi dò từ thư mục gốc '{AppContext.BaseDirectory}'.");
    }

    private static readonly Lazy<IReadOnlyList<SeedWriteSqlInfo>> WriteSqlStatementsCache = new(LoadWriteSqlStatementsInternal);

    /// <summary>
    /// Description: Đọc danh sách 18 câu ghi chiều nhận từ kịch bản seed 02.
    /// </summary>
    public static IReadOnlyList<SeedWriteSqlInfo> ReadWriteSqlStatements()
    {
        return WriteSqlStatementsCache.Value;
    }

    private static IReadOnlyList<SeedWriteSqlInfo> LoadWriteSqlStatementsInternal()
    {
        var scriptPath = ResolveSeedScriptPath();
        var content = File.ReadAllText(scriptPath);

        var regex = new Regex(
            @"INSERT\s+INTO\s+\[dbo\]\.\[ShareDataPacketSql\][^\)]*\)\s*VALUES\s*\(\s*LOWER\(CONVERT\(NVARCHAR\(64\),\s*NEWID\(\)\)\),\s*'(?<packetCode>[^']+)',\s*'(?<tableName>[^']+)',\s*(?<orderNo>\d+),\s*N'(?<writeSql>.*?)',\s*1,\s*@TenantIdPacketSql,\s*GETDATE\(\),\s*'SHAREDATA-SEED-02'\);",
            RegexOptions.Singleline);

        var matches = regex.Matches(content);
        if (matches.Count != 18)
        {
            throw new InvalidOperationException(
                $"Kỳ vọng 18 câu ghi chiều nhận trong kịch bản seed 02, nhưng tìm thấy {matches.Count} câu.");
        }

        var list = new List<SeedWriteSqlInfo>(matches.Count);
        foreach (Match match in matches)
        {
            var packetCode = match.Groups["packetCode"].Value;
            var tableName = match.Groups["tableName"].Value;
            var orderNo = int.Parse(match.Groups["orderNo"].Value);
            var writeSql = match.Groups["writeSql"].Value.Replace("''", "'");

            list.Add(new SeedWriteSqlInfo(packetCode, tableName, orderNo, writeSql));
        }

        return list;
    }
}
