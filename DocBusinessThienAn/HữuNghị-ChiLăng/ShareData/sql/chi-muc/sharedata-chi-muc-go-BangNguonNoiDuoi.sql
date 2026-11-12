USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-chi-muc-go-BangNguonNoiDuoi.sql
-- BẢNG GHI: chỉ đổi CẤU TRÚC (gỡ 1 chỉ mục + 1 cột tính toán), không ghi dữ liệu, trên 6 bảng nguồn của 7 gói nối đuôi:
--             [dbo].[TmsTrafficData], [dbo].[TmsIncident], [dbo].[TollTransactionOut],
--             [dbo].[TmsSignalLog], [dbo].[TmsZoneStatus], [dbo].[TmsWeather]
--           Ngoại lệ Rule 19.103 (một tệp nhiều bảng) và Rule 19.85, cùng lý do với tệp thêm.
-- DESCRIPTION: Gỡ những gì sharedata-chi-muc-them-BangNguonNoiDuoi.sql đã thêm: chỉ mục
--              [IX_<TênBảng>_CursorKey] rồi cột tính toán [CursorKey]; gỡ luôn tên của bản đầu
--              ([IX_<TênBảng>_ShareDataCursorKey], [ShareDataCursorKey]) nếu còn.
--              Gỡ xong câu nối đuôi vẫn chạy đúng, chỉ quay lại quét và sắp xếp cả bảng như trước.
--              Dùng khi: dịch vụ ghi vào bảng báo lỗi 1934 sau khi đánh chỉ mục, CodeFirst báo lỗi vì cột ngoài entity,
--              hoặc cần đổi biểu thức mốc (gỡ rồi chạy lại tệp thêm).
-- THỨ TỰ CHẠY: Chạy độc lập. Nên dừng các dịch vụ ShareDataWorker trước khi chạy.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL đã chạy tệp thêm.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent): không có chỉ mục / cột thì bỏ qua.
--           Chỉ gỡ cột [CursorKey] khi đó là cột tính toán; cột thường trùng tên thì giữ nguyên và cảnh báo.
--           Mỗi lệnh chờ khóa tối đa @ChoKhoaMs rồi báo lỗi 1222; khi đó chạy lại tệp.
-- CẬP NHẬT LẦN CUỐI: 12112026 (đổi tên cột mốc ShareDataCursorKey -> CursorKey, gỡ cả tên cũ)
-- =========================================================================================

SET NOCOUNT ON;
SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-chi-muc-go-BangNguonNoiDuoi.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ
-- -----------------------------------------------------------------------------------------
DECLARE @ChiBang SYSNAME = NULL;                      -- NULL = cả 6 bảng; hoặc tên một bảng, vd N'TollTransactionOut'
DECLARE @ChoKhoaMs INT = 5000;                        -- chờ khóa tối đa (mili giây) cho mỗi lệnh

DECLARE @Bang TABLE ([Stt] INT PRIMARY KEY, [TenBang] SYSNAME);
INSERT INTO @Bang ([Stt], [TenBang]) VALUES
    (1, N'TmsTrafficData'),
    (2, N'TmsIncident'),
    (3, N'TollTransactionOut'),
    (4, N'TmsSignalLog'),
    (5, N'TmsZoneStatus'),
    (6, N'TmsWeather');

DECLARE @TenCot TABLE ([Thu] INT PRIMARY KEY, [CotMoc] SYSNAME);
INSERT INTO @TenCot ([Thu], [CotMoc]) VALUES (1, N'CursorKey'), (2, N'ShareDataCursorKey');   -- tên hiện tại, tên của bản đầu

DECLARE @Stt INT = 0, @Thu INT, @CotMoc SYSNAME, @TenBang SYSNAME, @TenChiMuc SYSNAME, @DoiTuong NVARCHAR(300), @Sql NVARCHAR(MAX);

