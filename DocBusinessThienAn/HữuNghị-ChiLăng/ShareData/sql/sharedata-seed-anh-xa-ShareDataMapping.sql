USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-seed-anh-xa-ShareDataMapping.sql
-- BẢNG GHI: [dbo].[ShareDataMapping] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Hồ sơ ánh xạ gói 101 cho 2 đối tác hiện tại (chốt 06/11/2026):
--                PC08  gửi đi  (Direction = 0) : PC08_101COMMONDATA_OUT
--                PC08  nhận về (Direction = 1) : PC08_101COMMONDATA_IN
--                TTDHT nhận về (Direction = 1) : TTDHT_101COMMONDATA_IN
--              1. Xóa mềm mọi hồ sơ đang sống khác của PC08, TTDHT (giống nút Xóa: ghi IsDelete).
--              2. Thêm 3 hồ sơ trên nếu chưa có (đang bật). Mã hồ sơ theo cách màn hình tự sinh: ĐỐI TÁC_GÓI TIN_CHIỀU.
--              3. Áp dụng Bộ mã Chuẩn hóa (tệp 15 sharedata-seed-bo-ma-ShareDataCodeSet.sql) cho cả 2 chiều Gửi đi / Nhận về:
--                   - laneId           -> bộ mã lane_code          (0..3 <-> ALL_LANES, LANE_1..LANE_3)
--                   - trafficCondition -> bộ mã traffic_condition  (GOOD/NORMAL/SLOW/CONGES <-> FREE_FLOW/NORMAL/SLOW/CONGESTED)
--                 Cập nhật tự động cho 3 hồ sơ mẫu đã tạo trước đó nếu quản trị viên chưa chỉnh sửa tay trên màn hình.
-- THỨ TỰ CHẠY: 16 (sau tệp 2 câu SQL mẫu, tệp 14 đối tác và tệp 15 bộ mã chuẩn hóa ShareDataCodeSet) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường (đổi tên CSDL ở dòng USE đầu tệp nếu chạy trên DEV_ITS10 local).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 06/11/2026 (áp dụng bộ mã chuẩn hóa lane_code và traffic_condition cho 3 hồ sơ của PC08, TTDHT)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-anh-xa-ShareDataMapping.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataMapping]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataMapping] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp hồ sơ ánh xạ.';
    SET NOEXEC ON;
END;
GO

IF OBJECT_ID(N'[dbo].[ShareDataCodeSet]', N'U') IS NULL
   OR (SELECT COUNT(DISTINCT [Code])
       FROM [dbo].[ShareDataCodeSet]
       WHERE [IsDelete] IS NULL
         AND [Status] = 1
         AND [Code] IN (N'lane_code', N'traffic_condition')) < 2
BEGIN
    PRINT N'⚠️ Chưa có đủ bộ mã chuẩn hóa đang bật (lane_code, traffic_condition) trong [dbo].[ShareDataCodeSet]!';
    PRINT N'   Vui lòng chạy tệp 15 sharedata-seed-bo-ma-ShareDataCodeSet.sql trước rồi chạy lại tệp này.';
    SET NOEXEC ON;
END;
GO

DECLARE @GhiDeAnhXaMau BIT = 0; -- Đặt = 1 nếu muốn ghi đè cả hồ sơ quản trị viên đã sửa tay trên giao diện
DECLARE @TenantIdMapping NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdMapping IS NULL
    SET @TenantIdMapping = N'Default';

DECLARE @RemarkMapping NVARCHAR(256) = N'Áp dụng bộ mã chuẩn hóa lane_code (làn đường) và traffic_condition (tình trạng giao thông); ánh xạ lại khi có mẫu JSON riêng của đối tác.';

DECLARE @SchemaJson NVARCHAR(MAX) = N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "zoneStatusId": "<zoneStatusId>",
      "zoneId": "<zoneId>",
      "zoneName": "<zoneName>",
      "fromLocationKm": 0,
      "fromLocationMet": 0,
      "toLocationKm": 0,
      "toLocationMet": 0,
      "laneId": "LANE_1",
      "averageSpeed": 80.5,
      "trafficCondition": "NORMAL",
      "dataTime": "2026-11-06T08:00:00+07:00",
      "speedLimit": 80,
      "vehicleCount": 120
    }
  ]
}';

