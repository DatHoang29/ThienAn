-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 01_CREATE_TABLES.SQL
-- DESCRIPTION: Tạo các bảng CSDL cho Module Trao đổi & Chia sẻ Dữ liệu (ShareData)
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

-- 1. BẢNG ĐỐI TÁC (ShareDataPartner)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataPartner')
BEGIN
    CREATE TABLE [dbo].[ShareDataPartner] (
        [ID]                 NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Code]               NVARCHAR(64)  NULL,
        [Name]               NVARCHAR(256) NULL,
        [Address]            NVARCHAR(256) NULL,
        [Port]               INT           NULL,
        [ProtocolProfile]    NVARCHAR(32)  NULL,
        [Username]           NVARCHAR(64)  NULL,
        [PasswordHash]       NVARCHAR(512) NULL,
        [InitiatorMode]      NVARCHAR(64)  NULL,
        [HeartbeatMaxSec]    INT           NULL,
        [DatagramSize]       INT           NULL,
        [ResponseTimeoutSec] INT           NULL,
        [UseTls]             BIT           NULL DEFAULT 0,
        [Status]             NVARCHAR(32)  NULL DEFAULT 'ENABLED',
        [OrderNo]            INT           NULL DEFAULT 100,
        [Remark]             NVARCHAR(512) NULL,
        [TenantId]           NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]         DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]          NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]         DATETIME      NULL,
        [UpdateUId]          NVARCHAR(64)  NULL,
        [RowStatus]          NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]           BIT           NULL DEFAULT 0
    );
END;
GO

-- 2. BẢNG NGUỒN DỮ LIỆU (ShareDataDataSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataDataSource')
BEGIN
    CREATE TABLE [dbo].[ShareDataDataSource] (
        [ID]          NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [Code]        NVARCHAR(64)   NULL,
        [Name]        NVARCHAR(256)  NULL,
        [Kind]        NVARCHAR(32)   NULL, -- FIELD_PICKER | SAVED_QUERY
        [DbRef]       NVARCHAR(64)   NULL,
        [TableOrView] NVARCHAR(256)  NULL,
        [FieldsJson]  NVARCHAR(MAX)  NULL,
        [QueryText]   NVARCHAR(MAX)  NULL,
        [TopN]        INT            NULL DEFAULT 50,
        [Remark]      NVARCHAR(512)  NULL,
        [TenantId]    NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]  DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]   NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]  DATETIME       NULL,
        [UpdateUId]   NVARCHAR(64)   NULL,
        [RowStatus]   NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]    BIT            NULL DEFAULT 0
    );
END;
GO

-- 3. BẢNG HỒ SƠ ÁNH XẠ DỮ LIỆU (ShareDataMappingProfile)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataMappingProfile')
BEGIN
    CREATE TABLE [dbo].[ShareDataMappingProfile] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Code]         NVARCHAR(64)  NULL,
        [Name]         NVARCHAR(256) NULL,
        [VendorId]     NVARCHAR(64)  NULL,
        [DatatypeId]   NVARCHAR(64)  NULL,
        [Direction]    NVARCHAR(32)  NULL DEFAULT 'OUT', -- OUT | IN
        [DataSourceId] NVARCHAR(64)  NULL,
        [MappingsJson] NVARCHAR(MAX) NULL,
        [IsActive]     BIT           NULL DEFAULT 1,
        [Remark]       NVARCHAR(512) NULL,
        [TenantId]     NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]   DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]   DATETIME      NULL,
        [UpdateUId]    NVARCHAR(64)  NULL,
        [RowStatus]    NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]     BIT           NULL DEFAULT 0
    );
END;
GO

-- 4. BẢNG ĐĂNG KÝ TRUYỀN NHẬN (ShareDataSubscription)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataSubscription')
BEGIN
    CREATE TABLE [dbo].[ShareDataSubscription] (
        [ID]               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [SerialNbr]        NVARCHAR(64)  NULL,
        [PartnerId]        NVARCHAR(64)  NULL,
        [SessionId]        NVARCHAR(64)  NULL,
        [Direction]        NVARCHAR(32)  NULL DEFAULT 'OUTBOUND', -- OUTBOUND | INBOUND
        [DatatypeId]       NVARCHAR(64)  NULL,
        [Mode]             NVARCHAR(32)  NULL DEFAULT 'BATCH',    -- BATCH | PERIODIC | SINGLE | EVENT
        [IntervalSeconds]  INT           NULL DEFAULT 60,
        [ScheduleJson]     NVARCHAR(MAX) NULL,
        [Format]           NVARCHAR(32)  NULL DEFAULT 'DATA',
        [Priority]         INT           NULL DEFAULT 5,
        [Guaranteed]       BIT           NULL DEFAULT 0,
        [Persistent]       BIT           NULL DEFAULT 0,
        [State]            NVARCHAR(32)  NULL DEFAULT 'ACTIVE',   -- PENDING | ACTIVE | PAUSED
        [RunStatus]        NVARCHAR(32)  NULL DEFAULT 'IDLE',     -- IDLE | RUNNING | ERROR
        [LastTimeRun]      DATETIME      NULL,
        [NextTimeRun]      DATETIME      NULL DEFAULT GETDATE(),
        [RejectReason]     NVARCHAR(512) NULL,
        [CancelReason]     NVARCHAR(512) NULL,
        [RequestedAt]      DATETIME      NULL,
        [ResolvedAt]       DATETIME      NULL,
        [ResolvedBy]       NVARCHAR(64)  NULL,
        [DataSourceId]     NVARCHAR(64)  NULL,
        [MappingProfileId] NVARCHAR(64)  NULL,
        [EventSourceId]    NVARCHAR(64)  NULL,
        [DebounceSec]      INT           NULL DEFAULT 0,
        [Remark]           NVARCHAR(512) NULL,
        [TenantId]         NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]       DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]        NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]       DATETIME      NULL,
        [UpdateUId]        NVARCHAR(64)  NULL,
        [RowStatus]        NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]         BIT           NULL DEFAULT 0
    );
