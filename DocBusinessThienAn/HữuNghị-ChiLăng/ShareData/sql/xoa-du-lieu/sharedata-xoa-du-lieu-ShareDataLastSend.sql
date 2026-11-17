USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-ShareDataLastSend.sql
-- BẢNG GHI: [dbo].[ShareDataLastSend] — tệp chỉ XÓA dữ liệu trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Xóa sạch toàn bộ mốc gửi nối đuôi đang chạy của mọi cặp đối tác × gói tin × nhóm mốc
--              để hệ thống chạy lại từ đầu hoặc từ Mốc bắt đầu sau khi seed lại.
-- THỨ TỰ CHẠY: X1 (đầu tiên trong bộ xóa dữ liệu ShareData). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-ShareDataLastSend.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataLastSend]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataLastSend] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (SELECT COUNT(1) FROM [dbo].[ShareDataLastSend]);
PRINT N'-> Số dòng hiện có trong [dbo].[ShareDataLastSend]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Bảng [dbo].[ShareDataLastSend] đang trống, không cần xóa.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(N'[dbo].[ShareDataLastSend]'))
            DELETE FROM [dbo].[ShareDataLastSend];
        ELSE
            TRUNCATE TABLE [dbo].[ShareDataLastSend];
        COMMIT TRAN;
        PRINT N'✅ Đã xóa sạch ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng khỏi [dbo].[ShareDataLastSend].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[ShareDataLastSend]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-ShareDataLastSend.sql';
PRINT N'=============================================================================';
GO
