-- ============================================================================
-- MODULE: SHARE DATA (ESHARE V1)
-- FILE: SEED_DEMO_SHARE_DATA.SQL
-- DESCRIPTION: Nạp Dữ Liệu Mẫu Dem0 Cho 6 Đối Tác & 6 Subscriptions Đầu Ra (Outbound)
-- AUTHOR: Antigravity AI
-- CREATED DATE: 2026-08-05
-- ============================================================================

USE [dev_its10];
GO

SET NOCOUNT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- 1. XOÁ DỮ LIỆU CŨ TRÁNH XUNG ĐỘT
    DELETE FROM [dbo].[EshFieldMapping] WHERE [MappingProfileId] IN (SELECT [ID] FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%');
    DELETE FROM [dbo].[EshSubscription] WHERE [SerialNbr] LIKE 'SUB-DEMO-%';
    DELETE FROM [dbo].[EshMappingProfile] WHERE [Code] LIKE 'MP-DEMO-%';
    DELETE FROM [dbo].[EshDataSource] WHERE [Code] LIKE 'DS-DEMO-%';
    DELETE FROM [dbo].[EshPartner] WHERE [Code] LIKE 'PTN-DEMO-%';

    -- 2. THÊM 6 ĐỐI TÁC DEMO (PTN-DEMO-001 ➔ PTN-DEMO-006)
    INSERT INTO [dbo].[EshPartner] ([ID], [Code], [Name], [SecretKey], [Status], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('P-DEMO-001', 'PTN-DEMO-001', N'Sở Giao thông Vận tải Hà Nội',   'SECRET_KEY_SGTVT_HN', 'ENABLED', '1', GETDATE(), 0),
    ('P-DEMO-002', 'PTN-DEMO-002', N'Cục Cảnh sát Giao thông (C08)', 'SECRET_KEY_C08_BCA',  'ENABLED', '1', GETDATE(), 0),
    ('P-DEMO-003', 'PTN-DEMO-003', N'Cục Đường bộ Việt Nam',          'SECRET_KEY_CDB_VN',   'ENABLED', '1', GETDATE(), 0),
    ('P-DEMO-004', 'PTN-DEMO-004', N'Trung tâm Khí tượng Thủy văn',   'SECRET_KEY_KTTV_NB',  'ENABLED', '1', GETDATE(), 0),
    ('P-DEMO-005', 'PTN-DEMO-005', N'Công ty Đầu tư Thu phí VETC',     'SECRET_KEY_VETC_ETC', 'ENABLED', '1', GETDATE(), 0),
    ('P-DEMO-006', 'PTN-DEMO-006', N'Công ty Thu phí Tự động VDTC',    'SECRET_KEY_VDTC_ETC', 'ENABLED', '1', GETDATE(), 0);

    -- 3. THÊM 6 NGUỒN DỮ LIỆU (DS-DEMO-001 ➔ DS-DEMO-006)
    INSERT INTO [dbo].[EshDataSource] ([ID], [Code], [Name], [Kind], [Table], [QueryText], [TopN], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('DS-DEMO-001', 'DS-DEMO-001', N'Nguồn Dữ liệu Giao thông (FieldPicker)', 'FieldPicker', 'TmsTrafficData', NULL, 100, '1', GETDATE(), 0),
    ('DS-DEMO-002', 'DS-DEMO-002', N'Nguồn Dữ liệu Đăng kiểm Xe (SavedQuery)', 'SavedQuery', NULL, N'SELECT LocationCode AS VehicleNo, VehicleCount AS Speed FROM TmsTrafficData', 50, '1', GETDATE(), 0),
    ('DS-DEMO-003', 'DS-DEMO-003', N'Nguồn Dữ liệu Sự cố Giao thông (FieldPicker)', 'FieldPicker', 'TmsTrafficData', NULL, 100, '1', GETDATE(), 0),
    ('DS-DEMO-004', 'DS-DEMO-004', N'Nguồn Dữ liệu Khí tượng Thủy văn (FieldPicker)', 'FieldPicker', 'TmsTrafficData', NULL, 100, '1', GETDATE(), 0),
    ('DS-DEMO-005', 'DS-DEMO-005', N'Nguồn Dữ liệu Thu phí Không dừng (SavedQuery)', 'SavedQuery', NULL, N'SELECT LocationCode AS StationCode, VehicleCount AS FeeAmount FROM TmsTrafficData', 100, '1', GETDATE(), 0),
    ('DS-DEMO-006', 'DS-DEMO-006', N'Nguồn Dữ liệu Sự kiện Xe (SavedQuery)', 'SavedQuery', NULL, N'SELECT LocationCode AS PlateNo, LogTime AS EventTime FROM TmsTrafficData', 100, '1', GETDATE(), 0);

    -- 4. THÊM 6 HỒ SƠ ÁNH XẠ (MP-DEMO-001 ➔ MP-DEMO-006) VỚI DATATYPE_ID KHÔNG TRÙNG NHAU
    INSERT INTO [dbo].[EshMappingProfile] ([ID], [Code], [Name], [PartnerId], [DatatypeId], [Direction], [DataSourceId], [IsActive], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('MP-DEMO-001', 'MP-DEMO-001', N'Ánh xạ Giao thông SGTVT',     'P-DEMO-001', 'SHARE_TRAFFIC_01', 'OUTBOUND', 'DS-DEMO-001', 1, '1', GETDATE(), 0),
    ('MP-DEMO-002', 'MP-DEMO-002', N'Ánh xạ Đăng kiểm C08',          'P-DEMO-002', 'SHARE_XE_01',      'OUTBOUND', 'DS-DEMO-002', 1, '1', GETDATE(), 0),
    ('MP-DEMO-003', 'MP-DEMO-003', N'Ánh xạ Sự cố Cục Đường bộ',    'P-DEMO-003', 'SHARE_SUCO_01',    'OUTBOUND', 'DS-DEMO-003', 1, '1', GETDATE(), 0),
    ('MP-DEMO-004', 'MP-DEMO-004', N'Ánh xạ KTTV Trung tâm NB',     'P-DEMO-004', 'SHARE_KTTV_01',    'OUTBOUND', 'DS-DEMO-004', 1, '1', GETDATE(), 0),
    ('MP-DEMO-005', 'MP-DEMO-005', N'Ánh xạ Thu phí VETC',          'P-DEMO-005', 'SHARE_THUPHI_01',  'OUTBOUND', 'DS-DEMO-005', 1, '1', GETDATE(), 0),
    ('MP-DEMO-006', 'MP-DEMO-006', N'Ánh xạ Sự kiện VDTC',         'P-DEMO-006', 'SHARE_EVENT_01',   'OUTBOUND', 'DS-DEMO-006', 1, '1', GETDATE(), 0);

    -- 5. THÊM CHI TIẾT ÁNH XẠ TRƯỜNG (EshFieldMapping)
    INSERT INTO [dbo].[EshFieldMapping] ([ID], [MappingProfileId], [SourceKey], [TargetKey], [DefaultValue], [IsRequired], [OrderNo], [TenantId], [CreateTime], [IsDelete])
    VALUES
    -- MP-DEMO-001 (SGTVT)
    ('FM-DEMO-001', 'MP-DEMO-001', 'LocationCode', 'ma_tram', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-002', 'MP-DEMO-001', 'VehicleCount', 'luu_luong_xe', '0', 0, 2, '1', GETDATE(), 0),
    -- MP-DEMO-002 (C08)
    ('FM-DEMO-003', 'MP-DEMO-002', 'VehicleNo',    'bien_so_xe', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-004', 'MP-DEMO-002', 'Speed',        'toc_do_do', '0', 0, 2, '1', GETDATE(), 0),
    -- MP-DEMO-003 (Cục Đường Bộ)
    ('FM-DEMO-005', 'MP-DEMO-003', 'LocationCode', 'ma_vi_tri_su_co', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-006', 'MP-DEMO-003', 'VehicleCount', 'muc_do_nghiem_trong', '1', 0, 2, '1', GETDATE(), 0),
    -- MP-DEMO-004 (KTTV)
    ('FM-DEMO-007', 'MP-DEMO-004', 'LocationCode', 'ma_tram_kttv', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-008', 'MP-DEMO-004', 'VehicleCount', 'luong_mua_mm', '0', 0, 2, '1', GETDATE(), 0),
    -- MP-DEMO-005 (VETC)
    ('FM-DEMO-009', 'MP-DEMO-005', 'StationCode',  'ma_tram_thu_phi', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-010', 'MP-DEMO-005', 'FeeAmount',   'so_tien_vnd', '0', 0, 2, '1', GETDATE(), 0),
    -- MP-DEMO-006 (VDTC)
    ('FM-DEMO-011', 'MP-DEMO-006', 'PlateNo',      'bien_so_qua_tram', NULL, 1, 1, '1', GETDATE(), 0),
    ('FM-DEMO-012', 'MP-DEMO-006', 'EventTime',    'thoi_gian_quyet', NULL, 0, 2, '1', GETDATE(), 0);

    -- 6. THÊM 6 SUBCRIPTIONS ĐẦU RA HOẠT ĐỘNG (SUB-DEMO-001 ➔ SUB-DEMO-006)
    INSERT INTO [dbo].[EshSubscription] 
    ([ID], [SerialNbr], [PartnerId], [DatatypeId], [MappingProfileId], [DataSourceId], [Direction], [Mode], [State], [RunStatus], [ProcessLockId], [IntervalSeconds], [NextTimeRun], [TenantId], [CreateTime], [IsDelete])
    VALUES
    ('SUB-D-001', 'SUB-DEMO-001', 'P-DEMO-001', 'SHARE_TRAFFIC_01', 'MP-DEMO-001', 'DS-DEMO-001', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0),
    ('SUB-D-002', 'SUB-DEMO-002', 'P-DEMO-002', 'SHARE_XE_01',      'MP-DEMO-002', 'DS-DEMO-002', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0),
    ('SUB-D-003', 'SUB-DEMO-003', 'P-DEMO-003', 'SHARE_SUCO_01',    'MP-DEMO-003', 'DS-DEMO-003', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0),
    ('SUB-D-004', 'SUB-DEMO-004', 'P-DEMO-004', 'SHARE_KTTV_01',    'MP-DEMO-004', 'DS-DEMO-004', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0),
    ('SUB-D-005', 'SUB-DEMO-005', 'P-DEMO-005', 'SHARE_THUPHI_01',  'MP-DEMO-005', 'DS-DEMO-005', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0),
    ('SUB-D-006', 'SUB-DEMO-006', 'P-DEMO-006', 'SHARE_EVENT_01',   'MP-DEMO-006', 'DS-DEMO-006', 'OUTBOUND', 'BATCH', 'ACTIVE', 'IDLE', NULL, 30, GETDATE(), '1', GETDATE(), 0);

    -- 7. THÊM DỮ LIỆU MẪU BẢNG TRAFFIC DATA (TmsTrafficData)
    IF NOT EXISTS (SELECT 1 FROM [dbo].[TmsTrafficData])
    BEGIN
        INSERT INTO [dbo].[TmsTrafficData] ([ID], [LocationCode], [VehicleCount], [DetectTime], [LogTime], [TenantId], [CreateTime], [IsDelete])
        VALUES
        ('TR-001', 'TRAM_CAU_DIEN',   1250, GETDATE(), GETDATE(), '1', GETDATE(), 0),
        ('TR-002', 'TRAM_PHAP_VAN',   3400, GETDATE(), GETDATE(), '1', GETDATE(), 0),
        ('TR-003', 'TRAM_NOI_BAI',    2100, GETDATE(), GETDATE(), '1', GETDATE(), 0),
        ('TR-004', 'TRAM_HA_DONG',    1850, GETDATE(), GETDATE(), '1', GETDATE(), 0),
        ('TR-005', 'TRAM_LONG_BIEN',  2900, GETDATE(), GETDATE(), '1', GETDATE(), 0);
    END;

    COMMIT TRANSACTION;
    PRINT N'✅ Nạp dữ liệu mẫu ShareData cho 6 Subscriptions thành công 100%!';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
    RAISERROR(@ErrMsg, 16, 1);
END CATCH;
GO
