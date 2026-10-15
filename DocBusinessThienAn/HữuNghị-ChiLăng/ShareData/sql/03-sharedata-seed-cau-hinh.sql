USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 03-sharedata-seed-cau-hinh.sql
-- DESCRIPTION: Nạp cấu hình danh mục và hệ thống cho phân hệ ShareData (ESHARE):
--              1. Khởi tạo SysConfigType 'sharedata_error_code' (nếu chưa có).
--              2. Nạp đầy đủ 24 mã cảnh báo lỗi ESH-xxxx theo ESHARE §9 vào SysConfigData
--                 (13 Outbound, 7 Inbound, 4 Tracking), chỉ thêm các mã chưa có.
--              3. Nạp danh mục 15 bảng theo dõi Change Tracking 'sharedata_tracker' vào SysOpConfig
--                 (chỉ thêm các bảng chưa có; bảng TmsEquipment giữ Status = 0).
--              4. Khai nhóm 'sharedata_tracker' vào SysConfigData loại 'operation_config_type' để màn hình
--                 "Cấu hình hoạt động" hiển thị và cho chọn nhóm này (chỉ thêm nếu chưa có).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, không ghi đè cấu hình đã sửa).
-- CẬP NHẬT LẦN CUỐI: 16102026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN SEED CẤU HÌNH SHAREDATA (03-sharedata-seed-cau-hinh.sql)';
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

-- -----------------------------------------------------------------------------------------
-- 2. NẠP DANH SÁCH 24 MÃ CẢNH BÁO LỖI ESH-xxxx VÀO SysConfigData
-- -----------------------------------------------------------------------------------------
PRINT N'-> 2. Nạp danh mục 24 mã cảnh báo lỗi ESH-xxxx vào SysConfigData...';

DECLARE @ErrorCodes TABLE (
    [Code] NVARCHAR(64),
    [Name] NVARCHAR(256),
    [Value] NVARCHAR(64),
    [TagType] NVARCHAR(64),
    [OrderNo] INT,
    [Remark] NVARCHAR(512)
);

