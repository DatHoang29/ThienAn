using System.Text.RegularExpressions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;

namespace Tests.ShareData.Common;

/// <summary>
/// Description: Ghi và đọc câu SQL trích xuất của gói tin trong bảng ShareDataSql cho các bài test
///              (dòng PartnerId trống là câu mẫu chung, dòng có PartnerId là cấu hình riêng của đối tác).
/// </summary>
internal static class ShareDataSqlSeed
{
    /// <summary>
    /// Biến câu SQL mẫu cũ hay dùng và cách khai tương ứng ở bảng tham số câu truy vấn
    /// (không còn biến hệ thống: câu SQL dùng biến nào thì bộ biến của câu đó phải có biến đó).
    /// </summary>
    private static readonly (string Code, string DataType, string ValueKind, string Value)[] StandardVariables =
    [
        ("lastTime", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableSourceLastTime),
        ("lastKey", ShareDataConst.SqlVariableTypeString, ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableSourceLastKey),
        ("pageSize", ShareDataConst.SqlVariableTypeInt, ShareDataConst.SqlVariableKindConst, "100")
    ];

    /// <summary>
    /// Khai các biến @lastTime / @lastKey / @pageSize mà câu SQL có dùng vào bộ biến của câu đó (partnerId trống = mẫu chung), biến đã khai thì giữ.
    /// </summary>
    public static async Task DeclareStandardVariablesAsync(ISqlSugarClient db, string? packetId, string? partnerId, string? extractSql)
    {
        if (string.IsNullOrWhiteSpace(extractSql))
        {
            return;
        }

        var existing = await db.Queryable<ShareDataSqlVariable>()
            .Where(v => v.PacketId == packetId)
            .WhereIF(partnerId == null, v => v.PartnerId == null)
            .WhereIF(partnerId != null, v => v.PartnerId == partnerId)
            .Select(v => v.Code)
            .ToListAsync();
        var rows = StandardVariables
            .Where(s => Regex.IsMatch(extractSql, $@"@{s.Code}\b", RegexOptions.IgnoreCase)
                && !existing.Contains(s.Code, StringComparer.OrdinalIgnoreCase))
            .Select((s, i) => new ShareDataSqlVariable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                PartnerId = partnerId,
                Code = s.Code,
                DataType = s.DataType,
                ValueKind = s.ValueKind,
                Value = s.Value,
                OrderNo = i + 1
            })
            .ToList();
        if (rows.Count > 0)
        {
            await db.Insertable(rows).ExecuteCommandAsync();
        }
    }

    /// <summary>
    /// Ghi (thêm hoặc cập nhật) câu SQL mẫu chung và thiết lập SQL của gói tin.
    /// </summary>
    public static async Task SetCommonAsync(
        ISqlSugarClient db,
        string? packetId,
        string? extractSql,
        string? cursorTimeColumn = null,
        int? maxLookBackDays = null)
    {
        if (string.IsNullOrWhiteSpace(packetId))
        {
            return;
        }

        await DeclareStandardVariablesAsync(db, packetId, null, extractSql);

        var row = await GetCommonAsync(db, packetId);
        if (row == null)
        {
            await db.Insertable(new ShareDataSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                ExtractSql = extractSql,
                CursorTimeColumn = cursorTimeColumn,
                MaxLookBackDays = maxLookBackDays,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();
            return;
        }

        row.ExtractSql = extractSql;
        row.CursorTimeColumn = cursorTimeColumn;
        row.MaxLookBackDays = maxLookBackDays;
        await db.Updateable(row).ExecuteCommandAsync();
    }

    /// <summary>
    /// Ghi (thêm hoặc cập nhật) câu SQL riêng của đối tác cho gói tin.
    /// </summary>
    public static async Task<ShareDataSql> SetPartnerAsync(ISqlSugarClient db, string? packetId, string? partnerId, string? extractSql)
    {
        await DeclareStandardVariablesAsync(db, packetId, partnerId, extractSql);

        var row = await db.Queryable<ShareDataSql>()
            .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId == partnerId)
            .FirstAsync();
        if (row == null)
        {
            row = new ShareDataSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                PartnerId = partnerId,
                ExtractSql = extractSql,
                CreateTime = DateTime.Now
            };
            await db.Insertable(row).ExecuteCommandAsync();
            return row;
        }

        row.ExtractSql = extractSql;
        await db.Updateable(row).ExecuteCommandAsync();
        return row;
    }

    /// <summary>
    /// Đọc dòng câu SQL mẫu chung của gói tin.
    /// </summary>
    public static async Task<ShareDataSql?> GetCommonAsync(ISqlSugarClient db, string? packetId)
    {
        return await db.Queryable<ShareDataSql>()
            .Where(s => s.IsDelete == null && s.PacketId == packetId && s.PartnerId == null)
            .FirstAsync();
    }

    /// <summary>
    /// Xoá mọi dòng ShareDataSql của gói tin (dọn dữ liệu test).
    /// </summary>
    public static async Task DeleteForPacketAsync(ISqlSugarClient db, string? packetId)
    {
        await db.Deleteable<ShareDataSql>().Where(s => s.PacketId == packetId).ExecuteCommandAsync();
    }
}
