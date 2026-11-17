USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-SysConfigType.sql
-- BẢNG GHI: [dbo].[SysConfigType] — tệp chỉ XÓA đúng loại danh mục của ShareData; phân hệ khác giữ nguyên (Rule 19.103).
-- DESCRIPTION: Xóa loại danh mục 'sharedata_error_code' khỏi SysConfigType
--              để nạp lại sạch từ tệp 4 (sharedata-seed-cau-hinh-SysConfigType.sql).
-- THỨ TỰ CHẠY: X14 (sau X13 xóa SysConfigData). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); không đụng operation_config_type hay loại khác.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-SysConfigType.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysConfigType]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysConfigType] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (
    SELECT COUNT(1)
    FROM [dbo].[SysConfigType]
    WHERE [Code] = N'sharedata_error_code'
);
PRINT N'-> Số dòng [sharedata_error_code] sẽ xóa trong [dbo].[SysConfigType]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Không có [sharedata_error_code] trong [dbo].[SysConfigType].';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM [dbo].[SysConfigType]
        WHERE [Code] = N'sharedata_error_code';
        DECLARE @Deleted INT = @@ROWCOUNT;
        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@Deleted AS NVARCHAR(12)) + N' dòng [sharedata_error_code] khỏi [dbo].[SysConfigType].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[SysConfigType]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-SysConfigType.sql';
PRINT N'=============================================================================';
GO
