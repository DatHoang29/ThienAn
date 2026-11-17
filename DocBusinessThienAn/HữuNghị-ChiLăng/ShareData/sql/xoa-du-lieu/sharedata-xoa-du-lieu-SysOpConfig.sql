USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-SysOpConfig.sql
-- BẢNG GHI: [dbo].[SysOpConfig] — tệp chỉ XÓA đúng các tham số của ShareData; bảng và phân hệ khác giữ nguyên (Rule 19.103).
-- DESCRIPTION: Xóa toàn bộ tham số vận hành ShareData (GroupCode = N'sharedata_config') trong SysOpConfig
--              để nạp lại sạch từ tệp 7 (sharedata-seed-cau-hinh-SysOpConfig.sql).
-- THỨ TỰ CHẠY: X12. Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); chỉ xóa GroupCode = N'sharedata_config'.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-SysOpConfig.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysOpConfig]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysOpConfig] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (
    SELECT COUNT(1)
    FROM [dbo].[SysOpConfig]
    WHERE [GroupCode] = N'sharedata_config'
);
PRINT N'-> Số dòng nhóm [sharedata_config] sẽ xóa trong [dbo].[SysOpConfig]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Không có tham số [sharedata_config] trong [dbo].[SysOpConfig].';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM [dbo].[SysOpConfig]
        WHERE [GroupCode] = N'sharedata_config';
        DECLARE @Deleted INT = @@ROWCOUNT;
        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@Deleted AS NVARCHAR(12)) + N' tham số [sharedata_config] khỏi [dbo].[SysOpConfig].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[SysOpConfig]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-SysOpConfig.sql';
PRINT N'=============================================================================';
GO
