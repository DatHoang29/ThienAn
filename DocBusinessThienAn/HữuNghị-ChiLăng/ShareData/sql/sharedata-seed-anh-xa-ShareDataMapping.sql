USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-seed-anh-xa-ShareDataMapping.sql
-- BẢNG GHI: [dbo].[ShareDataMapping] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Tạo hồ sơ ánh xạ cho 2 đối tác PC08, TTDHT × 10 gói 101 - 110 × 2 chiều tách riêng
--              (Gửi đi: Direction = 0, mã ..._OUT; Nhận về: Direction = 1, mã ..._IN), tối đa 40 hồ sơ, đều bật.
--              Gói đã xóa (vd 105_rfidData xóa trên giao diện DEV_ITS015 ngày 06/11/2026) thì bỏ qua, có in tên.
--              Mã hồ sơ theo đúng cách màn hình Ánh xạ dữ liệu tự sinh: ĐỐI TÁC_GÓI TIN_CHIỀU (vd PC08_101COMMONDATA_OUT).
--              Chưa có mẫu JSON của đối tác nên bộ khung dựng theo dạng header + data:
--                header: dataTime ($meta Now), partnerCode ($meta PartnerCode), packetCode ($meta PacketCode),
--                        serial ($meta Serial) — đủ 4 giá trị hệ thống bắt buộc của hồ sơ gửi đi;
--                data  : mảng bản ghi, mỗi khoá giữ nguyên tên trường gói tin ({"$field":"<tên>"}), không luật quy đổi.
--              Tên trường lấy từ cột trả về của câu SQL mẫu chuẩn trong sharedata-seed-goi-tin-ShareDataSql.sql
--              (bỏ cột kỹ thuật __lastKey):
--                101 (13 trường): zoneStatusId, zoneId, zoneName, fromLocationKm, fromLocationMet, toLocationKm, toLocationMet, laneId, averageSpeed, trafficCondition, dataTime, speedLimit, vehicleCount
--                102 (8 trường): cameraCode, cameraName, snapshot, snapshotTime, deviceState, locationKm, locationMet, direction
--                103 (11 trường): detectionId, detectTime, vehicleType, licensePlate, speed, lane, direction, locationRoute, equipmentId, locationKm, locationMet
--                104 (12 trường): weatherStationId, locationDetail, temperature, humidity, windSpeed, windDirection, rainfall, rainfallHour, visibility, weatherDescription, weatherCode, detectTime
--                105 (10 trường): transactionId, tagId, licensePlate, vehicleTypeId, entryTime, exitTime, laneId, stationId, vehicleBrand, vehicleOwner
--                106 (7 trường): detectTime, lane, locationCode, speed, height, width, length
--                107 (14 trường): incidentCode, incidentName, eventTypeId, eventTypeName, occurredTime, locationKm, locationMet, locationRoute, direction, injuredCount, vehicleCount, incidentState, description, source
--                108 (11 trường): equipmentCode, vmsName, locationKm, locationMet, direction, laneId, displayContent, displayImageUrl, displaySize, priority, executedTime
--                109 (12 trường): transactionId, entryTime, exitTime, vehicleTypeId, licensePlate, tagId, laneId, laneName, stationId, stationName, tollPrice, syncTime
--                110 (9 trường): messageId, incidentMessage, weatherMessage, guidanceContent, locationKm, locationMet, publishedTime, channel, deliveryState
--              Khi đối tác gửi mẫu JSON thật: mở hồ sơ trên màn hình Ánh xạ dữ liệu, dán mẫu và ánh xạ lại.
--              Bỏ qua (không ghi đè) khi đã có hồ sơ đang sống cùng mã, hoặc đã có hồ sơ đang bật cùng
--              đối tác × gói × chiều (kể cả hồ sơ 2 chiều) — giống luật chặn trùng của API.
-- THỨ TỰ CHẠY: 15 (sau tệp 1 gói tin và tệp 14 đối tác) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 06/11/2026 (tạo mới)
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

