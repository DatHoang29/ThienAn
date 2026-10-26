USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-goi-tin-ShareDataSql.sql
-- BẢNG GHI: [dbo].[ShareDataSql] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Nạp câu SQL trích xuất mẫu chung (dòng PartnerId trống) cho gói 101 - 111 vào ShareDataSql:
--              0.3 Chuyển câu SQL cũ (ShareDataPacket.ExtractSql/CursorTimeColumn, bảng ShareDataPartnerSql)
--                  sang ShareDataSql; chạy trước bước 1 để giữ câu SQL đã sửa, không bị câu mẫu ghi đè.
--              1. Câu mẫu chung = TemplateSql của gói khi đang trống hoặc @GhiDeMauGoiTin = 1.
--              5. Chuyển câu SQL riêng cũ ở ShareDataMapping.ExtractSql sang ShareDataSql; đổi __rowid -> __lastKey.
-- THỨ TỰ CHẠY: 2 / 3 (sau sharedata-seed-goi-tin-ShareDataPacket.sql, vì đọc TemplateSql của gói) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 26/10/2026 (bỏ cột MaxLookBackDays)
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

IF OBJECT_ID('dbo.ShareDataSql', 'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataSql] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua bước chuyển dữ liệu.';
    RETURN;
END;

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

-- -----------------------------------------------------------------------------------------
-- 1. NẠP CÂU MẪU CHUNG (ShareDataSql) THEO TemplateSql CHO GÓI 101 - 111
-- -----------------------------------------------------------------------------------------
-- @GhiDeMauGoiTin = 0 (Mặc định): Chỉ nạp câu mẫu chung nếu đang NULL hoặc rỗng.
-- @GhiDeMauGoiTin = 1: Buộc ghi đè câu mẫu chung về TemplateSql của gói.
DECLARE @GhiDeMauGoiTin BIT = 0;
DECLARE @UpdatedRows INT = 0;
DECLARE @TotalUpdated INT = 0;
DECLARE @TenantIdSql NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdSql IS NULL
    SET @TenantIdSql = N'Default';

PRINT N'-> 1. Bắt đầu nạp câu mẫu chung vào bảng ShareDataSql (101 - 111)...';

-- Gói 101: TrafficFlow (Snapshot)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
    s.[CursorTimeColumn] = NULL,
    s.[UpdateTime] = GETDATE()
FROM [dbo].[ShareDataSql] s
JOIN [dbo].[ShareDataPacket] p ON p.[ID] = s.[PacketId]
WHERE (p.[Code] = '101' OR p.[Code] LIKE '101[_]%')
  AND p.[IsDelete] IS NULL
  AND s.[PartnerId] IS NULL
  AND s.[IsDelete] IS NULL
  AND (@GhiDeMauGoiTin = 1 OR s.[ExtractSql] IS NULL OR LTRIM(RTRIM(s.[ExtractSql])) = '');
SET @UpdatedRows = @@ROWCOUNT;

INSERT INTO [dbo].[ShareDataSql] ([ID], [PacketId], [PartnerId], [ExtractSql], [CursorTimeColumn], [TenantId], [CreateTime], [CreateUId])
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '101' OR p.[Code] LIKE '101[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 101 (TrafficFlow): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 102: CctvImage (Snapshot)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '102' OR p.[Code] LIKE '102[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 102 (CctvImage): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 103: VehicleDetection (Incremental)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '103' OR p.[Code] LIKE '103[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 103 (VehicleDetection): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 104: Weather (Incremental)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '104' OR p.[Code] LIKE '104[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 104 (Weather): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 105: VehicleIdentification (Snapshot)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '105' OR p.[Code] LIKE '105[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 105 (VehicleIdentification): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 106: WeighInMotion (Incremental)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '106' OR p.[Code] LIKE '106[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 106 (WeighInMotion): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 107: TrafficIncident (Incremental)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '107' OR p.[Code] LIKE '107[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 107 (TrafficIncident): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 108: VmsDisplay (Snapshot)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '108' OR p.[Code] LIKE '108[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 108 (VmsDisplay): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 109: TollCollection (Incremental)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '109' OR p.[Code] LIKE '109[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 109 (TollCollection): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 110: PublicMessaging (Snapshot)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], NULL, @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '110' OR p.[Code] LIKE '110[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSql] s WHERE s.[PacketId] = p.[ID] AND s.[PartnerId] IS NULL AND s.[IsDelete] IS NULL);
SET @UpdatedRows = @UpdatedRows + @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @UpdatedRows;
PRINT N'   - Gói 110 (PublicMessaging): đã nạp câu mẫu chung ' + CAST(@UpdatedRows AS NVARCHAR(10)) + N' bản ghi.';

-- Gói 111: InterCenter (Incremental, Status = 0: Disable)
UPDATE s
SET s.[ExtractSql] = p.[TemplateSql],
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
SELECT LOWER(CONVERT(NVARCHAR(64), NEWID())), p.[ID], NULL, p.[TemplateSql], N'UpdateTime', @TenantIdSql, GETDATE(), 'SHAREDATA-SEED-02'
FROM [dbo].[ShareDataPacket] p
WHERE (p.[Code] = '111' OR p.[Code] LIKE '111[_]%')
  AND p.[IsDelete] IS NULL
  AND p.[TemplateSql] IS NOT NULL
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

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-goi-tin-ShareDataSql.sql';
PRINT N'=============================================================================';
GO
