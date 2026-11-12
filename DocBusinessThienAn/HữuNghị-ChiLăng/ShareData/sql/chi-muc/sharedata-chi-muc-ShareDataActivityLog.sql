USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-ShareDataActivityLog.sql
-- BẢNG GHI: [dbo].[ShareDataActivityLog] — chỉ đổi CẤU TRÚC (thêm / gỡ 1 chỉ mục), không ghi dữ liệu (Rule 19.103).
--           Ngoại lệ Rule 19.85 (script tạo chỉ mục cho bảng có entity): SugarIndex không khai được INCLUDE.
-- DESCRIPTION: Màn hình Theo dõi chia sẻ > Lịch sử chia sẻ (Nhật ký cấu hình, Nhật ký truyền nhận) lọc
--                  ParentId IS NULL AND LogType = ? AND OccurredAt BETWEEN ? AND ? AND IsDelete IS NULL
--              cho cả 3 câu: đếm 5 ô thống kê, đếm tổng số dòng, lấy trang 50 dòng. Bảng không có chỉ mục nào trên
--              OccurredAt (chỉ mục index_ShareDataActivityLog_OA khai trên CreateTime) nên mỗi câu quét cả bảng;
--              staging 12/11/2026: 750.955 dòng, 1.332 MB, mỗi câu đọc khoảng 150.000 trang, 9–10 giây.
--              Tệp này:
--                1) Sửa lại chỉ mục [index_ShareDataActivityLog_OA] từ (CreateTime DESC) sang đúng (OccurredAt DESC)
--                   cho khớp với khai báo mới trên Entity ShareDataActivityLog;
--                2) Thêm chỉ mục [IX_ShareDataActivityLog_LogType_ParentId_OccurredAt]:
--                   khóa (LogType, ParentId, OccurredAt), INCLUDE (IsDelete, Action, Success, TransferDirection)
--                   để ba câu trên đi thẳng tới đúng khoảng thời gian của dòng cha; câu đếm không phải đọc bảng.
-- THỨ TỰ CHẠY: Chạy độc lập, sau CodeFirst (bảng phải có sẵn).
--              GỠ: đặt @GoChiMuc = 1 rồi chạy lại.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL vận hành.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): đã có chỉ mục thì bỏ qua; gỡ khi không có thì bỏ qua.
--           Bản SQL Server hỗ trợ thì dựng ONLINE = ON (Worker vẫn ghi nhật ký được); bản khác thì bảng bị chặn GHI
--           cho tới khi dựng xong, nên dừng các dịch vụ ShareDataWorker trước khi chạy.
--           Mỗi lệnh chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222; khi đó chạy lại tệp.
-- CẬP NHẬT LẦN CUỐI: 12112026 (sửa cả index_ShareDataActivityLog_OA sang OccurredAt DESC)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-ShareDataActivityLog.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataActivityLog]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataActivityLog] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @GoChiMuc BIT = 0;                            -- 1 = gỡ chỉ mục
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây)

DECLARE @TenChiMuc SYSNAME = N'IX_ShareDataActivityLog_LogType_ParentId_OccurredAt';
-- Bản hỗ trợ dựng chỉ mục trực tuyến: 3 = Enterprise / Developer / Evaluation, 5 = Azure SQL Database, 8 = Managed Instance.
DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;
DECLARE @CoChiMuc BIT = CASE WHEN EXISTS (SELECT 1 FROM sys.indexes
                                          WHERE [object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]') AND [name] = @TenChiMuc)
                             THEN 1 ELSE 0 END;
DECLARE @Sql NVARCHAR(MAX);
DECLARE @TenChiMucOA SYSNAME = N'index_ShareDataActivityLog_OA';
DECLARE @OaTrenCreateTime BIT = CASE WHEN EXISTS (
    SELECT 1
    FROM sys.indexes i
    JOIN sys.index_columns ic ON ic.[object_id] = i.[object_id] AND ic.[index_id] = i.[index_id] AND ic.[key_ordinal] = 1
    JOIN sys.columns c ON c.[object_id] = ic.[object_id] AND c.[column_id] = ic.[column_id]
    WHERE i.[object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]')
      AND i.[name] = @TenChiMucOA
      AND c.[name] = N'CreateTime'
) THEN 1 ELSE 0 END;

BEGIN TRY
    -- 1. Đồng bộ index_ShareDataActivityLog_OA sang cột OccurredAt DESC (nếu đang nằm nhầm trên CreateTime)
    IF @GoChiMuc = 0
    BEGIN
        IF @OaTrenCreateTime = 1
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMucOA) + N' ON [dbo].[ShareDataActivityLog];';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> Đã gỡ ' + @TenChiMucOA + N' cũ (nằm trên CreateTime).';
        END;

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]') AND [name] = @TenChiMucOA)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMucOA) + N'
    ON [dbo].[ShareDataActivityLog] ([OccurredAt] DESC)'
                + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> Đã tạo chỉ mục ' + @TenChiMucOA + N' trên cột OccurredAt DESC.';
        END
        ELSE
            PRINT N'-> Chỉ mục ' + @TenChiMucOA + N' đã nằm đúng trên cột OccurredAt, bỏ qua.';
    END;

    -- 2. Chỉ mục phủ (LogType, ParentId, OccurredAt) INCLUDE (IsDelete, Action, Success, TransferDirection)
    IF @GoChiMuc = 1
    BEGIN
        IF @CoChiMuc = 1
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMuc) + N' ON [dbo].[ShareDataActivityLog];';
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

        SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMuc) + N'
    ON [dbo].[ShareDataActivityLog] ([LogType], [ParentId], [OccurredAt])
    INCLUDE ([IsDelete], [Action], [Success], [TransferDirection])'
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
WHERE i.[object_id] = OBJECT_ID(N'[dbo].[ShareDataActivityLog]')
ORDER BY i.[index_id];
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-ShareDataActivityLog.sql';
PRINT N'=============================================================================';
GO
