USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 05-sharedata-du-lieu-gia-lap.sql
-- DESCRIPTION: Nạp dữ liệu giả lập (Mock Data) phục vụ kiểm thử thông luồng ShareData Worker E2E:
--              1. Gói 104 (Thời tiết - TmsWeather): Trạm WS_HN_01 (Km12+500) và WS_HN_02 (Km45+200).
--              2. Gói 106 (Tải trọng WIM - TmsTrafficData): Bản ghi WIM_MOCK_106_01 và WIM_MOCK_106_02.
--              3. Gói 110 (Sự cố & Biển báo VMS): TmsIncident (INCIDENT_MOCK_110_01),
--                 TmsEquipment (EQUIP_VMS_KM25) và VmsCurrent (VMS_CURR_KM25).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST.
--             ⛔ TUYỆT ĐỐI CẤM CHẠY TRÊN CƠ SỞ DỮ LIỆU THẬT (STAGING / PRODUCTION)!
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, IF NOT EXISTS kiểm tra theo khóa).
-- CẬP NHẬT LẦN CUỐI: 16102026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (05-sharedata-du-lieu-gia-lap.sql)';
PRINT N'⚠️ CẢNH BÁO: CHỈ DÀNH CHO MÔI TRƯỜNG LOCAL / TEST!';
PRINT N'=============================================================================';

-- -----------------------------------------------------------------------------------------
-- 1. SEED DỮ LIỆU GÓI 104: THỜI TIẾT (TmsWeather)
-- -----------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsWeather] WHERE [RefId] = 'WS_HN_01')
BEGIN
    PRINT N'  -> Đang seed dữ liệu mẫu trạm thời tiết WS_HN_01, WS_HN_02 (Gói 104)...';
    INSERT INTO [dbo].[TmsWeather] (
        [ID], [TenantId], [RefId], [LocationDetail], [Temperature], [Hudmidity],
        [WindDirection], [WindSpeed], [Rain], [RainHour], [Foresight],
        [TimeDetect], [Description], [ShortDescription], [Type],
        [CreateTime], [UpdateTime], [IsDelete]
    ) VALUES (
        'WEATHER_MOCK_104_01', 1, 'WS_HN_01', N'Trạm Km12+500 Hữu Nghị - Chi Lăng', 26.5, 78.0,
        N'Đông Bắc', 12.5, 0.0, 0.0, 1000.0,
        GETDATE(), N'Thời tiết nhiều mây, tầm nhìn tốt', N'CLOUDY', '1',
        GETDATE(), GETDATE(), NULL
    ),
    (
        'WEATHER_MOCK_104_02', 1, 'WS_HN_02', N'Trạm Km45+200 Hữu Nghị - Chi Lăng', 24.2, 85.0,
        N'Bắc', 18.0, 2.5, 1.2, 800.0,
        GETDATE(), N'Mưa rào nhẹ rải rác', N'LIGHT_RAIN', '1',
        GETDATE(), GETDATE(), NULL
    );
    PRINT N'  -> Đã seed thành công 2 bản ghi TmsWeather (WS_HN_01, WS_HN_02).';
END
ELSE
BEGIN
    PRINT N'  -> Dữ liệu TmsWeather (WS_HN_01) đã tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 2. SEED DỮ LIỆU GÓI 106: TẢI TRỌNG XE / KÍCH THƯỚC WIM (TmsTrafficData)
-- -----------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsTrafficData] WHERE [ID] = 'WIM_MOCK_106_01')
BEGIN
    PRINT N'  -> Đang seed dữ liệu mẫu kiểm tra tải trọng WIM (Gói 106)...';
    INSERT INTO [dbo].[TmsTrafficData] (
        [ID], [TenantId], [DetectTime], [Location], [Lane], [Speed],
        [IpAddress], [Height], [Width], [Length],
        [AlarmHeight], [AlarmWidth], [AlarmLength], [AlarmSpeed],
        [LicensePlate], [Type], [Direction], [ZoneId],
        [CreateTime], [UpdateTime], [IsDelete]
    ) VALUES (
        'WIM_MOCK_106_01', 1, GETDATE(), N'KM15', N'Lane1', 65.5,
        '10.10.8.101', 380, 250, 1200,
        0, 0, 0, 0,
        N'29C-12345', N'TRUCK', N'1', NULL,
        GETDATE(), GETDATE(), NULL
    ),
    (
        'WIM_MOCK_106_02', 1, GETDATE(), N'KM15', N'Lane2', 58.0,
        '10.10.8.101', 420, 260, 1650,
        1, 1, 0, 0,
        N'98H-67890', N'CONTAINER', N'1', NULL,
        GETDATE(), GETDATE(), NULL
    );
    PRINT N'  -> Đã seed thành công 2 bản ghi TmsTrafficData WIM (WIM_MOCK_106_01, WIM_MOCK_106_02).';
