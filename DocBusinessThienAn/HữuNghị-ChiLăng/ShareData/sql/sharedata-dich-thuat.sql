USE [DEV_ITS10];
GO

-- =========================================================================================
-- FILE: sharedata-dich-thuat.sql
-- DESCRIPTION: Bổ sung từ điển bản dịch đa ngôn ngữ (vi-VN, en-US) cho phân hệ ShareData (ESHARE) vào SysTerminology:
--              1. Chuẩn hóa lại mã khóa cũ thừa tiền tố (như lz.validation...lookbackDaysRange nếu có).
--              2. Nạp toàn bộ 112 mã khóa dịch thuật chuẩn hóa (224 bản ghi đa ngôn ngữ vi-VN và en-US):
--                 - 23 khóa trường thực thể (Entity) dùng cho validation / khóa ngoại / tên đối tượng.
--                 - 37 khóa ngoại lệ nghiệp vụ (Exception: phân hệ ShareData, quyền quản trị).
--                 - 50 khóa kiểm tra hợp lệ (Validation: SQL Validator, kết nối, đăng ký, gói tin, bộ mã, ánh xạ).
--                 - 2 khóa thông báo (Message: kết nối đối tác).
--              3. Cơ chế Idempotent:
--                 - MERGE chỉ INSERT các bản ghi chưa có (WHEN NOT MATCHED THEN INSERT).
--                 - Khối UPDATE có điều kiện chỉ đồng bộ lại các bản ghi seed cũ khi giá trị trên DB chưa bị người dùng sửa tay.
--                 - Không ghi đè (không UPDATE) lên các nội dung dịch mà quản trị viên đã chỉnh sửa trực tiếp trên DB.
-- PHÂN HỆ: ShareData (ESHARE)
-- MÔI TRƯỜNG: Mọi môi trường.
-- ĐẶC TÍNH: Chạy lại nhiều lần an toàn (idempotent).
-- CẬP NHẬT LẦN CUỐI: 22/10/2026
-- =========================================================================================

SET NOCOUNT ON;

PRINT N'=============================================================================';
PRINT N'BẮT ĐẦU KỊCH BẢN SEED BẢN DỊCH THUẬT NGỮ SHAREDATA (sharedata-dich-thuat.sql)';
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
WHERE Code LIKE N'lz.validation%lookbackDaysRange'
  AND Code <> N'sharedata.lookbackDaysRange'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.SysTerminology t2
      WHERE t2.Name = N'lz.validation'
        AND t2.Code = N'sharedata.lookbackDaysRange'
        AND t2.Lang = dbo.SysTerminology.Lang
  );

