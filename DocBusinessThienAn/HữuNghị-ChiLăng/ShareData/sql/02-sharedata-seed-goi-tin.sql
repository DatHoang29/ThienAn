USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 02-sharedata-seed-goi-tin.sql
-- DESCRIPTION: Nạp Template câu truy vấn SQL trích xuất chuẩn Catalog (gói 101-110),
--              chuẩn hóa chế độ gửi (SyncMode) và trạng thái gói (Status) trong ShareDataPacket:
--              1. Nạp câu truy vấn SQL trích xuất mẫu chuẩn cho các gói tin 101 - 110:
--                 - Sử dụng biến @GhiDeMauGoiTin BIT = 0 (mặc định không ghi đè câu lệnh SQL mà
--                   SuperAdmin/quản trị viên đã chỉnh sửa trên màn hình quản trị).
--                 - Khi @GhiDeMauGoiTin = 1: Cho phép ghi đè toàn bộ đưa về template Catalog chuẩn.
--              2. Chuẩn hóa SyncMode mặc định theo nghiệp vụ cho các gói còn NULL:
--                 - Snapshot (1): 101, 102, 105, 108
--                 - Incremental (2): 103, 104, 106, 107, 109
--              3. Cập nhật Status = 0 (Disable) cho các gói 110* và 111* do tạm hoãn/chưa hoàn thiện.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, không ghi đè dữ liệu quản trị đã sửa).
-- CẬP NHẬT LẦN CUỐI: 16102026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN SEED GÓI TIN SHAREDATA (02-sharedata-seed-goi-tin.sql)';
PRINT N'=============================================================================';

-- -----------------------------------------------------------------------------------------
-- 0. CẤU HÌNH GHI ĐÈ TEMPLATE
-- -----------------------------------------------------------------------------------------
-- @GhiDeMauGoiTin = 0 (Mặc định): Chỉ nạp template nếu ExtractSql đang NULL hoặc rỗng.
-- @GhiDeMauGoiTin = 1: Buộc ghi đè toàn bộ câu truy vấn về mẫu chuẩn Catalog.
DECLARE @GhiDeMauGoiTin BIT = 0;

IF @GhiDeMauGoiTin = 1
    PRINT N'⚠️ CHẾ ĐỘ: GHI ĐÈ TOÀN BỘ (@GhiDeMauGoiTin = 1) - Các câu lệnh trích xuất sẽ được đặt lại theo Catalog!';
ELSE
    PRINT N'ℹ️ CHẾ ĐỘ: AN TOÀN (@GhiDeMauGoiTin = 0) - Chỉ nạp mẫu cho gói chưa có ExtractSql, giữ nguyên cấu hình đã sửa.';

-- 1. ĐẢM BẢO CÁC CỘT CẤU HÌNH TRUY VẤN TỒN TẠI TRÊN ShareDataPacket
IF COL_LENGTH('dbo.ShareDataPacket', 'ExtractSql') IS NULL
    ALTER TABLE [dbo].[ShareDataPacket] ADD [ExtractSql] NVARCHAR(MAX) NULL;

IF COL_LENGTH('dbo.ShareDataPacket', 'SyncMode') IS NULL
    ALTER TABLE [dbo].[ShareDataPacket] ADD [SyncMode] INT NULL; -- 1 = Snapshot, 2 = Incremental

IF COL_LENGTH('dbo.ShareDataPacket', 'CursorTimeColumn') IS NULL
    ALTER TABLE [dbo].[ShareDataPacket] ADD [CursorTimeColumn] NVARCHAR(128) NULL;

IF COL_LENGTH('dbo.ShareDataPacket', 'MaxLookBackDays') IS NULL
    ALTER TABLE [dbo].[ShareDataPacket] ADD [MaxLookBackDays] INT NULL;
GO

DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @UpdatedRows INT = 0;
DECLARE @TotalUpdated INT = 0;

PRINT N'-> 1. Bắt đầu nạp Template câu lệnh SQL trích xuất vào bảng ShareDataPacket...';

