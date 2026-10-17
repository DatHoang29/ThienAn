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
--              5. Tạo bảng ShareDataTrackTable, di trú cấu hình từ SysOpConfig (sharedata_tracker)
--                 kèm mốc LastVersion từ ShareDataTrackVersion, xóa mềm SysOpConfig và DROP ShareDataTrackVersion.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: ⚠️ CHỈ CHẠY MỘT LẦN KHI NÂNG CẤP/DI TRÚ HỆ THỐNG.
-- CẬP NHẬT LẦN CUỐI: 17/10/2026
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

-- -----------------------------------------------------------------------------------------
-- 5. TẠO BẢNG ShareDataTrackTable, DI TRÚ TỪ SysOpConfig + ShareDataTrackVersion VÀ DỌN BẢNG CŨ
-- -----------------------------------------------------------------------------------------
-- 5.a Tạo bảng dbo.ShareDataTrackTable nếu chưa tồn tại
IF OBJECT_ID(N'[dbo].[ShareDataTrackTable]', N'U') IS NULL
BEGIN
    PRINT N'  -> Đang tạo bảng [dbo].[ShareDataTrackTable]...';
    CREATE TABLE [dbo].[ShareDataTrackTable] (
        [ID]             VARCHAR(64)    NOT NULL PRIMARY KEY,
        [TableName]      NVARCHAR(128)  NOT NULL,
        [Name]           NVARCHAR(128)  NULL,
        [PacketCodes]    NVARCHAR(250)  NULL,
        [Status]         INT            NULL,
        [OrderNo]        INT            NULL,
        [Remark]         NVARCHAR(1000) NULL,
        [LastVersion]    BIGINT         NULL,
        [LastChangeTime] DATETIME       NULL,
        [LastIssue]      NVARCHAR(250)  NULL,
        [Code]           NVARCHAR(128)  NULL,
        [TenantId]       VARCHAR(64)    NULL,
        [CreateTime]     DATETIME       NULL,
        [UpdateTime]     DATETIME       NULL,
        [CreateUId]      VARCHAR(64)    NULL,
        [UpdateUId]      VARCHAR(64)    NULL,
        [RowStatus]      NVARCHAR(64)   NULL,
        [IsDelete]       DATETIME       NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX [index_ShareDataTrackTable_TableName]
        ON [dbo].[ShareDataTrackTable] ([TableName] ASC, [IsDelete] ASC);

    PRINT N'  -> Đã tạo bảng [dbo].[ShareDataTrackTable] và unique index thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng [dbo].[ShareDataTrackTable] đã tồn tại, bỏ qua bước tạo bảng.';
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'index_ShareDataTrackTable_TableName'
          AND object_id = OBJECT_ID(N'[dbo].[ShareDataTrackTable]')
    )
    BEGIN
        CREATE UNIQUE NONCLUSTERED INDEX [index_ShareDataTrackTable_TableName]
            ON [dbo].[ShareDataTrackTable] ([TableName] ASC, [IsDelete] ASC);
        PRINT N'  -> Đã bổ sung unique index [index_ShareDataTrackTable_TableName].';
    END
END
GO

