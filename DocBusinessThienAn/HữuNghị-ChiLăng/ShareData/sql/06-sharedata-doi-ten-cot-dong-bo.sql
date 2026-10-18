-- =========================================================================================
-- Script: 06-sharedata-doi-ten-cot-dong-bo.sql
-- Mục đích: Chuẩn hóa và đồng bộ 3 trường cấu hình đồng bộ ở phân hệ ShareData:
--           1. SyncKind     -> SyncMode        (1 = Snapshot, 2 = Incremental)
--           2. TimeColumn / CursorColumn -> CursorTimeColumn (tên cột mốc thời gian con trỏ)
--           3. LookbackDays / MaxLookbackDays -> MaxLookBackDays (số ngày quét lùi tối đa)
--           Theo Rule 19.85: Entity là SSOT, CodeFirst chạy trước tạo sẵn các cột đích.
--           Script này chạy SAU CodeFirst:
--           - Kiểm tra điều kiện tiên quyết: nếu chưa có cột mới thì PRINT "chưa chạy CodeFirst" và bỏ qua.
--           - Sao chép dữ liệu: UPDATE cột mới = cột cũ WHERE cột mới IS NULL AND cột cũ IS NOT NULL.
--           - Dọn dẹp: Xóa default constraint (nếu có) và DROP cột cũ.
--           - TUYỆT ĐỐI KHÔNG đổi tên cột trực tiếp trên CSDL; Entity là SSOT duy nhất.
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
-- 0. KIỂM TRA ĐIỀU KIỆN TIÊN QUYẾT: CodeFirst đã tạo các cột mới chưa (Rule 19.85)
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 0. Kiểm tra điều kiện tiên quyết (CodeFirst tạo cột mới)...';

IF OBJECT_ID('dbo.ShareDataPacket', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NULL
       OR COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NULL
       OR COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NULL
    BEGIN
        PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa có đầy đủ các cột mới (SyncMode, CursorTimeColumn, MaxLookBackDays).';
        PRINT N'   Nguyên nhân: Chưa chạy CodeFirst để sinh cột mới từ Entity (Rule 19.85).';
        PRINT N'   Hướng dẫn: Khởi động WebAPI để CodeFirst tạo khung cột mới trước khi chạy script này.';
        PRINT N'=========================================================================================';
        PRINT N'BỎ QUA THỰC THI: 06-sharedata-doi-ten-cot-dong-bo.sql';
        PRINT N'=========================================================================================';
        RETURN;
    END
    ELSE
    BEGIN
        PRINT N'   ✅ Đã phát hiện đầy đủ 3 cột mới do CodeFirst tạo trên [ShareDataPacket]: SyncMode, CursorTimeColumn, MaxLookBackDays.';
    END;
END
ELSE
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataPacket] chưa tồn tại trên CSDL DEV_ITS10 (chưa chạy CodeFirst).';
    PRINT N'   Hướng dẫn: Khởi động WebAPI để CodeFirst tạo khung bảng và cột trước khi chạy script này.';
    PRINT N'=========================================================================================';
    PRINT N'BỎ QUA THỰC THI: 06-sharedata-doi-ten-cot-dong-bo.sql';
    PRINT N'=========================================================================================';
    RETURN;
END;

-- ─────────────────────────────────────────────────────────────────────────────────────────
-- 1. BẢNG [dbo].[ShareDataPacket] (Chuyển dữ liệu sang cột mới và DROP cột cũ)
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 1. Kiểm tra và chuyển dữ liệu sang cột mới trên bảng [ShareDataPacket]...';

-- 1.1 SyncKind -> SyncMode
IF COL_LENGTH('dbo.ShareDataPacket', 'SyncKind') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NOT NULL
    BEGIN
        PRINT N'   - Đang chuyển dữ liệu từ [SyncKind] sang [SyncMode] trên [ShareDataPacket]...';
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
        IF @defSyncKind IS NOT NULL
        BEGIN
            EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defSyncKind + N'];');
            PRINT N'   -> Đã DROP default constraint [' + @defSyncKind + N'] của [SyncKind].';
        END;

        ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [SyncKind];
        PRINT N'   -> Đã DROP cột cũ [SyncKind] trên [ShareDataPacket].';
    END
    ELSE
    BEGIN
        PRINT N'   ⚠️ Cột đích [SyncMode] chưa tồn tại (chưa chạy CodeFirst). Bỏ qua thao tác trên [SyncKind].';
    END;