WHILE 1 = 1
BEGIN
    SELECT TOP 1 @Stt = [Stt], @TenBang = [TenBang]
    FROM @Bang
    WHERE [Stt] > @Stt AND (@ChiBang IS NULL OR [TenBang] = @ChiBang)
    ORDER BY [Stt];
    IF @@ROWCOUNT = 0 BREAK;

    SET @DoiTuong = N'[dbo].' + QUOTENAME(@TenBang);

    IF OBJECT_ID(@DoiTuong, N'U') IS NULL
    BEGIN
        PRINT N'-> ' + @TenBang + N': bảng chưa tồn tại, bỏ qua.';
        CONTINUE;
    END;

    SET @Thu = 0;
    WHILE 1 = 1
    BEGIN
    SELECT TOP 1 @Thu = [Thu], @CotMoc = [CotMoc] FROM @TenCot WHERE [Thu] > @Thu ORDER BY [Thu];
    IF @@ROWCOUNT = 0 BREAK;
    SET @TenChiMuc = N'IX_' + @TenBang + N'_' + @CotMoc;

    BEGIN TRY
        -- 1. Chỉ mục (phải gỡ trước cột)
        IF EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @TenChiMuc)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
DROP INDEX ' + QUOTENAME(@TenChiMuc) + N' ON ' + @DoiTuong + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã gỡ chỉ mục ' + @TenChiMuc + N'.';
        END
        ELSE
            PRINT N'-> ' + @TenBang + N': không có chỉ mục ' + @TenChiMuc + N', bỏ qua.';

        -- 2. Cột tính toán
        IF EXISTS (SELECT 1 FROM sys.columns WHERE [object_id] = OBJECT_ID(@DoiTuong) AND [name] = @CotMoc AND [is_computed] = 1)
        BEGIN
            SET @Sql = N'SET LOCK_TIMEOUT ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N';
ALTER TABLE ' + @DoiTuong + N' DROP COLUMN ' + QUOTENAME(@CotMoc) + N';';
            EXEC sys.sp_executesql @Sql;
            PRINT N'-> ' + @TenBang + N': đã gỡ cột tính toán ' + @CotMoc + N'.';
        END
        ELSE IF COL_LENGTH(@DoiTuong, @CotMoc) IS NOT NULL
            PRINT N'⚠️ ' + @TenBang + N': cột ' + @CotMoc + N' KHÔNG phải cột tính toán, giữ nguyên.';
        ELSE
            PRINT N'-> ' + @TenBang + N': không có cột ' + @CotMoc + N', bỏ qua.';
    END TRY
    BEGIN CATCH
        PRINT N'❌ ' + @TenBang + N': lỗi ' + CAST(ERROR_NUMBER() AS NVARCHAR(12)) + N' — ' + ERROR_MESSAGE();
        IF ERROR_NUMBER() = 1222
            PRINT N'   Bảng đang bị câu khác giữ khóa quá ' + CAST(@ChoKhoaMs AS NVARCHAR(12)) + N' ms. Dừng ShareDataWorker rồi chạy lại tệp.';
    END CATCH;
    END;
END;

-- -----------------------------------------------------------------------------------------
-- KẾT QUẢ
-- -----------------------------------------------------------------------------------------
SELECT b.[TenBang],
       CASE WHEN t.[object_id] IS NULL THEN N'chưa có bảng' ELSE N'có' END AS [Bang],
       CASE WHEN MAX(c.[column_id]) IS NOT NULL THEN N'CÒN' ELSE N'đã gỡ' END AS [CotMoc],
       CASE WHEN MAX(i.[index_id]) IS NOT NULL THEN N'CÒN' ELSE N'đã gỡ' END AS [ChiMuc]
FROM @Bang b
LEFT JOIN sys.tables t ON t.[object_id] = OBJECT_ID(N'[dbo].' + QUOTENAME(b.[TenBang]), N'U')
LEFT JOIN sys.columns c ON c.[object_id] = t.[object_id] AND c.[name] IN (SELECT [CotMoc] FROM @TenCot) AND c.[is_computed] = 1
LEFT JOIN sys.indexes i ON i.[object_id] = t.[object_id] AND i.[name] IN (SELECT N'IX_' + b.[TenBang] + N'_' + [CotMoc] FROM @TenCot)
WHERE @ChiBang IS NULL OR b.[TenBang] = @ChiBang
GROUP BY b.[Stt], b.[TenBang], t.[object_id]
ORDER BY b.[Stt];

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-chi-muc-go-BangNguonNoiDuoi.sql';
PRINT N'=============================================================================';
GO
