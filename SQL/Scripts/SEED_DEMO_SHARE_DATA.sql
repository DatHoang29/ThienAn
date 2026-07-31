-- ============================================================================
-- SEED DEMO DATA - Module ShareData (ESHARE V1)
-- Database: [dev_its10] / ConfigId: dev_its10
-- Author: Đạt
-- Created date: 29/07/2026
-- Description: Script nạp dữ liệu demo mẫu cho các bảng master/cấu hình.
--              Các bảng EshExportLog, EshSystemLog sẽ tự sinh khi runtime.
--              Thứ tự INSERT theo dependency: Partner → DataSource → EventSource
--              → MappingProfile → FieldMapping → Subscription
-- ============================================================================

USE [dev_its10];
GO

BEGIN TRANSACTION;

-- ============================================================================
-- BƯỚC 0: NỚI LỎNG SCHEMA CŨ NẾU CÓ (ALLOW NULL) VÀ XÓA DỮ LIỆU CŨ
-- ============================================================================
-- EshPartner schema fixes
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'DatagramSize')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [DatagramSize] INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'ResponseTimeoutSec')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [ResponseTimeoutSec] INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'HeartbeatMaxSec')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [HeartbeatMaxSec] INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'IsTlsEnabled')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [IsTlsEnabled] BIT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'UseTls')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [UseTls] BIT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshPartner]') AND name = N'ProtocolProfile')
    ALTER TABLE [dbo].[EshPartner] ALTER COLUMN [ProtocolProfile] NVARCHAR(64) NULL;

-- EshMappingProfile schema fixes
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshMappingProfile]') AND name = N'VendorId')
    ALTER TABLE [dbo].[EshMappingProfile] ALTER COLUMN [VendorId] NVARCHAR(64) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshMappingProfile]') AND name = N'DatatypeId')
    ALTER TABLE [dbo].[EshMappingProfile] ALTER COLUMN [DatatypeId] NVARCHAR(64) NULL;

-- EshSubscription schema fixes
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'Guaranteed')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [Guaranteed] BIT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'VendorId')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [VendorId] NVARCHAR(64) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'QueueName')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [QueueName] NVARCHAR(256) NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'RetryCount')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [RetryCount] INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'MaxRetryAttempts')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [MaxRetryAttempts] INT NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[EshSubscription]') AND name = N'QoS')
    ALTER TABLE [dbo].[EshSubscription] ALTER COLUMN [QoS] INT NULL;

-- Clean dữ liệu theo đúng thứ tự phụ thuộc (Foreign Key)
DELETE FROM [dbo].[EshExportLog];
DELETE FROM [dbo].[EshSystemLog];
DELETE FROM [dbo].[EshSubscription];
DELETE FROM [dbo].[EshFieldMapping];
DELETE FROM [dbo].[EshMappingProfile];
DELETE FROM [dbo].[EshEventSource];
DELETE FROM [dbo].[EshDataSource];
DELETE FROM [dbo].[EshPartner];

PRINT N'✅ Đã dọn dẹp schema cũ & xóa dữ liệu cũ thành công.';

-- ============================================================================
-- BƯỚC 0.1: TẠO MOCK VIEWS/TABLES DEMO NẾU CHƯA TỒN TẠI
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_TrafficFlow')
    EXEC(N'CREATE VIEW vw_TrafficFlow AS SELECT N''ST-01'' AS StationId, 1 AS LaneNo, 65.5 AS Speed, 120 AS Volume, 15.2 AS Occupancy, GETDATE() AS RecordTime;');

IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_VmsStatus')
    EXEC(N'CREATE VIEW vw_VmsStatus AS SELECT N''VMS-01'' AS VmsId, N''Km 12+300'' AS Location, N''Tốc độ tối đa 80km/h'' AS CurrentMessage, N''NORMAL'' AS DisplayMode, GETDATE() AS LastUpdated, 1 AS IsActive;');

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_Incident')
    CREATE TABLE tbl_Incident (
        IncidentId NVARCHAR(64), IncidentType NVARCHAR(64), Location NVARCHAR(256),
        Latitude DECIMAL(18,6), Longitude DECIMAL(18,6), Severity INT, ReportedAt DATETIME, Status NVARCHAR(32)
    );

IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_Weather')
    EXEC(N'CREATE VIEW vw_Weather AS SELECT N''W-01'' AS StationCode, 31.5 AS Temperature, 75.0 AS Humidity, 12.0 AS WindSpeed, 10.0 AS Visibility, 0.0 AS RainFall, GETDATE() AS RecordTime;');

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tbl_ParkingLot')
    CREATE TABLE tbl_ParkingLot (
        LotId NVARCHAR(64), LotName NVARCHAR(128), TotalSlots INT, AvailableSlots INT, OccupancyRate DECIMAL(5,2), UpdatedAt DATETIME
    );