END;
GO

-- 5. BẢNG NHẬT KÝ HOẠT ĐỘNG (ShareDataActivityLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataActivityLog')
BEGIN
    CREATE TABLE [dbo].[ShareDataActivityLog] (
        [ID]                NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [LogType]           NVARCHAR(32)   NULL, -- CONFIG | TRANSFER
        [Action]            NVARCHAR(32)   NULL,
        [OccurredAt]        DATETIME       NULL DEFAULT GETDATE(),
        [Status]            NVARCHAR(32)   NULL DEFAULT 'SUCCESS', -- SUCCESS | FAILED
        [Description]       NVARCHAR(1024) NULL,
        [ErrorMessage]      NVARCHAR(MAX)  NULL,
        [TargetType]        NVARCHAR(64)   NULL,
        [TargetId]          NVARCHAR(64)   NULL,
        [TargetName]        NVARCHAR(256)  NULL,
        [PartnerId]         NVARCHAR(64)   NULL,
        [PartnerName]       NVARCHAR(256)  NULL,
        [BeforeJson]        NVARCHAR(MAX)  NULL,
        [AfterJson]         NVARCHAR(MAX)  NULL,
        [ChangedFields]     NVARCHAR(1024) NULL,
        [SubscriptionId]    NVARCHAR(64)   NULL,
        [SessionId]         NVARCHAR(64)   NULL,
        [TransferDirection] NVARCHAR(16)   NULL,
        [DatatypeId]        NVARCHAR(64)   NULL,
        [SerialNbr]         INT            NULL,
        [PacketNbr]         INT            NULL,
        [PduType]           NVARCHAR(64)   NULL,
        [Format]            NVARCHAR(32)   NULL,
        [ByteSize]          BIGINT         NULL DEFAULT 0,
        [RecordCount]       INT            NULL DEFAULT 0,
        [FilePath]          NVARCHAR(512)  NULL,
        [Hash]              NVARCHAR(128)  NULL,
        [OperatorName]      NVARCHAR(128)  NULL,
        [OperatorIp]        NVARCHAR(128)  NULL,
        [Remark]            NVARCHAR(512)  NULL,
        [TenantId]          NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]        DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]         NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]        DATETIME       NULL,
        [UpdateUId]         NVARCHAR(64)   NULL,
        [RowStatus]         NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]          BIT            NULL DEFAULT 0
    );
END;
GO

-- 6. BẢNG PHIÊN KẾT NỐI (ShareDataSession)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataSession')
BEGIN
    CREATE TABLE [dbo].[ShareDataSession] (
        [ID]             NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [PartnerId]      NVARCHAR(64)  NULL,
        [SessionToken]   NVARCHAR(128) NULL,
        [Status]         NVARCHAR(32)  NULL,
        [ConnectedAt]    DATETIME      NULL,
        [DisconnectedAt] DATETIME      NULL,
        [Remark]         NVARCHAR(512) NULL,
        [TenantId]       NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]     DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]      NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]     DATETIME      NULL,
        [UpdateUId]      NVARCHAR(64)  NULL,
        [RowStatus]      NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]       BIT           NULL DEFAULT 0
    );
END;
GO

-- 7. BẢNG SỰ KIỆN NỘI BỘ (ShareDataEventSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataEventSource')
BEGIN
    CREATE TABLE [dbo].[ShareDataEventSource] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Code]         NVARCHAR(64)  NULL,
        [Name]         NVARCHAR(256) NULL,
        [Subject]      NVARCHAR(256) NULL,
        [DatatypeCode] NVARCHAR(32)  NULL,
        [Description]  NVARCHAR(512) NULL,
        [Remark]       NVARCHAR(512) NULL,
        [TenantId]     NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]   DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]   DATETIME      NULL,
        [UpdateUId]    NVARCHAR(64)  NULL,
        [RowStatus]    NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]     BIT           NULL DEFAULT 0
    );
END;
GO
