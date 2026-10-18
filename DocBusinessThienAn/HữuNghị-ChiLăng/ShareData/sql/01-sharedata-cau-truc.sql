-- =========================================================================================
-- FILE: 01-sharedata-cau-truc.sql
-- DESCRIPTION: Bổ sung và chuẩn hóa cấu trúc cột/bảng cho phân hệ ShareData (ESHARE):
--              - CSDL chính DEV_ITS10:
--                + Đảm bảo các cột ConnectionType (INT NULL - enum), ConnectionConfig tồn tại trên ShareDataPartner.
--                + Kiểm tra xung đột và chuyển đổi cấu hình địa chỉ/cổng/endpoint từ JSON
--                  ConnectionConfig sang các cột riêng (Address, Port, EndPointApiUrl).
--                + Dọn dẹp các key địa chỉ/cổng khỏi ConnectionConfig bằng JSON_MODIFY.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 18/10/2026
-- =========================================================================================

-- =========================================================================================
-- CSDL CHÍNH [DEV_ITS10] - CHUẨN HÓA CỘT ĐỐI TÁC ShareDataPartner
-- =========================================================================================
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'CHUẨN HÓA CỘT ĐỐI TÁC ShareDataPartner TRÊN DEV_ITS10';
PRINT N'=============================================================================';

-- 0. Đảm bảo các cột ConnectionType và ConnectionConfig đã tồn tại trên ShareDataPartner
IF OBJECT_ID(N'dbo.ShareDataPartner', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.ShareDataPartner', N'ConnectionType') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ShareDataPartner] ADD [ConnectionType] INT NULL;
        PRINT N'  -> Đã bổ sung cột ConnectionType kiểu INT vào bảng ShareDataPartner.';
    END
    ELSE
    BEGIN
        DECLARE @partnerColType NVARCHAR(128);
        SELECT @partnerColType = tp.name
        FROM sys.columns c
        JOIN sys.types tp ON c.user_type_id = tp.user_type_id
        WHERE c.object_id = OBJECT_ID(N'dbo.ShareDataPartner')
          AND c.name = N'ConnectionType';

        IF @partnerColType <> N'int'
        BEGIN
            PRINT N'  -> Phát hiện cột ConnectionType kiểu ' + @partnerColType + N', đang chuẩn hóa dữ liệu sang số nguyên (1..5)...';
            UPDATE [dbo].[ShareDataPartner]
            SET [ConnectionType] = CASE
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'API', N'REST', N'HTTP', N'WEBAPI', N'1') THEN 1
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FILE', N'DISK', N'NAS', N'2') THEN 2
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FTP', N'3') THEN 3
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'FTPS', N'4') THEN 4
                WHEN UPPER(LTRIM(RTRIM(CAST([ConnectionType] AS NVARCHAR(64))))) IN (N'KAFKA', N'5') THEN 5
                ELSE NULL
            END;
            ALTER TABLE [dbo].[ShareDataPartner] ALTER COLUMN [ConnectionType] INT NULL;
            PRINT N'  -> Đã chuyển đổi cột ConnectionType sang INT NULL trên ShareDataPartner.';
        END
        ELSE
            PRINT N'  -> Cột ConnectionType (INT) đã tồn tại trên ShareDataPartner, bỏ qua.';
    END

    IF COL_LENGTH(N'dbo.ShareDataPartner', N'ConnectionConfig') IS NULL
    BEGIN
        ALTER TABLE [dbo].[ShareDataPartner] ADD [ConnectionConfig] NVARCHAR(MAX) NULL;
        PRINT N'  -> Đã bổ sung cột ConnectionConfig vào bảng ShareDataPartner.';
    END
    ELSE
        PRINT N'  -> Cột ConnectionConfig đã tồn tại trên ShareDataPartner, bỏ qua.';
END
ELSE
BEGIN
    PRINT N'⚠️ Bảng dbo.ShareDataPartner không tồn tại trên CSDL DEV_ITS10!';
END
GO

IF OBJECT_ID(N'dbo.ShareDataPartner', N'U') IS NOT NULL
    AND COL_LENGTH(N'dbo.ShareDataPartner', N'ConnectionConfig') IS NOT NULL