-- Gói 101: TrafficFlow (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT
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
FROM TmsZoneStatus zs
LEFT JOIN TmsZone z ON zs.ZoneId = z.ID
LEFT JOIN TmsTrafficStatistic ts ON zs.ZoneId = ts.ZoneId',
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '101' OR [Code] LIKE '101_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 101 (TrafficFlow): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 102: CctvImage (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT
    e.Code AS cameraCode,
    c.Name AS cameraName,
    NULL AS snapshot,
    c.SnapshotTime AS snapshotTime,
    c.DeviceState AS deviceState,
    e.KmNumber AS locationKm,
    e.MetNumber AS locationMet,
    e.DirectionId AS direction
FROM CctvDevice c
LEFT JOIN TmsEquipment e ON c.Ip = e.Ip',
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '102' OR [Code] LIKE '102_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 102 (CctvImage): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 103: VehicleDetection (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT TOP (@pageSize)
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
    td.ID AS __rowid,
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
OPTION (RECOMPILE)',
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '103' OR [Code] LIKE '103_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 103 (VehicleDetection): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 104: Weather (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT TOP (@pageSize)
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
    w.ID AS __rowid,
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
OPTION (RECOMPILE)',
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '104' OR [Code] LIKE '104_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 104 (Weather): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 105: VehicleIdentification (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT
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
ORDER BY i.TransactionDateTime DESC',
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '105' OR [Code] LIKE '105_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 105 (VehicleIdentification): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 106: WeighInMotion (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT TOP (@pageSize)
    td.DetectTime AS detectTime,
    td.Lane AS lane,
    td.Location AS locationCode,
    td.Speed AS speed,
    td.Height AS height,
    td.Width AS width,
    td.Length AS length,
    COALESCE(td.UpdateTime, td.CreateTime, td.DetectTime) AS __lastTime,
    td.ID AS __rowid,
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
OPTION (RECOMPILE)',
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '106' OR [Code] LIKE '106_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 106 (WeighInMotion): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 107: TrafficIncident (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT TOP (@pageSize)
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
    i.ID AS __rowid,
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
OPTION (RECOMPILE)',
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '107' OR [Code] LIKE '107_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 107 (TrafficIncident): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 108: VmsDisplay (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT
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
LEFT JOIN TmsEquipment e ON v.EquipmentId = e.ID',
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '108' OR [Code] LIKE '108_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 108 (VmsDisplay): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 109: TollCollection (Incremental)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT TOP (@pageSize)
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
    t.ID AS __rowid,
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
OPTION (RECOMPILE)',
    [SyncMode] = 2,
    [CursorTimeColumn] = N'UpdateTime',
    [MaxLookBackDays] = 1
WHERE ([Code] = '109' OR [Code] LIKE '109_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 109 (TollCollection): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 110: PublicMessaging (Snapshot)
UPDATE [dbo].[ShareDataPacket]
SET [ExtractSql] = N'SELECT
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
))',
    [SyncMode] = 1,
    [CursorTimeColumn] = NULL,
    [MaxLookBackDays] = NULL
WHERE ([Code] = '110' OR [Code] LIKE '110_%')
  AND (@GhiDeMauGoiTin = 1 OR [ExtractSql] IS NULL OR LTRIM(RTRIM([ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 110 (PublicMessaging): đã cập nhật ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

PRINT N'   -> Tổng số bản ghi template SQL đã cập nhật: ' + CAST(@TotalUpdated AS NVARCHAR(10)) + N' dòng.';

-- -----------------------------------------------------------------------------------------
-- 2. CHUẨN HÓA SyncMode MẶC ĐỊNH CHO CÁC GÓI CÒN NULL
-- -----------------------------------------------------------------------------------------
PRINT N'-> 2. Chuẩn hóa SyncMode mặc định khi còn NULL...';

-- Gói Snapshot (101, 102, 105, 108, 110)
UPDATE [dbo].[ShareDataPacket]
SET [SyncMode] = 1
WHERE [SyncMode] IS NULL
  AND ([Code] IN ('101', '102', '105', '108', '110')
       OR [Code] LIKE '101[_]%'
       OR [Code] LIKE '102[_]%'
       OR [Code] LIKE '105[_]%'
       OR [Code] LIKE '108[_]%'
       OR [Code] LIKE '110[_]%');
PRINT N'   - Cập nhật SyncMode = 1 (Snapshot) cho các gói 101, 102, 105, 108, 110 còn NULL: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- Gói Incremental (103, 104, 106, 107, 109)
UPDATE [dbo].[ShareDataPacket]
SET [SyncMode] = 2
WHERE [SyncMode] IS NULL
  AND ([Code] IN ('103', '104', '106', '107', '109')
       OR [Code] LIKE '103[_]%'
       OR [Code] LIKE '104[_]%'
       OR [Code] LIKE '106[_]%'
       OR [Code] LIKE '107[_]%'
       OR [Code] LIKE '109[_]%');
PRINT N'   - Cập nhật SyncMode = 2 (Incremental) cho các gói 103, 104, 106, 107, 109 còn NULL: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- -----------------------------------------------------------------------------------------
-- 3. CHUẨN HÓA TRẠNG THÁI GÓI (Status = 1 CHO 110*, Status = 0 CHO 111*)
-- -----------------------------------------------------------------------------------------
PRINT N'-> 3. Chuẩn hóa trạng thái gói tin (Bật 110*, Tắt 111*)...';

-- Bật gói 110* (Enable = 1) khi đang NULL hoặc 0 do cấu hình cũ
UPDATE [dbo].[ShareDataPacket]
SET [Status] = 1
WHERE ([Status] IS NULL OR [Status] = 0)
  AND ([Code] = '110' OR [Code] LIKE '110[_]%');
PRINT N'   - Cập nhật Status = 1 (Enable) cho các gói 110*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

-- Giữ gói 111* (Disable = 0) do chưa hoàn thiện
UPDATE [dbo].[ShareDataPacket]
SET [Status] = 0
WHERE ([Status] IS NULL OR [Status] <> 0)
  AND ([Code] = '111' OR [Code] LIKE '111[_]%');
PRINT N'   - Cập nhật Status = 0 (Disable) cho các gói 111*: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED GÓI TIN SHAREDATA (02-sharedata-seed-goi-tin.sql)';
PRINT N'=============================================================================';
GO
