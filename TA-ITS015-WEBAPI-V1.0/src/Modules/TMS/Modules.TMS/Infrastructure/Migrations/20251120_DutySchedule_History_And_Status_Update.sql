-- =============================================
-- Migration Script: Duty Schedule History & Status Update
-- Author: System
-- Created Date: 20/11/2025
-- Description: 
--   1. Tạo bảng TmsDutyScheduleHistory để lưu lịch sử thay đổi
--   2. Thêm cột TargetScheduleId vào TmsDutyScheduleChangeRequest
--   3. Cập nhật giá trị trạng thái theo constants mới
-- =============================================

-- ===========================================
-- PART 1: Tạo bảng TmsDutyScheduleHistory
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[TmsDutyScheduleHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[TmsDutyScheduleHistory] (
        [ID] VARCHAR(64) NOT NULL PRIMARY KEY,
        [Code] VARCHAR(64) NULL,
        [DutyScheduleId] VARCHAR(64) NOT NULL,
        [ChangeRequestId] VARCHAR(64) NULL,
        [ActionType] VARCHAR(32) NOT NULL,
        [ActionBy] VARCHAR(64) NOT NULL,
        [AffectedUserId] VARCHAR(64) NULL,
        [OldData] TEXT NULL,
        [NewData] TEXT NULL,
        [Description] VARCHAR(512) NULL,
        [Reason] VARCHAR(512) NULL,
        [IpAddress] VARCHAR(64) NULL,
        [UserAgent] VARCHAR(256) NULL,
        [Remark] VARCHAR(256) NULL,
        
        -- EntityTenant fields
        [TenantId] VARCHAR(64) NULL,
        [CreateTime] DATETIME NULL DEFAULT GETDATE(),
        [CreateBy] VARCHAR(64) NULL,
        [CreateUId] VARCHAR(64) NULL,
        [UpdateTime] DATETIME NULL,
        [UpdateBy] VARCHAR(64) NULL,
        [UpdateUId] VARCHAR(64) NULL,
        [IsDeleted] BIT NULL DEFAULT 0,
        
        CONSTRAINT [FK_TmsDutyScheduleHistory_DutySchedule] FOREIGN KEY ([DutyScheduleId]) 
            REFERENCES [dbo].[TmsDutySchedule]([ID]) ON DELETE CASCADE,
        CONSTRAINT [FK_TmsDutyScheduleHistory_ChangeRequest] FOREIGN KEY ([ChangeRequestId]) 
            REFERENCES [dbo].[TmsDutyScheduleChangeRequest]([ID]) ON DELETE NO ACTION
    );
    
    PRINT 'Created table TmsDutyScheduleHistory';
END
GO

-- Tạo indexes cho performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'index_TmsDutyScheduleHistory_DS' AND object_id = OBJECT_ID('TmsDutyScheduleHistory'))
BEGIN
    CREATE INDEX [index_TmsDutyScheduleHistory_DS] ON [dbo].[TmsDutyScheduleHistory]([DutyScheduleId] ASC);
    PRINT 'Created index on DutyScheduleId';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'index_TmsDutyScheduleHistory_CT' AND object_id = OBJECT_ID('TmsDutyScheduleHistory'))
BEGIN
    CREATE INDEX [index_TmsDutyScheduleHistory_CT] ON [dbo].[TmsDutyScheduleHistory]([CreateTime] DESC);
    PRINT 'Created index on CreateTime';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'index_TmsDutyScheduleHistory_AT' AND object_id = OBJECT_ID('TmsDutyScheduleHistory'))
BEGIN
    CREATE INDEX [index_TmsDutyScheduleHistory_AT] ON [dbo].[TmsDutyScheduleHistory]([ActionType] ASC);
    PRINT 'Created index on ActionType';
END
GO

