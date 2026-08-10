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

-- ============================================================================
-- CHỐT CHẶN SERVER
--   File .sql KHÔNG chọn được server — server do kết nối của trình soạn thảo
--   quyết định. Nhưng script có thể kiểm tra rồi tự dừng nếu chạy nhầm máy.
--
--   Điền tên server bạn MUỐN chạy vào @ServerKyVong. Để trống thì bỏ qua kiểm.
--   Xem tên server hiện tại bằng: SELECT @@SERVERNAME
-- ============================================================================
SET NOEXEC OFF;

DECLARE @ServerKyVong SYSNAME = N'';   -- ví dụ: N'aa0914ef791d'

IF @ServerKyVong <> N'' AND @@SERVERNAME <> @ServerKyVong
BEGIN
    RAISERROR(N'DUNG LAI: dang o server [%s] nhung ky vong [%s]. Khong tao bang nao ca.',
              16, 1, @@SERVERNAME, @ServerKyVong);
    SET NOEXEC ON;   -- chặn toàn bộ batch phía sau trong cùng phiên
END;
GO

-- Ép về đúng database của môi trường local. Đổi tên ở đây nếu dùng database khác.
-- Đặt SAU chốt chặn để khi sai server thì NOEXEC đã bật, câu USE này cũng không chạy.
USE [dev_its10];
GO

PRINT N'▶ Server: ' + @@SERVERNAME
    + N' | Database: ' + DB_NAME()
    + N' | Login: ' + SUSER_SNAME()
    + N' | Phiên bản: ' + CONVERT(NVARCHAR(100), SERVERPROPERTY('Edition'));
GO

-- 1. BẢNG ĐỐI TÁC (ShareDataPartner)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataPartner')
BEGIN
    PRINT N'  → đang tạo ShareDataPartner...';
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
        [Status]             INT           NULL DEFAULT 1,     -- 1 = Enable, 0 = Disable
        [SessionState]       INT           NULL DEFAULT 0,     -- 1 = Connected, 0 = Disconnected
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
    PRINT N'  → đang tạo ShareDataDataSource...';
    CREATE TABLE [dbo].[ShareDataDataSource] (
        [ID]          NVARCHAR(64)   NOT NULL PRIMARY KEY,
        [Name]        NVARCHAR(128)  NULL,
        [Kind]        NVARCHAR(16)   NULL,   -- FIELD_PICKER | SAVED_QUERY
        [DbRef]       NVARCHAR(64)   NULL,
        [TableOrView] NVARCHAR(128)  NULL,
        [FieldsJson]  NVARCHAR(MAX)  NULL,
        -- SAVED_QUERY: viết @lastTime để worker lọc tăng dần, VD:
        --   SELECT ID, DetectTime, Speed FROM TmsTrafficData WHERE DetectTime > @lastTime
        -- Chu kỳ đầu (chưa có LastTimeRun) worker truyền mốc sàn 1900-01-01 và tự gắn
        -- OPTION (RECOMPILE) để kế hoạch quét toàn bảng không bị cache lại cho chu kỳ sau.
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
    PRINT N'  → đang tạo ShareDataMappingProfile...';
    CREATE TABLE [dbo].[ShareDataMappingProfile] (
        [ID]           NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [Name]         NVARCHAR(128) NULL,
        [VendorId]     NVARCHAR(64)  NULL,
        [DatatypeId]   NVARCHAR(32)  NULL,   -- mã nhóm shareData_type (101..111)
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
    PRINT N'  → đang tạo ShareDataEventSource...';
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
    PRINT N'  → đang tạo ShareDataSession...';
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
    PRINT N'  → đang tạo ShareDataSubscription...';
    CREATE TABLE [dbo].[ShareDataSubscription] (
        [ID]               NVARCHAR(64)  NOT NULL PRIMARY KEY,
        [PartnerId]        NVARCHAR(64)  NULL,
        [SessionId]        NVARCHAR(64)  NULL,
        [Direction]        NVARCHAR(16)  NULL DEFAULT 'OUTBOUND',   -- OUTBOUND | INBOUND
        [SerialNbr]        INT           NULL,
        [DatatypeId]       NVARCHAR(32)  NULL,   -- mã nhóm shareData_type (101..111)
        [Mode]             NVARCHAR(16)  NULL DEFAULT 'PERIODIC',   -- SINGLE | EVENT | PERIODIC
        [ScheduleJson]     NVARCHAR(MAX) NULL,
        [IntervalSeconds]  INT           NULL,   -- chu kỳ riêng (giây); NULL -> worker dùng mặc định 30s
        [Format]           NVARCHAR(8)   NULL DEFAULT 'DATA',       -- DATA | FILE
        [Priority]         INT           NULL DEFAULT 5,
        [Guaranteed]       BIT           NULL DEFAULT 0,
        [Persistent]       BIT           NULL DEFAULT 0,
        [State]            INT           NULL DEFAULT 1,     -- 0=Pending, 1=Active, 2=Paused, 3=Rejected, 4=Cancelled, 5=Expired
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
        [RowStatus]        NVARCHAR(32)  NULL DEFAULT '1',
        [IsDelete]         DATETIME      NULL
    );

    CREATE NONCLUSTERED INDEX [index_ShareDataSubscription_Scheduler]
        ON [dbo].[ShareDataSubscription] ([State] ASC, [Direction] ASC, [NextTimeRun] ASC);
END;
GO

-- 7. BẢNG NHẬT KÝ HOẠT ĐỘNG (ShareDataActivityLog)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShareDataActivityLog')
BEGIN
    PRINT N'  → đang tạo ShareDataActivityLog...';
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
        [DatatypeId]        NVARCHAR(32)   NULL,
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

PRINT N'✅ Chạy xong 01_CREATE_TABLES. Xem 2 bảng kết quả ở trên để kiểm chứng.';
GO