IF NOT EXISTS (SELECT 1 FROM sys.views WHERE name = 'vw_VehicleDetection')
    EXEC(N'CREATE VIEW vw_VehicleDetection AS SELECT N''CAM-01'' AS CameraId, N''51H-12345'' AS PlateNumber, N''CAR'' AS VehicleType, N''WHITE'' AS Color, 60.0 AS Speed, GETDATE() AS DetectedAt, N''NORTH'' AS Direction;');

-- ============================================================================
-- 1. EshPartner (Đối tác chia sẻ dữ liệu)
-- ============================================================================
INSERT INTO [dbo].[EshPartner]
    ([ID], [Code], [Name], [Address], [Port], [Username], [PasswordHash],
     [Status],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'PTN-001', N'SGTVT-HCM', N'Sở Giao thông Vận tải TP.HCM',
     N'10.10.1.100', 8443, N'sgtvt_hcm', N'$2b$12$hash_sgtvt_hcm_placeholder',
     N'ENABLED',
     1, GETDATE(), 1, 1, NULL),

    (N'PTN-002', N'VEC-CORP', N'Tổng Công ty Đầu tư Phát triển Đường cao tốc Việt Nam',
     N'172.16.20.50', 9443, N'vec_api', N'$2b$12$hash_vec_placeholder',
     N'ENABLED',
     1, GETDATE(), 1, 1, NULL),

    (N'PTN-003', N'TTQLDB-III', N'Trung tâm Quản lý Đường bộ III',
     N'192.168.5.10', 443, N'qldb3_user', NULL,
     N'ENABLED',
     1, GETDATE(), 1, 1, NULL),

    (N'PTN-004', N'KTTV-NB', N'Đài Khí tượng Thủy văn khu vực Nam Bộ',
     N'10.20.30.40', 8080, N'kttv_nb', N'$2b$12$hash_kttv_placeholder',
     N'ENABLED',
     1, GETDATE(), 1, 1, NULL),

    (N'PTN-005', N'CII-INFRA', N'Công ty CP Đầu tư Hạ tầng Kỹ thuật TP.HCM',
     N'api.cii-infra.vn', 443, N'cii_connect', N'$2b$12$hash_cii_placeholder',
     N'ENABLED',
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshPartner: 5 đối tác.';

-- ============================================================================
-- 2. EshDataSource (Nguồn dữ liệu nội bộ)
-- ============================================================================
INSERT INTO [dbo].[EshDataSource]
    ([ID], [Code], [Name], [Kind], [DbRef], [Table], [ColumnsJson], [QueryText], [TopN],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'DS-001', N'DS-TRAFFIC-FLOW', N'Dữ liệu lưu lượng giao thông',
     N'FIELD_PICKER', N'dev_its10', N'vw_TrafficFlow',
     N'[{"name":"StationId","type":"STRING"},{"name":"LaneNo","type":"INT"},{"name":"Speed","type":"DECIMAL"},{"name":"Volume","type":"INT"},{"name":"Occupancy","type":"DECIMAL"},{"name":"RecordTime","type":"DATETIME"}]',
     NULL, 500,
     1, GETDATE(), 1, 1, NULL),

    (N'DS-002', N'DS-VMS-STATUS', N'Trạng thái biển báo VMS',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT v.VmsId, v.Location, v.CurrentMessage, v.DisplayMode, v.LastUpdated FROM vw_VmsStatus v WHERE v.IsActive = 1 ORDER BY v.LastUpdated DESC',
     200,
     1, GETDATE(), 1, 1, NULL),

    (N'DS-003', N'DS-INCIDENT', N'Sự cố giao thông',
     N'FIELD_PICKER', N'dev_its10', N'tbl_Incident',
     N'[{"name":"IncidentId","type":"STRING"},{"name":"IncidentType","type":"STRING"},{"name":"Location","type":"STRING"},{"name":"Latitude","type":"DECIMAL"},{"name":"Longitude","type":"DECIMAL"},{"name":"Severity","type":"INT"},{"name":"ReportedAt","type":"DATETIME"},{"name":"Status","type":"STRING"}]',
     NULL, 1000,
     1, GETDATE(), 1, 1, NULL),

    (N'DS-004', N'DS-WEATHER', N'Dữ liệu thời tiết',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT w.StationCode, w.Temperature, w.Humidity, w.WindSpeed, w.Visibility, w.RainFall, w.RecordTime FROM vw_Weather w WHERE w.RecordTime >= DATEADD(HOUR, -24, GETDATE())',
     5000,
     1, GETDATE(), 1, 1, NULL),

    (N'DS-005', N'DS-PARKING', N'Thông tin bãi đỗ xe',
     N'FIELD_PICKER', N'dev_its10', N'tbl_ParkingLot',
     N'[{"name":"LotId","type":"STRING"},{"name":"LotName","type":"STRING"},{"name":"TotalSlots","type":"INT"},{"name":"AvailableSlots","type":"INT"},{"name":"OccupancyRate","type":"DECIMAL"},{"name":"UpdatedAt","type":"DATETIME"}]',
     NULL, 100,
     1, GETDATE(), 1, 1, NULL),

    (N'DS-006', N'DS-VEHICLE-DET', N'Phát hiện phương tiện',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT d.CameraId, d.PlateNumber, d.VehicleType, d.Color, d.Speed, d.DetectedAt, d.Direction FROM vw_VehicleDetection d WHERE d.DetectedAt >= DATEADD(HOUR, -1, GETDATE()) ORDER BY d.DetectedAt DESC',
     10000,
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshDataSource: 6 nguồn dữ liệu.';

-- ============================================================================
-- 3. EshEventSource (Nguồn sự kiện thời gian thực)
-- ============================================================================
INSERT INTO [dbo].[EshEventSource]
    ([ID], [Code], [Name], [Subject], [DatatypeCode], [Description],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'EVT-001', N'EVT-TRAFFIC-RT', N'Sự kiện lưu lượng thời gian thực',
     N'its/traffic/flow/realtime', N'JSON',
     N'Topic nhận dữ liệu lưu lượng giao thông realtime từ các trạm đếm xe qua MQTT/Kafka',
     1, GETDATE(), 1, 1, NULL),

    (N'EVT-002', N'EVT-INCIDENT-RT', N'Sự kiện sự cố giao thông',
     N'its/incident/alert', N'JSON',
     N'Topic cảnh báo sự cố giao thông realtime - Trigger khi có sự cố mới',
     1, GETDATE(), 1, 1, NULL),

    (N'EVT-003', N'EVT-CAMERA-AI', N'Sự kiện phát hiện phương tiện AI',
     N'its/camera/detection/vehicle', N'PROTOBUF',
     N'Topic nhận dữ liệu nhận dạng phương tiện từ hệ thống camera AI - Protobuf format',
     1, GETDATE(), 1, 1, NULL),

    (N'EVT-004', N'EVT-VMS-CHANGE', N'Sự kiện thay đổi biển báo VMS',
     N'its/vms/status/changed', N'JSON',
     N'Topic thông báo khi nội dung hiển thị trên biển báo VMS thay đổi',
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshEventSource: 4 nguồn sự kiện.';

-- ============================================================================
-- 4. EshMappingProfile (Hồ sơ cấu hình ánh xạ dữ liệu)
-- ============================================================================
INSERT INTO [dbo].[EshMappingProfile]
    ([ID], [Code], [Name], [PartnerId], [DatatypeId], [Direction], [DataSourceId],
     [IsActive], [Remark],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'MP-001', N'MP-TRAFFIC-SGTVT', N'Ánh xạ lưu lượng giao thông → Sở GTVT HCM',
     N'PTN-001', N'JSON', N'OUTBOUND', N'DS-001',
     1, N'Mapping dữ liệu lưu lượng giao thông gửi cho Sở GTVT theo format chuẩn DATEX II',
     1, GETDATE(), 1, 1, NULL),

    (N'MP-002', N'MP-INCIDENT-VEC', N'Ánh xạ sự cố giao thông → VEC',
     N'PTN-002', N'XML', N'OUTBOUND', N'DS-003',
     1, N'Mapping sự cố giao thông gửi cho VEC theo XML schema v2.1',
     1, GETDATE(), 1, 1, NULL),

    (N'MP-003', N'MP-WEATHER-IN', N'Ánh xạ dữ liệu thời tiết ← Đài KTTV Nam Bộ',
     N'PTN-004', N'JSON', N'INBOUND', N'DS-004',
     1, N'Nhận dữ liệu thời tiết từ Đài KTTV Nam Bộ về hệ thống ITS',
     1, GETDATE(), 1, 1, NULL),

    (N'MP-004', N'MP-VMS-QLDB3', N'Ánh xạ trạng thái VMS → TT QLĐB III',
     N'PTN-003', N'JSON', N'OUTBOUND', N'DS-002',
     1, N'Gửi trạng thái biển báo VMS cho Trung tâm Quản lý Đường bộ III',
     1, GETDATE(), 1, 1, NULL),

    (N'MP-005', N'MP-CAMERA-CII', N'Ánh xạ phát hiện phương tiện → CII',
     N'PTN-005', N'CSV', N'OUTBOUND', N'DS-006',
     1, N'Ánh xạ phương tiện CII',
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshMappingProfile: 5 hồ sơ ánh xạ.';

-- ============================================================================
-- 5. EshFieldMapping (Chi tiết ánh xạ trường dữ liệu)
-- ============================================================================
INSERT INTO [dbo].[EshFieldMapping]
    ([ID], [Code], [MappingProfileId], [SourceKey], [TargetKey],
     [Expression], [DefaultValue], [IsRequired], [OrderNo],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    -- === MP-001: Lưu lượng giao thông → SGTVT ===
    (N'FM-001', N'FM-TF-STATION', N'MP-001',
     N'StationId', N'station_code', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-002', N'FM-TF-LANE', N'MP-001',
     N'LaneNo', N'lane_number', NULL, N'0', 1, 2,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-003', N'FM-TF-SPEED', N'MP-001',
     N'Speed', N'avg_speed_kmh', N'ROUND(Speed, 1)', N'0.0', 1, 3,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-004', N'FM-TF-VOLUME', N'MP-001',
     N'Volume', N'vehicle_count', NULL, N'0', 1, 4,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-005', N'FM-TF-OCC', N'MP-001',
     N'Occupancy', N'occupancy_pct', N'ROUND(Occupancy * 100, 2)', N'0.00', 0, 5,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-006', N'FM-TF-TIME', N'MP-001',
     N'RecordTime', N'timestamp', N'FORMAT(RecordTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 6,
     1, GETDATE(), 1, 1, NULL),

    -- === MP-002: Sự cố → VEC ===
    (N'FM-007', N'FM-INC-ID', N'MP-002',
     N'IncidentId', N'incident_ref', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-008', N'FM-INC-TYPE', N'MP-002',
     N'IncidentType', N'type_code', NULL, N'UNKNOWN', 1, 2,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-009', N'FM-INC-LOC', N'MP-002',
     N'Location', N'location_desc', NULL, NULL, 1, 3,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-010', N'FM-INC-LAT', N'MP-002',
     N'Latitude', N'lat', N'ROUND(Latitude, 6)', NULL, 1, 4,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-011', N'FM-INC-LNG', N'MP-002',
     N'Longitude', N'lng', N'ROUND(Longitude, 6)', NULL, 1, 5,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-012', N'FM-INC-SEV', N'MP-002',
     N'Severity', N'severity_level', NULL, N'1', 1, 6,
     1, GETDATE(), 1, 1, NULL),

    -- === MP-004: VMS → QLDB III ===
    (N'FM-013', N'FM-VMS-ID', N'MP-004',
     N'VmsId', N'vms_id', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-014', N'FM-VMS-LOC', N'MP-004',
     N'Location', N'vms_location', NULL, NULL, 1, 2,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-015', N'FM-VMS-MSG', N'MP-004',
     N'CurrentMessage', N'display_text', NULL, N'(Không có thông tin)', 1, 3,
     1, GETDATE(), 1, 1, NULL),

    -- === MP-003: Thời tiết ===
    (N'FM-016', N'FM-WTH-ID', N'MP-003',
     N'StationCode', N'ma_tram_kttv', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-017', N'FM-WTH-TEMP', N'MP-003',
     N'Temperature', N'nhiet_do', NULL, N'0.0', 1, 2,
     1, GETDATE(), 1, 1, NULL),

    -- === MP-005: Camera AI ===
    (N'FM-018', N'FM-CAM-ID', N'MP-005',
     N'CameraId', N'ma_camera', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, NULL),

    (N'FM-019', N'FM-CAM-PLATE', N'MP-005',
     N'PlateNumber', N'bien_so_xe', NULL, N'KTT', 1, 2,
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshFieldMapping: 19 chi tiết ánh xạ.';

-- ============================================================================
-- 6. EshSubscription (Đăng ký chia sẻ dữ liệu)
-- ============================================================================
INSERT INTO [dbo].[EshSubscription]
    ([ID], [Code], [PartnerId], [Direction], [SerialNbr], [DatatypeId], [Mode],
     [ScheduleJson], [IntervalSeconds], [LastTimeRun], [NextTimeRun],
     [RunStatus], [ProcessLockId], [Format], [Priority], [State],
     [DataSourceId], [MappingProfileId], [EventSourceId], [DebounceSec],
     [RejectReason], [CancelReason], [RequestedAt], [ResolvedAt], [ResolvedBy],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    -- SUB1: Giao thông → SGTVT HCM (Batch 5 phút - ACTIVE)
    (N'SUB-001', N'SUB-TRAFFIC-SGTVT', N'PTN-001', N'OUTBOUND',
     N'SN-2026-0001', N'101', N'BATCH',
     N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     300, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'RAW', 1, N'ACTIVE',
     N'DS-001', N'MP-001', NULL, 0,
     NULL, NULL, DATEADD(DAY, -30, GETDATE()), DATEADD(DAY, -29, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL),

    -- SUB2: Sự cố → VEC (Batch 5 phút - ACTIVE)
    (N'SUB-002', N'SUB-INCIDENT-VEC', N'PTN-002', N'OUTBOUND',
     N'SN-2026-0002', N'103', N'BATCH',
     N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     300, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'RAW', 1, N'ACTIVE',
     N'DS-003', N'MP-002', N'EVT-002', 5,
     NULL, NULL, DATEADD(DAY, -25, GETDATE()), DATEADD(DAY, -24, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL),

    -- SUB3: Thời tiết ← Đài KTTV (Batch 15 phút - INBOUND - ACTIVE)
    (N'SUB-003', N'SUB-WEATHER-IN', N'PTN-004', N'INBOUND',
     N'SN-2026-0003', N'106', N'BATCH',
     N'{"cron":"*/15 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     900, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'RAW', 3, N'ACTIVE',
     N'DS-004', N'MP-003', NULL, 0,
     NULL, NULL, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -19, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL),

    -- SUB4: VMS → QLDB III (Batch 10 phút - ACTIVE)
    (N'SUB-004', N'SUB-VMS-QLDB3', N'PTN-003', N'OUTBOUND',
     N'SN-2026-0004', N'102', N'BATCH',
     N'{"cron":"*/10 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     600, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'ZIP', 2, N'ACTIVE',
     N'DS-002', N'MP-004', NULL, 0,
     NULL, NULL, DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -14, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL),

    -- SUB5: Camera AI → CII (Batch 5 phút - PAUSED)
    (N'SUB-005', N'SUB-CAMERA-CII', N'PTN-005', N'OUTBOUND',
     N'SN-2026-0005', N'109', N'BATCH',
     N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     300, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'GZIP', 3, N'PAUSED',
     N'DS-006', N'MP-005', NULL, 0,
     NULL, NULL, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -2, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL),

    -- SUB6: Camera AI -> SGTVT HCM (Batch 5 phút - PENDING)
    (N'SUB-006', N'SUB-CAMERA-SGTVT-RT', N'PTN-001', N'OUTBOUND',
     N'SN-2026-0006', N'110', N'BATCH',
     N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     300, NULL, DATEADD(SECOND, -10, GETDATE()),
     N'IDLE', NULL, N'RAW', 1, N'PENDING',
     N'DS-006', N'MP-001', N'EVT-003', 2,
     NULL, NULL, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -4, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, NULL);

PRINT N'✅ EshSubscription: 6 đăng ký.';

COMMIT TRANSACTION;
GO

-- ============================================================================
-- TỔNG KẾT
-- ============================================================================
PRINT N'';
PRINT N'══════════════════════════════════════════════════════════════';
PRINT N'  ✅ SEED DEMO DATA HOÀN TẤT - Module ShareData (ESHARE V1)';
PRINT N'══════════════════════════════════════════════════════════════';
PRINT N'  📋 EshPartner        : 5 đối tác';
PRINT N'  📋 EshDataSource     : 6 nguồn dữ liệu';
PRINT N'  📋 EshEventSource    : 4 nguồn sự kiện';
PRINT N'  📋 EshMappingProfile : 5 hồ sơ ánh xạ';
PRINT N'  📋 EshFieldMapping   : 19 chi tiết trường';
PRINT N'  📋 EshSubscription   : 6 đăng ký (4 ACTIVE, 1 PAUSED, 1 PENDING)';
PRINT N'  ─────────────────────────────────────────────────────────';
PRINT N'  ℹ️  EshExportLog     : Tự sinh khi runtime';
PRINT N'  ℹ️  EshSystemLog     : Tự sinh khi runtime';
PRINT N'══════════════════════════════════════════════════════════════';
GO
