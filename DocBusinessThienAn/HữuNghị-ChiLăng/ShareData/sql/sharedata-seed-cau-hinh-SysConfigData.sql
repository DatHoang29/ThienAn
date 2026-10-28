USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-seed-cau-hinh-SysConfigData.sql
-- BẢNG GHI: [dbo].[SysConfigData] — tệp chỉ thêm / sửa / xóa trên đúng bảng này; bảng khác chỉ đọc (Rule 19.103).
-- DESCRIPTION: 2. Nạp 25 mã cảnh báo lỗi ESH-xxxx (14 Outbound, 7 Inbound, 4 Tracking) vào loại 'sharedata_error_code',
--                 chỉ thêm các mã chưa có.
--              4.1 Đăng ký nhóm 'sharedata_config' vào loại 'operation_config_type'.
-- THỨ TỰ CHẠY: 5 (sau sharedata-seed-cau-hinh-SysConfigType.sql) (xem README.md cùng thư mục).
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 24/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN sharedata-seed-cau-hinh-SysConfigData.sql';
PRINT N'=============================================================================';
GO

IF OBJECT_ID(N'[dbo].[SysConfigData]', N'U') IS NULL
BEGIN
    PRINT N'⚠️ Bảng [dbo].[SysConfigData] chưa tồn tại (chưa chạy CodeFirst)! Bỏ qua kịch bản.';
    SET NOEXEC ON;
END;
GO

DECLARE @TenantId NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantId IS NULL
    SET @TenantId = N'Default';

DECLARE @ConfigTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID] FROM [dbo].[SysConfigType]
    WHERE [Code] = N'sharedata_error_code' AND [IsDelete] IS NULL
);

IF @ConfigTypeId IS NULL
BEGIN
    PRINT N'⚠️ Chưa có SysConfigType [sharedata_error_code]. Hãy chạy sharedata-seed-cau-hinh-SysConfigType.sql trước.';
    SET NOEXEC ON;
END;

-- -----------------------------------------------------------------------------------------
-- 2. NẠP DANH SÁCH 25 MÃ CẢNH BÁO LỖI ESH-xxxx VÀO SysConfigData
-- -----------------------------------------------------------------------------------------
PRINT N'-> 2. Nạp danh mục 25 mã cảnh báo lỗi ESH-xxxx vào SysConfigData...';

DECLARE @ErrorCodes TABLE (
    [Code] NVARCHAR(64),
    [Name] NVARCHAR(256),
    [Value] NVARCHAR(64),
    [TagType] NVARCHAR(64),
    [OrderNo] INT,
    [Remark] NVARCHAR(512)
);

