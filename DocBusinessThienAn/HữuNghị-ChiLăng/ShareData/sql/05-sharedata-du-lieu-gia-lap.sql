USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 05-sharedata-du-lieu-gia-lap.sql
-- DESCRIPTION: Nạp dữ liệu giả lập (Mock Data) phục vụ kiểm thử thông luồng ShareData Worker E2E:
--              1. Gói 104 (Thời tiết - TmsWeather): Trạm WS_HN_01 (Km12+500) và WS_HN_02 (Km45+200).
--              2. Gói 106 (Tải trọng WIM - TmsTrafficData): Bản ghi WIM_MOCK_106_01 và WIM_MOCK_106_02.
--              3. Gói 110 (Sự cố & Biển báo VMS): TmsIncident (INCIDENT_MOCK_110_01),
--                 TmsEquipment (EQUIP_VMS_KM25), VmsCurrent (VMS_CURR_KM25) và TmsWeather (WEATHER_MOCK_110_01).
--              4. Chuẩn bị chiều nhận E0 (cô lập qua gói kiểm thử riêng E2E_INBOUND_TEST):
--                 - Tạo bảng đích dbo.InboundTrafficFlow trên CSDL DEV_ITS10_Inbound.
--                 - Xóa mềm các dòng ShareDataPacketSql cũ nếu từng chạy bản cũ.
--                 - Khởi tạo gói kiểm thử E2E_INBOUND_TEST trong ShareDataPacket
--                   và câu ghi MERGE OPENJSON(@records) trong ShareDataPacketSql.
-- GHI CHÚ:     - Khi ShareDataWorker đang chạy, thao tác chèn vào các bảng nguồn đang bật theo dõi
--                Change Tracking (TmsWeather, TmsTrafficData, TmsIncident, VmsCurrent) sẽ kích hoạt
--                luồng phát hiện thay đổi và gửi gói tin ngay (dùng được cho kịch bản D9).
--              - Mọi dòng script tạo đều ghi nhận CreateUId = N'SHAREDATA-SEED-GIA-LAP'.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST.
--             ⛔ TUYỆT ĐỐI CẤM CHẠY TRÊN CƠ SỞ DỮ LIỆU THẬT (STAGING / PRODUCTION)!
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, chống trùng theo khóa nghiệp vụ hoặc CreateUId).
-- CẬP NHẬT LẦN CUỐI: 19/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (05-sharedata-du-lieu-gia-lap.sql)';
PRINT N'⚠️ CẢNH BÁO: CHỈ DÀNH CHO MÔI TRƯỜNG LOCAL / TEST!';
PRINT N'=============================================================================';
GO