-- -----------------------------------------------------------------------------------------
-- 2. DANH MỤC 112 KHÓA BẢN DỊCH SHAREDATA (224 BẢN GHI ĐA NGÔN NGỮ)
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
    -- NHÓM 1: CẤU HÌNH VÀ THAM SỐ GÓI TIN / ĐĂNG KÝ (5 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.lookbackDaysRange',
     N'Số ngày quét lùi tối đa phải nằm trong khoảng từ {0} đến {1} ngày.',
     N'Maximum lookback days must be between {0} and {1} days.'),

    (N'lz.entity', N'sharedata.intervalSeconds',
     N'Chu kỳ gửi (giây)',
     N'Interval (seconds)'),

    (N'lz.entity', N'sharedata.priority',
     N'Độ ưu tiên',
     N'Priority'),

    (N'lz.entity', N'sharedata.debounceSec',
     N'Thời gian gom sự kiện (giây)',
     N'Debounce time (seconds)'),

    (N'lz.validation', N'sharedata.valueOutOfRange',
     N'{0} phải nằm trong khoảng {1}..{2}.',
     N'{0} must be in range {1}..{2}.'),

    -- =========================================================================
    -- NHÓM 2: TRƯỜNG THỰC THỂ (ENTITY) CHO VALIDATION / KHÓA NGOẠI (23 khóa)
    -- =========================================================================
    (N'lz.entity', N'sharedata.aliasFieldKey',
     N'Khóa field',
     N'Field key'),

    (N'lz.entity', N'sharedata.codeSetCode',
     N'Mã bộ mã quy đổi',
     N'Code set code'),

    (N'lz.entity', N'sharedata.codeSetDefaultPartnerValue',
     N'Giá trị đối tác mặc định',
     N'Default partner value'),

    (N'lz.entity', N'sharedata.codeSetDefaultSourceValue',
     N'Giá trị nội bộ mặc định',
     N'Default internal value'),

    (N'lz.entity', N'sharedata.codeSetValues',
     N'Bảng quy đổi giá trị',
     N'Value conversion table'),

    (N'lz.entity', N'sharedata.datatypeId',
     N'Loại dữ liệu chia sẻ',
     N'Shared data type'),

    (N'lz.entity', N'sharedata.direction',
     N'Chiều chia sẻ',
     N'Direction'),

    (N'lz.entity', N'sharedata.eventSourceId',
     N'Nguồn sự kiện',
     N'Event source'),

    (N'lz.entity', N'sharedata.groupName',
     N'Nhóm trường',
     N'Field group'),

    (N'lz.entity', N'sharedata.isRequired',
     N'Bắt buộc',
     N'Required'),

    (N'lz.entity', N'sharedata.mode',
     N'Chế độ đăng ký',
     N'Subscription mode'),

    (N'lz.entity', N'sharedata.packetCode',
     N'Mã gói tin',
     N'Packet code'),

    (N'lz.entity', N'sharedata.partnerId',
     N'Đối tác chia sẻ',
     N'Sharing partner'),

    (N'lz.entity', N'sharedata.schedule',
     N'Lịch gửi',
     N'Sending schedule'),

    (N'lz.entity', N'sharedata.sessionId',
     N'Phiên kết nối',
     N'Connection session'),

    (N'lz.entity', N'sharedata.subscriptionId',
     N'Đăng ký chia sẻ',
     N'Sharing subscription'),

    (N'lz.entity', N'sharedata.dataSourceId',
     N'Nguồn dữ liệu',
     N'Data source'),

    (N'lz.entity', N'sharedata.mappingId',
     N'Phễu lọc',
     N'Data mapping'),

    (N'lz.entity', N'sharedata.codeSetId',
     N'Bộ mã quy đổi',
     N'Code set'),

    (N'lz.entity', N'sharedata.tableName',
     N'Bảng vật lý',
     N'Physical table'),

    (N'lz.entity', N'sharedata.fieldKey',
     N'Field của gói tin',
     N'Packet field'),

    (N'lz.entity', N'sharedata.targetRootEntity',
     N'Thực thể gốc phía đối tác',
     N'Partner target root entity'),

    (N'lz.entity', N'sharedata.code',
     N'Mã field',
     N'Field code'),

    -- =========================================================================
    -- NHÓM 3: NGOẠI LỆ NGHIỆP VỤ PHÂN HỆ SHAREDATA (27 khóa)
    -- =========================================================================
    (N'lz.exception', N'sharedata.codeSetInUse',
     N'Bộ mã đang được gói tin hoặc hồ sơ ánh xạ sử dụng, không thể xóa.',
     N'The code set is used by a packet or mapping profile and cannot be deleted.'),

    (N'lz.exception', N'sharedata.codeSetPartnerValueDuplicated',
     N'Giá trị đối tác [{0}] bị trùng trong cùng bộ mã.',
     N'Partner value [{0}] is duplicated in the same code set.'),

    (N'lz.exception', N'sharedata.codeSetSourceValueDuplicated',
     N'Giá trị nguồn [{0}] bị trùng trong cùng bộ mã.',
     N'Source value [{0}] is duplicated in the same code set.'),

    (N'lz.exception', N'sharedata.httpContextUnavailable',
     N'Không lấy được ngữ cảnh HTTP để đọc nội dung gói tin.',
     N'Unable to get the HTTP context to read the packet content.'),

    (N'lz.exception', N'sharedata.mappingActiveDuplicated',
     N'Đã có phễu lọc đang áp dụng cho cùng đối tác, gói tin và chiều.',
     N'An active mapping already exists for the same partner, packet, and direction.'),

    (N'lz.exception', N'sharedata.mappingCodeSetNotExist',
     N'Bộ mã [{0}] không tồn tại.',
     N'Code set [{0}] does not exist.'),

    (N'lz.exception', N'sharedata.mappingConflictInUse',
     N'Không thể bật hồ sơ ánh xạ này vì hồ sơ đang áp dụng cho cùng đối tác/gói tin/chiều đang được đăng ký bật sử dụng, vui lòng tạm dừng đăng ký đó trước.',
     N'Cannot enable this mapping profile because the active profile for the same partner/packet/direction is used by an enabled subscription. Please pause that subscription first.'),

    (N'lz.exception', N'sharedata.mappingFieldKeyNotExist',
     N'Field [{0}] không tồn tại trong gói tin.',
     N'Field [{0}] does not exist in packet.'),

    (N'lz.exception', N'sharedata.mappingInUse',
     N'Hồ sơ ánh xạ đang được sử dụng trong cấu hình đăng ký, không thể sửa đổi hoặc xóa.',
     N'The mapping profile is used by an enabled subscription and cannot be edited or deleted.'),

    (N'lz.exception', N'sharedata.mappingMetaFieldsRequired',
     N'Hồ sơ có chiều gửi phải khai đủ các giá trị hệ thống. Còn thiếu: [{0}].',
     N'An outbound profile must declare all system values. Missing: [{0}].'),

    (N'lz.exception', N'sharedata.mappingNotResolved',
     N'Chưa có ánh xạ đang áp dụng cho đối tác và gói tin này.',
     N'No active mapping exists for this partner and packet.'),

    (N'lz.exception', N'sharedata.mappingPacketNotExist',
     N'Gói tin [{0}] không tồn tại.',
     N'Packet [{0}] does not exist.'),

    (N'lz.exception', N'sharedata.mappingTargetDuplicated',
     N'Có hai dòng cùng ánh xạ về [{0}].',
     N'Two rows map to the same target [{0}].'),

    (N'lz.exception', N'sharedata.packetCodeNameLocked',
     N'Gói tin đang được hồ sơ ánh xạ sử dụng, chỉ được sửa các trường mô tả, không được đổi Mã/Tên.',
     N'The packet is used by a mapping profile. Only descriptive fields can be edited; Code/Name cannot be changed.'),

    (N'lz.exception', N'sharedata.packetExtractSqlMissing',
     N'Gói tin [{0}] chưa cấu hình câu truy vấn trích xuất (ExtractSql).',
     N'Packet [{0}] has no extract SQL configured.'),

    (N'lz.exception', N'sharedata.packetFieldInUse',
     N'Field gói tin đang được hồ sơ ánh xạ sử dụng, không thể xóa.',
     N'The packet field is used by a mapping profile and cannot be deleted.'),

    (N'lz.exception', N'sharedata.packetFieldLocked',
     N'Field gói tin đang được hồ sơ ánh xạ sử dụng, chỉ được sửa Nhóm trường/Ghi chú.',
     N'The packet field is used by a mapping profile. Only Field group/Remark can be edited.'),

    (N'lz.exception', N'sharedata.packetInUse',
     N'Gói tin đang được hồ sơ ánh xạ sử dụng, không thể xóa.',
     N'The packet is used by a mapping profile and cannot be deleted.'),

    (N'lz.exception', N'sharedata.partnerDisabled',
     N'Đối tác đang ở trạng thái không sử dụng.',
     N'The partner is disabled.'),

    (N'lz.exception', N'sharedata.partnerHasActiveSubscription',
     N'Đối tác còn đăng ký đang chạy, vui lòng hủy trước khi xóa.',
     N'The partner still has running subscriptions. Please cancel them before deleting.'),

    (N'lz.exception', N'sharedata.partnerNotConnected',
     N'Đối tác chưa kết nối, vui lòng kết nối trước khi bật đăng ký.',
     N'The partner is not connected. Please connect before enabling the subscription.'),

    (N'lz.exception', N'sharedata.partnerSessionActive',
     N'Đối tác đang có phiên kết nối, vui lòng ngắt kết nối trước.',
     N'The partner has an active session. Please disconnect first.'),

    (N'lz.exception', N'sharedata.subscriptionDuplicated',
     N'Đối tác đã có đăng ký cho gói tin này.',
     N'The partner already has a subscription for this packet.'),

    (N'lz.exception', N'sharedata.subscriptionFinished',
     N'Đăng ký đã kết thúc, không thể sửa.',
     N'The subscription has ended and cannot be edited.'),

    (N'lz.exception', N'sharedata.subscriptionMustPauseBeforeDelete',
     N'Đăng ký đang chạy, vui lòng tắt trước khi xóa.',
     N'The subscription is running. Please turn it off before deleting.'),

    (N'lz.exception', N'sharedata.subscriptionMustPauseBeforeUpdate',
     N'Vui lòng tắt đăng ký trước khi sửa.',
     N'Please turn off the subscription before editing.'),

    (N'lz.exception', N'sharedata.subscriptionStateInvalid',
     N'Không thể chuyển trạng thái đăng ký từ [{0}] sang [{1}].',
     N'Cannot change the subscription state from [{0}] to [{1}].'),

    -- =========================================================================
    -- NHÓM 4: QUẢN TRỊ VÀ CẤU HÌNH SQL TRÍCH XUẤT & METADATA (11 khóa)
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
     N'Only SuperAdmin account has permission to preview data source query.'),

    (N'lz.exception', N'sharedata.sqlSampleExecutionFailed',
     N'Lỗi thực thi câu truy vấn mẫu: {0}',
     N'Sample query execution failed: {0}'),

    (N'lz.validation', N'sharedata.packetExtractSqlMissingForField',
     N'Gói tin chưa có câu lệnh SQL trích xuất nên chưa thể thêm trường dữ liệu.',
     N'The packet does not have an extract SQL query configured yet, so fields cannot be added.'),

    (N'lz.validation', N'sharedata.fieldKeyNotInExtractSql',
     N'Khóa [{0}] không có trong câu lệnh SQL trích xuất của gói tin.',
     N'The field key [{0}] is not present in the extract SQL query of the packet.'),

    (N'lz.validation', N'sharedata.fieldKeyCannotBeModified',
     N'Không thể sửa khóa trường hoặc gói tin của trường dữ liệu; muốn đổi khóa hãy sửa bí danh trong câu SQL rồi khai trường mới.',
     N'Field key or packet cannot be modified; to change a field key, update the alias in the extract SQL and create a new field.'),

    (N'lz.exception', N'sharedata.packetCodeNotFound',
     N'Không tìm thấy gói tin có mã [{0}].',
     N'Packet with code [{0}] was not found.'),

    (N'lz.exception', N'sharedata.superAdminRequiredForMetadata',
     N'Chỉ tài khoản SuperAdmin mới có quyền truy cập siêu dữ liệu CSDL.',
     N'Only SuperAdmin accounts are authorized to access database metadata.'),

    (N'lz.validation', N'sharedata.packetCodeRequired',
     N'Mã gói tin không được để trống.',
     N'Packet code is required.'),

    -- =========================================================================
    -- NHÓM 5: KIỂM TRA HỢP LỆ NGHIỆP VỤ GÓI TIN, BỘ MÃ, ĐĂNG KÝ (8 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.codeSetValuesRequired',
     N'Bảng quy đổi phải có ít nhất một dòng.',
     N'The conversion table must have at least one row.'),

    (N'lz.validation', N'sharedata.datatypeIdOutOfRange',
     N'Loại dữ liệu chia sẻ phải nằm trong khoảng 101–111.',
     N'Sharing data type must be between 101 and 111.'),

    (N'lz.validation', N'sharedata.eventSourceRequired',
     N'Chế độ theo sự kiện bắt buộc chọn nguồn sự kiện.',
     N'Event-driven mode requires an event source.'),

    (N'lz.validation', N'sharedata.mappingFieldKeyRequired',
     N'Mỗi dòng hồ sơ ánh xạ bắt buộc chỉ ra field của gói tin.',
     N'Each mapping row must specify a packet field.'),

    (N'lz.validation', N'sharedata.packetCodeHeaderRequired',
     N'Thiếu header PacketCode.',
     N'The PacketCode header is missing.'),

    (N'lz.validation', N'sharedata.partnerCodeHeaderRequired',
     N'Thiếu header PartnerCode.',
     N'The PartnerCode header is missing.'),

    (N'lz.validation', N'sharedata.scheduleRequired',
     N'Chế độ định kỳ bắt buộc cấu hình lịch gửi.',
     N'Periodic mode requires a sending schedule.'),

    (N'lz.validation', N'sharedata.scheduleTimeRangeInvalid',
     N'Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu.',
     N'End time must be greater than or equal to start time.'),

    -- =========================================================================
    -- NHÓM 6: METADATA CƠ SỞ DỮ LIỆU (3 khóa)
    -- =========================================================================
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
    -- NHÓM 7: SQL VALIDATOR - KIỂM SOÁT CÚ PHÁP VÀ AN TOÀN TRUY VẤN (21 khóa)
    -- =========================================================================
    (N'lz.validation', N'sharedata.sqlScriptEmpty',
     N'Câu lệnh truy vấn không được để trống.',
     N'Query script cannot be empty.'),

    (N'lz.validation', N'sharedata.sqlScriptSyntaxError',
     N'Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2}',
     N'Invalid SQL syntax (Line {0}, Column {1}): {2}'),

    (N'lz.validation', N'sharedata.sqlScriptInvalid',
     N'Cú pháp SQL không hợp lệ hoặc không chứa script thực thi.',
     N'Invalid SQL syntax or contains no executable statements.'),

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

    (N'lz.validation', N'sharedata.sqlTableNotAllowed',
     N'Bảng {0} chưa có trong danh mục bảng nguồn được phép chia sẻ.' + CHAR(10) + N'Thêm bảng vào ShareDataSourceTable (IsQueryable = 1) rồi thử lại.',
     N'Table {0} is not in the allowed sharing source tables.' + CHAR(10) + N'Add the table to ShareDataSourceTable (IsQueryable = 1) and try again.'),

    (N'lz.validation', N'sharedata.sqlUnsupportedParameter',
     N'Tham số [{'@'}{0}] không được hỗ trợ. Chỉ hỗ trợ: {'@'}lastTime, {'@'}lastKey, {'@'}pageSize, {'@'}fromDate, {'@'}toDate.',
     N'Parameter [{'@'}{0}] is not supported. Supported parameters: {'@'}lastTime, {'@'}lastKey, {'@'}pageSize, {'@'}fromDate, {'@'}toDate.'),

    (N'lz.validation', N'sharedata.sqlDatabaseExecutionError',
     N'Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0}',
     N'SQL syntax error on database: {0}'),

    (N'lz.validation', N'sharedata.sqlIncrementalMissingTop',
     N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề phân trang TOP ({'@'}pageSize).',
     N'Incremental queries must include a TOP ({'@'}pageSize) pagination clause.'),

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
    -- NHÓM 8: PARTNER VALIDATOR - KIỂM TRA CẤU HÌNH KẾT NỐI ĐỐI TÁC (12 khóa)
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
     N'Attribute [FileNamePattern] must contain at least one dynamic token ({serial}, {timestamp}, {packet}, or {partner}) to prevent file overwriting.'),

    -- =========================================================================
    -- NHÓM 9: THÔNG BÁO VẬN HÀNH (MESSAGE) (2 khóa)
    -- =========================================================================
    (N'lz.message', N'sharedata.partnerConnected',
     N'Đã thiết lập phiên kết nối với đối tác.',
     N'Established connection session with partner.'),

    (N'lz.message', N'sharedata.partnerDisconnected',
     N'Đã ngắt kết nối với đối tác.',
     N'Disconnected from partner.');

