-- Tạo login SQL Server chỉ-đọc cho MCP/DAB, thay thế 'sa'.
-- Chạy thủ công (SSMS / sqlcmd) nhắm vào: localhost,14333 (test)
-- Không tự động thực thi — theo đúng quy tắc "Strict Manual SQL Execution" trong CLAUDE.md.

USE master;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE LOGIN mcp_reader WITH PASSWORD = 'YSxm5epDgRPsKOHpWDJUViXvGopA', CHECK_POLICY = ON;
END
GO

USE test;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'mcp_reader')
BEGIN
    CREATE USER mcp_reader FOR LOGIN mcp_reader;
END
GO

ALTER ROLE db_datareader ADD MEMBER mcp_reader;
GO
