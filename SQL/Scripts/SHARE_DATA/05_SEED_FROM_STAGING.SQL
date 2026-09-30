-- ============================================================================
-- MODULE: SHARE DATA (SHARE DATA V2 - ISO 14827)
-- FILE: 05_SEED_FROM_STAGING.SQL
-- DESCRIPTION: Nạp dữ liệu cấu hình chuẩn từ môi trường Staging (10.10.8.30 / DEV_ITS10)
--              cho Partner, Packets (101-110), CodeSet, Mapping và Subscriptions.
--              Khắc phục lỗi ESH-1304: "Không tìm thấy phễu lọc chiều gửi đang bật cho gói tin [101_commonData]"
-- TARGET DATABASE: DEV_ITS10 / DEV_ITS10_Inbound (môi trường local của lập trình viên)
-- CREATED DATE: 2026-09-29
-- ============================================================================

SET NOEXEC OFF;

-- ============================================================================
-- 0. CHỐT CHẶN AN TOÀN (Server Safeguard)
--   Script thiết kế an toàn, chạy idempotency (MERGE / IF NOT EXISTS)
-- ============================================================================
PRINT N'▶ Đang nạp dữ liệu Seed ShareData từ Staging vào database: [' + DB_NAME() + N'] trên server [' + @@SERVERNAME + N']...';
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================================
    -- 1. BẢNG MÃ CHUẨN HÓA (ShareDataCodeSet)
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataCodeSet', 'U') IS NOT NULL
    BEGIN
        -- CodeSet: condition
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataCodeSet] WHERE [ID] = '5c18df08-f9ee-4396-83d9-347f23324a9e' OR [Code] = 'condition')
        BEGIN
            INSERT INTO [dbo].[ShareDataCodeSet] ([ID], [Code], [Name], [ValuesJson], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '5c18df08-f9ee-4396-83d9-347f23324a9e',
                'condition',
                N'condition',
                N'{"values":[{"sourceValue":"SLOW","partnerValue":"0","displayName":""},{"sourceValue":"NORMAL","partnerValue":"1","displayName":""},{"sourceValue":"GOOD","partnerValue":"2","displayName":""},{"sourceValue":"NEW","partnerValue":"3","displayName":""}],"defaultSourceValue":"Như","defaultPartnerValue":"Riby"}',
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm CodeSet: condition';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[ShareDataCodeSet]
            SET [Code] = 'condition',
                [Name] = N'condition',
                [ValuesJson] = N'{"values":[{"sourceValue":"SLOW","partnerValue":"0","displayName":""},{"sourceValue":"NORMAL","partnerValue":"1","displayName":""},{"sourceValue":"GOOD","partnerValue":"2","displayName":""},{"sourceValue":"NEW","partnerValue":"3","displayName":""}],"defaultSourceValue":"Như","defaultPartnerValue":"Riby"}',
                [IsDelete] = NULL,
                [UpdateTime] = GETDATE()
            WHERE [ID] = '5c18df08-f9ee-4396-83d9-347f23324a9e' OR [Code] = 'condition';
            PRINT N'  ✔ Đã cập nhật CodeSet: condition';
        END;

        -- CodeSet: test
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataCodeSet] WHERE [ID] = '0113df08-8668-4811-8cf9-f5119c0ab722' OR [Code] = 'test')
        BEGIN
            INSERT INTO [dbo].[ShareDataCodeSet] ([ID], [Code], [Name], [ValuesJson], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '0113df08-8668-4811-8cf9-f5119c0ab722',
                'test',
                N'test condition',
                N'[{"sourceValue":"SLOW","partnerValue":"0","displayName":"","isDefault":false},{"sourceValue":"NORMAL","partnerValue":"1","displayName":"","isDefault":false},{"sourceValue":"GOOD","partnerValue":"2","displayName":"","isDefault":false},{"sourceValue":"OLD","partnerValue":"3","displayName":"","isDefault":true}]',
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm CodeSet: test';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[ShareDataCodeSet]
            SET [Code] = 'test',
                [Name] = N'test condition',
                [ValuesJson] = N'[{"sourceValue":"SLOW","partnerValue":"0","displayName":"","isDefault":false},{"sourceValue":"NORMAL","partnerValue":"1","displayName":"","isDefault":false},{"sourceValue":"GOOD","partnerValue":"2","displayName":"","isDefault":false},{"sourceValue":"OLD","partnerValue":"3","displayName":"","isDefault":true}]',
                [IsDelete] = NULL,
                [UpdateTime] = GETDATE()
            WHERE [ID] = '0113df08-8668-4811-8cf9-f5119c0ab722' OR [Code] = 'test';
            PRINT N'  ✔ Đã cập nhật CodeSet: test';
        END;
    END;

    -- ============================================================================
    -- 2. ĐỐI TÁC CHIA SẺ DỮ LIỆU (ShareDataPartner)
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataPartner', 'U') IS NOT NULL
    BEGIN
        -- Partner chính thức: 'Partner' (ID: 5415df08-4bc1-4af9-8917-ee55dafa4b79)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPartner] WHERE [ID] = '5415df08-4bc1-4af9-8917-ee55dafa4b79' OR [Code] = 'Partner')
        BEGIN
            INSERT INTO [dbo].[ShareDataPartner] 
                ([ID], [Code], [Name], [Address], [Port], [EndPointApiUrl], [Status], [SessionState], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                'Partner',
                N'Partner Name',
                '115.78.1.139',
                12204,
                '/api/sharedata/sharedatainbound/receive',
                1, -- Enable (1)
                1, -- Connected (1)
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm Partner: Partner (5415df08-4bc1-4af9-8917-ee55dafa4b79)';
        END
        ELSE
        BEGIN
            -- Đảm bảo trạng thái Enable và Connected để luồng xuất bản xử lý được
            UPDATE [dbo].[ShareDataPartner]
            SET [Status] = 1,
                [SessionState] = 1,
                [Address] = '115.78.1.139',
                [Port] = 12204,
                [EndPointApiUrl] = '/api/sharedata/sharedatainbound/receive',
                [IsDelete] = NULL
            WHERE [ID] = '5415df08-4bc1-4af9-8917-ee55dafa4b79' OR [Code] = 'Partner';
            PRINT N'  ✔ Đã cập nhật trạng thái Partner: Partner (Active & Connected)';
        END

        -- Partner TTCSDL (ID: 5e18df08-fdc4-4f0c-8314-e14ad4a40cd1)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPartner] WHERE [ID] = '5e18df08-fdc4-4f0c-8314-e14ad4a40cd1' OR [Code] = 'TTCSDL')
        BEGIN
            INSERT INTO [dbo].[ShareDataPartner] 
                ([ID], [Code], [Name], [Address], [Port], [EndPointApiUrl], [Status], [SessionState], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '5e18df08-fdc4-4f0c-8314-e14ad4a40cd1',
                'TTCSDL',
                N'TTCSDL',
                '115.78.1.139',
                12204,
                '/api/sharedata/sharedatainbound/receive',
                1, 1, 'Default', GETDATE(), 'superadmin'
            );
            PRINT N'  ✔ Đã thêm Partner: TTCSDL';
        END
    END;

    -- ============================================================================
    -- 3. ĐỊNH NGHĨA GÓI TIN CHUẨN (ShareDataPacket 101 -> 110)
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataPacket', 'U') IS NOT NULL
    BEGIN
        -- Gói 101 - TrafficFlow (Common Data)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '1EC77A6E-051D-43BA-A5B9-46A01AFC267A' OR [Code] = '101_commonData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('1EC77A6E-051D-43BA-A5B9-46A01AFC267A', '101_commonData', N'Gói 101 - Dữ liệu giao thông chung', '1.0', 1, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 101_commonData';
        END

        -- Gói 102 - CctvImage
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '8B2C1C8A-433A-447E-87CD-8B88616D6BC7' OR [Code] = '102_cctvData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('8B2C1C8A-433A-447E-87CD-8B88616D6BC7', '102_cctvData', N'Gói 102 - Dữ liệu camera giám sát (CCTV)', '1.0', 2, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 102_cctvData';
        END

        -- Gói 103 - VehicleDetection (VDS)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '7F64ED85-9ED1-4CE2-BE10-223B356C37E7' OR [Code] = '103_vdsData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('7F64ED85-9ED1-4CE2-BE10-223B356C37E7', '103_vdsData', N'Gói 103 - Dữ liệu thiết bị dò xe (VDS)', '1.0', 3, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 103_vdsData';
        END

        -- Gói 104 - Weather
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '29C879DE-93D3-439C-B994-D9F556F6A5AA' OR [Code] = '104_weatherData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('29C879DE-93D3-439C-B994-D9F556F6A5AA', '104_weatherData', N'Gói 104 - Dữ liệu thời tiết', '1.0', 4, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 104_weatherData';
        END

        -- Gói 105 - RFID / AVI
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '3F7B2C91-8D4E-4A15-B6C3-0A5E9D71F204' OR [Code] = '105_rfidData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('3F7B2C91-8D4E-4A15-B6C3-0A5E9D71F204', '105_rfidData', N'Gói 105 - Dữ liệu định danh phương tiện (AVI/RFID)', '1.0', 5, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 105_rfidData';
        END

        -- Gói 106 - WIM
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '6E9DAAF6-7637-4BE6-966F-A3465FADB9B4' OR [Code] = '106_wimData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('6E9DAAF6-7637-4BE6-966F-A3465FADB9B4', '106_wimData', N'Gói 106 - Dữ liệu cân động (WIM)', '1.0', 6, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 106_wimData';
        END

        -- Gói 107 - Incident
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = 'D543BDD1-77BA-467E-816A-7D61EE264400' OR [Code] = '107_incidentData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('D543BDD1-77BA-467E-816A-7D61EE264400', '107_incidentData', N'Gói 107 - Dữ liệu sự cố giao thông', '1.0', 7, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 107_incidentData';
        END

        -- Gói 108 - VMS
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '7C426776-5E22-4134-97FC-F4BC61166DC3' OR [Code] = '108_vmsInfo')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('7C426776-5E22-4134-97FC-F4BC61166DC3', '108_vmsInfo', N'Gói 108 - Dữ liệu biển báo điện tử (VMS)', '1.0', 8, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 108_vmsInfo';
        END

        -- Gói 109 - ETC
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = 'D42CDB0A-81C2-4F61-A14D-10BB1AFB95E7' OR [Code] = '109_etcData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('D42CDB0A-81C2-4F61-A14D-10BB1AFB95E7', '109_etcData', N'Gói 109 - Dữ liệu thu phí không dừng (ETC)', '1.0', 9, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 109_etcData';
        END

        -- Gói 110 - WP
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataPacket] WHERE [ID] = '8B18BB24-7589-48BC-B20F-DB61CCC9927A' OR [Code] = '110_wpData')
        BEGIN
            INSERT INTO [dbo].[ShareDataPacket] ([ID], [Code], [Name], [PacketVersion], [OrderNo], [Status], [TenantId], [CreateTime], [CreateUId])
            VALUES ('8B18BB24-7589-48BC-B20F-DB61CCC9927A', '110_wpData', N'Gói 110 - Dữ liệu cảnh báo phát hành (WP)', '1.0', 10, 1, 'Default', GETDATE(), 'seed');
            PRINT N'  ✔ Đã thêm Packet: 110_wpData';
        END
    END;

    -- ============================================================================
    -- 4. PHỄU LỌC ÁNH XẠ DỮ LIỆU (ShareDataMapping) — GIẢI QUYẾT LỖI ESH-1304
    -- ============================================================================
    -- Lưu ý mấu chốt:
    -- Code DataOutboundService.cs hiện tìm theo: .Where(m => m.DatatypeId == packet.Code) -> '101_commonData'
    -- CSDL Staging gốc lưu: DatatypeId = '1EC77A6E-051D-43BA-A5B9-46A01AFC267A'
    -- Script này chèn cấu hình hỗ trợ CẢ HAI khóa (Packet.Code và Packet.ID) để tương thích 100%.
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataMapping', 'U') IS NOT NULL
    BEGIN
        DECLARE @TargetShapeJson_101 NVARCHAR(MAX) = N'{
  "header": {
    "source": null,
    "dataTime": {
      "$meta": "Now"
    },
    "partnerCode": {
      "$meta": "PartnerCode",
      "$value": "Partner"
    },
    "packetCode": {
      "$meta": "PacketCode",
      "$value": "101_commonData"
    },
    "serial": {
      "$meta": "Serial"
    }
  },
  "data": [
    {
      "fromLocationMet": {
        "$field": "fromLocationMet"
      },
      "averageSpeed": {
        "$field": "averageSpeed"
      },
      "speedLimit": {
        "$field": "speedLimit"
      },
      "pavementType": {
        "$field": "pavementType"
      },
      "fromLocationKm": {
        "$field": "fromLocationKm"
      },
      "zoneName": {
        "$field": "zoneName"
      },
      "laneCount": {
        "$field": "laneCount"
      },
      "laneId": {
        "$field": "laneId"
      },
      "roadAuthority": {
        "$field": "roadAuthority"
      },
      "routeName": {
        "$field": "routeName"
      },
      "roadType": {
        "$field": "roadType"
      },
      "zoneStatusId": {
        "$field": "zoneStatusId"
      },
      "shoulderWidth": {
        "$field": "shoulderWidth"
      },
      "toLocationMet": {
        "$field": "toLocationMet"
      },
      "zoneId": {
        "$field": "zoneId"
      },
      "trafficInfo": [
        {
          "trafficCondition": {
            "$field": "trafficCondition",
            "$extend": {
              "codeSet": "condition"
            }
          },
          "dataTime": null
        }
      ]
    }
  ]
}';

        DECLARE @PartnerSchemaJson_101 NVARCHAR(MAX) = N'{
  "header": {
    "source": "ITS-TMS",
    "dataTime": "2026-09-14T09:30:00+07:00",
    "partnerCode": "parttner test",
    "packetCode": "101_cbcsj",
    "serial": "Serial"
  },
  "data": [
    {
      "fromLocationMet": 0,
      "averageSpeed": 45.5,
      "speedLimit": 60,
      "pavementType": "Asphalt",
      "fromLocationKm": 10,
      "zoneName": "Khu vực trung tâm",
      "laneCount": 4,
      "laneId": 101,
      "roadAuthority": "Sở Giao thông Vận tải",
      "routeName": "Quốc lộ 1A",
      "roadType": "National Highway",
      "zoneStatusId": 1,
      "shoulderWidth": 2.5,
      "toLocationMet": 500,
      "zoneId": 1001,
      "trafficInfo": [
        {
          "trafficCondition": "Normal",
          "dataTime": "2026-09-14T09:30:00+07:00"
        }
      ]
    }
  ]
}';

        -- 4.1. Mapping chính (ID: 5d18df08-c9cc-4035-89aa-471bcece7948 - Chuẩn Staging & WebVue lưu GUID)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataMapping] WHERE [ID] = '5d18df08-c9cc-4035-89aa-471bcece7948')
        BEGIN
            INSERT INTO [dbo].[ShareDataMapping] 
                ([ID], [Name], [Code], [PartnerId], [DatatypeId], [Direction], [PartnerSchemaJson], [TargetShapeJson], [IsActive], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '5d18df08-c9cc-4035-89aa-471bcece7948',
                N'PARTNER_101COMMONDATA_BOTH',
                'PARTNER_101COMMONDATA_BOTH',
                '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                '1EC77A6E-051D-43BA-A5B9-46A01AFC267A', -- Chuẩn Staging & WebVue: lưu GUID của Packet
                2,               -- Both (Direction = 2)
                @PartnerSchemaJson_101,
                @TargetShapeJson_101,
                1,               -- IsActive = true
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm ShareDataMapping: PARTNER_101COMMONDATA_BOTH (5d18df08-c9cc-4035-89aa-471bcece7948)';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[ShareDataMapping]
            SET [Name] = N'PARTNER_101COMMONDATA_BOTH',
                [Code] = 'PARTNER_101COMMONDATA_BOTH',
                [PartnerId] = '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                [DatatypeId] = '1EC77A6E-051D-43BA-A5B9-46A01AFC267A', -- Chuẩn Staging & WebVue lưu GUID
                [Direction] = 2,
                [PartnerSchemaJson] = @PartnerSchemaJson_101,
                [TargetShapeJson] = @TargetShapeJson_101,
                [IsActive] = 1,
                [IsDelete] = NULL,
                [UpdateTime] = GETDATE()
            WHERE [ID] = '5d18df08-c9cc-4035-89aa-471bcece7948';
            PRINT N'  ✔ Đã cập nhật và kích hoạt lại ShareDataMapping: PARTNER_101COMMONDATA_BOTH (5d18df08-c9cc-4035-89aa-471bcece7948)';
        END;

        -- 4.2. Mapping phụ (ID: 5d18df08-c9cc-4035-89aa-471bcece7949, DatatypeId = '101_commonData' dự phòng)
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataMapping] WHERE [ID] = '5d18df08-c9cc-4035-89aa-471bcece7949')
        BEGIN
            INSERT INTO [dbo].[ShareDataMapping] 
                ([ID], [Name], [Code], [PartnerId], [DatatypeId], [Direction], [PartnerSchemaJson], [TargetShapeJson], [IsActive], [TenantId], [CreateTime], [CreateUId])
            VALUES (
                '5d18df08-c9cc-4035-89aa-471bcece7949',
                N'PARTNER_101COMMONDATA_CODE',
                'PARTNER_101COMMONDATA_CODE',
                '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                '101_commonData', -- Khớp packet.Code
                2,
                @PartnerSchemaJson_101,
                @TargetShapeJson_101,
                1,
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm ShareDataMapping (Code): PARTNER_101COMMONDATA_CODE (5d18df08-c9cc-4035-89aa-471bcece7949)';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[ShareDataMapping]
            SET [Name] = N'PARTNER_101COMMONDATA_CODE',
                [Code] = 'PARTNER_101COMMONDATA_CODE',
                [PartnerId] = '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                [DatatypeId] = '101_commonData',
                [Direction] = 2,
                [PartnerSchemaJson] = @PartnerSchemaJson_101,
                [TargetShapeJson] = @TargetShapeJson_101,
                [IsActive] = 1,
                [IsDelete] = NULL,
                [UpdateTime] = GETDATE()
            WHERE [ID] = '5d18df08-c9cc-4035-89aa-471bcece7949';
            PRINT N'  ✔ Đã cập nhật ShareDataMapping (Code): PARTNER_101COMMONDATA_CODE (5d18df08-c9cc-4035-89aa-471bcece7949)';
        END;
    END;

    -- ============================================================================
    -- 5. ĐĂNG KÝ XUẤT BẢN DỮ LIỆU (ShareDataSubscription)
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataSubscription', 'U') IS NOT NULL
    BEGIN
        -- Subscription đang lỗi trong log: 6815df08-5c07-4627-807f-16d1feda75af
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSubscription] WHERE [ID] = '6815df08-5c07-4627-807f-16d1feda75af')
        BEGIN
            INSERT INTO [dbo].[ShareDataSubscription] (
                [ID], [PartnerId], [SessionId], [Direction], [SerialNbr], [DatatypeId],
                [Mode], [Format], [ScheduleJson], [Priority], [State],
                [IntervalSeconds], [SendOnNewData], [MappingId], [TenantId],
                [NextTimeRun], [LastTimeRun], [CreateTime], [CreateUId]
            ) VALUES (
                '6815df08-5c07-4627-807f-16d1feda75af',
                '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                '6015df08-d3bf-41ef-8843-9513f4654d37',
                0, -- Outbound (0)
                1,
                '1EC77A6E-051D-43BA-A5B9-46A01AFC267A',
                2, -- Continuous (2)
                0, -- JSON (0)
                N'{"Kind":"continuous","IntervalSeconds":30,"StartTime":"06:00","EndTime":"22:00","DaysOfWeek":null,"StartDate":null,"EndDate":null,"DurationMinutes":null}',
                5,
                1, -- Active (1)
                30,
                1, -- SendOnNewData = true
                '5d18df08-c9cc-4035-89aa-471bcece7948',
                'Default',
                GETDATE(), -- Sẵn sàng chạy ngay
                NULL,
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm Subscription: 6815df08-5c07-4627-807f-16d1feda75af (Active)';
        END
        ELSE
        BEGIN
            UPDATE [dbo].[ShareDataSubscription]
            SET [State] = 1,
                [IsDelete] = NULL,
                [PartnerId] = '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                [MappingId] = '5d18df08-c9cc-4035-89aa-471bcece7948',
                [SendOnNewData] = 1,
                [NextTimeRun] = GETDATE(),
                [ProcessingUntil] = NULL -- Xóa lock cũ nếu có
            WHERE [ID] = '6815df08-5c07-4627-807f-16d1feda75af';
            PRINT N'  ✔ Đã cập nhật lại Subscription: 6815df08-5c07-4627-807f-16d1feda75af (nhả lock & kích hoạt)';
        END
    END;

    -- ============================================================================
    -- 6. PHIÊN KẾT NỐI (ShareDataSession)
    -- ============================================================================
    IF OBJECT_ID('dbo.ShareDataSession', 'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[ShareDataSession] WHERE [ID] = '6015df08-d3bf-41ef-8843-9513f4654d37')
        BEGIN
            INSERT INTO [dbo].[ShareDataSession] (
                [ID], [PartnerId], [State], [StartedAt], [AcceptedAt], [TenantId], [CreateTime], [CreateUId]
            ) VALUES (
                '6015df08-d3bf-41ef-8843-9513f4654d37',
                '5415df08-4bc1-4af9-8917-ee55dafa4b79',
                1, -- Active / Connected
                GETDATE(),
                GETDATE(),
                'Default',
                GETDATE(),
                'superadmin'
            );
            PRINT N'  ✔ Đã thêm Session: 6015df08-d3bf-41ef-8843-9513f4654d37';
        END
    END;

    -- ============================================================================
    -- 7. DỮ LIỆU NGUỒN DEMO CHO GÓI 101 (TmsZone, TmsZoneStatus)
    --    Giúp hàm QueryPacket101 trích xuất được bản ghi thật thay vì 0 dòng
    -- ============================================================================
    IF OBJECT_ID('dbo.TmsZone', 'U') IS NOT NULL AND OBJECT_ID('dbo.TmsZoneStatus', 'U') IS NOT NULL
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsZone] WHERE [ID] = '1001')
        BEGIN
            INSERT INTO [dbo].[TmsZone] ([ID], [Name], [FromKmNumber], [FromMetNumber], [ToKmNumber], [ToMetNumber], [LaneId], [MaxSpeed], [TenantId], [CreateTime], [IsDelete])
            VALUES ('1001', N'Đoạn Km10 - Km15 Cao tốc Hữu Nghị - Chi Lăng', 10, 0, 15, 0, 1, 100, 'Default', GETDATE(), NULL);
            PRINT N'  ✔ Đã thêm dữ liệu nguồn mẫu: TmsZone (1001)';
        END;

        IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsZoneStatus] WHERE [ZoneId] = '1001')
        BEGIN
            INSERT INTO [dbo].[TmsZoneStatus] ([ID], [ZoneId], [AverageSpeed], [Condition], [UpdateTime], [TenantId], [CreateTime], [IsDelete])
            VALUES ('ZS-1001', '1001', 65.5, N'NORMAL', GETDATE(), 'Default', GETDATE(), NULL);
            PRINT N'  ✔ Đã thêm dữ liệu nguồn mẫu: TmsZoneStatus (ZS-1001)';
        END;
    END;

    COMMIT TRANSACTION;
    PRINT N'';
    PRINT N'🎉 NẠP SEED DATA THÀNH CÔNG!';
    PRINT N'   Đã có đầy đủ Partner, Packet 101, CodeSet condition, Mapping PARTNER_101COMMONDATA_BOTH và Subscription 6815df08-5c07-4627-807f-16d1feda75af.';
    PRINT N'   Bây giờ ShareDataWorker chạy sẽ tìm thấy phễu lọc và xuất bản dữ liệu bình thường.';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT N'❌ LỖI TRONG QUÁ TRÌNH NẠP SEED DATA:';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