-- -----------------------------------------------------------------------------------------
-- 3. NẠP DỮ LIỆU ĐA NGÔN NGỮ (MERGE CHỈ THÊM DÒNG CHƯA CÓ, KHÔNG GHI ĐÈ)
-- -----------------------------------------------------------------------------------------
-- Xóa idempotent khóa cũ đã đổi tên hoặc bãi bỏ (nếu có)
DELETE FROM dbo.SysTerminology
WHERE ([Name] = N'lz.validation'
  AND [Code] IN (
      N'sharedata.connectConfigUnsupportedCodec',
      N'sharedata.sqlIncrementalNewStyleTopNotAllowed',
      N'sharedata.sqlIncrementalNewStyleOrderByNotAllowed',
      N'sharedata.sqlIncrementalNewStyleOptionNotAllowed',
      N'sharedata.sqlIncrementalNewStyleWithNotAllowed',
      N'sharedata.sqlIncrementalNewStyleParamsNotAllowed'
  ))
  OR ([Name] = N'lz.exception' AND [Code] IN (
      N'sharedata.superAdminRequiredForColumnSync'
  ));

-- 3.0. Đồng bộ cập nhật các bản ghi seed cũ (nếu chưa bị người dùng sửa tay trên DB)
UPDATE dbo.SysTerminology SET Value = N'Mã gói tin', UpdateTime = GETDATE()
WHERE Name = N'lz.entity' AND Code = N'sharedata.packetCode' AND Lang = N'vi-VN' AND Value = N'Gói tin';

