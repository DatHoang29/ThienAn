USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: 04-sharedata-dich-thuat.sql
-- DESCRIPTION: Bổ sung từ điển bản dịch đa ngôn ngữ (vi-VN, en-US) cho phân hệ ShareData (ESHARE) vào SysTerminology:
--              1. Chuẩn hóa lại mã khóa cũ lz.validation.sharedata.lookbackDaysRange (nếu có).
--              2. Nạp toàn bộ 48 mã khóa dịch thuật chuẩn hóa:
--                 - 1 khóa khoảng số ngày quét lùi (lookbackDaysRange).
--                 - 10 khóa quản trị câu truy vấn SQL trích xuất (superAdmin, packetCode, partnerMapping...).
--                 - 37 khóa kiểm tra SQL Validator, Metadata và cấu hình kênh đối tác (OutputPath, ConnectConfig).
--              3. Cơ chế Idempotent:
--                 - MERGE chỉ INSERT các bản ghi chưa có (WHEN NOT MATCHED THEN INSERT).
--                 - Không ghi đè (không UPDATE) lên các nội dung dịch mà quản trị viên đã chỉnh sửa trực tiếp trên DB.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 16102026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN SEED BẢN DỊCH THUẬT NGỮ SHAREDATA (04-sharedata-dich-thuat.sql)';
PRINT N'=============================================================================';

DECLARE @TenantId NVARCHAR(64) = (SELECT TOP 1 CAST([TenantId] AS NVARCHAR(64)) FROM [dbo].[SysTerminology] WHERE [TenantId] IS NOT NULL);
IF @TenantId IS NULL
    SET @TenantId = N'Default';

-- -----------------------------------------------------------------------------------------
-- 1. CHUẨN HÓA MÃ KHÓA CŨ (NẾU CÓ DÒNG LƯU TRÙNG TIỀN TỐ TRƯỚC ĐÂY)
-- -----------------------------------------------------------------------------------------
UPDATE dbo.SysTerminology
SET Name = N'lz.validation',
    Code = N'sharedata.lookbackDaysRange',
    Remark = ISNULL(Remark, N'ShareData Localization'),
    UpdateTime = GETDATE(),
    UpdateUId = N'superadmin'
WHERE Code = N'lz.validation.sharedata.lookbackDaysRange'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.SysTerminology t2
      WHERE t2.Name = N'lz.validation'
        AND t2.Code = N'sharedata.lookbackDaysRange'
        AND t2.Lang = dbo.SysTerminology.Lang
  );

-- -----------------------------------------------------------------------------------------
-- 2. DANH MỤC 48 KHÓA BẢN DỊCH SHAREDATA
-- -----------------------------------------------------------------------------------------
DECLARE @Translations TABLE (
    [Name] NVARCHAR(64),
    [Code] NVARCHAR(256),
    [ViValue] NVARCHAR(MAX),
    [EnValue] NVARCHAR(MAX)
);