DECLARE @ShapeJson NVARCHAR(MAX) = N'{
  "header": {
    "dataTime": {
      "$meta": "Now"
    },
    "partnerCode": {
      "$meta": "PartnerCode",
      "$value": "{{PARTNER_CODE}}"
    },
    "packetCode": {
      "$meta": "PacketCode",
      "$value": "{{PACKET_CODE}}"
    },
    "serial": {
      "$meta": "Serial"
    }
  },
  "data": [
    {
      "zoneStatusId": {
        "$field": "zoneStatusId"
      },
      "zoneId": {
        "$field": "zoneId"
      },
      "zoneName": {
        "$field": "zoneName"
      },
      "fromLocationKm": {
        "$field": "fromLocationKm"
      },
      "fromLocationMet": {
        "$field": "fromLocationMet"
      },
      "toLocationKm": {
        "$field": "toLocationKm"
      },
      "toLocationMet": {
        "$field": "toLocationMet"
      },
      "laneId": {
        "$field": "laneId",
        "$extend": {
          "targetType": "string",
          "codeSet": "lane_code"
        }
      },
      "averageSpeed": {
        "$field": "averageSpeed"
      },
      "trafficCondition": {
        "$field": "trafficCondition",
        "$extend": {
          "targetType": "string",
          "codeSet": "traffic_condition"
        }
      },
      "dataTime": {
        "$field": "dataTime"
      },
      "speedLimit": {
        "$field": "speedLimit"
      },
      "vehicleCount": {
        "$field": "vehicleCount"
      }
    }
  ]
}';

-- Hồ sơ cần có: đối tác × chiều (gói 101)
DECLARE @Wanted TABLE ([PartnerCode] NVARCHAR(32), [Direction] INT, [DirCode] NVARCHAR(8), [DirName] NVARCHAR(16));
INSERT INTO @Wanted ([PartnerCode], [Direction], [DirCode], [DirName])
VALUES (N'PC08', 0, N'OUT', N'Gửi đi'),
       (N'PC08', 1, N'IN', N'Nhận về'),
       (N'TTDHT', 1, N'IN', N'Nhận về');

DECLARE @PacketId NVARCHAR(128), @PacketCode NVARCHAR(128);
SELECT TOP 1 @PacketId = p.[ID], @PacketCode = p.[Code]
FROM [dbo].[ShareDataPacket] p
WHERE p.[IsDelete] IS NULL AND (p.[Code] = N'101' OR p.[Code] LIKE N'101[_]%')
ORDER BY CASE WHEN p.[Code] = N'101_commonData' THEN 0 ELSE 1 END, p.[CreateTime];

DECLARE @Targets TABLE ([PartnerId] NVARCHAR(128), [PartnerCode] NVARCHAR(32), [Direction] INT, [DirName] NVARCHAR(16), [MappingCode] NVARCHAR(128));
INSERT INTO @Targets ([PartnerId], [PartnerCode], [Direction], [DirName], [MappingCode])
SELECT pt.[ID], pt.[Code], w.[Direction], w.[DirName],
       UPPER(pt.[Code]) + N'_' + UPPER(REPLACE(ISNULL(@PacketCode, N''), N'_', N'')) + N'_' + w.[DirCode]
FROM @Wanted w
JOIN [dbo].[ShareDataPartner] pt ON pt.[Code] = w.[PartnerCode] AND pt.[IsDelete] IS NULL AND pt.[Status] = 1;

IF @PacketId IS NULL
    PRINT N'⚠️ Chưa có gói 101 đang sống: chưa thêm hồ sơ nào.';
IF (SELECT COUNT(DISTINCT [PartnerCode]) FROM @Targets) < 2
    PRINT N'⚠️ Thiếu đối tác PC08 hoặc TTDHT đang bật: chạy sharedata-seed-doi-tac-ShareDataPartner.sql trước.';

-- -----------------------------------------------------------------------------------------
-- 1. XÓA MỀM HỒ SƠ KHÁC CỦA PC08, TTDHT
-- -----------------------------------------------------------------------------------------
UPDATE m
SET m.[IsDelete] = GETDATE(),
    m.[UpdateTime] = GETDATE(),
    m.[UpdateUId] = N'SHAREDATA-SEED-16'
FROM [dbo].[ShareDataMapping] m
JOIN [dbo].[ShareDataPartner] pt ON pt.[ID] = m.[PartnerId] AND pt.[Code] IN (N'PC08', N'TTDHT')
WHERE m.[IsDelete] IS NULL
  AND (@PacketId IS NULL OR NOT EXISTS (
        SELECT 1 FROM @Targets t
        WHERE t.[PartnerId] = m.[PartnerId] AND t.[Direction] = m.[Direction]
          AND (m.[DatatypeId] = @PacketId OR m.[DatatypeId] = @PacketCode)
      ));
