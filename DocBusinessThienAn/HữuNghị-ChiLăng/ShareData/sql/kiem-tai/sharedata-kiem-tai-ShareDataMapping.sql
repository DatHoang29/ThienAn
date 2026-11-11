USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-kiem-tai-ShareDataMapping.sql
-- BẢNG GHI: [dbo].[ShareDataMapping] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Kiểm tải ShareData trên Staging (nhiều đối tác × nhiều gói tin):
--              - Tạo / khôi phục hồ sơ ánh xạ đang bật cho toàn bộ các gói tin đang bật trên CSDL
--                (trên staging là 9 gói: 101, 102, 103, 104, 106, 107, 108, 109, 110; nếu 105 bật thì có cả 105)
--                × cả 2 đối tác gốc (PC08, TTDHT) lẫn toàn bộ đối tác kiểm tải (KT001..KT050) do K1 tạo
--                × 2 chiều (Gửi đi: Direction = 0, mã ..._OUT; Nhận về: Direction = 1, mã ..._IN).
--              - Gói 101 áp dụng sẵn bộ mã chuẩn hóa lane_code và traffic_condition; các gói 102..110 giữ nguyên
--                tên trường khớp cột trả về của câu SQL mẫu chuẩn trong ShareDataSql.
--              - Những hồ sơ của PC08, TTDHT đã có sẵn trên staging (do SHAREDATA-SEED-15 tạo trước đây) nhưng đang
--                bị xóa mềm sẽ được khôi phục tạm (ghi UpdateUId = N'SHAREDATA-KIEM-TAI') và tự trả về xóa mềm khi dọn.
-- THỨ TỰ CHẠY: K2, chạy sau K1 (sharedata-kiem-tai-ShareDataPartner.sql), trước K3.
--              DỌN: đặt @XoaDuLieuKiemTai = 1, chạy sau K3 và trước K1.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Staging (chạy theo yêu cầu kiểm tải). ⛔ Không chạy trên production.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 11112026 (tự bỏ gói chưa có câu ghi chiều nhận, vd 110; hỗ trợ đầy đủ các gói 101..110 đang bật cho PC08, TTDHT và KT001..KT050)
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-kiem-tai-ShareDataMapping.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataMapping]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataMapping] chưa tồn tại (chưa chạy CodeFirst)!';
    SET NOEXEC ON;
END;
GO

-- -----------------------------------------------------------------------------------------
-- THAM SỐ — giữ @TienToMa giống K1, K3.
-- -----------------------------------------------------------------------------------------
DECLARE @TienToMa NVARCHAR(16) = N'KT';
DECLARE @GoiBoQua NVARCHAR(256) = N'';                           -- Mã gói bỏ thêm khỏi kiểm tải, cách nhau dấu phẩy, vd N'109'; N'' = mọi gói đang bật. Gói chưa có câu ghi chiều nhận (vd 110) tệp tự bỏ
DECLARE @XoaDuLieuKiemTai BIT = 0;                               -- 1 = xóa hồ sơ ánh xạ kiểm tải (chạy sau khi đã dọn K3)

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-KIEM-TAI';
DECLARE @TenantIdMapping NVARCHAR(64) = (
    SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64))
    FROM [dbo].[SysOpConfig]
    WHERE [TenantId] IS NOT NULL
);
IF @TenantIdMapping IS NULL
    SET @TenantIdMapping = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. DỌN DỮ LIỆU KIỂM TẢI
