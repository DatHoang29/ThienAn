-- ============================================================================
-- MODULE: SHARE DATA (ESHARE V1) - TEST DATABASE
-- FILE: 03_CLEANUP.SQL
-- DESCRIPTION: Dọn Dệp Dữ Liệu Mẫu & Reset Trạng Thái Subscriptions Trong CSDL [test]
-- AUTHOR: Antigravity AI
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [test];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. DỌN DỆP DỮ LIỆU DEMO (NẾU BẢNG TỒN TẠI)
    IF OBJECT_ID('dbo.EshExportLog', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshExportLog] WHERE [DatatypeId] LIKE 'SHARE_%';
    IF OBJECT_ID('dbo.EshFieldMapping', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshFieldMapping] WHERE [MappingProfileId] IN (SELECT [ID] FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%');
    IF OBJECT_ID('dbo.EshSubscription', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshSubscription] WHERE [SerialNbr] LIKE 'SUB-DEMO-%';
    IF OBJECT_ID('dbo.EshMappingProfile', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%';
    IF OBJECT_ID('dbo.EshDataSource', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshDataSource] WHERE [Code] LIKE 'DS-DEMO-%';
    IF OBJECT_ID('dbo.EshPartner', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshPartner] WHERE [Code] LIKE 'PTN-DEMO-%';
    IF OBJECT_ID('dbo.EshEventSource', 'U') IS NOT NULL 
        DELETE FROM [dbo].[EshEventSource] WHERE [PartnerCode] LIKE 'PTN-DEMO-%';

    COMMIT TRANSACTION;
    PRINT N'✅ Dọn dẹp dữ liệu mẫu ShareData trong CSDL [test] thành công!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
