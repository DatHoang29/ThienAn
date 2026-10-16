-- =========================================================================================
-- Script: 06-sharedata-doi-ten-cot-dong-bo.sql
-- Mục đích: Đổi tên 3 trường cấu hình đồng bộ ở phân hệ ShareData cho dễ hiểu:
--           1. SyncKind     -> SyncMode        (1 = Snapshot, 2 = Incremental)
--           2. TimeColumn / CursorColumn -> CursorTimeColumn (tên cột mốc thời gian con trỏ)
--           3. LookbackDays -> MaxLookBackDays (số ngày quét lùi tối đa)
-- Áp dụng cho: Bảng [ShareDataPacket]
-- Môi trường:  DEV_ITS10 (SQL Server)
-- Nguyên tắc:  Idempotent — chạy lại nhiều lần an toàn, không làm mất dữ liệu hiện có
-- =========================================================================================

USE [DEV_ITS10];
GO

SET NOCOUNT ON;

PRINT N'=========================================================================================';
PRINT N'Bắt đầu thực thi: 06-sharedata-doi-ten-cot-dong-bo.sql';
PRINT N'=========================================================================================';

-- ─────────────────────────────────────────────────────────────────────────────────────────
-- 1. BẢNG [dbo].[ShareDataPacket]
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 1. Kiểm tra và đổi tên cột trên bảng [ShareDataPacket]...';

-- 1.1 SyncKind -> SyncMode
IF COL_LENGTH('dbo.ShareDataPacket', 'SyncKind') IS NOT NULL AND COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NULL
BEGIN
    PRINT N'   - Đang đổi tên cột [SyncKind] -> [SyncMode] trên [ShareDataPacket]...';
    EXEC sp_rename 'dbo.ShareDataPacket.SyncKind', 'SyncMode', 'COLUMN';
    PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[SyncMode].';
END
ELSE IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NOT NULL
BEGIN
    PRINT N'   - Cột [SyncMode] đã tồn tại trên [ShareDataPacket], bỏ qua.';
END
ELSE
BEGIN
    PRINT N'   - Cột [SyncKind] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;

-- 1.2 TimeColumn / CursorColumn -> CursorTimeColumn
IF COL_LENGTH('dbo.ShareDataPacket', 'TimeColumn') IS NOT NULL AND COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NULL
BEGIN
    PRINT N'   - Đang đổi tên cột [TimeColumn] -> [CursorTimeColumn] trên [ShareDataPacket]...';
    EXEC sp_rename 'dbo.ShareDataPacket.TimeColumn', 'CursorTimeColumn', 'COLUMN';
    PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[CursorTimeColumn].';
END
ELSE IF COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NOT NULL AND COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NULL
BEGIN
    PRINT N'   - Đang đổi tên cột [CursorColumn] -> [CursorTimeColumn] trên [ShareDataPacket]...';
    EXEC sp_rename 'dbo.ShareDataPacket.CursorColumn', 'CursorTimeColumn', 'COLUMN';
    PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[CursorTimeColumn].';
END
ELSE IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
BEGIN
    PRINT N'   - Cột [CursorTimeColumn] đã tồn tại trên [ShareDataPacket], bỏ qua.';
END;

-- 1.3 LookbackDays -> MaxLookBackDays
IF COL_LENGTH('dbo.ShareDataPacket', 'LookbackDays') IS NOT NULL AND COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NULL
BEGIN
    PRINT N'   - Đang đổi tên cột [LookbackDays] -> [MaxLookBackDays] trên [ShareDataPacket]...';
    EXEC sp_rename 'dbo.ShareDataPacket.LookbackDays', 'MaxLookBackDays', 'COLUMN';
    PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[MaxLookBackDays].';
END
ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataPacket') AND name COLLATE Latin1_General_CS_AS = 'MaxLookbackDays')
BEGIN
    PRINT N'   - Đang chuẩn hóa chữ hoa cột [MaxLookbackDays] -> [MaxLookBackDays] trên [ShareDataPacket]...';
    EXEC sp_rename 'dbo.ShareDataPacket.MaxLookbackDays', 'MaxLookBackDays', 'COLUMN';
    PRINT N'   -> Chuẩn hóa thành công: [ShareDataPacket].[MaxLookBackDays].';
END
ELSE IF COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NOT NULL
BEGIN
    PRINT N'   - Cột [MaxLookBackDays] đã tồn tại trên [ShareDataPacket], bỏ qua.';
END
ELSE
BEGIN
    PRINT N'   - Cột [LookbackDays] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;



PRINT N'=========================================================================================';
PRINT N'Hoàn tất: 06-sharedata-doi-ten-cot-dong-bo.sql';
PRINT N'=========================================================================================';
GO
