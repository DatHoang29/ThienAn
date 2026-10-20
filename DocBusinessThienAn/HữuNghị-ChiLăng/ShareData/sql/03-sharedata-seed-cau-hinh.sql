USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 03-sharedata-seed-cau-hinh.sql
-- DESCRIPTION: Nạp cấu hình danh mục, bảng nguồn và tham số vận hành cho phân hệ ShareData (ESHARE):
--              1. Khởi tạo SysConfigType 'sharedata_error_code' (nếu chưa có).
--              2. Nạp đầy đủ 25 mã cảnh báo lỗi ESH-xxxx theo ESHARE §9 vào SysConfigData
--                 (14 Outbound, 7 Inbound, 4 Tracking), chỉ thêm các mã chưa có.
--              3. Nạp danh mục 15 bảng nguồn vào ShareDataSourceTable (chống trùng theo TableName + IsDelete IS NULL;
--                 ID sinh động bằng LOWER(CONVERT(NVARCHAR(64), NEWID())); IsTracked = 0 cho TmsEquipment,
--                 IsQueryable = 1 cả 15 bảng; CodeFirst tạo khung trước theo Rule 19.85).
--              4. Khai báo nhóm 'sharedata_config' vào SysConfigData (loại 'operation_config_type'),
--                 nạp 48 tham số vận hành & giới hạn kiểm tra vào SysOpConfig (19 mã Worker, 23 mã FE,
--                 6 mã BE) và xóa mềm nhóm cũ 'sharedata_fe_config'.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, chống trùng theo khóa nghiệp vụ, không ghi đè cấu hình đã sửa).
-- CẬP NHẬT LẦN CUỐI: 19/10/2026
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
-- 2. NẠP DANH SÁCH 25 MÃ CẢNH BÁO LỖI ESH-xxxx VÀO SysConfigData
-- -----------------------------------------------------------------------------------------
PRINT N'-> 2. Nạp danh mục 25 mã cảnh báo lỗi ESH-xxxx vào SysConfigData...';

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
    -- Outbound (14 mã)
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
     N'Kiểm tra câu truy vấn SQL (ExtractSql) trong cấu hình gói tin ShareDataPacket.'),
    (N'ESH-1306', N'Thứ tự con trỏ dữ liệu không tăng dần hoặc thiếu khóa __rowid', N'outbound', N'danger', 1306,
     N'Kiểm tra mệnh đề ORDER BY trong câu lệnh trích xuất dữ liệu.'),
    (N'ESH-1307', N'Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL', N'outbound', N'warning', 1307,
     N'Kiểm tra mốc đồng bộ con trỏ và dữ liệu thời gian.'),
    (N'ESH-1401', N'Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công', N'outbound', N'warning', 1401,
     N'Kiểm tra kết nối mạng và endpoint của đối tác.'),
    (N'ESH-1402', N'Lưu đệm payload/bằng chứng thất bại, hủy gửi và giữ nguyên mốc', N'outbound', N'danger', 1402,
     N'Kiểm tra quyền ghi thư mục NasStorage:BasePath hoặc dung lượng ổ đĩa lưu trữ bằng chứng.'),
    (N'ESH-1403', N'Giá trị cấu hình kết nối không hợp lệ bị bỏ qua hoặc thay bằng mặc định', N'outbound', N'warning', 1403,
     N'Kiểm tra lại cấu hình kết nối ConnectionConfig của đối tác.'),

    -- Inbound (7 mã)
    (N'ESH-1501', N'Không đọc được nội dung gói tin nhận về', N'inbound', N'danger', 1501,
     N'Kiểm tra định dạng JSON/XML của gói tin nhận.'),
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
    (N'ESH-1507', N'Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag)', N'inbound', N'danger', 1507,
     N'Kiểm tra thuật toán mã hóa, khóa bí mật hoặc định dạng envelope.'),

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
GO

-- -----------------------------------------------------------------------------------------
-- 3. NẠP DANH MỤC 15 BẢNG NGUỒN VÀO ShareDataSourceTable (CodeFirst tạo khung trước)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;
PRINT N'-> 3. Kiểm tra bảng ShareDataSourceTable và nạp danh mục 15 bảng nguồn...';

IF OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Chưa chạy CodeFirst tạo bảng ShareDataSourceTable. Bỏ qua nạp dữ liệu bảng nguồn.';
    RETURN;
