-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 03_CLEANUP.SQL
-- DESCRIPTION: Dọn Dệp Dữ Liệu Mẫu & Reset Trạng Thái Subscriptions ShareData
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. DỌN DỆP DỮ LIỆU DEMO (NẾU BẢNG TỒN TẠI)
    IF OBJECT_ID('dbo.ShareDataActivityLog', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataActivityLog] WHERE [DatatypeId] LIKE 'SHARE_%';
    IF OBJECT_ID('dbo.ShareDataSubscription', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataSubscription] WHERE [Code] LIKE 'SUB-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataMappingProfile', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataDataSource', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataDataSource] WHERE [Code] LIKE 'DS-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataPartner', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataPartner] WHERE [Code] LIKE 'PTN-DEMO-%';
    IF OBJECT_ID('dbo.ShareDataEventSource', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataEventSource] WHERE [Code] LIKE 'ES-DEMO-%';

    COMMIT TRANSACTION;
    PRINT N'✅ Dọn dẹp dữ liệu mẫu ShareData thành công!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