PRINT N'-> 1. Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ thừa của PC08, TTDHT.';

-- -----------------------------------------------------------------------------------------
-- 2. THÊM 3 HỒ SƠ GÓI 101 CÒN THIẾU (CÓ GẮN BỘ MÃ CHUẨN HÓA)
-- -----------------------------------------------------------------------------------------
-- IsDelete để trống: cột là thời điểm xóa mềm, ghi 0 sẽ thành 1900-01-01 và hồ sơ bị coi là đã xóa.
INSERT INTO [dbo].[ShareDataMapping]
    ([ID], [Code], [Name], [PartnerId], [DatatypeId], [PacketVersion], [Direction],
     [PartnerSchemaJson], [TargetShapeJson], [IsActive], [Remark], [TenantId], [CreateTime], [CreateUId])
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())),
    t.[MappingCode],
    t.[PartnerCode] + N' - ' + @PacketCode + N' - ' + t.[DirName],
    t.[PartnerId],
    @PacketId,
    NULL,
    t.[Direction],
    REPLACE(REPLACE(@SchemaJson, N'{{PARTNER_CODE}}', t.[PartnerCode]), N'{{PACKET_CODE}}', @PacketCode),
    REPLACE(REPLACE(@ShapeJson, N'{{PARTNER_CODE}}', t.[PartnerCode]), N'{{PACKET_CODE}}', @PacketCode),
    1,
    @RemarkMapping,
    @TenantIdMapping,
    GETDATE(),
    N'SHAREDATA-SEED-16'
FROM @Targets t
WHERE @PacketId IS NOT NULL
  AND NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataMapping] m
        WHERE m.[IsDelete] IS NULL AND m.[Code] = t.[MappingCode]
    )
  AND NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataMapping] m
        WHERE m.[IsDelete] IS NULL AND m.[IsActive] = 1
          AND m.[PartnerId] = t.[PartnerId]
          AND (m.[DatatypeId] = @PacketId OR m.[DatatypeId] = @PacketCode)
          AND (m.[Direction] = t.[Direction] OR m.[Direction] = 2)
    );
PRINT N'-> 2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ gói 101 còn thiếu (tối đa 3).';

-- -----------------------------------------------------------------------------------------
-- 3. CẬP NHẬT ÁP DỤNG BỘ MÃ CHUẨN HÓA CHO 3 HỒ SƠ MẪU ĐÃ TẠO TRƯỚC ĐÓ
-- -----------------------------------------------------------------------------------------
UPDATE m
SET m.[PartnerSchemaJson] = REPLACE(REPLACE(@SchemaJson, N'{{PARTNER_CODE}}', t.[PartnerCode]), N'{{PACKET_CODE}}', @PacketCode),
    m.[TargetShapeJson]   = REPLACE(REPLACE(@ShapeJson, N'{{PARTNER_CODE}}', t.[PartnerCode]), N'{{PACKET_CODE}}', @PacketCode),
    m.[Remark]            = @RemarkMapping,
    m.[UpdateTime]        = GETDATE(),
    m.[UpdateUId]         = N'SHAREDATA-SEED-16'
FROM [dbo].[ShareDataMapping] m
JOIN @Targets t
  ON t.[PartnerId] = m.[PartnerId]
 AND t.[Direction] = m.[Direction]
WHERE @PacketId IS NOT NULL
  AND m.[IsDelete] IS NULL
  AND (m.[DatatypeId] = @PacketId OR m.[DatatypeId] = @PacketCode)
  AND (
        @GhiDeAnhXaMau = 1
        OR (
            m.[CreateUId] IN (N'SHAREDATA-SEED-15', N'SHAREDATA-SEED-16')
            AND (m.[UpdateUId] IS NULL OR m.[UpdateUId] IN (N'SHAREDATA-SEED-15', N'SHAREDATA-SEED-16'))
            AND (
                m.[TargetShapeJson] IS NULL
                OR m.[TargetShapeJson] NOT LIKE N'%"traffic_condition"%'
                OR m.[TargetShapeJson] NOT LIKE N'%"lane_code"%'
            )
        )
      );
PRINT N'-> 3. Đã cập nhật áp dụng bộ mã chuẩn hóa (lane_code, traffic_condition) cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ của PC08, TTDHT.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-anh-xa-ShareDataMapping.sql';
PRINT N'=============================================================================';
GO
