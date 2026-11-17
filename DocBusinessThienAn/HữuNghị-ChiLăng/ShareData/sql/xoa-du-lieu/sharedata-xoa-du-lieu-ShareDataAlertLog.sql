USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-ShareDataAlertLog.sql
-- BẢNG GHI: [dbo].[ShareDataAlertLog] — tệp chỉ XÓA dữ liệu trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Xóa sạch toàn bộ cảnh báo và lỗi vận hành ShareData (ESH-xxxx).
-- THỨ TỰ CHẠY: X3 (sau X2). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-ShareDataAlertLog.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataAlertLog]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataAlertLog] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (SELECT COUNT(1) FROM [dbo].[ShareDataAlertLog]);
PRINT N'-> Số dòng hiện có trong [dbo].[ShareDataAlertLog]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Bảng [dbo].[ShareDataAlertLog] đang trống, không cần xóa.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(N'[dbo].[ShareDataAlertLog]'))
            DELETE FROM [dbo].[ShareDataAlertLog];
        ELSE
            TRUNCATE TABLE [dbo].[ShareDataAlertLog];
        COMMIT TRAN;
        PRINT N'✅ Đã xóa sạch ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng khỏi [dbo].[ShareDataAlertLog].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[ShareDataAlertLog]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-ShareDataAlertLog.sql';
PRINT N'=============================================================================';
GO
