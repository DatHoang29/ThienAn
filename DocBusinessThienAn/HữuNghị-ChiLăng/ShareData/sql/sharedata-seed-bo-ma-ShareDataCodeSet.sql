USE [DEV_ITS015];
GO

-- =========================================================================================
-- FILE: sharedata-seed-bo-ma-ShareDataCodeSet.sql
-- BẢNG GHI: [dbo].[ShareDataCodeSet] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: Nạp danh mục Bộ mã Chuẩn hóa dùng cho quy đổi dữ liệu 2 chiều (Gửi đi / Nhận về)
--              giữa giá trị nội bộ hệ thống ITS/TMS/ETC và mã chuẩn trao đổi với đối tác (PC08, TTDHT...):
--                1. traffic_condition  - Tình trạng giao thông đoạn tuyến (gói 101: trafficCondition)
--                2. lane_code          - Mã làn đường cao tốc (gói 101, 103, 106, 108: laneId / lane)
--                3. device_state       - Trạng thái hoạt động thiết bị ITS (gói 102: deviceState)
--                4. vds_vehicle_class  - Phân loại xe dò xe VDS theo chiều dài (gói 103: vehicleType)
--                5. toll_vehicle_type  - Nhóm phương tiện thu phí ETC (gói 105, 109: vehicleTypeId)
--                6. incident_direction - Hướng tuyến lưu thông (gói 102, 103, 107, 108: direction)
--                7. incident_state     - Trạng thái xử lý sự cố giao thông (gói 107: incidentState)
--                8. incident_source    - Nguồn phát hiện sự cố (gói 107: source)
--                9. incident_type      - Loại sự cố giao thông (gói 107: eventType, đọc từ TmsEventType)
--              Đồng thời dọn mềm dòng thử nghiệm cũ 'condition' (Name = 'condition') nếu chưa có
--              hồ sơ ánh xạ đang sống nào tham chiếu.
-- THỨ TỰ CHẠY: 15 (sau CodeFirst; chạy trước tệp 16 sharedata-seed-anh-xa-ShareDataMapping.sql) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường (đổi tên CSDL ở dòng USE đầu tệp nếu chạy trên DEV_ITS10 local).
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent); không ghi đè bộ mã quản trị đã chỉnh sửa khi @GhiDeBoMaMau = 0.
-- CẬP NHẬT LẦN CUỐI: 06/11/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-bo-ma-ShareDataCodeSet.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[ShareDataCodeSet]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[ShareDataCodeSet] chưa tồn tại (chưa chạy CodeFirst)! Vui lòng chạy CodeFirst trước khi nạp bộ mã chuẩn hóa.';
    SET NOEXEC ON;
END;
GO

DECLARE @GhiDeBoMaMau BIT = 0; -- Đặt = 1 nếu muốn đưa toàn bộ bộ mã mẫu về giá trị chuẩn ban đầu
DECLARE @TenantIdCodeSet NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdCodeSet IS NULL
    SET @TenantIdCodeSet = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. DỌN DÒNG THỬ NGHIỆM CŨ 'condition' (NẾU KHÔNG CÓ HỒ SƠ ÁNH XẠ ĐANG SỐNG THAM CHIẾU)
