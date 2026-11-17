USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-SysConfigData.sql
-- BẢNG GHI: [dbo].[SysConfigData] — tệp chỉ XÓA đúng cấu hình của ShareData; phân hệ khác giữ nguyên (Rule 19.103).
-- DESCRIPTION: Xóa 26 mã cảnh báo ShareData (ConfigType 'sharedata_error_code') và mục đăng ký nhóm
--              'sharedata_config' trong 'operation_config_type' khỏi SysConfigData
--              để nạp lại sạch từ tệp 5 (sharedata-seed-cau-hinh-SysConfigData.sql).
-- THỨ TỰ CHẠY: X13 (trước X14 xóa SysConfigType). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL chính chạy lại từ đầu (dev / staging / test). ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); không đụng dữ liệu phân hệ khác.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-SysConfigData.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysConfigData]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysConfigData] chưa tồn tại trên DB ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @ErrorTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID] FROM [dbo].[SysConfigType] WHERE [Code] = N'sharedata_error_code'
);
DECLARE @OpTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID] FROM [dbo].[SysConfigType] WHERE [Code] = N'operation_config_type'
);

DECLARE @SoDong INT = (
    SELECT COUNT(1)
    FROM [dbo].[SysConfigData]
    WHERE (@ErrorTypeId IS NOT NULL AND [ConfigTypeId] = @ErrorTypeId)
       OR (@OpTypeId IS NOT NULL AND [ConfigTypeId] = @OpTypeId AND [Code] = N'sharedata_config')
       OR [Code] LIKE N'ESH-1[2-6]%'
);
PRINT N'-> Số dòng ShareData sẽ xóa trong [dbo].[SysConfigData]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Không có dữ liệu ShareData cần xóa trong [dbo].[SysConfigData].';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        DELETE FROM [dbo].[SysConfigData]
        WHERE (@ErrorTypeId IS NOT NULL AND [ConfigTypeId] = @ErrorTypeId)
           OR (@OpTypeId IS NOT NULL AND [ConfigTypeId] = @OpTypeId AND [Code] = N'sharedata_config')
           OR [Code] LIKE N'ESH-1[2-6]%';
        DECLARE @Deleted INT = @@ROWCOUNT;
        COMMIT TRAN;
        PRINT N'✅ Đã xóa ' + CAST(@Deleted AS NVARCHAR(12)) + N' dòng ShareData khỏi [dbo].[SysConfigData].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[SysConfigData]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-SysConfigData.sql';
PRINT N'=============================================================================';
GO
