USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-SysConfigType.sql
-- BẢNG GHI: [dbo].[SysConfigType] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Khởi tạo loại danh mục 'sharedata_error_code' (mã cảnh báo lỗi ShareData) nếu chưa có.
-- THỨ TỰ CHẠY: 4 (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-SysConfigType.sql';
PRINT N'=============================================================================';

DECLARE @TenantId NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantId IS NULL
    SET @TenantId = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. KHỞI TẠO SysConfigType CHO MÃ LỖI SHAREDATA (sharedata_error_code)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 1. Kiểm tra và khởi tạo SysConfigType [sharedata_error_code]...';

DECLARE @ConfigTypeId NVARCHAR(64);

SELECT @ConfigTypeId = ID
FROM   dbo.SysConfigType
WHERE  Code = N'sharedata_error_code'
  AND  IsDelete IS NULL;

IF @ConfigTypeId IS NULL
BEGIN
    SET @ConfigTypeId = LOWER(CAST(NEWID() AS NVARCHAR(64)));
    INSERT INTO dbo.SysConfigType
        (ID, Name, Code, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (@ConfigTypeId, N'Mã cảnh báo lỗi ShareData', N'sharedata_error_code', 1900,
         N'Danh mục mã cảnh báo lỗi của hệ thống trao đổi dữ liệu ShareData ESHARE §9', 1, @TenantId, GETDATE(), N'superadmin');
    PRINT N'   -> Đã tạo mới SysConfigType [sharedata_error_code] thành công.';
END
ELSE
BEGIN
    PRINT N'   -> SysConfigType [sharedata_error_code] đã tồn tại (ID: ' + @ConfigTypeId + N').';
END

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-SysConfigType.sql';
PRINT N'=============================================================================';
GO