-- -----------------------------------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[ShareDataMapping]', N'U') IS NOT NULL
BEGIN
    UPDATE cs
    SET cs.[IsDelete] = GETDATE(),
        cs.[UpdateTime] = GETDATE(),
        cs.[UpdateUId] = N'SHAREDATA-SEED-15'
    FROM [dbo].[ShareDataCodeSet] cs
    WHERE cs.[IsDelete] IS NULL
      AND cs.[Code] = N'condition'
      AND (cs.[Name] = N'condition' OR cs.[ValuesJson] LIKE N'%Riby%')
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[ShareDataMapping] m
          WHERE m.[IsDelete] IS NULL
            AND m.[TargetShapeJson] LIKE N'%"condition"%'
      );
    PRINT N'-> 1.1. Đã xóa mềm ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bộ mã thử nghiệm cũ (condition) không còn hồ sơ tham chiếu.';

    -- Trường hợp DB còn hồ sơ ánh xạ cũ đang tham chiếu 'condition': chuẩn hóa lại tên và bảng giá trị để không gãy tham chiếu
    UPDATE cs
    SET cs.[Name] = N'Tình trạng lưu thông (kế thừa)',
        cs.[Remark] = N'Bộ mã kế thừa cho hồ sơ ánh xạ cũ đang tham chiếu mã condition; hồ sơ mới dùng traffic_condition.',
        cs.[Status] = 1,
        cs.[ValuesJson] = N'{"values":[{"sourceValue":"GOOD","partnerValue":"2","displayName":"Thông thoáng"},{"sourceValue":"NORMAL","partnerValue":"1","displayName":"Bình thường"},{"sourceValue":"SLOW","partnerValue":"0","displayName":"Di chuyển chậm"},{"sourceValue":"CONGES","partnerValue":"3","displayName":"Ùn tắc"}],"defaultSourceValue":"NORMAL","defaultPartnerValue":"1"}',
        cs.[UpdateTime] = GETDATE(),
        cs.[UpdateUId] = N'SHAREDATA-SEED-15'
    FROM [dbo].[ShareDataCodeSet] cs
    WHERE cs.[IsDelete] IS NULL
      AND cs.[Code] = N'condition'
      AND (cs.[Name] = N'condition' OR cs.[ValuesJson] LIKE N'%Riby%' OR cs.[Status] IS NULL);
    IF @@ROWCOUNT > 0
        PRINT N'-> 1.2. Đã chuẩn hóa lại bộ mã kế thừa condition đang được hồ sơ cũ tham chiếu.';
END;

-- -----------------------------------------------------------------------------------------
-- 2. KHAI BÁO DANH MỤC BỘ MÃ CHUẨN HÓA (1-1 GIỮA GIÁ TRỊ HỆ THỐNG VÀ GIÁ TRỊ CHUẨN ĐỐI TÁC)
-- -----------------------------------------------------------------------------------------
DECLARE @SeedCodeSets TABLE (
    [OrderNo]    INT            NOT NULL,
    [Code]       NVARCHAR(64)   NOT NULL,
    [Name]       NVARCHAR(128)  NOT NULL,
    [Remark]     NVARCHAR(256)  NULL,
    [ValuesJson] NVARCHAR(MAX)  NOT NULL
);

