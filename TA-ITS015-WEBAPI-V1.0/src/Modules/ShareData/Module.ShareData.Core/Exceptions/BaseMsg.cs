using Shared.DTO.Constants.Localization;

namespace Module.ShareData.Core.Exceptions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nội dung dịch thuật riêng của phân hệ Chia sẻ dữ liệu (ShareData)
    /// Created date: 2026-08-04
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: 
        /// Created date: 14/08/2026
        /// </summary>
        public class ShareData
        {
            /// <summary>
            /// Author: HieuNV
            /// Description: Prefix
            /// Created date: 14/08/2026
            /// </summary>
            protected class Prefix
            {
                internal const string Message = "lz.message.sharedata.";
                internal const string Validation = "lz.validation.sharedata.";
                internal const string Exception = "lz.exception.sharedata.";
                internal const string Entity = "lz.entity.sharedata.";
            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Tên hiển thị các trường thực thể, dùng trong message lỗi khóa ngoại.
            /// Created date: 14/08/2026
            /// </summary>
            public class Entity
            {
                /// <summary>Đối tác chia sẻ (ShareDataPartner)</summary>
                public const string PartnerId = Prefix.Entity + "partnerId";

                /// <summary>Phiên kết nối (ShareDataSession)</summary>
                public const string SessionId = Prefix.Entity + "sessionId";

                /// <summary>Đăng ký chia sẻ (ShareDataSubscription)</summary>
                public const string SubscriptionId = Prefix.Entity + "subscriptionId";

                /// <summary>Nguồn dữ liệu (ShareDataDataSource)</summary>
                public const string DataSourceId = Prefix.Entity + "dataSourceId";

                /// <summary>Nguồn sự kiện (ShareDataEventSource)</summary>
                public const string EventSourceId = Prefix.Entity + "eventSourceId";

                /// <summary>Loại dữ liệu chia sẻ (101–111)</summary>
                public const string DatatypeId = Prefix.Entity + "datatypeId";

                /// <summary>Chiều chia sẻ</summary>
                public const string Direction = Prefix.Entity + "direction";

                /// <summary>Chế độ đăng ký</summary>
                public const string Mode = Prefix.Entity + "mode";

                /// <summary>Lịch gửi</summary>
                public const string Schedule = Prefix.Entity + "schedule";

                /// <summary>Phễu lọc (ShareDataMapping)</summary>
                public const string MappingId = Prefix.Entity + "mappingId";

                /// <summary>Bộ mã quy đổi (ShareDataCodeSet)</summary>
                public const string CodeSetId = Prefix.Entity + "codeSetId";

                /// <summary>Mã bộ mã quy đổi (ShareDataCodeSet.Code)</summary>
                public const string CodeSetCode = Prefix.Entity + "codeSetCode";

                /// <summary>Bảng quy đổi giá trị (ShareDataCodeSet.ValuesJson)</summary>
                public const string CodeSetValues = Prefix.Entity + "codeSetValues";

                /// <summary>Giá trị nội bộ mặc định (ShareDataCodeSet.DefaultSourceValue)</summary>
                public const string CodeSetDefaultSourceValue = Prefix.Entity + "codeSetDefaultSourceValue";

                /// <summary>Giá trị đối tác mặc định (ShareDataCodeSet.DefaultPartnerValue)</summary>
                public const string CodeSetDefaultPartnerValue = Prefix.Entity + "codeSetDefaultPartnerValue";

                /// <summary>Gói tin (ShareDataPacket.Code)</summary>
                public const string PacketCode = Prefix.Entity + "packetCode";

                /// <summary>Bảng vật lý (ShareDataTable.TableName)</summary>
                public const string TableName = Prefix.Entity + "tableName";

                /// <summary>Field của gói tin (ShareDataTable.FieldsJson.fieldKey)</summary>
                public const string FieldKey = Prefix.Entity + "fieldKey";

                /// <summary>Thực thể gốc phía đối tác (ShareDataMapping.TargetRootEntity)</summary>
                public const string TargetRootEntity = Prefix.Entity + "targetRootEntity";

                /// <summary>Mã bản ghi</summary>
                public const string Code = Prefix.Entity + "code";

                /// <summary>Chu kỳ gửi (giây)</summary>
                public const string IntervalSeconds = Prefix.Entity + "intervalSeconds";

                /// <summary>Độ ưu tiên</summary>
                public const string Priority = Prefix.Entity + "priority";

                /// <summary>Thời gian gom sự kiện (giây)</summary>
                public const string DebounceSec = Prefix.Entity + "debounceSec";
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Exception
            /// Created date: 14/08/2026
            /// </summary>

            public class Exception
            {
                /// <summary>"Đối tác đang có phiên kết nối, vui lòng ngắt kết nối trước."</summary>
                public const string PartnerSessionActive = Prefix.Exception + "partnerSessionActive";

                /// <summary>"Không đổi được mã đối tác vì mốc gửi và gói tin nhận gắn theo mã. Muốn đổi mã, hãy tạo đối tác mới."</summary>
                public const string PartnerCodeLocked = Prefix.Exception + "partnerCodeLocked";

                /// <summary>"Đối tác còn đăng ký đang chạy, vui lòng hủy trước khi xóa."</summary>
                public const string PartnerHasActiveSubscription = Prefix.Exception + "partnerHasActiveSubscription";

                /// <summary>"Đối tác đang ở trạng thái không sử dụng."</summary>
                public const string PartnerDisabled = Prefix.Exception + "partnerDisabled";

                /// <summary>"Đối tác chưa kết nối, vui lòng kết nối trước khi bật đăng ký."</summary>
                public const string PartnerNotConnected = Prefix.Exception + "partnerNotConnected";

                /// <summary>"Đối tác đã có đăng ký cho gói tin này."</summary>
                public const string SubscriptionDuplicated = Prefix.Exception + "subscriptionDuplicated";

                /// <summary>"Gói tin từ đối tác [{0}], loại [{1}], số thứ tự [{2}] đã tồn tại (trùng lặp)."</summary>
                public const string InboundPacketDuplicated = Prefix.Exception + "inboundPacketDuplicated";

                /// <summary>"Không thể chuyển trạng thái đăng ký từ [{0}] sang [{1}]."</summary>
                public const string SubscriptionStateInvalid = Prefix.Exception + "subscriptionStateInvalid";

                /// <summary>"Đăng ký đã kết thúc, không thể sửa."</summary>
                public const string SubscriptionFinished = Prefix.Exception + "subscriptionFinished";

                /// <summary>"Vui lòng tắt đăng ký trước khi sửa."</summary>
                public const string SubscriptionMustPauseBeforeUpdate = Prefix.Exception + "subscriptionMustPauseBeforeUpdate";

                /// <summary>"Đăng ký đang chạy, vui lòng tắt trước khi xóa."</summary>
                public const string SubscriptionMustPauseBeforeDelete = Prefix.Exception + "subscriptionMustPauseBeforeDelete";

                /// <summary>"Chưa có ánh xạ đang áp dụng cho đối tác và gói tin này."</summary>
                public const string MappingNotResolved = Prefix.Exception + "mappingNotResolved";

                // ── Phễu lọc (ShareDataMapping) — ESH-21xx ───────────────

                /// <summary>ESH-2101 · "Đã có phễu lọc đang áp dụng cho cùng đối tác, gói tin và chiều."</summary>
                public const string MappingActiveDuplicated = Prefix.Exception + "mappingActiveDuplicated";

                /// <summary>ESH-2102 · "Field [{0}] không tồn tại trong gói tin."</summary>
                public const string MappingFieldKeyNotExist = Prefix.Exception + "mappingFieldKeyNotExist";

                /// <summary>ESH-2103 · "Có hai dòng cùng ánh xạ về [{0}]."</summary>
                public const string MappingTargetDuplicated = Prefix.Exception + "mappingTargetDuplicated";

                /// <summary>ESH-2104 · "Bộ mã [{0}] không tồn tại."</summary>
                public const string MappingCodeSetNotExist = Prefix.Exception + "mappingCodeSetNotExist";

                /// <summary>ESH-2105 · "Gói tin [{0}] không tồn tại."</summary>
                public const string MappingPacketNotExist = Prefix.Exception + "mappingPacketNotExist";

                /// <summary>ESH-2106 · "Hồ sơ ánh xạ đang được đăng ký (Subscription) đang bật sử dụng, không thể sửa/xóa."</summary>
                public const string MappingInUse = Prefix.Exception + "mappingInUse";

                /// <summary>ESH-2107 · "Không thể bật hồ sơ ánh xạ này vì hồ sơ đang áp dụng cho cùng đối tác/gói tin/chiều đang được đăng ký bật sử dụng — vui lòng tạm dừng đăng ký đó trước."</summary>
                public const string MappingConflictInUse = Prefix.Exception + "mappingConflictInUse";

                /// <summary>ESH-2108 · "Hồ sơ có chiều gửi phải khai đủ các giá trị hệ thống. Còn thiếu: [{0}]."</summary>
                public const string MappingMetaFieldsRequired = Prefix.Exception + "mappingMetaFieldsRequired";

                // ── Bộ mã quy đổi (ShareDataCodeSet) — ESH-22xx ──────────

                /// <summary>ESH-2203 · "Giá trị nguồn [{0}] bị trùng trong cùng bộ mã."</summary>
                public const string CodeSetSourceValueDuplicated = Prefix.Exception + "codeSetSourceValueDuplicated";

                /// <summary>ESH-2204 · "Bộ mã đang được gói tin hoặc phễu lọc sử dụng, không thể xóa."</summary>
                public const string CodeSetInUse = Prefix.Exception + "codeSetInUse";

                /// <summary>ESH-2205 · "Giá trị đối tác [{0}] bị trùng trong cùng bộ mã."</summary>
                public const string CodeSetPartnerValueDuplicated = Prefix.Exception + "codeSetPartnerValueDuplicated";

                /// <summary>ESH-2206 · "Bộ mã [{0}] đang được hồ sơ ánh xạ có đăng ký bật sử dụng ({1}), vui lòng tạm dừng đăng ký trước khi sửa bảng quy đổi hoặc giá trị mặc định."</summary>
                public const string CodeSetValuesInUse = Prefix.Exception + "codeSetValuesInUse";

                // ── Gói tin (ShareDataPacket) — ESH-23xx ──

                /// <summary>ESH-2301 · "Gói tin đang được phễu lọc sử dụng, không thể xóa."</summary>
                public const string PacketInUse = Prefix.Exception + "packetInUse";

                /// <summary>ESH-2302 · "Gói tin đang được phễu lọc sử dụng, chỉ được sửa các trường mô tả, không được đổi Mã/Tên."</summary>
                public const string PacketCodeNameLocked = Prefix.Exception + "packetCodeNameLocked";

                /// <summary>ESH-2305 · "Gói tin [{0}] chưa có câu lệnh SQL trích xuất (ExtractSql)."</summary>
                public const string PacketExtractSqlMissing = Prefix.Exception + "packetExtractSqlMissing";

                /// <summary>ESH-2306 · "Gói tin [{0}] đang có đăng ký bật sử dụng ({1}), vui lòng tắt đăng ký trước khi sửa cấu hình SQL, đặt mốc hoặc đặt lại mốc gửi."</summary>
                public const string PacketSqlInUse = Prefix.Exception + "packetSqlInUse";

                /// <summary>ESH-2307 · "Gói tin [{0}] đang có đăng ký bật sử dụng ({1}), vui lòng tắt đăng ký trước khi tắt gói tin."</summary>
                public const string PacketDisableInUse = Prefix.Exception + "packetDisableInUse";

                /// <summary>"Không lấy được ngữ cảnh HTTP để đọc nội dung gói tin."</summary>
                public const string HttpContextUnavailable = Prefix.Exception + "httpContextUnavailable";

                /// <summary>"Chỉ tài khoản SuperAdmin mới có quyền chỉnh sửa câu truy vấn nguồn dữ liệu."</summary>
                public const string SuperAdminRequiredForSqlEdit = Prefix.Exception + "superAdminRequiredForSqlEdit";

                /// <summary>"Chỉ tài khoản SuperAdmin mới có quyền xem cấu hình câu truy vấn nguồn dữ liệu."</summary>
                public const string SuperAdminRequiredForSqlView = Prefix.Exception + "superAdminRequiredForSqlView";

                /// <summary>"Chỉ tài khoản SuperAdmin mới có quyền kiểm tra câu truy vấn nguồn dữ liệu."</summary>
                public const string SuperAdminRequiredForSqlValidate = Prefix.Exception + "superAdminRequiredForSqlValidate";

                /// <summary>"Chỉ tài khoản SuperAdmin mới có quyền chạy thử câu truy vấn nguồn dữ liệu."</summary>
                public const string SuperAdminRequiredForSqlPreview = Prefix.Exception + "superAdminRequiredForSqlPreview";

                /// <summary>"Lỗi thực thi câu truy vấn mẫu: {0}"</summary>
                public const string SqlSampleExecutionFailed = Prefix.Exception + "sqlSampleExecutionFailed";


                /// <summary>"Không tìm thấy gói tin có mã [{0}]."</summary>
                public const string PacketCodeNotFound = Prefix.Exception + "packetCodeNotFound";

                /// <summary>"Chỉ tài khoản SuperAdmin mới có quyền truy cập siêu dữ liệu CSDL."</summary>
                public const string SuperAdminRequiredForMetadata = Prefix.Exception + "superAdminRequiredForMetadata";

                /// <summary>"Không tìm thấy hồ sơ ánh xạ đối tác."</summary>
                public const string PartnerMappingNotFound = Prefix.Exception + "partnerMappingNotFound";
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Validation
            /// Created date: 14/08/2026
            /// </summary>

            public class Validation
            {
                /// <summary>"Chế độ theo sự kiện bắt buộc chọn nguồn sự kiện."</summary>
                public const string EventSourceRequired = Prefix.Validation + "eventSourceRequired";

                /// <summary>"Chế độ định kỳ bắt buộc cấu hình lịch gửi."</summary>
                public const string ScheduleRequired = Prefix.Validation + "scheduleRequired";

                /// <summary>"Loại dữ liệu chia sẻ phải nằm trong khoảng 101–111."</summary>
                public const string DatatypeIdOutOfRange = Prefix.Validation + "datatypeIdOutOfRange";

                /// <summary>"Bảng quy đổi phải có ít nhất một dòng."</summary>
                public const string CodeSetValuesRequired = Prefix.Validation + "codeSetValuesRequired";

                /// <summary>"Mỗi dòng phễu lọc bắt buộc chỉ ra field của gói tin."</summary>
                public const string MappingFieldKeyRequired = Prefix.Validation + "mappingFieldKeyRequired";

                /// <summary>"Thiếu header PartnerCode."</summary>
                public const string PartnerCodeHeaderRequired = Prefix.Validation + "partnerCodeHeaderRequired";

                /// <summary>"Thiếu header PacketCode."</summary>
                public const string PacketCodeHeaderRequired = Prefix.Validation + "packetCodeHeaderRequired";

                /// <summary>"Giờ kết thúc phải lớn hơn hoặc bằng giờ bắt đầu."</summary>
                public const string ScheduleTimeRangeInvalid = Prefix.Validation + "scheduleTimeRangeInvalid";

                /// <summary>"Mã gói tin không được để trống."</summary>
                public const string PacketCodeRequired = Prefix.Validation + "packetCodeRequired";

                /// <summary>"Tên bảng nguồn không được để trống."</summary>
                public const string TableNameRequired = Prefix.Validation + "tableNameRequired";

                /// <summary>"Tên bảng '{0}' không hợp lệ hoặc chứa ký tự bị cấm."</summary>
                public const string TableNameInvalid = Prefix.Validation + "tableNameInvalid";

                /// <summary>"Bảng '{0}' không thuộc danh mục được phép chia sẻ dữ liệu."</summary>
                public const string TableNotAllowed = Prefix.Validation + "tableNotAllowed";

                /// <summary>"Câu lệnh truy vấn không được để trống."</summary>
                public const string SqlScriptEmpty = Prefix.Validation + "sqlScriptEmpty";

                /// <summary>"Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2}"</summary>
                public const string SqlScriptSyntaxError = Prefix.Validation + "sqlScriptSyntaxError";

                /// <summary>"Cú pháp SQL không hợp lệ hoặc không chứa script thực thi."</summary>
                public const string SqlScriptInvalid = Prefix.Validation + "sqlScriptInvalid";

                /// <summary>"Chỉ chấp nhận duy nhất một lô lệnh (batch). Không được sử dụng lệnh phân đoạn 'GO'."</summary>
                public const string SqlMultipleBatchesNotAllowed = Prefix.Validation + "sqlMultipleBatchesNotAllowed";

                /// <summary>"Chỉ chấp nhận duy nhất một câu lệnh SELECT (không cho phép đa câu lệnh)."</summary>
                public const string SqlMultipleStatementsNotAllowed = Prefix.Validation + "sqlMultipleStatementsNotAllowed";

                /// <summary>"Chỉ chấp nhận câu lệnh đọc dữ liệu bắt đầu bằng SELECT."</summary>
                public const string SqlOnlySelectAllowed = Prefix.Validation + "sqlOnlySelectAllowed";

                /// <summary>"Không cho phép sử dụng cú pháp SELECT ... INTO tạo bảng mới."</summary>
                public const string SqlSelectIntoNotAllowed = Prefix.Validation + "sqlSelectIntoNotAllowed";

                /// <summary>"Không cho phép gọi hàm hoặc thủ tục: [{0}]."</summary>
                public const string SqlFunctionCallBlocked = Prefix.Validation + "sqlFunctionCallBlocked";

                /// <summary>"Không cho phép sử dụng các nguồn dữ liệu ngoài (OPENROWSET/OPENQUERY/...)."</summary>
                public const string SqlExternalDataSourcesBlocked = Prefix.Validation + "sqlExternalDataSourcesBlocked";

                /// <summary>"Không cho phép thực thi câu lệnh EXEC/EXECUTE."</summary>
                public const string SqlExecuteStatementBlocked = Prefix.Validation + "sqlExecuteStatementBlocked";

                /// <summary>"Không được phép truy vấn bảng vượt ra ngoài CSDL hiện tại (tên bảng 3-4 phần: [{0}])."</summary>
                public const string SqlCrossDatabaseQueryBlocked = Prefix.Validation + "sqlCrossDatabaseQueryBlocked";

                /// <summary>"Không được phép truy vấn schema hệ thống: [{0}]."</summary>
                public const string SqlSystemSchemaBlocked = Prefix.Validation + "sqlSystemSchemaBlocked";

                /// <summary>"Không được phép truy vấn bảng hệ thống nhạy cảm: [{0}]."</summary>
                public const string SqlSensitiveTableBlocked = Prefix.Validation + "sqlSensitiveTableBlocked";

                /// <summary>"Biến [@{0}] chưa khai ở bảng Tham số câu truy vấn. Mọi biến dùng trong câu SQL phải được khai ở bảng này."</summary>
                public const string SqlUnsupportedParameter = Prefix.Validation + "sqlUnsupportedParameter";

                /// <summary>"Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0}"</summary>
                public const string SqlDatabaseExecutionError = Prefix.Validation + "sqlDatabaseExecutionError";

                /// <summary>"Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề ORDER BY ở cấp ngoài cùng."</summary>
                public const string SqlIncrementalMissingOrderBy = Prefix.Validation + "sqlIncrementalMissingOrderBy";

                /// <summary>"Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC) ở mệnh đề ORDER BY ngoài cùng."</summary>
                public const string SqlIncrementalOrderByDescNotAllowed = Prefix.Validation + "sqlIncrementalOrderByDescNotAllowed";

                /// <summary>"Truy vấn đồng bộ nối đuôi theo khóa phải sắp xếp ORDER BY ngoài cùng theo cột [__lastKey], nếu không có thể bỏ sót dòng."</summary>
                public const string SqlIncrementalOrderByKeyMismatch = Prefix.Validation + "sqlIncrementalOrderByKeyMismatch";

                /// <summary>"Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ khóa [__lastKey]."</summary>
                public const string SqlIncrementalMissingLastKey = Prefix.Validation + "sqlIncrementalMissingLastKey";

                /// <summary>"Điều kiện WHERE của truy vấn nối đuôi theo khóa phải có [{1}] > @{0}, ..."</summary>
                public const string SqlIncrementalKeyFilterInvalid = Prefix.Validation + "sqlIncrementalKeyFilterInvalid";

                /// <summary>"Nhóm mốc [{0}] còn thiếu {1}. Mỗi nhóm cần cột [__lastKey_{0}] và biến @lastKey_{0}."</summary>
                public const string SqlIncrementalGroupColumnMissing = Prefix.Validation + "sqlIncrementalGroupColumnMissing";

                /// <summary>"Câu SQL đối tác đang chạy không có nhóm mốc [{0}]."</summary>
                public const string SqlIncrementalGroupNotFound = Prefix.Validation + "sqlIncrementalGroupNotFound";

                /// <summary>"Nhóm mốc [{0}]: ORDER BY ngoài cùng sắp theo [{1}], ở nhánh của nhóm này cột đó khác công thức cột [__lastKey_{0}] nên dòng ra sai thứ tự mốc. Hãy sắp theo cột có cùng công thức cột mốc ở mọi nhánh."</summary>
                public const string SqlIncrementalGroupOrderInvalid = Prefix.Validation + "sqlIncrementalGroupOrderInvalid";

                /// <summary>"Cột mốc chỉ có ngày giờ nên sót các dòng trùng giờ. Ghép thêm đuôi phân biệt dòng: CONVERT(varchar(8), &lt;cột thời gian&gt;, 112) + REPLACE(CONVERT(varchar(12), &lt;cột thời gian&gt;, 114), ':', '') + '_' + ID."</summary>
                public const string SqlIncrementalKeyTimeOnly = Prefix.Validation + "sqlIncrementalKeyTimeOnly";

                /// <summary>"Cột mốc [{0}] dùng UpdateTime trơn nên dòng mới thêm (chưa có UpdateTime) không được gửi. Dùng COALESCE(UpdateTime, CreateTime) ở cả cột mốc và WHERE."</summary>
                public const string SqlIncrementalUpdateTimeEmpty = Prefix.Validation + "sqlIncrementalUpdateTimeEmpty";

                /// <summary>"Cột mốc [{0}]: so với biến mốc phải dùng dấu &gt;; dùng &gt;= thì dòng cuối bị gửi lặp mỗi lượt."</summary>
                public const string SqlIncrementalKeyFilterInclusive = Prefix.Validation + "sqlIncrementalKeyFilterInclusive";

                /// <summary>"Cột mốc ghép ngày giờ sai định dạng nên sắp sai thứ tự, có thể bỏ sót dòng. Dùng CONVERT(varchar(8), &lt;cột thời gian&gt;, 112) + REPLACE(CONVERT(varchar(12), &lt;cột thời gian&gt;, 114), ':', '')."</summary>
                public const string SqlIncrementalKeyDateUnsortable = Prefix.Validation + "sqlIncrementalKeyDateUnsortable";

                /// <summary>"Câu có cột mốc [{0}] nhưng không dùng biến mốc (nguồn Theo Mốc đang chạy) nên lượt nào cũng gửi lại cùng dữ liệu. So cột mốc với biến lastKey trong WHERE, hoặc bỏ cột mốc nếu muốn gửi toàn bộ."</summary>
                public const string SqlIncrementalCursorVariableMissing = Prefix.Validation + "sqlIncrementalCursorVariableMissing";

                /// <summary>"Cột mốc phải viết đúng công thức CONVERT(varchar(8), &lt;thời gian&gt;, 112) + REPLACE(CONVERT(varchar(12), &lt;thời gian&gt;, 114), ':', '') + '_' + &lt;đuôi&gt;; phần thời gian phải có CreateTime, đuôi không được NULL (cột có thể NULL thì bọc ISNULL(&lt;cột&gt;, '')); viết khác thì có thể bỏ sót dòng."</summary>
                public const string SqlIncrementalKeyFormulaInvalid = Prefix.Validation + "sqlIncrementalKeyFormulaInvalid";

                /// <summary>"Câu nối đuôi không được dùng OFFSET: các dòng bị bỏ qua sẽ không bao giờ được gửi. Giới hạn số dòng bằng TOP."</summary>
                public const string SqlIncrementalOffsetNotAllowed = Prefix.Validation + "sqlIncrementalOffsetNotAllowed";

                /// <summary>"Câu gộp nhiều nguồn không dùng chung một mốc: nguồn về trễ sẽ mất dòng. Mỗi nguồn cần cột __lastKey_nhóm và biến lastKey_nhóm riêng."</summary>
                public const string SqlIncrementalUnionOneCursor = Prefix.Validation + "sqlIncrementalUnionOneCursor";

                /// <summary>"Câu con có TOP phải sắp tăng dần theo cột mốc: sắp theo cột khác thì dòng có mốc nhỏ hơn bị bỏ sót. Đổi ORDER BY trong câu con thành cột mốc."</summary>
                public const string SqlIncrementalInnerTopOrderInvalid = Prefix.Validation + "sqlIncrementalInnerTopOrderInvalid";

                /// <summary>"Cấu hình kết nối (ConnectionConfig) phải là đối tượng JSON phẳng dạng key-value."</summary>
                public const string ConnectConfigNotJsonObject = Prefix.Validation + "connectConfigNotJsonObject";

                /// <summary>"Cấu hình kết nối (ConnectionConfig) sai cú pháp JSON."</summary>
                public const string ConnectConfigSyntaxError = Prefix.Validation + "connectConfigSyntaxError";

                /// <summary>"Cấu hình kết nối (ConnectionConfig) không được chứa khóa (Key) rỗng."</summary>
                public const string ConnectConfigEmptyKey = Prefix.Validation + "connectConfigEmptyKey";

                /// <summary>"Khóa cấu hình bị cấm sử dụng vì là khóa dành riêng của hệ thống."</summary>
                public const string ConnectConfigReservedKey = Prefix.Validation + "connectConfigReservedKey";

                /// <summary>"Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]."</summary>
                public const string ConnectConfigObsoleteKey = Prefix.Validation + "connectConfigObsoleteKey";

                /// <summary>"Cấu hình kết nối (ConnectionConfig) chứa khóa bị trùng lặp (không phân biệt hoa thường)."</summary>
                public const string ConnectConfigDuplicateKey = Prefix.Validation + "connectConfigDuplicateKey";

                /// <summary>"Cấu hình kết nối (ConnectionConfig) chỉ cho phép giá trị phẳng (chuỗi/số/boolean), không cho phép lồng mảng hoặc object con."</summary>
                public const string ConnectConfigNestedValue = Prefix.Validation + "connectConfigNestedValue";

                /// <summary>"Giá trị Timeout phải là số nguyên dương (giây)."</summary>
                public const string ConnectConfigInvalidTimeout = Prefix.Validation + "connectConfigInvalidTimeout";

                /// <summary>"Giá trị cấu hình boolean chỉ chấp nhận 'true' hoặc 'false'."</summary>
                public const string ConnectConfigInvalidBoolean = Prefix.Validation + "connectConfigInvalidBoolean";

                /// <summary>"Giá trị AuthType chỉ hỗ trợ: None, Bearer, ApiKey, Basic."</summary>
                public const string ConnectConfigUnsupportedAuthType = Prefix.Validation + "connectConfigUnsupportedAuthType";

                /// <summary>"Giá trị mã hóa (Encryptor / InboundEncryptor) chỉ hỗ trợ: None, AesGcm."</summary>
                public const string ConnectConfigUnsupportedEncryptor = Prefix.Validation + "connectConfigUnsupportedEncryptor";

                /// <summary>"Thuộc tính [FileNamePattern] bắt buộc chứa ít nhất một biến động ({serial}, {timestamp}, {packet} hoặc {partner}) để tránh ghi đè tệp."</summary>
                public const string ConnectConfigFileNamePatternNoToken = Prefix.Validation + "connectConfigFileNamePatternNoToken";

                /// <summary>"Bảng {0} chưa có trong danh mục bảng nguồn được phép chia sẻ. Vui lòng liên hệ quản trị viên để bổ sung."</summary>
                public const string SqlTableNotAllowed = Prefix.Validation + "sqlTableNotAllowed";

                /// <summary>"{0} phải nằm trong khoảng {1}..{2}."</summary>
                public const string ValueOutOfRange = Prefix.Validation + "valueOutOfRange";

                /// <summary>"Mã hồ sơ ánh xạ không được để trống."</summary>
                public const string MappingIdRequired = Prefix.Validation + "mappingIdRequired";

                /// <summary>"Câu truy vấn trích xuất riêng chỉ áp dụng cho hồ sơ ánh xạ chiều gửi đi (Outbound hoặc Hai chiều)."</summary>
                public const string PartnerSqlDirectionInvalid = Prefix.Validation + "partnerSqlDirectionInvalid";

                /// <summary>"Hồ sơ ánh xạ đối tác đang tắt, không thể cấu hình câu truy vấn trích xuất riêng."</summary>
                public const string PartnerSqlMappingNotActive = Prefix.Validation + "partnerSqlMappingNotActive";

                /// <summary>"Câu truy vấn trích xuất riêng không được để trống. Muốn dùng lại mẫu chung, hãy chọn Dùng lại mẫu chung."</summary>
                public const string PartnerExtractSqlRequired = Prefix.Validation + "partnerExtractSqlRequired";

                /// <summary>"Câu ghi chiều nhận riêng không được để trống. Muốn dùng lại mẫu chung, hãy chọn Dùng lại mẫu chung."</summary>
                public const string PartnerWriteSqlRequired = Prefix.Validation + "partnerWriteSqlRequired";

                /// <summary>"Cột thứ {0} của câu SQL chưa có tên. Hãy đặt tên cột bằng AS, tên cột chính là khoá trường của gói tin."</summary>
                public const string SqlColumnUnnamed = Prefix.Validation + "sqlColumnUnnamed";

                /// <summary>"Câu SQL trả về trùng tên cột [{0}]. Hãy đặt tên khác bằng AS."</summary>
                public const string SqlColumnDuplicate = Prefix.Validation + "sqlColumnDuplicate";

                /// <summary>"Khoá trường [{0}] không có trong danh sách trường của câu SQL."</summary>
                public const string SqlFieldKeyNotFound = Prefix.Validation + "sqlFieldKeyNotFound";

                /// <summary>"Trường [{0}]: kiểu [{1}] không có trong danh sách kiểu dữ liệu."</summary>
                public const string SqlFieldTypeInvalid = Prefix.Validation + "sqlFieldTypeInvalid";

                /// <summary>"Trường [{0}]: {1} vượt quá {2} ký tự."</summary>
                public const string SqlFieldValueTooLong = Prefix.Validation + "sqlFieldValueTooLong";

                /// <summary>"Không thể bỏ qua trường [{0}] vì hồ sơ ánh xạ [{1}] đang dùng. Hãy bỏ chọn trường ở hồ sơ ánh xạ trước."</summary>
                public const string SqlFieldSkipInUse = Prefix.Validation + "sqlFieldSkipInUse";
                /// <summary>"Tên biến [{0}] không hợp lệ: bắt đầu bằng chữ cái, chỉ gồm chữ, số và dấu gạch dưới, tối đa 64 ký tự."</summary>
                public const string SqlVariableCodeInvalid = Prefix.Validation + "sqlVariableCodeInvalid";

                /// <summary>"Tên biến [{0}] bị trùng."</summary>
                public const string SqlVariableCodeDuplicate = Prefix.Validation + "sqlVariableCodeDuplicate";

                /// <summary>"Nguồn giá trị [{1}] của biến [{0}] không hợp lệ."</summary>
                public const string SqlVariableSourceInvalid = Prefix.Validation + "sqlVariableSourceInvalid";

                /// <summary>"Giá trị của biến [{0}] không hợp lệ hoặc vượt quá 256 ký tự (kiểu [{1}])."</summary>
                public const string SqlVariableValueInvalid = Prefix.Validation + "sqlVariableValueInvalid";

                /// <summary>"Câu ghi đang trống."</summary>
                public const string WriteSqlEmpty = Prefix.Validation + "writeSqlEmpty";

                /// <summary>"Chưa nhập bảng được phép ghi."</summary>
                public const string WriteTableRequired = Prefix.Validation + "writeTableRequired";

                /// <summary>"Câu ghi phải là một câu lệnh MERGE, INSERT hoặc UPDATE."</summary>
                public const string WriteSqlStatementInvalid = Prefix.Validation + "writeSqlStatementInvalid";

                /// <summary>"Câu ghi ghi vào bảng [{0}], khác bảng được phép ghi [{1}]."</summary>
                public const string WriteSqlTableNotAllowed = Prefix.Validation + "writeSqlTableNotAllowed";

                /// <summary>"Bảng [{0}] có trong danh sách bảng bị ảnh hưởng nhưng chưa có câu ghi nào trong khối tác động tới bảng này."</summary>
                public const string WriteSqlTableUnused = Prefix.Validation + "writeSqlTableUnused";

                /// <summary>"Câu ghi không được dùng {0}."</summary>
                public const string WriteSqlForbidden = Prefix.Validation + "writeSqlForbidden";

                /// <summary>"Câu ghi không được dùng bảng ở CSDL khác: {0}."</summary>
                public const string WriteSqlCrossDatabase = Prefix.Validation + "writeSqlCrossDatabase";

                /// <summary>"Tham số [@{0}] không có trong danh sách tham số hệ thống: {1}."</summary>
                public const string WriteSqlParameterInvalid = Prefix.Validation + "writeSqlParameterInvalid";

                /// <summary>"Bảng [{0}] không có trong CSDL nhận."</summary>
                public const string WriteTableNotFound = Prefix.Validation + "writeTableNotFound";

                /// <summary>"CSDL nhận báo lỗi khi biên dịch câu ghi: {0}"</summary>
                public const string WriteSqlCompileError = Prefix.Validation + "writeSqlCompileError";

                /// <summary>"Chưa cấu hình kết nối CSDL nhận, chưa kiểm được bảng và câu ghi trên CSDL."</summary>
                public const string WriteDbNotConfigured = Prefix.Validation + "writeDbNotConfigured";

                /// <summary>"Câu ghi không đọc OPENJSON(@records): không dùng dữ liệu đối tác gửi về."</summary>
                public const string WriteSqlRecordsNotUsed = Prefix.Validation + "writeSqlRecordsNotUsed";

                /// <summary>"Cột [{0}] trong OPENJSON … WITH không có trong danh sách trường của gói tin."</summary>
                public const string WriteSqlFieldNotInPacket = Prefix.Validation + "writeSqlFieldNotInPacket";

                /// <summary>"Cột [{0}] khác chữ hoa/thường với trường [{1}] của gói tin, OPENJSON sẽ đọc ra NULL."</summary>
                public const string WriteSqlFieldCaseMismatch = Prefix.Validation + "writeSqlFieldCaseMismatch";

                /// <summary>"Không dò được khoá bản ghi từ điều kiện ON của câu MERGE."</summary>
                public const string WriteSqlKeyNotFound = Prefix.Validation + "writeSqlKeyNotFound";

                /// <summary>"Bảng nguồn chưa GROUP BY theo khoá [{0}]: đối tác gửi trùng khoá thì cả gói lỗi."</summary>
                public const string WriteSqlKeyNotGrouped = Prefix.Validation + "writeSqlKeyNotGrouped";

                /// <summary>"Bảng nguồn chưa lọc [{0}] IS NOT NULL: bản ghi thiếu khoá sẽ lỗi khi thêm mới."</summary>
                public const string WriteSqlKeyNotFiltered = Prefix.Validation + "writeSqlKeyNotFiltered";

                /// <summary>"Câu MERGE chưa có OUTPUT $action: nhật ký không đếm được số dòng thêm/sửa."</summary>
                public const string WriteSqlMissingOutputAction = Prefix.Validation + "writeSqlMissingOutputAction";

                /// <summary>"Câu ghi #{0} ({1}): {2}"</summary>
                public const string WriteSqlItemInvalid = Prefix.Validation + "writeSqlItemInvalid";

                /// <summary>"Dữ liệu chạy thử phải là mảng JSON các bản ghi."</summary>
                public const string WriteSqlRecordsInvalid = Prefix.Validation + "writeSqlRecordsInvalid";




            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Message
            /// Created date: 14/08/2026
            /// </summary>

            public class Message
            {
                /// <summary>"Đã thiết lập phiên kết nối với đối tác."</summary>
                public const string PartnerConnected = Prefix.Message + "partnerConnected";

                /// <summary>"Đã ngắt kết nối với đối tác."</summary>
                public const string PartnerDisconnected = Prefix.Message + "partnerDisconnected";
            }
        }
    }
}
