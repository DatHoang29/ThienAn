-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 04_DROP_TABLES.SQL
-- DESCRIPTION: Xóa Hẳn Các Bảng CSDL Của Module ShareData (DROP TABLES)
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. XOÁ CÁC BẢNG LOG & PHIÊN & SỰ KIỆN
    IF OBJECT_ID('dbo.ShareDataActivityLog', 'U') IS NOT NULL DROP TABLE dbo.ShareDataActivityLog;
    IF OBJECT_ID('dbo.ShareDataSession', 'U') IS NOT NULL DROP TABLE dbo.ShareDataSession;
    IF OBJECT_ID('dbo.ShareDataEventSource', 'U') IS NOT NULL DROP TABLE dbo.ShareDataEventSource;

    -- 2. XOÁ CÁC BẢNG CẤU HÌNH & ÁNH XẠ
    IF OBJECT_ID('dbo.ShareDataSubscription', 'U') IS NOT NULL DROP TABLE dbo.ShareDataSubscription;
    IF OBJECT_ID('dbo.ShareDataMappingProfile', 'U') IS NOT NULL DROP TABLE dbo.ShareDataMappingProfile;
    IF OBJECT_ID('dbo.ShareDataDataSource', 'U') IS NOT NULL DROP TABLE dbo.ShareDataDataSource;
    IF OBJECT_ID('dbo.ShareDataPartner', 'U') IS NOT NULL DROP TABLE dbo.ShareDataPartner;

    COMMIT TRANSACTION;
    PRINT N'🔥 Đã xoá toàn bộ 7 bảng CSDL thuộc Scope Module ShareData thành công!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
