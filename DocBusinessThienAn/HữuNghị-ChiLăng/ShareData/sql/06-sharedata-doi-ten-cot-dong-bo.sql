-- =========================================================================================
-- Script: 06-sharedata-doi-ten-cot-dong-bo.sql
-- Mục đích: Chuẩn hóa và đồng bộ 3 trường cấu hình đồng bộ ở phân hệ ShareData:
--           1. SyncKind     -> SyncMode        (1 = Snapshot, 2 = Incremental)
--           2. TimeColumn / CursorColumn -> CursorTimeColumn (tên cột mốc thời gian con trỏ)
--           3. LookbackDays -> MaxLookBackDays (số ngày quét lùi tối đa)
--           Khi CodeFirst đã chạy trước (Rule 19.85), các cột đích đã được tạo sẵn từ Entity.
--           Script sao chép dữ liệu từ cột cũ sang cột mới (nếu chưa có dữ liệu) và DROP cột cũ.
-- Áp dụng cho: Bảng [ShareDataPacket] và dọn cột di sản trên [ShareDataMapping]
-- Môi trường:  DEV_ITS10 (SQL Server)
-- Nguyên tắc:  Idempotent — chạy lại nhiều lần an toàn, không làm mất dữ liệu hiện có
-- CẬP NHẬT LẦN CUỐI: 18/10/2026
-- =========================================================================================

USE [DEV_ITS10];
GO

SET NOCOUNT ON;

PRINT N'=========================================================================================';
PRINT N'Bắt đầu thực thi: 06-sharedata-doi-ten-cot-dong-bo.sql';
PRINT N'=========================================================================================';

-- ─────────────────────────────────────────────────────────────────────────────────────────
-- 1. BẢNG [dbo].[ShareDataPacket]
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 1. Kiểm tra và đồng bộ cột trên bảng [ShareDataPacket]...';

IF OBJECT_ID('dbo.ShareDataPacket', 'U') IS NOT NULL
BEGIN
    -- 1.1 SyncKind -> SyncMode
    IF COL_LENGTH('dbo.ShareDataPacket', 'SyncKind') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NOT NULL
        BEGIN
            PRINT N'   - Cả [SyncKind] và [SyncMode] cùng tồn tại, đang đồng bộ dữ liệu sang [SyncMode]...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataPacket]
                SET [SyncMode] = [SyncKind]
                WHERE [SyncMode] IS NULL AND [SyncKind] IS NOT NULL;
            ';
            PRINT N'   -> Đã sao chép dữ liệu từ [SyncKind] sang [SyncMode].';

            DECLARE @defSyncKind NVARCHAR(256);
            SELECT @defSyncKind = d.name
            FROM sys.default_constraints d
            JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
            WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'SyncKind';
            IF @defSyncKind IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defSyncKind + N'];');

            ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [SyncKind];
            PRINT N'   -> Đã DROP cột cũ [SyncKind] trên [ShareDataPacket].';
        END
        ELSE
        BEGIN
            PRINT N'   - Cột [SyncMode] chưa có, tiến hành đổi tên cột [SyncKind] -> [SyncMode]...';
            EXEC sp_rename 'dbo.ShareDataPacket.SyncKind', 'SyncMode', 'COLUMN';
            PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[SyncMode].';
        END
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [SyncKind] không tồn tại trên [ShareDataPacket], bỏ qua.';
    END;

    -- 1.2 TimeColumn / CursorColumn -> CursorTimeColumn
    IF COL_LENGTH('dbo.ShareDataPacket', 'TimeColumn') IS NOT NULL OR COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
        BEGIN
            IF COL_LENGTH('dbo.ShareDataPacket', 'TimeColumn') IS NOT NULL
            BEGIN
                PRINT N'   - Đang chuyển dữ liệu từ [TimeColumn] sang [CursorTimeColumn]...';
                EXEC sp_executesql N'
                    UPDATE [dbo].[ShareDataPacket]
                    SET [CursorTimeColumn] = [TimeColumn]
                    WHERE [CursorTimeColumn] IS NULL AND [TimeColumn] IS NOT NULL;
                ';
                DECLARE @defTimeCol NVARCHAR(256);
                SELECT @defTimeCol = d.name
                FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'TimeColumn';
                IF @defTimeCol IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defTimeCol + N'];');

                ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [TimeColumn];
                PRINT N'   -> Đã DROP cột cũ [TimeColumn] trên [ShareDataPacket].';
            END;

            IF COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NOT NULL
            BEGIN
                PRINT N'   - Đang chuyển dữ liệu từ [CursorColumn] sang [CursorTimeColumn]...';
                EXEC sp_executesql N'
                    UPDATE [dbo].[ShareDataPacket]
                    SET [CursorTimeColumn] = [CursorColumn]
                    WHERE [CursorTimeColumn] IS NULL AND [CursorColumn] IS NOT NULL;
                ';
                DECLARE @defCurCol NVARCHAR(256);
                SELECT @defCurCol = d.name
                FROM sys.default_constraints d
                JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
                WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'CursorColumn';
                IF @defCurCol IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defCurCol + N'];');

                ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [CursorColumn];
                PRINT N'   -> Đã DROP cột cũ [CursorColumn] trên [ShareDataPacket].';
            END;
        END
        ELSE
        BEGIN
            IF COL_LENGTH('dbo.ShareDataPacket', 'TimeColumn') IS NOT NULL
            BEGIN
                PRINT N'   - Cột [CursorTimeColumn] chưa có, đang đổi tên [TimeColumn] -> [CursorTimeColumn]...';
                EXEC sp_rename 'dbo.ShareDataPacket.TimeColumn', 'CursorTimeColumn', 'COLUMN';
                PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[CursorTimeColumn].';
            END
            ELSE IF COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NOT NULL
            BEGIN
                PRINT N'   - Cột [CursorTimeColumn] chưa có, đang đổi tên [CursorColumn] -> [CursorTimeColumn]...';
                EXEC sp_rename 'dbo.ShareDataPacket.CursorColumn', 'CursorTimeColumn', 'COLUMN';
                PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[CursorTimeColumn].';
            END;
        END
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [TimeColumn] / [CursorColumn] không tồn tại trên [ShareDataPacket], bỏ qua.';
    END;

    -- 1.3 LookbackDays -> MaxLookBackDays
    IF COL_LENGTH('dbo.ShareDataPacket', 'LookbackDays') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NOT NULL
        BEGIN
            PRINT N'   - Đang chuyển dữ liệu từ [LookbackDays] sang [MaxLookBackDays]...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataPacket]
                SET [MaxLookBackDays] = [LookbackDays]
                WHERE [MaxLookBackDays] IS NULL AND [LookbackDays] IS NOT NULL;
            ';
            DECLARE @defLookback NVARCHAR(256);
            SELECT @defLookback = d.name
            FROM sys.default_constraints d
            JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
            WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'LookbackDays';
            IF @defLookback IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defLookback + N'];');

            ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [LookbackDays];
            PRINT N'   -> Đã DROP cột cũ [LookbackDays] trên [ShareDataPacket].';
        END
        ELSE
        BEGIN
            PRINT N'   - Cột [MaxLookBackDays] chưa có, đang đổi tên [LookbackDays] -> [MaxLookBackDays]...';
            EXEC sp_rename 'dbo.ShareDataPacket.LookbackDays', 'MaxLookBackDays', 'COLUMN';
            PRINT N'   -> Đổi tên thành công: [ShareDataPacket].[MaxLookBackDays].';
        END
    END
    ELSE IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataPacket') AND name COLLATE Latin1_General_CS_AS = 'MaxLookbackDays')
         AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataPacket') AND name COLLATE Latin1_General_CS_AS = 'MaxLookBackDays')
    BEGIN
        PRINT N'   - Đang chuẩn hóa chữ hoa cột [MaxLookbackDays] -> [MaxLookBackDays] trên [ShareDataPacket]...';
        EXEC sp_rename 'dbo.ShareDataPacket.MaxLookbackDays', 'MaxLookBackDays', 'COLUMN';
        PRINT N'   -> Chuẩn hóa thành công: [ShareDataPacket].[MaxLookBackDays].';
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [LookbackDays] không tồn tại trên [ShareDataPacket], bỏ qua.';
    END;
