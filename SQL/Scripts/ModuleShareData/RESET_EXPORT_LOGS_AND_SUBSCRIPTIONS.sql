-- ============================================================================
-- SCRIPT RESET NHẬT KÝ KẾT XUẤT VÀ TRẠNG THÁI SUBSCRIPTION
-- Database: [dev_its10] / ConfigId: dev_its10
-- Author: Đạt
-- Created date: 31/07/2026
-- Description: Script dùng để xóa nhật ký kết xuất cũ (xóa FileHash, FilePath)
--              và reset thời gian chạy Subscription để cho phép Worker Service
--              kết xuất lại từ đầu trong môi trường Dev/Test.
-- ============================================================================

USE [dev_its10];
GO

BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Xóa toàn bộ nhật ký xuất dữ liệu (bao gồm cả FileHash và FilePath đợt trước)
    DELETE FROM [dbo].[EshExportLog];

    -- 2. Xóa toàn bộ nhật ký hệ thống
    DELETE FROM [dbo].[EshSystemLog];

    -- 3. Reset thời gian chạy và trạng thái của tất cả Subscription về ban đầu
    UPDATE [dbo].[EshSubscription]
    SET 
        [LastTimeRun] = NULL,
        [NextTimeRun] = GETDATE(),
        [RunStatus] = N'Idle',
        [ProcessLockId] = NULL,
        [UpdateTime] = GETDATE();

    COMMIT TRANSACTION;
    PRINT N'✅ Đã reset thành công Nhật ký kết xuất (EshExportLog) và Trạng thái Subscription!';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'❌ Lỗi khi thực thi Reset Script: ' + ERROR_MESSAGE();
END CATCH;
GO