-- -----------------------------------------------------------------------------------------
-- 1. BỘ KHUNG THEO GÓI TIN ({{PARTNER_CODE}}, {{PACKET_CODE}} thay bằng mã thật lúc thêm)
-- -----------------------------------------------------------------------------------------
DECLARE @TenantIdMapping NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdMapping IS NULL
    SET @TenantIdMapping = N'Default';

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
      "fromLocationKm": "<fromLocationKm>",
      "fromLocationMet": "<fromLocationMet>",
      "toLocationKm": "<toLocationKm>",
      "toLocationMet": "<toLocationMet>",
      "laneId": "<laneId>",
      "averageSpeed": "<averageSpeed>",
      "trafficCondition": "<trafficCondition>",
      "dataTime": "<dataTime>",
      "speedLimit": "<speedLimit>",
      "vehicleCount": "<vehicleCount>"
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
        "$field": "laneId"
      },
      "averageSpeed": {
        "$field": "averageSpeed"
      },
      "trafficCondition": {
        "$field": "trafficCondition"
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
      "cameraCode": {
        "$field": "cameraCode"
      },
      "cameraName": {
        "$field": "cameraName"
      },
      "snapshot": {
        "$field": "snapshot"
      },
      "snapshotTime": {
        "$field": "snapshotTime"
      },
      "deviceState": {
        "$field": "deviceState"
      },
      "locationKm": {
        "$field": "locationKm"
      },
      "locationMet": {
        "$field": "locationMet"
      },
      "direction": {
        "$field": "direction"
      }
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
      "detectionId": {
        "$field": "detectionId"
      },
      "detectTime": {
        "$field": "detectTime"
      },
      "vehicleType": {
        "$field": "vehicleType"
      },
      "licensePlate": {
        "$field": "licensePlate"
      },
      "speed": {
        "$field": "speed"
      },
      "lane": {
        "$field": "lane"
      },
      "direction": {
        "$field": "direction"
      },
      "locationRoute": {
        "$field": "locationRoute"
      },
      "equipmentId": {
        "$field": "equipmentId"
      },
      "locationKm": {
        "$field": "locationKm"
      },
      "locationMet": {
        "$field": "locationMet"
      }
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
      "weatherStationId": {
        "$field": "weatherStationId"
      },
      "locationDetail": {
        "$field": "locationDetail"
      },
      "temperature": {
        "$field": "temperature"
      },
      "humidity": {
        "$field": "humidity"
      },
      "windSpeed": {
        "$field": "windSpeed"
      },
      "windDirection": {
        "$field": "windDirection"
      },
      "rainfall": {
        "$field": "rainfall"
      },
      "rainfallHour": {
        "$field": "rainfallHour"
      },
      "visibility": {
        "$field": "visibility"
      },
      "weatherDescription": {
        "$field": "weatherDescription"
      },
      "weatherCode": {
        "$field": "weatherCode"
      },
      "detectTime": {
        "$field": "detectTime"
      }
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
      "transactionId": {
        "$field": "transactionId"
      },
      "tagId": {
        "$field": "tagId"
      },
      "licensePlate": {
        "$field": "licensePlate"
      },
      "vehicleTypeId": {
        "$field": "vehicleTypeId"
      },
      "entryTime": {
        "$field": "entryTime"
      },
      "exitTime": {
        "$field": "exitTime"
      },
      "laneId": {
        "$field": "laneId"
      },
      "stationId": {
        "$field": "stationId"
      },
      "vehicleBrand": {
        "$field": "vehicleBrand"
      },
      "vehicleOwner": {
        "$field": "vehicleOwner"
      }
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
      "detectTime": {
        "$field": "detectTime"
      },
      "lane": {
        "$field": "lane"
      },
      "locationCode": {
        "$field": "locationCode"
      },
      "speed": {
        "$field": "speed"
      },
      "height": {
        "$field": "height"
      },
      "width": {
        "$field": "width"
      },
      "length": {
        "$field": "length"
      }
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
      "incidentCode": {
        "$field": "incidentCode"
      },
      "incidentName": {
        "$field": "incidentName"
      },
      "eventTypeId": {
        "$field": "eventTypeId"
      },
      "eventTypeName": {
        "$field": "eventTypeName"
      },
      "occurredTime": {
        "$field": "occurredTime"
      },
      "locationKm": {
        "$field": "locationKm"
      },
      "locationMet": {
        "$field": "locationMet"
      },
      "locationRoute": {
        "$field": "locationRoute"
      },
      "direction": {
        "$field": "direction"
      },
      "injuredCount": {
        "$field": "injuredCount"
      },
      "vehicleCount": {
        "$field": "vehicleCount"
      },
      "incidentState": {
        "$field": "incidentState"
      },
      "description": {
        "$field": "description"
      },
      "source": {
        "$field": "source"
      }
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
      "equipmentCode": {
        "$field": "equipmentCode"
      },
      "vmsName": {
        "$field": "vmsName"
      },
      "locationKm": {
        "$field": "locationKm"
      },
      "locationMet": {
        "$field": "locationMet"
      },
      "direction": {
        "$field": "direction"
      },
      "laneId": {
        "$field": "laneId"
      },
      "displayContent": {
        "$field": "displayContent"
      },
      "displayImageUrl": {
        "$field": "displayImageUrl"
      },
      "displaySize": {
        "$field": "displaySize"
      },
      "priority": {
        "$field": "priority"
      },
      "executedTime": {
        "$field": "executedTime"
      }
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
      "transactionId": {
        "$field": "transactionId"
      },
      "entryTime": {
        "$field": "entryTime"
      },
      "exitTime": {
        "$field": "exitTime"
      },
      "vehicleTypeId": {
        "$field": "vehicleTypeId"
      },
      "licensePlate": {
        "$field": "licensePlate"
      },
      "tagId": {
        "$field": "tagId"
      },
      "laneId": {
        "$field": "laneId"
      },
      "laneName": {
        "$field": "laneName"
      },
      "stationId": {
        "$field": "stationId"
      },
      "stationName": {
        "$field": "stationName"
      },
      "tollPrice": {
        "$field": "tollPrice"
      },
      "syncTime": {
        "$field": "syncTime"
      }
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
      "messageId": {
        "$field": "messageId"
      },
      "incidentMessage": {
        "$field": "incidentMessage"
      },
      "weatherMessage": {
        "$field": "weatherMessage"
      },
      "guidanceContent": {
        "$field": "guidanceContent"
      },
      "locationKm": {
        "$field": "locationKm"
      },
      "locationMet": {
        "$field": "locationMet"
      },
      "publishedTime": {
        "$field": "publishedTime"
      },
      "channel": {
        "$field": "channel"
      },
      "deliveryState": {
        "$field": "deliveryState"
      }
    }
  ]
}');

