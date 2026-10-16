USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 00-sharedata-migration-mot-lan.sql
-- DESCRIPTION: Kịch bản di trú một lần cho phân hệ ShareData (ESHARE):
--              1. Bổ sung các cột cấu hình truy vấn và đồng bộ vào ShareDataPacket
--                 (ExtractSql, SyncMode, CursorTimeColumn, MaxLookBackDays).
--              2. Đồng bộ các cột cấu hình vào CSDL test (nếu tồn tại) cho ShareDataPacket.
--              3. Kiểm tra và DROP bảng mồ côi ShareDataTable (chỉ xóa nếu bảng rỗng).
--              4. Dọn dòng ROOT của thiết kế cũ trong ShareDataPacketSql (xóa mềm).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: ⚠️ CHỈ CHẠY MỘT LẦN KHI NÂNG CẤP/DI TRÚ HỆ THỐNG.
-- CẬP NHẬT LẦN CUỐI: 16/10/2026
-- =========================================================================================

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA (00-sharedata-migration-mot-lan.sql)';
PRINT N'=============================================================================';

-- -----------------------------------------------------------------------------------------
-- 1. BỔ SUNG CỘT CẤU HÌNH TRUY VẤN VÀO BẢNG ShareDataPacket
-- -----------------------------------------------------------------------------------------
IF COL_LENGTH('dbo.ShareDataPacket', 'ExtractSql') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột ExtractSql vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [ExtractSql] NVARCHAR(MAX) NULL;
END
ELSE
    PRINT N'  -> Cột ExtractSql đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột SyncMode vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [SyncMode] INT NULL; -- 1 = Snapshot, 2 = Incremental
END
ELSE
    PRINT N'  -> Cột SyncMode đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột CursorTimeColumn vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [CursorTimeColumn] NVARCHAR(128) NULL;
END
ELSE
    PRINT N'  -> Cột CursorTimeColumn đã tồn tại trên ShareDataPacket, bỏ qua.';

IF COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NULL
BEGIN
    PRINT N'  -> Đang thêm cột MaxLookBackDays vào bảng ShareDataPacket...';
    ALTER TABLE [dbo].[ShareDataPacket] ADD [MaxLookBackDays] INT NULL;
END
ELSE
    PRINT N'  -> Cột MaxLookBackDays đã tồn tại trên ShareDataPacket, bỏ qua.';
GO

