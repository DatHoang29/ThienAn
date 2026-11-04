USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-goi-tin-ShareDataPacket.sql
-- BẢNG GHI: [dbo].[ShareDataPacket] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Khởi tạo và chuẩn hóa bản ghi gói tin Catalog (101 - 111) trong ShareDataPacket:
--              1. Thêm các gói còn thiếu (khớp mã '1NN' hoặc '1NN_...' đang sống, IsDelete trống; ID sinh động).
--                 Gói đã có thì giữ nguyên tên, mô tả, phiên bản.
--              2. Chuẩn hóa OrderNo về đúng 101 - 111 cho các gói khớp mã chuẩn.
--              3. Chuẩn hóa Status: bật 110*, tắt 111* (chưa hoàn thiện).
--              Không chứa câu SQL: câu mẫu chung nằm ở sharedata-seed-goi-tin-ShareDataSql.sql.
-- THỨ TỰ CHẠY: 1 (sau CodeFirst, trước sharedata-seed-goi-tin-ShareDataSql.sql) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 03/11/2026 (viết lại sau khi tệp bị mất ngày 27/10/2026; dòng mới để IsDelete trống, có TenantId)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacket.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp dữ liệu gói tin.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 1. THÊM GÓI CATALOG CÒN THIẾU, CHUẨN HÓA OrderNo VÀ Status (101 - 111)
-- -----------------------------------------------------------------------------------------
DECLARE @TenantIdPacket NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdPacket IS NULL
    SET @TenantIdPacket = N'Default';

DECLARE @Packets TABLE
(
    [Prefix] NVARCHAR(8),
    [Code] NVARCHAR(128),
    [Name] NVARCHAR(128),
    [Description] NVARCHAR(256),
    [OrderNo] INT,
    [Status] INT,
    [Remark] NVARCHAR(256)
);

INSERT INTO @Packets ([Prefix], [Code], [Name], [Description], [OrderNo], [Status], [Remark])
VALUES
    (N'101', N'101_commonData', N'Dữ liệu thông tin chung / luồng giao thông', N'Gói 101: Dữ liệu thông tin chung / luồng giao thông', 101, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'102', N'102_cctvData', N'Dữ liệu hình ảnh giao thông CCTV', N'Gói 102: Dữ liệu hình ảnh giao thông CCTV', 102, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'103', N'103_vdsData', N'Dữ liệu đếm xe VDS', N'Gói 103: Dữ liệu đếm xe VDS', 103, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'104', N'104_weatherData', N'Dữ liệu khí tượng / thời tiết', N'Gói 104: Dữ liệu khí tượng / thời tiết', 104, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'105', N'105_rfidData', N'Dữ liệu định danh phương tiện AVI / RFID', N'Gói 105: Dữ liệu định danh phương tiện AVI / RFID', 105, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'106', N'106_wimData', N'Dữ liệu cân tải trọng xe tự động WIM', N'Gói 106: Dữ liệu cân tải trọng xe tự động WIM', 106, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'107', N'107_incidentData', N'Dữ liệu thông tin sự cố / sự kiện giao thông', N'Gói 107: Dữ liệu thông tin sự cố / sự kiện giao thông', 107, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'108', N'108_vmsInfo', N'Dữ liệu thông tin biển báo điện tử VMS', N'Gói 108: Dữ liệu thông tin biển báo điện tử VMS', 108, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'109', N'109_etcData', N'Dữ liệu giao dịch thu phí ETC', N'Gói 109: Dữ liệu giao dịch thu phí ETC', 109, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'110', N'110_wpData', N'Dữ liệu trao đổi thông tin với người tham gia giao thông', N'Gói 110: Dữ liệu trao đổi thông tin với người tham gia giao thông', 110, 1, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData'),
    (N'111', N'111', N'Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến', N'Gói 111: Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến', 111, 0, N'Khởi tạo mặc định theo chuẩn phân hệ ShareData (tạm thời chưa kích hoạt)');

-- 1.1 Thêm gói chưa có. IsDelete để trống: cột là thời điểm xóa mềm, ghi 0 sẽ thành 1900-01-01 và gói bị coi là đã xóa
INSERT INTO [dbo].[ShareDataPacket]
    ([ID], [Code], [Name], [PacketVersion], [Description], [OrderNo], [Status], [Remark], [TenantId], [CreateTime], [CreateUId])
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())), src.[Code], src.[Name], N'1.0', src.[Description], src.[OrderNo], src.[Status], src.[Remark],
    @TenantIdPacket, GETDATE(), N'SHAREDATA-SEED-01'
FROM @Packets src
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[ShareDataPacket] p
    WHERE (p.[Code] = src.[Prefix] OR p.[Code] LIKE src.[Prefix] + N'[_]%')
      AND p.[IsDelete] IS NULL
);
PRINT N'-> 1.1. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' gói còn thiếu.';

-- 1.2 OrderNo đúng mã chuẩn
UPDATE p
SET p.[OrderNo] = src.[OrderNo]
FROM [dbo].[ShareDataPacket] p
INNER JOIN @Packets src ON (p.[Code] = src.[Prefix] OR p.[Code] LIKE src.[Prefix] + N'[_]%')
WHERE p.[IsDelete] IS NULL
  AND (p.[OrderNo] IS NULL OR p.[OrderNo] <> src.[OrderNo]);
PRINT N'-> 1.2. Đã chuẩn hóa OrderNo: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' gói.';

-- 1.3 Bật gói 110* khi đang trống hoặc tắt do cấu hình cũ; giữ gói 111* tắt vì chưa hoàn thiện
UPDATE [dbo].[ShareDataPacket]
SET [Status] = 1
WHERE [IsDelete] IS NULL
  AND ([Status] IS NULL OR [Status] = 0)
  AND ([Code] = N'110' OR [Code] LIKE N'110[_]%');
PRINT N'-> 1.3. Bật gói 110*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' gói.';

UPDATE [dbo].[ShareDataPacket]
SET [Status] = 0
WHERE [IsDelete] IS NULL
  AND ([Status] IS NULL OR [Status] <> 0)
  AND ([Code] = N'111' OR [Code] LIKE N'111[_]%');
PRINT N'-> 1.3. Tắt gói 111*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' gói.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-goi-tin-ShareDataPacket.sql';
PRINT N'=============================================================================';
GO
