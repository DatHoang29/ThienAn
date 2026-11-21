/*
    Author: ChuongLP
    Created date: 19/08/2026

    Mục đích: xóa cột TmsEquipmentEventRule.SuggestionRangeMeters đã NGỪNG DÙNG.

    Bối cảnh: phạm vi gợi ý thiết bị đã chốt lấy từ SysOpConfig (nhóm operation_config,
    mã incident_equipment_suggestion_small_range / _large_range). Cột này không được đọc ở
    bất kỳ nghiệp vụ nào, cũng không có UI cấu hình.

    LƯU Ý - script này XÓA DỮ LIỆU, KHÔNG chạy tự động:
      1. Backup database trước khi chạy.
      2. Chỉ chạy khi đã deploy bản build có property SuggestionRangeMeters bị xóa khỏi entity
         (nếu chạy trước, INSERT rule mới sẽ lỗi vì entity vẫn map cột đã bị xóa).
      3. Sau khi chạy: xóa property SuggestionRangeMeters trong
         Modules.TMS.Core/Entities/TmsEquipmentEventRule.cs.
*/

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[dbo].[TmsEquipmentEventRule]')
      AND name = N'SuggestionRangeMeters'
)
BEGIN
    /* Cột có DEFAULT constraint nên phải drop constraint trước mới drop được cột. */
    DECLARE @defaultConstraintName NVARCHAR(256);

    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'[dbo].[TmsEquipmentEventRule]')
      AND c.name = N'SuggestionRangeMeters';

    IF @defaultConstraintName IS NOT NULL
        EXEC(N'ALTER TABLE [dbo].[TmsEquipmentEventRule] DROP CONSTRAINT [' + @defaultConstraintName + N']');

    ALTER TABLE [dbo].[TmsEquipmentEventRule] DROP COLUMN [SuggestionRangeMeters];
END
GO
