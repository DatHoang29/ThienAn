USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-du-lieu-gia-lap.sql
-- DESCRIPTION: Nạp dữ liệu giả lập (Mock Data) phục vụ kiểm thử thông luồng ShareData Worker E2E:
--              1. Gói 104 (Thời tiết - TmsWeather): Trạm WS_HN_01 (Km12+500) và WS_HN_02 (Km45+200).
--              2. Gói 106 (Tải trọng WIM - TmsTrafficData): Bản ghi WIM_MOCK_106_01 và WIM_MOCK_106_02.
--              3. Gói 110 (Sự cố & Biển báo VMS): TmsIncident (INCIDENT_MOCK_110_01),
--                 TmsEquipment (EQUIP_VMS_KM25), VmsCurrent (VMS_CURR_KM25) và TmsWeather (WEATHER_MOCK_110_01).
--              4. Chuẩn bị chiều nhận E0 (cô lập qua gói kiểm thử riêng E2E_INBOUND_TEST):
--                 - Tạo bảng đích dbo.InboundTrafficFlow trên CSDL DEV_ITS10_Inbound.
--                 - Xóa mềm các dòng ShareDataPacketSql cũ nếu từng chạy bản cũ.
--                 - Khởi tạo gói kiểm thử E2E_INBOUND_TEST trong ShareDataPacket (OrderNo = 101)
--                   sao chép TemplateSql & ExtractSql từ gói 101 và câu ghi MERGE trong ShareDataPacketSql.
--              5. Gói 111 (InterCenter - TmsSignalLog): 3 bản ghi SIGNAL_MOCK_111_01..03.
--              6. Dữ liệu giả lập 150 dòng trùng timestamp (TmsTrafficData) kiểm thử phân trang con trỏ @lastKey.
--              7. Dữ liệu giả lập 3 dòng cũ hơn Lookback Window (TmsTrafficData) kiểm thử bộ lọc thời gian.
--              8. Khối dọn dẹp dữ liệu giả lập (@XoaDuLieuGiaLap = 1): Xóa toàn bộ dữ liệu mock đã tạo.
-- GHI CHÚ:     - Khi ShareDataWorker đang chạy, thao tác chèn vào các bảng nguồn đang bật theo dõi
--                Change Tracking (TmsWeather, TmsTrafficData, TmsIncident, VmsCurrent) sẽ kích hoạt
--                luồng phát hiện thay đổi và gửi gói tin ngay (dùng được cho kịch bản D9).
--              - Mọi dòng script tạo đều ghi nhận CreateUId = N'SHAREDATA-SEED-GIA-LAP'.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: ⚠️ CHỈ CHẠY TRÊN LOCAL VÀ TEST.
--             Khối 4 & Khối 8 được phép chạy trên Staging để kiểm thử chiều nhận E0.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent, chống trùng theo khóa nghiệp vụ hoặc CreateUId).
-- CẬP NHẬT LẦN CUỐI: 21/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (sharedata-du-lieu-gia-lap.sql)';
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
-- 3. GÓI 107 & 110: TRAO ĐỔI THÔNG TIN SỰ CỐ & BIỂN BÁO VMS (TmsIncident, TmsEquipment, VmsCurrent, TmsWeather)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';
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

DECLARE @IncidentEventTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[TmsEventType]
    WHERE [IsDelete] IS NULL AND [Status] = 1
    ORDER BY CASE
        WHEN [Code] = N'collision' THEN 0
        WHEN [Code] = N'accident' THEN 1
        ELSE 2
    END, [CreateTime]
);

DECLARE @IncidentSource NVARCHAR(64) = COALESCE((
    SELECT TOP 1 d.[Code]
    FROM [dbo].[SysConfigData] d
    INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
    WHERE t.[Code] = N'tms_source_detect'
      AND d.[IsDelete] IS NULL AND t.[IsDelete] IS NULL
      AND d.[Status] = 1
    ORDER BY CASE WHEN d.[Code] = N'vds' THEN 0 WHEN d.[Code] = N'user' THEN 1 ELSE 2 END, d.[OrderNo]
), N'vds');