END
ELSE
BEGIN
    PRINT N'   - Cột cũ [SyncKind] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;

-- 1.2 TimeColumn / CursorColumn -> CursorTimeColumn
IF COL_LENGTH('dbo.ShareDataPacket', 'TimeColumn') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
    BEGIN
        PRINT N'   - Đang chuyển dữ liệu từ [TimeColumn] sang [CursorTimeColumn] trên [ShareDataPacket]...';
        EXEC sp_executesql N'
            UPDATE [dbo].[ShareDataPacket]
            SET [CursorTimeColumn] = [TimeColumn]
            WHERE [CursorTimeColumn] IS NULL AND [TimeColumn] IS NOT NULL;
        ';
        PRINT N'   -> Đã sao chép dữ liệu từ [TimeColumn] sang [CursorTimeColumn].';

        DECLARE @defTimeCol NVARCHAR(256);
        SELECT @defTimeCol = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'TimeColumn';
        IF @defTimeCol IS NOT NULL
        BEGIN
            EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defTimeCol + N'];');
            PRINT N'   -> Đã DROP default constraint [' + @defTimeCol + N'] của [TimeColumn].';
        END;

        ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [TimeColumn];
        PRINT N'   -> Đã DROP cột cũ [TimeColumn] trên [ShareDataPacket].';
    END
    ELSE
    BEGIN
        PRINT N'   ⚠️ Cột đích [CursorTimeColumn] chưa tồn tại (chưa chạy CodeFirst). Bỏ qua thao tác trên [TimeColumn].';
    END;
END
ELSE
BEGIN
    PRINT N'   - Cột cũ [TimeColumn] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;

IF COL_LENGTH('dbo.ShareDataPacket', 'CursorColumn') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
    BEGIN
        PRINT N'   - Đang chuyển dữ liệu từ [CursorColumn] sang [CursorTimeColumn] trên [ShareDataPacket]...';
        EXEC sp_executesql N'
            UPDATE [dbo].[ShareDataPacket]
            SET [CursorTimeColumn] = [CursorColumn]
            WHERE [CursorTimeColumn] IS NULL AND [CursorColumn] IS NOT NULL;
        ';
        PRINT N'   -> Đã sao chép dữ liệu từ [CursorColumn] sang [CursorTimeColumn].';

        DECLARE @defCurCol NVARCHAR(256);
        SELECT @defCurCol = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'CursorColumn';
        IF @defCurCol IS NOT NULL
        BEGIN
            EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defCurCol + N'];');
            PRINT N'   -> Đã DROP default constraint [' + @defCurCol + N'] của [CursorColumn].';
        END;

        ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [CursorColumn];
        PRINT N'   -> Đã DROP cột cũ [CursorColumn] trên [ShareDataPacket].';
    END
    ELSE
    BEGIN
        PRINT N'   ⚠️ Cột đích [CursorTimeColumn] chưa tồn tại (chưa chạy CodeFirst). Bỏ qua thao tác trên [CursorColumn].';
    END;
END
ELSE
BEGIN
    PRINT N'   - Cột cũ [CursorColumn] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;

-- 1.3 LookbackDays / MaxLookbackDays -> MaxLookBackDays
IF COL_LENGTH('dbo.ShareDataPacket', 'LookbackDays') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NOT NULL
    BEGIN
        PRINT N'   - Đang chuyển dữ liệu từ [LookbackDays] sang [MaxLookBackDays] trên [ShareDataPacket]...';
        EXEC sp_executesql N'
            UPDATE [dbo].[ShareDataPacket]
            SET [MaxLookBackDays] = [LookbackDays]
            WHERE [MaxLookBackDays] IS NULL AND [LookbackDays] IS NOT NULL;
        ';
        PRINT N'   -> Đã sao chép dữ liệu từ [LookbackDays] sang [MaxLookBackDays].';

        DECLARE @defLookback NVARCHAR(256);
        SELECT @defLookback = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'LookbackDays';
        IF @defLookback IS NOT NULL
        BEGIN
            EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defLookback + N'];');
            PRINT N'   -> Đã DROP default constraint [' + @defLookback + N'] của [LookbackDays].';
        END;

        ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [LookbackDays];
        PRINT N'   -> Đã DROP cột cũ [LookbackDays] trên [ShareDataPacket].';
    END
    ELSE
    BEGIN
        PRINT N'   ⚠️ Cột đích [MaxLookBackDays] chưa tồn tại (chưa chạy CodeFirst). Bỏ qua thao tác trên [LookbackDays].';
    END;
