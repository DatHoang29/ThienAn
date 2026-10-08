/* ============================================================================
   Bổ sung bản dịch cho khoá lz.validation.sharedata.scheduleTimeRangeInvalid
   Issue F16 số 29 (đảo ngược quyết định "khung giờ qua đêm") - 08/10/2026
   Pham vi: CHI bang SysTerminology, CHI 2 dong (vi-VN, en-US) cua dung khoa duoi day.
   An toan chay lai nhieu lan (idempotent).
   ============================================================================ */

SET NOCOUNT ON;

DECLARE @Code NVARCHAR(256) = N'lz.validation.sharedata.scheduleTimeRangeInvalid';

-- vi-VN
IF EXISTS (SELECT 1 FROM SysTerminology WHERE Code = @Code AND Lang = N'vi-VN' AND IsDelete IS NULL)
    UPDATE SysTerminology
    SET Value = N'Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu.', Status = 1
    WHERE Code = @Code AND Lang = N'vi-VN' AND IsDelete IS NULL;
ELSE
    INSERT INTO SysTerminology
        (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (LOWER(CAST(NEWID() AS NVARCHAR(64))), @Code, @Code,
         N'Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu.', N'vi-VN', 100, NULL, 1, N'Default', GETDATE(), N'superadmin');

-- en-US
IF EXISTS (SELECT 1 FROM SysTerminology WHERE Code = @Code AND Lang = N'en-US' AND IsDelete IS NULL)
    UPDATE SysTerminology
    SET Value = N'End time must be greater than or equal to start time.', Status = 1
    WHERE Code = @Code AND Lang = N'en-US' AND IsDelete IS NULL;
ELSE
    INSERT INTO SysTerminology
        (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (LOWER(CAST(NEWID() AS NVARCHAR(64))), @Code, @Code,
         N'End time must be greater than or equal to start time.', N'en-US', 100, NULL, 1, N'Default', GETDATE(), N'superadmin');

PRINT N'Da seed xong khoa: ' + @Code;

/* Doi chieu: phai ra dung 2 dong (vi-VN, en-US). */
SELECT Code, Lang, Value, Status FROM SysTerminology WHERE Code = @Code;