-- ===========================================
-- PART 2: Thêm cột TargetScheduleId
-- ===========================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TmsDutyScheduleChangeRequest]') AND name = 'TargetScheduleId')
BEGIN
    ALTER TABLE [dbo].[TmsDutyScheduleChangeRequest]
    ADD [TargetScheduleId] VARCHAR(64) NULL;
    
    PRINT 'Added TargetScheduleId column to TmsDutyScheduleChangeRequest';
END
GO

-- Tạo index cho TargetScheduleId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'index_TmsDutyScheduleChangeRequest_TS' AND object_id = OBJECT_ID('TmsDutyScheduleChangeRequest'))
BEGIN
    CREATE INDEX [index_TmsDutyScheduleChangeRequest_TS] ON [dbo].[TmsDutyScheduleChangeRequest]([TargetScheduleId] ASC);
    PRINT 'Created index on TargetScheduleId';
END
GO

-- ===========================================
-- PART 3: Cập nhật giá trị trạng thái
-- ===========================================

-- Cập nhật trạng thái lịch trực (TmsDutySchedule.Status)
UPDATE [dbo].[TmsDutySchedule]
SET [Status] = CASE 
    WHEN LOWER([Status]) = 'draft' THEN 'register'
    WHEN LOWER([Status]) = 'inprogress' THEN 'process'
    WHEN LOWER([Status]) = 'completed' THEN 'finish'
    WHEN LOWER([Status]) = 'cancelled' THEN 'cancel'
    ELSE LOWER([Status])
END
WHERE [Status] IS NOT NULL;

PRINT 'Updated TmsDutySchedule.Status values';
GO

-- Cập nhật vai trò (TmsDutyScheduleDetail.Role)
UPDATE [dbo].[TmsDutyScheduleDetail]
SET [Role] = LOWER([Role])
WHERE [Role] IS NOT NULL;

PRINT 'Updated TmsDutyScheduleDetail.Role to lowercase';
GO

-- Cập nhật trạng thái người tham gia (TmsDutyScheduleDetail.Status)
UPDATE [dbo].[TmsDutyScheduleDetail]
SET [Status] = CASE 
    WHEN LOWER([Status]) = 'active' THEN 'join'
    ELSE LOWER([Status])
END
WHERE [Status] IS NOT NULL;

PRINT 'Updated TmsDutyScheduleDetail.Status values';
GO

-- Cập nhật trạng thái yêu cầu (TmsDutyScheduleChangeRequest.Status)
UPDATE [dbo].[TmsDutyScheduleChangeRequest]
SET [Status] = CASE 
    WHEN LOWER([Status]) = 'pending' THEN 'pending'
    WHEN LOWER([Status]) = 'approved' THEN 'approve'
    WHEN LOWER([Status]) = 'rejected' THEN 'reject'
    WHEN LOWER([Status]) = 'cancelled' THEN 'cancel'
    ELSE LOWER([Status])
END
WHERE [Status] IS NOT NULL;

PRINT 'Updated TmsDutyScheduleChangeRequest.Status values';
GO

-- ===========================================
-- PART 4: Thêm mô tả cho columns (optional)
-- ===========================================
EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'ID lịch trực đích (với loại Transfer - chuyển sang lịch khác)', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'TmsDutyScheduleChangeRequest',
    @level2type = N'COLUMN', @level2name = N'TargetScheduleId';
GO

EXEC sp_addextendedproperty 
    @name = N'MS_Description', 
    @value = N'Lịch sử thay đổi lịch trực - ghi lại mọi thao tác liên quan đến lịch trực', 
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'TmsDutyScheduleHistory';
GO

-- ===========================================
-- PART 5: Verify migration
-- ===========================================
SELECT 
    'TmsDutyScheduleHistory' AS TableName,
    COUNT(*) AS RecordCount
FROM [dbo].[TmsDutyScheduleHistory]
UNION ALL
SELECT 
    'TmsDutyScheduleChangeRequest with TargetScheduleId',
    COUNT(*)
FROM [dbo].[TmsDutyScheduleChangeRequest]
WHERE [TargetScheduleId] IS NOT NULL;

PRINT 'Migration completed successfully!';
GO
