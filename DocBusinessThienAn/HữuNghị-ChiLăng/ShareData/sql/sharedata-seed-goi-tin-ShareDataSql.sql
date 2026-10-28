USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-goi-tin-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Nạp câu SQL trích xuất mẫu chung (dòng PartnerId trống) cho gói 101 - 111 vào ShareDataSql:
--              0.3 Chuyển câu SQL cũ (ShareDataPacket.ExtractSql/CursorTimeColumn, bảng ShareDataPartnerSql)
--                  sang ShareDataSql; chạy trước bước 1 để giữ câu SQL đã sửa, không bị câu mẫu ghi đè.
--              1. Câu mẫu chung (@SqlNNN viết ngay trong tệp này, nguồn duy nhất) khi đang trống hoặc @GhiDeMauGoiTin = 1.
--              5. Chuyển câu SQL riêng cũ ở ShareDataMapping.ExtractSql sang ShareDataSql; đổi __rowid -> __lastKey.
--              0.4 Bộ biến của câu SQL ở cột VariablesJson: chuyển từ bảng cũ ShareDataSqlVariable (bỏ bảng 27/10/2026).
--              6. Khối câu ghi chiều nhận (WriteSql, AffectedTables) ở dòng mẫu chung: chuyển từ bảng cũ ShareDataPacketSql,
--                 rồi nạp khối chuẩn Catalog (101 - 109) cho gói chưa có câu ghi. Gộp tệp ShareDataPacketSql cũ (27/10/2026).
-- THỨ TỰ CHẠY: 2 / 3 (sau sharedata-seed-goi-tin-ShareDataPacket.sql, vì cần bản ghi gói) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 27/10/2026 (gộp câu ghi chiều nhận vào ShareDataSql; câu SQL mẫu chung viết ngay trong tệp)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-goi-tin-ShareDataSql.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataSql]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSql] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua kịch bản.';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- 0.3 CHUYỂN CÂU SQL CŨ SANG BẢNG ShareDataSql (IDEMPOTENT)
-- -----------------------------------------------------------------------------------------
-- Câu SQL mẫu chung và thiết lập SQL trước đây nằm ở ShareDataPacket (ExtractSql, CursorTimeColumn),
-- câu SQL riêng của đối tác nằm ở bảng ShareDataPartnerSql. Chạy trước bước 1 để giữ câu SQL đã sửa, không bị câu mẫu ghi đè.
-- CodeFirst mới không còn tạo các cột, bảng cũ nên chỉ chạy khi còn tồn tại và dùng SQL động để không lỗi biên dịch.
PRINT N'-> 0.3. Chuyển câu SQL cũ (ShareDataPacket, ShareDataPartnerSql) sang bảng ShareDataSql...';

DECLARE @TenantIdMove NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdMove IS NULL
    SET @TenantIdMove = N'Default';

DECLARE @MovedCommon INT = 0;
DECLARE @MovedPartner INT = 0;

IF COL_LENGTH('dbo.ShareDataPacket', 'ExtractSql') IS NOT NULL
   AND COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NOT NULL
