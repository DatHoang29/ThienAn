-- =========================================================================================
-- FILE: sharedata-seed-menu-SysMenu.sql
-- BẢNG GHI: [dbo].[SysMenu] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Cấu hình menu phân hệ ShareData (ESHARE) dạng tab gồm 2 nhóm:
--              1. "Cấu hình chia sẻ" (shareDataConfigGroup, tabPage/index):
--                 - Cấu hình (shareDataSharing)
--                 - Cấu hình gói tin (shareDataSource)
--                 - Ánh xạ dữ liệu (shareDataMapping)
--                 - Bộ mã Chuẩn hóa (shareDataCodeSet)
--                 - Mốc gửi (shareDataCursor)
--                 - Nguồn sự kiện (shareEventSource - status ẩn/tắt)
--              2. "Theo dõi chia sẻ" (shareDataMonitorGroup, tabPage/index):
--                 - Lịch sử chia sẻ (shareDataHistory)
--                 - Cảnh báo & lỗi (shareDataErrorLog)
-- THỨ TỰ CHẠY: 13 (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường. Không gán vai trò: API của trang chỉ nhận SuperAdmin, như các menu ShareData khác.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); giữ nguyên ID của các menu hiện có.
-- CẬP NHẬT LẦN CUỐI: 06/11/2026
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

-- 1. Tìm menu thư mục gốc phân hệ ShareData
DECLARE @RootDirId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysMenu]
    WHERE ([Name] = N'sharedata' OR [Path] = N'/sharedata')
      AND [IsDelete] IS NULL
);

-- Fallback tìm qua Pid của các menu con hiện có nếu chưa đổi cha
IF @RootDirId IS NULL
BEGIN
    SELECT TOP 1 @RootDirId = [Pid]
    FROM [dbo].[SysMenu]
    WHERE [Name] IN (N'shareDataSharing', N'shareDataSource', N'shareDataHistory', N'shareDataErrorLog')
      AND [IsDelete] IS NULL;
END;

IF @RootDirId IS NULL
BEGIN
    PRINT N'⚠️ Không tìm thấy nhóm menu gốc ShareData (menu sharedata / shareDataErrorLog). Bỏ qua kịch bản.';
    SET NOEXEC ON;
END;
GO

-- 2. Đảm bảo menu "Mốc gửi" (shareDataCursor) tồn tại trước khi chuyển tab
DECLARE @RootDirId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysMenu]
    WHERE ([Name] = N'sharedata' OR [Path] = N'/sharedata')
      AND [IsDelete] IS NULL
);
IF @RootDirId IS NULL
BEGIN
    SELECT TOP 1 @RootDirId = [Pid]
    FROM [dbo].[SysMenu]
    WHERE [Name] IN (N'shareDataSharing', N'shareDataSource', N'shareDataHistory', N'shareDataErrorLog')
      AND [IsDelete] IS NULL;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[SysMenu] WHERE [Name] = N'shareDataCursor' AND [IsDelete] IS NULL)
BEGIN
    INSERT INTO [dbo].[SysMenu]
        ([ID], [Pid], [Type], [Name], [Path], [Component], [Title], [Icon], [IsIframe], [IsHide], [IsKeepAlive], [IsAffix], [IsFullscreen], [OrderNo], [Status], [CreateTime], [CreateUId])
    VALUES
        (LOWER(CONVERT(NVARCHAR(36), NEWID())), @RootDirId, N'menu', N'shareDataCursor', N'/shareData/cursor', N'/sharedata/cursor/index', N'Mốc gửi', N'ele-Flag', 0, 0, 1, 0, 0, 7, 1, GETDATE(), N'superadmin');
    PRINT N'✅ Đã thêm menu shareDataCursor.';
END;
GO

-- 3. Tạo/cập nhật 2 menu nhóm tab (Component = /tabPage/index)
DECLARE @RootDirId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysMenu]
    WHERE ([Name] = N'sharedata' OR [Path] = N'/sharedata')
      AND [IsDelete] IS NULL
);
IF @RootDirId IS NULL
BEGIN
    SELECT TOP 1 @RootDirId = [Pid]
    FROM [dbo].[SysMenu]
    WHERE [Name] IN (N'shareDataSharing', N'shareDataSource', N'shareDataHistory', N'shareDataErrorLog')
      AND [IsDelete] IS NULL;
END;

-- Nhóm 1: "Cấu hình chia sẻ"
DECLARE @ConfigGroupId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysMenu]
    WHERE [Name] = N'shareDataConfigGroup' AND [IsDelete] IS NULL
);

