-- =========================================================================================
-- FILE: 01-sharedata-cau-truc.sql
-- DESCRIPTION: Chuẩn hóa cấu hình đối tác cho phân hệ ShareData (ESHARE):
--              - Bảng ShareDataPartner đã được CodeFirst tạo khung trước (gồm ConnectionType, ConnectionConfig).
--              - Kiểm tra xung đột và chuyển đổi cấu hình địa chỉ/cổng/endpoint từ JSON
--                ConnectionConfig sang các cột riêng (Address, Port, EndPointApiUrl).
--              - Dọn dẹp các key địa chỉ/cổng khỏi ConnectionConfig bằng JSON_MODIFY.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 18/10/2026
-- =========================================================================================

-- =========================================================================================
-- CSDL CHÍNH [DEV_ITS10] - CHUẨN HÓA CẤU HÌNH ĐỐI TÁC ShareDataPartner
-- =========================================================================================
USE [DEV_ITS10];
GO

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'CHUẨN HÓA CẤU HÌNH ĐỐI TÁC ShareDataPartner TRÊN DEV_ITS10';
PRINT N'=============================================================================';

IF OBJECT_ID(N'dbo.ShareDataPartner', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng dbo.ShareDataPartner không tồn tại trên CSDL DEV_ITS10 (chưa chạy CodeFirst)! Bỏ qua.';
END
ELSE IF COL_LENGTH(N'dbo.ShareDataPartner', N'ConnectionConfig') IS NULL
BEGIN
    PRINT N'⚠️ Cột ConnectionConfig chưa tồn tại trên ShareDataPartner (chưa chạy CodeFirst)! Bỏ qua.';
END
ELSE
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

