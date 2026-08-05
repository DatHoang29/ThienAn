-- ============================================================================
-- MODULE: SHARE DATA (ESHARE V1) - TEST DATABASE
-- FILE: 01_CREATE_TABLES.SQL
-- DESCRIPTION: Tạo các bảng CSDL cho Module ShareData trong CSDL [test]
-- AUTHOR: Antigravity AI
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [test];
GO

-- 1. BẢNG ĐỐI TÁC (EshPartner)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshPartner')
BEGIN
    CREATE TABLE [dbo].[EshPartner] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Code]         NVARCHAR(64)  NOT NULL,
        [Name]         NVARCHAR(256) NOT NULL,
        [Address]      NVARCHAR(512) NULL,
        [Port]         INT           NULL,
        [Username]     NVARCHAR(64)  NULL,
        [PasswordHash] NVARCHAR(512) NULL,
        [Status]       NVARCHAR(32)  NULL DEFAULT 'ENABLED',
        [TenantId]     NVARCHAR(64)  NULL DEFAULT '1',
        [CreateTime]   DATETIME      NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)  NULL DEFAULT '1',
        [UpdateTime]   DATETIME      NULL,
        [UpdateUId]    NVARCHAR(64)  NULL,
        [RowStatus]    NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]     BIT           NULL DEFAULT 0
    );
    CREATE UNIQUE INDEX [UX_EshPartner_Code] ON [dbo].[EshPartner]([Code]) WHERE [IsDelete] = 0;
END;
GO

-- 2. BẢNG NGUỒN DỮ LIỆU (EshDataSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshDataSource')
BEGIN
    CREATE TABLE [dbo].[EshDataSource] (
        [ID]          NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [Code]        NVARCHAR(64)   NULL,
        [Name]        NVARCHAR(256)  NOT NULL,
        [Kind]        NVARCHAR(32)   NOT NULL, -- FIELD_PICKER | SAVED_QUERY
        [Table]       NVARCHAR(128)  NULL,
        [QueryText]   NVARCHAR(MAX)  NULL,
        [TopN]        INT            NULL DEFAULT 1000,
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

-- 3. BẢNG HỒ SƠ ÁNH XẠ DỮ LIỆU (EshMappingProfile)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshMappingProfile')
BEGIN
    CREATE TABLE [dbo].[EshMappingProfile] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Code]         NVARCHAR(64)  NOT NULL,
        [Name]         NVARCHAR(256) NOT NULL,
        [PartnerId]    NVARCHAR(64)  NOT NULL,
        [DatatypeId]   NVARCHAR(64)  NOT NULL,
        [Direction]    NVARCHAR(32)  NOT NULL DEFAULT 'OUTBOUND', -- OUTBOUND | INBOUND
        [DataSourceId] NVARCHAR(64)  NOT NULL,
        [IsActive]     BIT           NOT NULL DEFAULT 1,
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

-- 4. BẢNG CHI TIẾT ÁNH XẠ TRƯỜNG (EshFieldMapping)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshFieldMapping')
BEGIN
    CREATE TABLE [dbo].[EshFieldMapping] (
        [ID]               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [MappingProfileId] NVARCHAR(64)  NOT NULL,
        [SourceKey]        NVARCHAR(128) NOT NULL,
        [TargetKey]        NVARCHAR(128) NOT NULL,
        [DefaultValue]     NVARCHAR(256) NULL,
        [IsRequired]       BIT           NOT NULL DEFAULT 0,
        [OrderNo]          INT           NULL DEFAULT 0,
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

-- 5. BẢNG ĐĂNG KÝ TRUYỀN NHẬN (EshSubscription)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshSubscription')
BEGIN
    CREATE TABLE [dbo].[EshSubscription] (
        [ID]               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [SerialNbr]        NVARCHAR(64)  NOT NULL,
        [PartnerId]        NVARCHAR(64)  NOT NULL,
        [DatatypeId]       NVARCHAR(64)  NOT NULL,
        [MappingProfileId] NVARCHAR(64)  NOT NULL,
        [DataSourceId]     NVARCHAR(64)  NOT NULL,
        [Direction]        NVARCHAR(32)  NOT NULL DEFAULT 'OUTBOUND', -- OUTBOUND | INBOUND
        [Mode]             NVARCHAR(32)  NOT NULL DEFAULT 'BATCH',    -- BATCH | PERIODIC | REALTIME
        [State]            NVARCHAR(32)  NOT NULL DEFAULT 'ACTIVE',   -- ACTIVE | DISABLED
        [RunStatus]        NVARCHAR(32)  NULL DEFAULT 'IDLE',         -- IDLE | RUNNING | FAILED
        [ProcessLockId]    NVARCHAR(64)  NULL,
        [IntervalSeconds]  INT           NULL DEFAULT 300,
        [LastTimeRun]      DATETIME      NULL,
        [NextTimeRun]      DATETIME      NULL DEFAULT GETDATE(),
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

-- 6. BẢNG LỊCH SỬ KẾT XUẤT & TRUYỀN NHẬN (EshExportLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshExportLog')
BEGIN
    CREATE TABLE [dbo].[EshExportLog] (
        [ID]             NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [SubscriptionId] NVARCHAR(64)   NULL,
        [MappingId]      NVARCHAR(64)   NULL,
        [PartnerId]      NVARCHAR(64)   NULL,
        [DatatypeId]     NVARCHAR(64)   NULL,
        [ExportedAt]     DATETIME       NOT NULL DEFAULT GETDATE(),
        [RecordCount]    BIGINT         NULL DEFAULT 0,
        [ByteSize]       BIGINT         NULL DEFAULT 0,
        [FilePath]       NVARCHAR(512)  NULL,
        [Hash]           NVARCHAR(128)  NULL,
        [Status]         NVARCHAR(32)   NOT NULL DEFAULT 'SUCCESS', -- SUCCESS | FAILED
        [ErrorMessage]   NVARCHAR(MAX)  NULL,
        [TenantId]       NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]     DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]      NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]     DATETIME       NULL,
        [UpdateUId]      NVARCHAR(64)   NULL,
        [RowStatus]      NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]       BIT            NULL DEFAULT 0
    );
