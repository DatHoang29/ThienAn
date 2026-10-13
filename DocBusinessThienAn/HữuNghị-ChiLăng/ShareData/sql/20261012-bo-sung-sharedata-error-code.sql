/* ============================================================================
   Bổ sung danh mục mã lỗi hệ thống ShareData (nhóm ESH)
   Task: BE-7 · Bảng mã lỗi hệ thống ShareData (XD001.5.6)
   Tệp: DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/20261012-bo-sung-sharedata-error-code.sql
   Ngày soạn: 12/10/2026
   Phạm vi: Bảng SysConfigType ('sharedata_error_code') và SysConfigData (26 mã lỗi ESH).
   An toàn chạy lại nhiều lần (idempotent).
   ============================================================================ */

USE [DEV_ITS10];
GO

SET NOCOUNT ON;

DECLARE @ConfigTypeCode NVARCHAR(64) = N'sharedata_error_code';
DECLARE @ConfigTypeName NVARCHAR(64) = N'Mã lỗi hệ thống ShareData';
DECLARE @ConfigTypeRemark NVARCHAR(256) = N'Danh mục mã lỗi ESH hệ thống chia sẻ dữ liệu ShareData';
DECLARE @ConfigTypeId NVARCHAR(64);

/* 1. Đảm bảo SysConfigType tồn tại */
SELECT @ConfigTypeId = ID
FROM   dbo.SysConfigType
WHERE  Code = @ConfigTypeCode
  AND  IsDelete IS NULL;

