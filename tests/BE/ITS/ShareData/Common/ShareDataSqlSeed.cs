using Module.ShareData.Core.Entities;

namespace Tests.ShareData.Common;

/// <summary>
/// Description: Ghi và đọc câu SQL trích xuất của gói tin trong bảng ShareDataSql cho các bài test
///              (dòng PartnerId trống là câu mẫu chung, dòng có PartnerId là cấu hình riêng của đối tác).
/// </summary>
internal static class ShareDataSqlSeed
{
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
