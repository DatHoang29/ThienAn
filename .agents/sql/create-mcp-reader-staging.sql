-- Tạo login SQL Server chỉ-đọc cho MCP/DAB, thay thế 'sa'.
-- Chạy thủ công (SSMS / sqlcmd) nhắm vào: 10.10.8.30 (dev_its10)
-- ⚠️ Server 10.10.8.30 có mật khẩu 'sa' đã bị lộ (đã commit vào git) — sau khi tạo login
-- mcp_reader và xác nhận DAB dùng được, PHẢI đổi luôn mật khẩu 'sa' của server này.
-- Không tự động thực thi — theo đúng quy tắc "Strict Manual SQL Execution" trong CLAUDE.md.

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE LOGIN mcp_reader WITH PASSWORD = 'EXyENvX7CEW5jGaAdfkHJyq7vzaI', CHECK_POLICY = ON;
END
GO

USE dev_its10;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE USER mcp_reader FOR LOGIN mcp_reader;
END
GO

ALTER ROLE db_datareader ADD MEMBER mcp_reader;
GO