IF @ConfigGroupId IS NULL
BEGIN
    SET @ConfigGroupId = LOWER(CONVERT(NVARCHAR(36), NEWID()));
    INSERT INTO [dbo].[SysMenu]
        ([ID], [Pid], [Type], [Name], [Path], [Component], [Title], [Icon], [IsIframe], [IsHide], [IsKeepAlive], [IsAffix], [IsFullscreen], [OrderNo], [Status], [CreateTime], [CreateUId])
    VALUES
        (@ConfigGroupId, @RootDirId, N'menu', N'shareDataConfigGroup', N'/shareData/config', N'/tabPage/index', N'Cấu hình chia sẻ', N'ele-Setting', 0, 0, 1, 0, 0, 10, 1, GETDATE(), N'superadmin');
    PRINT N'✅ Đã tạo menu nhóm "Cấu hình chia sẻ" (shareDataConfigGroup).';
END
ELSE
BEGIN
    UPDATE [dbo].[SysMenu]
    SET [Pid] = @RootDirId,
        [Type] = N'menu',
        [Path] = N'/shareData/config',
        [Component] = N'/tabPage/index',
        [Title] = N'Cấu hình chia sẻ',
        [OrderNo] = 10,
        [Status] = 1
    WHERE [ID] = @ConfigGroupId;
    PRINT N'ℹ️ Menu nhóm "Cấu hình chia sẻ" đã có, cập nhật đường dẫn/component.';
END;

-- Nhóm 2: "Theo dõi chia sẻ"
DECLARE @MonitorGroupId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysMenu]
    WHERE [Name] = N'shareDataMonitorGroup' AND [IsDelete] IS NULL
);

IF @MonitorGroupId IS NULL
BEGIN
    SET @MonitorGroupId = LOWER(CONVERT(NVARCHAR(36), NEWID()));
    INSERT INTO [dbo].[SysMenu]
        ([ID], [Pid], [Type], [Name], [Path], [Component], [Title], [Icon], [IsIframe], [IsHide], [IsKeepAlive], [IsAffix], [IsFullscreen], [OrderNo], [Status], [CreateTime], [CreateUId])
    VALUES
        (@MonitorGroupId, @RootDirId, N'menu', N'shareDataMonitorGroup', N'/shareData/monitor', N'/tabPage/index', N'Theo dõi chia sẻ', N'ele-Monitor', 0, 0, 1, 0, 0, 20, 1, GETDATE(), N'superadmin');
    PRINT N'✅ Đã tạo menu nhóm "Theo dõi chia sẻ" (shareDataMonitorGroup).';
END
ELSE
BEGIN
    UPDATE [dbo].[SysMenu]
    SET [Pid] = @RootDirId,
        [Type] = N'menu',
        [Path] = N'/shareData/monitor',
        [Component] = N'/tabPage/index',
        [Title] = N'Theo dõi chia sẻ',
        [OrderNo] = 20,
        [Status] = 1
    WHERE [ID] = @MonitorGroupId;
    PRINT N'ℹ️ Menu nhóm "Theo dõi chia sẻ" đã có, cập nhật đường dẫn/component.';
END;

-- 4. Chuyển các menu chức năng thành tab con (Type = 'tab') và cập nhật Pid (giữ nguyên ID)
-- 4.1. Nhóm "Cấu hình chia sẻ"
UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 10
WHERE [Name] = N'shareDataSharing' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 20
WHERE [Name] = N'shareDataSource' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 30
WHERE [Name] = N'shareDataMapping' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 40
WHERE [Name] = N'shareDataCodeSet' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 50
WHERE [Name] = N'shareDataCursor' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @ConfigGroupId, [Type] = N'tab', [OrderNo] = 60
WHERE [Name] = N'shareEventSource' AND [IsDelete] IS NULL;

PRINT N'✅ Đã chuyển 6 menu cấu hình thành tab con của "Cấu hình chia sẻ".';

-- 4.2. Nhóm "Theo dõi chia sẻ"
UPDATE [dbo].[SysMenu]
SET [Pid] = @MonitorGroupId, [Type] = N'tab', [OrderNo] = 10
WHERE [Name] = N'shareDataHistory' AND [IsDelete] IS NULL;

UPDATE [dbo].[SysMenu]
SET [Pid] = @MonitorGroupId, [Type] = N'tab', [OrderNo] = 20
WHERE [Name] = N'shareDataErrorLog' AND [IsDelete] IS NULL;

PRINT N'✅ Đã chuyển 2 menu theo dõi thành tab con của "Theo dõi chia sẻ".';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'KẾT THÚC KỊCH BẢN sharedata-seed-menu-SysMenu.sql';
PRINT N'=============================================================================';
GO
