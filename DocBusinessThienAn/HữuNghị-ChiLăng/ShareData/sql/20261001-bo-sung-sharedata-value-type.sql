/* ============================================================================
   Bổ sung kiểu dữ liệu cho danh mục 'sharedata_value_type'
   Issue F16 số 7 và 25 - TuyenHTN ghi nhan 29/09/2026
   Ngay soan: 01/10/2026
   Pham vi: CHI bang SysConfigData, CHI cac dong thuoc dung 1 ConfigTypeId ben duoi.
   An toan chay lai nhieu lan (idempotent).
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @ConfigTypeId NVARCHAR(64);

SELECT @ConfigTypeId = ID
FROM   SysConfigType
WHERE  Code = 'sharedata_value_type'
  AND  IsDelete IS NULL;

IF @ConfigTypeId IS NULL
BEGIN
    RAISERROR(N'Khong tim thay SysConfigType.Code = sharedata_value_type. Dung lai, khong them gi.', 16, 1);
    RETURN;
END

/* Bang tam chua cac kieu can co, kem thu tu hien thi.
   4 dong cu deu OrderNo = 100, nen kieu moi dung so lon hon de xep sau. */
DECLARE @WantedTypes TABLE (TypeCode NVARCHAR(64), SortNo INT);

INSERT INTO @WantedTypes (TypeCode, SortNo) VALUES
    (N'long',    110),
    (N'float',   120),
    (N'double',  130),
    (N'decimal', 140),
    (N'guid',    150);

INSERT INTO SysConfigData
    (ID, ConfigTypeId, Name, Value, TagType, StyleSetting, ClassSetting,
     OrderNo, Remark, Status, TenantId, Code, CreateTime, CreateUId)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))), @ConfigTypeId, w.TypeCode, NULL, N'primary', NULL, NULL,
    w.SortNo, NULL, 1, N'Default', w.TypeCode, GETDATE(), N'superadmin'
FROM   @WantedTypes w
WHERE  NOT EXISTS (
           SELECT 1
           FROM   SysConfigData d
           WHERE  d.ConfigTypeId = @ConfigTypeId
             AND  d.Code         = w.TypeCode
       );

PRINT N'So dong vua them: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* Doi chieu: phai ra dung 9 dong. */
SELECT  d.Code, d.Name, d.OrderNo, d.Status, d.TagType, d.TenantId, d.CreateTime
FROM    SysConfigData d
WHERE   d.ConfigTypeId = @ConfigTypeId
ORDER BY d.OrderNo, d.Code;