INSERT INTO @SeedCodeSets ([OrderNo], [Code], [Name], [Remark], [ValuesJson])
VALUES
    -- 1. Gói 101 (PC08, TTDHT đang dùng trực tiếp): Tình trạng giao thông đoạn tuyến (TmsZoneStatus.Condition)
    (1, N'traffic_condition',
     N'Tình trạng giao thông đoạn tuyến',
     N'Quy đổi tình trạng lưu thông gói 101_commonData (TmsZoneStatus.Condition <-> mã chuẩn đối tác PC08, TTDHT).',
     N'{"values":[{"sourceValue":"GOOD","partnerValue":"FREE_FLOW","displayName":"Thông thoáng"},{"sourceValue":"NORMAL","partnerValue":"NORMAL","displayName":"Bình thường"},{"sourceValue":"SLOW","partnerValue":"SLOW","displayName":"Di chuyển chậm"},{"sourceValue":"CONGES","partnerValue":"CONGESTED","displayName":"Ùn tắc"}],"defaultSourceValue":"NORMAL","defaultPartnerValue":"NORMAL"}'),

    -- 2. Gói 101, 103, 106, 108 (PC08, TTDHT đang dùng trực tiếp): Mã làn đường cao tốc (SysConfigData tms_lane / TmsZone.LaneId)
    (2, N'lane_code',
     N'Mã làn đường cao tốc',
     N'Quy đổi mã làn đường gói 101, 103, 106, 108 (TmsZone.LaneId / SysConfigData tms_lane <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"0","partnerValue":"ALL_LANES","displayName":"Tất cả các làn"},{"sourceValue":"1","partnerValue":"LANE_1","displayName":"Làn 1"},{"sourceValue":"2","partnerValue":"LANE_2","displayName":"Làn 2"},{"sourceValue":"3","partnerValue":"LANE_3","displayName":"Làn 3"}],"defaultSourceValue":"1","defaultPartnerValue":"LANE_1"}'),

    -- 3. Gói 102: Trạng thái hoạt động thiết bị ITS (SysConfigData equipment_state / CctvDevice.DeviceState)
    (3, N'device_state',
     N'Trạng thái hoạt động thiết bị ITS',
     N'Quy đổi trạng thái thiết bị gói 102_cctvData (SysConfigData equipment_state <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"0","partnerValue":"INACTIVE","displayName":"Không hoạt động"},{"sourceValue":"1","partnerValue":"ONLINE","displayName":"Đang hoạt động"},{"sourceValue":"2","partnerValue":"ERROR","displayName":"Lỗi thiết bị"},{"sourceValue":"3","partnerValue":"DAMAGED","displayName":"Hư hỏng"},{"sourceValue":"4","partnerValue":"DISCONNECTED","displayName":"Mất kết nối"},{"sourceValue":"5","partnerValue":"POWER_FAULT","displayName":"Lỗi nguồn"},{"sourceValue":"7","partnerValue":"PIXEL_FAULT","displayName":"Lỗi điểm ảnh"}],"defaultSourceValue":"4","defaultPartnerValue":"DISCONNECTED"}'),

    -- 4. Gói 103: Phân loại xe dò xe VDS theo chiều dài (SysConfigData vehicle_type / TmsTrafficData.VehicleType)
    (4, N'vds_vehicle_class',
     N'Phân loại xe dò xe VDS',
     N'Quy đổi nhóm xe theo chiều dài của thiết bị VDS gói 103_vdsData (SysConfigData vehicle_type <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"short","partnerValue":"CLASS_S","displayName":"Xe con / Xe dài dưới 6m"},{"sourceValue":"medium","partnerValue":"CLASS_M","displayName":"Xe tải trung / Xe khách 6m - 12m"},{"sourceValue":"long","partnerValue":"CLASS_L","displayName":"Xe tải nặng / Container trên 12m"}],"defaultSourceValue":"short","defaultPartnerValue":"CLASS_S"}'),

    -- 5. Gói 105, 109: Nhóm phương tiện thu phí ETC/RFID (TollTransactionOut/In.VehicleTypeId)
    (5, N'toll_vehicle_type',
     N'Nhóm phương tiện thu phí ETC',
     N'Quy đổi 5 nhóm phương tiện thu phí gói 105_rfidData, 109_etcData (VehicleTypeId <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"1","partnerValue":"TYPE_1","displayName":"Nhóm 1 - Xe dưới 12 ghế, xe tải dưới 2 tấn"},{"sourceValue":"2","partnerValue":"TYPE_2","displayName":"Nhóm 2 - Xe 12 đến 30 ghế, xe tải 2 đến dưới 4 tấn"},{"sourceValue":"3","partnerValue":"TYPE_3","displayName":"Nhóm 3 - Xe từ 31 ghế trở lên, xe tải 4 đến dưới 10 tấn"},{"sourceValue":"4","partnerValue":"TYPE_4","displayName":"Nhóm 4 - Xe tải 10 đến dưới 18 tấn, container 20ft"},{"sourceValue":"5","partnerValue":"TYPE_5","displayName":"Nhóm 5 - Xe tải từ 18 tấn trở lên, container 40ft"}],"defaultSourceValue":"1","defaultPartnerValue":"TYPE_1"}'),

    -- 6. Gói 102, 103, 107, 108: Hướng tuyến lưu thông (SysConfigData direction_config_type)
    (6, N'incident_direction',
     N'Hướng tuyến lưu thông',
     N'Quy đổi hướng tuyến gói 102, 103, 107, 108 (SysConfigData direction_config_type <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"0","partnerValue":"BOTH","displayName":"Cả hai chiều"},{"sourceValue":"1","partnerValue":"CL_HN","displayName":"Chi Lăng → Hữu Nghị"},{"sourceValue":"2","partnerValue":"HN_CL","displayName":"Hữu Nghị → Chi Lăng"}],"defaultSourceValue":"1","defaultPartnerValue":"CL_HN"}'),

    -- 7. Gói 107: Trạng thái xử lý sự cố giao thông (SysConfigData tms_incident_status)
    (7, N'incident_state',
     N'Trạng thái sự cố giao thông',
     N'Quy đổi trạng thái xử lý sự cố gói 107_incidentData (SysConfigData tms_incident_status <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"0","partnerValue":"SKIPPED","displayName":"Bỏ qua"},{"sourceValue":"1","partnerValue":"REGISTERED","displayName":"Mới ghi nhận"},{"sourceValue":"2","partnerValue":"VERIFIED","displayName":"Đã xác nhận"},{"sourceValue":"3","partnerValue":"PROCESSING","displayName":"Đang xử lý"},{"sourceValue":"4","partnerValue":"FINISHED","displayName":"Hoàn thành"},{"sourceValue":"5","partnerValue":"CANCELED","displayName":"Đã hủy"}],"defaultSourceValue":"1","defaultPartnerValue":"REGISTERED"}'),

    -- 8. Gói 107: Nguồn phát hiện sự cố (SysConfigData tms_source_detect)
    (8, N'incident_source',
     N'Nguồn phát hiện sự cố',
     N'Quy đổi nguồn tiếp nhận sự cố gói 107_incidentData (SysConfigData tms_source_detect <-> mã chuẩn đối tác).',
     N'{"values":[{"sourceValue":"user","partnerValue":"USER","displayName":"Vận hành viên nhập"},{"sourceValue":"vds","partnerValue":"VDS","displayName":"Thiết bị dò xe VDS"},{"sourceValue":"fa","partnerValue":"FA","displayName":"Hệ thống báo cháy"},{"sourceValue":"api","partnerValue":"API","displayName":"Chia sẻ từ đối tác"}],"defaultSourceValue":"vds","defaultPartnerValue":"VDS"}');

