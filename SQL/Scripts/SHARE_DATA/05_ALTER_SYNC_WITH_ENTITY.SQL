-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V1)
-- FILE: 05_ALTER_SYNC_WITH_ENTITY.SQL
-- DESCRIPTION: Đồng bộ schema CSDL đang chạy về đúng Entity gốc
--              (Module.ShareData.Core/Entities). Dùng cho DB đã tạo bằng bản
--              01_CREATE_TABLES.SQL cũ — bản mới chỉ tạo khi bảng chưa tồn tại.
-- AUTHOR: Đạt
-- CREATED DATE: 2026-08-06
-- ============================================================================
-- ⚠️ ĐỌC KỸ TRƯỚC KHI CHẠY
--   Script có thao tác XOÁ CỘT và ĐỔI KIỂU DỮ LIỆU. Hãy sao lưu CSDL trước.
--   Bước 3 chuyển SerialNbr từ NVARCHAR sang INT: giá trị chữ (VD 'SUB-DEMO-001')
--   được chuyển sang cột Code trước, phần không phải số sẽ thành NULL.
--
--   Mọi câu lệnh tham chiếu cột có thể đã bị xoá đều đặt trong sp_executesql.
--   Lý do: SQL Server phân giải tên cột lúc BIÊN DỊCH cả batch, nên câu lệnh
--   tĩnh nằm trong IF vẫn gây lỗi "Invalid column name" dù nhánh IF không chạy.
--   Nhờ vậy script CHẠY LẠI ĐƯỢC nhiều lần mà không lỗi.
-- ============================================================================

-- ============================================================================
-- CHỐT CHẶN SERVER
--   File .sql KHÔNG chọn được server — server do kết nối của trình soạn thảo
--   quyết định. Điền tên server bạn MUỐN chạy vào @ServerKyVong để script tự
--   dừng nếu chạy nhầm máy. Để trống thì bỏ qua kiểm.
--   Xem tên server hiện tại: SELECT @@SERVERNAME
-- ============================================================================
SET NOEXEC OFF;

DECLARE @ServerKyVong SYSNAME = N'';   -- ví dụ: N'aa0914ef791d'

IF @ServerKyVong <> N'' AND @@SERVERNAME <> @ServerKyVong
BEGIN
    RAISERROR(N'DUNG LAI: dang o server [%s] nhung ky vong [%s]. Khong thuc thi gi ca.',
              16, 1, @@SERVERNAME, @ServerKyVong);
    SET NOEXEC ON;
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

SET XACT_ABORT ON;
BEGIN TRANSACTION;
GO

-- ── Helper: xoá ràng buộc DEFAULT của một cột trước khi ALTER/DROP ──────────
-- Dùng CREATE thường thay CREATE OR ALTER vì cú pháp OR ALTER không áp dụng
-- cho thủ tục tạm (#).
IF OBJECT_ID('tempdb..#DropDefaultConstraint') IS NOT NULL
    DROP PROCEDURE #DropDefaultConstraint;
GO

CREATE PROCEDURE #DropDefaultConstraint
    @TableName SYSNAME,
    @ColumnName SYSNAME
AS
BEGIN
    DECLARE @ConstraintName SYSNAME, @Sql NVARCHAR(MAX);

    SELECT @ConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(@TableName) AND c.name = @ColumnName;

    IF @ConstraintName IS NOT NULL
    BEGIN
        SET @Sql = N'ALTER TABLE ' + QUOTENAME(@TableName) + N' DROP CONSTRAINT ' + QUOTENAME(@ConstraintName);
        EXEC sp_executesql @Sql;
    END;
END;
GO

-- ============================================================================
-- 1. BỔ SUNG CỘT Code (kế thừa EntityBase) CHO 3 BẢNG CÒN THIẾU
-- ============================================================================
IF COL_LENGTH('dbo.ShareDataSubscription', 'Code') IS NULL
    ALTER TABLE [dbo].[ShareDataSubscription] ADD [Code] NVARCHAR(256) NULL;
GO
IF COL_LENGTH('dbo.ShareDataSession', 'Code') IS NULL
    ALTER TABLE [dbo].[ShareDataSession] ADD [Code] NVARCHAR(256) NULL;
GO
IF COL_LENGTH('dbo.ShareDataActivityLog', 'Code') IS NULL
    ALTER TABLE [dbo].[ShareDataActivityLog] ADD [Code] NVARCHAR(256) NULL;
GO

-- ============================================================================
-- 2. GIỮ LẠI DỮ LIỆU SerialNbr DẠNG CHỮ TRƯỚC KHI ĐỔI KIỂU
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.ShareDataSubscription')
             AND name = 'SerialNbr' AND system_type_id = TYPE_ID('nvarchar'))