INSERT INTO @Translations ([Name], [Code], [ViValue], [EnValue])
VALUES
    -- =========================================================================
    -- NHÓM 1: CẤU HÌNH VÀ THAM SỐ GÓI TIN (1 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.lookbackDaysRange',
     N'Số ngày quét lùi tối đa phải nằm trong khoảng từ {0} đến {1} ngày.',
     N'Maximum lookback days must be between {0} and {1} days.'),

    -- =========================================================================
    -- NHÓM 2: QUẢN TRỊ VÀ CẤU HÌNH SQL TRÍCH XUẤT (10 khóa)
    -- =========================================================================
    (N'lz.exception', N'sharedata.superAdminRequiredForSqlEdit',
     N'Chỉ tài khoản SuperAdmin mới có quyền chỉnh sửa câu truy vấn nguồn dữ liệu.',
     N'Only SuperAdmin accounts are authorized to modify data source queries.'),

    (N'lz.exception', N'sharedata.superAdminRequiredForSqlView',
     N'Chỉ tài khoản SuperAdmin mới có quyền xem cấu hình câu truy vấn nguồn dữ liệu.',
     N'Only SuperAdmin accounts are authorized to view data source query configurations.'),

    (N'lz.exception', N'sharedata.superAdminRequiredForSqlValidate',
     N'Chỉ tài khoản SuperAdmin mới có quyền kiểm tra câu truy vấn nguồn dữ liệu.',
     N'Only SuperAdmin accounts are authorized to validate data source queries.'),

    (N'lz.exception', N'sharedata.superAdminRequiredForSqlPreview',
     N'Chỉ tài khoản SuperAdmin mới có quyền chạy thử câu truy vấn nguồn dữ liệu.',
     N'Only SuperAdmin accounts are authorized to preview data source query results.'),

    (N'lz.exception', N'sharedata.superAdminRequiredForColumnSync',
     N'Chỉ tài khoản SuperAdmin mới có quyền đồng bộ trường dữ liệu của gói tin.',
     N'Only SuperAdmin accounts are authorized to sync packet field definitions.'),

    (N'lz.exception', N'sharedata.packetCodeNotFound',
     N'Không tìm thấy gói tin có mã [{0}].',
     N'Packet with code [{0}] was not found.'),

    (N'lz.exception', N'sharedata.partnerCodeNotFound',
     N'Không tìm thấy đối tác có mã [{0}].',
     N'Partner with code [{0}] was not found.'),

    (N'lz.exception', N'sharedata.partnerMappingNotFound',
     N'Đối tác [{0}] chưa có hồ sơ ánh xạ chiều gửi đang bật cho gói tin [{1}].',
     N'Partner [{0}] has no active outbound mapping profile for packet [{1}].'),

    (N'lz.validation', N'sharedata.packetCodeRequired',
     N'Mã gói tin không được để trống.',
     N'Packet code is required.'),

    (N'lz.validation', N'sharedata.extractSqlRequired',
     N'Câu lệnh truy vấn SQL không được để trống.',
     N'SQL extract query is required.'),

    -- =========================================================================
    -- NHÓM 3: METADATA CƠ SỞ DỮ LIỆU (4 khóa)
    -- =========================================================================
    (N'lz.exception', N'sharedata.superAdminRequiredForMetadata',
     N'Chỉ tài khoản SuperAdmin mới có quyền truy cập siêu dữ liệu CSDL.',
     N'Only SuperAdmin accounts are authorized to access database metadata.'),

    (N'lz.validation', N'sharedata.tableNameRequired',
     N'Tên bảng nguồn không được để trống.',
     N'Source table name is required.'),

    (N'lz.validation', N'sharedata.tableNameInvalid',
     N'Tên bảng ''{0}'' không hợp lệ hoặc chứa ký tự bị cấm.',
     N'Table name ''{0}'' is invalid or contains forbidden characters.'),

    (N'lz.validation', N'sharedata.tableNotAllowed',
     N'Bảng ''{0}'' không thuộc danh mục được phép chia sẻ dữ liệu.',
     N'Table ''{0}'' is not in the allowed sharing catalog.'),

    -- =========================================================================
    -- NHÓM 4: CẤU HÌNH KÊNH ĐỐI TÁC (1 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.outputPathDeprecated',
     N'Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern].',
     N'Attribute [OutputPath] is deprecated. Please use [DirectoryPath] and [FileNamePattern].'),

    -- =========================================================================
    -- NHÓM 5: SQL VALIDATOR - KIỂM SOÁT CÚ PHÁP VÀ AN TOÀN TRUY VẤN (20 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.sqlScriptEmpty',
     N'Câu lệnh truy vấn không được để trống.',
     N'Query script is required.'),

    (N'lz.validation', N'sharedata.sqlScriptSyntaxError',
     N'Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2}',
     N'Invalid SQL syntax (Line {0}, Column {1}): {2}'),

    (N'lz.validation', N'sharedata.sqlScriptInvalid',
     N'Cú pháp SQL không hợp lệ hoặc không chứa script thực thi.',
     N'SQL script is invalid or contains no executable statements.'),

    (N'lz.validation', N'sharedata.sqlMultipleBatchesNotAllowed',
     N'Chỉ chấp nhận duy nhất một lô lệnh (batch). Không được sử dụng lệnh phân đoạn ''GO''.',
     N'Only a single batch is allowed. ''GO'' delimiter is not permitted.'),

    (N'lz.validation', N'sharedata.sqlMultipleStatementsNotAllowed',
     N'Chỉ chấp nhận duy nhất một câu lệnh SELECT (không cho phép đa câu lệnh).',
     N'Only a single SELECT statement is allowed (multiple statements are prohibited).'),

    (N'lz.validation', N'sharedata.sqlOnlySelectAllowed',
     N'Chỉ chấp nhận câu lệnh đọc dữ liệu bắt đầu bằng SELECT.',
     N'Only read statements starting with SELECT are allowed.'),

    (N'lz.validation', N'sharedata.sqlSelectIntoNotAllowed',
     N'Không cho phép sử dụng cú pháp SELECT ... INTO tạo bảng mới.',
     N'SELECT ... INTO syntax is not permitted.'),

    (N'lz.validation', N'sharedata.sqlFunctionCallBlocked',
     N'Không cho phép gọi hàm hoặc thủ tục: [{0}].',
     N'Calling function or procedure [{0}] is not allowed.'),

    (N'lz.validation', N'sharedata.sqlExternalDataSourcesBlocked',
     N'Không cho phép sử dụng các nguồn dữ liệu ngoài (OPENROWSET/OPENQUERY/...).',
     N'External data sources (OPENROWSET/OPENQUERY/...) are not allowed.'),

    (N'lz.validation', N'sharedata.sqlExecuteStatementBlocked',
     N'Không cho phép thực thi câu lệnh EXEC/EXECUTE.',
     N'Executing EXEC/EXECUTE statements is not allowed.'),

    (N'lz.validation', N'sharedata.sqlCrossDatabaseQueryBlocked',
     N'Không được phép truy vấn bảng vượt ra ngoài CSDL hiện tại (tên bảng 3-4 phần: [{0}]).',
     N'Cross-database queries are not allowed (3-4 part table name: [{0}]).'),

    (N'lz.validation', N'sharedata.sqlSystemSchemaBlocked',
     N'Không được phép truy vấn schema hệ thống: [{0}].',
     N'Querying system schema [{0}] is not allowed.'),

    (N'lz.validation', N'sharedata.sqlSensitiveTableBlocked',
     N'Không được phép truy vấn bảng hệ thống nhạy cảm: [{0}].',
     N'Querying sensitive system table [{0}] is not allowed.'),

    (N'lz.validation', N'sharedata.sqlUnsupportedParameter',
     N'Tham số [@{0}] không được hỗ trợ. Chỉ hỗ trợ: @lastTime, @lastKey, @pageSize, @fromDate, @toDate.',
     N'Parameter [@{0}] is not supported. Supported parameters: @lastTime, @lastKey, @pageSize, @fromDate, @toDate.'),

    (N'lz.validation', N'sharedata.sqlDatabaseExecutionError',
     N'Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0}',
     N'SQL syntax error on database: {0}'),

    (N'lz.validation', N'sharedata.sqlIncrementalMissingTop',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề phân trang TOP (@pageSize).',
     N'Incremental queries must include a TOP (@pageSize) pagination clause.'),

    (N'lz.validation', N'sharedata.sqlIncrementalMissingOrderBy',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề ORDER BY ở cấp ngoài cùng.',
     N'Incremental queries must include an outer ORDER BY clause.'),

    (N'lz.validation', N'sharedata.sqlIncrementalOrderByDescNotAllowed',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC) ở mệnh đề ORDER BY ngoài cùng.',
     N'Incremental queries must sort ascending (ASC); descending (DESC) sorting is not allowed in the outer ORDER BY clause.'),

    (N'lz.validation', N'sharedata.sqlIncrementalMissingLastTime',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ thời gian [__lastTime].',
     N'Incremental queries must include the time cursor column [__lastTime].'),

    (N'lz.validation', N'sharedata.sqlIncrementalMissingRowid',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ khóa [__rowid].',
     N'Incremental queries must include the key cursor column [__rowid].'),

    -- =========================================================================
    -- NHÓM 6: PARTNER VALIDATOR - KIỂM TRA CẤU HÌNH KẾT NỐI ĐỐI TÁC (12 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.connectConfigNotJsonObject',
     N'Cấu hình kết nối (ConnectionConfig) phải là đối tượng JSON phẳng dạng key-value.',
     N'ConnectionConfig must be a flat key-value JSON object.'),

    (N'lz.validation', N'sharedata.connectConfigSyntaxError',
     N'Cấu hình kết nối (ConnectionConfig) sai cú pháp JSON.',
     N'ConnectionConfig contains invalid JSON syntax.'),

    (N'lz.validation', N'sharedata.connectConfigEmptyKey',
     N'Cấu hình kết nối (ConnectionConfig) không được chứa khóa (Key) rỗng.',
     N'ConnectionConfig cannot contain empty keys.'),

    (N'lz.validation', N'sharedata.connectConfigReservedKey',
     N'Khóa cấu hình bị cấm sử dụng vì là khóa dành riêng của hệ thống.',
     N'Configuration key is reserved by the system and cannot be used.'),

    (N'lz.validation', N'sharedata.connectConfigObsoleteKey',
     N'Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern].',
     N'Attribute [OutputPath] is obsolete. Please use [DirectoryPath] and [FileNamePattern].'),

    (N'lz.validation', N'sharedata.connectConfigDuplicateKey',
     N'Cấu hình kết nối (ConnectionConfig) chứa khóa bị trùng lặp (không phân biệt hoa thường).',
     N'ConnectionConfig contains duplicate keys (case-insensitive).'),

    (N'lz.validation', N'sharedata.connectConfigNestedValue',
     N'Cấu hình kết nối (ConnectionConfig) chỉ cho phép giá trị phẳng (chuỗi/số/boolean), không cho phép lồng mảng hoặc object con.',
     N'ConnectionConfig only allows flat primitive values (string/number/boolean); nested arrays or objects are not allowed.'),

    (N'lz.validation', N'sharedata.connectConfigInvalidTimeout',
     N'Giá trị Timeout phải là số nguyên dương (giây).',
     N'Timeout value must be a positive integer (seconds).'),

    (N'lz.validation', N'sharedata.connectConfigInvalidBoolean',
     N'Giá trị cấu hình boolean chỉ chấp nhận ''true'' hoặc ''false''.',
     N'Boolean configuration value only accepts ''true'' or ''false''.'),

    (N'lz.validation', N'sharedata.connectConfigUnsupportedAuthType',
     N'Giá trị AuthType chỉ hỗ trợ: None, Bearer, ApiKey, Basic.',
     N'AuthType only supports: None, Bearer, ApiKey, Basic.'),

    (N'lz.validation', N'sharedata.connectConfigUnsupportedEncryptor',
     N'Giá trị mã hóa (Encryptor / InboundEncryptor) chỉ hỗ trợ: None, AesGcm.',
     N'Encryptor / InboundEncryptor only supports: None, AesGcm.'),

    (N'lz.validation', N'sharedata.connectConfigFileNamePatternNoToken',
     N'Thuộc tính [FileNamePattern] bắt buộc chứa ít nhất một biến động ({serial}, {timestamp}, {packet} hoặc {partner}) để tránh ghi đè tệp.',
     N'Attribute [FileNamePattern] must contain at least one dynamic token ({serial}, {timestamp}, {packet}, or {partner}) to prevent file overwriting.');