UPDATE dbo.SysTerminology SET Value = N'Packet code', UpdateTime = GETDATE()
WHERE Name = N'lz.entity' AND Code = N'sharedata.packetCode' AND Lang = N'en-US' AND Value = N'Packet';

UPDATE dbo.SysTerminology SET Value = N'Query script cannot be empty.', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlScriptEmpty' AND Lang = N'en-US' AND Value = N'Query script is required.';

UPDATE dbo.SysTerminology SET Value = N'Invalid SQL syntax or contains no executable statements.', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlScriptInvalid' AND Lang = N'en-US' AND Value = N'SQL script is invalid or contains no executable statements.';

UPDATE dbo.SysTerminology SET Value = N'Tham số [{'@'}{0}] không được hỗ trợ. Chỉ hỗ trợ: {'@'}lastTime, {'@'}lastKey, {'@'}pageSize, {'@'}fromDate, {'@'}toDate.', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlUnsupportedParameter' AND Lang = N'vi-VN' AND Value LIKE N'%Tham số [@{0}]%';

UPDATE dbo.SysTerminology SET Value = N'Parameter [{'@'}{0}] is not supported. Supported parameters: {'@'}lastTime, {'@'}lastKey, {'@'}pageSize, {'@'}fromDate, {'@'}toDate.', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlUnsupportedParameter' AND Lang = N'en-US' AND Value LIKE N'%Parameter [@{0}]%';

UPDATE dbo.SysTerminology SET Value = N'Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề phân trang TOP ({'@'}pageSize).', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlIncrementalMissingTop' AND Lang = N'vi-VN' AND Value LIKE N'%TOP (@pageSize)%';

UPDATE dbo.SysTerminology SET Value = N'Incremental queries must include a TOP ({'@'}pageSize) pagination clause.', UpdateTime = GETDATE()
WHERE Name = N'lz.validation' AND Code = N'sharedata.sqlIncrementalMissingTop' AND Lang = N'en-US' AND Value LIKE N'%TOP (@pageSize)%';

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
PRINT N'✅ HOÀN TẤT KỊCH BẢN SEED BẢN DỊCH THUẬT NGỮ SHAREDATA (sharedata-dich-thuat.sql)';
PRINT N'=============================================================================';
GO