BEGIN
    DECLARE @MoveCommonSql NVARCHAR(MAX) = N'
    INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
    SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[ExtractSql], p.[CursorTimeColumn], @TenantId, GETDATE(), ''SHAREDATA-SEED-02''
    FROM [dbo].[ShareDataPacket] p
    WHERE p.[IsDelete] IS NULL
      AND ((p.[ExtractSql] IS NOT NULL AND LTRIM(RTRIM(p.[ExtractSql])) <> '''') OR p.[CursorTimeColumn] IS NOT NULL)
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[ShareDataSql] s
          WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);';
    EXEC sp_executesql @MoveCommonSql, N'@TenantId NVARCHAR(64)', @TenantId = @TenantIdMove;
    SET @MovedCommon = @@ROWCOUNT;
END;

IF OBJECT_ID('dbo.ShareDataPartnerSql', 'U') IS NOT NULL
BEGIN
    DECLARE @MovePartnerSql NVARCHAR(MAX) = N'
    INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [TenantId], [CreateTime], [CreateUId])
    SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), ps.[PacketId], ps.[PartnerId], ps.[ExtractSql], @TenantId, GETDATE(), ''SHAREDATA-SEED-02''
    FROM [dbo].[ShareDataPartnerSql] ps
    WHERE ps.[IsDelete] IS NULL
      AND ps.[PartnerId] IS NOT NULL AND ps.[PacketId] IS NOT NULL
      AND ps.[ExtractSql] IS NOT NULL AND LTRIM(RTRIM(ps.[ExtractSql])) <> ''''
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[ShareDataSql] s
          WHERE s.[PacketId] = ps.[PacketId] AND s.[PartnerId] = ps.[PartnerId] AND s.[IsDelete] IS NULL);';
    EXEC sp_executesql @MovePartnerSql, N'@TenantId NVARCHAR(64)', @TenantId = @TenantIdMove;
    SET @MovedPartner = @@ROWCOUNT;
END;

PRINT N'   -> Đã chuyển sang ShareDataSql: ' + CAST(@MovedCommon AS NVARCHAR(10)) + N' câu mẫu chung, ' + CAST(@MovedPartner AS NVARCHAR(10)) + N' câu SQL riêng của đối tác.';
GO

-- 0.4 Chuyển bộ biến từ bảng cũ ShareDataSqlVariable (nếu còn) sang cột VariablesJson của dòng ShareDataSql cùng (gói, đối tác).
--     Chỉ điền dòng có VariablesJson IS NULL; dòng đối tác chưa có thì tạo dòng chỉ chứa bộ biến. Dùng SQL động vì bảng cũ có thể không còn.
DECLARE @MovedVars INT = 0;
IF OBJECT_ID('dbo.ShareDataSqlVariable', 'U') IS NOT NULL
BEGIN
    DECLARE @TenantIdVars NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
    IF @TenantIdVars IS NULL
        SET @TenantIdVars = N'Default';
    DECLARE @MoveVarsSql NVARCHAR(MAX) = N'
    ;WITH g AS (
        SELECT DISTINCT v.[PacketId], v.[PartnerId]
        FROM [dbo].[ShareDataSqlVariable] v
        WHERE v.[IsDelete] IS NULL AND v.[PacketId] IS NOT NULL AND v.[Code] IS NOT NULL AND v.[Code] <> ''''
    ), j AS (
        SELECT g.[PacketId], g.[PartnerId],
               (SELECT v.[Code], v.[Name], v.[DataType], v.[ValueKind], v.[Value], v.[DefaultValue], v.[Remark], v.[OrderNo]
                FROM [dbo].[ShareDataSqlVariable] v
                WHERE v.[IsDelete] IS NULL AND v.[PacketId] = g.[PacketId] AND ISNULL(v.[PartnerId], '''') = ISNULL(g.[PartnerId], '''')
                  AND v.[Code] IS NOT NULL AND v.[Code] <> ''''
                ORDER BY v.[OrderNo]
                FOR JSON PATH, INCLUDE_NULL_VALUES) AS [VariablesJson]
        FROM g
    )
    UPDATE s SET s.[VariablesJson] = j.[VariablesJson], s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSql] s
    JOIN j ON j.[PacketId] = s.[PacketId] AND ISNULL(j.[PartnerId], '''') = ISNULL(s.[PartnerId], '''')
    WHERE s.[IsDelete] IS NULL AND s.[VariablesJson] IS NULL;
    SET @Moved = @@ROWCOUNT;

    ;WITH g AS (
        SELECT DISTINCT v.[PacketId], v.[PartnerId]
        FROM [dbo].[ShareDataSqlVariable] v
        WHERE v.[IsDelete] IS NULL AND v.[PacketId] IS NOT NULL AND v.[Code] IS NOT NULL AND v.[Code] <> ''''
    )
    INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [VariablesJson], [TenantId], [CreateTime], [CreateUId])
    SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), g.[PacketId], g.[PartnerId],
           (SELECT v.[Code], v.[Name], v.[DataType], v.[ValueKind], v.[Value], v.[DefaultValue], v.[Remark], v.[OrderNo]
            FROM [dbo].[ShareDataSqlVariable] v
            WHERE v.[IsDelete] IS NULL AND v.[PacketId] = g.[PacketId] AND ISNULL(v.[PartnerId], '''') = ISNULL(g.[PartnerId], '''')
              AND v.[Code] IS NOT NULL AND v.[Code] <> ''''
            ORDER BY v.[OrderNo]
            FOR JSON PATH, INCLUDE_NULL_VALUES),
           @TenantId, GETDATE(), ''SHAREDATA-SEED-02''
    FROM g
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataSql] s
        WHERE s.[IsDelete] IS NULL AND s.[PacketId] = g.[PacketId] AND ISNULL(s.[PartnerId], '''') = ISNULL(g.[PartnerId], ''''));
    SET @Moved = @Moved + @@ROWCOUNT;';
    EXEC sp_executesql @MoveVarsSql, N'@TenantId NVARCHAR(64), @Moved INT OUTPUT', @TenantId = @TenantIdVars, @Moved = @MovedVars OUTPUT;
END;
PRINT N'   -> Đã chuyển bộ biến từ bảng cũ ShareDataSqlVariable sang VariablesJson: ' + CAST(@MovedVars AS NVARCHAR(10)) + N' dòng.';
GO

-- -----------------------------------------------------------------------------------------
-- 1. NẠP CÂU MẪU CHUNG (ShareDataSql) CHO GÓI 101 - 111
-- -----------------------------------------------------------------------------------------
-- @GhiDeMauGoiTin = 0 (Mặc định): Chỉ nạp câu mẫu chung nếu đang NULL hoặc rỗng.
-- @GhiDeMauGoiTin = 1: Buộc ghi đè câu mẫu chung về câu chuẩn @SqlNNN khai trong tệp này.
DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @UpdatedRows INT = 0;
DECLARE @TotalUpdated INT = 0;
DECLARE @TenantIdSql NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdSql IS NULL
    SET @TenantIdSql = N'Default';

PRINT N'-> 1. Bắt đầu nạp câu mẫu chung vào bảng ShareDataSql (101 - 111)...';

-- Gói 101: TrafficFlow (Incremental)
DECLARE @Sql101 NVARCHAR(MAX) = N'SELECT TOP (@pageSize)
    CONVERT(varchar(23), zs.UpdateTime, 121) + ''_'' + CAST(zs.ID AS varchar(64)) AS __lastKey,
    zs.ID AS zoneStatusId,
    zs.ZoneId AS zoneId,
    z.Name AS zoneName,
    z.FromKmNumber AS fromLocationKm,
    z.FromMetNumber AS fromLocationMet,
    z.ToKmNumber AS toLocationKm,
    z.ToMetNumber AS toLocationMet,
    z.LaneId AS laneId,
    CAST(zs.AverageSpeed AS DECIMAL(18, 2)) AS averageSpeed,
    zs.Condition AS trafficCondition,
    zs.UpdateTime AS dataTime,
    z.MaxSpeed AS speedLimit,
    ts.TotalVehicleNumber AS vehicleCount
FROM dbo.TmsZoneStatus zs
LEFT JOIN dbo.TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN dbo.TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId
WHERE (CONVERT(varchar(23), zs.UpdateTime, 121) + ''_'' + CAST(zs.ID AS varchar(64))) > @lastKey
ORDER BY __lastKey ASC';

DECLARE @Vars101 NVARCHAR(MAX) = N'[{"Code":"pageSize","Name":null,"DataType":"Int","ValueKind":"Const","Value":"100","DefaultValue":null,"Remark":null,"OrderNo":1},{"Code":"lastKey","Name":null,"DataType":"String","ValueKind":"Builtin","Value":"lastKey","DefaultValue":"","Remark":null,"OrderNo":2}]';

UPDATE s
SET s.[ExtractSql] = @Sql101,
    s.[CursorTimeColumn] = NULL,
    s.[VariablesJson] = ISNULL(s.[VariablesJson], @Vars101),
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '101' OR p.[Code] LIKE '101[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [VariablesJson], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql101, NULL, @Vars101, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '101' OR p.[Code] LIKE '101[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 101 (TrafficFlow): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 102: CctvImage (Snapshot)
DECLARE @Sql102 NVARCHAR(MAX) = N'SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip';

UPDATE s
SET s.[ExtractSql] = @Sql102,
    s.[CursorTimeColumn] = NULL,
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '102' OR p.[Code] LIKE '102[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql102, NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '102' OR p.[Code] LIKE '102[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 102 (CctvImage): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 103: VehicleDetection (Incremental)
DECLARE @Sql103 NVARCHAR(MAX) = N'SELECT TOP 100
    td.ID AS detectionId,
    td.DetectTime AS detectTime,
    td.Type AS vehicleType,
    td.LicensePlate AS licensePlate,
    td.Speed AS speed,
    td.Lane AS lane,
    td.Direction AS direction,
    td.Location AS locationRoute,
    td.EquipmentId AS equipmentId,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey,
    td.UpdateTime AS __auditUpdate,
    td.CreateTime AS __auditCreate
FROM TmsTrafficData td
LEFT JOIN TmsEquipment e ON td.EquipmentId = e.ID
WHERE td.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
      OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
  )
ORDER BY COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) ASC, td.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql103,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '103' OR p.[Code] LIKE '103[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql103, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '103' OR p.[Code] LIKE '103[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 103 (VehicleDetection): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 104: Weather (Incremental)
DECLARE @Sql104 NVARCHAR(MAX) = N'SELECT TOP 100
    w.RefId AS weatherStationId,
    w.LocationDetail AS locationDetail,
    w.Temperature AS temperature,
    w.Hudmidity AS humidity,
    w.WindSpeed AS windSpeed,
    w.WindDirection AS windDirection,
    w.Rain AS rainfall,
    w.RainHour AS rainfallHour,
    w.Foresight AS visibility,
    w.Description AS weatherDescription,
    w.ShortDescription AS weatherCode,
    w.TimeDetect AS detectTime,
    COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) AS __lastTime,
    w.ID AS __lastKey,
    w.UpdateTime AS __auditUpdate,
    w.CreateTime AS __auditCreate
FROM TmsWeather w
WHERE w.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) > @lastTime
      OR (COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) = @lastTime AND w.ID > @lastKey)
  )