END;
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
     N'Giám sát danh mục trạm thu phí kích hoạt gói dữ liệu ETC (109).');

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

-- -----------------------------------------------------------------------------------------
-- 4. KHỞI TẠO NHÓM sharedata_config TRONG SysConfigData VÀ SEED 48 THAM SỐ SysOpConfig
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;
PRINT N'-> 4. Khởi tạo nhóm cấu hình hoạt động [sharedata_config] và nạp 48 tham số vào SysOpConfig...';

DECLARE @TenantIdOp NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdOp IS NULL
    SET @TenantIdOp = N'Default';

-- 4.1 Đăng ký nhóm sharedata_config vào SysConfigData thuộc loại operation_config_type
DECLARE @OpConfigTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysConfigType]
    WHERE [Code] = N'operation_config_type'
      AND [IsDelete] IS NULL
);

IF @OpConfigTypeId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[SysConfigData]
        WHERE [ConfigTypeId] = @OpConfigTypeId
          AND [Code] = N'sharedata_config'
          AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[SysConfigData] (
            [ID], [ConfigTypeId], [Name], [Code], [Value], [TagType],
            [OrderNo], [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
        )
        VALUES (
            LOWER(CAST(NEWID() AS NVARCHAR(64))),
            @OpConfigTypeId,
            N'ShareData Config',
            N'sharedata_config',
            N'sharedata_config',
            N'primary',
            100,
            N'Cấu hình vận hành & giới hạn Chia sẻ dữ liệu (ShareData)',
            1,
            @TenantIdOp,
            GETDATE(),
            N'superadmin'
        );
        PRINT N'   -> Đã đăng ký nhóm [sharedata_config] vào SysConfigData (operation_config_type).';
    END
    ELSE
    BEGIN
        PRINT N'   -> Nhóm [sharedata_config] đã tồn tại trong SysConfigData (operation_config_type), bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'   -> ⚠️ Không tìm thấy SysConfigType [operation_config_type], bỏ qua đăng ký nhóm vào SysConfigData.';
END

-- 4.2 Khai báo danh sách 48 tham số nhóm sharedata_config (19 mã Worker + 23 mã FE + 6 mã BE)
DECLARE @OpConfigs TABLE (
    [Code]    NVARCHAR(128),
    [Name]    NVARCHAR(128),
    [Value]   NVARCHAR(512),
    [OrderNo] INT,
    [Remark]  NVARCHAR(2048)
);

INSERT INTO @OpConfigs ([Code], [Name], [Value], [OrderNo], [Remark])
VALUES
    -- A. 19 tham số vận hành của ShareDataWorker
    (N'outbound_check_interval_sec', N'Chu kỳ quét gửi gói tin (giây)', N'5', 10,
     N'Chu kỳ vòng lặp quét gửi gói tin của Outbound Worker (giây, khoảng cho phép: 1–300).'),
    (N'inbound_check_interval_sec', N'Chu kỳ quét nhận gói tin (giây)', N'5', 11,
     N'Chu kỳ vòng lặp quét nhận gói tin của Inbound Worker (giây, khoảng cho phép: 1–300).'),
    (N'tracking_poll_interval_sec', N'Chu kỳ quét Change Tracking (giây)', N'1', 12,
     N'Chu kỳ vòng lặp quét SQL Server Change Tracking của Tracking Worker (giây, khoảng cho phép: 1–60).'),
    (N'outbound_max_pages_per_run', N'Số trang trích xuất tối đa mỗi lượt gửi', N'20', 20,
     N'Số trang trích xuất tối đa mỗi lượt chạy của một đăng ký gửi (khoảng cho phép: 1–1000).'),
    (N'outbound_lock_budget_percent', N'Ngân sách thời gian giữ khóa gửi (%)', N'50', 21,
     N'Tỷ lệ ngân sách thời gian giữ khóa phân tán mỗi lượt gửi (%, khoảng cho phép: 10–90).'),
    (N'outbound_lock_min_sec', N'Thời gian giữ khóa gửi tối thiểu (giây)', N'300', 22,
     N'Thời gian giữ khóa phân tán tối thiểu của đăng ký gửi (giây, khoảng cho phép: 30–3600).'),
    (N'outbound_lock_interval_multiplier', N'Hệ số nhân chu kỳ gửi tính thời gian giữ khóa', N'3', 23,
     N'Hệ số nhân với chu kỳ gửi để tính thời gian giữ khóa phân tán (khoảng cho phép: 1–10).'),
    (N'outbound_page_size', N'Số dòng mỗi trang trích xuất gửi', N'100', 24,
     N'Số dòng dữ liệu mặc định mỗi trang trích xuất gửi đi (khoảng cho phép: 1–10000).'),
    (N'outbound_default_interval_sec', N'Chu kỳ gửi mặc định của Worker (giây)', N'30', 25,
     N'Chu kỳ gửi mặc định khi đăng ký không khai báo IntervalSeconds (giây, khoảng cho phép: 1–86400).'),
    (N'inbound_max_packets_per_run', N'Số gói tin nhận xử lý tối đa mỗi lượt', N'50', 30,
     N'Số gói tin Pending xử lý tối đa mỗi lượt của Inbound Worker (khoảng cho phép: 1–1000).'),
    (N'inbound_stuck_timeout_min', N'Ngưỡng thu hồi gói nhận bị kẹt (phút)', N'10', 31,
     N'Ngưỡng thời gian thu hồi gói tin Inbound bị kẹt ở trạng thái Processing (phút, khoảng cho phép: 1–1440).'),
    (N'inbound_warn_interval_min', N'Chu kỳ tối thiểu ghi cảnh báo lặp Inbound (phút)', N'5', 32,
     N'Chu kỳ tối thiểu giữa hai lần ghi cảnh báo lặp lại ở Inbound Worker (phút, khoảng cho phép: 1–1440).'),
    (N'inbound_write_timeout_sec', N'Thời gian chờ ghi SQL chiều nhận (giây)', N'120', 33,
     N'Thời gian chờ tối đa khi thực thi câu lệnh SQL ghi dữ liệu nhận về (giây, khoảng cho phép: 10–3600).'),
    (N'tracking_table_retry_min', N'Chu kỳ thử lại bảng nguồn thiếu Change Tracking (phút)', N'5', 40,
     N'Chu kỳ thử khôi phục bảng nguồn bị cô lập do thiếu Change Tracking (phút, khoảng cho phép: 1–1440).'),
    (N'tracking_log_retention_days', N'Số ngày lưu giữ nhật ký hạ tầng Change Tracking', N'7', 41,
     N'Số ngày lưu giữ nhật ký hạ tầng Change Tracking mã ESH-16xx (ngày, khoảng cho phép: 1–365).'),
    (N'tracking_log_purge_batch', N'Số dòng xóa mỗi lô nhật ký Change Tracking', N'1000', 42,
     N'Số dòng xóa tối đa mỗi lô khi dọn nhật ký hạ tầng Change Tracking (khoảng cho phép: 100–50000).'),
    (N'tracking_change_retention_days', N'Số ngày giữ lịch sử CHANGE_RETENTION khi bật CT', N'2', 43,
     N'Số ngày giữ lịch sử thay đổi CHANGE_RETENTION khi tự bật Change Tracking cho CSDL (ngày, khoảng cho phép: 1–30).'),
    (N'cleanup_interval_min', N'Chu kỳ chạy dọn dẹp định kỳ (phút)', N'1440', 46,
     N'Chu kỳ lặp của tác vụ dọn dẹp định kỳ (phút, khoảng cho phép: 60–10080, mặc định 1440 = 24 giờ).'),
    (N'reload_interval_sec', N'Chu kỳ nạp lại cấu hình và danh sách bảng nguồn (giây)', N'30', 47,
     N'Chu kỳ nạp lại cấu hình vận hành và danh sách bảng nguồn từ CSDL (giây, khoảng cho phép: 5–3600, mặc định 30).'),

    -- B. 23 tham số giới hạn giao diện FE & dùng chung WebAPI
    (N'subscription_interval_sec_min', N'Chu kỳ gửi - tối thiểu', N'5', 101,
     N'Giây. Đăng ký gói tin (editSubscription) và kiểm tra SubscriptionValidator.'),
    (N'subscription_interval_sec_max', N'Chu kỳ gửi - tối đa', N'86400', 102,
     N'Giây (24h). Đăng ký gói tin (editSubscription) và kiểm tra SubscriptionValidator.'),
    (N'subscription_interval_sec_default', N'Chu kỳ gửi - mặc định', N'30', 103,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_min', N'Thời gian gom sự kiện - tối thiểu', N'0', 111,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_max', N'Thời gian gom sự kiện - tối đa', N'3600', 112,
     N'Giây (1h). Đăng ký gói tin (editSubscription).'),
    (N'subscription_debounce_sec_default', N'Thời gian gom sự kiện - mặc định', N'30', 113,
     N'Giây. Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_min', N'Độ ưu tiên - tối thiểu', N'0', 121,
     N'Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_max', N'Độ ưu tiên - tối đa', N'10', 122,
     N'Đăng ký gói tin (editSubscription).'),
    (N'subscription_priority_default', N'Độ ưu tiên - mặc định', N'5', 123,
     N'Đăng ký gói tin (editSubscription).'),
    (N'partner_heartbeat_sec_min', N'Heartbeat - tối thiểu', N'5', 201,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_heartbeat_sec_max', N'Heartbeat - tối đa', N'300', 202,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_heartbeat_sec_default', N'Heartbeat - mặc định', N'30', 203,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_min', N'Thời gian chờ phản hồi - tối thiểu', N'3', 211,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_max', N'Thời gian chờ phản hồi - tối đa', N'60', 212,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_response_timeout_sec_default', N'Thời gian chờ phản hồi - mặc định', N'10', 213,
     N'Giây. Đối tác (editPartner).'),
    (N'partner_datagram_size_min', N'Kích thước datagram - tối thiểu', N'512', 221,
     N'Byte. Đối tác (editPartner).'),
    (N'partner_datagram_size_max', N'Kích thước datagram - tối đa', N'65507', 222,
     N'Byte. Payload UDP tối đa 65507. Đối tác (editPartner).'),
    (N'partner_datagram_size_default', N'Kích thước datagram - mặc định', N'1024', 223,
     N'Byte. Đối tác (editPartner).'),
    (N'mapping_sample_top_min', N'Số bản ghi mẫu - tối thiểu', N'1', 301,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'mapping_sample_top_max', N'Số bản ghi mẫu - tối đa', N'50', 302,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'mapping_sample_top_default', N'Số bản ghi mẫu - mặc định', N'5', 303,
     N'Bản ghi. Ánh xạ dữ liệu (editMapping) và PacketQueryHandler.SampleData.'),
    (N'history_range_default_days', N'Khoảng tra cứu - mặc định', N'7', 401,
     N'Ngày. Nhật ký lỗi (errorLog) và lịch sử chia sẻ.'),
    (N'history_range_max_days', N'Khoảng tra cứu - tối đa', N'31', 402,
     N'Ngày. Nhật ký lỗi (errorLog) và lịch sử chia sẻ.'),

    -- C. 6 tham số giới hạn kiểm tra Backend WebAPI & SQL Center
    (N'packet_lookback_days_min', N'Số ngày quét lùi tối đa - tối thiểu', N'1', 501,
     N'Ngày. Giới hạn dưới của MaxLookBackDays khi cấu hình SQL trích xuất gói tin.'),
    (N'packet_lookback_days_max', N'Số ngày quét lùi tối đa - tối đa', N'365', 502,
     N'Ngày. Giới hạn trên của MaxLookBackDays khi cấu hình SQL trích xuất gói tin.'),
    (N'sql_sample_top_min', N'Số dòng xem mẫu SQL Center - tối thiểu', N'1', 511,
     N'Bản ghi. Giới hạn dưới khi chạy thử SQL trích xuất tại SQL Center.'),
    (N'sql_sample_top_max', N'Số dòng xem mẫu SQL Center - tối đa', N'50', 512,
     N'Bản ghi. Giới hạn trên khi chạy thử SQL trích xuất tại SQL Center.'),
    (N'sql_sample_top_default', N'Số dòng xem mẫu SQL Center - mặc định', N'10', 513,
     N'Bản ghi. Số dòng xem mẫu mặc định tại SQL Center (dataSource).'),
    (N'log_list_default_top', N'Số dòng nhật ký trả về mặc định', N'1000', 521,
     N'Bản ghi. Số dòng mặc định trả về khi truy vấn danh sách ActivityLog / AlertLog không truyền TopN.');

-- 4.3 Nạp vào SysOpConfig (nếu SysConfigData của sharedata_fe_config đang có giá trị chưa xóa mềm thì ưu tiên giữ lại)
INSERT INTO [dbo].[SysOpConfig] (
    [ID], [GroupCode], [Code], [Name], [Value],
    [OrderNo], [Remark], [Status], [IsSysConfig], [TenantId], [CreateTime], [CreateUId]
)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    N'sharedata_config',
    src.[Code],
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Name])), N''), src.[Name]),
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Value])), N''), src.[Value]),
    src.[OrderNo],
    COALESCE(NULLIF(LTRIM(RTRIM(oldFe.[Remark])), N''), src.[Remark]),
    1,
    1,
    @TenantIdOp,
    GETDATE(),
    N'superadmin'
