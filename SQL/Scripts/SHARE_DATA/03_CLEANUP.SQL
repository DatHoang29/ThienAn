-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 03_CLEANUP.SQL
-- DESCRIPTION: Dọn Dệp Dữ Liệu Mẫu & Reset Trạng Thái Subscriptions ShareData
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================

-- ============================================================================
-- CHỐT CHẶN SERVER
--   File .sql KHÔNG chọn được server — server do kết nối của trình soạn thảo
--   quyết định. Điền tên server bạn MUỐN chạy vào @ServerKyVong để script tự
--   dừng nếu chạy nhầm máy. Để trống thì bỏ qua kiểm.
--   Xem tên server hiện tại: SELECT @@SERVERNAME
-- ============================================================================
SET NOEXEC OFF;

DECLARE @ServerKyVong SYSNAME = N'';   -- ví dụ: N'aa0914ef791d'

IF @ServerKyVong <> N'' AND @@SERVERNAME <> @ServerKyVong
BEGIN
    RAISERROR(N'DUNG LAI: dang o server [%s] nhung ky vong [%s]. Khong thuc thi gi ca.',
              16, 1, @@SERVERNAME, @ServerKyVong);
    SET NOEXEC ON;
END;
GO

-- Ép về đúng database của môi trường local. Đổi tên ở đây nếu dùng database khác.
-- Đặt SAU chốt chặn để khi sai server thì NOEXEC đã bật, câu USE này cũng không chạy.
USE [dev_its10];
GO

PRINT N'▶ Server: ' + @@SERVERNAME
    + N' | Database: ' + DB_NAME()
    + N' | Login: ' + SUSER_SNAME()
    + N' | Phiên bản: ' + CONVERT(NVARCHAR(100), SERVERPROPERTY('Edition'));
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. DỌN DỆP DỮ LIỆU DEMO (NẾU BẢNG TỒN TẠI)
    IF OBJECT_ID('dbo.ShareDataActivityLog', 'U') IS NOT NULL 
        DELETE FROM [dbo].[ShareDataActivityLog] WHERE [SubscriptionId] IN
            (SELECT [ID] FROM [dbo].[ShareDataSubscription] WHERE [Code] LIKE 'SUB-DEMO-%');
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

SET NOEXEC OFF;
GO