-- 5.b Nếu bảng ShareDataTrackTable đang rỗng (0 dòng), chuyển dữ liệu từ SysOpConfig + ShareDataTrackVersion
IF OBJECT_ID(N'[dbo].[ShareDataTrackTable]', N'U') IS NOT NULL
BEGIN
    DECLARE @CurrentTrackCount INT = 0;
    SELECT @CurrentTrackCount = COUNT(1) FROM [dbo].[ShareDataTrackTable];

    IF @CurrentTrackCount = 0
    BEGIN
        DECLARE @OldVersion BIGINT = NULL;

        IF OBJECT_ID(N'[dbo].[ShareDataTrackVersion]', N'U') IS NOT NULL
        BEGIN
            DECLARE @sqlReadOldVer NVARCHAR(500) = N'
                SELECT TOP 1 @verOut = [LastVersion]
                FROM [dbo].[ShareDataTrackVersion]
                WHERE [IsDelete] IS NULL
                ORDER BY [ID];
            ';
            EXEC sp_executesql @sqlReadOldVer, N'@verOut BIGINT OUTPUT', @verOut = @OldVersion OUTPUT;
            PRINT N'  -> Đọc mốc LastVersion từ ShareDataTrackVersion: ' + ISNULL(CAST(@OldVersion AS NVARCHAR(30)), N'NULL');
        END

        IF OBJECT_ID(N'[dbo].[SysOpConfig]', N'U') IS NOT NULL
           AND EXISTS (SELECT 1 FROM [dbo].[SysOpConfig] WHERE [GroupCode] = N'sharedata_tracker' AND [IsDelete] IS NULL)
        BEGIN
            INSERT INTO [dbo].[ShareDataTrackTable] (
                [ID], [TableName], [Name], [PacketCodes], [Status], [OrderNo],
                [Remark], [LastVersion], [TenantId], [CreateTime], [CreateUId]
            )
            SELECT
                CASE [Code]
                    WHEN N'TmsZoneStatus'          THEN '1500000000201'
                    WHEN N'TmsZone'                THEN '1500000000202'
                    WHEN N'TmsTrafficStatistic'    THEN '1500000000203'
                    WHEN N'CctvDevice'             THEN '1500000000204'
                    WHEN N'TmsTrafficData'         THEN '1500000000205'
                    WHEN N'TmsWeather'             THEN '1500000000206'
                    WHEN N'TollTransactionIn'      THEN '1500000000207'
                    WHEN N'TollTransactionOut'     THEN '1500000000208'
                    WHEN N'TmsVehicleRegistration' THEN '1500000000209'
                    WHEN N'TmsIncident'            THEN '1500000000210'
                    WHEN N'TmsEventType'           THEN '1500000000211'
                    WHEN N'VmsCurrent'             THEN '1500000000212'
                    WHEN N'TmsEquipment'           THEN '1500000000213'
                    WHEN N'TollLane'               THEN '1500000000214'
                    WHEN N'TollStation'            THEN '1500000000215'
                    ELSE LOWER(CAST(NEWID() AS VARCHAR(64)))
                END,
                [Code],
                [Name],
                [Value],
                [Status],
                [OrderNo],
                [Remark],
                @OldVersion,
                [TenantId],
                ISNULL([CreateTime], GETDATE()),
                ISNULL([CreateUId], 'superadmin')
            FROM [dbo].[SysOpConfig]
            WHERE [GroupCode] = N'sharedata_tracker'
              AND [IsDelete] IS NULL;

            PRINT N'  -> Đã di trú ' + CAST(@@ROWCOUNT AS NVARCHAR(20))
                + N' dòng từ SysOpConfig (sharedata_tracker) sang ShareDataTrackTable.';
        END
        ELSE
        BEGIN
            PRINT N'  -> Không có dòng SysOpConfig (sharedata_tracker) nào để di trú (sẽ được nạp ở script 03).';
        END
    END
    ELSE
    BEGIN
        PRINT N'  -> Bảng ShareDataTrackTable đã có ' + CAST(@CurrentTrackCount AS NVARCHAR(20)) + N' dòng, bỏ qua bước chép từ SysOpConfig.';
    END
END
GO

-- 5.c Xóa mềm các dòng SysOpConfig có GroupCode = 'sharedata_tracker'
IF OBJECT_ID(N'[dbo].[SysOpConfig]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM [dbo].[SysOpConfig] WHERE [GroupCode] = N'sharedata_tracker' AND [IsDelete] IS NULL)
    BEGIN
        UPDATE [dbo].[SysOpConfig]
        SET [IsDelete] = GETDATE()
        WHERE [GroupCode] = N'sharedata_tracker'
          AND [IsDelete] IS NULL;

        PRINT N'  -> Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(20)) + N' dòng SysOpConfig (GroupCode = sharedata_tracker).';
    END
    ELSE
    BEGIN
        PRINT N'  -> Không còn dòng SysOpConfig (GroupCode = sharedata_tracker) đang hoạt động, bỏ qua.';
    END
END
GO

-- 5.d Kiểm tra và DROP bảng cũ ShareDataTrackVersion
IF OBJECT_ID(N'[dbo].[ShareDataTrackVersion]', N'U') IS NOT NULL
BEGIN
    DECLARE @trackVerRowCount INT = 0;
    DECLARE @sqlCountTrackVer NVARCHAR(500) = N'SELECT @cnt = COUNT(1) FROM [dbo].[ShareDataTrackVersion]';
    EXEC sp_executesql @sqlCountTrackVer, N'@cnt INT OUTPUT', @cnt = @trackVerRowCount OUTPUT;

    PRINT N'  -> Bảng cũ ShareDataTrackVersion hiện có ' + CAST(@trackVerRowCount AS NVARCHAR(20)) + N' bản ghi trước khi DROP.';
    DROP TABLE [dbo].[ShareDataTrackVersion];
    PRINT N'  -> Đã DROP bảng cũ [dbo].[ShareDataTrackVersion] thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng cũ [dbo].[ShareDataTrackVersion] không tồn tại, bỏ qua.';
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA!';
PRINT N'=============================================================================';