DECLARE @Partners TABLE ([Code] NVARCHAR(32));
INSERT INTO @Partners ([Code]) VALUES (N'PC08'), (N'TTDHT');

DECLARE @Directions TABLE ([Direction] INT, [DirCode] NVARCHAR(8), [DirName] NVARCHAR(16));
INSERT INTO @Directions ([Direction], [DirCode], [DirName]) VALUES (0, N'OUT', N'Gửi đi'), (1, N'IN', N'Nhận về');

-- 1.1 Báo đối tác chưa có hoặc đang tắt (API không cho tạo hồ sơ cho đối tác tắt)
IF EXISTS (
    SELECT 1 FROM @Partners c
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataPartner] pt
        WHERE pt.[Code] = c.[Code] AND pt.[IsDelete] IS NULL AND pt.[Status] = 1
    )
)
    PRINT N'⚠️ Thiếu đối tác PC08 hoặc TTDHT đang bật: chạy sharedata-seed-doi-tac-ShareDataPartner.sql trước. Đối tác thiếu sẽ bị bỏ qua.';

-- 1.2 Báo gói chưa có hoặc đã xóa: không tạo hồ sơ cho gói đó
DECLARE @MissingPackets NVARCHAR(200) = (
    SELECT STRING_AGG(t.[CatalogCode], N', ')
    FROM @Templates t
    WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataPacket] p
        WHERE p.[IsDelete] IS NULL AND (p.[Code] = t.[Prefix] OR p.[Code] LIKE t.[Prefix] + N'[_]%')
    )
);
IF @MissingPackets IS NOT NULL
    PRINT N'ℹ️ Bỏ qua gói chưa có hoặc đã xóa: ' + @MissingPackets;

