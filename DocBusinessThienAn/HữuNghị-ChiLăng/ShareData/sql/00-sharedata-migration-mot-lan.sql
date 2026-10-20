USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 00-sharedata-migration-mot-lan.sql
-- DESCRIPTION: Kịch bản di trú một lần cho phân hệ ShareData (ESHARE):
--              1. Kiểm tra và DROP bảng mồ côi ShareDataTable (chỉ xóa nếu bảng rỗng).
--              2. Dọn dòng ROOT của thiết kế cũ trong ShareDataPacketSql (xóa mềm).
--              3. Di trú dữ liệu bảng nguồn sang ShareDataSourceTable (CodeFirst tạo khung trước):
--                 - Chép dữ liệu từ ShareDataTrackTable (nếu tồn tại) sang ShareDataSourceTable,
--                   giữ LastVersion, LastChangeTime, LastIssue, chuyển Status -> IsTracked, sau đó DROP ShareDataTrackTable.
--                 - Xóa cột Category trên ShareDataSourceTable nếu còn tồn tại.
--                 - Di trú từ SysOpConfig (sharedata_tracker) + ShareDataTrackVersion sang nếu bảng rỗng
--                   (chống trùng theo TableName, ID sinh động bằng LOWER(CONVERT(NVARCHAR(64), NEWID()))).
--                 - Xóa mềm các dòng SysOpConfig (sharedata_tracker).
--                 - Kiểm tra và DROP bảng cũ ShareDataTrackVersion.
--              4. Di trú cột ConnectionType trên ShareDataPartner từ NVARCHAR sang INT (1-Api, 2-File, 3-Ftp, 4-Ftps, 5-Kafka).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: ⚠️ CHỈ CHẠY MỘT LẦN KHI NÂNG CẤP/DI TRÚ HỆ THỐNG.
-- CẬP NHẬT LẦN CUỐI: 19/10/2026
-- =========================================================================================

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA (00-sharedata-migration-mot-lan.sql)';
PRINT N'=============================================================================';

