-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 01_CREATE_TABLES.SQL
-- DESCRIPTION: Tạo các bảng CSDL cho Module Trao đổi & Chia sẻ Dữ liệu (ShareData).
--              Cấu trúc bám sát Entity gốc tại Module.ShareData.Core/Entities.
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-05
-- ============================================================================
-- QUY ƯỚC CỘT KẾ THỪA (Shared.Core.Domain)
--   EntityBaseId : ID
--   EntityBase   : Code, CreateTime, CreateUId, UpdateTime, UpdateUId, RowStatus, IsDelete
--   EntityTenant : TenantId
-- Mọi Entity đều kế thừa EntityTenant nên 9 cột trên có mặt ở TẤT CẢ các bảng.
-- IsDelete dùng kiểu DATETIME theo đúng quy ước nền tảng (102/109 bảng trong
-- dev_its10 đang dùng DATETIME), KHÔNG dùng BIT.
-- ============================================================================

USE [dev_its10];
GO

-- 1. BẢNG ĐỐI TÁC (ShareDataPartner)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataPartner')
BEGIN
    CREATE TABLE [dbo].[ShareDataPartner] (
        [ID]                 NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Name]               NVARCHAR(128) NULL,
        [Address]            NVARCHAR(128) NULL,
        [Port]               INT           NULL,
        [ProtocolProfile]    NVARCHAR(16)  NULL,   -- ASN | XML_A
        [Username]           NVARCHAR(64)  NULL,
        [PasswordHash]       NVARCHAR(256) NULL,
        [InitiatorMode]      NVARCHAR(32)  NULL,   -- clientInitiated | serverInitiated
        [HeartbeatMaxSec]    INT           NULL,
        [DatagramSize]       INT           NULL,
        [ResponseTimeoutSec] INT           NULL,
        [UseTls]             BIT           NULL DEFAULT 0,
        [Status]             NVARCHAR(16)  NULL DEFAULT 'ENABLED',   -- CONFIGURED | ENABLED | DISABLED
        [OrderNo]            INT           NULL DEFAULT 100,
        [Remark]             NVARCHAR(256) NULL,
        [Code]               NVARCHAR(256) NULL,
        [TenantId]           NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]         DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]          NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]         DATETIME      NULL,
        [UpdateUId]          NVARCHAR(64)  NULL,
        [RowStatus]          NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]           DATETIME      NULL
    );
END;
GO

-- 2. BẢNG NGUỒN DỮ LIỆU (ShareDataDataSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataDataSource')
BEGIN
    CREATE TABLE [dbo].[ShareDataDataSource] (
        [ID]          NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [Name]        NVARCHAR(128)  NULL,
        [Kind]        NVARCHAR(16)   NULL,   -- FIELD_PICKER | SAVED_QUERY
        [DbRef]       NVARCHAR(64)   NULL,
        [TableOrView] NVARCHAR(128)  NULL,
        [FieldsJson]  NVARCHAR(MAX)  NULL,
        [QueryText]   NVARCHAR(MAX)  NULL,
        [TopN]        INT            NULL,
        [Remark]      NVARCHAR(256)  NULL,
        [Code]        NVARCHAR(256)  NULL,
        [TenantId]    NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]  DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]   NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]  DATETIME       NULL,
        [UpdateUId]   NVARCHAR(64)   NULL,
        [RowStatus]   NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]    DATETIME       NULL
    );
END;
GO

-- 3. BẢNG HỒ SƠ ÁNH XẠ DỮ LIỆU (ShareDataMappingProfile)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataMappingProfile')
BEGIN
    CREATE TABLE [dbo].[ShareDataMappingProfile] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Name]         NVARCHAR(128) NULL,
        [VendorId]     NVARCHAR(64)  NULL,
        [DatatypeId]   INT           NULL,   -- 101..111
        [Direction]    NVARCHAR(8)   NULL,   -- OUT | IN
        [DataSourceId] NVARCHAR(64)  NULL,
        [MappingsJson] NVARCHAR(MAX) NULL,
        [IsActive]     BIT           NULL DEFAULT 1,
        [Remark]       NVARCHAR(256) NULL,
        [Code]         NVARCHAR(256) NULL,
        [TenantId]     NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]   DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]   DATETIME      NULL,
        [UpdateUId]    NVARCHAR(64)  NULL,
        [RowStatus]    NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]     DATETIME      NULL
    );
