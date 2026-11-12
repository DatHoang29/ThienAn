USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-TmsTrafficStatistic.sql
-- BẢNG GHI: [dbo].[TmsTrafficStatistic] (bảng nguồn của gói 101 chiều gửi) — chỉ đổi CẤU TRÚC (thêm / gỡ chỉ mục),
--           không ghi dữ liệu (Rule 19.103).
-- DESCRIPTION: Câu mẫu gói 101 (TmsZoneStatus) lấy số xe theo khu vực bằng:
--                  LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
--              Bảng không có chỉ mục nào chứa [ZoneId] nên mỗi lượt quét toàn bảng qua Table Spool.
--              Đo trên staging 12/11/2026 (sys.dm_exec_query_stats + query plan):
--                - TmsTrafficStatistic 448.939 dòng (448.929 dòng ZoneId = NULL), TmsZoneStatus 299 dòng;
--                - câu gói 101: 331 lượt, tổng CPU 403 giây, đọc trung bình 1.091.005 trang/lượt;
--                - plan: Clustered Index Scan TmsZoneStatus -> Sort -> Table Spool + Clustered Index Scan
--                  PK_TmsTrafficStatistic_ID; SQL Server gợi ý Missing Index (Impact 90,1):
--                  TmsTrafficStatistic (ZoneId) INCLUDE (TotalVehicleNumber).
--              Tệp này thêm chỉ mục [IX_TmsTrafficStatistic_ZoneId]:
--                  khóa ([ZoneId]), INCLUDE ([TotalVehicleNumber])
--              để phép nối theo ZoneId đi Index Seek, không còn quét bảng thống kê ở mỗi lượt gửi.
--              Không đổi câu SQL gói tin, không đổi mã nguồn.
--              Ngoại lệ Rule 19.85 (script tạo chỉ mục cho bảng có entity): SqlSugar CodeFirst không khai được INCLUDE.
-- THỨ TỰ CHẠY: Chạy độc lập, sau CodeFirst (bảng phải có sẵn).
--              GỠ: đặt @GoChiMuc = 1 rồi chạy lại.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL vận hành có bảng TmsTrafficStatistic.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đã có chỉ mục thì bỏ qua; gỡ khi không có thì bỏ qua.
--           Dựng chỉ mục: bản SQL Server hỗ trợ thì dùng ONLINE = ON (đọc ghi vẫn chạy); bản khác thì bảng bị chặn GHI
--           cho tới khi dựng xong. Chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222, khi đó chạy lại tệp.
-- CẬP NHẬT LẦN CUỐI: 12112026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-TmsTrafficStatistic.sql trên DB: ' + DB_NAME();
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[TmsTrafficStatistic]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[TmsTrafficStatistic] chưa tồn tại trên DB ' + DB_NAME() + N'!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @GoChiMuc BIT = 0;                            -- 1 = gỡ chỉ mục
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây)

DECLARE @TenChiMuc SYSNAME = N'IX_TmsTrafficStatistic_ZoneId';
DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;
DECLARE @CoChiMuc BIT = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes
                                          WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsTrafficStatistic]') AND [name] = @TenChiMuc)
                             THEN 1 ELSE 0 END;
DECLARE @Sql NVARCHAR(MAX);

BEGIN TRY
    IF @GoChiMuc = 1
    BEGIN
        IF @CoChiMuc = 1
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMuc) + N' ON [dbo].[TmsTrafficStatistic];';
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

        -- Thêm: phép nối gói 101 (zs.ZoneId = ts.ZoneId) đang quét toàn bảng, đây là chỉ mục SQL Server gợi ý (Impact 90,1).
        SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMuc) + N'
    ON [dbo].[TmsTrafficStatistic] ([ZoneId] ASC)
    INCLUDE ([TotalVehicleNumber])'
            + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
        EXEC sys.sp_executesql @Sql;
        PRINT N'-> Đã tạo chỉ mục ' + @TenChiMuc + N'.';
    END;
END TRY
BEGIN CATCH
    PRINT N'❌ Lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
    IF ERROR_NUMBER() = 1222
        PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Dừng ShareDataWorker rồi chạy lại tệp.';
END CATCH;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ
-- -----------------------------------------------------------------------------------------
SELECT i.[name] AS [ChiMuc], i.[type_desc] AS [Loai],
       (SELECT SUM(p.[rows]) FROM sys.partitions p WHERE p.[object_id] = i.[object_id] AND p.[index_id] = i.[index_id]) AS [SoDong]
FROM sys.indexes i
WHERE i.[object_id] = OBJECT_ID(N'[dbo].[TmsTrafficStatistic]')
ORDER BY i.[index_id];
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-TmsTrafficStatistic.sql';
PRINT N'=============================================================================';
GO