IF @ConfigTypeId IS NULL
BEGIN
    SET @ConfigTypeId = LOWER(CAST(NEWID() AS NVARCHAR(64)));
    INSERT INTO dbo.SysConfigType
        (ID, Code, Name, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES
        (@ConfigTypeId, @ConfigTypeCode, @ConfigTypeName, 100, @ConfigTypeRemark, 1, N'Default', GETDATE(), N'superadmin');
    PRINT N'Đã tạo mới SysConfigType: ' + @ConfigTypeCode;
END
ELSE
BEGIN
    UPDATE dbo.SysConfigType
    SET Name = @ConfigTypeName,
        Remark = @ConfigTypeRemark,
        Status = 1,
        IsDelete = NULL
    WHERE ID = @ConfigTypeId;
    PRINT N'Đã cập nhật SysConfigType: ' + @ConfigTypeCode;
END

/* 2. Bảng tạm chứa đầy đủ 26 mã lỗi ESH theo đặc tả hành vi thật */
DECLARE @Codes TABLE (
    Code NVARCHAR(64),
    Name NVARCHAR(500),
    Value NVARCHAR(64),
    TagType NVARCHAR(64),
    Remark NVARCHAR(500),
    OrderNo INT
);

INSERT INTO @Codes (Code, Name, Value, TagType, Remark, OrderNo) VALUES
    -- Nhóm Outbound (13 mã)
    (N'ESH-1201', N'Bộ mã khai trong metadata không có trong CSDL', N'outbound', N'warning', N'Kiểm tra cấu hình metadata của gói tin và khai báo bộ mã nguồn trong CSDL', 1201),
    (N'ESH-1202', N'Thiếu/null trường bắt buộc, hủy kết xuất, không gửi', N'outbound', N'danger', N'Kiểm tra dữ liệu nguồn hoặc sửa cấu hình bắt buộc trong hồ sơ ánh xạ', 1202),
    (N'ESH-1203', N'Biểu thức lỗi/không an toàn: giữ giá trị gốc, vẫn gửi', N'outbound', N'warning', N'Kiểm tra cú pháp biểu thức chuyển đổi trong cấu hình hồ sơ ánh xạ', 1203),
    (N'ESH-1204', N'Số nguyên bị nâng thành decimal sau quy đổi đơn vị', N'outbound', N'warning', N'Kiểm tra cấu hình quy đổi đơn vị hoặc kiểu dữ liệu trường đầu ra', 1204),
    (N'ESH-1205', N'Không còn dùng từ 18/09', N'outbound', N'warning', N'Mã lỗi cũ đã ngừng sử dụng, lưu lại phục vụ tra cứu lịch sử', 1205),
    (N'ESH-1206', N'TargetShapeJson rỗng/sai cú pháp, hủy kết xuất', N'outbound', N'danger', N'Cập nhật định dạng TargetShapeJson hợp lệ trong cấu hình gói tin', 1206),
    (N'ESH-1301', N'Không có cấu hình gói tin cho gói đang gửi', N'outbound', N'danger', N'Khai báo cấu hình gói tin trong danh mục gói ShareData', 1301),
    (N'ESH-1302', N'Lỗi truy vấn dữ liệu nguồn', N'outbound', N'danger', N'Kiểm tra câu SQL nguồn (QuerySql), kết nối CSDL và bảng dữ liệu', 1302),
    (N'ESH-1303', N'Mất khoá xử lý, node khác đã nhận đăng ký', N'outbound', N'warning', N'Kiểm tra tình trạng phân tán của các worker hoặc thời gian timeout lock', 1303),
    (N'ESH-1304', N'Không có hồ sơ ánh xạ chiều gửi đang bật, hủy kết xuất', N'outbound', N'danger', N'Bật hoặc tạo mới hồ sơ ánh xạ dữ liệu (Data Mapping Profile) cho gói tin', 1304),
    (N'ESH-1305', N'Khóa bản ghi cuối trang dài hơn cột LastKey, hủy trước khi gửi', N'outbound', N'danger', N'Kiểm tra độ dài giá trị khóa chính của bảng nguồn hoặc cấu hình trường khóa', 1305),
    (N'ESH-1401', N'Lỗi ghi tệp kết xuất', N'outbound', N'danger', N'Kiểm tra quyền truy cập thư mục xuất tệp và dung lượng ổ đĩa', 1401),
    (N'ESH-1402', N'Gửi HTTP tới đối tác lỗi (4xx/5xx/timeout/exception): phiên ghi Fail, file vẫn xuất, mốc giữ nguyên để gửi lại', N'outbound', N'warning', N'Kiểm tra URL endpoint của đối tác, kết nối mạng và log lỗi phía đối tác', 1402),

    -- Nhóm Inbound (9 mã)
    (N'ESH-1501', N'Đối tác chưa có URL API nhận [Cần dev xác nhận còn dùng]', N'inbound', N'warning', N'Cập nhật URL nhận trong cấu hình đối tác nếu dùng cơ chế kéo', 1501),
    (N'ESH-1502', N'Gọi hộp gửi đối tác lỗi [Cần dev xác nhận còn dùng]', N'inbound', N'danger', N'Kiểm tra kết nối và endpoint của hộp gửi đối tác nếu dùng cơ chế kéo', 1502),
    (N'ESH-1503', N'Không đọc được nội dung gói nhận về (JSON hỏng/XML) → gói Failed', N'inbound', N'danger', N'Kiểm tra định dạng payload bên gửi truyền sang (phải là JSON hợp lệ)', 1503),
    (N'ESH-1504', N'Không xác định được khóa đối chiếu, hoặc khóa khớp nhiều bản ghi', N'inbound', N'danger', N'Kiểm tra cấu hình khóa đối chiếu (MatchKey) và dữ liệu trùng lặp ở bảng đích', 1504),
    (N'ESH-1505', N'Lỗi ghi CSDL nhận, đã rollback cả gói → Failed', N'inbound', N'danger', N'Kiểm tra ràng buộc CSDL đích, kiểu dữ liệu các cột hoặc câu lệnh WriteSql', 1505),
    (N'ESH-1506', N'Không có cấu hình gói tin cho PacketCode nhận về', N'inbound', N'danger', N'Khai báo mã gói tin (PacketCode) này trong cấu hình gói tin hệ thống', 1506),
    (N'ESH-1507', N'Đối tác không tồn tại hoặc đang tắt', N'inbound', N'danger', N'Bật hoặc tạo mới thông tin đối tác tương ứng với PartnerCode', 1507),
    (N'ESH-1508', N'Chưa có đăng ký nhận hợp lệ → gói NoSubscription', N'inbound', N'danger', N'Tạo và kích hoạt đăng ký nhận (Inbound Subscription) cho gói tin và đối tác này', 1508),
    (N'ESH-1509', N'Gói tin chưa khai câu ghi chiều nhận (cột WriteSql của ShareDataPacketSql)', N'inbound', N'danger', N'Bổ sung câu lệnh SQL chiều nhận (WriteSql) trong cấu hình SQL của gói tin', 1509),

    -- Nhóm Tracking (4 mã)
    (N'ESH-1601', N'Hạ tầng Change Tracking tự nhảy cóc mốc', N'tracking', N'danger', N'Kiểm tra retention period của Change Tracking trên CSDL nguồn', 1601),
    (N'ESH-1602', N'Truy vấn CHANGETABLE lỗi', N'tracking', N'danger', N'Kiểm tra quyền truy cập Change Tracking của tài khoản CSDL hoặc cú pháp câu truy vấn', 1602),
    (N'ESH-1603', N'Chưa bật Change Tracking trên bảng nguồn', N'tracking', N'danger', N'Chạy ALTER TABLE ... ENABLE CHANGE_TRACKING trên bảng dữ liệu nguồn', 1603),
    (N'ESH-1604', N'Mốc Change Tracking bị đặt lại', N'tracking', N'warning', N'Kiểm tra thao tác can thiệp thủ công vào mốc đồng bộ LastVersion', 1604);

/* 3. Cập nhật các dòng đã tồn tại */
UPDATE d
SET d.Name = c.Name,
    d.Value = c.Value,
    d.TagType = c.TagType,
    d.Remark = c.Remark,
    d.OrderNo = c.OrderNo,
    d.Status = 1,
    d.IsDelete = NULL,
    d.UpdateTime = GETDATE(),
    d.UpdateUId = N'superadmin'
FROM dbo.SysConfigData d
JOIN @Codes c ON d.Code = c.Code
WHERE d.ConfigTypeId = @ConfigTypeId;

PRINT N'Số dòng SysConfigData đã cập nhật: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* 4. Thêm mới các dòng chưa tồn tại */
INSERT INTO dbo.SysConfigData
    (ID, ConfigTypeId, Name, Value, TagType, StyleSetting, ClassSetting,
     OrderNo, Remark, Status, TenantId, Code, CreateTime, CreateUId)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    @ConfigTypeId,
    c.Name,
    c.Value,
    c.TagType,
    NULL,
    NULL,
    c.OrderNo,
    c.Remark,
    1,
    N'Default',
    c.Code,
    GETDATE(),
    N'superadmin'
FROM @Codes c
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.SysConfigData d
    WHERE d.ConfigTypeId = @ConfigTypeId
      AND d.Code = c.Code
);