FROM @OpConfigs src
OUTER APPLY (
    SELECT TOP 1 d.[Name], d.[Value], d.[Remark]
    FROM [dbo].[SysConfigData] d
    INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
    WHERE t.[Code] = N'sharedata_fe_config'
      AND d.[Code] = src.[Code]
      AND d.[IsDelete] IS NULL
      AND t.[IsDelete] IS NULL
    ORDER BY d.[UpdateTime] DESC, d.[CreateTime] DESC
) oldFe
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[SysOpConfig] target
    WHERE target.[GroupCode] = N'sharedata_config'
      AND target.[Code] = src.[Code]
      AND target.[IsDelete] IS NULL
);

DECLARE @InsertedOpConfigs INT = @@ROWCOUNT;
DECLARE @TotalOpConfigs INT = (SELECT COUNT(1) FROM @OpConfigs);
DECLARE @SkippedOpConfigs INT = @TotalOpConfigs - @InsertedOpConfigs;
PRINT N'   -> Đã seed ' + CAST(@InsertedOpConfigs AS NVARCHAR(10)) + N' tham số SysOpConfig mới; bỏ qua ' + CAST(@SkippedOpConfigs AS NVARCHAR(10)) + N' tham số đã có sẵn trong nhóm [sharedata_config].';