-- 9. Gói 107: Loại sự cố giao thông (đọc trực tiếp danh mục TmsEventType của CSDL hiện tại để khớp đúng GUID)
IF OBJECT_ID(N'[dbo].[TmsEventType]', N'U') IS NOT NULL
BEGIN
    DECLARE @IncidentTypeValues NVARCHAR(MAX);
    DECLARE @DefaultEventId NVARCHAR(64);
    DECLARE @DefaultEventCode NVARCHAR(64);

    ;WITH DedupEventTypes AS (
        SELECT
            CAST(et.[ID] AS NVARCHAR(64))   AS [sourceValue],
            LTRIM(RTRIM(et.[Code]))         AS [partnerValue],
            ISNULL(et.[Name], et.[Code])    AS [displayName],
            ROW_NUMBER() OVER (
                PARTITION BY LOWER(LTRIM(RTRIM(et.[Code])))
                ORDER BY CASE WHEN LOWER(LTRIM(RTRIM(et.[Code]))) = N'collision' THEN 0 ELSE 1 END, et.[CreateTime], et.[ID]
            ) AS [rnCode],
            ROW_NUMBER() OVER (
                PARTITION BY LOWER(LTRIM(RTRIM(CAST(et.[ID] AS NVARCHAR(64)))))
                ORDER BY et.[CreateTime], et.[Code]
            ) AS [rnId]
        FROM [dbo].[TmsEventType] et
        WHERE et.[IsDelete] IS NULL
          AND et.[ID] IS NOT NULL
          AND et.[Code] IS NOT NULL
          AND LTRIM(RTRIM(et.[Code])) <> N''
    )
    SELECT @IncidentTypeValues = (
        SELECT [sourceValue], [partnerValue], [displayName]
        FROM DedupEventTypes
        WHERE [rnCode] = 1 AND [rnId] = 1
        ORDER BY [partnerValue]
        FOR JSON PATH
    );

    IF @IncidentTypeValues IS NOT NULL
    BEGIN
        SELECT TOP 1
            @DefaultEventId = CAST(et.[ID] AS NVARCHAR(64)),
            @DefaultEventCode = LTRIM(RTRIM(et.[Code]))
        FROM [dbo].[TmsEventType] et
        WHERE et.[IsDelete] IS NULL
          AND et.[ID] IS NOT NULL
          AND et.[Code] IS NOT NULL
          AND LTRIM(RTRIM(et.[Code])) <> N''
        ORDER BY CASE WHEN LOWER(LTRIM(RTRIM(et.[Code]))) IN (N'collision', N'accident', N'other') THEN 0 ELSE 1 END,
                 LTRIM(RTRIM(et.[Code]));

        INSERT INTO @SeedCodeSets ([OrderNo], [Code], [Name], [Remark], [ValuesJson])
        VALUES (
            9,
            N'incident_type',
            N'Loại sự cố giao thông',
            N'Quy đổi loại sự cố gói 107_incidentData (TmsEventType.ID <-> TmsEventType.Code).',
            N'{"values":' + @IncidentTypeValues
            + N',"defaultSourceValue":"' + STRING_ESCAPE(ISNULL(@DefaultEventId, N''), 'json')
            + N'","defaultPartnerValue":"' + STRING_ESCAPE(ISNULL(@DefaultEventCode, N''), 'json') + N'"}'
        );
    END;
