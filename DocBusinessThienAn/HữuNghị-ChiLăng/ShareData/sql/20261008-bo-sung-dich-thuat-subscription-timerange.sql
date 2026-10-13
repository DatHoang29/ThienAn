/* ============================================================================
   Bổ sung bản dịch cho khoá lz.validation.sharedata.scheduleTimeRangeInvalid
   Issue F16 số 29 (đảo ngược quyết định "khung giờ qua đêm") - 08/10/2026
   Pham vi: CHI bang SysTerminology, CHI 2 dong (vi-VN, en-US) cua dung khoa duoi day.
   An toan chay lai nhieu lan (idempotent).
   ============================================================================ */

USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @Name NVARCHAR(64)  = N'lz.validation';
DECLARE @Code NVARCHAR(256) = N'sharedata.scheduleTimeRangeInvalid';
DECLARE @LegacyCode NVARCHAR(256) = N'lz.validation.sharedata.scheduleTimeRangeInvalid';

-- Chuẩn hóa lại dòng cũ nếu trước đó đã chạy với Code chứa tiền tố lz.validation.
UPDATE dbo.SysTerminology
SET Name = @Name,
    Code = @Code,
    Remark = ISNULL(Remark, N'ShareData Localization'),
    UpdateTime = GETDATE(),
    UpdateUId = N'superadmin'
WHERE Code = @LegacyCode
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.SysTerminology t2
      WHERE t2.Name = @Name
        AND t2.Code = @Code
        AND t2.Lang = dbo.SysTerminology.Lang
  );

-- vi-VN
IF EXISTS (SELECT 1 FROM dbo.SysTerminology WHERE Name = @Name AND Code = @Code AND Lang = N'vi-VN' AND IsDelete IS NULL)
    UPDATE dbo.SysTerminology
    SET Value = N'Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu.',
        Status = 1,
        UpdateTime = GETDATE(),
        UpdateUId = N'superadmin'
    WHERE Name = @Name AND Code = @Code AND Lang = N'vi-VN' AND IsDelete IS NULL;
ELSE
    INSERT INTO dbo.SysTerminology
        (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (LOWER(CAST(NEWID() AS NVARCHAR(64))), @Code, @Name,
         N'Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu.', N'vi-VN', 100, N'ShareData Localization', 1, N'Default', GETDATE(), N'superadmin');

-- en-US
IF EXISTS (SELECT 1 FROM dbo.SysTerminology WHERE Name = @Name AND Code = @Code AND Lang = N'en-US' AND IsDelete IS NULL)
    UPDATE dbo.SysTerminology
    SET Value = N'End time must be greater than or equal to start time.',
        Status = 1,
        UpdateTime = GETDATE(),
        UpdateUId = N'superadmin'
    WHERE Name = @Name AND Code = @Code AND Lang = N'en-US' AND IsDelete IS NULL;
ELSE
    INSERT INTO dbo.SysTerminology
        (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (LOWER(CAST(NEWID() AS NVARCHAR(64))), @Code, @Name,
         N'End time must be greater than or equal to start time.', N'en-US', 100, N'ShareData Localization', 1, N'Default', GETDATE(), N'superadmin');

PRINT N'Da seed xong khoa: ' + @Name + N'.' + @Code;

/* Doi chieu: phai ra dung 2 dong (vi-VN, en-US). */
SELECT Name, Code, Lang, Value, Status
FROM dbo.SysTerminology
WHERE Name = @Name AND Code = @Code;