INSERT INTO @ErrorCodes ([Code], [Name], [Value], [TagType], [OrderNo], [Remark])
VALUES
    -- Outbound (14 mã)
    (N'ESH-1201', N'Bộ mã khai trong metadata không tồn tại trong CSDL', N'outbound', N'warning', 1201,
     N'Kiểm tra lại cấu hình CodeSet trong metadata.'),
    (N'ESH-1202', N'Trường bắt buộc bị thiếu hoặc null — huỷ kết xuất', N'outbound', N'danger', 1202,
     N'Kiểm tra dữ liệu nguồn hoặc cấu hình trường bắt buộc.'),
    (N'ESH-1203', N'Biểu thức không qua kiểm tra an toàn tĩnh hoặc lỗi khi tính — giữ giá trị nguồn', N'outbound', N'warning', 1203,
     N'Kiểm tra biểu thức biến đổi trong mapping.'),
    (N'ESH-1204', N'TargetShapeJson rỗng hoặc sai cú pháp — huỷ kết xuất', N'outbound', N'danger', 1204,
     N'Kiểm tra định dạng TargetShapeJson trong mapping.'),
    (N'ESH-1301', N'Không tìm thấy cấu hình gói tin cho DatatypeId', N'outbound', N'danger', 1301,
     N'Kiểm tra danh mục gói tin ShareDataPacket.'),
    (N'ESH-1302', N'Lỗi truy vấn dữ liệu nguồn của gói tin', N'outbound', N'danger', 1302,
     N'Kiểm tra câu lệnh SQL trích xuất và kết nối CSDL.'),
    (N'ESH-1303', N'Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất', N'outbound', N'danger', 1303,
     N'Kiểm tra cấu hình mapping giữa đối tác và gói tin.'),
    (N'ESH-1304', N'Khoá bản ghi cuối trang (__lastKey) dài hơn cột LastKey cho phép — huỷ kết xuất trước khi gửi', N'outbound', N'danger', 1304,
     N'Kiểm tra kiểu dữ liệu hoặc giá trị khóa __lastKey.'),
    (N'ESH-1305', N'Không tìm thấy câu lệnh SQL trích xuất dữ liệu ở bất kỳ tầng nào', N'outbound', N'danger', 1305,
     N'Kiểm tra câu truy vấn SQL (ExtractSql) trong cấu hình gói tin ShareDataPacket.'),
    (N'ESH-1306', N'Thứ tự con trỏ dữ liệu không tăng dần hoặc thiếu khóa __lastKey', N'outbound', N'danger', 1306,
     N'Kiểm tra mệnh đề ORDER BY trong câu lệnh trích xuất dữ liệu.'),
    (N'ESH-1307', N'Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL', N'outbound', N'warning', 1307,
     N'Kiểm tra mốc đồng bộ con trỏ và dữ liệu thời gian.'),
    (N'ESH-1401', N'Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công', N'outbound', N'warning', 1401,
     N'Kiểm tra kết nối mạng và endpoint của đối tác.'),
    (N'ESH-1402', N'Lưu đệm payload/bằng chứng thất bại, hủy gửi và giữ nguyên mốc', N'outbound', N'danger', 1402,
     N'Kiểm tra quyền ghi thư mục NasStorage:BasePath hoặc dung lượng ổ đĩa lưu trữ bằng chứng.'),
    (N'ESH-1403', N'Giá trị cấu hình kết nối không hợp lệ bị bỏ qua hoặc thay bằng mặc định', N'outbound', N'warning', 1403,
     N'Kiểm tra lại cấu hình kết nối ConnectionConfig của đối tác.'),

    -- Inbound (7 mã)
    (N'ESH-1501', N'Không đọc được nội dung gói tin nhận về', N'inbound', N'danger', 1501,
     N'Kiểm tra định dạng JSON/XML của gói tin nhận.'),
    (N'ESH-1502', N'Lỗi ghi dữ liệu xuống CSDL nhận', N'inbound', N'danger', 1502,
     N'Kiểm tra kết nối và quyền ghi CSDL Inbound.'),
    (N'ESH-1503', N'Không tìm thấy cấu hình gói tin cho DatatypeId nhận về', N'inbound', N'danger', 1503,
     N'Kiểm tra danh mục gói tin chiều nhận trong ShareDataPacket.'),
    (N'ESH-1504', N'Đối tác của gói tin không tồn tại hoặc đang tắt', N'inbound', N'danger', 1504,
     N'Kiểm tra danh mục đối tác ShareDataPartner.'),
    (N'ESH-1505', N'Gói tin chưa có đăng ký nhận hợp lệ', N'inbound', N'danger', 1505,
     N'Kiểm tra cấu hình mapping chiều nhận ShareDataMapping.'),
    (N'ESH-1506', N'Gói tin chưa khai câu ghi chiều nhận (ShareDataPacketWrite)', N'inbound', N'danger', 1506,
     N'Kiểm tra cấu hình câu ghi CSDL trong ShareDataPacketWrite.'),
    (N'ESH-1507', N'Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag)', N'inbound', N'danger', 1507,
     N'Kiểm tra thuật toán mã hóa, khóa bí mật hoặc định dạng envelope.'),

    -- Tracking (4 mã)
    (N'ESH-1601', N'Mốc Change Tracking không hợp lệ — đã tự nhảy cóc để hồi phục', N'tracking', N'warning', 1601,
     N'Hệ thống tự động đồng bộ lại mốc Change Tracking khi vượt cleanup.'),
    (N'ESH-1602', N'Truy vấn CHANGETABLE lỗi vì lý do KHÁC version — đã bỏ cache câu SQL và hẹn quét lại', N'tracking', N'danger', 1602,
     N'Kiểm tra cú pháp hoặc quyền truy cập CHANGETABLE trên bảng nguồn.'),
    (N'ESH-1603', N'CSDL hoặc bảng nguồn chưa bật Change Tracking — polling tạm dừng', N'tracking', N'warning', 1603,
     N'Kiểm tra cấu hình ALTER DATABASE / TABLE ENABLE CHANGE_TRACKING.'),
    (N'ESH-1604', N'Mốc đang giữ không còn tương thích CSDL — đã hạ mốc về -1 để đi lại đường khởi tạo', N'tracking', N'warning', 1604,
     N'Tracking bị tắt/bật lại hoặc CSDL restore từ bản cũ hơn.');

