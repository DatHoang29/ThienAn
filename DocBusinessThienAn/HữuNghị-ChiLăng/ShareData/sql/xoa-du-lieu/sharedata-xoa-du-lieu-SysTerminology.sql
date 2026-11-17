USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-SysTerminology.sql
-- BẢNG GHI: [dbo].[SysTerminology] — tệp chỉ XÓA đúng các khóa dịch thuật của ShareData; phân hệ khác giữ nguyên (Rule 19.103).
-- DESCRIPTION: Xóa toàn bộ bản dịch đa ngôn ngữ thuộc phân hệ ShareData (Code LIKE N'sharedata.%' hoặc
--              khóa cũ lz.validation%lookbackDaysRange) khỏi SysTerminology để nạp lại sạch từ tệp 10
--              (sharedata-dich-thuat-SysTerminology.sql). Xóa xong và nạp lại nhớ bấm 🔄 Ngôn ngữ ở giao diện.
-- THỨ TỰ CHẠY: X15. Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); không đụng thuật ngữ phân hệ khác.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-SysTerminology.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysTerminology]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysTerminology] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (
    SELECT COUNT(1)
    FROM [dbo].[SysTerminology]
    WHERE [Code] LIKE N'sharedata.%'
       OR [Code] LIKE N'lz.validation%lookbackDaysRange'
);
PRINT N'-> Số dòng bản dịch ShareData sẽ xóa trong [dbo].[SysTerminology]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Không có bản dịch ShareData nào cần xóa trong [dbo].[SysTerminology].';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM [dbo].[SysTerminology]
        WHERE [Code] LIKE N'sharedata.%'
           OR [Code] LIKE N'lz.validation%lookbackDaysRange';
        DECLARE @Deleted INT = @@ROWCOUNT;
        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@Deleted AS NVARCHAR(12)) + N' dòng bản dịch ShareData khỏi [dbo].[SysTerminology].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[SysTerminology]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-SysTerminology.sql';
PRINT N'=============================================================================';
GO
