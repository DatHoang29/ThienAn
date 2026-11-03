USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-menu-SysMenu.sql
-- BẢNG GHI: [dbo].[SysMenu] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Thêm menu "Mốc gửi" (views/sharedata/cursor) dưới nhóm ShareData nếu chưa có.
-- THỨ TỰ CHẠY: 13 (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường. Vai trò cần thấy menu thì gán thêm ở màn Vai trò.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); không ghi đè menu đã có.
-- CẬP NHẬT LẦN CUỐI: 02/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-menu-SysMenu.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysMenu]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysMenu] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua kịch bản.';
    SET NOEXEC ON;
END;
GO

-- Tra nhóm cha qua menu con đã có để không phụ thuộc ID của từng môi trường
DECLARE @ParentId NVARCHAR(64) = (SELECT TOP 1 [Pid] FROM [dbo].[SysMenu] WHERE [Name] = N'shareDataErrorLog' AND [IsDelete] IS NULL);

IF @ParentId IS NULL
    PRINT N'⚠️ Không tìm thấy nhóm menu ShareData (menu shareDataErrorLog). Bỏ qua, không thêm menu.';
ELSE IF EXISTS (SELECT 1 FROM [dbo].[SysMenu] WHERE [Name] = N'shareDataCursor' AND [IsDelete] IS NULL)
    PRINT N'ℹ️ Menu shareDataCursor đã có, giữ nguyên.';
ELSE
BEGIN
    INSERT INTO [dbo].[SysMenu]
        ([ID], [Pid], [Type], [Name], [Path], [Component], [Title], [Icon], [IsIframe], [IsHide], [IsKeepAlive], [IsAffix], [IsFullscreen], [OrderNo], [Status], [CreateTime], [CreateUId])
    VALUES
        (LOWER(CONVERT(NVARCHAR(36), NEWID())), @ParentId, N'menu', N'shareDataCursor', N'/shareData/cursor', N'/sharedata/cursor/index', N'Mốc gửi', N'ele-Flag', 0, 0, 1, 0, 0, 7, 1, GETDATE(), N'superadmin');
    PRINT N'✅ Đã thêm menu shareDataCursor.';
END;
GO

SET NOEXEC OFF;
GO

PRINT N'KẾT THÚC KỊCH BẢN sharedata-seed-menu-SysMenu.sql';
GO
