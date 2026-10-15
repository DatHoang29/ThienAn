USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 00-sharedata-migration-mot-lan.sql
-- DESCRIPTION: Kịch bản di trú một lần cho phân hệ ShareData (ESHARE):
--              1. Bổ sung các cột cấu hình truy vấn và đồng bộ vào ShareDataPacket
--                 (ExtractSql, SyncKind, CursorColumn, LookbackDays).
--              2. Bổ sung các cột cấu hình truy vấn và đồng bộ vào ShareDataMapping
--                 (ExtractSql, SyncKind, CursorColumn, LookbackDays).
--              3. Kiểm tra và DROP bảng mồ côi ShareDataTable (chỉ xóa nếu bảng rỗng).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: ⚠️ CHỈ CHẠY MỘT LẦN KHI NÂNG CẤP/DI TRÚ HỆ THỐNG.
-- CẬP NHẬT LẦN CUỐI: 16102026
-- =========================================================================================

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA (00-sharedata-migration-mot-lan.sql)';
PRINT N'=============================================================================';

-- -----------------------------------------------------------------------------------------
-- 1. BỔ SUNG CỘT CẤU HÌNH TRUY VẤN VÀO BẢNG ShareDataPacket
-- -----------------------------------------------------------------------------------------
IF COL_LENGTH('dbo.ShareDataPacket', 'ExtractSql') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột ExtractSql vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [ExtractSql] NVARCHAR(MAX) NULL;
END
ELSE
    PRINT N'  -> Cột ExtractSql đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'SyncKind') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột SyncKind vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [SyncKind] INT NULL; -- 1 = Snapshot, 2 = Incremental
END
ELSE
    PRINT N'  -> Cột SyncKind đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột CursorColumn vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [CursorColumn] NVARCHAR(128) NULL;
END
ELSE
    PRINT N'  -> Cột CursorColumn đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'LookbackDays') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột LookbackDays vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [LookbackDays] INT NULL;
END
ELSE
    PRINT N'  -> Cột LookbackDays đã tồn tại trên ShareDataPacket, bỏ qua.';
GO

-- -----------------------------------------------------------------------------------------
-- 2. BỔ SUNG CỘT CẤU HÌNH TRUY VẤN VÀO BẢNG ShareDataMapping
-- -----------------------------------------------------------------------------------------
IF COL_LENGTH('dbo.ShareDataMapping', 'ExtractSql') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột ExtractSql vào bảng ShareDataMapping...';
    ALTER TABLE [dbo].[ShareDataMapping] ADD [ExtractSql] NVARCHAR(MAX) NULL;
END
ELSE
    PRINT N'  -> Cột ExtractSql đã tồn tại trên ShareDataMapping, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataMapping', 'SyncKind') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột SyncKind vào bảng ShareDataMapping...';
    ALTER TABLE [dbo].[ShareDataMapping] ADD [SyncKind] INT NULL; -- 1 = Snapshot, 2 = Incremental
END
ELSE
    PRINT N'  -> Cột SyncKind đã tồn tại trên ShareDataMapping, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataMapping', 'CursorColumn') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột CursorColumn vào bảng ShareDataMapping...';
    ALTER TABLE [dbo].[ShareDataMapping] ADD [CursorColumn] NVARCHAR(128) NULL;
END
ELSE
    PRINT N'  -> Cột CursorColumn đã tồn tại trên ShareDataMapping, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataMapping', 'LookbackDays') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột LookbackDays vào bảng ShareDataMapping...';
    ALTER TABLE [dbo].[ShareDataMapping] ADD [LookbackDays] INT NULL;
END
ELSE
    PRINT N'  -> Cột LookbackDays đã tồn tại trên ShareDataMapping, bỏ qua.';
GO

-- -----------------------------------------------------------------------------------------
-- 3. KIỂM TRA VÀ DROP BẢNG MỒ CÔI ShareDataTable
-- -----------------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShareDataTable')
BEGIN
    DECLARE @rowCount INT;
    DECLARE @sqlCount NVARCHAR(500) = N'SELECT @cnt = COUNT(1) FROM [dbo].[ShareDataTable]';
    EXEC sp_executesql @sqlCount, N'@cnt INT OUTPUT', @cnt = @rowCount OUTPUT;

    PRINT N'  -> Bảng ShareDataTable hiện có ' + CAST(@rowCount AS NVARCHAR(20)) + N' bản ghi.';

    IF @rowCount > 0
    BEGIN
        PRINT N'⚠️ CẢNH BÁO: Bảng ShareDataTable đang có ' + CAST(@rowCount AS NVARCHAR(20)) + N' bản ghi dữ liệu!';
        PRINT N'⚠️ Vui lòng lập trình viên kiểm tra và sao lưu (backup) trước khi thực hiện DROP thủ công.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Bảng ShareDataTable có 0 dòng, tiến hành xóa (DROP) bảng mồ côi...';
        DROP TABLE [dbo].[ShareDataTable];
        PRINT N'  -> Đã DROP bảng ShareDataTable thành công.';
    END
END
ELSE
BEGIN
    PRINT N'  -> Bảng ShareDataTable không tồn tại, bỏ qua.';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA!';
PRINT N'=============================================================================';
