-- ============================================================================
-- SEED DEMO DATA - Module ShareData (ESHARE V1)
-- Database: [test] / ConfigId: dev_its10
-- Author: Đạt
-- Created date: 29/07/2026
-- Description: Giả lập dữ liệu demo cho các bảng master/cấu hình.
--              Các bảng EshExportLog, EshSystemLog sẽ tự sinh khi runtime.
--              Thứ tự INSERT theo dependency: Partner → DataSource → EventSource
--              → MappingProfile → FieldMapping → Subscription
-- ============================================================================

USE [test];
GO

-- ============================================================================
-- BƯỚC 0: XÓA DỮ LIỆU CŨ (theo thứ tự ngược dependency)
-- ============================================================================
DELETE FROM [dbo].[EshSubscription];
DELETE FROM [dbo].[EshFieldMapping];
DELETE FROM [dbo].[EshMappingProfile];
DELETE FROM [dbo].[EshEventSource];
DELETE FROM [dbo].[EshDataSource];
DELETE FROM [dbo].[EshPartner];
GO

PRINT N'✅ Đã xóa dữ liệu cũ thành công.';
GO

-- ============================================================================
-- 1. EshPartner (Đối tác chia sẻ dữ liệu)
-- ============================================================================
INSERT INTO [dbo].[EshPartner]
    ([ID], [Code], [Name], [Address], [Port], [Username], [PasswordHash],
     [ProtocolProfile], [HeartbeatMaxSec], [DatagramSize], [ResponseTimeoutSec],
     [IsTlsEnabled], [Status], [Notes],
     [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'PTN-001', N'SGTVT-HCM', N'Sở Giao thông Vận tải TP.HCM',
     N'10.10.1.100', 8443, N'sgtvt_hcm', N'$2b$12$hash_sgtvt_hcm_placeholder',
     N'ASN', 30, 4096, 15, 1, N'ENABLED',
     N'Đối tác chính kết nối ITS - Trung tâm điều hành giao thông',
     1, GETDATE(), 1, 1, 0),

    (N'PTN-002', N'VEC-CORP', N'Tổng Công ty Đầu tư Phát triển Đường cao tốc Việt Nam',
     N'172.16.20.50', 9443, N'vec_api', N'$2b$12$hash_vec_placeholder',
     N'XML_A', 60, 8192, 30, 1, N'ENABLED',
     N'Đối tác cao tốc - Chia sẻ dữ liệu giao thông 2 chiều',
     1, GETDATE(), 1, 1, 0),

    (N'PTN-003', N'TTQLDB-III', N'Trung tâm Quản lý Đường bộ III',
     N'192.168.5.10', 443, N'qldb3_user', NULL,
     NULL, 30, 4096, 15, 0, N'ENABLED',
     N'Quản lý đường bộ khu vực miền Trung',
     1, GETDATE(), 1, 1, 0),

    (N'PTN-004', N'KTTV-NB', N'Đài Khí tượng Thủy văn khu vực Nam Bộ',
     N'10.20.30.40', 8080, N'kttv_nb', N'$2b$12$hash_kttv_placeholder',
     N'ASN', 45, 4096, 20, 0, N'DISABLED',
     N'Cung cấp dữ liệu thời tiết - Tạm ngưng do bảo trì hệ thống',
     1, GETDATE(), 1, 1, 0),

    (N'PTN-005', N'CII-INFRA', N'Công ty CP Đầu tư Hạ tầng Kỹ thuật TP.HCM',
     N'api.cii-infra.vn', 443, N'cii_connect', N'$2b$12$hash_cii_placeholder',
     N'XML_A', 30, 4096, 15, 1, N'ENABLED',
     N'Hạ tầng giao thông đô thị - Dữ liệu camera & cảm biến',
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshPartner: 5 đối tác.';
GO

-- ============================================================================
-- 2. EshDataSource (Nguồn dữ liệu nội bộ)
-- ============================================================================
INSERT INTO [dbo].[EshDataSource]
    ([ID], [Code], [Name], [Kind], [DbRef], [TableOrView], [ColumnsJson], [QueryText], [TopN],
     [Notes], [TenantId], [CreateTime], [CreateUId], [RowStatus], [IsDelete])
VALUES
    (N'DS-001', N'DS-TRAFFIC-FLOW', N'Dữ liệu lưu lượng giao thông',
     N'FIELD_PICKER', N'dev_its10', N'vw_TrafficFlow',
     N'[{"name":"StationId","type":"STRING"},{"name":"LaneNo","type":"INT"},{"name":"Speed","type":"DECIMAL"},{"name":"Volume","type":"INT"},{"name":"Occupancy","type":"DECIMAL"},{"name":"RecordTime","type":"DATETIME"}]',
     NULL, 500,
     N'View tổng hợp lưu lượng giao thông từ các trạm đếm xe',
     1, GETDATE(), 1, 1, 0),

    (N'DS-002', N'DS-VMS-STATUS', N'Trạng thái biển báo VMS',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT v.VmsId, v.Location, v.CurrentMessage, v.DisplayMode, v.LastUpdated FROM vw_VmsStatus v WHERE v.IsActive = 1 ORDER BY v.LastUpdated DESC',
     200,
     N'Trạng thái hiện tại các biển báo VMS trên tuyến',
     1, GETDATE(), 1, 1, 0),

    (N'DS-003', N'DS-INCIDENT', N'Sự cố giao thông',
     N'FIELD_PICKER', N'dev_its10', N'tbl_Incident',
     N'[{"name":"IncidentId","type":"STRING"},{"name":"IncidentType","type":"STRING"},{"name":"Location","type":"STRING"},{"name":"Latitude","type":"DECIMAL"},{"name":"Longitude","type":"DECIMAL"},{"name":"Severity","type":"INT"},{"name":"ReportedAt","type":"DATETIME"},{"name":"Status","type":"STRING"}]',
     NULL, 1000,
     N'Bảng quản lý sự cố giao thông trên toàn tuyến',
     1, GETDATE(), 1, 1, 0),

    (N'DS-004', N'DS-WEATHER', N'Dữ liệu thời tiết',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT w.StationCode, w.Temperature, w.Humidity, w.WindSpeed, w.Visibility, w.RainFall, w.RecordTime FROM vw_Weather w WHERE w.RecordTime >= DATEADD(HOUR, -24, GETDATE())',
     5000,
     N'Dữ liệu thời tiết 24h gần nhất từ các trạm quan trắc',
     1, GETDATE(), 1, 1, 0),

    (N'DS-005', N'DS-PARKING', N'Thông tin bãi đỗ xe',
     N'FIELD_PICKER', N'dev_its10', N'tbl_ParkingLot',
     N'[{"name":"LotId","type":"STRING"},{"name":"LotName","type":"STRING"},{"name":"TotalSlots","type":"INT"},{"name":"AvailableSlots","type":"INT"},{"name":"OccupancyRate","type":"DECIMAL"},{"name":"UpdatedAt","type":"DATETIME"}]',
     NULL, 100,
     N'Tình trạng bãi đỗ xe thông minh',
     1, GETDATE(), 1, 1, 0),

    (N'DS-006', N'DS-VEHICLE-DET', N'Phát hiện phương tiện',
     N'SAVED_QUERY', N'dev_its10', NULL, NULL,
     N'SELECT d.CameraId, d.PlateNumber, d.VehicleType, d.Color, d.Speed, d.DetectedAt, d.Direction FROM vw_VehicleDetection d WHERE d.DetectedAt >= DATEADD(HOUR, -1, GETDATE()) ORDER BY d.DetectedAt DESC',
     10000,
     N'Dữ liệu nhận dạng phương tiện từ camera AI',
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshDataSource: 6 nguồn dữ liệu.';
GO

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
     1, GETDATE(), 1, 1, 0),

    (N'EVT-002', N'EVT-INCIDENT-RT', N'Sự kiện sự cố giao thông',
     N'its/incident/alert', N'JSON',
     N'Topic cảnh báo sự cố giao thông realtime - Trigger khi có sự cố mới',
     1, GETDATE(), 1, 1, 0),

    (N'EVT-003', N'EVT-CAMERA-AI', N'Sự kiện phát hiện phương tiện AI',
     N'its/camera/detection/vehicle', N'PROTOBUF',
     N'Topic nhận dữ liệu nhận dạng phương tiện từ hệ thống camera AI - Protobuf format',
     1, GETDATE(), 1, 1, 0),

    (N'EVT-004', N'EVT-VMS-CHANGE', N'Sự kiện thay đổi biển báo VMS',
     N'its/vms/status/changed', N'JSON',
     N'Topic thông báo khi nội dung hiển thị trên biển báo VMS thay đổi',
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshEventSource: 4 nguồn sự kiện.';
GO

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
     1, GETDATE(), 1, 1, 0),

    (N'MP-002', N'MP-INCIDENT-VEC', N'Ánh xạ sự cố giao thông → VEC',
     N'PTN-002', N'XML', N'OUTBOUND', N'DS-003',
     1, N'Mapping sự cố giao thông gửi cho VEC theo XML schema v2.1',
     1, GETDATE(), 1, 1, 0),

    (N'MP-003', N'MP-WEATHER-IN', N'Ánh xạ dữ liệu thời tiết ← Đài KTTV Nam Bộ',
     N'PTN-004', N'JSON', N'INBOUND', NULL,
     1, N'Nhận dữ liệu thời tiết từ Đài KTTV Nam Bộ về hệ thống ITS',
     1, GETDATE(), 1, 1, 0),

    (N'MP-004', N'MP-VMS-QLDB3', N'Ánh xạ trạng thái VMS → TT QLĐB III',
     N'PTN-003', N'JSON', N'OUTBOUND', N'DS-002',
     1, N'Gửi trạng thái biển báo VMS cho Trung tâm Quản lý Đường bộ III',
     1, GETDATE(), 1, 1, 0),

    (N'MP-005', N'MP-CAMERA-CII', N'Ánh xạ phát hiện phương tiện → CII',
     N'PTN-005', N'CSV', N'OUTBOUND', N'DS-006',
     0, N'Tạm tắt - Đang chờ CII cập nhật endpoint mới',
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshMappingProfile: 5 hồ sơ ánh xạ.';
GO

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
     1, GETDATE(), 1, 1, 0),

    (N'FM-002', N'FM-TF-LANE', N'MP-001',
     N'LaneNo', N'lane_number', NULL, N'0', 1, 2,
     1, GETDATE(), 1, 1, 0),

    (N'FM-003', N'FM-TF-SPEED', N'MP-001',
     N'Speed', N'avg_speed_kmh', N'ROUND(Speed, 1)', N'0.0', 1, 3,
     1, GETDATE(), 1, 1, 0),

    (N'FM-004', N'FM-TF-VOLUME', N'MP-001',
     N'Volume', N'vehicle_count', NULL, N'0', 1, 4,
     1, GETDATE(), 1, 1, 0),

    (N'FM-005', N'FM-TF-OCC', N'MP-001',
     N'Occupancy', N'occupancy_pct', N'ROUND(Occupancy * 100, 2)', N'0.00', 0, 5,
     1, GETDATE(), 1, 1, 0),

    (N'FM-006', N'FM-TF-TIME', N'MP-001',
     N'RecordTime', N'timestamp', N'FORMAT(RecordTime, ''yyyy-MM-ddTHH:mm:ssZ'')', NULL, 1, 6,
     1, GETDATE(), 1, 1, 0),

    -- === MP-002: Sự cố → VEC ===
    (N'FM-007', N'FM-INC-ID', N'MP-002',
     N'IncidentId', N'incident_ref', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, 0),

    (N'FM-008', N'FM-INC-TYPE', N'MP-002',
     N'IncidentType', N'type_code', NULL, N'UNKNOWN', 1, 2,
     1, GETDATE(), 1, 1, 0),

    (N'FM-009', N'FM-INC-LOC', N'MP-002',
     N'Location', N'location_desc', NULL, NULL, 1, 3,
     1, GETDATE(), 1, 1, 0),

    (N'FM-010', N'FM-INC-LAT', N'MP-002',
     N'Latitude', N'lat', N'ROUND(Latitude, 6)', NULL, 1, 4,
     1, GETDATE(), 1, 1, 0),

    (N'FM-011', N'FM-INC-LNG', N'MP-002',
     N'Longitude', N'lng', N'ROUND(Longitude, 6)', NULL, 1, 5,
     1, GETDATE(), 1, 1, 0),

    (N'FM-012', N'FM-INC-SEV', N'MP-002',
     N'Severity', N'severity_level', NULL, N'1', 1, 6,
     1, GETDATE(), 1, 1, 0),

    -- === MP-004: VMS → QLDB III ===
    (N'FM-013', N'FM-VMS-ID', N'MP-004',
     N'VmsId', N'vms_id', NULL, NULL, 1, 1,
     1, GETDATE(), 1, 1, 0),

    (N'FM-014', N'FM-VMS-LOC', N'MP-004',
     N'Location', N'vms_location', NULL, NULL, 1, 2,
     1, GETDATE(), 1, 1, 0),

    (N'FM-015', N'FM-VMS-MSG', N'MP-004',
     N'CurrentMessage', N'display_text', NULL, N'(Không có thông tin)', 1, 3,
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshFieldMapping: 15 chi tiết ánh xạ.';
GO

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
    -- SUB1: Giao thông → SGTVT HCM (Batch 5 phút)
    (N'SUB-001', N'SUB-TRAFFIC-SGTVT', N'PTN-001', N'OUTBOUND',
     N'SN-2026-0001', N'101', N'BATCH',
     N'{"cron":"*/5 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     300, NULL, NULL,
     N'IDLE', NULL, N'RAW', 1, N'ACTIVE',
     N'DS-001', N'MP-001', NULL, 0,
     NULL, NULL, DATEADD(DAY, -30, GETDATE()), DATEADD(DAY, -29, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, 0),

    -- SUB2: Sự cố → VEC (Realtime)
    (N'SUB-002', N'SUB-INCIDENT-VEC', N'PTN-002', N'OUTBOUND',
     N'SN-2026-0002', N'103', N'REALTIME',
     NULL,
     0, NULL, NULL,
     N'IDLE', NULL, N'RAW', 1, N'ACTIVE',
     N'DS-003', N'MP-002', N'EVT-002', 5,
     NULL, NULL, DATEADD(DAY, -25, GETDATE()), DATEADD(DAY, -24, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, 0),

    -- SUB3: Thời tiết ← Đài KTTV (Batch 15 phút - Inbound - PAUSED)
    (N'SUB-003', N'SUB-WEATHER-IN', N'PTN-004', N'INBOUND',
     N'SN-2026-0003', N'106', N'BATCH',
     N'{"cron":"*/15 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     900, NULL, NULL,
     N'DISABLED', NULL, N'RAW', 3, N'PAUSED',
     NULL, N'MP-003', NULL, 0,
     NULL, NULL, DATEADD(DAY, -20, GETDATE()), DATEADD(DAY, -19, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, 0),

    -- SUB4: VMS → QLDB III (Batch 10 phút)
    (N'SUB-004', N'SUB-VMS-QLDB3', N'PTN-003', N'OUTBOUND',
     N'SN-2026-0004', N'102', N'BATCH',
     N'{"cron":"*/10 * * * *","timezone":"Asia/Ho_Chi_Minh"}',
     600, NULL, NULL,
     N'IDLE', NULL, N'ZIP', 2, N'ACTIVE',
     N'DS-002', N'MP-004', NULL, 0,
     NULL, NULL, DATEADD(DAY, -15, GETDATE()), DATEADD(DAY, -14, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, 0),

    -- SUB5: Camera AI → CII (On-demand - PENDING)
    (N'SUB-005', N'SUB-CAMERA-CII', N'PTN-005', N'OUTBOUND',
     N'SN-2026-0005', N'109', N'ON_DEMAND',
     NULL,
     0, NULL, NULL,
     N'IDLE', NULL, N'GZIP', 3, N'PENDING',
     N'DS-006', N'MP-005', NULL, 0,
     NULL, NULL, DATEADD(DAY, -3, GETDATE()), NULL, NULL,
     1, GETDATE(), 1, 1, 0),

    -- SUB6: Camera AI realtime → SGTVT HCM
    (N'SUB-006', N'SUB-CAMERA-SGTVT-RT', N'PTN-001', N'OUTBOUND',
     N'SN-2026-0006', N'109', N'REALTIME',
     NULL,
     0, NULL, NULL,
     N'IDLE', NULL, N'RAW', 1, N'ACTIVE',
     N'DS-006', NULL, N'EVT-003', 2,
     NULL, NULL, DATEADD(DAY, -5, GETDATE()), DATEADD(DAY, -4, GETDATE()), N'admin',
     1, GETDATE(), 1, 1, 0);
GO

PRINT N'✅ EshSubscription: 6 đăng ký.';
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
PRINT N'  📋 EshFieldMapping   : 15 chi tiết trường';
PRINT N'  📋 EshSubscription   : 6 đăng ký (4 ACTIVE, 1 PAUSED, 1 PENDING)';
PRINT N'  ─────────────────────────────────────────────────────────';
PRINT N'  ℹ️  EshExportLog     : Tự sinh khi runtime';
PRINT N'  ℹ️  EshSystemLog     : Tự sinh khi runtime';
PRINT N'══════════════════════════════════════════════════════════════';
GO
