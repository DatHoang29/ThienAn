-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 04_DROP_TABLES.SQL
-- DESCRIPTION: Xóa Hẳn Các Bảng CSDL Của Module ShareData (DROP TABLES)
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

SET NOEXEC OFF;
GO