ORDER BY COALESCE(w.UpdateTime, w.CreateTime, w.TimeDetect) ASC, w.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql104,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '104' OR p.[Code] LIKE '104[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql104, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '104' OR p.[Code] LIKE '104[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 104 (Weather): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 105: VehicleIdentification (Snapshot)
DECLARE @Sql105 NVARCHAR(MAX) = N'SELECT
    i.TransactionId AS transactionId,
    i.TagId AS tagId,
    NULLIF(ISNULL(NULLIF(i.PlateEdit, ''''), NULLIF(i.PlateLpr, '''')), '''') AS licensePlate,
    i.VehicleTypeId AS vehicleTypeId,
    i.TransactionDateTime AS entryTime,
    CAST(NULL AS DATETIME) AS exitTime,
    i.LaneId AS laneId,
    i.StationId AS stationId,
    vr.Brand AS vehicleBrand,
    vr.Owner AS vehicleOwner
FROM TollTransactionIn i
LEFT JOIN TmsVehicleRegistration vr
       ON NULLIF(ISNULL(NULLIF(i.PlateEdit, ''''), NULLIF(i.PlateLpr, '''')), '''') = vr.LicensePlate
      AND vr.IsDelete IS NULL
WHERE i.IsDelete IS NULL
ORDER BY i.TransactionDateTime DESC';

UPDATE s
SET s.[ExtractSql] = @Sql105,
    s.[CursorTimeColumn] = NULL,
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '105' OR p.[Code] LIKE '105[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql105, NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '105' OR p.[Code] LIKE '105[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 105 (VehicleIdentification): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 106: WeighInMotion (Incremental)
DECLARE @Sql106 NVARCHAR(MAX) = N'SELECT TOP 100
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __lastKey,
    td.UpdateTime AS __auditUpdate,
    td.CreateTime AS __auditCreate
FROM TmsTrafficData td
WHERE td.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) > @lastTime
      OR (COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) = @lastTime AND td.ID > @lastKey)
  )
ORDER BY COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) ASC, td.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql106,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '106' OR p.[Code] LIKE '106[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql106, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '106' OR p.[Code] LIKE '106[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 106 (WeighInMotion): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 107: TrafficIncident (Incremental)
DECLARE @Sql107 NVARCHAR(MAX) = N'SELECT TOP 100
    i.Code AS incidentCode,
    i.Name AS incidentName,
    i.EventTypeId AS eventTypeId,
    et.Name AS eventTypeName,
    i.StartDate AS occurredTime,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.Location AS locationRoute,
    i.InfluenceScope AS direction,
    i.InjuredNumber AS injuredCount,
    i.VehicleNumber AS vehicleCount,
    i.State AS incidentState,
    i.Description AS description,
    i.Source AS source,
    COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) AS __lastTime,
    i.ID AS __lastKey,
    i.UpdateTime AS __auditUpdate,
    i.CreateTime AS __auditCreate
FROM TmsIncident i
LEFT JOIN TmsEventType et ON i.EventTypeId = et.ID
WHERE i.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) > @lastTime
      OR (COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) = @lastTime AND i.ID > @lastKey)
  )
ORDER BY COALESCE(i.UpdateTime, i.CreateTime, i.StartDate) ASC, i.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql107,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '107' OR p.[Code] LIKE '107[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql107, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '107' OR p.[Code] LIKE '107[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 107 (TrafficIncident): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 108: VmsDisplay (Snapshot)
DECLARE @Sql108 NVARCHAR(MAX) = N'SELECT
    e.Code AS equipmentCode,
    v.Name AS vmsName,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction,
    e.LaneId AS laneId,
    v.RowData AS displayContent,
    v.Url AS displayImageUrl,
    v.Size AS displaySize,
    v.Priority AS priority,
    v.ExecutedDate AS executedTime
FROM VmsCurrent v
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID';

UPDATE s
SET s.[ExtractSql] = @Sql108,
    s.[CursorTimeColumn] = NULL,
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '108' OR p.[Code] LIKE '108[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql108, NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '108' OR p.[Code] LIKE '108[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 108 (VmsDisplay): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 109: TollCollection (Incremental)
DECLARE @Sql109 NVARCHAR(MAX) = N'SELECT TOP 100
    t.TransactionId AS transactionId,
    t.TransactionDateTimeIn AS entryTime,
    t.TransactionDateTime AS exitTime,
    t.VehicleTypeId AS vehicleTypeId,
    ISNULL(t.PlateEdit, t.PlateLpr) AS licensePlate,
    t.TagId AS tagId,
    t.LaneId AS laneId,
    l.Name AS laneName,
    t.StationId AS stationId,
    s.Name AS stationName,
    CAST(NULL AS DECIMAL(18, 2)) AS tollPrice,
    t.SyncTime AS syncTime,
    COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) AS __lastTime,
    t.ID AS __lastKey,
    t.UpdateTime AS __auditUpdate,
    t.CreateTime AS __auditCreate
FROM TollTransactionOut t
LEFT JOIN TollLane l ON t.LaneId = l.LaneId
LEFT JOIN TollStation s ON t.StationId = s.StationId
WHERE t.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) > @lastTime
      OR (COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) = @lastTime AND t.ID > @lastKey)
  )
