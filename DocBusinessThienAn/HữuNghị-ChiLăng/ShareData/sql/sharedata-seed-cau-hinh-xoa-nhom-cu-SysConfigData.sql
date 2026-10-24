USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql
-- BẢNG GHI: [dbo].[SysConfigData] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 4.4 Xóa mềm các dòng SysConfigData của nhóm cũ 'sharedata_fe_config' (idempotent).
-- THỨ TỰ CHẠY: 8 (sau sharedata-seed-cau-hinh-SysOpConfig.sql) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql';
PRINT N'=============================================================================';

-- 4.4 Xóa mềm các dòng SysConfigData của nhóm cũ sharedata_fe_config (idempotent)
UPDATE d
SET d.[IsDelete] = GETDATE(),
    d.[Status] = 0,
    d.[UpdateTime] = GETDATE()
FROM [dbo].[SysConfigData] d
INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
WHERE t.[Code] = N'sharedata_fe_config'
  AND d.[IsDelete] IS NULL;

DECLARE @SoftDeletedFeData INT = @@ROWCOUNT;
IF @SoftDeletedFeData > 0
    PRINT N'   -> Đã xóa mềm ' + CAST(@SoftDeletedFeData AS NVARCHAR(10)) + N' dòng SysConfigData của nhóm cũ [sharedata_fe_config].';
ELSE
    PRINT N'   -> Dữ liệu nhóm cũ [sharedata_fe_config] đã được xóa mềm hoặc không tồn tại, bỏ qua.';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-xoa-nhom-cu-SysConfigData.sql';
PRINT N'=============================================================================';
GO