DECLARE @IncidentState NVARCHAR(64) = COALESCE((
    SELECT TOP 1 d.[Code]
    FROM [dbo].[SysConfigData] d
    INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
    WHERE t.[Code] = N'tms_incident_status'
      AND d.[IsDelete] IS NULL AND t.[IsDelete] IS NULL
      AND d.[Status] = 1
    ORDER BY CASE WHEN d.[Code] = N'3' THEN 0 WHEN d.[Name] LIKE N'%Đang xử lý%' THEN 1 ELSE 2 END, d.[OrderNo]
), N'3');

DECLARE @IncidentInfluenceScope NVARCHAR(64) = COALESCE((
    SELECT TOP 1 d.[Code]
    FROM [dbo].[SysConfigData] d
    INNER JOIN [dbo].[SysConfigType] t ON d.[ConfigTypeId] = t.[ID]
    WHERE t.[Code] = N'direction_config_type'
      AND d.[IsDelete] IS NULL AND t.[IsDelete] IS NULL
      AND d.[Status] = 1
    ORDER BY CASE WHEN d.[Code] = N'1' THEN 0 ELSE 1 END, d.[OrderNo]
), N'1');

DECLARE @IncidentEquipmentId NVARCHAR(64) = COALESCE((
    SELECT TOP 1 [ID]
    FROM [dbo].[TmsEquipment]
    WHERE [IsDelete] IS NULL AND [Status] = 1
    ORDER BY CASE
        WHEN [Code] = N'FIX-01' THEN 0
        WHEN [Code] = N'VMS_KM25_HN' THEN 1
        ELSE 2
    END, [CreateTime]
), @VmsEquipmentId);

IF NOT EXISTS (
    SELECT 1 FROM [dbo].[TmsIncident]
    WHERE ([Code] = N'SC_20261014_01' OR [ID] = N'INCIDENT_MOCK_110_01' OR ([CreateUId] = @SeedUser AND [Code] = N'SC_20261014_01'))
      AND [IsDelete] IS NULL
)
BEGIN
    PRINT N'  -> [Khối 3] Đang seed bản ghi sự cố giao thông (Gói 107 & 110)...';
    INSERT INTO [dbo].[TmsIncident] (
        [ID], [TenantId], [Code], [Name], [EventTypeId], [EventGroup],
        [StartDate], [Source], [Location], [KmNumber], [MetNumber],
        [InfluenceScope], [InjuredNumber], [VehicleNumber], [EquipmentId],
        [State], [Description], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
    ) VALUES (
        LOWER(CONVERT(NVARCHAR(64), NEWID())), NULL, N'SC_20261014_01', N'Sự cố va quẹt nhẹ xe tải', @IncidentEventTypeId, N'TRAFFIC',
        GETDATE(), @IncidentSource, N'0', 25, 400,
        @IncidentInfluenceScope, 0, 2, @IncidentEquipmentId,
        @IncidentState, N'Va quẹt tại Km25+400 hướng Lạng Sơn đi Hà Nội, lưu thông chậm',
        GETDATE(), GETDATE(), NULL, @SeedUser
    );
    PRINT N'  -> [Khối 3] Đã seed thành công TmsIncident (SC_20261014_01).';
END
ELSE
BEGIN
    UPDATE [dbo].[TmsIncident]
    SET [EventTypeId]    = COALESCE([EventTypeId], @IncidentEventTypeId),
        [Source]         = CASE WHEN [Source] IS NULL OR [Source] = N'0' THEN @IncidentSource ELSE [Source] END,
        [State]          = CASE WHEN [State] IS NULL OR [State] = N'PROCESSING' THEN @IncidentState ELSE [State] END,
        [InfluenceScope] = COALESCE(NULLIF([InfluenceScope], N''), @IncidentInfluenceScope),
        [EquipmentId]    = COALESCE([EquipmentId], @IncidentEquipmentId),
        [UpdateTime]     = GETDATE()
    WHERE ([Code] = N'SC_20261014_01' OR [ID] = N'INCIDENT_MOCK_110_01')
      AND [IsDelete] IS NULL
      AND (
          [EventTypeId] IS NULL
          OR [Source] IS NULL OR [Source] = N'0'
          OR [State] IS NULL OR [State] = N'PROCESSING'
          OR [EquipmentId] IS NULL
      );

    IF @@ROWCOUNT > 0
        PRINT N'  -> [Khối 3] Đã chuẩn hóa từ điển cho bản ghi TmsIncident (SC_20261014_01) sẵn có.';
    ELSE
        PRINT N'  -> [Khối 3] Dữ liệu TmsIncident (SC_20261014_01) đã tồn tại và chuẩn từ điển, bỏ qua.';
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