-- -----------------------------------------------------------------------------------------
-- 3. NẠP DỮ LIỆU ĐA NGÔN NGỮ (MERGE CHỈ THÊM DÒNG CHƯA CÓ, KHÔNG GHI ĐÈ)
-- -----------------------------------------------------------------------------------------
-- Xóa idempotent khóa cũ đã đổi tên (nếu có)
DELETE FROM dbo.SysTerminology
WHERE [Name] = N'lz.validation' AND [Code] = N'sharedata.connectConfigUnsupportedCodec';

-- 3.1. Xử lý bản dịch Tiếng Việt (vi-VN)
MERGE INTO dbo.SysTerminology AS target
USING (SELECT [Name], [Code], [ViValue] AS [Value], N'vi-VN' AS [Lang] FROM @Translations) AS source
ON (target.[Name] = source.[Name] AND target.[Code] = source.[Code] AND target.[Lang] = source.[Lang] AND target.IsDelete IS NULL)
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES (LOWER(CAST(NEWID() AS NVARCHAR(64))), source.Code, source.Name, source.Value, source.Lang, 100, N'ShareData Localization', 1, @TenantId, GETDATE(), N'superadmin');

DECLARE @InsertedVi INT = @@ROWCOUNT;
DECLARE @TotalKeys INT = (SELECT COUNT(1) FROM @Translations);
DECLARE @SkippedVi INT = @TotalKeys - @InsertedVi;
PRINT N'  -> [vi-VN] Đã thêm mới ' + CAST(@InsertedVi AS NVARCHAR(10)) + N' bản dịch; bỏ qua ' + CAST(@SkippedVi AS NVARCHAR(10)) + N' bản dịch đã tồn tại.';

