USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-xoa-du-lieu-BangDichInboundDB.sql
-- BẢNG GHI: 14 bảng đích chiều nhận trong CSDL nhận riêng (*_Inbound):
--             [dbo].[TmsZoneStatus], [dbo].[TmsTrafficStatistic], [dbo].[TmsZone],
--             [dbo].[CctvDevice], [dbo].[TmsTrafficData], [dbo].[TmsWeather],
--             [dbo].[TmsVehicleRegistration], [dbo].[TollTransactionOut],
--             [dbo].[TmsIncident], [dbo].[TmsEventType], [dbo].[VmsCurrent],
--             [dbo].[TmsEquipment], [dbo].[TollLane], [dbo].[TollStation]
--           Ngoại lệ Rule 19.103 (một tệp nhiều bảng): cùng lý do với N1 (sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql),
--           tất cả là 14 bảng đích không có entity nằm riêng trong CSDL nhận *_Inbound; tách 14 tệp thì quá vụn.
-- DESCRIPTION: Xóa sạch dữ liệu nghiệp vụ đã nhận vào 14 bảng đích trên CSDL *_Inbound để chạy lại sạch từ đầu.
--              ⛔ KHÔNG bao giờ chạy trên CSDL chính DEV_ITS015 (chốt chặn IF DB_NAME() NOT LIKE N'%[_]Inbound').
-- THỨ TỰ CHẠY: X17 (sau X16, trên CSDL nhận DEV_ITS015_Inbound). Tắt ShareDataWorker và WebAPI trước khi chạy.
--              Đặt @ChiXem = 1 để xem trước số dòng từng bảng sẽ xóa.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL nhận *_Inbound chạy lại từ đầu. ⛔ Cẩn trọng trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); ưu tiên TRUNCATE để không phình log giao dịch trên HDD.
-- CẬP NHẬT LẦN CUỐI: 17/11/2026
-- =========================================================================================

SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-xoa-du-lieu-BangDichInboundDB.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound), đang kết nối ' + DB_NAME() + N' — bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

DECLARE @ChiXem BIT = 0; -- 1 = chỉ thống kê số dòng từng bảng, không xóa dữ liệu

DECLARE @Bang TABLE ([Stt] INT PRIMARY KEY, [TenBang] SYSNAME);
INSERT INTO @Bang ([Stt], [TenBang]) VALUES
    (1,  N'TmsZoneStatus'),
    (2,  N'TmsTrafficStatistic'),
    (3,  N'TmsZone'),
    (4,  N'CctvDevice'),
    (5,  N'TmsTrafficData'),
    (6,  N'TmsWeather'),
    (7,  N'TmsVehicleRegistration'),
    (8,  N'TollTransactionOut'),
    (9,  N'TmsIncident'),
    (10, N'TmsEventType'),
    (11, N'VmsCurrent'),
    (12, N'TmsEquipment'),
    (13, N'TollLane'),
    (14, N'TollStation');

DECLARE @Stt INT = 0, @TenBang SYSNAME, @DoiTuong NVARCHAR(300), @Sql NVARCHAR(MAX), @SoDong INT;

BEGIN TRY
    IF @ChiXem = 0
        BEGIN TRAN;

    WHILE 1 = 1
    BEGIN
        SELECT TOP 1 @Stt = [Stt], @TenBang = [TenBang]
        FROM @Bang
        WHERE [Stt] > @Stt
        ORDER BY [Stt];
        IF @@ROWCOUNT = 0 BREAK;

        SET @DoiTuong = N'[dbo].' + QUOTENAME(@TenBang);
        IF OBJECT_ID(@DoiTuong, N'U') IS NULL
        BEGIN
            PRINT N'-> ' + @TenBang + N': bảng chưa có, bỏ qua.';
            CONTINUE;
        END;

        SET @SoDong = 0;
        SET @Sql = N'SELECT @Cnt = COUNT(1) FROM ' + @DoiTuong + N';';
        EXEC sys.sp_executesql @Sql, N'@Cnt INT OUTPUT', @Cnt = @SoDong OUTPUT;

        IF @SoDong = 0
            PRINT N'-> ' + @TenBang + N': đã trống (0 dòng).';
        ELSE IF @ChiXem = 1
            PRINT N'-> [ChiXem] ' + @TenBang + N': sẽ xóa ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng.';
        ELSE
        BEGIN
            IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [referenced_object_id] = OBJECT_ID(@DoiTuong))
                SET @Sql = N'DELETE FROM ' + @DoiTuong + N';';
            ELSE
                SET @Sql = N'TRUNCATE TABLE ' + @DoiTuong + N';';

            EXEC sys.sp_executesql @Sql;
            PRINT N'✅ ' + @TenBang + N': đã xóa sạch ' + CAST(@SoDong AS NVARCHAR(12)) + N' dòng.';
        END;
    END;

    IF @ChiXem = 0 AND @@TRANCOUNT > 0
        COMMIT TRAN;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(N'❌ Lỗi xóa dữ liệu bảng đích InboundDB: %s', 16, 1, @ErrMsg);
END CATCH;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-xoa-du-lieu-BangDichInboundDB.sql';
PRINT N'=============================================================================';
GO
