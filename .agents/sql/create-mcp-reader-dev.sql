-- Tạo login SQL Server chỉ-đọc cho MCP/DAB, thay thế 'sa'.
-- Chạy thủ công (SSMS / sqlcmd) nhắm vào: localhost,14333 (dev_its10)
-- Không tự động thực thi — theo đúng quy tắc "Strict Manual SQL Execution" trong CLAUDE.md.

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE LOGIN mcp_reader WITH PASSWORD = '9L6365asEGe90WtmRgxS2MK9Xv57', CHECK_POLICY = ON;
END
GO

USE dev_its10;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE USER mcp_reader FOR LOGIN mcp_reader;
END
GO

-- db_datareader = SELECT trên mọi bảng/view của mọi schema trong DB (dbo + HangFire),
-- không có quyền ghi/sửa/xoá/DDL — đúng tinh thần READ-ONLY hiện DAB đang khai báo.
ALTER ROLE db_datareader ADD MEMBER mcp_reader;
GO

-- Kiểm tra sau khi chạy:
-- SELECT dp.name, r.name AS role
-- FROM sys.database_role_members drm
-- JOIN sys.database_principals dp ON dp.principal_id = drm.member_principal_id
-- JOIN sys.database_principals r ON r.principal_id = drm.role_principal_id
-- WHERE dp.name = 'mcp_reader';