-- 3.2. Xử lý bản dịch Tiếng Anh (en-US)
MERGE INTO dbo.SysTerminology AS target
USING (SELECT [Name], [Code], [EnValue] AS [Value], N'en-US' AS [Lang] FROM @Translations) AS source
ON (target.[Name] = source.[Name] AND target.[Code] = source.[Code] AND target.[Lang] = source.[Lang] AND target.IsDelete IS NULL)
WHEN NOT MATCHED THEN
    INSERT (ID, Code, Name, Value, Lang, OrderNo, Remark, Status, TenantId, CreateTime, CreateUId)
    VALUES (LOWER(CAST(NEWID() AS NVARCHAR(64))), source.Code, source.Name, source.Value, source.Lang, 100, N'ShareData Localization', 1, @TenantId, GETDATE(), N'superadmin');

DECLARE @InsertedEn INT = @@ROWCOUNT;
DECLARE @SkippedEn INT = @TotalKeys - @InsertedEn;
PRINT N'  -> [en-US] Đã thêm mới ' + CAST(@InsertedEn AS NVARCHAR(10)) + N' bản dịch; bỏ qua ' + CAST(@SkippedEn AS NVARCHAR(10)) + N' bản dịch đã tồn tại.';

PRINT N'=============================================================================';
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED BẢN DỊCH THUẬT NGỮ SHAREDATA (04-sharedata-dich-thuat.sql)';
PRINT N'=============================================================================';
GO