END;
GO

-- 4. BẢNG NGUỒN SỰ KIỆN (ShareDataEventSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataEventSource')
BEGIN
    CREATE TABLE [dbo].[ShareDataEventSource] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Name]         NVARCHAR(128) NULL,
        [Subject]      NVARCHAR(128) NULL,
        [DatatypeCode] NVARCHAR(16)  NULL,
        [Description]  NVARCHAR(256) NULL,
        [Remark]       NVARCHAR(256) NULL,
        [Code]         NVARCHAR(256) NULL,
        [TenantId]     NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]   DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]   DATETIME      NULL,
        [UpdateUId]    NVARCHAR(64)  NULL,
        [RowStatus]    NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]     DATETIME      NULL
    );
END;
GO

-- 5. BẢNG PHIÊN KẾT NỐI (ShareDataSession)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataSession')
BEGIN
    CREATE TABLE [dbo].[ShareDataSession] (
        [ID]                     NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [PartnerId]              NVARCHAR(64)  NULL,
        [Direction]              NVARCHAR(8)   NULL,   -- OUT | IN
        [State]                  NVARCHAR(16)  NULL,   -- IDLE | CONNECTING | LOGIN_WAIT | ACTIVE | CLOSING | CLOSED | RECONNECT_WAIT | REJECTED | TIMEOUT
        [StartedAt]              DATETIME      NULL,
        [AcceptedAt]             DATETIME      NULL,
        [EndedAt]                DATETIME      NULL,
        [EndReason]              NVARCHAR(64)  NULL,
        [PacketsSent]            BIGINT        NULL,
        [PacketsRecv]            BIGINT        NULL,
        [LastHeartbeatAt]        DATETIME      NULL,
        [HeartbeatRttMs]         INT           NULL,
        [NegotiatedDatagramSize] INT           NULL,
        [Remark]                 NVARCHAR(256) NULL,
        [Code]                   NVARCHAR(256) NULL,
        [TenantId]               NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]             DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]              NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]             DATETIME      NULL,
        [UpdateUId]              NVARCHAR(64)  NULL,
        [RowStatus]              NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]               DATETIME      NULL
    );
END;
GO

-- 6. BẢNG ĐĂNG KÝ CHIA SẺ (ShareDataSubscription)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataSubscription')
BEGIN
    CREATE TABLE [dbo].[ShareDataSubscription] (
        [ID]               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [PartnerId]        NVARCHAR(64)  NULL,
        [SessionId]        NVARCHAR(64)  NULL,
        [Direction]        NVARCHAR(16)  NULL DEFAULT 'OUTBOUND',   -- OUTBOUND | INBOUND
        [SerialNbr]        INT           NULL,
        [DatatypeId]       INT           NULL,   -- 101..111
        [Mode]             NVARCHAR(16)  NULL DEFAULT 'PERIODIC',   -- SINGLE | EVENT | PERIODIC
        [ScheduleJson]     NVARCHAR(MAX) NULL,
        [Format]           NVARCHAR(8)   NULL DEFAULT 'DATA',       -- DATA | FILE
        [Priority]         INT           NULL DEFAULT 5,
        [Guaranteed]       BIT           NULL DEFAULT 0,
        [Persistent]       BIT           NULL DEFAULT 0,
        [State]            NVARCHAR(16)  NULL DEFAULT 'ACTIVE',     -- PENDING | ACTIVE | PAUSED | REJECTED | CANCELLED | EXPIRED
        -- Mốc dữ liệu đã kết xuất tới, chỉ tiến khi xuất thành công có bản ghi
        [LastTimeRun]      DATETIME      NULL,
        -- Thời điểm chạy kế tiếp, kiêm lock lease giữa nhiều worker
        [NextTimeRun]      DATETIME      NULL DEFAULT GETDATE(),
        [RejectReason]     NVARCHAR(256) NULL,
        [CancelReason]     NVARCHAR(256) NULL,
        [RequestedAt]      DATETIME      NULL,
        [ResolvedAt]       DATETIME      NULL,
        [ResolvedBy]       NVARCHAR(64)  NULL,
        [DataSourceId]     NVARCHAR(64)  NULL,
        [MappingProfileId] NVARCHAR(64)  NULL,
        [EventSourceId]    NVARCHAR(64)  NULL,
        [DebounceSec]      INT           NULL DEFAULT 0,
        [Remark]           NVARCHAR(256) NULL,
        [Code]             NVARCHAR(256) NULL,
        [TenantId]         NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]       DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]        NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]       DATETIME      NULL,
        [UpdateUId]        NVARCHAR(64)  NULL,
        -- Worker mượn RowStatus làm trạng thái thực thi: IDLE | RUNNING | DISABLED
        [RowStatus]        NVARCHAR(32)  NULL DEFAULT 'IDLE',
        [IsDelete]         DATETIME      NULL
    );

    CREATE NONCLUSTERED INDEX [index_ShareDataSubscription_Scheduler]
        ON [dbo].[ShareDataSubscription] ([State] ASC, [Direction] ASC, [NextTimeRun] ASC);