-- 4.2.b Khởi tạo/cập nhật gói kiểm thử chiều nhận E2E_INBOUND_TEST trong ShareDataPacket
IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NOT NULL
BEGIN
    DECLARE @Template101 NVARCHAR(MAX), @Extract101 NVARCHAR(MAX);
    SELECT TOP 1
        @Template101 = [TemplateSql],
        @Extract101 = [ExtractSql]
    FROM [dbo].[ShareDataPacket]
    WHERE ([Code] = N'101' OR [Code] LIKE N'101[_]%')
      AND [IsDelete] IS NULL
    ORDER BY CASE WHEN [Code] = N'101' THEN 0 ELSE 1 END, [ID];

    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[ShareDataPacket]
        WHERE [Code] = N'E2E_INBOUND_TEST' AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[ShareDataPacket] (
            [ID], [TenantId], [Code], [Name], [PacketVersion], [Description],
            [OrderNo], [Status], [TemplateSql], [ExtractSql], [CursorTimeColumn], [MaxLookBackDays],
            [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            LOWER(CONVERT(NVARCHAR(64), NEWID())),
            @DefaultTenantId,
            N'E2E_INBOUND_TEST',
            N'Gói kiểm thử chiều nhận E0 (chỉ local)',
            N'1.0',
            N'Gói kiểm thử chiều nhận độc lập cho kịch bản E0 trên môi trường local/test',
            101,
            1,
            @Template101,
            @Extract101,
            NULL,
            NULL,
            N'Chỉ dùng kiểm thử chiều nhận E0 trên local/test, tách biệt với gói 101_commonData',
            GETDATE(),
            GETDATE(),
            NULL,
            @SeedUser
        );
        PRINT N'  -> [Khối 4.2] Đã thêm gói kiểm thử E2E_INBOUND_TEST vào ShareDataPacket (OrderNo = 101).';
    END
    ELSE
    BEGIN
        UPDATE [dbo].[ShareDataPacket]
        SET [OrderNo] = 101,
            [TemplateSql] = COALESCE([TemplateSql], @Template101),
            [ExtractSql] = COALESCE([ExtractSql], @Extract101),
            [UpdateTime] = GETDATE()
        WHERE [Code] = N'E2E_INBOUND_TEST'
          AND [IsDelete] IS NULL
          AND ([OrderNo] <> 101 OR [TemplateSql] IS NULL OR [ExtractSql] IS NULL);

        PRINT N'  -> [Khối 4.2] Gói kiểm thử E2E_INBOUND_TEST đã tồn tại trong ShareDataPacket (đồng bộ OrderNo = 101 và SQL).';
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

-- -----------------------------------------------------------------------------------------
-- 5. SEED DỮ LIỆU GÓI 111: INTERCENTER (TmsSignalLog)
-- -----------------------------------------------------------------------------------------
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF OBJECT_ID(N'[dbo].[TmsSignalLog]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[TmsSignalLog]
        WHERE [ID] IN (N'SIGNAL_MOCK_111_01', N'SIGNAL_MOCK_111_02', N'SIGNAL_MOCK_111_03')
           OR ([CreateUId] = @SeedUser AND [EquipmentId] = N'VMS_HN_01')
    )
    BEGIN
        PRINT N'  -> [Khối 5] Đang seed 3 bản ghi mẫu TmsSignalLog (Gói 111)...';
        INSERT INTO [dbo].[TmsSignalLog] (
            [ID], [TenantId], [EquipmentId], [OldData], [NewData],
            [State], [Status], [Priority], [ExecuteBy], [ExecuteTime],
            [Remark], [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            N'SIGNAL_MOCK_111_01', NULL, N'VMS_HN_01', N'{"speed":80}', N'{"speed":60,"msg":"MUA TO GIAM TOC DO"}',
            N'EXECUTED', 1, 1, N'operator_hn', GETDATE(),
            N'Lệnh điều khiển hiển thị biển VMS giảm tốc độ', GETDATE(), GETDATE(), NULL, @SeedUser
        ),
        (
            N'SIGNAL_MOCK_111_02', NULL, N'VMS_HN_01', N'{"speed":60}', N'{"speed":80,"msg":"DUONG THONG THOANG"}',
            N'EXECUTED', 1, 2, N'operator_hn', GETDATE(),
            N'Lệnh khôi phục tốc độ bình thường', GETDATE(), GETDATE(), NULL, @SeedUser
        ),
        (
            N'SIGNAL_MOCK_111_03', NULL, N'VMS_HN_02', NULL, N'{"alert":"SU CO KM25 PHAN LUONG"}',
            N'PENDING', 1, 1, N'system_auto', GETDATE(),
            N'Cảnh báo phân luồng khẩn cấp từ trung tâm Chi Lăng', GETDATE(), GETDATE(), NULL, @SeedUser
        );
        PRINT N'  -> [Khối 5] Đã seed 3 bản ghi TmsSignalLog thành công.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 5] Dữ liệu mẫu TmsSignalLog đã tồn tại, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 5] Bảng dbo.TmsSignalLog chưa tồn tại trên DEV_ITS10, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 6. SEED DỮ LIỆU PHÂN TRANG CON TRỎ @lastKey (150 BẢN GHI TRÙNG MỐC THỜI GIAN)
-- -----------------------------------------------------------------------------------------
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[TmsTrafficData]
        WHERE [CreateUId] = @SeedUser AND [LocationDetail] = N'MOCK_LASTKEY_TEST'
    )
    BEGIN
        PRINT N'  -> [Khối 6] Đang nạp 150 bản ghi trùng mốc thời gian vào TmsTrafficData để kiểm thử @lastKey...';
        
        DECLARE @FixedTime DATETIME2 = DATEADD(HOUR, -2, GETDATE());
        DECLARE @i INT = 1;

        WHILE @i <= 150
        BEGIN
            INSERT INTO [dbo].[TmsTrafficData] (
                [ID], [TenantId], [EquipmentId], [PlateNumber], [VehicleType],
                [Speed], [LaneNo], [DetectTime], [LocationDetail],
                [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
            ) VALUES (
                N'LKSAME_ID_' + RIGHT('000' + CAST(@i AS NVARCHAR(3)), 3),
                NULL,
                N'WIM_HN_01',
                N'12A-' + RIGHT('00000' + CAST(@i AS NVARCHAR(5)), 5),
                1,
                65.0,
                1,
                @FixedTime,
                N'MOCK_LASTKEY_TEST',
                @FixedTime,
                @FixedTime,
                NULL,
                @SeedUser
            );
            SET @i = @i + 1;
        END;
        PRINT N'  -> [Khối 6] Đã nạp thành công 150 bản ghi kiểm thử phân trang con trỏ @lastKey.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 6] 150 bản ghi kiểm thử @lastKey đã tồn tại, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 6] Bảng dbo.TmsTrafficData chưa tồn tại trên DEV_ITS10, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 7. SEED DỮ LIỆU CŨ HƠN LOOKBACK WINDOW ĐỂ KIỂM THỬ BỘ LỌC THỜI GIAN
-- -----------------------------------------------------------------------------------------
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [dbo].[TmsTrafficData]
        WHERE [CreateUId] = @SeedUser AND [LocationDetail] = N'MOCK_LOOKBACK_EXCLUDE'
    )
    BEGIN
        PRINT N'  -> [Khối 7] Đang nạp 3 bản ghi cũ hơn Lookback Window vào TmsTrafficData...';
        
        DECLARE @OldTime DATETIME2 = DATEADD(DAY, -10, GETDATE());

        INSERT INTO [dbo].[TmsTrafficData] (
            [ID], [TenantId], [EquipmentId], [PlateNumber], [VehicleType],
            [Speed], [LaneNo], [DetectTime], [LocationDetail],
            [CreateTime], [UpdateTime], [IsDelete], [CreateUId]
        ) VALUES (
            N'LKOLD_ID_001', NULL, N'WIM_HN_01', N'12A-99901', 1, 60.0, 1, @OldTime, N'MOCK_LOOKBACK_EXCLUDE', @OldTime, @OldTime, NULL, @SeedUser
        ),
        (
            N'LKOLD_ID_002', NULL, N'WIM_HN_01', N'12A-99902', 2, 55.0, 1, @OldTime, N'MOCK_LOOKBACK_EXCLUDE', @OldTime, @OldTime, NULL, @SeedUser
        ),
        (
            N'LKOLD_ID_003', NULL, N'WIM_HN_01', N'12A-99903', 1, 70.0, 2, @OldTime, N'MOCK_LOOKBACK_EXCLUDE', @OldTime, @OldTime, NULL, @SeedUser
        );
        PRINT N'  -> [Khối 7] Đã nạp thành công 3 bản ghi cũ hơn Lookback Window.';
    END
    ELSE
    BEGIN
        PRINT N'  -> [Khối 7] Dữ liệu kiểm thử Lookback Window đã tồn tại, bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'⚠️ [Khối 7] Bảng dbo.TmsTrafficData chưa tồn tại trên DEV_ITS10, bỏ qua.';
END
GO

-- -----------------------------------------------------------------------------------------
-- 8. KHỐI DỌN DẸP DỮ LIỆU GIẢ LẬP (KÍCH HOẠT KHI @XoaDuLieuGiaLap = 1)
-- -----------------------------------------------------------------------------------------
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @XoaDuLieuGiaLap BIT = 0; -- Đặt = 1 khi cần dọn sạch dữ liệu mock
DECLARE @SeedUser NVARCHAR(64) = N'SHAREDATA-SEED-GIA-LAP';

IF @XoaDuLieuGiaLap = 1
BEGIN
    PRINT N'=============================================================================';
    PRINT N'BẮT ĐẦU DỌN DẸP TOÀN BỘ DỮ LIỆU GIẢ LẬP (@XoaDuLieuGiaLap = 1)...';
    PRINT N'=============================================================================';

    IF OBJECT_ID(N'[dbo].[TmsWeather]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[TmsWeather] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong TmsWeather: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[TmsTrafficData]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[TmsTrafficData] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong TmsTrafficData: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[TmsIncident]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[TmsIncident] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong TmsIncident: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[TmsEquipment]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[TmsEquipment] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong TmsEquipment: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[VmsCurrent]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[VmsCurrent] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong VmsCurrent: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[TmsSignalLog]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[TmsSignalLog] WHERE [CreateUId] = @SeedUser;
        PRINT N'  -> Đã xóa dữ liệu giả lập trong TmsSignalLog: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[ShareDataPacket]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[ShareDataPacket] WHERE [Code] = N'E2E_INBOUND_TEST';
        PRINT N'  -> Đã xóa gói kiểm thử E2E_INBOUND_TEST trong ShareDataPacket: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF OBJECT_ID(N'[dbo].[ShareDataPacketSql]', N'U') IS NOT NULL
    BEGIN
        DELETE FROM [dbo].[ShareDataPacketSql] WHERE [PacketCode] = N'E2E_INBOUND_TEST';
        PRINT N'  -> Đã xóa câu ghi E2E_INBOUND_TEST trong ShareDataPacketSql: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bản ghi.';
    END

    IF DB_ID(N'DEV_ITS10_Inbound') IS NOT NULL
    BEGIN
        EXEC(N'
            USE [DEV_ITS10_Inbound];
            IF OBJECT_ID(N''[dbo].[InboundTrafficFlow]'', N''U'') IS NOT NULL
            BEGIN
                DELETE FROM [dbo].[InboundTrafficFlow];
                PRINT N''  -> Đã xóa dữ liệu bảng [DEV_ITS10_Inbound].[dbo].[InboundTrafficFlow].'';
            END
        ');
    END

    PRINT N'✅ Đã hoàn tất dọn dẹp dữ liệu giả lập!';
END
ELSE
BEGIN
    PRINT N'  -> Biến @XoaDuLieuGiaLap = 0 (mặc định), không kích hoạt khối dọn dẹp.';
END
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN NẠP DỮ LIỆU GIẢ LẬP SHAREDATA (sharedata-du-lieu-gia-lap.sql)';
PRINT N'=============================================================================';
GO
