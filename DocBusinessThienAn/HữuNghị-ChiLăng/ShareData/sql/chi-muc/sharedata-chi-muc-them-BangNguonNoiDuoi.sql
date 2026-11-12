USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-them-BangNguonNoiDuoi.sql
-- BẢNG GHI: chỉ đổi CẤU TRÚC (thêm 1 cột tính toán + 1 chỉ mục), không ghi dữ liệu, trên 6 bảng nguồn của 7 gói nối đuôi:
--             [dbo].[TmsTrafficData]      (gói 103, 106)
--             [dbo].[TmsIncident]         (gói 107)
--             [dbo].[TollTransactionOut]  (gói 109)
--             [dbo].[TmsSignalLog]        (gói 111)
--             [dbo].[TmsZoneStatus]       (gói 101)
--             [dbo].[TmsWeather]          (gói 104)
--           Ngoại lệ Rule 19.103 (một tệp nhiều bảng) và Rule 19.85 (script tạo cột, chỉ mục cho bảng có entity):
--           SqlSugar CodeFirst không tạo được cột tính toán và chỉ mục có INCLUDE.
-- DESCRIPTION: Câu nối đuôi so mốc trên chuỗi ghép CONVERT(varchar(23), COALESCE(<thời gian>), 121) + '_' + ID nên không
--              chỉ mục thường nào dùng được: mỗi lượt quét và sắp xếp cả bảng, bảng lớn thì tràn xuống tempdb.
--              Tệp này thêm vào mỗi bảng:
--                - cột tính toán [CursorKey] (không PERSISTED) bằng ĐÚNG biểu thức mốc của câu mẫu gói đọc bảng đó;
--                - chỉ mục [IX_<TênBảng>_CursorKey] trên cột đó, INCLUDE ([IsDelete]).
--              Bảng đã chạy bản đầu của tệp này (cột [ShareDataCursorKey], chỉ mục [IX_<TênBảng>_ShareDataCursorKey])
--              được gỡ tên cũ trước rồi tạo lại theo tên mới.
--              Câu nối đuôi KHÔNG phải đổi: SQL Server tự khớp biểu thức trong câu với cột tính toán và đi theo chỉ mục,
--              đọc đúng một trang, không còn phép sắp xếp. Câu ghép mốc bằng biểu thức khác thì không dùng được chỉ mục này.
-- THỨ TỰ CHẠY: Chạy độc lập, sau CodeFirst (bảng phải có sẵn). Nên dừng các dịch vụ ShareDataWorker trước khi chạy.
--              GỠ: chạy sharedata-chi-muc-go-BangNguonNoiDuoi.sql.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL vận hành có bảng nguồn.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): bảng đã có cột / chỉ mục thì bỏ qua; bảng chưa tồn tại thì bỏ qua;
--           không còn cột / chỉ mục tên cũ thì bỏ qua bước gỡ.
--           Lệnh thêm cột chỉ đổi metadata nhưng cần khóa bảng trong tích tắc; chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222,
--           khi đó chạy lại tệp (các bảng đã xong được bỏ qua).
--           Dựng chỉ mục: bản SQL Server hỗ trợ thì dùng ONLINE = ON (đọc ghi vẫn chạy); bản khác thì bảng bị chặn GHI
--           cho tới khi dựng xong.
--           Sau khi chạy, phiên ghi vào 6 bảng phải bật QUOTED_IDENTIFIER, ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS,
--           ARITHABORT, CONCAT_NULL_YIELDS_NULL (mặc định của SqlClient / SqlSugar đã bật); phiên không đạt sẽ lỗi 1934.
-- CẬP NHẬT LẦN CUỐI: 12112026 (đổi tên cột mốc ShareDataCursorKey -> CursorKey, tự gỡ cột và chỉ mục tên cũ)
-- =========================================================================================

SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-them-BangNguonNoiDuoi.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @ChiBang SYSNAME = NULL;                      -- NULL = cả 6 bảng; hoặc tên một bảng, vd N'TmsTrafficData'
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây) cho mỗi lệnh

DECLARE @Bang TABLE ([Stt] INT PRIMARY KEY, [TenBang] SYSNAME, [CotThoiGian] NVARCHAR(256));
INSERT INTO @Bang ([Stt], [TenBang], [CotThoiGian]) VALUES
    (1, N'TmsTrafficData',     N'[UpdateTime], [CreateTime], [DetectTime]'),
    (2, N'TmsIncident',        N'[UpdateTime], [CreateTime], [StartDate]'),
    (3, N'TollTransactionOut', N'[UpdateTime], [CreateTime], [TransactionDateTime]'),
    (4, N'TmsSignalLog',       N'[UpdateTime], [CreateTime]'),
    (5, N'TmsZoneStatus',      N'[UpdateTime], [CreateTime]'),
    (6, N'TmsWeather',         N'[UpdateTime], [CreateTime], [TimeDetect]');

DECLARE @CotMoc SYSNAME = N'CursorKey';
DECLARE @CotMocCu SYSNAME = N'ShareDataCursorKey';   -- tên của bản đầu, gỡ nếu còn
-- Bản hỗ trợ dựng chỉ mục trực tuyến: 3 = Enterprise / Developer / Evaluation, 5 = Azure SQL Database, 8 = Managed Instance.
DECLARE @TrucTuyen BIT = CASE WHEN CAST(SERVERPROPERTY('EngineEdition') AS INT) IN (3, 5, 8) THEN 1 ELSE 0 END;