-- -----------------------------------------------------------------------------------------
-- 2. THÊM HỒ SƠ ÁNH XẠ CÒN THIẾU
-- -----------------------------------------------------------------------------------------
-- Mỗi tiền tố lấy đúng một gói: ưu tiên mã Catalog (vd 101_commonData), bỏ gói thử như 101_commonData1.
-- IsDelete để trống: cột là thời điểm xóa mềm, ghi 0 sẽ thành 1900-01-01 và hồ sơ bị coi là đã xóa.
;WITH src AS (
    SELECT
        pt.[ID] AS [PartnerId],
        pt.[Code] AS [PartnerCode],
        pk.[ID] AS [PacketId],
        pk.[Code] AS [PacketCode],
        d.[Direction],
        d.[DirName],
        UPPER(pt.[Code]) + N'_' + UPPER(REPLACE(pk.[Code], N'_', N'')) + N'_' + d.[DirCode] AS [MappingCode],
        t.[SchemaJson],
        t.[ShapeJson]
    FROM @Partners c
    JOIN [dbo].[ShareDataPartner] pt
        ON pt.[Code] = c.[Code] AND pt.[IsDelete] IS NULL AND pt.[Status] = 1
    CROSS JOIN @Templates t
    CROSS APPLY (
        SELECT TOP 1 p.[ID], p.[Code]
        FROM [dbo].[ShareDataPacket] p
        WHERE p.[IsDelete] IS NULL
          AND (p.[Code] = t.[Prefix] OR p.[Code] LIKE t.[Prefix] + N'[_]%')
        ORDER BY CASE WHEN p.[Code] = t.[CatalogCode] THEN 0 ELSE 1 END, p.[CreateTime]
    ) pk
    CROSS JOIN @Directions d
)
INSERT INTO [dbo].[ShareDataMapping]
    ([ID], [Code], [Name], [PartnerId], [DatatypeId], [PacketVersion], [Direction],
     [PartnerSchemaJson], [TargetShapeJson], [IsActive], [Remark], [TenantId], [CreateTime], [CreateUId])
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())),
    s.[MappingCode],
    s.[PartnerCode] + N' - ' + s.[PacketCode] + N' - ' + s.[DirName],
    s.[PartnerId],
    s.[PacketId],
    NULL,
    s.[Direction],
    REPLACE(REPLACE(s.[SchemaJson], N'{{PARTNER_CODE}}', s.[PartnerCode]), N'{{PACKET_CODE}}', s.[PacketCode]),
    REPLACE(REPLACE(s.[ShapeJson], N'{{PARTNER_CODE}}', s.[PartnerCode]), N'{{PACKET_CODE}}', s.[PacketCode]),
    1,
    N'Giữ nguyên tên trường của câu SQL mẫu chuẩn; ánh xạ lại khi có mẫu JSON của đối tác.',
    @TenantIdMapping,
    GETDATE(),
    N'SHAREDATA-SEED-15'
FROM src s
WHERE NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataMapping] m
        WHERE m.[IsDelete] IS NULL AND m.[Code] = s.[MappingCode]
    )
  AND NOT EXISTS (
        SELECT 1 FROM [dbo].[ShareDataMapping] m
        WHERE m.[IsDelete] IS NULL
          AND m.[IsActive] = 1
          AND m.[PartnerId] = s.[PartnerId]
          AND (m.[DatatypeId] = s.[PacketId] OR m.[DatatypeId] = s.[PacketCode])
          AND (m.[Direction] = s.[Direction] OR m.[Direction] = 2)
    );
PRINT N'-> 2. Đã thêm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' hồ sơ ánh xạ (tối đa 40).';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-anh-xa-ShareDataMapping.sql';
PRINT N'=============================================================================';
GO