PRINT N'Số dòng SysConfigData đã thêm mới: ' + CAST(@@ROWCOUNT AS NVARCHAR(10));

/* 5. Bổ sung bản dịch giao diện (SysTerminology) theo chuẩn Name = 'lz.label' */
DECLARE @Terms TABLE (
    Name NVARCHAR(64),
    Code NVARCHAR(256),
    Lang NVARCHAR(32),
    Value NVARCHAR(500)
);

INSERT INTO @Terms (Name, Code, Lang, Value) VALUES
    (N'lz.label', N'sharedataAlertLog.alertDesc',       N'vi-VN', N'Mô tả'),
    (N'lz.label', N'sharedataAlertLog.alertDesc',       N'en-US', N'Description'),
    (N'lz.label', N'sharedataAlertDetail.alertDesc',    N'vi-VN', N'Mô tả'),
    (N'lz.label', N'sharedataAlertDetail.alertDesc',    N'en-US', N'Description'),
    (N'lz.label', N'sharedataAlertDetail.actionGuide',  N'vi-VN', N'Cách xử lý'),
    (N'lz.label', N'sharedataAlertDetail.actionGuide',  N'en-US', N'Troubleshooting');

UPDATE t
SET t.Value = src.Value,
    t.Status = 1,
    t.IsDelete = NULL,
    t.UpdateTime = GETDATE(),
    t.UpdateUId = N'superadmin'
FROM dbo.SysTerminology t
JOIN @Terms src
  ON t.Name = src.Name
 AND t.Code = src.Code
 AND t.Lang = src.Lang;

INSERT INTO dbo.SysTerminology
    (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
SELECT
    LOWER(CAST(NEWID() AS NVARCHAR(64))),
    src.Code,
    src.Name,
    src.Value,
    src.Lang,
    100,
    N'ShareData Localization',
    1,
    N'Default',
    GETDATE(),
    N'superadmin'
FROM @Terms src
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.SysTerminology t
    WHERE t.Name = src.Name
      AND t.Code = src.Code
      AND t.Lang = src.Lang
);

PRINT N'Đã seed xong bản dịch SysTerminology cho các nhãn giao diện mới.';

/* 6. Đối chiếu kết quả: Bắt buộc đúng 26 dòng */
SELECT
    d.Code,
    d.Name,
    d.Value AS [Group],
    d.TagType AS [Severity],
    d.OrderNo,
    d.Status,
    d.Remark
FROM dbo.SysConfigData d
JOIN dbo.SysConfigType t ON t.ID = d.ConfigTypeId
WHERE t.Code = @ConfigTypeCode
  AND d.IsDelete IS NULL
ORDER BY d.OrderNo;

SELECT COUNT(*) AS [TotalCodes]
FROM dbo.SysConfigData d
JOIN dbo.SysConfigType t ON t.ID = d.ConfigTypeId
WHERE t.Code = @ConfigTypeCode
  AND d.IsDelete IS NULL;