INSERT INTO @ErrorCodes ([Code], [Name], [Value], [TagType], [OrderNo], [Remark])
VALUES
    -- Outbound (13 mã)
    (N'ESH-1201', N'Bộ mã khai trong metadata không tồn tại trong CSDL', N'outbound', N'warning', 1201,
     N'Kiểm tra lại cấu hình CodeSet trong metadata.'),
    (N'ESH-1202', N'Trường bắt buộc bị thiếu hoặc null — huỷ kết xuất', N'outbound', N'danger', 1202,
     N'Kiểm tra dữ liệu nguồn hoặc cấu hình trường bắt buộc.'),
    (N'ESH-1203', N'Biểu thức không qua kiểm tra an toàn tĩnh hoặc lỗi khi tính — giữ giá trị nguồn', N'outbound', N'warning', 1203,
     N'Kiểm tra biểu thức biến đổi trong mapping.'),
    (N'ESH-1204', N'TargetShapeJson rỗng hoặc sai cú pháp — huỷ kết xuất', N'outbound', N'danger', 1204,
     N'Kiểm tra định dạng TargetShapeJson trong mapping.'),
    (N'ESH-1301', N'Không tìm thấy cấu hình gói tin cho DatatypeId', N'outbound', N'danger', 1301,
     N'Kiểm tra danh mục gói tin ShareDataPacket.'),
    (N'ESH-1302', N'Lỗi truy vấn dữ liệu nguồn của gói tin', N'outbound', N'danger', 1302,
     N'Kiểm tra câu lệnh SQL trích xuất và kết nối CSDL.'),
    (N'ESH-1303', N'Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất', N'outbound', N'danger', 1303,
     N'Kiểm tra cấu hình mapping giữa đối tác và gói tin.'),
    (N'ESH-1304', N'Khoá bản ghi cuối trang (__rowid) dài hơn cột LastKey cho phép — huỷ kết xuất trước khi gửi', N'outbound', N'danger', 1304,
     N'Kiểm tra kiểu dữ liệu hoặc giá trị khóa __rowid.'),
    (N'ESH-1305', N'Không tìm thấy câu lệnh SQL trích xuất dữ liệu ở bất kỳ tầng nào', N'outbound', N'danger', 1305,
     N'Kiểm tra câu truy vấn SQL trong ShareDataMapping hoặc ShareDataPacket.'),
    (N'ESH-1306', N'Thứ tự con trỏ dữ liệu không tăng dần hoặc thiếu khóa __rowid', N'outbound', N'danger', 1306,
     N'Kiểm tra mệnh đề ORDER BY trong câu lệnh trích xuất dữ liệu.'),
    (N'ESH-1307', N'Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL', N'outbound', N'warning', 1307,
     N'Kiểm tra mốc đồng bộ con trỏ và dữ liệu thời gian.'),
    (N'ESH-1401', N'Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công', N'outbound', N'warning', 1401,
     N'Kiểm tra kết nối mạng và endpoint của đối tác.'),
    (N'ESH-1402', N'Lưu đệm payload/bằng chứng thất bại, hủy gửi và giữ nguyên mốc', N'outbound', N'danger', 1402,
     N'Kiểm tra quyền ghi thư mục NasStorage:BasePath hoặc dung lượng ổ đĩa lưu trữ bằng chứng.'),

    -- Inbound (7 mã)
    (N'ESH-1501', N'Không đọc được nội dung gói tin nhận về', N'inbound', N'danger', 1501,
     N'Kiểm tra định dạng JSON/XML của gói tin nhận.'),
    (N'ESH-2405', N'Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag)', N'inbound', N'danger', 2405,
     N'Kiểm tra thuật toán mã hóa, khóa bí mật hoặc định dạng envelope.'),
    (N'ESH-1502', N'Lỗi ghi dữ liệu xuống CSDL nhận', N'inbound', N'danger', 1502,
     N'Kiểm tra kết nối và quyền ghi CSDL Inbound.'),
    (N'ESH-1503', N'Không tìm thấy cấu hình gói tin cho DatatypeId nhận về', N'inbound', N'danger', 1503,
     N'Kiểm tra danh mục gói tin chiều nhận trong ShareDataPacket.'),
    (N'ESH-1504', N'Đối tác của gói tin không tồn tại hoặc đang tắt', N'inbound', N'danger', 1504,
     N'Kiểm tra danh mục đối tác ShareDataPartner.'),
    (N'ESH-1505', N'Gói tin chưa có đăng ký nhận hợp lệ', N'inbound', N'danger', 1505,
     N'Kiểm tra cấu hình mapping chiều nhận ShareDataMapping.'),
    (N'ESH-1506', N'Gói tin chưa khai câu ghi chiều nhận (ShareDataPacketWrite)', N'inbound', N'danger', 1506,
     N'Kiểm tra cấu hình câu ghi CSDL trong ShareDataPacketWrite.'),

    -- Tracking (4 mã)
    (N'ESH-1601', N'Mốc Change Tracking không hợp lệ — đã tự nhảy cóc để hồi phục', N'tracking', N'warning', 1601,
     N'Hệ thống tự động đồng bộ lại mốc Change Tracking khi vượt cleanup.'),
    (N'ESH-1602', N'Truy vấn CHANGETABLE lỗi vì lý do KHÁC version — đã bỏ cache câu SQL và hẹn quét lại', N'tracking', N'danger', 1602,
     N'Kiểm tra cú pháp hoặc quyền truy cập CHANGETABLE trên bảng nguồn.'),
    (N'ESH-1603', N'CSDL hoặc bảng nguồn chưa bật Change Tracking — polling tạm dừng', N'tracking', N'warning', 1603,
     N'Kiểm tra cấu hình ALTER DATABASE / TABLE ENABLE CHANGE_TRACKING.'),
    (N'ESH-1604', N'Mốc đang giữ không còn tương thích CSDL — đã hạ mốc về -1 để đi lại đường khởi tạo', N'tracking', N'warning', 1604,
     N'Tracking bị tắt/bật lại hoặc CSDL restore từ bản cũ hơn.');