BEGIN
    EXEC sp_executesql N'
        UPDATE [dbo].[ShareDataSubscription]
           SET [Code] = [SerialNbr]
         WHERE [Code] IS NULL
           AND [SerialNbr] IS NOT NULL
           AND TRY_CONVERT(INT, [SerialNbr]) IS NULL;

        -- Giá trị không phải số không đưa được vào cột INT -> đặt NULL
        UPDATE [dbo].[ShareDataSubscription]
           SET [SerialNbr] = NULL
         WHERE [SerialNbr] IS NOT NULL
           AND TRY_CONVERT(INT, [SerialNbr]) IS NULL;';
END;
GO

-- ============================================================================
-- 3. ĐỔI KIỂU SerialNbr SANG INT, DatatypeId SANG NVARCHAR(32)
--    DatatypeId là MÃ trong bảng cấu hình nhóm shareData_type, không phải số học.
-- ============================================================================
EXEC #DropDefaultConstraint 'dbo.ShareDataSubscription', 'SerialNbr';
EXEC #DropDefaultConstraint 'dbo.ShareDataSubscription', 'DatatypeId';
EXEC #DropDefaultConstraint 'dbo.ShareDataActivityLog', 'DatatypeId';
GO

ALTER TABLE [dbo].[ShareDataSubscription]  ALTER COLUMN [SerialNbr]  INT NULL;
ALTER TABLE [dbo].[ShareDataSubscription]  ALTER COLUMN [DatatypeId] NVARCHAR(32) NULL;
ALTER TABLE [dbo].[ShareDataActivityLog]   ALTER COLUMN [DatatypeId] NVARCHAR(32) NULL;
GO

-- ============================================================================
-- 4. XOÁ CỘT KHÔNG CÓ TRONG ENTITY
--    GIỮ LẠI NextTimeRun và LastTimeRun — worker sở hữu hai cột này:
--      NextTimeRun = thời điểm chạy kế tiếp, kiêm lock lease (luôn ghi mỗi chu kỳ)
--      LastTimeRun = mốc dữ liệu đã kết xuất tới (chỉ tiến khi xuất thành công)
--    KHÔNG dùng UpdateTime cho hai việc trên: đó là cột kiểm toán dùng chung,
--    bất kỳ thao tác cập nhật nào của API cũng ghi đè.
--    XOÁ RunStatus và IntervalSeconds: cơ chế chống chạy trùng chỉ dựa vào lease
--    NextTimeRun, không cần cột trạng thái riêng. Chu kỳ chạy dùng hằng
--    DefaultIntervalSeconds (30 giây) trong mã nguồn.
-- ============================================================================
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'index_ShareDataSubscription_Scheduler'
                                       AND object_id = OBJECT_ID('dbo.ShareDataSubscription'))
    DROP INDEX [index_ShareDataSubscription_Scheduler] ON [dbo].[ShareDataSubscription];
GO

EXEC #DropDefaultConstraint 'dbo.ShareDataSubscription', 'IntervalSeconds';
EXEC #DropDefaultConstraint 'dbo.ShareDataSubscription', 'RunStatus';
GO

IF COL_LENGTH('dbo.ShareDataSubscription', 'IntervalSeconds') IS NOT NULL ALTER TABLE [dbo].[ShareDataSubscription] DROP COLUMN [IntervalSeconds];
IF COL_LENGTH('dbo.ShareDataSubscription', 'RunStatus')       IS NOT NULL ALTER TABLE [dbo].[ShareDataSubscription] DROP COLUMN [RunStatus];
GO

CREATE NONCLUSTERED INDEX [index_ShareDataSubscription_Scheduler]
    ON [dbo].[ShareDataSubscription] ([State] ASC, [Direction] ASC, [NextTimeRun] ASC);
GO

-- ============================================================================
-- 5. ĐỒNG BỘ BẢNG ShareDataSession THEO ENTITY GỐC
-- ============================================================================
IF COL_LENGTH('dbo.ShareDataSession', 'Direction')              IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [Direction] NVARCHAR(8) NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'State')                  IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [State] NVARCHAR(16) NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'StartedAt')              IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [StartedAt] DATETIME NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'AcceptedAt')             IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [AcceptedAt] DATETIME NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'EndedAt')                IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [EndedAt] DATETIME NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'EndReason')              IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [EndReason] NVARCHAR(64) NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'PacketsSent')            IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [PacketsSent] BIGINT NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'PacketsRecv')            IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [PacketsRecv] BIGINT NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'LastHeartbeatAt')        IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [LastHeartbeatAt] DATETIME NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'HeartbeatRttMs')         IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [HeartbeatRttMs] INT NULL;
IF COL_LENGTH('dbo.ShareDataSession', 'NegotiatedDatagramSize') IS NULL ALTER TABLE [dbo].[ShareDataSession] ADD [NegotiatedDatagramSize] INT NULL;
GO