END
ELSE
BEGIN
    PRINT N'  -> Dữ liệu TmsTrafficData (WIM_MOCK_106_01) đã tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 3. GÓI 110: TRAO ĐỔI THÔNG TIN SỰ CỐ & BIỂN BÁO VMS (TmsIncident, TmsEquipment, VmsCurrent)
-- -----------------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsIncident] WHERE [ID] = 'INCIDENT_MOCK_110_01')
BEGIN
    PRINT N'  -> Đang seed dữ liệu mẫu sự cố giao thông và biển báo VMS (Gói 110)...';
    INSERT INTO [dbo].[TmsIncident] (
        [ID], [TenantId], [Code], [Name], [EventTypeId], [EventGroup],
        [StartDate], [Source], [Location], [KmNumber], [MetNumber],
        [InfluenceScope], [InjuredNumber], [VehicleNumber],
        [State], [Description], [CreateTime], [UpdateTime], [IsDelete]
    ) VALUES (
        'INCIDENT_MOCK_110_01', 1, 'SC_20261014_01', N'Sự cố va quẹt nhẹ xe tải', NULL, 'TRAFFIC',
        GETDATE(), '0', '0', 25, 400,
        '1', 0, 2,
        'PROCESSING', N'Va quẹt tại Km25+400 hướng Lạng Sơn đi Hà Nội, lưu thông chậm',
        GETDATE(), GETDATE(), NULL
    );

    -- Seed thiết bị VMS tương ứng tại Km25 để phối ghép thông tin cảnh báo
    IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsEquipment] WHERE [ID] = 'EQUIP_VMS_KM25')
    BEGIN
        INSERT INTO [dbo].[TmsEquipment] (
            [ID], [TenantId], [Code], [Name], [EquipmentTypeId], [Ip],
            [KmNumber], [MetNumber], [DirectionId], [Status],
            [CreateTime], [UpdateTime], [IsDelete]
        ) VALUES (
            'EQUIP_VMS_KM25', 1, 'VMS_KM25_HN', N'Biển báo VMS Km25', 'EQUIP_TYPE_VMS', '10.10.8.201',
            25, 400, '1', 1,
            GETDATE(), GETDATE(), NULL
        );
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[VmsCurrent] WHERE [ID] = 'VMS_CURR_KM25')
    BEGIN
        INSERT INTO [dbo].[VmsCurrent] (
            [ID], [TenantId], [EquipmentId], [Name], [RowData], [ExecutedDate],
            [CreateTime], [UpdateTime], [IsDelete]
        ) VALUES (
            'VMS_CURR_KM25', 1, 'EQUIP_VMS_KM25', N'Cảnh báo sự cố Km25',
            N'CHÚ Ý: SỰ CỐ KM25 - XE GIẢM TỐC ĐỘ DƯỚI 60KM/H',
            GETDATE(), GETDATE(), GETDATE(), NULL
        );
    END

    IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsWeather] WHERE [ID] = 'WEATHER_MOCK_110_01')
    BEGIN
        INSERT INTO [dbo].[TmsWeather] (
            [ID], [TenantId], [WeatherStationId], [TimeDetect], [Temperature],
            [Humidity], [WindSpeed], [WindDirection], [Rainfall], [RainfallHour],
            [Visibility], [Description], [WeatherCode], [Status],
            [CreateTime], [UpdateTime], [IsDelete]
        ) VALUES (
            'WEATHER_MOCK_110_01', 1, 'WS_KM25', GETDATE(), 28.5,
            75.0, 12.0, N'Đông Bắc', 0.0, 0.0,
            10000.0, N'Trời nhiều mây, tầm nhìn thoáng, không mưa', 'CLOUDY', 1,
            GETDATE(), GETDATE(), NULL
        );
    END

    PRINT N'  -> Đã seed thành công dữ liệu mẫu TmsIncident, TmsEquipment, VmsCurrent và TmsWeather cho gói 110.';
END
ELSE
BEGIN
    PRINT N'  -> Dữ liệu TmsIncident (INCIDENT_MOCK_110_01) đã tồn tại, bỏ qua.';
END
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (05-sharedata-du-lieu-gia-lap.sql)';
PRINT N'=============================================================================';