-- -----------------------------------------------------------------------------------------
IF @XoaDuLieuKiemTai = 1
BEGIN
    DELETE FROM [dbo].[ShareDataMapping]
    WHERE [CreateUId] = @SeedUser;
    PRINT N'-> 1.1. Đã xóa ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ kiểm tải tạo mới.';

    UPDATE m
    SET m.[IsDelete]   = GETDATE(),
        m.[UpdateTime] = GETDATE(),
        m.[UpdateUId]  = N'SHAREDATA-SEED-16'
    FROM [dbo].[ShareDataMapping] m
    WHERE m.[IsDelete] IS NULL
      AND m.[UpdateUId] = @SeedUser
      AND ISNULL(m.[CreateUId], N'') <> @SeedUser
      AND m.[Code] NOT IN (N'PC08_101COMMONDATA_OUT', N'PC08_101COMMONDATA_IN', N'TTDHT_101COMMONDATA_IN');
    PRINT N'-> 1.2. Đã trả về trạng thái xóa mềm cho ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ của PC08, TTDHT.';
END
ELSE
BEGIN
    -- -------------------------------------------------------------------------------------
    -- 2. BỘ KHUNG ÁNH XẠ THEO GÓI TIN 101..110 ({{PARTNER_CODE}}, {{PACKET_CODE}})
    -- -------------------------------------------------------------------------------------
    DECLARE @Templates TABLE
    (
        [Prefix] NVARCHAR(8),
        [CatalogCode] NVARCHAR(64),
        [SchemaJson] NVARCHAR(MAX),
        [ShapeJson] NVARCHAR(MAX)
    );

    INSERT INTO @Templates ([Prefix], [CatalogCode], [SchemaJson], [ShapeJson])
    VALUES
        (N'101', N'101_commonData',
N'{
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
}',
N'{
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
      "zoneStatusId": { "$field": "zoneStatusId" },
      "zoneId": { "$field": "zoneId" },
      "zoneName": { "$field": "zoneName" },
      "fromLocationKm": { "$field": "fromLocationKm" },
      "fromLocationMet": { "$field": "fromLocationMet" },
      "toLocationKm": { "$field": "toLocationKm" },
      "toLocationMet": { "$field": "toLocationMet" },
      "laneId": {
        "$field": "laneId",
        "$extend": { "targetType": "string", "codeSet": "lane_code" }
      },
      "averageSpeed": { "$field": "averageSpeed" },
      "trafficCondition": {
        "$field": "trafficCondition",
        "$extend": { "targetType": "string", "codeSet": "traffic_condition" }
      },
      "dataTime": { "$field": "dataTime" },
      "speedLimit": { "$field": "speedLimit" },
      "vehicleCount": { "$field": "vehicleCount" }
    }
  ]
}'),
        (N'102', N'102_cctvData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "cameraCode": "<cameraCode>",
      "cameraName": "<cameraName>",
      "snapshot": "<snapshot>",
      "snapshotTime": "<snapshotTime>",
      "deviceState": "<deviceState>",
      "locationKm": "<locationKm>",
      "locationMet": "<locationMet>",
      "direction": "<direction>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "cameraCode": { "$field": "cameraCode" },
      "cameraName": { "$field": "cameraName" },
      "snapshot": { "$field": "snapshot" },
      "snapshotTime": { "$field": "snapshotTime" },
      "deviceState": { "$field": "deviceState" },
      "locationKm": { "$field": "locationKm" },
      "locationMet": { "$field": "locationMet" },
      "direction": { "$field": "direction" }
    }
  ]
}'),
        (N'103', N'103_vdsData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "detectionId": "<detectionId>",
      "detectTime": "<detectTime>",
      "vehicleType": "<vehicleType>",
      "licensePlate": "<licensePlate>",
      "speed": "<speed>",
      "lane": "<lane>",
      "direction": "<direction>",
      "locationRoute": "<locationRoute>",
      "equipmentId": "<equipmentId>",
      "locationKm": "<locationKm>",
      "locationMet": "<locationMet>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "detectionId": { "$field": "detectionId" },
      "detectTime": { "$field": "detectTime" },
      "vehicleType": { "$field": "vehicleType" },
      "licensePlate": { "$field": "licensePlate" },
      "speed": { "$field": "speed" },
      "lane": { "$field": "lane" },
      "direction": { "$field": "direction" },
      "locationRoute": { "$field": "locationRoute" },
      "equipmentId": { "$field": "equipmentId" },
      "locationKm": { "$field": "locationKm" },
      "locationMet": { "$field": "locationMet" }
    }
  ]
}'),
        (N'104', N'104_weatherData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "weatherStationId": "<weatherStationId>",
      "locationDetail": "<locationDetail>",
      "temperature": "<temperature>",
      "humidity": "<humidity>",
      "windSpeed": "<windSpeed>",
      "windDirection": "<windDirection>",
      "rainfall": "<rainfall>",
      "rainfallHour": "<rainfallHour>",
      "visibility": "<visibility>",
      "weatherDescription": "<weatherDescription>",
      "weatherCode": "<weatherCode>",
      "detectTime": "<detectTime>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "weatherStationId": { "$field": "weatherStationId" },
      "locationDetail": { "$field": "locationDetail" },
      "temperature": { "$field": "temperature" },
      "humidity": { "$field": "humidity" },
      "windSpeed": { "$field": "windSpeed" },
      "windDirection": { "$field": "windDirection" },
      "rainfall": { "$field": "rainfall" },
      "rainfallHour": { "$field": "rainfallHour" },
      "visibility": { "$field": "visibility" },
      "weatherDescription": { "$field": "weatherDescription" },
      "weatherCode": { "$field": "weatherCode" },
      "detectTime": { "$field": "detectTime" }
    }
  ]
}'),
        (N'105', N'105_rfidData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "transactionId": "<transactionId>",
      "tagId": "<tagId>",
      "licensePlate": "<licensePlate>",
      "vehicleTypeId": "<vehicleTypeId>",
      "entryTime": "<entryTime>",
      "exitTime": "<exitTime>",
      "laneId": "<laneId>",
      "stationId": "<stationId>",
      "vehicleBrand": "<vehicleBrand>",
      "vehicleOwner": "<vehicleOwner>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "transactionId": { "$field": "transactionId" },
      "tagId": { "$field": "tagId" },
      "licensePlate": { "$field": "licensePlate" },
      "vehicleTypeId": { "$field": "vehicleTypeId" },
      "entryTime": { "$field": "entryTime" },
      "exitTime": { "$field": "exitTime" },
      "laneId": { "$field": "laneId" },
      "stationId": { "$field": "stationId" },
      "vehicleBrand": { "$field": "vehicleBrand" },
      "vehicleOwner": { "$field": "vehicleOwner" }
    }
  ]
}'),
        (N'106', N'106_wimData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "detectTime": "<detectTime>",
      "lane": "<lane>",
      "locationCode": "<locationCode>",
      "speed": "<speed>",
      "height": "<height>",
      "width": "<width>",
      "length": "<length>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "detectTime": { "$field": "detectTime" },
      "lane": { "$field": "lane" },
      "locationCode": { "$field": "locationCode" },
      "speed": { "$field": "speed" },
      "height": { "$field": "height" },
      "width": { "$field": "width" },
      "length": { "$field": "length" }
    }
  ]
}'),
        (N'107', N'107_incidentData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "incidentCode": "<incidentCode>",
      "incidentName": "<incidentName>",
      "eventTypeId": "<eventTypeId>",
      "eventTypeName": "<eventTypeName>",
      "occurredTime": "<occurredTime>",
      "locationKm": "<locationKm>",
      "locationMet": "<locationMet>",
      "locationRoute": "<locationRoute>",
      "direction": "<direction>",
      "injuredCount": "<injuredCount>",
      "vehicleCount": "<vehicleCount>",
      "incidentState": "<incidentState>",
      "description": "<description>",
      "source": "<source>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "incidentCode": { "$field": "incidentCode" },
      "incidentName": { "$field": "incidentName" },
      "eventTypeId": { "$field": "eventTypeId" },
      "eventTypeName": { "$field": "eventTypeName" },
      "occurredTime": { "$field": "occurredTime" },
      "locationKm": { "$field": "locationKm" },
      "locationMet": { "$field": "locationMet" },
      "locationRoute": { "$field": "locationRoute" },
      "direction": { "$field": "direction" },
      "injuredCount": { "$field": "injuredCount" },
      "vehicleCount": { "$field": "vehicleCount" },
      "incidentState": { "$field": "incidentState" },
      "description": { "$field": "description" },
      "source": { "$field": "source" }
    }
  ]
}'),
        (N'108', N'108_vmsInfo',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "equipmentCode": "<equipmentCode>",
      "vmsName": "<vmsName>",
      "locationKm": "<locationKm>",
      "locationMet": "<locationMet>",
      "direction": "<direction>",
      "laneId": "<laneId>",
      "displayContent": "<displayContent>",
      "displayImageUrl": "<displayImageUrl>",
      "displaySize": "<displaySize>",
      "priority": "<priority>",
      "executedTime": "<executedTime>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "equipmentCode": { "$field": "equipmentCode" },
      "vmsName": { "$field": "vmsName" },
      "locationKm": { "$field": "locationKm" },
      "locationMet": { "$field": "locationMet" },
      "direction": { "$field": "direction" },
      "laneId": { "$field": "laneId" },
      "displayContent": { "$field": "displayContent" },
      "displayImageUrl": { "$field": "displayImageUrl" },
      "displaySize": { "$field": "displaySize" },
      "priority": { "$field": "priority" },
      "executedTime": { "$field": "executedTime" }
    }
  ]
}'),
        (N'109', N'109_etcData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "transactionId": "<transactionId>",
      "entryTime": "<entryTime>",
      "exitTime": "<exitTime>",
      "vehicleTypeId": "<vehicleTypeId>",
      "licensePlate": "<licensePlate>",
      "tagId": "<tagId>",
      "laneId": "<laneId>",
      "laneName": "<laneName>",
      "stationId": "<stationId>",
      "stationName": "<stationName>",
      "tollPrice": "<tollPrice>",
      "syncTime": "<syncTime>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "transactionId": { "$field": "transactionId" },
      "entryTime": { "$field": "entryTime" },
      "exitTime": { "$field": "exitTime" },
      "vehicleTypeId": { "$field": "vehicleTypeId" },
      "licensePlate": { "$field": "licensePlate" },
      "tagId": { "$field": "tagId" },
      "laneId": { "$field": "laneId" },
      "laneName": { "$field": "laneName" },
      "stationId": { "$field": "stationId" },
      "stationName": { "$field": "stationName" },
      "tollPrice": { "$field": "tollPrice" },
      "syncTime": { "$field": "syncTime" }
    }
  ]
}'),
        (N'110', N'110_wpData',
N'{
  "header": {
    "dataTime": "2026-11-06T08:00:00+07:00",
    "partnerCode": "{{PARTNER_CODE}}",
    "packetCode": "{{PACKET_CODE}}",
    "serial": 1
  },
  "data": [
    {
      "messageId": "<messageId>",
      "incidentMessage": "<incidentMessage>",
      "weatherMessage": "<weatherMessage>",
      "guidanceContent": "<guidanceContent>",
      "locationKm": "<locationKm>",
      "locationMet": "<locationMet>",
      "publishedTime": "<publishedTime>",
      "channel": "<channel>",
      "deliveryState": "<deliveryState>"
    }
  ]
}',
N'{
  "header": {
    "dataTime": { "$meta": "Now" },
    "partnerCode": { "$meta": "PartnerCode", "$value": "{{PARTNER_CODE}}" },
    "packetCode": { "$meta": "PacketCode", "$value": "{{PACKET_CODE}}" },
    "serial": { "$meta": "Serial" }
  },
  "data": [
    {
      "messageId": { "$field": "messageId" },
      "incidentMessage": { "$field": "incidentMessage" },
      "weatherMessage": { "$field": "weatherMessage" },
      "guidanceContent": { "$field": "guidanceContent" },
      "locationKm": { "$field": "locationKm" },
      "locationMet": { "$field": "locationMet" },
      "publishedTime": { "$field": "publishedTime" },
      "channel": { "$field": "channel" },
      "deliveryState": { "$field": "deliveryState" }
    }
  ]
}');

    DECLARE @Directions TABLE ([Direction] INT, [DirCode] NVARCHAR(8), [DirName] NVARCHAR(16));
    INSERT INTO @Directions ([Direction], [DirCode], [DirName])
    VALUES (0, N'OUT', N'Gửi đi'),
           (1, N'IN',  N'Nhận về');

    DECLARE @TargetPartners TABLE ([PartnerId] NVARCHAR(64), [PartnerCode] NVARCHAR(32));
    INSERT INTO @TargetPartners ([PartnerId], [PartnerCode])
    SELECT p.[ID], p.[Code]
    FROM [dbo].[ShareDataPartner] p
    WHERE p.[IsDelete] IS NULL
      AND p.[Status] = 1
      AND (
            p.[Code] IN (N'PC08', N'TTDHT')
            OR (p.[CreateUId] = @SeedUser AND p.[Code] LIKE @TienToMa + N'[0-9][0-9][0-9]')
          );

    DECLARE @ActivePackets TABLE
    (
        [PacketId] NVARCHAR(64),
        [PacketCode] NVARCHAR(64),
        [SchemaJson] NVARCHAR(MAX),
        [ShapeJson] NVARCHAR(MAX)
    );
    INSERT INTO @ActivePackets ([PacketId], [PacketCode], [SchemaJson], [ShapeJson])
    SELECT pk.[ID], pk.[Code], t.[SchemaJson], t.[ShapeJson]
    FROM @Templates t
    CROSS APPLY (
        SELECT TOP 1 p.[ID], p.[Code]
        FROM [dbo].[ShareDataPacket] p
        WHERE p.[IsDelete] IS NULL
          AND p.[Status] = 1
          AND (p.[Code] = t.[Prefix] OR p.[Code] LIKE t.[Prefix] + N'[_]%')
        ORDER BY CASE WHEN p.[Code] = t.[CatalogCode] THEN 0 ELSE 1 END, p.[CreateTime]
    ) pk
    WHERE t.[Prefix] NOT IN (SELECT LTRIM(RTRIM([value])) FROM STRING_SPLIT(@GoiBoQua, N','))
      -- Gói chưa có câu ghi chiều nhận (vd 110) thì bỏ: tạo hồ sơ cũng không chạy được hai chiều
      AND EXISTS (
          SELECT 1
          FROM [dbo].[ShareDataSql] sw
          WHERE sw.[PacketId] = pk.[ID]
            AND sw.[PartnerId] IS NULL
            AND sw.[IsDelete] IS NULL
            AND NULLIF(LTRIM(RTRIM(sw.[WriteSql])), N'') IS NOT NULL
      );

    -- -------------------------------------------------------------------------------------
    -- 2.1. KHÔI PHỤC HỒ SƠ ĐÃ CÓ SẴN NHƯNG ĐANG BỊ XÓA MỀM (PC08, TTDHT GÓI 101..110)
    -- -------------------------------------------------------------------------------------
    ;WITH [CanKhoiPhuc] AS (
        SELECT
            m.[ID],
            ROW_NUMBER() OVER (
                PARTITION BY m.[PartnerId], m.[DatatypeId], m.[Direction]
                ORDER BY m.[UpdateTime] DESC, m.[CreateTime] DESC
            ) AS [Rn]
        FROM [dbo].[ShareDataMapping] m
        JOIN @TargetPartners tp ON tp.[PartnerId] = m.[PartnerId]
        JOIN @ActivePackets ap ON (m.[DatatypeId] = ap.[PacketId] OR m.[DatatypeId] = ap.[PacketCode])
        JOIN @Directions d ON d.[Direction] = m.[Direction]
        WHERE m.[IsDelete] IS NOT NULL
          AND NOT EXISTS (
              SELECT 1
              FROM [dbo].[ShareDataMapping] live
              WHERE live.[IsDelete] IS NULL
                AND live.[PartnerId] = m.[PartnerId]
                AND (live.[DatatypeId] = ap.[PacketId] OR live.[DatatypeId] = ap.[PacketCode])
                AND (live.[Direction] = m.[Direction] OR live.[Direction] = 2)
          )
    )
    UPDATE m
    SET m.[IsDelete]   = NULL,
        m.[IsActive]   = 1,
        m.[UpdateTime] = GETDATE(),
        m.[UpdateUId]  = @SeedUser
    FROM [dbo].[ShareDataMapping] m
    JOIN [CanKhoiPhuc] c ON c.[ID] = m.[ID]
    WHERE c.[Rn] = 1;
    PRINT N'-> 2.1. Đã khôi phục ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ sẵn có của PC08, TTDHT cho các gói đang bật.';

    -- -------------------------------------------------------------------------------------
    -- 2.2. THÊM MỚI HỒ SƠ ÁNH XẠ CÒN THIẾU CHO PC08, TTDHT VÀ KT001..KT050
    -- -------------------------------------------------------------------------------------
    ;WITH [Src] AS (
        SELECT
            tp.[PartnerId],
            tp.[PartnerCode],
            ap.[PacketId],
            ap.[PacketCode],
            d.[Direction],
            d.[DirName],
            LEFT(UPPER(tp.[PartnerCode]) + N'_' + UPPER(REPLACE(ap.[PacketCode], N'_', N'')) + N'_' + d.[DirCode], 64) AS [MappingCode],
            ap.[SchemaJson],
            ap.[ShapeJson]
        FROM @TargetPartners tp
        CROSS JOIN @ActivePackets ap
        CROSS JOIN @Directions d
    )
    INSERT INTO [dbo].[ShareDataMapping]
        ([ID], [Code], [Name], [PartnerId], [DatatypeId], [PacketVersion], [Direction],
         [PartnerSchemaJson], [TargetShapeJson], [IsActive], [Remark], [TenantId], [CreateTime], [CreateUId])
    SELECT
        LOWER(CONVERT(NVARCHAR(64), NEWID())),
        s.[MappingCode],
        LEFT(s.[PartnerCode] + N' - ' + s.[PacketCode] + N' - ' + s.[DirName], 128),
        s.[PartnerId],
        s.[PacketId],
        NULL,
        s.[Direction],
        REPLACE(REPLACE(s.[SchemaJson], N'{{PARTNER_CODE}}', s.[PartnerCode]), N'{{PACKET_CODE}}', s.[PacketCode]),
        REPLACE(REPLACE(s.[ShapeJson], N'{{PARTNER_CODE}}', s.[PartnerCode]), N'{{PACKET_CODE}}', s.[PacketCode]),
        1,
        N'Dữ liệu kiểm tải nhiều đối tác × gói tin',
        @TenantIdMapping,
        GETDATE(),
        @SeedUser
    FROM [Src] s
    WHERE NOT EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataMapping] m
            WHERE m.[IsDelete] IS NULL
              AND m.[Code] = s.[MappingCode]
        )
      AND NOT EXISTS (
            SELECT 1
            FROM [dbo].[ShareDataMapping] m
            WHERE m.[IsDelete] IS NULL
              AND m.[IsActive] = 1
              AND m.[PartnerId] = s.[PartnerId]
              AND (m.[DatatypeId] = s.[PacketId] OR m.[DatatypeId] = s.[PacketCode])
              AND (m.[Direction] = s.[Direction] OR m.[Direction] = 2)
        );
    PRINT N'-> 2.2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ mới (nhiều đối tác × gói tin).';
END;
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-kiem-tai-ShareDataMapping.sql';
PRINT N'=============================================================================';
GO