-- -----------------------------------------------------------------------------------------
-- 1. SEED DỮ LIỆU GÓI 104: THỜI TIẾT (TmsWeather)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsWeather]
    WHERE ([RefId] = N'WS_HN_01' OR [ID] = N'WEATHER_MOCK_104_01' OR ([CreateUId] = @SeedUser AND [RefId] = N'WS_HN_01'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 1] Đang seed dữ liệu mẫu trạm thời tiết WS_HN_01, WS_HN_02 (Gói 104)...';
    INSERT INTO [dbo].[TmsWeather] (
        [ID], [TenantId], [RefId], [LocationDetail], [Temperature], [Hudmidity],
        [WindDirection], [WindSpeed], [Rain], [RainHour], [Foresight],
        [TimeDetect], [Description], [ShortDescription], [Type],
        [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, N'WS_HN_01', N'Trạm Km12+500 Hữu Nghị - Chi Lăng', 26.5, 78.0,
        N'Đông Bắc', 12.5, 0.0, 0.0, 1000.0,
        GETDATE(), N'Thời tiết nhiều mây, tầm nhìn tốt', N'CLOUDY', N'1',
        GETDATE(), GETDATE(), NULL, @SeedUser
    ),
    (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, N'WS_HN_02', N'Trạm Km45+200 Hữu Nghị - Chi Lăng', 24.2, 85.0,
        N'Bắc', 18.0, 2.5, 1.2, 800.0,
        GETDATE(), N'Mưa rào nhẹ rải rác', N'LIGHT_RAIN', N'1',
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 1] Đã seed thành công 2 bản ghi TmsWeather (WS_HN_01, WS_HN_02).';
END
ELSE
BEGIN
    PRINT N'  -> [Khối 1] Dữ liệu TmsWeather (WS_HN_01) đã tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 2. SEED DỮ LIỆU GÓI 106: TẢI TRỌNG XE / KÍCH THƯỚC WIM (TmsTrafficData)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsTrafficData]
    WHERE (([CreateUId] = @SeedUser AND [Location] = N'KM15') OR [ID] = N'WIM_MOCK_106_01')
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 2] Đang seed dữ liệu mẫu kiểm tra tải trọng WIM (Gói 106)...';
    INSERT INTO [dbo].[TmsTrafficData] (
        [ID], [TenantId], [DetectTime], [Location], [Lane], [Speed],
        [IpAddress], [Height], [Width], [Length],
        [AlarmHeight], [AlarmWidth], [AlarmLength], [AlarmSpeed],
        [LicensePlate], [Type], [Direction], [ZoneId],
        [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, GETDATE(), N'KM15', N'Lane1', 65.5,
        N'10.10.8.101', 380, 250, 1200,
        0, 0, 0, 0,
        N'29C-12345', N'TRUCK', N'1', NULL,
        GETDATE(), GETDATE(), NULL, @SeedUser
    ),
    (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, GETDATE(), N'KM15', N'Lane2', 58.0,
        N'10.10.8.101', 420, 260, 1650,
        1, 1, 0, 0,
        N'98H-67890', N'CONTAINER', N'1', NULL,
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 2] Đã seed thành công 2 bản ghi TmsTrafficData WIM (WIM_MOCK_106_01, WIM_MOCK_106_02).';
END
ELSE
BEGIN
    PRINT N'  -> [Khối 2] Dữ liệu TmsTrafficData (WIM_MOCK_106_01) đã tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 3. GÓI 110: TRAO ĐỔI THÔNG TIN SỰ CỐ & BIỂN BÁO VMS (TmsIncident, TmsEquipment, VmsCurrent, TmsWeather)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsIncident]
    WHERE ([Code] = N'SC_20261014_01' OR [ID] = N'INCIDENT_MOCK_110_01' OR ([CreateUId] = @SeedUser AND [Code] = N'SC_20261014_01'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 3] Đang seed bản ghi sự cố giao thông (Gói 110)...';
    INSERT INTO [dbo].[TmsIncident] (
        [ID], [TenantId], [Code], [Name], [EventTypeId], [EventGroup],
        [StartDate], [Source], [Location], [KmNumber], [MetNumber],
        [InfluenceScope], [InjuredNumber], [VehicleNumber],
        [State], [Description], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, N'SC_20261014_01', N'Sự cố va quẹt nhẹ xe tải', NULL, N'TRAFFIC',
        GETDATE(), N'0', N'0', 25, 400,
        N'1', 0, 2,
        N'PROCESSING', N'Va quẹt tại Km25+400 hướng Lạng Sơn đi Hà Nội, lưu thông chậm',
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 3] Đã seed thành công TmsIncident (SC_20261014_01).';
END
ELSE
BEGIN
    PRINT N'  -> [Khối 3] Dữ liệu TmsIncident (SC_20261014_01) đã tồn tại, bỏ qua.';
END

DECLARE @VmsEquipmentId NVARCHAR(64);

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsEquipment]
    WHERE ([Code] = N'VMS_KM25_HN' OR [ID] = N'EQUIP_VMS_KM25' OR ([CreateUId] = @SeedUser AND [Code] = N'VMS_KM25_HN'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 3] Đang seed thiết bị VMS tại Km25 (Gói 110)...';
    SET @VmsEquipmentId = LOWER(CONVERT(NVARCHAR(64), NEWID()));
    INSERT INTO [dbo].[TmsEquipment] (
        [ID], [TenantId], [Code], [Name], [EquipmentTypeId], [Ip],
        [KmNumber], [MetNumber], [DirectionId], [Status],
        [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        @VmsEquipmentId, NULL, N'VMS_KM25_HN', N'Biển báo VMS Km25', N'EQUIP_TYPE_VMS', N'10.10.8.201',
        25, 400, 1, 1,
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 3] Đã seed thành công TmsEquipment (VMS_KM25_HN).';
END
ELSE
BEGIN
    SELECT TOP 1 @VmsEquipmentId = [ID]
    FROM [dbo].[TmsEquipment]
    WHERE ([Code] = N'VMS_KM25_HN' OR [ID] = N'EQUIP_VMS_KM25')
      AND [IsDelete] IS NULL;
    PRINT N'  -> [Khối 3] Dữ liệu TmsEquipment (VMS_KM25_HN) đã tồn tại, bỏ qua.';
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[VmsCurrent]
    WHERE (([EquipmentId] = @VmsEquipmentId AND @VmsEquipmentId IS NOT NULL) OR [ID] = N'VMS_CURR_KM25' OR ([CreateUId] = @SeedUser AND [Name] = N'Cảnh báo sự cố Km25'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 3] Đang seed bản tin biển báo VMS (Gói 110)...';
    INSERT INTO [dbo].[VmsCurrent] (
        [ID], [TenantId], [EquipmentId], [Name], [RowData], [ExecutedDate],
        [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, @VmsEquipmentId, N'Cảnh báo sự cố Km25',
        N'CHÚ Ý: SỰ CỐ KM25 - XE GIẢM TỐC ĐỘ DƯỚI 60KM/H',
        GETDATE(), GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 3] Đã seed thành công VmsCurrent.';
END
ELSE
BEGIN
    PRINT N'  -> [Khối 3] Dữ liệu VmsCurrent đã tồn tại, bỏ qua.';
END

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsWeather]
    WHERE ([RefId] = N'WS_KM25' OR [ID] = N'WEATHER_MOCK_110_01' OR ([CreateUId] = @SeedUser AND [RefId] = N'WS_KM25'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 3] Đang seed dữ liệu thời tiết tại Km25 (Gói 110)...';
    INSERT INTO [dbo].[TmsWeather] (
        [ID], [TenantId], [RefId], [LocationDetail], [Temperature], [Hudmidity],
        [WindDirection], [WindSpeed], [Rain], [RainHour], [Foresight],
        [TimeDetect], [Description], [ShortDescription], [Type],
        [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, N'WS_KM25', N'Trạm Km25+400 Hữu Nghị - Chi Lăng', 28.5, 75.0,
        N'Đông Bắc', 12.0, 0.0, 0.0, 10000.0,
        GETDATE(), N'Trời nhiều mây, tầm nhìn thoáng, không mưa', N'CLOUDY', N'1',
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 3] Đã seed thành công TmsWeather (WS_KM25).';
END
ELSE
BEGIN
    PRINT N'  -> [Khối 3] Dữ liệu TmsWeather (WS_KM25) đã tồn tại, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 4. CHUẨN BỊ CHIỀU NHẬN E0: BẢNG ĐÍCH InboundTrafficFlow VÀ GÓI KIỂM THỬ E2E_INBOUND_TEST
-- -----------------------------------------------------------------------------------------
-- 4.1 Tạo bảng đích InboundTrafficFlow trên CSDL DEV_ITS10_Inbound
SET NOCOUNT ON;

IF DB_ID(N'DEV_ITS10_Inbound') IS NOT NULL
BEGIN
    EXEC(N'
    USE [DEV_ITS10_Inbound];
    IF OBJECT_ID(N''[dbo].[InboundTrafficFlow]'', N''U'') IS NULL
    BEGIN
        CREATE TABLE [dbo].[InboundTrafficFlow] (
            [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
            [code] NVARCHAR(64) NULL,
            [recorded_time] DATETIME2 NULL,
            [val] INT NULL
        );
        PRINT N''  -> [Khối 4.1] Đã tạo bảng đích [dbo].[InboundTrafficFlow] trên CSDL DEV_ITS10_Inbound.'';
    END
    ELSE
    BEGIN
        PRINT N''  -> [Khối 4.1] Bảng [dbo].[InboundTrafficFlow] đã tồn tại trên CSDL DEV_ITS10_Inbound, bỏ qua.'';
    END
    ');
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 4.1] CSDL DEV_ITS10_Inbound chưa tồn tại, bỏ qua tạo bảng InboundTrafficFlow (vui lòng khởi tạo CSDL Inbound trước).';
END
GO

-- 4.2 Cô lập kịch bản E0 bằng gói kiểm thử riêng E2E_INBOUND_TEST trên CSDL DEV_ITS10
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

DECLARE @DefaultTenantId NVARCHAR(64) = (
    SELECT TOP 1 [TenantId]
    FROM [dbo].[ShareDataPacket]
    WHERE [TenantId] IS NOT NULL
);
IF @DefaultTenantId IS NULL
    SET @DefaultTenantId = N'Default';

-- 4.2.a Xóa mềm idempotent các dòng ShareDataPacketSql cũ nếu từng chạy bản cũ
IF OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacketSql]
        WHERE (
            ([CreateUId] = @SeedUser AND [PacketCode] IN (N'101', N'101_commonData'))
            OR [ID] IN (N'1500000000901', N'1500000000902') -- dòng do bản cũ của script tạo
        ) AND [IsDelete] IS NULL
    )
    BEGIN
        UPDATE [dbo].[ShareDataPacketSql]
        SET [IsDelete] = GETDATE(),
            [Status] = 0,
            [UpdateTime] = GETDATE()
        WHERE (
            ([CreateUId] = @SeedUser AND [PacketCode] IN (N'101', N'101_commonData'))
            OR [ID] IN (N'1500000000901', N'1500000000902')
        ) AND [IsDelete] IS NULL;
        PRINT N'  -> [Khối 4.2] Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng ShareDataPacketSql cũ của gói 101/101_commonData.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 4.2] Không có dòng ShareDataPacketSql cũ cần xóa mềm, bỏ qua.';
    END
END

-- 4.2.b Khởi tạo gói kiểm thử chiều nhận E2E_INBOUND_TEST trong ShareDataPacket
IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacket]
        WHERE [Code] = N'E2E_INBOUND_TEST' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacket] (
            [ID], [TenantId], [Code], [Name], [PacketVersion], [Description],
            [OrderNo], [Status], [ExtractSql], [SyncMode], [CursorTimeColumn], [MaxLookBackDays],
            [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @DefaultTenantId,
            N'E2E_INBOUND_TEST',
            N'Gói kiểm thử chiều nhận E0 (chỉ local)',
            N'1.0',
            N'Gói kiểm thử chiều nhận độc lập cho kịch bản E0 trên môi trường local/test',
            900,
            1,
            NULL,
            NULL,
            NULL,
            NULL,
            N'Chỉ dùng kiểm thử chiều nhận E0 trên local/test, tách biệt với gói 101_commonData',
            GETDATE(),
            GETDATE(),
            NULL,
            @SeedUser
        );
        PRINT N'  -> [Khối 4.2] Đã thêm gói kiểm thử E2E_INBOUND_TEST vào ShareDataPacket.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 4.2] Gói kiểm thử E2E_INBOUND_TEST đã tồn tại trong ShareDataPacket, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 4.2] Bảng dbo.ShareDataPacket chưa tồn tại trên DEV_ITS10, bỏ qua nạp gói E2E_INBOUND_TEST.';
END

-- 4.2.c Nạp câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST vào ShareDataPacketSql
IF OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacketSql]
        WHERE [PacketCode] = N'E2E_INBOUND_TEST' AND [TableName] = N'dbo.InboundTrafficFlow' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacketSql] (
            [ID], [TenantId], [PacketCode], [Code], [OrderNo], [TableName], [WriteSql],
            [Status], [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @DefaultTenantId,
            N'E2E_INBOUND_TEST',
            N'E2E_INBOUND_TEST',
            1,
            N'dbo.InboundTrafficFlow',
            N'MERGE dbo.InboundTrafficFlow AS target
USING (
    SELECT id, code, recorded_time, val
    FROM OPENJSON(@records)
    WITH (
        id NVARCHAR(64) ''$.id'',
        code NVARCHAR(64) ''$.code'',
        recorded_time DATETIME2 ''$.time'',
        val INT ''$.val''
    )
) AS src
ON target.id = src.id
WHEN MATCHED THEN
    UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
WHEN NOT MATCHED THEN
    INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
OUTPUT $action;',
            1,
            N'Chỉ dùng kiểm thử chiều nhận E0 trên local/test cho gói E2E_INBOUND_TEST',
            GETDATE(),
            GETDATE(),
            NULL,
            @SeedUser
        );
        PRINT N'  -> [Khối 4.2] Đã nạp câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST vào ShareDataPacketSql.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 4.2] Câu ghi MERGE chiều nhận cho gói E2E_INBOUND_TEST đã tồn tại trong ShareDataPacketSql, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 4.2] Bảng dbo.ShareDataPacketSql chưa tồn tại trên DEV_ITS10, bỏ qua nạp câu ghi chiều nhận.';
END
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (05-sharedata-du-lieu-gia-lap.sql)';
PRINT N'=============================================================================';
GO