-- -----------------------------------------------------------------------------------------
-- 2. ĐỒNG BỘ CỘT CẤU HÌNH VÀO CSDL test (NẾU TỒN TẠI) CHO BẢNG ShareDataPacket
-- -----------------------------------------------------------------------------------------
IF DB_ID('test') IS NOT NULL
BEGIN
    PRINT N'  -> Phát hiện CSDL [test], kiểm tra và bổ sung cột vào [test].[dbo].[ShareDataPacket]...';
    EXEC('
        USE [test];
        IF COL_LENGTH(''dbo.ShareDataPacket'', ''ExtractSql'') IS NULL
        BEGIN
            PRINT N''    -> [test] Đang thêm cột ExtractSql vào bảng ShareDataPacket...'';
            ALTER TABLE [dbo].[ShareDataPacket] ADD [ExtractSql] NVARCHAR(MAX) NULL;
        END

        IF COL_LENGTH(''dbo.ShareDataPacket'', ''SyncMode'') IS NULL
        BEGIN
            PRINT N''    -> [test] Đang thêm cột SyncMode vào bảng ShareDataPacket...'';
            ALTER TABLE [dbo].[ShareDataPacket] ADD [SyncMode] INT NULL;
        END

        IF COL_LENGTH(''dbo.ShareDataPacket'', ''CursorTimeColumn'') IS NULL
        BEGIN
            PRINT N''    -> [test] Đang thêm cột CursorTimeColumn vào bảng ShareDataPacket...'';
            ALTER TABLE [dbo].[ShareDataPacket] ADD [CursorTimeColumn] NVARCHAR(128) NULL;
        END

        IF COL_LENGTH(''dbo.ShareDataPacket'', ''MaxLookBackDays'') IS NULL
        BEGIN
            PRINT N''    -> [test] Đang thêm cột MaxLookBackDays vào bảng ShareDataPacket...'';
            ALTER TABLE [dbo].[ShareDataPacket] ADD [MaxLookBackDays] INT NULL;
        END
    ');
END
ELSE
BEGIN
    PRINT N'  -> CSDL [test] không tồn tại trên máy chủ hiện tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 3. KIỂM TRA VÀ DROP BẢNG MỒ CÔI ShareDataTable
-- -----------------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShareDataTable')
BEGIN
    DECLARE @rowCount INT;
    DECLARE @sqlCount NVARCHAR(500) = N'SELECT @cnt = COUNT(1) FROM [dbo].[ShareDataTable]';
    EXEC sp_executesql @sqlCount, N'@cnt INT OUTPUT', @cnt = @rowCount OUTPUT;

    PRINT N'  -> Bảng ShareDataTable hiện có ' + CAST(@rowCount AS NVARCHAR(20)) + N' bản ghi.';

    IF @rowCount > 0
    BEGIN
        PRINT N'⚠️ CẢNH BÁO: Bảng ShareDataTable đang có ' + CAST(@rowCount AS NVARCHAR(20)) + N' bản ghi dữ liệu!';
        PRINT N'⚠️ Vui lòng lập trình viên kiểm tra và sao lưu (backup) trước khi thực hiện DROP thủ công.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Bảng ShareDataTable có 0 dòng, tiến hành xóa (DROP) bảng mồ côi...';
        DROP TABLE [dbo].[ShareDataTable];
        PRINT N'  -> Đã DROP bảng ShareDataTable thành công.';
    END
END
ELSE
BEGIN
    PRINT N'  -> Bảng ShareDataTable không tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 4. DỌN DÒNG ROOT CỦA THIẾT KẾ CŨ TRONG BẢNG ShareDataPacketSql
-- -----------------------------------------------------------------------------------------
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ShareDataPacketSql')
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[ShareDataPacketSql] WHERE [TableName] = 'ROOT' AND [IsDelete] IS NULL)
    BEGIN
        DECLARE @rootCount INT;
        SELECT @rootCount = COUNT(1) FROM [dbo].[ShareDataPacketSql] WHERE [TableName] = 'ROOT' AND [IsDelete] IS NULL;
        PRINT N'  -> Phát hiện ' + CAST(@rootCount AS NVARCHAR(20)) + N' dòng ROOT của thiết kế cũ trong ShareDataPacketSql:';

        -- In danh sách các dòng bị ảnh hưởng (PacketCode, OrderNo, 100 ký tự đầu WriteSql)
        DECLARE @pCode NVARCHAR(100), @orderNo INT, @writeSqlPreview NVARCHAR(100);
        DECLARE curRoot CURSOR LOCAL FAST_FORWARD FOR
            SELECT [PacketCode], [OrderNo], LEFT(ISNULL([WriteSql], N''), 100)
            FROM [dbo].[ShareDataPacketSql]
            WHERE [TableName] = 'ROOT' AND [IsDelete] IS NULL
            ORDER BY [PacketCode], [OrderNo];

        OPEN curRoot;
        FETCH NEXT FROM curRoot INTO @pCode, @orderNo, @writeSqlPreview;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            PRINT N'     * PacketCode=' + ISNULL(@pCode, N'NULL')
                + N', OrderNo=' + CAST(ISNULL(@orderNo, 0) AS NVARCHAR(10))
                + N', WriteSql(100 ký tự đầu)=' + @writeSqlPreview;
            FETCH NEXT FROM curRoot INTO @pCode, @orderNo, @writeSqlPreview;
        END;
        CLOSE curRoot;
        DEALLOCATE curRoot;

        PRINT N'  -> Đang cập nhật IsDelete = 1 cho các dòng TableName = ''ROOT''...';
        UPDATE [dbo].[ShareDataPacketSql]
        SET [IsDelete] = 1
        WHERE [TableName] = 'ROOT' AND [IsDelete] IS NULL;
        PRINT N'  -> Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng ROOT thành công.';
    END
    ELSE
    BEGIN
        PRINT N'  -> Không có dòng ROOT nào cần dọn trong ShareDataPacketSql, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'  -> Bảng ShareDataPacketSql không tồn tại, bỏ qua.';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA!';
PRINT N'=============================================================================';
