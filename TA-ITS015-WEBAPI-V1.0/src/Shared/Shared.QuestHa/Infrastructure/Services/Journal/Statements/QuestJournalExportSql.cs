namespace Shared.QuestHa.Infrastructure.Services.Journal.Statements;

/// <summary>
/// Author: KienLT
/// Description: SQL catalog bản xuất Parquet (<c>dbo.QuestExportCatalog</c>) và truy vấn giờ có dòng journal đến muộn.
/// Cột <c>[Rows]</c> luôn đặt trong ngoặc vuông (ROWS là từ khoá T-SQL).
/// Created date: 02/11/2026
/// </summary>
internal static class QuestJournalExportSql
{
    internal const string ListExports = """
        SELECT ExportId, TableName, DayLocal, FromUtc, ToUtc, Node, WatermarkSeq, [Rows] AS RowTotal, Bytes, Path, Verified, CreatedUtc, RetiredUtc
        FROM dbo.QuestExportCatalog
        WHERE (@table IS NULL OR TableName = @table) AND (@from IS NULL OR DayLocal >= @from) AND (@to IS NULL OR DayLocal <= @to)
        ORDER BY DayLocal DESC, TableName, CreatedUtc DESC;
        """;

    // Thêm bản mới và retire bản cũ của cùng bảng/ngày trong một transaction: không có lúc nào hai bản cùng hiệu lực
    // hoặc không bản nào hiệu lực.
    internal const string AddExport = """
        SET NOCOUNT ON; SET XACT_ABORT ON;
        BEGIN TRAN;
        UPDATE dbo.QuestExportCatalog WITH (UPDLOCK, HOLDLOCK) SET RetiredUtc = SYSUTCDATETIME()
        WHERE TableName = @table AND DayLocal = @day AND RetiredUtc IS NULL;
        INSERT dbo.QuestExportCatalog (ExportId, TableName, DayLocal, FromUtc, ToUtc, Node, WatermarkSeq, [Rows], Bytes, Path, Verified, CreatedUtc)
        VALUES (@id, @table, @day, @from, @to, @node, @seq, @rows, @bytes, @path, @ver, SYSUTCDATETIME());
        COMMIT;
        """;

    internal const string RetireExports = """
        SET NOCOUNT ON;
        UPDATE dbo.QuestExportCatalog SET RetiredUtc = SYSUTCDATETIME()
        WHERE ExportId IN (SELECT CONVERT(uniqueidentifier, value) FROM OPENJSON(@ids)) AND RetiredUtc IS NULL;
        SELECT @@ROWCOUNT;
        """;

    // Giờ (tính từ đầu ngày, theo giây/3600) có dòng journal sau watermark của bản xuất: dữ liệu đến muộn phải xuất lại.
    internal const string LateHours = """
        SELECT DISTINCT CAST(DATEDIFF_BIG(SECOND, @from, DesignatedTs) / 3600 AS int) AS Hour
        FROM dbo.QuestOutbox
        WHERE TargetTable = @table AND DesignatedTs >= @from AND DesignatedTs < @to AND Seq > @after AND Seq <= @max
          AND RowVer < MIN_ACTIVE_ROWVERSION();
        """;
}