-- -----------------------------------------------------------------------------------------
-- 1. KIỂM TRA VÀ DROP BẢNG MỒ CÔI ShareDataTable
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
-- 2. DỌN DÒNG ROOT CỦA THIẾT KẾ CŨ TRONG BẢNG ShareDataPacketSql
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
-- 3. DI TRÚ DỮ LIỆU BẢNG NGUỒN SANG ShareDataSourceTable VÀ DỌN BẢNG CŨ
-- -----------------------------------------------------------------------------------------
-- 3.a Nếu còn bảng cũ ShareDataTrackTable: chép dữ liệu sang ShareDataSourceTable rồi DROP ShareDataTrackTable
IF OBJECT_ID(N'[dbo].[ShareDataTrackTable]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NOT NULL
BEGIN
    PRINT N'  -> Phát hiện bảng cũ [dbo].[ShareDataTrackTable], đang chuyển dữ liệu sang [dbo].[ShareDataSourceTable]...';

    DECLARE @sqlMigrateTrackTable NVARCHAR(MAX) = N'
        INSERT INTO [dbo].[ShareDataSourceTable] (
            [ID], [TableName], [Name], [PacketCodes], [IsTracked], [OrderNo],
            [Remark], [LastVersion], [LastChangeTime], [LastIssue], [IsQueryable],
            [DefaultAlias], [TenantId], [CreateTime], [CreateUId]
        )
        SELECT
            src.[ID],
            src.[TableName],
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'Name') IS NOT NULL THEN N'src.[Name]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'PacketCodes') IS NOT NULL THEN N'src.[PacketCodes]' ELSE N'NULL' END + N',
            ' + CASE 
                    WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'IsTracked') IS NOT NULL THEN N'src.[IsTracked]'
                    WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'Status') IS NOT NULL THEN N'CASE WHEN src.[Status] = 1 THEN 1 ELSE 0 END'
                    ELSE N'1'
                END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'OrderNo') IS NOT NULL THEN N'src.[OrderNo]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'Remark') IS NOT NULL THEN N'src.[Remark]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'LastVersion') IS NOT NULL THEN N'src.[LastVersion]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'LastChangeTime') IS NOT NULL THEN N'src.[LastChangeTime]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'LastIssue') IS NOT NULL THEN N'src.[LastIssue]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'IsQueryable') IS NOT NULL THEN N'ISNULL(src.[IsQueryable], 1)' ELSE N'1' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'DefaultAlias') IS NOT NULL THEN N'src.[DefaultAlias]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'TenantId') IS NOT NULL THEN N'src.[TenantId]' ELSE N'NULL' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'CreateTime') IS NOT NULL THEN N'src.[CreateTime]' ELSE N'GETDATE()' END + N',
            ' + CASE WHEN COL_LENGTH(N'dbo.ShareDataTrackTable', N'CreateUId') IS NOT NULL THEN N'src.[CreateUId]' ELSE N'''superadmin''' END + N'
        FROM [dbo].[ShareDataTrackTable] src
        WHERE NOT EXISTS (
            SELECT 1 FROM [dbo].[ShareDataSourceTable] dst
            WHERE dst.[TableName] = src.[TableName]
        );
    ';
    EXEC sp_executesql @sqlMigrateTrackTable;
    PRINT N'  -> Đã di trú dữ liệu từ ShareDataTrackTable sang ShareDataSourceTable.';

    PRINT N'  -> Đang DROP bảng cũ [dbo].[ShareDataTrackTable]...';
    DROP TABLE [dbo].[ShareDataTrackTable];
    PRINT N'  -> Đã DROP bảng cũ [dbo].[ShareDataTrackTable] thành công.';
END
ELSE IF OBJECT_ID(N'[dbo].[ShareDataTrackTable]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSourceTable] chưa được CodeFirst tạo ra. Vui lòng chạy ứng dụng để CodeFirst tạo bảng trước khi chạy di trú!';
END
ELSE
BEGIN
    PRINT N'  -> Bảng cũ [dbo].[ShareDataTrackTable] không tồn tại, bỏ qua.';
END
GO

-- 3.b Xóa cột Category trên ShareDataSourceTable nếu còn tồn tại
IF OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ShareDataSourceTable', N'Category') IS NOT NULL
BEGIN
    PRINT N'  -> Phát hiện cột [Category] trên [dbo].[ShareDataSourceTable], đang dọn default constraint và DROP cột...';

    DECLARE @defConstraint NVARCHAR(256);
    SELECT @defConstraint = d.name
    FROM sys.default_constraints d
    JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
    WHERE d.parent_object_id = OBJECT_ID(N'dbo.ShareDataSourceTable')
      AND c.name = N'Category';

    IF @defConstraint IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [dbo].[ShareDataSourceTable] DROP CONSTRAINT [' + @defConstraint + N'];');
        PRINT N'  -> Đã DROP default constraint [' + @defConstraint + N'] của cột Category.';
    END;

    ALTER TABLE [dbo].[ShareDataSourceTable] DROP COLUMN [Category];
    PRINT N'  -> Đã DROP cột [Category] trên [dbo].[ShareDataSourceTable] thành công.';
END
ELSE
BEGIN
    PRINT N'  -> Cột [Category] không tồn tại trên ShareDataSourceTable, bỏ qua.';
END
GO

-- 3.c Nếu bảng ShareDataSourceTable đang rỗng (0 dòng), chuyển dữ liệu từ SysOpConfig + ShareDataTrackVersion
IF OBJECT_ID(N'[dbo].[ShareDataSourceTable]', N'U') IS NOT NULL
BEGIN
    DECLARE @CurrentSourceCount INT = 0;
    SELECT @CurrentSourceCount = COUNT(1) FROM [dbo].[ShareDataSourceTable];

    IF @CurrentSourceCount = 0
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
        ELSE
        BEGIN
            PRINT N'  -> Bảng cũ ShareDataTrackVersion không tồn tại, mốc phiên bản khởi đầu là NULL.';
        END

        IF OBJECT_ID(N'[dbo].[SysOpConfig]', N'U') IS NOT NULL
           AND EXISTS (SELECT 1 FROM [dbo].[SysOpConfig] WHERE [GroupCode] = N'sharedata_tracker' AND [IsDelete] IS NULL)
        BEGIN
            INSERT INTO [dbo].[ShareDataSourceTable] (
                [ID], [TableName], [Name], [PacketCodes], [IsTracked], [OrderNo],
                [Remark], [LastVersion], [IsQueryable], [DefaultAlias],
                [TenantId], [CreateTime], [CreateUId]
            )
            SELECT
                LOWER(CONVERT(NVARCHAR(64), NEWID())),
                src.[Code],
                src.[Name],
                src.[Value],
                CASE WHEN src.[Status] = 1 THEN 1 ELSE 0 END,
                src.[OrderNo],
                src.[Remark],
                @OldVersion,
                1,
                CASE src.[Code]
                    WHEN N'TmsZone'                THEN N'z'
                    WHEN N'TmsZoneStatus'          THEN N'zs'
                    WHEN N'TmsTrafficStatistic'    THEN N'ts'
                    WHEN N'CctvDevice'             THEN N'c'
                    WHEN N'TmsTrafficData'         THEN N'td'
                    WHEN N'TmsWeather'             THEN N'w'
                    WHEN N'TollTransactionIn'      THEN N'i'
                    WHEN N'TollTransactionOut'     THEN N't'
                    WHEN N'TmsVehicleRegistration' THEN N'vr'
                    WHEN N'TmsIncident'            THEN N'i'
                    WHEN N'TmsEventType'           THEN N'et'
                    WHEN N'VmsCurrent'             THEN N'v'
                    WHEN N'TmsEquipment'           THEN N'e'
                    WHEN N'TollLane'               THEN N'l'
                    WHEN N'TollStation'            THEN N's'
                    ELSE N't'
                END,
                src.[TenantId],
                ISNULL(src.[CreateTime], GETDATE()),
                ISNULL(src.[CreateUId], 'superadmin')
            FROM [dbo].[SysOpConfig] src
            WHERE src.[GroupCode] = N'sharedata_tracker'
              AND src.[IsDelete] IS NULL
              AND NOT EXISTS (
                  SELECT 1
                  FROM [dbo].[ShareDataSourceTable] target
                  WHERE target.[TableName] = src.[Code]
                    AND target.[IsDelete] IS NULL
              );

            PRINT N'  -> Đã di trú ' + CAST(@@ROWCOUNT AS NVARCHAR(20))
                + N' dòng từ SysOpConfig (sharedata_tracker) sang ShareDataSourceTable.';
        END
        ELSE
        BEGIN
            PRINT N'  -> Không có dòng SysOpConfig (sharedata_tracker) nào để di trú (sẽ được nạp ở script 03).';
        END
    END
    ELSE
    BEGIN
        PRINT N'  -> Bảng ShareDataSourceTable đã có ' + CAST(@CurrentSourceCount AS NVARCHAR(20)) + N' dòng, bỏ qua bước chép từ SysOpConfig.';
    END
END
GO

-- 3.d Xóa mềm các dòng SysOpConfig có GroupCode = 'sharedata_tracker'
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

-- 3.e Kiểm tra và DROP bảng cũ ShareDataTrackVersion
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

-- -----------------------------------------------------------------------------------------
-- 4. DI TRÚ CỘT ConnectionType TRÊN BẢNG ShareDataPartner TỪ NVARCHAR SANG INT
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ShareDataPartner', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ShareDataPartner', N'ConnectionType') IS NULL
    BEGIN
        PRINT N'  -> Cột ConnectionType chưa tồn tại trên ShareDataPartner (sẽ được CodeFirst tạo kiểu INT), bỏ qua.';
    END
    ELSE
    BEGIN
        DECLARE @curPartnerColType NVARCHAR(128);
        SELECT @curPartnerColType = tp.name
        FROM sys.columns c
        JOIN sys.types tp ON c.user_type_id = tp.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.ShareDataPartner')
          AND c.name = N'ConnectionType';

        IF @curPartnerColType <> N'int'
        BEGIN
            PRINT N'  -> Phát hiện cột ConnectionType kiểu ' + @curPartnerColType + N', đang chuẩn hóa dữ liệu sang số nguyên (1-Api, 2-File, 3-Ftp, 4-Ftps, 5-Kafka)...';

            UPDATE [dbo].[ShareDataPartner]
            SET [ConnectionType] = CASE
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'API', N'REST', N'HTTP', N'WEBAPI', N'1') THEN 1
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FILE', N'DISK', N'NAS', N'2') THEN 2
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FTP', N'3') THEN 3
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FTPS', N'4') THEN 4
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'KAFKA', N'5') THEN 5
                ELSE NULL
            END;

            PRINT N'  -> Đang chuyển kiểu cột ConnectionType sang INT NULL...';
            ALTER TABLE [dbo].[ShareDataPartner] ALTER COLUMN [ConnectionType] INT NULL;
            PRINT N'  -> Đã di trú cột ConnectionType sang INT NULL thành công.';
        END
        ELSE
        BEGIN
            PRINT N'  -> Cột ConnectionType trên ShareDataPartner đã là kiểu INT, bỏ qua.';
        END
    END
END
GO

IF DB_ID('test') IS NOT NULL
BEGIN
    PRINT N'  -> Phát hiện CSDL [test], kiểm tra và di trú ConnectionType trên [test].[dbo].[ShareDataPartner]...';
    EXEC('
        USE [test];
        IF OBJECT_ID(N''dbo.ShareDataPartner'', N''U'') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N''dbo.ShareDataPartner'', N''ConnectionType'') IS NULL
            BEGIN
                PRINT N''    -> [test] Cột ConnectionType chưa tồn tại trên ShareDataPartner (sẽ được CodeFirst tạo kiểu INT), bỏ qua.'';
            END
            ELSE
            BEGIN
                DECLARE @curTestType NVARCHAR(128);
                SELECT @curTestType = tp.name
                FROM sys.columns c
                JOIN sys.types tp ON c.user_type_id = tp.user_type_id
                WHERE c.object_id = OBJECT_ID(N''dbo.ShareDataPartner'')
                  AND c.name = N''ConnectionType'';

                IF @curTestType <> N''int''
                BEGIN
                    PRINT N''    -> [test] Đang chuẩn hóa dữ liệu ConnectionType sang số nguyên...'';
                    UPDATE [dbo].[ShareDataPartner]
                    SET [ConnectionType] = CASE
                        WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N''API'', N''REST'', N''HTTP'', N''WEBAPI'', N''1'') THEN 1
                        WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N''FILE'', N''DISK'', N''NAS'', N''2'') THEN 2
                        WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N''FTP'', N''3'') THEN 3
                        WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N''FTPS'', N''4'') THEN 4
                        WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N''KAFKA'', N''5'') THEN 5
                        ELSE NULL
                    END;
                    ALTER TABLE [dbo].[ShareDataPartner] ALTER COLUMN [ConnectionType] INT NULL;
                    PRINT N''    -> [test] Đã di trú cột ConnectionType sang INT NULL thành công.'';
                END
            END
        END
    ');
END
GO

PRINT N'=============================================================================';
PRINT N'HOÀN TẤT KỊCH BẢN DI TRÚ MỘT LẦN CSDL SHAREDATA!';
PRINT N'=============================================================================';
