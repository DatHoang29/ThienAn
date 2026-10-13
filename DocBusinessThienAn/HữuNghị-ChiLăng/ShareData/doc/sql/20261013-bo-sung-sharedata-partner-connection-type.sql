-- =========================================================================================
-- Bổ sung cột ConnectionType và ConnectionConfig vào bảng ShareDataPartner
-- Ngày tạo: 13/10/2026
-- Phân hệ: ShareData (ESHARE)
-- =========================================================================================
USE [DEV_ITS10];
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ShareDataPartner' AND COLUMN_NAME = 'ConnectionType'
)
BEGIN
    ALTER TABLE [dbo].[ShareDataPartner]
    ADD [ConnectionType] NVARCHAR(64) NULL;
    PRINT 'Đã bổ sung cột ConnectionType vào bảng ShareDataPartner.';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'ShareDataPartner' AND COLUMN_NAME = 'ConnectionConfig'
)
BEGIN
    ALTER TABLE [dbo].[ShareDataPartner]
    ADD [ConnectionConfig] NVARCHAR(MAX) NULL;
    PRINT 'Đã bổ sung cột ConnectionConfig vào bảng ShareDataPartner.';
END
GO

