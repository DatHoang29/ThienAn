USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-ShareDataActivityLog.sql
-- BẢNG GHI: [dbo].[ShareDataActivityLog] — tệp chỉ XÓA dữ liệu trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Xóa sạch toàn bộ nhật ký hoạt động và nhật ký truyền nhận ShareData.
-- THỨ TỰ CHẠY: X2 (sau X1). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); ưu tiên TRUNCATE để không phình log giao dịch trên HDD.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-ShareDataActivityLog.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataActivityLog]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataActivityLog] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (SELECT COUNT(1) FROM [dbo].[ShareDataActivityLog]);
PRINT N'-> Số dòng hiện có trong [dbo].[ShareDataActivityLog]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Bảng [dbo].[ShareDataActivityLog] đang trống, không cần xóa.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]'))
            DELETE FROM [dbo].[ShareDataActivityLog];
        ELSE
            TRUNCATE TABLE [dbo].[ShareDataActivityLog];
        COMMIT TRAN;
        PRINT N'✅ Đã xóa sạch ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng khỏi [dbo].[ShareDataActivityLog].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[ShareDataActivityLog]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-ShareDataActivityLog.sql';
PRINT N'=============================================================================';
GO
