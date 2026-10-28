USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-ShareDataSourceTable.sql
-- BẢNG GHI: [dbo].[ShareDataSourceTable] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Nạp danh mục 15 bảng nguồn vào ShareDataSourceTable (chống trùng theo TableName + IsDelete IS NULL;
--              ID sinh động; IsTracked = 0 cho TmsEquipment, IsQueryable = 1 cả 15 bảng; CodeFirst tạo khung trước).
-- THỨ TỰ CHẠY: 6 (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-ShareDataSourceTable.sql';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- 3. NẠP DANH MỤC 15 BẢNG NGUỒN VÀO ShareDataSourceTable (CodeFirst tạo khung trước)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;
PRINT N'-> 3. Kiểm tra bảng ShareDataSourceTable và nạp danh mục 15 bảng nguồn...';

IF OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Chưa chạy CodeFirst tạo bảng ShareDataSourceTable. Bỏ qua nạp dữ liệu bảng nguồn.';
    SET NOEXEC ON;
END;
GO
SET NOCOUNT ON;

DECLARE @TenantIdSource NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdSource IS NULL
    SET @TenantIdSource = N'Default';

DECLARE @SourceTables TABLE (
    [TableName]    NVARCHAR(128),
    [Name]         NVARCHAR(128),
    [PacketCodes]  NVARCHAR(256),
    [OrderNo]      INT,
    [IsTracked]    BIT,
    [IsQueryable]  BIT,
    [DefaultAlias] NVARCHAR(32),
    [Remark]       NVARCHAR(512)
);

INSERT INTO @SourceTables (
    [TableName], [Name], [PacketCodes], [OrderNo],
    [IsTracked], [IsQueryable], [DefaultAlias], [Remark]
)
VALUES
    (N'TmsZoneStatus', N'Trạng thái khu vực giao thông', N'101,101_commonData', 10, 1, 1,
     N'zs',
     N'Giám sát thay đổi trạng thái zone để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'TmsZone', N'Danh mục khu vực giao thông', N'101,101_commonData', 20, 1, 1,
     N'z',
     N'Giám sát thay đổi danh mục zone để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'TmsTrafficStatistic', N'Thống kê lưu lượng giao thông', N'101,101_commonData', 30, 1, 1,
     N'tstat',
     N'Giám sát dữ liệu thống kê giao thông để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'CctvDevice', N'Thiết bị camera giám sát', N'102,102_cctvData', 40, 1, 1,
     N'c',
     N'Giám sát dữ liệu camera để kích hoạt gói dữ liệu CCTV (102).'),

    (N'TmsTrafficData', N'Dữ liệu lưu lượng VDS và WIM', N'103,103_vdsData,106,106_wimData', 50, 1, 1,
     N'td',
     N'Giám sát dữ liệu đo đếm giao thông kích hoạt gói VDS (103) và WIM cân tải trọng (106).'),

    (N'TmsWeather', N'Dữ liệu khí tượng thời tiết', N'104,104_weatherData', 60, 1, 1,
     N'w',
     N'Giám sát dữ liệu trạm khí tượng để kích hoạt gói thời tiết (104).'),

    (N'TollTransactionIn', N'Giao dịch thu phí đầu vào', N'105,105_rfidData,109,109_etcData', 70, 1, 1,
     N'tin',
     N'Giám sát giao dịch vào trạm kích hoạt gói RFID (105) và gói ETC thu phí (109).'),

    (N'TollTransactionOut', N'Giao dịch thu phí đầu ra', N'105,105_rfidData,109,109_etcData', 80, 1, 1,
     N'tout',
     N'Giám sát giao dịch ra trạm kích hoạt gói RFID (105) và gói ETC thu phí (109).'),

    (N'TmsVehicleRegistration', N'Đăng ký phương tiện', N'105,105_rfidData', 90, 1, 1,
     N'vr',
     N'Giám sát dữ liệu phương tiện đăng ký kích hoạt gói RFID (105).'),

    (N'TmsIncident', N'Sự cố giao thông', N'107,107_incidentData,110,110_wpData,111', 100, 1, 1,
     N'i',
     N'Giám sát sự cố giao thông kích hoạt gói sự cố (107) và các gói tích hợp phụ trợ (110, 111).'),

    (N'TmsEventType', N'Loại sự kiện giao thông', N'107,107_incidentData,110,110_wpData,111', 110, 1, 1,
     N'et',
     N'Giám sát danh mục loại sự kiện giao thông kích hoạt gói sự cố (107) và phụ trợ (110, 111).'),

    (N'VmsCurrent', N'Thông tin bản tin biển báo VMS', N'108,108_vmsInfo', 120, 1, 1,
     N'v',
     N'Giám sát trạng thái hiển thị biển báo VMS kích hoạt gói tin VMS (108).'),

    (N'TmsEquipment', N'Danh mục thiết bị phần cứng', N'102,102_cctvData,103,103_vdsData,108,108_vmsInfo', 130, 0, 1,
     N'eq',
     N'Bảng tra cứu thiết bị, tắt giám sát Change Tracking (IsTracked = 0) để tránh trigger quét thừa cho 102/103/108 khi chỉ cập nhật thông tin thiết bị, nhưng vẫn mở cho bộ dựng truy vấn (IsQueryable = 1).'),

    (N'TollLane', N'Danh mục làn thu phí', N'109,109_etcData', 140, 1, 1,
     N'tl',
     N'Giám sát danh mục làn thu phí trạm kích hoạt gói dữ liệu ETC (109).'),

    (N'TollStation', N'Danh mục trạm thu phí', N'109,109_etcData', 150, 1, 1,
     N'ts',
     N'Giám sát danh mục trạm thu phí kích hoạt gói dữ liệu ETC (109).'),

    (N'TmsSignalLog', N'Nhật ký tín hiệu điều khiển', N'111', 160, 1, 1,
     N'sl',
     N'Giám sát dữ liệu nhật ký tín hiệu điều khiển kích hoạt gói trao đổi thông tin TT QLĐHGT tuyến (111).');

-- Bổ sung thông tin cột mới cho các dòng đã tồn tại (không ghi đè IsTracked / LastVersion đang chạy)
UPDATE target
SET target.[IsTracked]    = ISNULL(target.[IsTracked], src.[IsTracked]),
    target.[IsQueryable]  = ISNULL(target.[IsQueryable], src.[IsQueryable]),
    target.[DefaultAlias] = ISNULL(NULLIF(target.[DefaultAlias], N''), src.[DefaultAlias])
FROM [dbo].[ShareDataSourceTable] target
INNER JOIN @SourceTables src ON target.[TableName] = src.[TableName]
WHERE target.[IsDelete] IS NULL
  AND (
      target.[IsTracked] IS NULL
      OR target.[IsQueryable] IS NULL
      OR target.[DefaultAlias] IS NULL OR target.[DefaultAlias] = N''
  );

INSERT INTO [dbo].[ShareDataSourceTable] (
    [ID], [TableName], [Name], [PacketCodes], [OrderNo],
    [Remark], [IsTracked], [IsQueryable], [DefaultAlias],
    [LastVersion], [TenantId], [CreateTime], [CreateUId]
)
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())),
    src.[TableName],
    src.[Name],
    src.[PacketCodes],
    src.[OrderNo],
    src.[Remark],
    src.[IsTracked],
    src.[IsQueryable],
    src.[DefaultAlias],
    NULL,
    @TenantIdSource,
    GETDATE(),
    'superadmin'
FROM @SourceTables src
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[ShareDataSourceTable] target
    WHERE target.[TableName] = src.[TableName]
      AND target.[IsDelete] IS NULL
);

DECLARE @InsertedSources INT = @@ROWCOUNT;
DECLARE @TotalSources INT = (SELECT COUNT(1) FROM @SourceTables);
DECLARE @SkippedSources INT = @TotalSources - @InsertedSources;
PRINT N'   -> Đã seed ' + CAST(@InsertedSources AS NVARCHAR(10)) + N' bảng nguồn mới; bỏ qua ' + CAST(@SkippedSources AS NVARCHAR(10)) + N' dòng đã tồn tại trong ShareDataSourceTable.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-ShareDataSourceTable.sql';
PRINT N'=============================================================================';
GO