INSERT INTO dbo.SysConfigData (
    [ID], [ConfigTypeId], [Name], [Code], [Value], [TagType], 
    [OrderNo], [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    @ConfigTypeId,
    src.[Name],
    src.[Code],
    src.[Value],
    src.[TagType],
    src.[OrderNo],
    src.[Remark],
    1,
    @TenantId,
    GETDATE(),
    N'superadmin'
FROM @ErrorCodes src
WHERE NOT EXISTS (
    SELECT 1
    FROM   dbo.SysConfigData target
    WHERE  target.ConfigTypeId = @ConfigTypeId
      AND  target.Code = src.Code
      AND  target.IsDelete IS NULL
);

DECLARE @InsertedErrors INT = @@ROWCOUNT;
DECLARE @TotalErrors INT = (SELECT COUNT(1) FROM @ErrorCodes);
DECLARE @SkippedErrors INT = @TotalErrors - @InsertedErrors;
PRINT N'   -> Đã seed ' + CAST(@InsertedErrors AS NVARCHAR(10)) + N' mã lỗi mới; bỏ qua ' + CAST(@SkippedErrors AS NVARCHAR(10)) + N' mã đã có sẵn trong SysConfigData.';
GO

-- -----------------------------------------------------------------------------------------
-- 4.1 ĐĂNG KÝ NHÓM sharedata_config VÀO SysConfigData (operation_config_type)
-- -----------------------------------------------------------------------------------------
SET NOCOUNT ON;

DECLARE @TenantIdOp NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysOpConfig] WHERE [TenantId] IS NOT NULL);
IF @TenantIdOp IS NULL
    SET @TenantIdOp = N'Default';

DECLARE @OpConfigTypeId NVARCHAR(64) = (
    SELECT TOP 1 [ID]
    FROM [dbo].[SysConfigType]
    WHERE [Code] = N'operation_config_type'
      AND [IsDelete] IS NULL
);

IF @OpConfigTypeId IS NOT NULL
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM [dbo].[SysConfigData]
        WHERE [ConfigTypeId] = @OpConfigTypeId
          AND [Code] = N'sharedata_config'
          AND [IsDelete] IS NULL
    )
    BEGIN
        INSERT INTO [dbo].[SysConfigData] (
            [ID], [ConfigTypeId], [Name], [Code], [Value], [TagType],
            [OrderNo], [Remark], [Status], [TenantId], [CreateTime], [CreateUId]
        )
        VALUES (
            LOWER(CAST(NEWID() AS NVARCHAR(64))),
            @OpConfigTypeId,
            N'ShareData Config',
            N'sharedata_config',
            N'sharedata_config',
            N'primary',
            100,
            N'Cấu hình vận hành & giới hạn Chia sẻ dữ liệu (ShareData)',
            1,
            @TenantIdOp,
            GETDATE(),
            N'superadmin'
        );
        PRINT N'   -> Đã đăng ký nhóm [sharedata_config] vào SysConfigData (operation_config_type).';
    END
    ELSE
    BEGIN
        PRINT N'   -> Nhóm [sharedata_config] đã tồn tại trong SysConfigData (operation_config_type), bỏ qua.';
    END
END
ELSE
BEGIN
    PRINT N'   -> ⚠️ Không tìm thấy SysConfigType [operation_config_type], bỏ qua đăng ký nhóm vào SysConfigData.';
END

SET NOEXEC OFF;
GO

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN sharedata-seed-cau-hinh-SysConfigData.sql';
PRINT N'=============================================================================';
GO