END;
GO

-- 7. BẢNG LOG HỆ THỐNG (EshSystemLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshSystemLog')
BEGIN
    CREATE TABLE [dbo].[EshSystemLog] (
        [ID]           NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [LogLevel]     NVARCHAR(32)   NOT NULL,
        [Source]       NVARCHAR(128)  NULL,
        [Message]      NVARCHAR(MAX)  NOT NULL,
        [Exception]    NVARCHAR(MAX)  NULL,
        [LogTime]      DATETIME       NOT NULL DEFAULT GETDATE(),
        [TenantId]     NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]   DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]   DATETIME       NULL,
        [UpdateUId]    NVARCHAR(64)   NULL,
        [RowStatus]    NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]     BIT            NULL DEFAULT 0
    );
END;
GO

-- 8. BẢNG SỰ KIỆN CHIỀU VÀO (EshEventSource)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EshEventSource')
BEGIN
    CREATE TABLE [dbo].[EshEventSource] (
        [ID]           NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [EventCode]    NVARCHAR(64)   NOT NULL,
        [PartnerCode]  NVARCHAR(64)   NOT NULL,
        [Payload]      NVARCHAR(MAX)  NOT NULL,
        [Status]       NVARCHAR(32)   NOT NULL DEFAULT 'PENDING', -- PENDING | PROCESSED | FAILED
        [RetryCount]   INT            NULL DEFAULT 0,
        [ReceivedAt]   DATETIME       NOT NULL DEFAULT GETDATE(),
        [ProcessedAt]  DATETIME       NULL,
        [ErrorMessage] NVARCHAR(MAX)  NULL,
        [TenantId]     NVARCHAR(64)   NULL DEFAULT '1',
        [CreateTime]   DATETIME       NULL DEFAULT GETDATE(),
        [CreateUId]    NVARCHAR(64)   NULL DEFAULT '1',
        [UpdateTime]   DATETIME       NULL,
        [UpdateUId]    NVARCHAR(64)   NULL,
        [RowStatus]    NVARCHAR(32)   NULL DEFAULT '1',
        [IsDelete]     BIT            NULL DEFAULT 0
    );
END;
GO

-- ============================================================================
-- PHẦN THÔNG TIN: CHỈ KHỞI TẠO CÁC BẢNG NẰM TRONG SCOPE CỦA SHARE MODULE (ESH*)
-- KHÔNG TỰ TẠO HOẶC SỬA BẢNG CỦA CÁC MODULE KHÁC NHƯ TmsTrafficData, TmsWeather...
-- ============================================================================
GO
