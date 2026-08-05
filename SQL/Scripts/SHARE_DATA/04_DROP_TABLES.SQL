-- ============================================================================
-- MODULE: SHARE DATA (ESHARE V1)
-- FILE: 04_DROP_TABLES.SQL
-- DESCRIPTION: Xóa Hẳn Các Bảng CSDL Của Module ShareData (DROP TABLES)
-- AUTHOR: Antigravity AI
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. XOÁ CÁC BẢNG LOG & SỰ KIỆN
    IF OBJECT_ID('dbo.EshExportLog', 'U') IS NOT NULL DROP TABLE dbo.EshExportLog;
    IF OBJECT_ID('dbo.EshSystemLog', 'U') IS NOT NULL DROP TABLE dbo.EshSystemLog;
    IF OBJECT_ID('dbo.EshEventSource', 'U') IS NOT NULL DROP TABLE dbo.EshEventSource;

    -- 2. XOÁ CÁC BẢNG CẤU HÌNH & ÁNH XẠ
    IF OBJECT_ID('dbo.EshSubscription', 'U') IS NOT NULL DROP TABLE dbo.EshSubscription;
    IF OBJECT_ID('dbo.EshFieldMapping', 'U') IS NOT NULL DROP TABLE dbo.EshFieldMapping;
    IF OBJECT_ID('dbo.EshMappingProfile', 'U') IS NOT NULL DROP TABLE dbo.EshMappingProfile;
    IF OBJECT_ID('dbo.EshDataSource', 'U') IS NOT NULL DROP TABLE dbo.EshDataSource;
    IF OBJECT_ID('dbo.EshPartner', 'U') IS NOT NULL DROP TABLE dbo.EshPartner;

    COMMIT TRANSACTION;
    PRINT N'🔥 Đã xoá toàn bộ 8 bảng CSDL thuộc Scope Module ShareData thành công!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
