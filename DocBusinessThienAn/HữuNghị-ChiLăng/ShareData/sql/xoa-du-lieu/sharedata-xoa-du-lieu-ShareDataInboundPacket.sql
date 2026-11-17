USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-ShareDataInboundPacket.sql
-- BẢNG GHI: [dbo].[ShareDataInboundPacket] trên CSDL nhận (*_Inbound, ConfigId = ShareDataDB) — chỉ XÓA dữ liệu bảng này (Rule 19.103).
-- DESCRIPTION: Xóa sạch toàn bộ gói tin thô đã tiếp nhận ở chiều nhận để không bị từ chối trùng SerialNbr
--              sau khi xóa / đặt lại ShareDataSubscription ở CSDL chính.
-- THỨ TỰ CHẠY: X16 (trên CSDL nhận DEV_ITS015_Inbound). Chạy trên CSDL không có hậu tố _Inbound sẽ tự bỏ qua.
--              Tắt ShareDataWorker và WebAPI trước khi chạy. Đặt @ChiXem = 1 để xem trước số dòng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL nhận *_Inbound chạy lại từ đầu. ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); ưu tiên TRUNCATE để không phình log giao dịch trên HDD.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-ShareDataInboundPacket.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
   OR OBJECT_ID(N'[dbo].[ShareDataInboundPacket]', N'U') IS NULL
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound) có bảng ShareDataInboundPacket, đang kết nối ' + DB_NAME() + N' — bỏ qua.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng, không xóa dữ liệu

DECLARE @SoDong INT = (SELECT COUNT(1) FROM [dbo].[ShareDataInboundPacket]);
PRINT N'-> Số dòng hiện có trong [dbo].[ShareDataInboundPacket]: ' + CAST(@SoDong AS NVARCHAR(12));

IF @SoDong = 0
    PRINT N'ℹ️ Bảng [dbo].[ShareDataInboundPacket] đang trống, không cần xóa.';
ELSE IF @ChiXem = 1
    PRINT N'ℹ️ @ChiXem = 1: chỉ xem trước, không xóa dữ liệu.';
ELSE
BEGIN
    BEGIN TRY
        BEGIN TRAN;
        IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(N'[dbo].[ShareDataInboundPacket]'))
            DELETE FROM [dbo].[ShareDataInboundPacket];
        ELSE
            TRUNCATE TABLE [dbo].[ShareDataInboundPacket];
        COMMIT TRAN;
        PRINT N'✅ Đã xóa sạch ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng khỏi [dbo].[ShareDataInboundPacket].';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'❌ Lỗi xóa [dbo].[ShareDataInboundPacket]: %s', 16, 1, @ErrMsg);
    END CATCH;
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-ShareDataInboundPacket.sql';
PRINT N'=============================================================================';
GO
