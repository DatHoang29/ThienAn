namespace Tests.ShareData.Common;

using System.Text.RegularExpressions;
using Module.ShareData.Core.Entities;

/// <summary>
/// Description: Đọc và phân tích cú pháp các tệp seed SQL của ShareData (mỗi tệp ghi một bảng, hậu tố tên bảng)
///              để trích xuất chuỗi SQL cấu hình của các gói tin ShareData.
/// </summary>
internal static class SeedScriptReader
{
    private const string PacketSeedFile = "sharedata-seed-goi-tin-ShareDataPacket.sql";
    private const string SqlSeedFile = "sharedata-seed-goi-tin-ShareDataSql.sql";
    private const string SourceTableSeedFile = "sharedata-seed-cau-hinh-ShareDataSourceTable.sql";

    private static readonly Lazy<Dictionary<string, SeedPacketInfo>> Cache = new(LoadAllInternal);
    private static readonly Lazy<IReadOnlyList<SeedWriteSqlInfo>> WriteSqlStatementsCache = new(LoadWriteSqlStatementsInternal);

    /// <summary>
    /// Thông tin gói tin được trích xuất từ kịch bản seed SQL.
    /// </summary>
    public sealed record SeedPacketInfo(
        string Code,
        string ExtractSql,
        string? CursorTimeColumn = null);

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
        string AffectedTables,
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
                $"Không tìm thấy cấu hình trích xuất của gói tin '{normalizedCode}' trong kịch bản seed {SqlSeedFile}.");
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
        var scriptPath = ResolveSqlScriptPath(PacketSeedFile);
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
        var sqlScriptPath = ResolveSqlScriptPath(SqlSeedFile);
        var sqlContent = File.ReadAllText(sqlScriptPath);

        // Câu mẫu chung và cột thời gian của gói đều nằm ở tệp ShareDataSql (nguồn duy nhất, bỏ TemplateSql 27/10/2026)
        var templateRegex = new Regex(
            @"DECLARE @Sql(?<code>\d{3}) NVARCHAR\(MAX\) = N'(?<sql>(?:''|[^'])*)';",
            RegexOptions.Singleline);
        var settingRegex = new Regex(
            @"SET s\.\[ExtractSql\] = @Sql\d{3},\s*s\.\[CursorTimeColumn\]\s*=\s*(?<cursor>NULL|N'[^']*'),.*?WHERE \(p\.\[Code\] = '(?<code>\d{3})'",
            RegexOptions.Singleline);

        var templates = templateRegex.Matches(sqlContent);
        if (templates.Count == 0)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy bất kỳ câu mẫu chung DECLARE @SqlNNN nào trong tệp seed '{sqlScriptPath}'.");
        }

        var settings = settingRegex.Matches(sqlContent)
            .ToDictionary(m => m.Groups["code"].Value, m => m, StringComparer.OrdinalIgnoreCase);

        var dict = new Dictionary<string, SeedPacketInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in templates)
        {
            var code = match.Groups["code"].Value;
            var extractSql = match.Groups["sql"].Value.Replace("''", "'");

            string? cursor = null;
            if (settings.TryGetValue(code, out var setting))
            {
                var cursorRaw = setting.Groups["cursor"].Value;
                cursor = cursorRaw.Equals("NULL", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : cursorRaw.Trim().TrimStart('N').Trim('\'');
            }

            dict[code] = new SeedPacketInfo(code, extractSql, cursor);
        }

        return dict;
    }

    /// <summary>
    /// Dò tệp script SQL của ShareData theo tên tệp, đi ngược từ thư mục chạy test lên gốc kho mã.
    /// </summary>
    private static string ResolveSqlScriptPath(string fileName)
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDir != null)
        {
            var targetFile = Path.Combine(currentDir.FullName, "DocBusinessThienAn", "HữuNghị-ChiLăng", "ShareData", "sql", fileName);
            if (File.Exists(targetFile))
            {
                return targetFile;
            }

            var altFile = Path.Combine(currentDir.FullName, "ShareData", "sql", fileName);
            if (File.Exists(altFile))
            {
                return altFile;
            }

            currentDir = currentDir.Parent;
        }

        throw new FileNotFoundException(
            $"Không tìm thấy tệp seed {fileName} khi dò từ thư mục gốc '{AppContext.BaseDirectory}'.");
    }

    /// <summary>
    /// Đọc danh mục 15 bảng nguồn từ script sharedata-seed-cau-hinh-ShareDataSourceTable.sql.
    /// </summary>
    public static List<ShareDataSourceTable> ReadSourceTables()
    {
        var scriptPath = ResolveSqlScriptPath(SourceTableSeedFile);
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

    /// <summary>
    /// Description: Đọc danh sách 18 câu ghi chiều nhận từ kịch bản seed 02.
    /// </summary>
    public static IReadOnlyList<SeedWriteSqlInfo> ReadWriteSqlStatements()
    {
        return WriteSqlStatementsCache.Value;
    }

    private static IReadOnlyList<SeedWriteSqlInfo> LoadWriteSqlStatementsInternal()
    {
        // Khối câu ghi chiều nhận nằm ở mục 6 tệp seed ShareDataSql (gộp tệp ShareDataPacketSql cũ 27/10/2026)
        var scriptPath = ResolveSqlScriptPath(SqlSeedFile);
        var content = File.ReadAllText(scriptPath);

        var regex = new Regex(
            @"INSERT\s+INTO\s+@SeedWrites\s*\([^\)]*\)\s*VALUES\s*\(\s*'(?<packetCode>[^']+)',\s*N'(?<affectedTables>[^']+)',\s*N'(?<writeSql>(?:''|[^'])*)'\);",
            RegexOptions.Singleline);

        var matches = regex.Matches(content);
        if (matches.Count != 9)
        {
            throw new InvalidOperationException(
                $"Kỳ vọng 9 khối câu ghi chiều nhận trong kịch bản seed 02, nhưng tìm thấy {matches.Count} khối.");
        }

        var list = new List<SeedWriteSqlInfo>(matches.Count);
        foreach (Match match in matches)
        {
            var packetCode = match.Groups["packetCode"].Value;
            var affectedTables = match.Groups["affectedTables"].Value;
            var tableName = affectedTables.Split(',')[0].Trim();
            var writeSql = match.Groups["writeSql"].Value.Replace("''", "'");

            list.Add(new SeedWriteSqlInfo(packetCode, tableName, affectedTables, 1, writeSql));
        }

        return list;
    }
}
