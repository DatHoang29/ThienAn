/*
    Author: HieuNV
    Created date: 26/10/2026

    Mục đích: chuyển câu SQL trích xuất và danh sách trường của gói tin sang ShareDataSql (dòng PartnerId trống = câu mẫu chung).

      1. ShareDataPacket.ExtractSql / CursorTimeColumn / MaxLookBackDays (cột cũ, entity đã bỏ) -> ShareDataSql.
         Gói không có ExtractSql thì lấy câu mẫu TemplateSql.
      2. ShareDataPacketField (bảng cũ, đã bỏ khỏi code) -> ShareDataSql.FieldsJson, giữ phần người dùng đã khai:
         AliasFieldKey -> key, Name -> label, IsRequired -> required, GroupName -> group, Remark -> remark,
         Status tắt -> skip. Kiểu quy về string | number | datetime | bool.
         Kiểu CSDL, cột nguồn và cờ mất khoá do bộ dò điền: mở gói tin, bấm "Dò cột trả về" rồi Lưu.

    Script luỹ đẳng: bước 1 bỏ qua gói đã có dòng mẫu chung; bước 2 chỉ điền dòng mẫu chung chưa có danh sách trường
    và chưa dò lần nào (FieldsJson, DescribedTime đều trống).
    Không xoá bảng ShareDataPacketField và các cột cũ của ShareDataPacket; xoá thủ công sau khi đã kiểm tra.
*/

DECLARE @now DATETIME = GETDATE();

-- 1. Câu SQL mẫu chung
INSERT INTO [dbo].[ShareDataSql]
    (ID, PacketId, PartnerId, ExtractSql, CursorTimeColumn, MaxLookBackDays, FieldsJson, DescribedTime, TenantId, CreateTime)
SELECT
    CONVERT(NVARCHAR(36), NEWID()),
    p.ID,
    NULL,
    COALESCE(NULLIF(LTRIM(RTRIM(p.ExtractSql)), N''), p.TemplateSql),
    p.CursorTimeColumn,
    p.MaxLookBackDays,
    NULL,
    NULL,
    p.TenantId,
    @now
FROM [dbo].[ShareDataPacket] p
WHERE p.IsDelete IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM [dbo].[ShareDataSql] s
      WHERE s.PacketId = p.ID AND s.PartnerId IS NULL AND s.IsDelete IS NULL
  );

-- 2. Danh sách trường từ ShareDataPacketField
UPDATE s
SET s.FieldsJson = (
        SELECT
            f.AliasFieldKey AS [key],
            CASE LOWER(LTRIM(RTRIM(ISNULL(f.Type, N''))))
                WHEN N'number' THEN N'number'
                WHEN N'int' THEN N'number'
                WHEN N'long' THEN N'number'
                WHEN N'decimal' THEN N'number'
                WHEN N'double' THEN N'number'
                WHEN N'datetime' THEN N'datetime'
                WHEN N'date' THEN N'datetime'
                WHEN N'bool' THEN N'bool'
                WHEN N'boolean' THEN N'bool'
                ELSE N'string'
            END AS [type],
            CAST(0 AS BIT) AS [lost],
            CASE WHEN NULLIF(LTRIM(RTRIM(f.Name)), N'') IS NULL OR f.Name = f.AliasFieldKey THEN NULL ELSE LEFT(LTRIM(RTRIM(f.Name)), 128) END AS [label],
            CAST(CASE WHEN f.IsRequired = 1 THEN 1 ELSE 0 END AS BIT) AS [required],
            CAST(CASE WHEN f.Status = 1 OR f.Status IS NULL THEN 0 ELSE 1 END AS BIT) AS [skip],
            NULLIF(LEFT(LTRIM(RTRIM(f.GroupName)), 128), N'') AS [group],
            NULLIF(LEFT(LTRIM(RTRIM(f.Remark)), 256), N'') AS [remark]
        FROM [dbo].[ShareDataPacketField] f
        WHERE f.IsDelete IS NULL
          AND NULLIF(LTRIM(RTRIM(f.AliasFieldKey)), N'') IS NOT NULL
          AND (f.DatatypeId = p.ID OR f.DatatypeId = p.Code)
        ORDER BY f.CreateTime, f.AliasFieldKey
        FOR JSON PATH
    )
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.ID = s.PacketId AND p.IsDelete IS NULL
WHERE s.PartnerId IS NULL
  AND s.IsDelete IS NULL
  AND NULLIF(LTRIM(RTRIM(s.FieldsJson)), N'') IS NULL
  AND s.DescribedTime IS NULL;