ORDER BY COALESCE(t.UpdateTime, t.CreateTime, t.TransactionDateTime) ASC, t.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql109,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '109' OR p.[Code] LIKE '109[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql109, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '109' OR p.[Code] LIKE '109[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 109 (TollCollection): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 110: PublicMessaging (Snapshot)
DECLARE @Sql110 NVARCHAR(MAX) = N'SELECT
    CAST(i.ID AS NVARCHAR(64)) AS messageId,
    CONCAT(ISNULL(i.Name, ''''), '' - '', ISNULL(i.Description, '''')) AS incidentMessage,
    w.Description AS weatherMessage,
    v.RowData AS guidanceContent,
    i.KmNumber AS locationKm,
    i.MetNumber AS locationMet,
    i.StartDate AS publishedTime,
    CAST(NULL AS NVARCHAR(64)) AS channel,
    CAST(NULL AS NVARCHAR(64)) AS deliveryState
FROM TmsIncident i
OUTER APPLY (
    SELECT TOP 1 e.ID
    FROM TmsEquipment e
    WHERE e.KmNumber = i.KmNumber
    ORDER BY e.ID DESC
) eq
OUTER APPLY (
    SELECT TOP 1 v.RowData
    FROM VmsCurrent v
    WHERE v.EquipmentId = eq.ID
      AND v.RowData IS NOT NULL
    ORDER BY v.ExecutedDate DESC
) v
OUTER APPLY (
    SELECT TOP 1 w.Description
    FROM TmsWeather w
    WHERE w.Description IS NOT NULL
    ORDER BY COALESCE(w.TimeDetect, w.CreateTime) DESC
) w
WHERE (i.State IS NULL OR (
    i.State != ''FINISHED'' AND i.State != ''CANCELED''
    AND i.State != ''Closed'' AND i.State != ''Cancelled''
))';

UPDATE s
SET s.[ExtractSql] = @Sql110,
    s.[CursorTimeColumn] = NULL,
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '110' OR p.[Code] LIKE '110[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql110, NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '110' OR p.[Code] LIKE '110[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 110 (PublicMessaging): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 111: InterCenter (Incremental, Status = 0: Disable)
DECLARE @Sql111 NVARCHAR(MAX) = N'SELECT TOP 100
    CAST(NULL AS NVARCHAR(16)) AS packetType,
    CAST(NULL AS NVARCHAR(MAX)) AS payload,
    GETDATE() AS createdTime,
    sl.NewData AS controlCommand,
    sl.State AS controlState,
    CAST(NULL AS NVARCHAR(64)) AS connectionState,
    CAST(NULL AS DATETIME) AS ackTime,
    CAST(NULL AS INT) AS retryCount,
    CAST(NULL AS NVARCHAR(512)) AS error,
    CAST(NULL AS NVARCHAR(64)) AS partnerId,
    COALESCE(sl.UpdateTime, sl.CreateTime) AS __lastTime,
    sl.ID AS __lastKey,
    sl.UpdateTime AS __auditUpdate,
    sl.CreateTime AS __auditCreate
FROM TmsSignalLog sl
WHERE sl.IsDelete IS NULL
  AND (
      @lastTime IS NULL
      OR COALESCE(sl.UpdateTime, sl.CreateTime) > @lastTime
      OR (COALESCE(sl.UpdateTime, sl.CreateTime) = @lastTime AND sl.ID > @lastKey)
  )
ORDER BY COALESCE(sl.UpdateTime, sl.CreateTime) ASC, sl.ID ASC
OPTION (RECOMPILE)';

UPDATE s
SET s.[ExtractSql] = @Sql111,
    s.[CursorTimeColumn] = N'UpdateTime',
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '111' OR p.[Code] LIKE '111[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, @Sql111, N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '111' OR p.[Code] LIKE '111[_]%')
  AND p.[IsDelete] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 111 (InterCenter): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

PRINT N'   -> Tổng số bản ghi câu mẫu chung đã cập nhật: ' + CAST(@TotalUpdated AS NVARCHAR(10)) + N' dòng.';
GO

-- -----------------------------------------------------------------------------------------
-- 5. CHUYỂN SQL RIÊNG CŨ VÀ ĐỔI KHÓA CON TRỎ __rowid -> __lastKey (IDEMPOTENT)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 5. Chuyển SQL riêng cũ và đổi cột con trỏ __rowid -> __lastKey trong ShareDataSql...';

DECLARE @TenantIdPacketSql NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdPacketSql IS NULL
    SET @TenantIdPacketSql = N'Default';

DECLARE @PartnerSqlMigrated INT = 0;
DECLARE @PartnerSqlRenamed INT = 0;

-- Chuyển câu SQL riêng cũ (cột ExtractSql của ShareDataMapping) sang bảng ShareDataSql (dòng có PartnerId).
-- CodeFirst mới không còn tạo cột cũ nên chỉ chạy khi cột còn tồn tại và dùng SQL động để không lỗi biên dịch.
-- Mỗi cặp (đối tác × gói) lấy hồ sơ ánh xạ chiều gửi (Outbound = 0, Both = 2) mới nhất có câu riêng; không ghi đè dòng đã có.
IF OBJECT_ID('dbo.ShareDataSql', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.ShareDataMapping', 'ExtractSql') IS NOT NULL
BEGIN
    DECLARE @MigrateSql NVARCHAR(MAX) = N'
    INSERT INTO [dbo].[ShareDataSql] ([ID], [PartnerId], [PacketId], [ExtractSql], [TenantId], [CreateTime], [CreateUId])
    SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), x.[PartnerId], x.[PacketId], x.[ExtractSql], @TenantId, GETDATE(), ''SHAREDATA-SEED-02''
    FROM (
        SELECT m.[PartnerId], p.[ID] AS [PacketId], m.[ExtractSql],
               ROW_NUMBER() OVER (PARTITION BY m.[PartnerId], p.[ID] ORDER BY ISNULL(m.[UpdateTime], m.[CreateTime]) DESC) AS rn
        FROM [dbo].[ShareDataMapping] m
        JOIN [dbo].[ShareDataPacket] p ON p.[IsDelete] IS NULL AND (p.[ID] = m.[DatatypeId] OR p.[Code] = m.[DatatypeId])
        WHERE m.[IsDelete] IS NULL
          AND m.[PartnerId] IS NOT NULL
          AND m.[ExtractSql] IS NOT NULL AND LTRIM(RTRIM(m.[ExtractSql])) <> ''''
          AND (m.[Direction] = 0 OR m.[Direction] = 2)
    ) x
    WHERE x.rn = 1
      AND NOT EXISTS (
          SELECT 1 FROM [dbo].[ShareDataSql] s
          WHERE s.[PartnerId] = x.[PartnerId] AND s.[PacketId] = x.[PacketId] AND s.[IsDelete] IS NULL);';
    EXEC sp_executesql @MigrateSql, N'@TenantId NVARCHAR(64)', @TenantId = @TenantIdPacketSql;
    SET @PartnerSqlMigrated = @@ROWCOUNT;
END;

-- Cập nhật ExtractSql trên ShareDataSql (câu mẫu chung và câu riêng của đối tác)
IF OBJECT_ID('dbo.ShareDataSql', 'U') IS NOT NULL
BEGIN
    UPDATE [dbo].[ShareDataSql]
    SET [ExtractSql] = REPLACE([ExtractSql], '__rowid', '__lastKey')
    WHERE [IsDelete] IS NULL
      AND [ExtractSql] LIKE '%[_][_]rowid%';
    SET @PartnerSqlRenamed = @@ROWCOUNT;
END;

PRINT N'   -> Đã chuyển đổi __rowid -> __lastKey: ' + CAST(@PartnerSqlRenamed AS NVARCHAR(10)) + N' câu SQL ở ShareDataSql.';
PRINT N'   -> Đã chuyển câu SQL riêng cũ từ ShareDataMapping sang ShareDataSql: ' + CAST(@PartnerSqlMigrated AS NVARCHAR(10)) + N' dòng.';

GO

-- -----------------------------------------------------------------------------------------
-- 6. KHỐI CÂU GHI CHIỀU NHẬN Ở DÒNG MẪU CHUNG (WriteSql, AffectedTables) — gộp từ bảng cũ ShareDataPacketSql
-- -----------------------------------------------------------------------------------------
-- Câu gửi (ExtractSql) và câu ghi chiều nhận (WriteSql) của gói nằm chung một dòng mẫu chung (PartnerId trống).
PRINT N'-> 6. Gộp khối câu ghi chiều nhận vào dòng mẫu chung ShareDataSql...';

-- 6.2 Chuyển câu ghi đang dùng ở bảng cũ ShareDataPacketSql (nếu còn) sang dòng mẫu chung; chạy trước 6.3 để giữ câu ghi đã sửa tay.
--     Bảng cũ có thể nhiều dòng một gói (OrderNo): nối theo OrderNo thành một khối. Chỉ ghi vào dòng chưa có câu ghi.
DECLARE @TenantIdWrite NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdWrite IS NULL
    SET @TenantIdWrite = N'Default';
DECLARE @MovedWrite INT = 0;

IF OBJECT_ID('dbo.ShareDataPacketSql', 'U') IS NOT NULL
BEGIN
    DECLARE @MoveWriteSql NVARCHAR(MAX) = N'
    ;WITH w AS (
        SELECT p.[ID] AS [PacketId],
               STRING_AGG(CAST(ps.[WriteSql] AS NVARCHAR(MAX)), CHAR(10) + CHAR(10)) WITHIN GROUP (ORDER BY ps.[OrderNo]) AS [WriteSql],
               STRING_AGG(CAST(COALESCE(NULLIF(ps.[AffectedTables], ''''), ps.[TableName]) AS NVARCHAR(MAX)), '','') WITHIN GROUP (ORDER BY ps.[OrderNo]) AS [AffectedTables],
               MAX(ps.[Remark]) AS [Remark]
        FROM [dbo].[ShareDataPacketSql] ps
        JOIN [dbo].[ShareDataPacket] p ON p.[IsDelete] IS NULL AND p.[Code] = ps.[PacketCode]
        WHERE ps.[IsDelete] IS NULL
          AND (ps.[Status] IS NULL OR ps.[Status] = 1)
          AND ps.[WriteSql] IS NOT NULL AND LTRIM(RTRIM(ps.[WriteSql])) <> ''''
        GROUP BY p.[ID]
    )
    UPDATE s
    SET s.[WriteSql] = w.[WriteSql],
        s.[AffectedTables] = LEFT(w.[AffectedTables], 1024),
        s.[WriteRemark] = w.[Remark],
        s.[UpdateTime] = GETDATE()
    FROM [dbo].[ShareDataSql] s
    JOIN w ON w.[PacketId] = s.[PacketId]
    WHERE s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL
      AND (s.[WriteSql] IS NULL OR LTRIM(RTRIM(s.[WriteSql])) = '''');
    SET @Moved = @@ROWCOUNT;

    ;WITH w AS (
        SELECT p.[ID] AS [PacketId],
               STRING_AGG(CAST(ps.[WriteSql] AS NVARCHAR(MAX)), CHAR(10) + CHAR(10)) WITHIN GROUP (ORDER BY ps.[OrderNo]) AS [WriteSql],
               STRING_AGG(CAST(COALESCE(NULLIF(ps.[AffectedTables], ''''), ps.[TableName]) AS NVARCHAR(MAX)), '','') WITHIN GROUP (ORDER BY ps.[OrderNo]) AS [AffectedTables],
               MAX(ps.[Remark]) AS [Remark]
        FROM [dbo].[ShareDataPacketSql] ps
        JOIN [dbo].[ShareDataPacket] p ON p.[IsDelete] IS NULL AND p.[Code] = ps.[PacketCode]
        WHERE ps.[IsDelete] IS NULL
          AND (ps.[Status] IS NULL OR ps.[Status] = 1)
          AND ps.[WriteSql] IS NOT NULL AND LTRIM(RTRIM(ps.[WriteSql])) <> ''''
        GROUP BY p.[ID]
    )
    INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [WriteSql], [AffectedTables], [WriteRemark], [TenantId], [CreateTime], [CreateUId])
    SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), w.[PacketId], NULL, w.[WriteSql], LEFT(w.[AffectedTables], 1024), w.[Remark], @TenantId, GETDATE(), ''SHAREDATA-SEED-02''
    FROM w
    WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = w.[PacketId] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
    SET @Moved = @Moved + @@ROWCOUNT;';
    EXEC sp_executesql @MoveWriteSql, N'@TenantId NVARCHAR(64), @Moved INT OUTPUT', @TenantId = @TenantIdWrite, @Moved = @MovedWrite OUTPUT;
END;
PRINT N'   -> Đã chuyển câu ghi từ bảng cũ ShareDataPacketSql: ' + CAST(@MovedWrite AS NVARCHAR(10)) + N' gói.';
GO

-- 6.3 Nạp khối câu ghi chuẩn Catalog cho gói chưa có câu ghi (hoặc ghi đè khi @GhiDeMauGoiTin = 1).
DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @TenantIdWrite NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdWrite IS NULL
    SET @TenantIdWrite = N'Default';

DECLARE @SeedWrites TABLE (
    [PacketCode]     NVARCHAR(64)   NOT NULL,
    [AffectedTables] NVARCHAR(1024) NOT NULL,
    [WriteSql]       NVARCHAR(MAX)  NOT NULL
);

-- Gói 101: CommonData (3 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('101_commonData', N'dbo.TmsZone, dbo.TmsTrafficStatistic, dbo.TmsZoneStatus', N'MERGE dbo.TmsZone WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneId,
            MAX(zoneName)        AS zoneName,
            MAX(fromLocationKm)  AS fromLocationKm,
            MAX(fromLocationMet) AS fromLocationMet,
            MAX(toLocationKm)    AS toLocationKm,
            MAX(toLocationMet)   AS toLocationMet,
            MAX(laneId)          AS laneId,
            MAX(speedLimit)      AS speedLimit
    FROM OPENJSON(@records) WITH (
            zoneId          nvarchar(64),
            zoneName        nvarchar(64),
            fromLocationKm  int,
            fromLocationMet int,
            toLocationKm    int,
            toLocationMet   int,
            laneId          nvarchar(32),
            speedLimit      float)
    WHERE zoneId IS NOT NULL
    GROUP BY zoneId
) AS s
   ON t.ID = s.zoneId
WHEN MATCHED THEN
    UPDATE SET t.Name          = ISNULL(s.zoneName, t.Name),
               t.FromKmNumber  = ISNULL(s.fromLocationKm, t.FromKmNumber),
               t.FromMetNumber = ISNULL(s.fromLocationMet, t.FromMetNumber),
               t.ToKmNumber    = ISNULL(s.toLocationKm, t.ToKmNumber),
               t.ToMetNumber   = ISNULL(s.toLocationMet, t.ToMetNumber),
               t.LaneId        = ISNULL(s.laneId, t.LaneId),
               t.MaxSpeed      = ISNULL(s.speedLimit, t.MaxSpeed),
               t.UpdateTime    = @now,
               t.UpdateUId     = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Name, FromKmNumber, FromMetNumber, ToKmNumber, ToMetNumber, LaneId, MaxSpeed,
            TenantId, CreateTime, CreateUId)
    VALUES (s.zoneId, s.zoneName, s.fromLocationKm, s.fromLocationMet, s.toLocationKm, s.toLocationMet,
            s.laneId, s.speedLimit, @tenantId, @now, @operator)
OUTPUT $action;

MERGE dbo.TmsTrafficStatistic WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneId,
            MAX(vehicleCount) AS vehicleCount
    FROM OPENJSON(@records) WITH (
            zoneId       nvarchar(64),
            vehicleCount int)
    WHERE zoneId IS NOT NULL AND vehicleCount IS NOT NULL
    GROUP BY zoneId
) AS s
   ON t.ZoneId = s.zoneId
WHEN MATCHED THEN
    UPDATE SET t.TotalVehicleNumber = s.vehicleCount,
               t.UpdateTime         = @now,
               t.UpdateUId          = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, ZoneId, TotalVehicleNumber, VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.zoneId, s.vehicleCount, 0, @tenantId, @now, @operator)
OUTPUT $action;

MERGE dbo.TmsZoneStatus WITH (HOLDLOCK) AS t
USING (
    SELECT  zoneStatusId,
            MAX(zoneId)           AS zoneId,
            MAX(averageSpeed)     AS averageSpeed,
            MAX(trafficCondition) AS trafficCondition,
            MAX(dataTime)         AS dataTime
    FROM OPENJSON(@records) WITH (
            zoneStatusId     nvarchar(64),
            zoneId           nvarchar(64),
            averageSpeed     decimal(18, 2),
            trafficCondition nvarchar(64),
            dataTime         datetime2)
    WHERE zoneStatusId IS NOT NULL
    GROUP BY zoneStatusId
) AS s
   ON t.ID = s.zoneStatusId
WHEN MATCHED THEN
    UPDATE SET t.ZoneId       = ISNULL(s.zoneId, t.ZoneId),
               t.AverageSpeed = ISNULL(CONVERT(nvarchar(16), s.averageSpeed), t.AverageSpeed),
               t.Condition    = ISNULL(s.trafficCondition, t.Condition),
               t.UpdateTime   = ISNULL(s.dataTime, @now),
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, ZoneId, AverageSpeed, Condition, UpdateTime, TenantId, CreateTime, CreateUId)
    VALUES (s.zoneStatusId, s.zoneId, CONVERT(nvarchar(16), s.averageSpeed), s.trafficCondition,
            ISNULL(s.dataTime, @now), @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 102: CCTV (2 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('102_cctvData', N'dbo.TmsEquipment, dbo.CctvDevice', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT cameraCode, MAX(cameraName) AS cameraName, MAX(locationKm) AS locationKm,
            MAX(locationMet) AS locationMet, MAX(direction) AS direction
    FROM OPENJSON(@records) WITH (
        cameraCode nvarchar(64), cameraName nvarchar(64),
        locationKm int, locationMet int, direction int)
    WHERE cameraCode IS NOT NULL
    GROUP BY cameraCode
) AS s
   ON t.Code = s.cameraCode
WHEN MATCHED THEN
    UPDATE SET t.KmNumber    = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber   = ISNULL(s.locationMet, t.MetNumber),
               t.DirectionId = ISNULL(s.direction, t.DirectionId),
               t.UpdateTime  = @now,
               t.UpdateUId   = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, KmNumber, MetNumber, DirectionId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.cameraCode, s.cameraName, s.locationKm, s.locationMet,
            s.direction, @tenantId, @now, @operator)
OUTPUT $action;

MERGE dbo.CctvDevice WITH (HOLDLOCK) AS t
USING (
    SELECT cameraCode, MAX(cameraName) AS cameraName, MAX(snapshotTime) AS snapshotTime,
            MAX(deviceState) AS deviceState
    FROM OPENJSON(@records) WITH (
        cameraCode nvarchar(64), cameraName nvarchar(64),
        snapshotTime datetime2, deviceState int)
    WHERE cameraCode IS NOT NULL
    GROUP BY cameraCode
) AS s
   ON t.Code = s.cameraCode
WHEN MATCHED THEN
    UPDATE SET t.Name         = ISNULL(s.cameraName, t.Name),
               t.SnapshotTime = ISNULL(s.snapshotTime, t.SnapshotTime),
               t.DeviceState  = ISNULL(s.deviceState, t.DeviceState),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, DeviceId, Name, SnapshotTime, DeviceState, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.cameraCode, s.cameraCode, s.cameraName, s.snapshotTime,
            s.deviceState, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 103: VDS (2 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('103_vdsData', N'dbo.TmsEquipment, dbo.TmsTrafficData', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT equipmentId, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet
    FROM OPENJSON(@records) WITH (equipmentId nvarchar(64), locationKm int, locationMet int)
    WHERE equipmentId IS NOT NULL
    GROUP BY equipmentId
) AS s
   ON t.ID = s.equipmentId
WHEN MATCHED THEN
    UPDATE SET t.KmNumber   = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber  = ISNULL(s.locationMet, t.MetNumber),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;

MERGE dbo.TmsTrafficData WITH (HOLDLOCK) AS t
USING (
    SELECT detectionId, MAX(detectTime) AS detectTime, MAX(vehicleType) AS vehicleType,
            MAX(licensePlate) AS licensePlate, MAX(speed) AS speed, MAX(lane) AS lane,
            MAX(direction) AS direction, MAX(locationRoute) AS locationRoute, MAX(equipmentId) AS equipmentId
    FROM OPENJSON(@records) WITH (
        detectionId nvarchar(64), detectTime datetime2, vehicleType nvarchar(32),
        licensePlate nvarchar(32), speed float, lane nvarchar(32),
        direction nvarchar(32), locationRoute nvarchar(32), equipmentId nvarchar(64))
    WHERE detectionId IS NOT NULL
    GROUP BY detectionId
) AS s
   ON t.ID = s.detectionId
WHEN MATCHED THEN
    UPDATE SET t.DetectTime   = ISNULL(s.detectTime, t.DetectTime),
               t.Type         = ISNULL(s.vehicleType, t.Type),
               t.LicensePlate = ISNULL(s.licensePlate, t.LicensePlate),
               t.Speed        = ISNULL(s.speed, t.Speed),
               t.Lane         = ISNULL(s.lane, t.Lane),
               t.Direction    = ISNULL(s.direction, t.Direction),
               t.Location     = ISNULL(s.locationRoute, t.Location),
               t.EquipmentId  = ISNULL(s.equipmentId, t.EquipmentId),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, DetectTime, Type, LicensePlate, Speed, Lane, Direction, Location, EquipmentId,
            VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (s.detectionId, s.detectTime, s.vehicleType, s.licensePlate, s.speed, s.lane, s.direction,
            s.locationRoute, s.equipmentId, 0, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 104: Weather (1 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('104_weatherData', N'dbo.TmsWeather', N'MERGE dbo.TmsWeather WITH (HOLDLOCK) AS t
USING (
    SELECT weatherStationId, detectTime,
            MAX(locationDetail) AS locationDetail, MAX(temperature) AS temperature, MAX(humidity) AS humidity,
            MAX(windSpeed) AS windSpeed, MAX(windDirection) AS windDirection, MAX(rainfall) AS rainfall,
            MAX(rainfallHour) AS rainfallHour, MAX(visibility) AS visibility,
            MAX(weatherDescription) AS weatherDescription, MAX(weatherCode) AS weatherCode
    FROM OPENJSON(@records) WITH (
        weatherStationId nvarchar(64), detectTime datetime2, locationDetail nvarchar(128),
        temperature float, humidity float, windSpeed float, windDirection nvarchar(32),
        rainfall float, rainfallHour float, visibility float,
        weatherDescription nvarchar(128), weatherCode nvarchar(32))
    WHERE weatherStationId IS NOT NULL AND detectTime IS NOT NULL
    GROUP BY weatherStationId, detectTime
) AS s
   ON t.RefId = s.weatherStationId AND t.TimeDetect = s.detectTime
WHEN MATCHED THEN
    UPDATE SET t.LocationDetail   = ISNULL(s.locationDetail, t.LocationDetail),
               t.Temperature      = ISNULL(s.temperature, t.Temperature),
               t.Hudmidity        = ISNULL(s.humidity, t.Hudmidity),
               t.WindSpeed        = ISNULL(s.windSpeed, t.WindSpeed),
               t.WindDirection    = ISNULL(s.windDirection, t.WindDirection),
               t.Rain             = ISNULL(s.rainfall, t.Rain),
               t.RainHour         = ISNULL(s.rainfallHour, t.RainHour),
               t.Foresight        = ISNULL(s.visibility, t.Foresight),
               t.Description      = ISNULL(s.weatherDescription, t.Description),
               t.ShortDescription = ISNULL(s.weatherCode, t.ShortDescription),
               t.UpdateTime       = @now,
               t.UpdateUId        = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, RefId, TimeDetect, LocationDetail, Temperature, Hudmidity, WindSpeed, WindDirection,
            Rain, RainHour, Foresight, Description, ShortDescription, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.weatherStationId, s.detectTime, s.locationDetail, s.temperature,
            s.humidity, s.windSpeed, s.windDirection, s.rainfall, s.rainfallHour, s.visibility,
            s.weatherDescription, s.weatherCode, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 105: RfidData (2 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('105_rfidData', N'dbo.TmsVehicleRegistration, dbo.TollTransactionOut', N'MERGE dbo.TmsVehicleRegistration WITH (HOLDLOCK) AS t
USING (
    SELECT licensePlate, MAX(vehicleBrand) AS vehicleBrand, MAX(vehicleOwner) AS vehicleOwner
    FROM OPENJSON(@records) WITH (licensePlate nvarchar(32), vehicleBrand nvarchar(64), vehicleOwner nvarchar(64))
    WHERE licensePlate IS NOT NULL AND (vehicleBrand IS NOT NULL OR vehicleOwner IS NOT NULL)
    GROUP BY licensePlate
) AS s
   ON t.LicensePlate = s.licensePlate
WHEN MATCHED THEN
    UPDATE SET t.Brand      = ISNULL(s.vehicleBrand, t.Brand),
               t.Owner      = ISNULL(s.vehicleOwner, t.Owner),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;

MERGE dbo.TollTransactionOut WITH (HOLDLOCK) AS t
USING (
    SELECT transactionId, MAX(tagId) AS tagId, MAX(licensePlate) AS licensePlate,
            MAX(vehicleTypeId) AS vehicleTypeId, MAX(entryTime) AS entryTime, MAX(exitTime) AS exitTime,
            MAX(laneId) AS laneId, MAX(stationId) AS stationId
    FROM OPENJSON(@records) WITH (
        transactionId nvarchar(128), tagId nvarchar(64), licensePlate nvarchar(32),
        vehicleTypeId nvarchar(64), entryTime datetime2, exitTime datetime2,
        laneId nvarchar(32), stationId nvarchar(32))
    WHERE transactionId IS NOT NULL
    GROUP BY transactionId
) AS s
   ON t.TransactionId = s.transactionId
WHEN MATCHED THEN
    UPDATE SET t.TagId                 = ISNULL(s.tagId, t.TagId),
               t.Plate                 = ISNULL(s.licensePlate, t.Plate),
               t.PlateEdit             = ISNULL(s.licensePlate, t.PlateEdit),
               t.VehicleTypeId         = ISNULL(s.vehicleTypeId, t.VehicleTypeId),
               t.TransactionDateTimeIn = ISNULL(s.entryTime, t.TransactionDateTimeIn),
               t.TransactionDateTime   = ISNULL(s.exitTime, t.TransactionDateTime),
               t.LaneId                = ISNULL(s.laneId, t.LaneId),
               t.StationId             = ISNULL(s.stationId, t.StationId),
               t.UpdateTime            = @now,
               t.UpdateUId             = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, TransactionId, TagId, Plate, PlateEdit, VehicleTypeId, TransactionDateTimeIn,
            TransactionDateTime, LaneId, StationId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.transactionId, s.tagId, s.licensePlate, s.licensePlate,
            s.vehicleTypeId, s.entryTime, s.exitTime, s.laneId, s.stationId, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 106: WimData (1 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('106_wimData', N'dbo.TmsTrafficData', N'MERGE dbo.TmsTrafficData WITH (HOLDLOCK) AS t
USING (
    SELECT detectTime, lane, locationCode,
            MAX(speed) AS speed, MAX(height) AS height, MAX(width) AS width, MAX(length) AS length
    FROM OPENJSON(@records) WITH (
        detectTime datetime2, lane nvarchar(32), locationCode nvarchar(32),
        speed float, height int, width int, length int)
    WHERE detectTime IS NOT NULL
    GROUP BY detectTime, lane, locationCode
) AS s
   ON t.DetectTime = s.detectTime
  AND ISNULL(t.Lane, N'''') = ISNULL(s.lane, N'''')
  AND ISNULL(t.Location, N'''') = ISNULL(s.locationCode, N'''')
WHEN MATCHED THEN
    UPDATE SET t.Speed      = ISNULL(s.speed, t.Speed),
               t.Height     = ISNULL(s.height, t.Height),
               t.Width      = ISNULL(s.width, t.Width),
               t.Length     = ISNULL(s.length, t.Length),
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, DetectTime, Lane, Location, Speed, Height, Width, Length,
            VendorEventId, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.detectTime, s.lane, s.locationCode, s.speed, s.height,
            s.width, s.length, 0, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 107: IncidentData (2 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('107_incidentData', N'dbo.TmsEventType, dbo.TmsIncident', N'MERGE dbo.TmsEventType WITH (HOLDLOCK) AS t
USING (
    SELECT eventTypeId, MAX(eventTypeName) AS eventTypeName
    FROM OPENJSON(@records) WITH (eventTypeId nvarchar(64), eventTypeName nvarchar(64))
    WHERE eventTypeId IS NOT NULL AND eventTypeName IS NOT NULL
    GROUP BY eventTypeId
) AS s
   ON t.ID = s.eventTypeId
WHEN MATCHED THEN
    UPDATE SET t.Name       = s.eventTypeName,
               t.UpdateTime = @now,
               t.UpdateUId  = @operator
OUTPUT $action;

MERGE dbo.TmsIncident WITH (HOLDLOCK) AS t
USING (
    SELECT incidentCode, MAX(incidentName) AS incidentName, MAX(eventTypeId) AS eventTypeId,
            MAX(occurredTime) AS occurredTime, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet,
            MAX(locationRoute) AS locationRoute, MAX(direction) AS direction,
            MAX(injuredCount) AS injuredCount, MAX(vehicleCount) AS vehicleCount,
            MAX(incidentState) AS incidentState, MAX(description) AS description, MAX(source) AS source
    FROM OPENJSON(@records) WITH (
        incidentCode nvarchar(64), incidentName nvarchar(128), eventTypeId nvarchar(64),
        occurredTime datetime2, locationKm int, locationMet int, locationRoute nvarchar(32),
        direction nvarchar(32), injuredCount int, vehicleCount int, incidentState nvarchar(32),
        description nvarchar(512), source nvarchar(32))
    WHERE incidentCode IS NOT NULL
    GROUP BY incidentCode
) AS s
   ON t.Code = s.incidentCode
WHEN MATCHED THEN
    UPDATE SET t.Name           = ISNULL(s.incidentName, t.Name),
               t.EventTypeId    = ISNULL(s.eventTypeId, t.EventTypeId),
               t.StartDate      = ISNULL(s.occurredTime, t.StartDate),
               t.KmNumber       = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber      = ISNULL(s.locationMet, t.MetNumber),
               t.Location       = ISNULL(s.locationRoute, t.Location),
               t.InfluenceScope = ISNULL(s.direction, t.InfluenceScope),
               t.InjuredNumber  = ISNULL(s.injuredCount, t.InjuredNumber),
               t.VehicleNumber  = ISNULL(s.vehicleCount, t.VehicleNumber),
               t.State          = ISNULL(s.incidentState, t.State),
               t.Description    = ISNULL(s.description, t.Description),
               t.Source         = ISNULL(s.source, t.Source),
               t.UpdateTime     = @now,
               t.UpdateUId      = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, EventTypeId, StartDate, KmNumber, MetNumber, Location, InfluenceScope,
            InjuredNumber, VehicleNumber, State, Description, Source, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.incidentCode, s.incidentName, s.eventTypeId, s.occurredTime,
            s.locationKm, s.locationMet, s.locationRoute, s.direction, s.injuredCount, s.vehicleCount,
            s.incidentState, s.description, s.source, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 108: VmsInfo (2 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('108_vmsInfo', N'dbo.TmsEquipment, dbo.VmsCurrent', N'MERGE dbo.TmsEquipment WITH (HOLDLOCK) AS t
USING (
    SELECT equipmentCode, MAX(locationKm) AS locationKm, MAX(locationMet) AS locationMet,
            MAX(direction) AS direction, MAX(laneId) AS laneId
    FROM OPENJSON(@records) WITH (
        equipmentCode nvarchar(64), locationKm int, locationMet int, direction int, laneId nvarchar(32))
    WHERE equipmentCode IS NOT NULL
    GROUP BY equipmentCode
) AS s
   ON t.Code = s.equipmentCode
WHEN MATCHED THEN
    UPDATE SET t.KmNumber    = ISNULL(s.locationKm, t.KmNumber),
               t.MetNumber   = ISNULL(s.locationMet, t.MetNumber),
               t.DirectionId = ISNULL(s.direction, t.DirectionId),
               t.LaneId      = ISNULL(s.laneId, t.LaneId),
               t.UpdateTime  = @now,
               t.UpdateUId   = @operator
OUTPUT $action;

MERGE dbo.VmsCurrent WITH (HOLDLOCK) AS t
USING (
    SELECT e.ID AS equipmentId, MAX(r.vmsName) AS vmsName, MAX(r.displayContent) AS displayContent,
            MAX(r.displayImageUrl) AS displayImageUrl, MAX(r.displaySize) AS displaySize,
            MAX(r.priority) AS priority, MAX(r.executedTime) AS executedTime
    FROM OPENJSON(@records) WITH (
        equipmentCode nvarchar(64), vmsName nvarchar(64), displayContent nvarchar(1024),
        displayImageUrl nvarchar(512), displaySize nvarchar(32), priority int, executedTime datetime2) AS r
    INNER JOIN dbo.TmsEquipment e ON e.Code = r.equipmentCode
    GROUP BY e.ID
) AS s
   ON t.EquipmentId = s.equipmentId
WHEN MATCHED THEN
    UPDATE SET t.Name         = ISNULL(s.vmsName, t.Name),
               t.RowData      = s.displayContent,
               t.Url          = s.displayImageUrl,
               t.Size         = ISNULL(s.displaySize, t.Size),
               t.Priority     = ISNULL(s.priority, t.Priority),
               t.ExecutedDate = ISNULL(s.executedTime, t.ExecutedDate),
               t.UpdateTime   = @now,
               t.UpdateUId    = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, EquipmentId, Name, RowData, Url, Size, Priority, ExecutedDate, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.equipmentId, s.vmsName, s.displayContent, s.displayImageUrl,
            s.displaySize, s.priority, s.executedTime, @tenantId, @now, @operator)
OUTPUT $action;');

-- Gói 109: EtcData (3 bảng)
INSERT INTO @SeedWrites ([PacketCode], [AffectedTables], [WriteSql])
VALUES ('109_etcData', N'dbo.TollStation, dbo.TollLane, dbo.TollTransactionOut', N'MERGE dbo.TollStation WITH (HOLDLOCK) AS t
USING (
    SELECT stationId, MAX(stationName) AS stationName
    FROM OPENJSON(@records) WITH (stationId nvarchar(64), stationName nvarchar(64))
    WHERE stationId IS NOT NULL AND stationName IS NOT NULL
    GROUP BY stationId
) AS s
   ON t.StationId = s.stationId
WHEN MATCHED THEN
    UPDATE SET t.Name = s.stationName, t.UpdateTime = @now, t.UpdateUId = @operator
OUTPUT $action;

MERGE dbo.TollLane WITH (HOLDLOCK) AS t
USING (
    SELECT laneId, MAX(laneName) AS laneName
    FROM OPENJSON(@records) WITH (laneId nvarchar(64), laneName nvarchar(64))
    WHERE laneId IS NOT NULL AND laneName IS NOT NULL
    GROUP BY laneId
) AS s
   ON t.LaneId = s.laneId
WHEN MATCHED THEN
    UPDATE SET t.Name = s.laneName, t.UpdateTime = @now, t.UpdateUId = @operator
OUTPUT $action;

MERGE dbo.TollTransactionOut WITH (HOLDLOCK) AS t
USING (
    SELECT transactionId, MAX(entryTime) AS entryTime, MAX(exitTime) AS exitTime,
            MAX(vehicleTypeId) AS vehicleTypeId, MAX(licensePlate) AS licensePlate, MAX(tagId) AS tagId,
            MAX(laneId) AS laneId, MAX(stationId) AS stationId, MAX(tollPrice) AS tollPrice,
            MAX(syncTime) AS syncTime
    FROM OPENJSON(@records) WITH (
        transactionId nvarchar(128), entryTime datetime2, exitTime datetime2,
        vehicleTypeId nvarchar(64), licensePlate nvarchar(32), tagId nvarchar(64),
        laneId nvarchar(32), stationId nvarchar(32), tollPrice decimal(18, 2), syncTime datetime2)
    WHERE transactionId IS NOT NULL
    GROUP BY transactionId
) AS s
   ON t.TransactionId = s.transactionId
WHEN MATCHED THEN
    UPDATE SET t.TransactionDateTimeIn = ISNULL(s.entryTime, t.TransactionDateTimeIn),
               t.TransactionDateTime   = ISNULL(s.exitTime, t.TransactionDateTime),
               t.VehicleTypeId         = ISNULL(s.vehicleTypeId, t.VehicleTypeId),
               t.Plate                 = ISNULL(s.licensePlate, t.Plate),
               t.PlateEdit             = ISNULL(s.licensePlate, t.PlateEdit),
               t.TagId                 = ISNULL(s.tagId, t.TagId),
               t.LaneId                = ISNULL(s.laneId, t.LaneId),
               t.StationId             = ISNULL(s.stationId, t.StationId),
               t.Price                 = ISNULL(s.tollPrice, t.Price),
               t.SyncTime              = ISNULL(s.syncTime, t.SyncTime),
               t.UpdateTime            = @now,
               t.UpdateUId             = @operator
WHEN NOT MATCHED THEN
    INSERT (ID, TransactionId, TransactionDateTimeIn, TransactionDateTime, VehicleTypeId, Plate, PlateEdit,
            TagId, LaneId, StationId, Price, SyncTime, TenantId, CreateTime, CreateUId)
    VALUES (CONVERT(nvarchar(64), NEWID()), s.transactionId, s.entryTime, s.exitTime, s.vehicleTypeId,
            s.licensePlate, s.licensePlate, s.tagId, s.laneId, s.stationId, s.tollPrice, s.syncTime,
            @tenantId, @now, @operator)
OUTPUT $action;');

UPDATE s
SET s.[WriteSql] = src.[WriteSql],
    s.[AffectedTables] = src.[AffectedTables],
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId] AND p.[IsDelete] IS NULL
JOIN @SeedWrites src ON src.[PacketCode] = p.[Code]
WHERE s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[WriteSql] IS NULL OR LTRIM(RTRIM(s.[WriteSql])) = '');
DECLARE @WriteUpdated INT = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [WriteSql], [AffectedTables], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, src.[WriteSql], src.[AffectedTables], @TenantIdWrite, GETDATE(), 'SHAREDATA-SEED-02'
FROM @SeedWrites src
JOIN [dbo].[ShareDataPacket] p ON p.[Code] = src.[PacketCode] AND p.[IsDelete] IS NULL
WHERE NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @WriteUpdated = @WriteUpdated + @@ROWCOUNT;
PRINT N'   -> Đã nạp khối câu ghi chuẩn Catalog: ' + CAST(@WriteUpdated AS NVARCHAR(10)) + N' gói.';

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-goi-tin-ShareDataSql.sql';
PRINT N'=============================================================================';
GO