END
ELSE
BEGIN
    PRINT N'   - Bảng [ShareDataPacket] không tồn tại trên DEV_ITS10, bỏ qua.';
END;
GO

-- ─────────────────────────────────────────────────────────────────────────────────────────
-- 2. BẢNG [dbo].[ShareDataMapping] (Dọn dẹp cột di sản nếu từng phát sinh trong CSDL cũ)
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 2. Kiểm tra cột di sản trên bảng [ShareDataMapping]...';

IF OBJECT_ID('dbo.ShareDataMapping', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataMapping', 'SyncKind') IS NOT NULL
    BEGIN
        PRINT N'   - Phát hiện cột di sản [SyncKind] trên [ShareDataMapping], đang dọn default constraint và DROP...';
        DECLARE @defMapSync NVARCHAR(256);
        SELECT @defMapSync = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'SyncKind';
        IF @defMapSync IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapSync + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [SyncKind];
        PRINT N'   -> Đã DROP cột [SyncKind] trên [ShareDataMapping].';
    END;

    IF COL_LENGTH('dbo.ShareDataMapping', 'TimeColumn') IS NOT NULL
    BEGIN
        PRINT N'   - Phát hiện cột di sản [TimeColumn] trên [ShareDataMapping], đang dọn default constraint và DROP...';
        DECLARE @defMapTime NVARCHAR(256);
        SELECT @defMapTime = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'TimeColumn';
        IF @defMapTime IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapTime + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [TimeColumn];
        PRINT N'   -> Đã DROP cột [TimeColumn] trên [ShareDataMapping].';
    END;

    IF COL_LENGTH('dbo.ShareDataMapping', 'CursorColumn') IS NOT NULL
    BEGIN
        PRINT N'   - Phát hiện cột di sản [CursorColumn] trên [ShareDataMapping], đang dọn default constraint và DROP...';
        DECLARE @defMapCur NVARCHAR(256);
        SELECT @defMapCur = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'CursorColumn';
        IF @defMapCur IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapCur + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [CursorColumn];
        PRINT N'   -> Đã DROP cột [CursorColumn] trên [ShareDataMapping].';
    END;

    IF COL_LENGTH('dbo.ShareDataMapping', 'LookbackDays') IS NOT NULL
    BEGIN
        PRINT N'   - Phát hiện cột di sản [LookbackDays] trên [ShareDataMapping], đang dọn default constraint và DROP...';
        DECLARE @defMapLook NVARCHAR(256);
        SELECT @defMapLook = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'LookbackDays';
        IF @defMapLook IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapLook + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [LookbackDays];
        PRINT N'   -> Đã DROP cột [LookbackDays] trên [ShareDataMapping].';
    END;

    PRINT N'   - Hoàn tất kiểm tra bảng [ShareDataMapping].';
END
ELSE
BEGIN
    PRINT N'   - Bảng [ShareDataMapping] không tồn tại, bỏ qua.';
END;
GO



PRINT N'=========================================================================================';
PRINT N'Hoàn tất: 06-sharedata-doi-ten-cot-dong-bo.sql';
PRINT N'=========================================================================================';
GO
