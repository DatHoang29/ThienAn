namespace Tests.ShareData.Common;

using System.Text.RegularExpressions;
using Module.ShareData.Core.Entities;

/// <summary>
/// Description: Đọc và phân tích cú pháp các tệp seed SQL của ShareData (mỗi tệp ghi một bảng, hậu tố tên bảng)
///              để trích xuất chuỗi SQL cấu hình của các gói tin ShareData.
/// </summary>
internal static class SeedScriptReader
{
    private const string SqlSeedFile = "sharedata-seed-goi-tin-ShareDataSql.sql";
    private const string SourceTableSeedFile = "sharedata-seed-cau-hinh-ShareDataSourceTable.sql";
    private const string TerminologySeedFile = "sharedata-dich-thuat-SysTerminology.sql";

    private static readonly Lazy<Dictionary<string, SeedPacketInfo>> Cache = new(LoadAllInternal);
    private static readonly Lazy<IReadOnlyList<SeedWriteSqlInfo>> WriteSqlStatementsCache = new(LoadWriteSqlStatementsInternal);

    /// <summary>
    /// Thông tin gói tin được trích xuất từ kịch bản seed SQL.
    /// </summary>
    public sealed record SeedPacketInfo(
        string Code,
        string ExtractSql);

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

    private static Dictionary<string, SeedPacketInfo> LoadAllInternal()
    {
        var sqlScriptPath = ResolveSqlScriptPath(SqlSeedFile);
        var sqlContent = File.ReadAllText(sqlScriptPath);

        // Câu mẫu chung của gói nằm ở tệp ShareDataSql (nguồn duy nhất, bỏ TemplateSql 27/10/2026)
        var templateRegex = new Regex(
            @"DECLARE @Sql(?<code>\d{3}) NVARCHAR\(MAX\) = N'(?<sql>(?:''|[^'])*)';",
            RegexOptions.Singleline);

        var templates = templateRegex.Matches(sqlContent);
        if (templates.Count == 0)
        {
            throw new InvalidOperationException(
                $"Không tìm thấy bất kỳ câu mẫu chung DECLARE @SqlNNN nào trong tệp seed '{sqlScriptPath}'.");
        }

        var dict = new Dictionary<string, SeedPacketInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in templates)
        {
            var code = match.Groups["code"].Value;
            var extractSql = match.Groups["sql"].Value.Replace("''", "'");
            dict[code] = new SeedPacketInfo(code, extractSql);
        }

        return dict;
    }

    /// <summary>
    /// Description: Lấy nguyên một khối lệnh của tệp seed ShareDataSql từ dòng tiêu đề mục tới dòng GO kế tiếp (không gồm GO),
    ///              để test chạy đúng câu lệnh nằm trong tệp.
    /// Modified date: 02/11/2026
    /// </summary>
    public static string ReadSqlSeedBatch(string heading)
    {
        return ReadScriptBatch(SqlSeedFile, heading);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đọc câu dịch tiếng Việt và tiếng Anh của một mã thông báo (vd lz.validation.sharedata.xxx) trong tệp seed SysTerminology.
    /// Created date: 04/11/2026
    /// </summary>
    public static (string Vi, string En) ReadTerminology(string key)
    {
        var parts = key.Split('.', 3);
        var content = File.ReadAllText(ResolveSqlScriptPath(TerminologySeedFile));
        var match = Regex.Match(
            content,
            $@"\(N'{Regex.Escape($"{parts[0]}.{parts[1]}")}',\s*N'{Regex.Escape(parts[2])}',\s*N'(?<vi>(?:''|[^'])*)',\s*N'(?<en>(?:''|[^'])*)'\)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Không tìm thấy câu dịch của mã {key} trong kịch bản seed {TerminologySeedFile}.");
        }

        return (match.Groups["vi"].Value, match.Groups["en"].Value);
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Đọc bộ biến @&lt;tên&gt; (Vars101, VarsCursor, VarsCursorPage) khai trong tệp seed ShareDataSql, trả về chuỗi JSON đúng như trong tệp.
    /// Created date: 02/11/2026
    /// </summary>
    public static string ReadSeedVariablesJson(string name)
    {
        var content = File.ReadAllText(ResolveSqlScriptPath(SqlSeedFile));
        var match = Regex.Match(content, $@"DECLARE @{Regex.Escape(name)} NVARCHAR\(MAX\) = N'(?<json>[^']*)';");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Không tìm thấy bộ biến @{name} trong kịch bản seed {SqlSeedFile}.");
        }

        return match.Groups["json"].Value;
    }

    /// <summary>
    /// Author: DatHQ
    /// Description: Cắt một khối lệnh của tệp script từ dòng tiêu đề mục tới dòng GO kế tiếp (không gồm GO).
    /// Created date: 02/11/2026
    /// </summary>
    private static string ReadScriptBatch(string fileName, string heading)
    {
        var content = File.ReadAllText(ResolveSqlScriptPath(fileName));
        var start = content.IndexOf(heading, StringComparison.Ordinal);
        if (start < 0)
        {
            throw new InvalidOperationException($"Không tìm thấy mục '{heading}' trong kịch bản {fileName}.");
        }

        var end = Regex.Match(content[start..], @"^GO\s*$", RegexOptions.Multiline);
        return end.Success ? content.Substring(start, end.Index) : content[start..];
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