END
ELSE
BEGIN
    PRINT N'   - Cột cũ [LookbackDays] không tồn tại trên [ShareDataPacket], bỏ qua.';
END;

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataPacket') AND name = 'MaxLookbackDays' COLLATE Latin1_General_CS_AS)
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataPacket') AND name = 'MaxLookBackDays' COLLATE Latin1_General_CS_AS)
BEGIN
    PRINT N'   - Phát hiện cột [MaxLookbackDays] khác chữ hoa với [MaxLookBackDays], đang đồng bộ và dọn dẹp...';
    EXEC sp_executesql N'
        UPDATE [dbo].[ShareDataPacket]
        SET [MaxLookBackDays] = [MaxLookbackDays]
        WHERE [MaxLookBackDays] IS NULL AND [MaxLookbackDays] IS NOT NULL;
    ';
    DECLARE @defMaxLookbackCs NVARCHAR(256);
    SELECT @defMaxLookbackCs = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataPacket') AND c.name = 'MaxLookbackDays' COLLATE Latin1_General_CS_AS;
    IF @defMaxLookbackCs IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[ShareDataPacket] DROP CONSTRAINT [' + @defMaxLookbackCs + N'];');
        PRINT N'   -> Đã DROP default constraint [' + @defMaxLookbackCs + N'] của [MaxLookbackDays].';
    END;

    ALTER TABLE [dbo].[ShareDataPacket] DROP COLUMN [MaxLookbackDays];
    PRINT N'   -> Đã DROP cột cũ [MaxLookbackDays] trên [ShareDataPacket].';
END;

-- ─────────────────────────────────────────────────────────────────────────────────────────
-- 2. BẢNG [dbo].[ShareDataMapping] (Dọn dẹp cột di sản nếu từng phát sinh trong CSDL cũ)
-- ─────────────────────────────────────────────────────────────────────────────────────────
PRINT N'-> 2. Kiểm tra cột di sản trên bảng [ShareDataMapping]...';

