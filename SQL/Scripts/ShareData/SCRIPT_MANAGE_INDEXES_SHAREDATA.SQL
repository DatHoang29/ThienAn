-- =================================================================================
-- SCRIPT MẪU TẠO VÀ XÓA INDEX CHO SQL SERVER (SHARE DATA MODULE)
-- Author: Đạt
-- Created Date: 30/07/2026
-- Mô tả: File SQL mẫu quản lý Index an toàn (Idempotent - Kiểm tra tồn tại trước khi tạo/xóa)
-- =================================================================================

USE [TA_ITS015_WebAPI_Dev]; -- Đổi tên Database của bạn tại đây nếu cần
GO

-- =================================================================================
-- 1. BẢNG EshSubscription (Quản lý hợp đồng kết nối / Background Worker)
-- =================================================================================

-- 1.1 Single-Column Index: Cột State (Lọc nhanh các hợp đồng Active cho CronJob)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshSubscription_State' AND object_id = OBJECT_ID('EshSubscription'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EshSubscription_State] 
    ON [dbo].[EshSubscription] ([State] ASC);
    PRINT '✅ Created Index: IX_EshSubscription_State';
END
GO

-- 1.2 Compound Index Mẫu: (PartnerId -> Direction -> State)
-- Tuân thủ Quy tắc tiền tố bên trái (Leftmost Prefix Rule): PartnerId đứng đầu
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshSubscription_Partner_Direction' AND object_id = OBJECT_ID('EshSubscription'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EshSubscription_Partner_Direction] 
    ON [dbo].[EshSubscription] ([PartnerId] ASC, [Direction] ASC, [State] ASC);
    PRINT '✅ Created Compound Index: IX_EshSubscription_Partner_Direction';
END
GO

-- ---------------------------------------------------------------------------------
-- [MẪU DROP INDEX] Xóa Index an toàn khi không dùng nữa
-- ---------------------------------------------------------------------------------
/*
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshSubscription_Partner_Direction' AND object_id = OBJECT_ID('EshSubscription'))
BEGIN
    DROP INDEX [IX_EshSubscription_Partner_Direction] ON [dbo].[EshSubscription];
    PRINT '🗑️ Dropped Index: IX_EshSubscription_Partner_Direction';
END
GO
*/


-- =================================================================================
-- 2. BẢNG EshFieldMapping (Chi tiết ánh xạ trường dữ liệu)
-- =================================================================================

-- 2.1 Single-Column / FK Index: Cột MappingProfileId & OrderNo
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshFieldMapping_ProfileId' AND object_id = OBJECT_ID('EshFieldMapping'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EshFieldMapping_ProfileId] 
    ON [dbo].[EshFieldMapping] ([MappingProfileId] ASC, [OrderNo] ASC);
    PRINT '✅ Created Index: IX_EshFieldMapping_ProfileId';
END
GO


-- =================================================================================
-- 3. BẢNG EshExportLog (Nhật ký kết xuất dữ liệu)
-- =================================================================================

-- 3.1 Compound Index: (SubscriptionId -> Status -> ExportedAt DESC)
-- Tối ưu cho truy vấn tìm Log thành công mới nhất phục vụ Deduplication Check Hash
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshExportLog_Sub_Status_ExportedAt' AND object_id = OBJECT_ID('EshExportLog'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EshExportLog_Sub_Status_ExportedAt] 
    ON [dbo].[EshExportLog] ([SubscriptionId] ASC, [Status] ASC, [ExportedAt] DESC);
    PRINT '✅ Created Compound Index: IX_EshExportLog_Sub_Status_ExportedAt';
END
GO


-- =================================================================================
-- 4. BẢNG EshMappingProfile (Hồ sơ ánh xạ)
-- =================================================================================

-- 4.1 Compound Index: Validate Rule R1 (PartnerId -> DatatypeId -> Direction -> IsActive)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_EshMappingProfile_RuleR1' AND object_id = OBJECT_ID('EshMappingProfile'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_EshMappingProfile_RuleR1] 
    ON [dbo].[EshMappingProfile] ([PartnerId] ASC, [DatatypeId] ASC, [Direction] ASC, [IsActive] ASC);
    PRINT '✅ Created Compound Index: IX_EshMappingProfile_RuleR1';
END
GO


-- =================================================================================
-- 5. SCRIPT KIỂM TRA TOÀN BỘ INDEX ĐANG CÓ TRONG BẢNG (UTILITY DIAGNOSTIC QUERY)
-- =================================================================================
/*
SELECT 
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    STRING_AGG(c.name, ', ') WITHIN GROUP (ORDER BY ic.key_ordinal) AS IndexColumns
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE t.name LIKE 'Esh%' AND i.is_primary_key = 0
GROUP BY t.name, i.name, i.type_desc;
*/
