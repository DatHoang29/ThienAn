-- ==============================================================================
-- BẢNG GHI: ShareDataPacket
-- THỨ TỰ CHẠY: 1
-- MÔ TẢ: Xóa các cột thừa không còn dùng trong Entity ShareDataPacket trên CSDL Staging (10.10.8.30)
-- CÁC CỘT LOẠI BỎ: ExtractSql, TemplateSql, CursorTimeColumn, SyncMode, MaxLookBackDays
-- THỰC HIỆN: 30/10/2026
-- ==============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShareDataPacket')
BEGIN
    PRINT N'[BỎ QUA] Bảng ShareDataPacket không tồn tại trên CSDL.';
    SET NOEXEC ON;
END
GO

-- 1. Xóa ràng buộc mặc định (nếu có) và xóa cột ExtractSql (nvarchar(max))
-- Lý do: Đã chuyển toàn bộ sang bảng ShareDataSql (cột ExtractSql) quản lý mẫu chung và riêng đối tác.
IF COL_LENGTH('ShareDataPacket', 'ExtractSql') IS NOT NULL
BEGIN
    DECLARE @df_ExtractSql NVARCHAR(256);
    SELECT @df_ExtractSql = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('ShareDataPacket') AND c.name = 'ExtractSql';

    IF @df_ExtractSql IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [ShareDataPacket] DROP CONSTRAINT [' + @df_ExtractSql + ']');
        PRINT N'Đã xóa Default Constraint: ' + @df_ExtractSql;
    END

    ALTER TABLE [ShareDataPacket] DROP COLUMN [ExtractSql];
    PRINT N'[THÀNH CÔNG] Đã xóa cột ExtractSql khỏi bảng ShareDataPacket.';
END
ELSE
BEGIN
    PRINT N'[ĐÃ XÓA] Cột ExtractSql không còn tồn tại trong bảng ShareDataPacket.';
END
GO

-- 2. Xóa ràng buộc mặc định (nếu có) và xóa cột TemplateSql (nvarchar(max))
-- Lý do: Đã bỏ ngày 27/10/2026, câu SQL chỉ còn một nguồn sự thật duy nhất tại ShareDataSql.
IF COL_LENGTH('ShareDataPacket', 'TemplateSql') IS NOT NULL
BEGIN
    DECLARE @df_TemplateSql NVARCHAR(256);
    SELECT @df_TemplateSql = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('ShareDataPacket') AND c.name = 'TemplateSql';

    IF @df_TemplateSql IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [ShareDataPacket] DROP CONSTRAINT [' + @df_TemplateSql + ']');
        PRINT N'Đã xóa Default Constraint: ' + @df_TemplateSql;
    END

    ALTER TABLE [ShareDataPacket] DROP COLUMN [TemplateSql];
    PRINT N'[THÀNH CÔNG] Đã xóa cột TemplateSql khỏi bảng ShareDataPacket.';
END
ELSE
BEGIN
    PRINT N'[ĐÃ XÓA] Cột TemplateSql không còn tồn tại trong bảng ShareDataPacket.';
END
GO

-- 3. Xóa ràng buộc mặc định (nếu có) và xóa cột CursorTimeColumn (nvarchar(128))
-- Lý do: Hệ thống đã chuyển sang phân tích AST ScriptDom tự động nhận diện __lastTime / __lastKey từ câu SQL.
IF COL_LENGTH('ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
BEGIN
    DECLARE @df_CursorTimeColumn NVARCHAR(256);
    SELECT @df_CursorTimeColumn = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('ShareDataPacket') AND c.name = 'CursorTimeColumn';

    IF @df_CursorTimeColumn IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [ShareDataPacket] DROP CONSTRAINT [' + @df_CursorTimeColumn + ']');
        PRINT N'Đã xóa Default Constraint: ' + @df_CursorTimeColumn;
    END

    ALTER TABLE [ShareDataPacket] DROP COLUMN [CursorTimeColumn];
    PRINT N'[THÀNH CÔNG] Đã xóa cột CursorTimeColumn khỏi bảng ShareDataPacket.';
END
ELSE
BEGIN
    PRINT N'[ĐÃ XÓA] Cột CursorTimeColumn không còn tồn tại trong bảng ShareDataPacket.';
END
GO

-- 4. Xóa ràng buộc mặc định (nếu có) và xóa cột SyncMode (int)
-- Lý do: Đã loại bỏ khỏi thiết kế gói tin.
IF COL_LENGTH('ShareDataPacket', 'SyncMode') IS NOT NULL
BEGIN
    DECLARE @df_SyncMode NVARCHAR(256);
    SELECT @df_SyncMode = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('ShareDataPacket') AND c.name = 'SyncMode';

    IF @df_SyncMode IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [ShareDataPacket] DROP CONSTRAINT [' + @df_SyncMode + ']');
        PRINT N'Đã xóa Default Constraint: ' + @df_SyncMode;
    END

    ALTER TABLE [ShareDataPacket] DROP COLUMN [SyncMode];
    PRINT N'[THÀNH CÔNG] Đã xóa cột SyncMode khỏi bảng ShareDataPacket.';
END
ELSE
BEGIN
    PRINT N'[ĐÃ XÓA] Cột SyncMode không còn tồn tại trong bảng ShareDataPacket.';
END
GO

-- 5. Xóa ràng buộc mặc định (nếu có) và xóa cột MaxLookBackDays (int)
-- Lý do: Đã loại bỏ khỏi thiết kế gói tin.
IF COL_LENGTH('ShareDataPacket', 'MaxLookBackDays') IS NOT NULL
BEGIN
    DECLARE @df_MaxLookBackDays NVARCHAR(256);
    SELECT @df_MaxLookBackDays = d.name
    FROM sys.default_constraints d
    INNER JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID('ShareDataPacket') AND c.name = 'MaxLookBackDays';

    IF @df_MaxLookBackDays IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE [ShareDataPacket] DROP CONSTRAINT [' + @df_MaxLookBackDays + ']');
        PRINT N'Đã xóa Default Constraint: ' + @df_MaxLookBackDays;
    END

    ALTER TABLE [ShareDataPacket] DROP COLUMN [MaxLookBackDays];
    PRINT N'[THÀNH CÔNG] Đã xóa cột MaxLookBackDays khỏi bảng ShareDataPacket.';
END
ELSE
BEGIN
    PRINT N'[ĐÃ XÓA] Cột MaxLookBackDays không còn tồn tại trong bảng ShareDataPacket.';
END
GO

SET NOEXEC OFF;
PRINT N'=== HOÀN TẤT DỌN DẸP CỘT THỪA BẢNG ShareDataPacket ===';
GO

