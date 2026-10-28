using System.Text.RegularExpressions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Helpers;

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
        ("pageSize", ShareDataConst.SqlVariableTypeInt, ShareDataConst.SqlVariableKindConst, "100"),
        ("lastTime", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableSourceLastTime),
        ("lastKey", ShareDataConst.SqlVariableTypeString, ShareDataConst.SqlVariableKindBuiltin, ShareDataConst.SqlVariableSourceLastKey)
    ];

    /// <summary>
    /// Khai các biến @lastTime / @lastKey mà câu SQL có dùng vào bộ biến của câu đó (partnerId trống = mẫu chung), biến đã khai thì giữ.
    /// </summary>
    public static async Task DeclareStandardVariablesAsync(ISqlSugarClient db, string? packetId, string? partnerId, string? extractSql)
    {
        if (string.IsNullOrWhiteSpace(extractSql))
        {
            return;
        }

        var existing = await GetVariablesAsync(db, packetId, partnerId);
        var rows = StandardVariables
            .Where(s => Regex.IsMatch(extractSql, $@"@{s.Code}\b", RegexOptions.IgnoreCase)
                && !existing.Any(v => string.Equals(v.Code, s.Code, StringComparison.OrdinalIgnoreCase)))
            .Select((s, i) => new SqlVariableItem
            {
                Code = s.Code,
                DataType = s.DataType,
                ValueKind = s.ValueKind,
                Value = s.Value,
                OrderNo = existing.Count + i + 1
            })
            .ToList();
        if (rows.Count > 0)
        {
            await SetVariablesAsync(db, packetId, partnerId, [.. existing, .. rows]);
        }
    }

    /// <summary>
    /// Ghi bộ biến (VariablesJson) vào dòng ShareDataSql của phạm vi (partnerId trống = mẫu chung); chưa có dòng thì tạo dòng chỉ chứa bộ biến.
    /// </summary>
    public static async Task SetVariablesAsync(ISqlSugarClient db, string? packetId, string? partnerId, List<SqlVariableItem> items)
    {
        var row = await db.Queryable<ShareDataSql>()
            .Where(s => s.IsDelete == null && s.PacketId == packetId)
            .WhereIF(partnerId == null, s => s.PartnerId == null)
            .WhereIF(partnerId != null, s => s.PartnerId == partnerId)
            .FirstAsync();
        if (row == null)
        {
            await db.Insertable(new ShareDataSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                PartnerId = partnerId,
                VariablesJson = SqlTextHelper.WriteVariables(items),
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();
            return;
        }

        row.VariablesJson = SqlTextHelper.WriteVariables(items);
        await db.Updateable(row).ExecuteCommandAsync();
    }

    /// <summary>
    /// Đọc bộ biến đã lưu ở dòng ShareDataSql của phạm vi (partnerId trống = mẫu chung).
    /// </summary>
    public static async Task<List<SqlVariableItem>> GetVariablesAsync(ISqlSugarClient db, string? packetId, string? partnerId)
    {
        var json = await db.Queryable<ShareDataSql>()
            .Where(s => s.IsDelete == null && s.PacketId == packetId)
            .WhereIF(partnerId == null, s => s.PartnerId == null)
            .WhereIF(partnerId != null, s => s.PartnerId == partnerId)
            .Select(s => s.VariablesJson)
            .FirstAsync();
        return SqlTextHelper.ReadVariables(json);
    }

    /// <summary>
    /// Ghi (thêm hoặc cập nhật) câu SQL mẫu chung và thiết lập SQL của gói tin.
    /// </summary>
    public static async Task SetCommonAsync(
        ISqlSugarClient db,
        string? packetId,
        string? extractSql,
        string? cursorTimeColumn = null)
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
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();
            return;
        }

        row.ExtractSql = extractSql;
        row.CursorTimeColumn = cursorTimeColumn;
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

    /// <summary>
    /// Ghi khối câu ghi chiều nhận vào dòng mẫu chung của gói (thêm dòng nếu gói chưa có); writeSql trống = gói chỉ gửi.
    /// Giữ nguyên câu SQL gửi đang có ở cùng dòng.
    /// </summary>
    public static async Task SetWriteAsync(ISqlSugarClient db, string? packetId, string? writeSql, string? affectedTables = null)
    {
        if (string.IsNullOrWhiteSpace(packetId))
        {
            return;
        }

        var row = await GetCommonAsync(db, packetId);
        if (row == null)
        {
            await db.Insertable(new ShareDataSql
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketId = packetId,
                WriteSql = writeSql,
                AffectedTables = affectedTables,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();
            return;
        }

        row.WriteSql = writeSql;
        row.AffectedTables = affectedTables;
        await db.Updateable(row).ExecuteCommandAsync();
    }

    /// <summary>
    /// Đọc khối câu ghi chiều nhận ở dòng mẫu chung của gói.
    /// </summary>
    public static async Task<string?> GetWriteSqlAsync(ISqlSugarClient db, string? packetId)
    {
        return (await GetCommonAsync(db, packetId))?.WriteSql;
    }
}