DECLARE @Stt INT = 0, @TenBang SYSNAME, @CotThoiGian NVARCHAR(256), @TenChiMuc SYSNAME, @TenChiMucCu SYSNAME, @DoiTuong NVARCHAR(300), @Sql NVARCHAR(MAX);

IF @TrucTuyen = 0
    PRINT N'⚠️ Bản SQL Server này không dựng chỉ mục trực tuyến: mỗi bảng bị chặn GHI trong lúc dựng chỉ mục.';

WHILE 1 = 1
BEGIN
    SELECT TOP 1 @Stt = [Stt], @TenBang = [TenBang], @CotThoiGian = [CotThoiGian]
    FROM @Bang
    WHERE [Stt] > @Stt AND (@ChiBang IS NULL OR [TenBang] = @ChiBang)
    ORDER BY [Stt];
    IF @@ROWCOUNT = 0 BREAK;

    SET @DoiTuong = N'[dbo].' + QUOTENAME(@TenBang);
    SET @TenChiMuc = N'IX_' + @TenBang + N'_' + @CotMoc;
    SET @TenChiMucCu = N'IX_' + @TenBang + N'_' + @CotMocCu;

    IF OBJECT_ID(@DoiTuong, N'U') IS NULL
    BEGIN
        PRINT N'-> ' + @TenBang + N': bảng chưa tồn tại, bỏ qua.';
        CONTINUE;
    END;

    BEGIN TRY
        -- 0. Gỡ chỉ mục và cột tính toán tên cũ (nếu còn)
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @TenChiMucCu)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMucCu) + N' ON ' + @DoiTuong + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã gỡ chỉ mục tên cũ ' + @TenChiMucCu + N'.';
        END;
        IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @CotMocCu AND [is_computed] = 1)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
ALTER TABLE ' + @DoiTuong + N' DROP COLUMN ' + QUOTENAME(@CotMocCu) + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã gỡ cột tính toán tên cũ ' + @CotMocCu + N'.';
        END;

        -- 1. Cột tính toán
        IF COL_LENGTH(@DoiTuong, @CotMoc) IS NULL
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
ALTER TABLE ' + @DoiTuong + N' ADD ' + QUOTENAME(@CotMoc) + N' AS
    (CONVERT(varchar(23), COALESCE(' + @CotThoiGian + N'), 121) + ''_'' + [ID]);';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã thêm cột tính toán ' + @CotMoc + N'.';
        END
        ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @CotMoc AND [is_computed] = 0)
        BEGIN
            PRINT N'⚠️ ' + @TenBang + N': đã có cột ' + @CotMoc + N' nhưng KHÔNG phải cột tính toán, bỏ qua bảng này.';
            CONTINUE;
        END
        ELSE
            PRINT N'-> ' + @TenBang + N': cột ' + @CotMoc + N' đã có, bỏ qua.';

        -- 2. Chỉ mục trên cột mốc
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @TenChiMuc)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
CREATE NONCLUSTERED INDEX ' + QUOTENAME(@TenChiMuc) + N'
    ON ' + @DoiTuong + N' (' + QUOTENAME(@CotMoc) + N') INCLUDE ([IsDelete])'
                + CASE WHEN @TrucTuyen = 1 THEN N' WITH (ONLINE = ON)' ELSE N'' END + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã tạo chỉ mục ' + @TenChiMuc + N'.';
        END
        ELSE
            PRINT N'-> ' + @TenBang + N': chỉ mục ' + @TenChiMuc + N' đã có, bỏ qua.';
    END TRY
    BEGIN CATCH
        PRINT N'❌ ' + @TenBang + N': lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
        IF ERROR_NUMBER() = 1222
            PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Dừng ShareDataWorker rồi chạy lại tệp.';
    END CATCH;
END;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ
-- -----------------------------------------------------------------------------------------
SELECT b.[TenBang],
       CASE WHEN t.[object_id] IS NULL THEN N'chưa có bảng' ELSE N'có' END AS [Bang],
       CASE WHEN c.[is_computed] = 1 THEN N'có' ELSE N'CHƯA' END AS [CotTinhToan],
       CASE WHEN i.[index_id] IS NOT NULL THEN N'có' ELSE N'CHƯA' END AS [ChiMuc],
       (SELECT SUM(p.[rows]) FROM sys.partitions p WHERE p.[object_id] = t.[object_id] AND p.[index_id] IN (0, 1)) AS [SoDong],
       cc.[definition] AS [BieuThucMoc]
FROM @Bang b
LEFT JOIN sys.tables t ON t.[object_id] = OBJECT_ID(N'[dbo].' + QUOTENAME(b.[TenBang]), N'U')
LEFT JOIN sys.columns c ON c.[object_id] = t.[object_id] AND c.[name] = @CotMoc
LEFT JOIN sys.computed_columns cc ON cc.[object_id] = c.[object_id] AND cc.[column_id] = c.[column_id]
LEFT JOIN sys.indexes i ON i.[object_id] = t.[object_id] AND i.[name] = N'IX_' + b.[TenBang] + N'_' + @CotMoc
WHERE @ChiBang IS NULL OR b.[TenBang] = @ChiBang
ORDER BY b.[Stt];

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-them-BangNguonNoiDuoi.sql';
PRINT N'=============================================================================';
GO
