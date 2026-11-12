USE [DEV_ITS015_Inbound];
GO

-- =========================================================================================
-- FILE: sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql
-- BẢNG GHI: 14 bảng đích của câu ghi chiều nhận, trong CSDL nhận riêng (*_Inbound), KHÔNG phải CSDL vận hành.
--           Ngoại lệ Rule 19.103 (một tệp một bảng): đây là việc dựng khung cho một CSDL riêng, chỉ tạo bảng,
--           không ghi dữ liệu; tách 14 tệp thì quá vụn.
-- DESCRIPTION: Đặc tả (CHUC-NANG-SHAREDATA.md AC-IN-06): dữ liệu nhận về chỉ ghi vào CSDL *_Inbound.
--              Câu ghi chuẩn Catalog gói 101 - 109 (sharedata-seed-goi-tin-ShareDataSql.sql, bước 6.3) MERGE vào các
--              bảng dưới đây qua kết nối ShareDataDB (Database.json). Các bảng này không thuộc entity của ShareDataDB
--              nên CodeFirst không tạo; thiếu bảng thì nhận dữ liệu báo lỗi "Invalid object name".
--              Cấu trúc chép từ bảng cùng tên trên DEV_ITS015 ngày 06/11/2026: cột, kiểu, cho phép trống, giá trị mặc định,
--              khóa chính; kèm chỉ mục phụ theo đúng cột khóa ON của các câu MERGE chiều nhận gói 101–109 để không quét bảng
--              khi nhiều dịch vụ nhận chạy song song.
--              Bảng/chỉ mục đã có thì bỏ qua (không sửa cấu trúc). Entity bảng gốc đổi cột thì sửa tay bảng tương ứng ở đây.
-- THỨ TỰ CHẠY: Sau khi CodeFirst tạo CSDL nhận và bảng ShareDataInboundPacket (ConfigId ShareDataDB) (xem README.md).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi CSDL nhận *_Inbound. Chạy trên CSDL không có hậu tố _Inbound thì tệp tự bỏ qua.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 12/11/2026 (bổ sung chỉ mục theo khóa MERGE của câu ghi chiều nhận gói 101–109)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql';
PRINT N'=============================================================================';
GO

IF DB_NAME() NOT LIKE N'%[_]Inbound'
BEGIN
    PRINT N'⛔ Tệp này chỉ chạy trên CSDL nhận (*_Inbound), đang kết nối ' + DB_NAME() + N'. Bỏ qua toàn bộ tệp.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 1. TẠO BẢNG ĐÍCH CÒN THIẾU
-- -----------------------------------------------------------------------------------------
DECLARE @Created INT = 0;