-- 4.4 Xóa mềm các dòng SysConfigData và SysConfigType của nhóm cũ sharedata_fe_config (idempotent)
UPDATE d
SET d.[IsDelete] = GETDATE(),
    d.[Status] = 0,
    d.[UpdateTime] = GETDATE()
FROM [dbo].[SysConfigData] d
INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
WHERE t.[Code] = N'sharedata_fe_config'
  AND d.[IsDelete] IS NULL;

DECLARE @SoftDeletedFeData INT = @@ROWCOUNT;

UPDATE [dbo].[SysConfigType]
SET [IsDelete] = GETDATE(),
    [Status] = 0,
    [UpdateTime] = GETDATE()
WHERE [Code] = N'sharedata_fe_config'
  AND [IsDelete] IS NULL;

DECLARE @SoftDeletedFeType INT = @@ROWCOUNT;
IF @SoftDeletedFeData > 0 OR @SoftDeletedFeType > 0
    PRINT N'   -> Đã xóa mềm ' + CAST(@SoftDeletedFeData AS NVARCHAR(10)) + N' dòng SysConfigData và ' + CAST(@SoftDeletedFeType AS NVARCHAR(10)) + N' dòng SysConfigType của nhóm cũ [sharedata_fe_config].';
ELSE
    PRINT N'   -> Nhóm cũ [sharedata_fe_config] đã được xóa mềm hoặc không tồn tại, bỏ qua.';

-- 4.5 Xóa mềm các tham số không còn sử dụng trong nhóm sharedata_config (idempotent)
UPDATE [dbo].[SysOpConfig]
SET [IsDelete] = GETDATE(),
    [Status] = 0,
    [UpdateTime] = GETDATE()
WHERE [GroupCode] = N'sharedata_config'
  AND [Code] IN (N'cleanup_enabled', N'cleanup_retention_days')
  AND [IsDelete] IS NULL;

DECLARE @SoftDeletedUnusedOpConfigs INT = @@ROWCOUNT;
IF @SoftDeletedUnusedOpConfigs > 0
    PRINT N'   -> Đã xóa mềm ' + CAST(@SoftDeletedUnusedOpConfigs AS NVARCHAR(10)) + N' tham số SysOpConfig không còn sử dụng (cleanup_enabled, cleanup_retention_days).';

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED CẤU HÌNH SHAREDATA (03-sharedata-seed-cau-hinh.sql)';
PRINT N'=============================================================================';
GO
