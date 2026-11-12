USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-TmsTrafficData-Inbound.sql
-- BẢNG GHI: [dbo].[TmsTrafficData] trên CSDL nhận *_Inbound (bảng đích chiều nhận) — chỉ đổi CẤU TRÚC (thêm / gỡ chỉ mục),
--           không ghi dữ liệu (Rule 19.103).
-- DESCRIPTION: Câu ghi mẫu chung của gói 106_wimData MERGE vào [TmsTrafficData] WITH (HOLDLOCK), so khớp theo
--                  t.DetectTime = s.detectTime AND ISNULL(t.Lane, '') = ... AND ISNULL(t.Location, '') = ...
--              Bảng trên CSDL nhận chỉ có khóa chính [PK_TmsTrafficData] (ID), không có chỉ mục nào trên DetectTime,
--              nên mỗi gói quét toàn bảng và HOLDLOCK giữ khóa dải trên cả vùng quét.
--              Đo trên staging 12/11/2026:
--                - TmsTrafficData (DEV_ITS015_Inbound) 1.113.828 dòng, tăng ~1.250 dòng/phút;
--                - mỗi gói 106 ghi mất 1,6–1,8 giây (Activity Monitor: MERGE TmsTrafficData 1.820 ms/lượt), chiếm ~66%
--                  thời gian chiều nhận; gói 103 MERGE cùng bảng bị chặn khóa (một số gói 2,5 giây);
--                - hàng chờ ShareDataInboundPacket tăng (Pending ~18.900), mỗi nhóm đối tác x gói ~60 phút mới được xử lý lại.
--              Tệp này thêm chỉ mục [IX_TmsTrafficData_DetectTime]:
--                  khóa ([DetectTime]), INCLUDE ([Lane], [Location])
--              để phép so khớp của MERGE đi Index Seek theo DetectTime rồi lọc Lane, Location ngay trên chỉ mục, không
--              quét bảng; khóa dải của HOLDLOCK chỉ còn trên vài dòng cùng DetectTime.
--              Không đổi câu ghi, không đổi mã nguồn.
-- THỨ TỰ CHẠY: Chạy độc lập trên CSDL nhận chứa bảng đích TmsTrafficData (staging: DEV_ITS015_Inbound),
--              sau sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql (bảng phải có sẵn).
--              GỠ: đặt @GoChiMuc = 1 rồi chạy lại.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: CSDL nhận *_Inbound có bảng đích TmsTrafficData.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đã có chỉ mục thì bỏ qua; gỡ khi không có thì bỏ qua.
--           Bản SQL Server hỗ trợ thì dựng ONLINE = ON (Worker nhận vẫn ghi được); bản khác thì bảng bị chặn GHI cho tới
--           khi dựng xong, nên dừng ShareDataWorker vai trò nhận trước khi chạy.
--           Mỗi lệnh chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222; khi đó chạy lại tệp.
-- CẬP NHẬT LẦN CUỐI: 12112026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-TmsTrafficData-Inbound.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[TmsTrafficData] chưa tồn tại trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @GoChiMuc BIT = 0;                            -- 1 = gỡ chỉ mục
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây)

DECLARE @TenChiMuc SYSNAME = N'IX_TmsTrafficData_DetectTime';
DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;
DECLARE @CoChiMuc BIT = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes
                                          WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsTrafficData]') AND [name] = @TenChiMuc)
                             THEN 1 ELSE 0 END;
DECLARE @Sql NVARCHAR(MAX);

BEGIN TRY
    IF @GoChiMuc = 1
    BEGIN
        IF @CoChiMuc = 1
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMuc) + N' ON [dbo].[TmsTrafficData];';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> Đã gỡ chỉ mục ' + @TenChiMuc + N'.';
        END
        ELSE
            PRINT N'-> Không có chỉ mục ' + @TenChiMuc + N', bỏ qua.';
    END
    ELSE IF @CoChiMuc = 1
        PRINT N'-> Chỉ mục ' + @TenChiMuc + N' đã có, bỏ qua.';
    ELSE
    BEGIN
        IF @TrucTuyen = 0
            PRINT N'⚠️ Bản SQL Server này không dựng chỉ mục trực tuyến: bảng bị chặn GHI trong lúc dựng chỉ mục.';

        -- Thêm: MERGE gói 106 so khớp theo DetectTime, Lane, Location đang quét toàn bảng 1,1 triệu dòng dưới HOLDLOCK.
        SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMuc) + N'
    ON [dbo].[TmsTrafficData] ([DetectTime] ASC)
    INCLUDE ([Lane], [Location])'
            + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
        EXEC sys.sp_executesql @Sql;
        PRINT N'-> Đã tạo chỉ mục ' + @TenChiMuc + N'.';
    END;
END TRY
BEGIN CATCH
    PRINT N'❌ Lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
    IF ERROR_NUMBER() = 1222
        PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Dừng ShareDataWorker vai trò nhận rồi chạy lại tệp.';
END CATCH;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ
-- -----------------------------------------------------------------------------------------
SELECT i.[name] AS [ChiMuc], i.[type_desc] AS [Loai],
       (SELECT SUM(p.[rows]) FROM sys.partitions p WHERE p.[object_id] = i.[object_id] AND p.[index_id] = i.[index_id]) AS [SoDong]
FROM sys.indexes i
WHERE i.[object_id] = OBJECT_ID(N'[dbo].[TmsTrafficData]')
ORDER BY i.[index_id];
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-TmsTrafficData-Inbound.sql';
PRINT N'=============================================================================';
GO
