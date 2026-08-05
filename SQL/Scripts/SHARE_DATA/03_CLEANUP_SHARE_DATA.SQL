-- ============================================================================
-- MODULE: SHARE DATA (ESHARE V1)
-- FILE: 03_CLEANUP_SHARE_DATA.SQL
-- DESCRIPTION: Dọn Dệp Dữ Liệu Mẫu & Reset Trạng Thái Subscriptions ShareData
-- AUTHOR: Antigravity AI
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. DỌN DỆP DỮ LIỆU DEMO
    DELETE FROM [dbo].[EshExportLog] WHERE [DatatypeId] LIKE 'SHARE_%';
    DELETE FROM [dbo].[EshFieldMapping] WHERE [MappingProfileId] IN (SELECT [ID] FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%');
    DELETE FROM [dbo].[EshSubscription] WHERE [SerialNbr] LIKE 'SUB-DEMO-%';
    DELETE FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%';
    DELETE FROM [dbo].[EshDataSource] WHERE [Code] LIKE 'DS-DEMO-%';
    DELETE FROM [dbo].[EshPartner] WHERE [Code] LIKE 'PTN-DEMO-%';
    DELETE FROM [dbo].[EshEventSource] WHERE [PartnerCode] LIKE 'PTN-DEMO-%';

    COMMIT TRANSACTION;
    PRINT N'✅ Dọn dẹp dữ liệu mẫu ShareData thành công!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