END;
GO

-- 7. BẢNG NHẬT KÝ HOẠT ĐỘNG (ShareDataActivityLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataActivityLog')
BEGIN
    CREATE TABLE [dbo].[ShareDataActivityLog] (
        [ID]                NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [LogType]           NVARCHAR(16)   NULL,   -- CONFIG | TRANSFER
        [Action]            NVARCHAR(16)   NULL,
        [OccurredAt]        DATETIME       NULL DEFAULT GETDATE(),
        [Status]            NVARCHAR(16)   NULL,   -- SUCCESS | FAILED
        [Description]       NVARCHAR(512)  NULL,
        [ErrorMessage]      NVARCHAR(512)  NULL,
        [TargetType]        NVARCHAR(32)   NULL,
        [TargetId]          NVARCHAR(64)   NULL,
        [TargetName]        NVARCHAR(128)  NULL,
        [PartnerId]         NVARCHAR(64)   NULL,
        [PartnerName]       NVARCHAR(128)  NULL,
        [BeforeJson]        NVARCHAR(MAX)  NULL,
        [AfterJson]         NVARCHAR(MAX)  NULL,
        [ChangedFields]     NVARCHAR(512)  NULL,
        [SubscriptionId]    NVARCHAR(64)   NULL,
        [SessionId]         NVARCHAR(64)   NULL,
        [TransferDirection] NVARCHAR(8)    NULL,   -- SND | RCV
        [DatatypeId]        INT            NULL,
        [SerialNbr]         INT            NULL,
        [PacketNbr]         INT            NULL,
        [PduType]           NVARCHAR(32)   NULL,
        [Format]            NVARCHAR(8)    NULL,
        [ByteSize]          BIGINT         NULL,
        [RecordCount]       INT            NULL,
        [FilePath]          NVARCHAR(512)  NULL,
        [Hash]              NVARCHAR(128)  NULL,
        [OperatorName]      NVARCHAR(64)   NULL,
        [OperatorIp]        NVARCHAR(64)   NULL,
        [Remark]            NVARCHAR(256)  NULL,
        [Code]              NVARCHAR(256)  NULL,
        [TenantId]          NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]        DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]         NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]        DATETIME       NULL,
        [UpdateUId]         NVARCHAR(64)   NULL,
        [RowStatus]         NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]          DATETIME       NULL
    );

    CREATE NONCLUSTERED INDEX [index_ShareDataActivityLog_LT] ON [dbo].[ShareDataActivityLog] ([LogType] ASC);
    CREATE NONCLUSTERED INDEX [index_ShareDataActivityLog_OA] ON [dbo].[ShareDataActivityLog] ([OccurredAt] DESC);
    CREATE NONCLUSTERED INDEX [index_ShareDataActivityLog_PI] ON [dbo].[ShareDataActivityLog] ([PartnerId] ASC);
END;
GO

PRINT N'✅ Tạo bảng Module ShareData hoàn tất.';
GO