INSERT INTO dbo.SysConfigData (
    [ID], [ConfigTypeId], [Name], [Code], [Value], [TagType], 
    [OrderNo], [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    @ConfigTypeId,
    src.[Name],
    src.[Code],
    src.[Value],
    src.[TagType],
    src.[OrderNo],
    src.[Remark],
    1,
    @TenantId,
    GETDATE(),
    N'superadmin'
FROM @ErrorCodes src
WHERE NOT EXISTS (
    SELECT 1
    FROM   dbo.SysConfigData target
    WHERE  target.ConfigTypeId = @ConfigTypeId
      AND  target.Code = src.Code
      AND  target.IsDelete IS NULL
);

DECLARE @InsertedErrors INT = @@ROWCOUNT;
DECLARE @TotalErrors INT = (SELECT COUNT(1) FROM @ErrorCodes);
DECLARE @SkippedErrors INT = @TotalErrors - @InsertedErrors;
PRINT N'   -> Đã seed ' + CAST(@InsertedErrors AS NVARCHAR(10)) + N' mã lỗi mới; bỏ qua ' + CAST(@SkippedErrors AS NVARCHAR(10)) + N' mã đã có sẵn trong SysConfigData.';

-- -----------------------------------------------------------------------------------------
-- 3. NẠP DANH SÁCH 15 BẢNG GIÁM SÁT CHANGE TRACKING VÀO SysOpConfig (sharedata_tracker)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 3. Nạp danh mục 15 bảng theo dõi Change Tracking vào SysOpConfig...';

DECLARE @TrackerConfigs TABLE (
    [Code] NVARCHAR(128),
    [Name] NVARCHAR(64),
    [Value] NVARCHAR(256),
    [OrderNo] INT,
    [Status] INT,
    [Remark] NVARCHAR(1024)
);

INSERT INTO @TrackerConfigs ([Code], [Name], [Value], [OrderNo], [Status], [Remark])
VALUES
    (N'TmsZoneStatus', N'Trạng thái khu vực giao thông', N'101,101_commonData', 10, 1,
     N'Giám sát thay đổi trạng thái zone để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'TmsZone', N'Danh mục khu vực giao thông', N'101,101_commonData', 20, 1,
     N'Giám sát thay đổi danh mục zone để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'TmsTrafficStatistic', N'Thống kê lưu lượng giao thông', N'101,101_commonData', 30, 1,
     N'Giám sát dữ liệu thống kê giao thông để kích hoạt gói dữ liệu dùng chung (101).'),

    (N'CctvDevice', N'Thiết bị camera giám sát', N'102,102_cctvData', 40, 1,
     N'Giám sát dữ liệu camera để kích hoạt gói dữ liệu CCTV (102).'),

    (N'TmsTrafficData', N'Dữ liệu lưu lượng VDS và WIM', N'103,103_vdsData,106,106_wimData', 50, 1,
     N'Giám sát dữ liệu đo đếm giao thông kích hoạt gói VDS (103) và WIM cân tải trọng (106).'),

    (N'TmsWeather', N'Dữ liệu khí tượng thời tiết', N'104,104_weatherData', 60, 1,
     N'Giám sát dữ liệu trạm khí tượng để kích hoạt gói thời tiết (104).'),

    (N'TollTransactionIn', N'Giao dịch thu phí đầu vào', N'105,105_rfidData,109,109_etcData', 70, 1,
     N'Giám sát giao dịch vào trạm kích hoạt gói RFID (105) và gói ETC thu phí (109).'),

    (N'TollTransactionOut', N'Giao dịch thu phí đầu ra', N'105,105_rfidData,109,109_etcData', 80, 1,
     N'Giám sát giao dịch ra trạm kích hoạt gói RFID (105) và gói ETC thu phí (109).'),

    (N'TmsVehicleRegistration', N'Đăng ký phương tiện', N'105,105_rfidData', 90, 1,
     N'Giám sát dữ liệu phương tiện đăng ký kích hoạt gói RFID (105).'),

    (N'TmsIncident', N'Sự cố giao thông', N'107,107_incidentData,110,110_wpData,111', 100, 1,
     N'Giám sát sự cố giao thông kích hoạt gói sự cố (107) và các gói tích hợp phụ trợ (110, 111).'),

    (N'TmsEventType', N'Loại sự kiện giao thông', N'107,107_incidentData,110,110_wpData,111', 110, 1,
     N'Giám sát danh mục loại sự kiện giao thông kích hoạt gói sự cố (107) và phụ trợ (110, 111).'),

    (N'VmsCurrent', N'Thông tin bản tin biển báo VMS', N'108,108_vmsInfo', 120, 1,
     N'Giám sát trạng thái hiển thị biển báo VMS kích hoạt gói tin VMS (108).'),

    (N'TmsEquipment', N'Danh mục thiết bị phần cứng', N'102,102_cctvData,103,103_vdsData,108,108_vmsInfo', 130, 0,
     N'Bảng tra cứu thiết bị, tắt (Status = 0) để tránh trigger quét thừa cho 102/103/108 khi chỉ cập nhật thông tin thiết bị.'),

    (N'TollLane', N'Danh mục làn thu phí', N'109,109_etcData', 140, 1,
     N'Giám sát danh mục làn thu phí trạm kích hoạt gói dữ liệu ETC (109).'),

    (N'TollStation', N'Danh mục trạm thu phí', N'109,109_etcData', 150, 1,
     N'Giám sát danh mục trạm thu phí kích hoạt gói dữ liệu ETC (109).');

INSERT INTO [dbo].[SysOpConfig] (
    [ID], [GroupCode], [Code], [Name], [Value], [IsSysConfig], [OrderNo], 
    [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
)
SELECT 
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    N'sharedata_tracker',
    src.[Code],
    src.[Name],
    src.[Value],
    1,
    src.[OrderNo],
    src.[Remark],
    src.[Status],
    @TenantId,
    GETDATE(),
    N'superadmin'
FROM @TrackerConfigs src
WHERE NOT EXISTS (
    SELECT 1 
    FROM [dbo].[SysOpConfig] target
    WHERE target.[GroupCode] = N'sharedata_tracker'
      AND target.[Code] = src.[Code]
      AND target.[IsDelete] IS NULL
);

DECLARE @InsertedTrackers INT = @@ROWCOUNT;
DECLARE @TotalTrackers INT = (SELECT COUNT(1) FROM @TrackerConfigs);
DECLARE @SkippedTrackers INT = @TotalTrackers - @InsertedTrackers;
PRINT N'   -> Đã seed ' + CAST(@InsertedTrackers AS NVARCHAR(10)) + N' cấu hình tracker mới; bỏ qua ' + CAST(@SkippedTrackers AS NVARCHAR(10)) + N' dòng đã tồn tại trong SysOpConfig.';

-- -----------------------------------------------------------------------------------------
-- 4. KHAI NHÓM sharedata_tracker VÀO SysConfigData (operation_config_type)
--    Màn hình "Cấu hình hoạt động" lấy danh sách nhóm từ SysConfigData thuộc loại
--    operation_config_type; thiếu dòng này thì nhóm không hiện trong ô chọn (SQL vẫn chạy).
-- -----------------------------------------------------------------------------------------
PRINT N'-> 4. Khai nhóm [sharedata_tracker] vào SysConfigData [operation_config_type]...';

DECLARE @OpConfigTypeId NVARCHAR(64);

SELECT @OpConfigTypeId = ID
FROM   dbo.SysConfigType
WHERE  Code = N'operation_config_type'
  AND  IsDelete IS NULL;

IF @OpConfigTypeId IS NULL
BEGIN
    PRINT N'   -> ⚠️ Chưa có SysConfigType [operation_config_type] (do seed của WebAPI tạo khi khởi động lần đầu). Bỏ qua mục 4, chạy lại script sau khi WebAPI đã khởi động.';
END
ELSE IF EXISTS (
    SELECT 1
    FROM   dbo.SysConfigData
    WHERE  ConfigTypeId = @OpConfigTypeId
      AND  Code = N'sharedata_tracker'
      AND  IsDelete IS NULL
)
BEGIN
    PRINT N'   -> Nhóm [sharedata_tracker] đã được khai, bỏ qua.';
END
ELSE
BEGIN
    INSERT INTO dbo.SysConfigData (
        [ID], [ConfigTypeId], [Name], [Code], [Value], [TagType],
        [OrderNo], [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
    )
    VALUES (
        LOWER(CAST(NEWID() AS NVARCHAR(64))),
        @OpConfigTypeId,
        N'ShareData - Bảng giám sát Change Tracking',
        N'sharedata_tracker',
        N'',
        N'',
        1900,
        N'Danh sách bảng nguồn Worker ShareData theo dõi Change Tracking: Code = tên bảng, Value = mã gói tin cách nhau dấu phẩy, Status 1 = bật / 0 = tắt.',
        1,
        @TenantId,
        GETDATE(),
        N'superadmin'
    );
    PRINT N'   -> Đã khai nhóm [sharedata_tracker] vào operation_config_type.';
END

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED CẤU HÌNH SHAREDATA (03-sharedata-seed-cau-hinh.sql)';
PRINT N'=============================================================================';
GO
