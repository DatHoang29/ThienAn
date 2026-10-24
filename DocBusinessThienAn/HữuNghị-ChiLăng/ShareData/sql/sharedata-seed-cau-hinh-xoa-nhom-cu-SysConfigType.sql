USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql
-- BẢNG GHI: [dbo].[SysConfigType] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 4.4 Xóa mềm loại danh mục cũ 'sharedata_fe_config' (idempotent).
-- THỨ TỰ CHẠY: 9 (sau sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql';
PRINT N'=============================================================================';

-- 4.4 Xóa mềm SysConfigType của nhóm cũ sharedata_fe_config (idempotent)
UPDATE [dbo].[SysConfigType]
SET [IsDelete] = GETDATE(),
    [Status] = 0,
    [UpdateTime] = GETDATE()
WHERE [Code] = N'sharedata_fe_config'
  AND [IsDelete] IS NULL;

DECLARE @SoftDeletedFeType INT = @@ROWCOUNT;
IF @SoftDeletedFeType > 0
    PRINT N'   -> Đã xóa mềm ' + CAST(@SoftDeletedFeType AS NVARCHAR(10)) + N' dòng SysConfigType của nhóm cũ [sharedata_fe_config].';
ELSE
    PRINT N'   -> Loại danh mục cũ [sharedata_fe_config] đã được xóa mềm hoặc không tồn tại, bỏ qua.';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigType.sql';
PRINT N'=============================================================================';
GO
