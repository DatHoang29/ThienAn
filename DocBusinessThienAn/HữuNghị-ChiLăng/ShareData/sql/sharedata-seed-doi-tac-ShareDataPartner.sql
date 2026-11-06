USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-seed-doi-tac-ShareDataPartner.sql
-- BẢNG GHI: [dbo].[ShareDataPartner] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Tạo 2 đối tác đầu tiên của dự án nếu chưa có đối tác đang sống cùng mã:
--                PC08  - Phòng Cảnh sát giao thông (PC08)
--                TTDHT - Trung tâm điều hành tuyến
--              Chưa có địa chỉ kết nối nên Address, Port, EndPointApiUrl để trống, kiểu kết nối để trống (mặc định API).
--              Đối tác để bật (Status = 1) vì màn hình Cấu hình chia sẻ ẩn đối tác tắt; chưa có đăng ký nên Worker
--              chưa gửi gì. Nhập địa chỉ ở màn hình Cấu hình chia sẻ, nút sửa đối tác.
--              Đối tác đã có thì giữ nguyên, không ghi đè; riêng dòng do bản trước của tệp này tạo ở trạng thái tắt
--              và chưa ai sửa thì bật lên (bước 1.2).
-- THỨ TỰ CHẠY: 14 (sau CodeFirst; chạy sau D3 nếu dọn đối tác cũ) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 06/11/2026 (tạo mới; tạo đối tác ở trạng thái bật vì màn hình ẩn đối tác tắt)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-doi-tac-ShareDataPartner.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPartner]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPartner] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp đối tác.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 1. THÊM ĐỐI TÁC CÒN THIẾU
-- -----------------------------------------------------------------------------------------
DECLARE @TenantIdPartner NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdPartner IS NULL
    SET @TenantIdPartner = N'Default';

DECLARE @Partners TABLE
(
    [Code] NVARCHAR(32),
    [Name] NVARCHAR(128),
    [OrderNo] INT
);

INSERT INTO @Partners ([Code], [Name], [OrderNo])
VALUES
    (N'PC08', N'Phòng Cảnh sát giao thông (PC08)', 1),
    (N'TTDHT', N'Trung tâm điều hành tuyến', 2);

-- IsDelete để trống: cột là thời điểm xóa mềm, ghi 0 sẽ thành 1900-01-01 và đối tác bị coi là đã xóa.
-- Status = 1 (bật): màn hình Cấu hình chia sẻ ẩn đối tác tắt. SessionState = 0 (chưa kết nối).
INSERT INTO [dbo].[ShareDataPartner]
    ([ID], [Code], [Name], [Address], [Port], [EndPointApiUrl], [Status], [SessionState], [OrderNo], [Remark],
     [ConnectionType], [ConnectionConfig], [TenantId], [CreateTime], [CreateUId])
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())), src.[Code], src.[Name], NULL, NULL, NULL, 1, 0, src.[OrderNo], NULL,
    NULL, NULL, @TenantIdPartner, GETDATE(), N'SHAREDATA-SEED-14'
FROM @Partners src
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[ShareDataPartner] p
    WHERE p.[Code] = src.[Code]
      AND p.[IsDelete] IS NULL
);
PRINT N'-> 1.1. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác còn thiếu.';

-- 1.2 Bật đối tác do bản trước của tệp này tạo ở trạng thái tắt, chưa ai sửa trên giao diện (UpdateTime trống).
UPDATE p
SET p.[Status] = 1,
    p.[UpdateTime] = GETDATE(),
    p.[UpdateUId] = N'SHAREDATA-SEED-14'
FROM [dbo].[ShareDataPartner] p
JOIN @Partners src ON src.[Code] = p.[Code]
WHERE p.[IsDelete] IS NULL
  AND p.[CreateUId] = N'SHAREDATA-SEED-14'
  AND p.[UpdateTime] IS NULL
  AND p.[Status] = 0;
PRINT N'-> 1.2. Đã bật ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' đối tác tạo trước đó ở trạng thái tắt.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-doi-tac-ShareDataPartner.sql';
PRINT N'=============================================================================';
GO
