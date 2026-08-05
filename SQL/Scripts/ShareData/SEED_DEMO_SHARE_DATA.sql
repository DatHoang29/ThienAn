-- ============================================================================
-- SEED DEMO DATA - Module ShareData (ESHARE V1)
-- Database: [dev_its10] / ConfigId: dev_its10
-- Author: Đạt
-- Created date: 29/07/2026
-- Description: Script nạp dữ liệu demo chuẩn cho các bảng Esh master/cấu hình.
--              Chỉ tác động đến các bảng Esh, KHÔNG tạo bảng giả lập/mock view.
--              Thứ tự INSERT theo dependency: Partner → DataSource → EventSource
--              → MappingProfile → FieldMapping → Subscription
-- ============================================================================

USE [dev_its10];
GO

BEGIN TRANSACTION;

BEGIN TRY
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

    -- Clean dữ liệu theo đúng thứ tự phụ thuộc (Foreign Key từ Con → Cha)
    DELETE FROM [dbo].[EshExportLog];
    DELETE FROM [dbo].[EshSystemLog];
    DELETE FROM [dbo].[EshSubscription];
    DELETE FROM [dbo].[EshFieldMapping];
    DELETE FROM [dbo].[EshMappingProfile];
    DELETE FROM [dbo].[EshEventSource];
    DELETE FROM [dbo].[EshDataSource];
    DELETE FROM [dbo].[EshPartner];

    PRINT N'✅ Đã dọn dẹp schema cũ & xóa dữ liệu cũ các bảng Esh thành công.';

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
    -- 2. EshDataSource (Nguồn dữ liệu nội bộ - Map chuẩn theo CSDL WebAPI)
    -- ============================================================================
    INSERT INTO [dbo].[EshDataSource]
        ([ID], [Code], [Name], [Kind], [DbRef], [Table], [ColumnsJson], [QueryText], [TopN],
         [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
    VALUES
        -- Gói 101: Luồng giao thông → Nguồn TmsZoneStatus & TmsZone
        (N'DS-001', N'DS-TRAFFIC-FLOW', N'Dữ liệu lưu lượng giao thông',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT zs.ZoneId, z.Name AS ZoneName, z.FromKmNumber, z.FromMetNumber, z.ToKmNumber, z.ToMetNumber, z.LaneId, zs.AverageSpeed, zs.Condition AS TrafficCondition, zs.UpdateTime AS DataTime, z.MaxSpeed AS SpeedLimit FROM TmsZoneStatus zs JOIN TmsZone z ON zs.ZoneId = z.ID ORDER BY zs.UpdateTime DESC',
         500,
         1, GETDATE(), 1, 1, NULL),

        -- Gói 108: Biển báo VMS → Nguồn VmsCurrent & TmsEquipment
        (N'DS-002', N'DS-VMS-STATUS', N'Trạng thái biển báo VMS',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT e.Code AS EquipmentCode, v.Name AS VmsName, e.KmNumber AS LocationKm, e.MetNumber AS LocationMet, e.DirectionId AS Direction, e.LaneId, v.RowData AS DisplayContent, v.Url AS DisplayImageUrl, v.Size AS DisplaySize, v.Priority, v.ExecutedDate AS ExecutedTime FROM VmsCurrent v JOIN TmsEquipment e ON v.EquipmentId = e.ID ORDER BY v.ExecutedDate DESC',
         200,
         1, GETDATE(), 1, 1, NULL),

        -- Gói 107: Sự cố giao thông → Nguồn TmsIncident & TmsEventType
        (N'DS-003', N'DS-INCIDENT', N'Sự cố giao thông',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT i.Code AS IncidentCode, i.Name AS IncidentName, i.EventTypeId, et.Name AS EventTypeName, i.StartDate AS OccurredTime, i.KmNumber AS LocationKm, i.MetNumber AS LocationMet, i.Location AS LocationRoute, i.InfluenceScope AS Direction, i.InjuredNumber AS InjuredCount, i.VehicleNumber AS VehicleCount, i.State AS IncidentState, i.Description, i.Source FROM TmsIncident i LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID ORDER BY i.StartDate DESC',
         1000,
         1, GETDATE(), 1, 1, NULL),

        -- Gói 104: Dữ liệu thời tiết → Nguồn TmsWeather
        (N'DS-004', N'DS-WEATHER', N'Dữ liệu thời tiết',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT w.RefId AS WeatherStationId, w.LocationDetail, w.Temperature, w.Hudmidity AS Humidity, w.WindSpeed, w.WindDirection, w.Rain AS Rainfall, w.RainHour AS RainfallHour, w.Foresight AS Visibility, w.Description AS WeatherDescription, w.ShortDescription AS WeatherCode, w.TimeDetect AS DetectTime FROM TmsWeather w WHERE w.TimeDetect IS NOT NULL ORDER BY w.TimeDetect DESC',
         5000,
         1, GETDATE(), 1, 1, NULL),

        -- Gói 109: Dữ liệu thu phí ETC → Nguồn TollTransactionOut & TollLane & TollStation
        (N'DS-005', N'DS-TOLL-ETC', N'Thông tin thu phí ETC',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT t.TransactionId, t.TransactionDateTimeIn AS EntryTime, t.TransactionDateTime AS ExitTime, t.VehicleTypeId, COALESCE(t.PlateEdit, t.Plate, t.PlateLpr) AS LicensePlate, t.TagId, t.LaneId, l.Name AS LaneName, t.StationId, s.Name AS StationName, t.Price AS TollPrice, t.SyncTime FROM TollTransactionOut t LEFT JOIN TollLane l ON t.LaneId = l.LaneId LEFT JOIN TollStation s ON t.StationId = s.StationId ORDER BY t.TransactionDateTime DESC',
         1000,
         1, GETDATE(), 1, 1, NULL),

        -- Gói 103: Dữ liệu dò xe VDS → Nguồn TmsTrafficData & TmsEquipment
        (N'DS-006', N'DS-VEHICLE-DET', N'Phát hiện phương tiện VDS',
         N'SAVED_QUERY', N'dev_its10', NULL, NULL,
         N'SELECT td.ID AS DetectionId, td.DetectTime, td.Type AS VehicleType, td.LicensePlate, td.Speed, td.Lane, td.Direction, td.Location AS LocationRoute, td.EquipmentId, e.KmNumber AS LocationKm, e.MetNumber AS LocationMet FROM TmsTrafficData td LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID ORDER BY td.DetectTime DESC',
         10000,
         1, GETDATE(), 1, 1, NULL);

    PRINT N'✅ EshDataSource: 6 nguồn dữ liệu (khớp bảng WebAPI).';

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
         N'PTN-001', N'101', N'OUTBOUND', N'DS-001',
         1, N'Mapping dữ liệu lưu lượng giao thông gửi cho Sở GTVT theo format chuẩn DATEX II',
         1, GETDATE(), 1, 1, NULL),

        (N'MP-002', N'MP-INCIDENT-VEC', N'Ánh xạ sự cố giao thông → VEC',
         N'PTN-002', N'107', N'OUTBOUND', N'DS-003',
         1, N'Mapping sự cố giao thông gửi cho VEC theo XML schema v2.1',
         1, GETDATE(), 1, 1, NULL),

        (N'MP-003', N'MP-WEATHER-IN', N'Ánh xạ dữ liệu thời tiết ← Đài KTTV Nam Bộ',
         N'PTN-004', N'104', N'INBOUND', N'DS-004',
         1, N'Nhận dữ liệu thời tiết từ Đài KTTV Nam Bộ về hệ thống ITS',
         1, GETDATE(), 1, 1, NULL),

        (N'MP-004', N'MP-VMS-QLDB3', N'Ánh xạ trạng thái VMS → TT QLĐB III',
         N'PTN-003', N'108', N'OUTBOUND', N'DS-002',
         1, N'Gửi trạng thái biển báo VMS cho Trung tâm Quản lý Đường bộ III',
         1, GETDATE(), 1, 1, NULL),

        (N'MP-005', N'MP-VDS-CII', N'Ánh xạ phát hiện phương tiện VDS → CII',
         N'PTN-005', N'103', N'OUTBOUND', N'DS-006',
         1, N'Ánh xạ phương tiện VDS cho CII',
         1, GETDATE(), 1, 1, NULL);

    PRINT N'✅ EshMappingProfile: 5 hồ sơ ánh xạ.';

    -- ============================================================================
    -- 5. EshFieldMapping (Chi tiết ánh xạ trường - Chuẩn theo Assessment 101, 103, 107, 108)
    -- ============================================================================
    INSERT INTO [dbo].[EshFieldMapping]
        ([ID], [Code], [MappingProfileId], [SourceKey], [TargetKey],
         [Expression], [DefaultValue], [IsRequired], [OrderNo],
         [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
    VALUES
        -- === MP-001: Gói 101 - Lưu lượng giao thông → SGTVT ===
        (N'FM-001', N'FM-TF-ZONEID', N'MP-001', N'ZoneId', N'zoneId', NULL, NULL, 1, 1, 1, GETDATE(), 1, 1, NULL),
        (N'FM-002', N'FM-TF-ZONENAME', N'MP-001', N'ZoneName', N'zoneName', NULL, N'Đoạn đường mẫu', 1, 2, 1, GETDATE(), 1, 1, NULL),
        (N'FM-003', N'FM-TF-SPEED', N'MP-001', N'AverageSpeed', N'averageSpeed', N'ROUND(AverageSpeed, 1)', N'0.0', 1, 3, 1, GETDATE(), 1, 1, NULL),
        (N'FM-004', N'FM-TF-COND', N'MP-001', N'TrafficCondition', N'trafficCondition', NULL, N'normal', 1, 4, 1, GETDATE(), 1, 1, NULL),
        (N'FM-005', N'FM-TF-TIME', N'MP-001', N'DataTime', N'dataTime', N'FORMAT(DataTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 5, 1, GETDATE(), 1, 1, NULL),
        (N'FM-006', N'FM-TF-LIMIT', N'MP-001', N'SpeedLimit', N'speedLimit', NULL, N'80', 0, 6, 1, GETDATE(), 1, 1, NULL),

        -- === MP-002: Gói 107 - Sự cố giao thông → VEC ===
        (N'FM-007', N'FM-INC-CODE', N'MP-002', N'IncidentCode', N'incidentCode', NULL, NULL, 1, 1, 1, GETDATE(), 1, 1, NULL),
        (N'FM-008', N'FM-INC-NAME', N'MP-002', N'IncidentName', N'incidentName', NULL, N'Sự cố giao thông', 1, 2, 1, GETDATE(), 1, 1, NULL),
        (N'FM-009', N'FM-INC-TYPEID', N'MP-002', N'EventTypeId', N'eventTypeId', NULL, N'unknown', 1, 3, 1, GETDATE(), 1, 1, NULL),
        (N'FM-010', N'FM-INC-TYPENAME', N'MP-002', N'EventTypeName', N'eventTypeName', NULL, NULL, 1, 4, 1, GETDATE(), 1, 1, NULL),
        (N'FM-011', N'FM-INC-TIME', N'MP-002', N'OccurredTime', N'occurredTime', N'FORMAT(OccurredTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 5, 1, GETDATE(), 1, 1, NULL),
        (N'FM-012', N'FM-INC-KM', N'MP-002', N'LocationKm', N'locationKm', NULL, N'0', 1, 6, 1, GETDATE(), 1, 1, NULL),
        (N'FM-013', N'FM-INC-MET', N'MP-002', N'LocationMet', N'locationMet', NULL, N'0', 1, 7, 1, GETDATE(), 1, 1, NULL),
        (N'FM-014', N'FM-INC-STATE', N'MP-002', N'IncidentState', N'incidentState', NULL, N'open', 1, 8, 1, GETDATE(), 1, 1, NULL),

        -- === MP-004: Gói 108 - Biển báo VMS → QLDB III ===
        (N'FM-015', N'FM-VMS-CODE', N'MP-004', N'EquipmentCode', N'equipmentCode', NULL, NULL, 1, 1, 1, GETDATE(), 1, 1, NULL),
        (N'FM-016', N'FM-VMS-NAME', N'MP-004', N'VmsName', N'vmsName', NULL, NULL, 1, 2, 1, GETDATE(), 1, 1, NULL),
        (N'FM-017', N'FM-VMS-CONTENT', N'MP-004', N'DisplayContent', N'displayContent', NULL, N'{}', 1, 3, 1, GETDATE(), 1, 1, NULL),
        (N'FM-018', N'FM-VMS-TIME', N'MP-004', N'ExecutedTime', N'executedTime', N'FORMAT(ExecutedTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 4, 1, GETDATE(), 1, 1, NULL),

        -- === MP-003: Gói 104 - Thời tiết ===
        (N'FM-019', N'FM-WTH-ID', N'MP-003', N'WeatherStationId', N'weatherStationId', NULL, NULL, 1, 1, 1, GETDATE(), 1, 1, NULL),
        (N'FM-020', N'FM-WTH-TEMP', N'MP-003', N'Temperature', N'temperature', NULL, N'0.0', 1, 2, 1, GETDATE(), 1, 1, NULL),
        (N'FM-021', N'FM-WTH-HUM', N'MP-003', N'Humidity', N'humidity', NULL, N'0', 1, 3, 1, GETDATE(), 1, 1, NULL),

        -- === MP-005: Gói 103 - Dò xe VDS ===
        (N'FM-022', N'FM-VDS-ID', N'MP-005', N'DetectionId', N'detectionId', NULL, NULL, 1, 1, 1, GETDATE(), 1, 1, NULL),
        (N'FM-023', N'FM-VDS-TIME', N'MP-005', N'DetectTime', N'detectTime', N'FORMAT(DetectTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 2, 1, GETDATE(), 1, 1, NULL),
        (N'FM-024', N'FM-VDS-SPEED', N'MP-005', N'Speed', N'speed', N'ROUND(Speed, 1)', N'0.0', 1, 3, 1, GETDATE(), 1, 1, NULL);

    PRINT N'✅ EshFieldMapping: 24 chi tiết ánh xạ chuẩn.';

    -- ============================================================================
    -- 6. EshSubscription (Đăng ký chia sẻ dữ liệu - Liên kết chuẩn FK)
    -- ============================================================================
    INSERT INTO [dbo].[EshSubscription]
        ([ID], [Code], [PartnerId], [Direction], [SerialNbr], [DatatypeId], [Mode],
         [ScheduleJson], [IntervalSeconds], [LastTimeRun], [NextTimeRun],
         [RunStatus], [Format], [Priority], [State],
         [DataSourceId], [MappingProfileId], [EventSourceId], [DebounceSec],
         [RejectReason], [CancelReason], [RequestedAt], [ResolvedAt], [ResolvedBy],
         [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
    VALUES
        -- SUB1: Giao thông → SGTVT HCM (Gói 101 - Batch 5 phút - ACTIVE)
        (N'SUB-001', N'SUB-TRAFFIC-SGTVT', N'PTN-001', N'OUTBOUND',
         N'SN-2026-0001', N'101', N'BATCH',
         N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         300, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'RAW', 1, N'ACTIVE',
         N'DS-001', N'MP-001', NULL, 0,
         NULL, NULL, DATEADD(DAY, -30, GETDATE()), DATEADD(DAY, -29, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL),

        -- SUB2: Sự cố → VEC (Gói 107 - Realtime Trigger + Debounce - ACTIVE)
        (N'SUB-002', N'SUB-INCIDENT-VEC', N'PTN-002', N'OUTBOUND',
         N'SN-2026-0002', N'107', N'BATCH',
         N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         300, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'RAW', 1, N'ACTIVE',
         N'DS-003', N'MP-002', N'EVT-002', 5,
         NULL, NULL, DATEADD(DAY, -25, GETDATE()), DATEADD(DAY, -24, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL),

        -- SUB3: Thời tiết ← Đài KTTV (Gói 104 - Batch 15 phút - INBOUND - ACTIVE)
        (N'SUB-003', N'SUB-WEATHER-IN', N'PTN-004', N'INBOUND',
         N'SN-2026-0003', N'104', N'BATCH',
         N'{"cron":"*/15 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         900, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'RAW', 3, N'ACTIVE',
         N'DS-004', N'MP-003', NULL, 0,
         NULL, NULL, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -19, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL),

        -- SUB4: VMS → QLDB III (Gói 108 - Batch 10 phút - ACTIVE)
        (N'SUB-004', N'SUB-VMS-QLDB3', N'PTN-003', N'OUTBOUND',
         N'SN-2026-0004', N'108', N'BATCH',
         N'{"cron":"*/10 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         600, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'ZIP', 2, N'ACTIVE',
         N'DS-002', N'MP-004', NULL, 0,
         NULL, NULL, DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -14, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL),

        -- SUB5: Dò xe VDS → CII (Gói 103 - Batch 5 phút - PAUSED)
        (N'SUB-005', N'SUB-VDS-CII', N'PTN-005', N'OUTBOUND',
         N'SN-2026-0005', N'103', N'BATCH',
         N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         300, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'GZIP', 3, N'PAUSED',
         N'DS-006', N'MP-005', NULL, 0,
         NULL, NULL, DATEADD(DAY, -3, GETDATE()), DATEADD(DAY, -2, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL),

        -- SUB6: Camera AI -> SGTVT HCM (Gói 102 - PENDING)
        (N'SUB-006', N'SUB-CAMERA-SGTVT-RT', N'PTN-001', N'OUTBOUND',
         N'SN-2026-0006', N'102', N'BATCH',
         N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
         300, NULL, DATEADD(SECOND, -10, GETDATE()),
         N'IDLE', N'RAW', 1, N'PENDING',
         N'DS-006', N'MP-001', N'EVT-003', 2,
         NULL, NULL, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -4, GETDATE()), N'admin',
         1, GETDATE(), 1, 1, NULL);

    PRINT N'✅ EshSubscription: 6 đăng ký (đã nối đúng FK).';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    PRINT N'❌ Lỗi khi thực thi Seed Script: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ============================================================================
-- TỔNG KẾT
-- ============================================================================
PRINT N'';
PRINT N'══════════════════════════════════════════════════════════════';
PRINT N'  ✅ SEED DEMO DATA HOÀN TẤT - Module ShareData (ESHARE V1)';
PRINT N'══════════════════════════════════════════════════════════════';
PRINT N'  📋 EshPartner        : 5 đối tác';
PRINT N'  📋 EshDataSource     : 6 nguồn dữ liệu (Query trực tiếp bảng WebAPI)';
PRINT N'  📋 EshEventSource    : 4 nguồn sự kiện';
PRINT N'  📋 EshMappingProfile : 5 hồ sơ ánh xạ';
PRINT N'  📋 EshFieldMapping   : 24 chi tiết trường (Khớp shareData-assessment.md)';
PRINT N'  📋 EshSubscription   : 6 đăng ký (4 ACTIVE, 1 PAUSED, 1 PENDING)';
PRINT N'  ─────────────────────────────────────────────────────────';
PRINT N'  ℹ️  EshExportLog     : Tự sinh khi runtime';
PRINT N'  ℹ️  EshSystemLog     : Tự sinh khi runtime';
PRINT N'══════════════════════════════════════════════════════════════';
GO