IF OBJECT_ID('dbo.ShareDataMapping', 'U') IS NOT NULL
BEGIN
    -- 2.1 SyncKind
    IF COL_LENGTH('dbo.ShareDataMapping', 'SyncKind') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataMapping', 'SyncMode') IS NOT NULL
        BEGIN
            PRINT N'   - Cả [SyncKind] và [SyncMode] cùng tồn tại trên [ShareDataMapping], đồng bộ sang [SyncMode]...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataMapping]
                SET [SyncMode] = [SyncKind]
                WHERE [SyncMode] IS NULL AND [SyncKind] IS NOT NULL;
            ';
        END;

        PRINT N'   - Đang dọn default constraint và DROP cột [SyncKind] trên [ShareDataMapping]...';
        DECLARE @defMapSync NVARCHAR(256);
        SELECT @defMapSync = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'SyncKind';
        IF @defMapSync IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapSync + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [SyncKind];
        PRINT N'   -> Đã DROP cột [SyncKind] trên [ShareDataMapping].';
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [SyncKind] không tồn tại trên [ShareDataMapping], bỏ qua.';
    END;

    -- 2.2 TimeColumn
    IF COL_LENGTH('dbo.ShareDataMapping', 'TimeColumn') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataMapping', 'CursorTimeColumn') IS NOT NULL
        BEGIN
            PRINT N'   - Cả [TimeColumn] và [CursorTimeColumn] cùng tồn tại trên [ShareDataMapping], đồng bộ...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataMapping]
                SET [CursorTimeColumn] = [TimeColumn]
                WHERE [CursorTimeColumn] IS NULL AND [TimeColumn] IS NOT NULL;
            ';
        END;

        PRINT N'   - Đang dọn default constraint và DROP cột [TimeColumn] trên [ShareDataMapping]...';
        DECLARE @defMapTime NVARCHAR(256);
        SELECT @defMapTime = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'TimeColumn';
        IF @defMapTime IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapTime + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [TimeColumn];
        PRINT N'   -> Đã DROP cột [TimeColumn] trên [ShareDataMapping].';
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [TimeColumn] không tồn tại trên [ShareDataMapping], bỏ qua.';
    END;

    -- 2.3 CursorColumn
    IF COL_LENGTH('dbo.ShareDataMapping', 'CursorColumn') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataMapping', 'CursorTimeColumn') IS NOT NULL
        BEGIN
            PRINT N'   - Cả [CursorColumn] và [CursorTimeColumn] cùng tồn tại trên [ShareDataMapping], đồng bộ...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataMapping]
                SET [CursorTimeColumn] = [CursorColumn]
                WHERE [CursorTimeColumn] IS NULL AND [CursorColumn] IS NOT NULL;
            ';
        END;

        PRINT N'   - Đang dọn default constraint và DROP cột [CursorColumn] trên [ShareDataMapping]...';
        DECLARE @defMapCur NVARCHAR(256);
        SELECT @defMapCur = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'CursorColumn';
        IF @defMapCur IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapCur + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [CursorColumn];
        PRINT N'   -> Đã DROP cột [CursorColumn] trên [ShareDataMapping].';
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [CursorColumn] không tồn tại trên [ShareDataMapping], bỏ qua.';
    END;

    -- 2.4 LookbackDays
    IF COL_LENGTH('dbo.ShareDataMapping', 'LookbackDays') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.ShareDataMapping', 'MaxLookBackDays') IS NOT NULL
        BEGIN
            PRINT N'   - Cả [LookbackDays] và [MaxLookBackDays] cùng tồn tại trên [ShareDataMapping], đồng bộ...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataMapping]
                SET [MaxLookBackDays] = [LookbackDays]
                WHERE [MaxLookBackDays] IS NULL AND [LookbackDays] IS NOT NULL;
            ';
        END;

        PRINT N'   - Đang dọn default constraint và DROP cột [LookbackDays] trên [ShareDataMapping]...';
        DECLARE @defMapLook NVARCHAR(256);
        SELECT @defMapLook = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'LookbackDays';
        IF @defMapLook IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapLook + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [LookbackDays];
        PRINT N'   -> Đã DROP cột [LookbackDays] trên [ShareDataMapping].';
    END
    ELSE
    BEGIN
        PRINT N'   - Cột cũ [LookbackDays] không tồn tại trên [ShareDataMapping], bỏ qua.';
    END;

    -- 2.5 MaxLookbackDays (nếu có trên ShareDataMapping)
    IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataMapping') AND name = 'MaxLookbackDays' COLLATE Latin1_General_CS_AS)
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ShareDataMapping') AND name = 'MaxLookBackDays' COLLATE Latin1_General_CS_AS)
        BEGIN
            PRINT N'   - Cả [MaxLookbackDays] và [MaxLookBackDays] cùng tồn tại trên [ShareDataMapping], đồng bộ...';
            EXEC sp_executesql N'
                UPDATE [dbo].[ShareDataMapping]
                SET [MaxLookBackDays] = [MaxLookbackDays]
                WHERE [MaxLookBackDays] IS NULL AND [MaxLookbackDays] IS NOT NULL;
            ';
        END;

        PRINT N'   - Đang dọn default constraint và DROP cột [MaxLookbackDays] trên [ShareDataMapping]...';
        DECLARE @defMapMaxLook NVARCHAR(256);
        SELECT @defMapMaxLook = d.name
        FROM sys.default_constraints d
        JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
        WHERE d.parent_object_id = OBJECT_ID('dbo.ShareDataMapping') AND c.name = 'MaxLookbackDays' COLLATE Latin1_General_CS_AS;
        IF @defMapMaxLook IS NOT NULL EXEC(N'ALTER TABLE [dbo].[ShareDataMapping] DROP CONSTRAINT [' + @defMapMaxLook + N'];');
        ALTER TABLE [dbo].[ShareDataMapping] DROP COLUMN [MaxLookbackDays];
        PRINT N'   -> Đã DROP cột [MaxLookbackDays] trên [ShareDataMapping].';
    END;

    PRINT N'   - Hoàn tất kiểm tra bảng [ShareDataMapping].';
END
ELSE
BEGIN
    PRINT N'   - Bảng [ShareDataMapping] không tồn tại, bỏ qua.';
END;

PRINT N'=========================================================================================';
PRINT N'Hoàn tất: 06-sharedata-doi-ten-cot-dong-bo.sql';
PRINT N'=========================================================================================';
GO