-- TmsZone (gói 101)
IF OBJECT_ID(N'[dbo].[TmsZone]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsZone]
    (
        [ID] nvarchar(64) NOT NULL,
        [Name] nvarchar(64) NULL,
        [Pid] nvarchar(64) NULL,
        [FromKmNumber] int NULL,
        [FromMetNumber] int NULL,
        [ToKmNumber] int NULL,
        [ToMetNumber] int NULL,
        [Status] int NULL,
        [LaneId] nvarchar(32) NULL,
        [Remark] nvarchar(256) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Type] nvarchar(32) NULL,
        [MaxSpeed] float NULL,
        [Location] nvarchar(32) NULL,
        [MinSpeed] float NULL,
        CONSTRAINT [PK_TmsZone] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsZone';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsZone';

-- TmsTrafficStatistic (gói 101)
IF OBJECT_ID(N'[dbo].[TmsTrafficStatistic]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsTrafficStatistic]
    (
        [ID] nvarchar(64) NOT NULL,
        [Direction] nvarchar(512) NULL,
        [KmNumber] int NULL,
        [MetNumber] int NULL,
        [AvgSpeed] float NULL,
        [LocationDetail] int NULL,
        [FromTime] datetime NULL,
        [ToTime] datetime NULL,
        [Location] nvarchar(16) NULL,
        [Remark] nvarchar(256) NULL,
        [IpAddress] nvarchar(32) NULL,
        [EquipmentId] nvarchar(64) NULL,
        [TotalVehicleNumber] int NULL,
        [SVehicleNumber] int NULL,
        [MVehicleNumber] int NULL,
        [LVehicleNumber] int NULL,
        [OVehicleNumber] int NULL,
        [SpaceOccupy] float NULL,
        [TimeOccupy] float NULL,
        [SpaceHeadway] float NULL,
        [TimeHeadway] float NULL,
        [StopTime] float NULL,
        [QueueLength] float NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [VendorEventId] bigint NOT NULL CONSTRAINT [DF_TmsTrafficStatistic_VendorEventId] DEFAULT ((0)),
        [PeriodMinutes] int NULL,
        [FlowVph] int NULL,
        [DensityVpkm] float NULL,
        [SpaceMeanKmh] float NULL,
        [SpeedSample] int NULL,
        [ZoneId] nvarchar(64) NULL,
        CONSTRAINT [PK_TmsTrafficStatistic] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsTrafficStatistic';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsTrafficStatistic';

-- TmsZoneStatus (gói 101)
IF OBJECT_ID(N'[dbo].[TmsZoneStatus]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsZoneStatus]
    (
        [ID] nvarchar(64) NOT NULL,
        [ZoneId] nvarchar(64) NULL,
        [AverageSpeed] nvarchar(16) NULL,
        [Condition] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [DetectWay] datetime NULL,
        [DetectStop] datetime NULL,
        [DetectSlow] datetime NULL,
        [DetectOthers] nvarchar(1024) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        CONSTRAINT [PK_TmsZoneStatus] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsZoneStatus';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsZoneStatus';

-- TmsEquipment (gói 102, 103, 108)
IF OBJECT_ID(N'[dbo].[TmsEquipment]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsEquipment]
    (
        [ID] nvarchar(64) NOT NULL,
        [Name] nvarchar(64) NULL,
        [Ip] nvarchar(32) NULL,
        [Port] nvarchar(8) NULL,
        [Url] nvarchar(128) NULL,
        [Username] nvarchar(64) NULL,
        [Password] nvarchar(64) NULL,
        [Status] int NULL,
        [KmNumber] int NULL,
        [MetNumber] int NULL,
        [EquipmentTypeId] nvarchar(64) NULL,
        [LaneId] nvarchar(32) NULL,
        [DirectionId] int NULL,
        [ActiveDirection] int NULL,
        [Location] nvarchar(32) NULL,
        [Remark] nvarchar(256) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [OrderNo] int NULL,
        [EquipmentState] int NULL,
        [Vendor] nvarchar(16) NULL,
        [Manufacturer] nvarchar(128) NULL,
        [ActiveSize] nvarchar(64) NULL,
        [Config] nvarchar(512) NULL,
        [Pid] nvarchar(255) NULL,
        [RefId] nvarchar(255) NULL,
        [IsScan] bit NULL,
        [EquipmentVisible] int NULL,
        CONSTRAINT [PK_TmsEquipment] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsEquipment';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsEquipment';

-- CctvDevice (gói 102)
IF OBJECT_ID(N'[dbo].[CctvDevice]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CctvDevice]
    (
        [ID] nvarchar(64) NOT NULL,
        [DeviceId] nvarchar(64) NOT NULL,
        [Ip] nvarchar(32) NULL,
        [Name] nvarchar(64) NULL,
        [Url] nvarchar(512) NULL,
        [DeviceState] int NULL,
        [Status] int NULL,
        [LastSyncTime] datetime NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [SnapshotUrl] nvarchar(512) NULL,
        [SnapshotTime] datetime NULL,
        CONSTRAINT [PK_CctvDevice] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng CctvDevice';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng CctvDevice';

-- TmsTrafficData (gói 103, 106)
IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsTrafficData]
    (
        [ID] nvarchar(64) NOT NULL,
        [DetectTime] datetime NULL,
        [Location] nvarchar(32) NULL,
        [Lane] nvarchar(32) NULL,
        [Speed] float NULL,
        [IpAddress] nvarchar(32) NULL,
        [Height] int NULL,
        [Width] int NULL,
        [Length] int NULL,
        [AlarmHeight] bit NULL,
        [AlarmWidth] bit NULL,
        [EquipmentId] nvarchar(64) NULL,
        [LicensePlate] nvarchar(32) NULL,
        [Type] nvarchar(32) NULL,
        [Direction] nvarchar(32) NULL,
        [AlarmLength] bit NULL,
        [AlarmSpeed] bit NULL,
        [ZoneId] nvarchar(64) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [PlateColor] nvarchar(32) NULL,
        [VehicleBrand] nvarchar(64) NULL,
        [VehicleColor] nvarchar(32) NULL,
        [VehicleType] nvarchar(32) NULL,
        [VendorEventId] bigint NOT NULL CONSTRAINT [DF_TmsTrafficData_VendorEventId] DEFAULT ((0)),
        [EventType] nvarchar(32) NULL,
        [Source] nvarchar(32) NULL,
        [MacAddress] nvarchar(32) NULL,
        CONSTRAINT [PK_TmsTrafficData] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsTrafficData';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsTrafficData';

-- TmsWeather (gói 104)
IF OBJECT_ID(N'[dbo].[TmsWeather]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsWeather]
    (
        [ID] nvarchar(64) NOT NULL,
        [RefId] nvarchar(64) NULL,
        [LocationDetail] nvarchar(128) NULL,
        [Temperature] float NULL,
        [Hudmidity] float NULL,
        [WindDirection] nvarchar(32) NULL,
        [WindSpeed] float NULL,
        [Rain] float NULL,
        [RainHour] float NULL,
        [Foresight] float NULL,
        [TimeDetect] datetime NULL,
        [Description] nvarchar(128) NULL,
        [ShortDescription] nvarchar(32) NULL,
        [Type] nvarchar(32) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        CONSTRAINT [PK_TmsWeather] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsWeather';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsWeather';

-- TmsVehicleRegistration (gói 105)
IF OBJECT_ID(N'[dbo].[TmsVehicleRegistration]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsVehicleRegistration]
    (
        [ID] nvarchar(64) NOT NULL,
        [LicensePlate] nvarchar(32) NULL,
        [VehicleType] nvarchar(64) NULL,
        [Brand] nvarchar(64) NULL,
        [YearManufacture] nvarchar(64) NULL,
        [Owner] nvarchar(64) NULL,
        [Address] nvarchar(128) NULL,
        [EngineNumber] nvarchar(64) NULL,
        [ChassisNumber] nvarchar(64) NULL,
        [OverallDimensions] nvarchar(64) NULL,
        [ContainerDimensions] nvarchar(64) NULL,
        [Seat] nvarchar(32) NULL,
        [InspectionStamp] nvarchar(64) NULL,
        [DateRegistrations] datetime NULL,
        [ExpirationDate] datetime NULL,
        [DataType] nvarchar(16) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        CONSTRAINT [PK_TmsVehicleRegistration] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsVehicleRegistration';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsVehicleRegistration';

-- TollTransactionOut (gói 105, 109)
IF OBJECT_ID(N'[dbo].[TollTransactionOut]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TollTransactionOut]
    (
        [ID] nvarchar(64) NOT NULL,
        [TransactionId] nvarchar(128) NULL,
        [StationIdIn] nvarchar(32) NULL,
        [StationId] nvarchar(32) NULL,
        [LaneIdIn] nvarchar(32) NULL,
        [LaneId] nvarchar(32) NULL,
        [StatusChecked] nvarchar(32) NULL,
        [VehicleTypeId] nvarchar(64) NULL,
        [ReasonId] nvarchar(64) NULL,
        [TicketId] nvarchar(64) NULL,
        [TicketTypeId] nvarchar(64) NULL,
        [Price] decimal(18, 2) NULL,
        [TagId] nvarchar(64) NULL,
        [Plate] nvarchar(32) NULL,
        [PlateLpr] nvarchar(32) NULL,
        [PlateEdit] nvarchar(32) NULL,
        [TransactionDateTime] datetime NULL,
        [TransactionDateTimeIn] datetime NULL,
        [StatusId] nvarchar(128) NULL,
        [ImageLane] nvarchar(256) NULL,
        [ImagePlate] nvarchar(256) NULL,
        [ImageLaneIn] nvarchar(256) NULL,
        [ImagePlateIn] nvarchar(256) NULL,
        [UserId] nvarchar(64) NULL,
        [OperatorName] nvarchar(128) NULL,
        [ShiftCode] nvarchar(64) NULL,
        [ProcessDate] datetime NULL,
        [Serial] nvarchar(128) NULL,
        [SyncTime] datetime NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        CONSTRAINT [PK_TollTransactionOut] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TollTransactionOut';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TollTransactionOut';

-- TmsEventType (gói 107)
IF OBJECT_ID(N'[dbo].[TmsEventType]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsEventType]
    (
        [ID] nvarchar(64) NOT NULL,
        [Name] nvarchar(64) NULL,
        [Type] nvarchar(32) NULL,
        [WarningType] nvarchar(32) NULL,
        [Status] int NULL,
        [ModuleId] nvarchar(64) NULL,
        [Priority] int NULL,
        [Remark] nvarchar(256) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Url] nvarchar(256) NULL,
        [Size] nvarchar(32) NULL,
        [DefaultPlayMode] varchar(16) NULL,
        CONSTRAINT [PK_TmsEventType] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsEventType';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsEventType';

-- TmsIncident (gói 107)
IF OBJECT_ID(N'[dbo].[TmsIncident]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TmsIncident]
    (
        [ID] nvarchar(64) NOT NULL,
        [Name] nvarchar(128) NULL,
        [EventTypeId] nvarchar(64) NULL,
        [StartDate] datetime NULL,
        [Source] nvarchar(32) NULL,
        [Location] nvarchar(32) NULL,
        [ZoneId] nvarchar(1024) NULL,
        [KmNumber] int NULL,
        [MetNumber] int NULL,
        [InfluenceScope] nvarchar(32) NULL,
        [InjuredNumber] int NULL,
        [VehicleNumber] int NULL,
        [CreatedBy] nvarchar(64) NULL,
        [CreatedDate] datetime NULL,
        [ValidatedBy] nvarchar(64) NULL,
        [ValidatedDate] datetime NULL,
        [ApprovedBy] nvarchar(64) NULL,
        [ApprovedDate] datetime NULL,
        [FinishedBy] nvarchar(64) NULL,
        [FinishedDate] datetime NULL,
        [TrafficPlanId] nvarchar(64) NULL,
        [EquipmentId] nvarchar(1024) NULL,
        [State] nvarchar(32) NULL,
        [Description] nvarchar(512) NULL,
        [Remark] nvarchar(256) NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Metadata] nvarchar(512) NULL,
        [EventId] nvarchar(64) NULL,
        [ProcessMode] nvarchar(32) NOT NULL CONSTRAINT [DF_TmsIncident_ProcessMode] DEFAULT ('manual'),
        [ConditionKey] nvarchar(64) NULL,
        [EventGroup] nvarchar(16) NULL,
        CONSTRAINT [PK_TmsIncident] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TmsIncident';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TmsIncident';

-- VmsCurrent (gói 108)
IF OBJECT_ID(N'[dbo].[VmsCurrent]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[VmsCurrent]
    (
        [ID] nvarchar(64) NOT NULL,
        [EquipmentId] nvarchar(64) NULL,
        [Name] nvarchar(64) NULL,
        [RowData] nvarchar(1024) NULL,
        [DefaultRowData] nvarchar(1024) NULL,
        [Url] nvarchar(512) NULL,
        [DefaultUrl] nvarchar(512) NULL,
        [Size] nvarchar(32) NULL,
        [Font] nvarchar(32) NULL,
        [DefaultFont] nvarchar(32) NULL,
        [FontSize] nvarchar(32) NULL,
        [Color] nvarchar(32) NULL,
        [DefaultColor] nvarchar(32) NULL,
        [BackColor] nvarchar(32) NULL,
        [DefaultBackColor] nvarchar(32) NULL,
        [ExecutedBy] nvarchar(64) NULL,
        [ExecutedDate] datetime NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Priority] int NULL,
        [Brightness] int NULL,
        CONSTRAINT [PK_VmsCurrent] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng VmsCurrent';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng VmsCurrent';

-- TollStation (gói 109)
IF OBJECT_ID(N'[dbo].[TollStation]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TollStation]
    (
        [ID] nvarchar(64) NOT NULL,
        [StationId] nvarchar(64) NULL,
        [Name] nvarchar(64) NULL,
        [Note] nvarchar(256) NULL,
        [Company] nvarchar(128) NULL,
        [StationType] int NULL,
        [StationBE] nvarchar(128) NULL,
        [Type] nvarchar(32) NULL,
        [Active] int NULL,
        [LastSyncTime] datetime NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Remark] nvarchar(256) NULL,
        [RefId] nvarchar(64) NULL,
        CONSTRAINT [PK_TollStation] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TollStation';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TollStation';

-- TollLane (gói 109)
IF OBJECT_ID(N'[dbo].[TollLane]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TollLane]
    (
        [ID] nvarchar(64) NOT NULL,
        [LaneId] nvarchar(64) NULL,
        [Name] nvarchar(64) NULL,
        [Note] nvarchar(256) NULL,
        [StationId] nvarchar(64) NULL,
        [Direction] nvarchar(16) NULL,
        [LaneBE] nvarchar(16) NULL,
        [LastSyncTime] datetime NULL,
        [TenantId] nvarchar(64) NULL,
        [Code] nvarchar(64) NULL,
        [CreateTime] datetime NULL,
        [CreateUId] nvarchar(64) NULL,
        [UpdateTime] datetime NULL,
        [UpdateUId] nvarchar(64) NULL,
        [RowStatus] nvarchar(32) NULL,
        [IsDelete] datetime NULL,
        [Remark] nvarchar(256) NULL,
        CONSTRAINT [PK_TollLane] PRIMARY KEY CLUSTERED ([ID])
    );
    SET @Created = @Created + 1;
    PRINT N'   - Đã tạo bảng TollLane';
END
ELSE
    PRINT N'   - [ĐÃ CÓ] Bảng TollLane';

PRINT N'-> 1. Đã tạo ' + CAST(@Created AS NVARCHAR(10)) + N' bảng đích.';
GO

-- -----------------------------------------------------------------------------------------
-- 2. TẠO CHỈ MỤC THEO KHÓA MERGE CỦA CÂU GHI CHIỀU NHẬN (GÓI 101 - 109)
-- -----------------------------------------------------------------------------------------
-- Các câu MERGE chuẩn (sharedata-seed-goi-tin-ShareDataSql.sql, bước 6.3) dùng WITH (HOLDLOCK).
-- Nếu bảng đích chỉ có PK trên [ID] mà câu MERGE lại khớp theo cột nghiệp vụ ([Code], [TransactionId], [ZoneId]...),
-- SQL Server phải quét và giữ khóa cả bảng ở mỗi lượt nhận.
DECLARE @IdxCreated INT = 0;

-- TmsTrafficStatistic (gói 101: ON t.ZoneId = s.zoneId)
IF OBJECT_ID(N'[dbo].[TmsTrafficStatistic]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsTrafficStatistic]') AND [name] = N'IX_TmsTrafficStatistic_ZoneId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsTrafficStatistic_ZoneId] ON [dbo].[TmsTrafficStatistic] ([ZoneId]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsTrafficStatistic_ZoneId';
END;

-- TmsEquipment (gói 102, 108: ON t.Code = s.cameraCode / s.equipmentCode)
IF OBJECT_ID(N'[dbo].[TmsEquipment]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsEquipment]') AND [name] = N'IX_TmsEquipment_Code')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsEquipment_Code] ON [dbo].[TmsEquipment] ([Code]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsEquipment_Code';
END;

-- CctvDevice (gói 102: ON t.Code = s.cameraCode)
IF OBJECT_ID(N'[dbo].[CctvDevice]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[CctvDevice]') AND [name] = N'IX_CctvDevice_Code')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CctvDevice_Code] ON [dbo].[CctvDevice] ([Code]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_CctvDevice_Code';
END;

-- TmsWeather (gói 104: ON t.RefId = s.weatherStationId AND t.TimeDetect = s.detectTime)
IF OBJECT_ID(N'[dbo].[TmsWeather]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsWeather]') AND [name] = N'IX_TmsWeather_RefId_TimeDetect')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsWeather_RefId_TimeDetect] ON [dbo].[TmsWeather] ([RefId], [TimeDetect]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsWeather_RefId_TimeDetect';
END;

-- TmsVehicleRegistration (gói 105: ON t.LicensePlate = s.licensePlate)
IF OBJECT_ID(N'[dbo].[TmsVehicleRegistration]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsVehicleRegistration]') AND [name] = N'IX_TmsVehicleRegistration_LicensePlate')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsVehicleRegistration_LicensePlate] ON [dbo].[TmsVehicleRegistration] ([LicensePlate]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsVehicleRegistration_LicensePlate';
END;

-- TollTransactionOut (gói 105, 109: ON t.TransactionId = s.transactionId)
IF OBJECT_ID(N'[dbo].[TollTransactionOut]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TollTransactionOut]') AND [name] = N'IX_TollTransactionOut_TransactionId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TollTransactionOut_TransactionId] ON [dbo].[TollTransactionOut] ([TransactionId]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TollTransactionOut_TransactionId';
END;

-- TmsTrafficData (gói 106: ON t.DetectTime = s.detectTime AND ISNULL(t.Lane, '') = ISNULL(s.lane, '') AND ISNULL(t.Location, '') = ISNULL(s.locationCode, ''))
IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsTrafficData]') AND [name] = N'IX_TmsTrafficData_DetectTime_Lane_Location')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsTrafficData_DetectTime_Lane_Location] ON [dbo].[TmsTrafficData] ([DetectTime], [Lane], [Location]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsTrafficData_DetectTime_Lane_Location';
END;

-- TmsIncident (gói 107: ON t.Code = s.incidentCode)
IF OBJECT_ID(N'[dbo].[TmsIncident]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TmsIncident]') AND [name] = N'IX_TmsIncident_Code')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TmsIncident_Code] ON [dbo].[TmsIncident] ([Code]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TmsIncident_Code';
END;

-- VmsCurrent (gói 108: ON t.EquipmentId = s.equipmentId)
IF OBJECT_ID(N'[dbo].[VmsCurrent]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[VmsCurrent]') AND [name] = N'IX_VmsCurrent_EquipmentId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_VmsCurrent_EquipmentId] ON [dbo].[VmsCurrent] ([EquipmentId]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_VmsCurrent_EquipmentId';
END;

-- TollStation (gói 109: ON t.StationId = s.stationId)
IF OBJECT_ID(N'[dbo].[TollStation]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TollStation]') AND [name] = N'IX_TollStation_StationId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TollStation_StationId] ON [dbo].[TollStation] ([StationId]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TollStation_StationId';
END;

-- TollLane (gói 109: ON t.LaneId = s.laneId)
IF OBJECT_ID(N'[dbo].[TollLane]', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE [object_id] = OBJECT_ID(N'[dbo].[TollLane]') AND [name] = N'IX_TollLane_LaneId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_TollLane_LaneId] ON [dbo].[TollLane] ([LaneId]);
    SET @IdxCreated = @IdxCreated + 1;
    PRINT N'   - Đã tạo chỉ mục IX_TollLane_LaneId';
END;

PRINT N'-> 2. Đã tạo ' + CAST(@IdxCreated AS NVARCHAR(10)) + N' chỉ mục chiều nhận.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-tao-bang-dich-chieu-nhan-InboundDB.sql';
PRINT N'=============================================================================';
GO