-- Chuyển dữ liệu cũ sang cột mới tương ứng rồi bỏ cột không có trong Entity gốc
IF COL_LENGTH('dbo.ShareDataSession', 'Status') IS NOT NULL
    EXEC sp_executesql N'UPDATE [dbo].[ShareDataSession] SET [State] = ISNULL([State], [Status]);';
IF COL_LENGTH('dbo.ShareDataSession', 'ConnectedAt') IS NOT NULL
    EXEC sp_executesql N'UPDATE [dbo].[ShareDataSession] SET [StartedAt] = ISNULL([StartedAt], [ConnectedAt]);';
IF COL_LENGTH('dbo.ShareDataSession', 'DisconnectedAt') IS NOT NULL
    EXEC sp_executesql N'UPDATE [dbo].[ShareDataSession] SET [EndedAt] = ISNULL([EndedAt], [DisconnectedAt]);';
IF COL_LENGTH('dbo.ShareDataSession', 'SessionToken') IS NOT NULL
    EXEC sp_executesql N'UPDATE [dbo].[ShareDataSession] SET [Code] = ISNULL([Code], [SessionToken]);';
GO

EXEC #DropDefaultConstraint 'dbo.ShareDataSession', 'SessionToken';
EXEC #DropDefaultConstraint 'dbo.ShareDataSession', 'Status';
EXEC #DropDefaultConstraint 'dbo.ShareDataSession', 'ConnectedAt';
EXEC #DropDefaultConstraint 'dbo.ShareDataSession', 'DisconnectedAt';
GO

IF COL_LENGTH('dbo.ShareDataSession', 'SessionToken')   IS NOT NULL ALTER TABLE [dbo].[ShareDataSession] DROP COLUMN [SessionToken];
IF COL_LENGTH('dbo.ShareDataSession', 'Status')         IS NOT NULL ALTER TABLE [dbo].[ShareDataSession] DROP COLUMN [Status];
IF COL_LENGTH('dbo.ShareDataSession', 'ConnectedAt')    IS NOT NULL ALTER TABLE [dbo].[ShareDataSession] DROP COLUMN [ConnectedAt];
IF COL_LENGTH('dbo.ShareDataSession', 'DisconnectedAt') IS NOT NULL ALTER TABLE [dbo].[ShareDataSession] DROP COLUMN [DisconnectedAt];
GO

-- ============================================================================
-- 6. ĐƯA IsDelete VỀ KIỂU DATETIME THEO QUY ƯỚC NỀN TẢNG
--    (102/109 bảng trong dev_its10 đang dùng DATETIME, riêng 7 bảng ShareData
--     đang là BIT do bản script cũ)
-- ============================================================================
DECLARE @Tbl SYSNAME, @Sql NVARCHAR(MAX);
DECLARE TableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT t.name
    FROM sys.tables t
    JOIN sys.columns c ON c.object_id = t.object_id
    WHERE t.name LIKE 'ShareData%' AND c.name = 'IsDelete' AND c.system_type_id = TYPE_ID('bit');

OPEN TableCursor;
FETCH NEXT FROM TableCursor INTO @Tbl;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC #DropDefaultConstraint @Tbl, 'IsDelete';

    -- BIT 1 (đã xoá) quy đổi thành mốc thời gian, BIT 0 thành NULL
    SET @Sql = N'ALTER TABLE ' + QUOTENAME(@Tbl) + N' ADD [IsDeleteTmp] DATETIME NULL;';
    EXEC sp_executesql @Sql;

    SET @Sql = N'UPDATE ' + QUOTENAME(@Tbl) + N' SET [IsDeleteTmp] = CASE WHEN [IsDelete] = 1 THEN GETDATE() ELSE NULL END;';
    EXEC sp_executesql @Sql;

    SET @Sql = N'ALTER TABLE ' + QUOTENAME(@Tbl) + N' DROP COLUMN [IsDelete];';
    EXEC sp_executesql @Sql;

    SET @Sql = N'EXEC sp_rename ''' + @Tbl + N'.IsDeleteTmp'', ''IsDelete'', ''COLUMN'';';
    EXEC sp_executesql @Sql;

    FETCH NEXT FROM TableCursor INTO @Tbl;
END;
CLOSE TableCursor;
DEALLOCATE TableCursor;
GO

IF OBJECT_ID('tempdb..#DropDefaultConstraint') IS NOT NULL
    DROP PROCEDURE #DropDefaultConstraint;
GO

COMMIT TRANSACTION;
GO

PRINT N'✅ Đồng bộ schema ShareData về đúng Entity gốc hoàn tất.';
GO

SET NOEXEC OFF;
GO