END;

-- -----------------------------------------------------------------------------------------
-- 3. CẬP NHẬT KHI BẬT CỜ @GhiDeBoMaMau = 1 HOẶC DÒNG THIẾU TRẠNG THÁI BẬT
-- -----------------------------------------------------------------------------------------
IF @GhiDeBoMaMau = 1
BEGIN
    UPDATE cs
    SET cs.[Name] = src.[Name],
        cs.[Remark] = src.[Remark],
        cs.[Status] = 1,
        cs.[ValuesJson] = src.[ValuesJson],
        cs.[UpdateTime] = GETDATE(),
        cs.[UpdateUId] = N'SHAREDATA-SEED-15'
    FROM [dbo].[ShareDataCodeSet] cs
    JOIN @SeedCodeSets src ON src.[Code] = cs.[Code]
    WHERE cs.[IsDelete] IS NULL;
    PRINT N'-> 2.1. (@GhiDeBoMaMau = 1) Đã cập nhật lại ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bộ mã mẫu.';
END;

-- -----------------------------------------------------------------------------------------
-- 4. THÊM CÁC BỘ MÃ CHUẨN HÓA CÒN THIẾU (IsDelete ĐỂ NULL)
-- -----------------------------------------------------------------------------------------
INSERT INTO [dbo].[ShareDataCodeSet]
    ([ID], [Code], [Name], [Remark], [Status], [ValuesJson], [TenantId], [CreateTime], [CreateUId])
SELECT
    LOWER(CONVERT(NVARCHAR(64), NEWID())),
    src.[Code],
    src.[Name],
    src.[Remark],
    1,
    src.[ValuesJson],
    @TenantIdCodeSet,
    GETDATE(),
    N'SHAREDATA-SEED-15'
FROM @SeedCodeSets src
WHERE NOT EXISTS (
    SELECT 1
    FROM [dbo].[ShareDataCodeSet] cs
    WHERE cs.[Code] = src.[Code]
      AND cs.[IsDelete] IS NULL
)
ORDER BY src.[OrderNo];
PRINT N'-> 2.2. Đã thêm mới ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' bộ mã chuẩn hóa còn thiếu.';
GO

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-bo-ma-ShareDataCodeSet.sql';
PRINT N'=============================================================================';
GO