BEGIN
    PRINT N'  -> Kiểm tra và chuyển đổi địa chỉ từ ConnectionConfig sang cột ShareDataPartner...';

    -- 1. Cảnh báo các đối tác có giá trị mâu thuẫn giữa cột và JSON
    DECLARE @ConflictCount INT = 0;
    
    SELECT @ConflictCount = COUNT(1)
    FROM dbo.ShareDataPartner
    WHERE ConnectionConfig IS NOT NULL
      AND ISJSON(ConnectionConfig) = 1
      AND (
          (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.Address') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.Address'))
          OR (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.BaseUrl') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.BaseUrl'))
          OR (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.Host') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.Host'))
          OR (Port IS NOT NULL AND TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT) IS NOT NULL AND Port <> TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT))
          OR (EndPointApiUrl IS NOT NULL AND EndPointApiUrl <> '' AND JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl') IS NOT NULL AND EndPointApiUrl <> JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl'))
      );

    IF @ConflictCount > 0
    BEGIN
        PRINT N'⚠️ CẢNH BÁO: Phát hiện ' + CAST(@ConflictCount AS NVARCHAR(10)) + N' đối tác có giá trị mâu thuẫn giữa cột và JSON. Danh sách chi tiết:';
        SELECT ID, Code, Name, Address, Port, EndPointApiUrl, ConnectionConfig
        FROM dbo.ShareDataPartner
        WHERE ConnectionConfig IS NOT NULL
          AND ISJSON(ConnectionConfig) = 1
          AND (
              (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.Address') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.Address'))
              OR (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.BaseUrl') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.BaseUrl'))
              OR (Address IS NOT NULL AND Address <> '' AND JSON_VALUE(ConnectionConfig, '$.Host') IS NOT NULL AND Address <> JSON_VALUE(ConnectionConfig, '$.Host'))
              OR (Port IS NOT NULL AND TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT) IS NOT NULL AND Port <> TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT))
              OR (EndPointApiUrl IS NOT NULL AND EndPointApiUrl <> '' AND JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl') IS NOT NULL AND EndPointApiUrl <> JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl'))
          );
    END;

    -- 2. Cập nhật các cột còn trống từ giá trị trong JSON
    -- 2.1 Cập nhật Address nếu cột đang rỗng và JSON có BaseUrl, Host, Address, hoặc IP
    UPDATE dbo.ShareDataPartner
    SET Address = COALESCE(
        NULLIF(JSON_VALUE(ConnectionConfig, '$.Address'), ''),
        NULLIF(JSON_VALUE(ConnectionConfig, '$.BaseUrl'), ''),
        NULLIF(JSON_VALUE(ConnectionConfig, '$.Host'), ''),
        NULLIF(JSON_VALUE(ConnectionConfig, '$.IP'), '')
    )
    WHERE ConnectionConfig IS NOT NULL
      AND ISJSON(ConnectionConfig) = 1
      AND (Address IS NULL OR Address = '')
      AND (
          JSON_VALUE(ConnectionConfig, '$.Address') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.BaseUrl') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Host') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.IP') IS NOT NULL
      );
    PRINT N'  -> Đã cập nhật cột Address từ JSON cho các bản ghi còn trống: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

    -- 2.2 Cập nhật Port nếu cột đang rỗng và JSON có Port hợp lệ
    UPDATE dbo.ShareDataPartner
    SET Port = TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT)
    WHERE ConnectionConfig IS NOT NULL
      AND ISJSON(ConnectionConfig) = 1
      AND Port IS NULL
      AND TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT) IS NOT NULL
      AND TRY_CAST(JSON_VALUE(ConnectionConfig, '$.Port') AS INT) BETWEEN 1 AND 65535;
    PRINT N'  -> Đã cập nhật cột Port từ JSON cho các bản ghi còn trống: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

    -- 2.3 Cập nhật EndPointApiUrl nếu cột đang rỗng và JSON có EndPointApiUrl, Endpoint, hoặc Path
    UPDATE dbo.ShareDataPartner
    SET EndPointApiUrl = COALESCE(
        NULLIF(JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl'), ''),
        NULLIF(JSON_VALUE(ConnectionConfig, '$.Endpoint'), ''),
        NULLIF(JSON_VALUE(ConnectionConfig, '$.Path'), '')
    )
    WHERE ConnectionConfig IS NOT NULL
      AND ISJSON(ConnectionConfig) = 1
      AND (EndPointApiUrl IS NULL OR EndPointApiUrl = '')
      AND (
          JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Endpoint') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Path') IS NOT NULL
      );
    PRINT N'  -> Đã cập nhật cột EndPointApiUrl từ JSON cho các bản ghi còn trống: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

    -- 3. Xoá các key dành riêng cho cột khỏi ConnectionConfig bằng JSON_MODIFY
    UPDATE dbo.ShareDataPartner
    SET ConnectionConfig = JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(
        JSON_MODIFY(ConnectionConfig, '$.BaseUrl', NULL)
        , '$.Url', NULL)
        , '$.Host', NULL)
        , '$.Server', NULL)
        , '$.Address', NULL)
        , '$.IP', NULL)
        , '$.Port', NULL)
        , '$.Endpoint', NULL)
        , '$.Path', NULL)
        , '$.EndPointApiUrl', NULL)
    WHERE ConnectionConfig IS NOT NULL
      AND ISJSON(ConnectionConfig) = 1
      AND (
          JSON_VALUE(ConnectionConfig, '$.BaseUrl') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Url') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Host') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Server') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Address') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.IP') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Port') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Endpoint') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.Path') IS NOT NULL
          OR JSON_VALUE(ConnectionConfig, '$.EndPointApiUrl') IS NOT NULL
      );
    PRINT N'  -> Đã dọn dẹp các key địa chỉ/cổng trong JSON ConnectionConfig: ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + N' dòng.';

    PRINT N'✅ Hoàn tất: Chuẩn hóa cột đối tác trên DEV_ITS10.';
END
ELSE
BEGIN
    PRINT N'  -> Bảng ShareDataPartner hoặc cột ConnectionConfig chưa sẵn sàng, bỏ qua chuyển đổi JSON.';
END
GO

