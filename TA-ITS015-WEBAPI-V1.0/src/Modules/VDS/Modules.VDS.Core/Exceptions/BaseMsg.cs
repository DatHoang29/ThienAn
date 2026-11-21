using Shared.DTO.Constants.Localization;

namespace Modules.VDS.Core.Exceptions
{
    /// <summary>
    /// Author: KienLT
    /// Description: Localization keys riêng của module VDS, kế thừa <see cref="BaseLocaleManager"/> nên
    /// dùng lại được toàn bộ key chung (<c>BaseValidation.*</c>, <c>BaseException.*</c>, <c>BaseEntity.*</c>).
    /// Chỉ khai ở đây những key ĐẶC THÙ của VDS mà key chung chưa có.
    ///
    /// Prefix theo quy ước: <c>lz.{category}.vds.{field}</c>.
    ///
    /// LƯU Ý phân loại (test LocalizationKeysTests kiểm): key <c>lz.validation.vds.*</c> BẮT BUỘC là
    /// validation template gắn qua <c>WithName</c> nên phải chứa placeholder <c>{PropertyName}</c>.
    /// Thông báo lỗi nghiệp vụ ném qua <c>Oops.Oh()</c> (không có {PropertyName}) phải nằm ở
    /// <c>lz.exception.vds.*</c>.
    /// Created date: 03/09/2026
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        public class Vds
        {
            protected class Prefix
            {
                public const string Message = "lz.message.vds.";
                public const string Validation = "lz.validation.vds.";
                public const string Exception = "lz.exception.vds.";
                public const string Entity = "lz.entity.vds.";
            }

            public class Entity
            {
                /// <summary>Mốc thời gian bắt đầu truy vấn.</summary>
                public const string FromDate = Prefix.Entity + "fromDate";

                /// <summary>Mốc thời gian kết thúc truy vấn.</summary>
                public const string ToDate = Prefix.Entity + "toDate";

                /// <summary>Cửa sổ thời gian truy vấn.</summary>
                public const string TimeWindow = Prefix.Entity + "timeWindow";

                /// <summary>Ngày cần truy vấn (cửa sổ cố định 24h, chỉ cần một mốc).</summary>
                public const string ValueDate = Prefix.Entity + "valueDate";

                /// <summary>Mã nhóm cấu hình VDS.</summary>
                public const string GroupCode = Prefix.Entity + "groupCode";

                /// <summary>Camera đầu đoạn tuyến.</summary>
                public const string FromEquipment = Prefix.Entity + "fromEquipment";

                /// <summary>Camera cuối đoạn tuyến.</summary>
                public const string ToEquipment = Prefix.Entity + "toEquipment";

                /// <summary>Chiều lưu thông của đoạn tuyến.</summary>
                public const string Direction = Prefix.Entity + "direction";

                /// <summary>Chiều dài đoạn tuyến, tính bằng mét.</summary>
                public const string DistanceMeter = Prefix.Entity + "distanceMeter";

                /// <summary>Thời gian di chuyển tối thiểu qua đoạn tuyến, tính bằng giây.</summary>
                public const string MinTravelSeconds = Prefix.Entity + "minTravelSeconds";

                /// <summary>Thời gian di chuyển tối đa qua đoạn tuyến, tính bằng giây.</summary>
                public const string MaxTravelSeconds = Prefix.Entity + "maxTravelSeconds";

                /// <summary>Chu kỳ tổng hợp: giờ / ngày / tháng.</summary>
                public const string PeriodType = Prefix.Entity + "periodType";

                /// <summary>Phạm vi tổng hợp: camera / hướng / vùng.</summary>
                public const string ScopeType = Prefix.Entity + "scopeType";

                /// <summary>Số trang.</summary>
                public const string Page = Prefix.Entity + "page";

                /// <summary>Số bản ghi một trang.</summary>
                public const string PageSize = Prefix.Entity + "pageSize";
            }

            /// <summary>Validation template gắn qua WithName — BẮT BUỘC có {PropertyName} trong bản dịch.</summary>
            public class Validation
            {
                /// <summary>ToDate phải sau FromDate.</summary>
                public const string ToDateAfterFromDate = Prefix.Validation + "toDateAfterFromDate";

                /// <summary>Cửa sổ thời gian vượt trần cho phép.</summary>
                public const string TimeWindowTooLarge = Prefix.Validation + "timeWindowTooLarge";

                /// <summary>Số bản ghi một trang vượt dải cho phép.</summary>
                public const string PageSizeOutOfRange = Prefix.Validation + "pageSizeOutOfRange";
            }

            /// <summary>Thông báo lỗi nghiệp vụ ném qua Oops.Oh — không có {PropertyName}.</summary>
            public class Exception
            {
                /// <summary>Tầng lưu trữ ghi đè ID tất định của đoạn khi thêm mới; tham số {0} ID mong đợi, {1} ID bị ghi.</summary>
                public const string SegmentIdOverwritten = Prefix.Exception + "segmentIdOverwritten";

                /// <summary>Không tìm thấy cấu hình VDS.</summary>
                public const string ConfigNotFound = Prefix.Exception + "configNotFound";

                /// <summary>Không được đổi mã hoặc nhóm của cấu hình hệ thống.</summary>
                public const string SysConfigCodeLocked = Prefix.Exception + "sysConfigCodeLocked";

                /// <summary>Không được xóa cấu hình hệ thống.</summary>
                public const string SysConfigDeleteLocked = Prefix.Exception + "sysConfigDeleteLocked";

                /// <summary>Mã cấu hình đã tồn tại trong nhóm.</summary>
                public const string CodeExistInGroup = Prefix.Exception + "codeExistInGroup";

                /// <summary>Camera không tồn tại.</summary>
                public const string CameraNotFound = Prefix.Exception + "cameraNotFound";

                /// <summary>Mã loại sự kiện ITS không tồn tại.</summary>
                public const string EventTypeNotFound = Prefix.Exception + "eventTypeNotFound";

                /// <summary>Mã SKIP/DEDUP phải là mã loại sự kiện hoặc sự cố TMS.</summary>
                public const string SkipDedupCodeInvalid = Prefix.Exception + "skipDedupCodeInvalid";

                /// <summary>Tham số chưa được hỗ trợ bởi API VDS.</summary>
                public const string ParameterNotSupported = Prefix.Exception + "parameterNotSupported";

                /// <summary>Đoạn tuyến A→B đã tồn tại.</summary>
                public const string SegmentExist = Prefix.Exception + "segmentExist";

                /// <summary>Đoạn tuyến không tồn tại.</summary>
                public const string SegmentNotFound = Prefix.Exception + "segmentNotFound";

                /// <summary>FromDate và ToDate là bắt buộc.</summary>
                public const string DateRangeRequired = Prefix.Exception + "dateRangeRequired";

                /// <summary>Khoảng thời gian phải tăng dần và không quá 60 ngày.</summary>
                public const string DateRangeInvalid = Prefix.Exception + "dateRangeInvalid";

                /// <summary>Tham số phân trang không hợp lệ.</summary>
                public const string PagingInvalid = Prefix.Exception + "pagingInvalid";

                /// <summary>Kết quả vượt 10000 bản ghi, cần thu hẹp bộ lọc.</summary>
                public const string ResultTooLarge = Prefix.Exception + "resultTooLarge";

                /// <summary>Ngưỡng tốc độ không hợp lệ.</summary>
                public const string SpeedThresholdInvalid = Prefix.Exception + "speedThresholdInvalid";

                /// <summary>MinSpeed phải nhỏ hơn hoặc bằng MaxSpeed.</summary>
                public const string MinSpeedGreaterThanMax = Prefix.Exception + "minSpeedGreaterThanMax";

                /// <summary>Chu kỳ thống kê phải từ 1 đến 60 phút.</summary>
                public const string StatisticPeriodInvalid = Prefix.Exception + "statisticPeriodInvalid";

                /// <summary>Thư mục media phải là đường dẫn tuyệt đối hợp lệ.</summary>
                public const string MediaDirectoryInvalid = Prefix.Exception + "mediaDirectoryInvalid";

                /// <summary>Danh sách camera không hợp lệ.</summary>
                public const string EquipmentListInvalid = Prefix.Exception + "equipmentListInvalid";

                /// <summary>Thời lượng phải có hậu tố s/m/h/d.</summary>
                public const string DurationSuffixRequired = Prefix.Exception + "durationSuffixRequired";

                /// <summary>Thời lượng vượt dải cho phép (0 đến 30 ngày).</summary>
                public const string DurationOutOfRange = Prefix.Exception + "durationOutOfRange";

                /// <summary>Nhóm cấu hình phải có tiền tố vds_ và trong giới hạn độ dài.</summary>
                public const string GroupPrefixInvalid = Prefix.Exception + "groupPrefixInvalid";

                /// <summary>Mã cấu hình bắt buộc và trong giới hạn độ dài.</summary>
                public const string CodeRequired = Prefix.Exception + "codeRequired";

                /// <summary>Tên cấu hình bắt buộc và trong giới hạn độ dài.</summary>
                public const string NameRequired = Prefix.Exception + "nameRequired";

                /// <summary>Giá trị cấu hình vượt giới hạn độ dài.</summary>
                public const string ValueTooLong = Prefix.Exception + "valueTooLong";

                /// <summary>Giá trị cấu hình không được rỗng với nhóm này.</summary>
                public const string ValueRequired = Prefix.Exception + "valueRequired";

                /// <summary>Trạng thái cấu hình phải là 0 hoặc 1.</summary>
                public const string StatusInvalid = Prefix.Exception + "statusInvalid";

                /// <summary>Ngưỡng quá hạn vùng phải thuộc nhóm mặc định và trong dải 1s đến 1 ngày.</summary>
                public const string ZoneStaleRangeInvalid = Prefix.Exception + "zoneStaleRangeInvalid";

                // KienLT - 31/10/2026: thông báo khi lưu tham số vận hành QuestDB HA (mã vds_questha_*).
                /// <summary>Mã vds_questha_* phải thuộc nhóm vds_config.</summary>
                public const string QuestHaGroupInvalid = Prefix.Exception + "questHaGroupInvalid";

                /// <summary>Mã vds_questha_* không nằm trong danh mục tham số QuestDB HA được hỗ trợ.</summary>
                public const string QuestHaCodeUnknown = Prefix.Exception + "questHaCodeUnknown";

                /// <summary>Giá trị tham số QuestDB HA sai định dạng hoặc ngoài biên cho phép.</summary>
                public const string QuestHaValueInvalid = Prefix.Exception + "questHaValueInvalid";

                /// <summary>Ngưỡng cảnh báo (Warn) phải nhỏ hơn ngưỡng nghiêm trọng (Critical) cùng cặp.</summary>
                public const string QuestHaWarnNotBelowCritical = Prefix.Exception + "questHaWarnNotBelowCritical";

                /// <summary>Giờ bắt đầu và kết thúc cửa sổ backup QuestDB HA không được trùng nhau.</summary>
                public const string QuestHaBackupWindowInvalid = Prefix.Exception + "questHaBackupWindowInvalid";

                /// <summary>Slot backup của từng node (lịch + độ lệch node) phải trong ngày và trong cửa sổ backup.</summary>
                public const string QuestHaBackupSlotOutsideWindow = Prefix.Exception + "questHaBackupSlotOutsideWindow";

                /// <summary>Khóa mapping cỡ xe không hợp lệ.</summary>
                public const string VehicleSizeKeyInvalid = Prefix.Exception + "vehicleSizeKeyInvalid";

                /// <summary>Mode mapping cỡ xe phải là length hoặc random.</summary>
                public const string VehicleSizeModeInvalid = Prefix.Exception + "vehicleSizeModeInvalid";

                /// <summary>Đơn vị mapping cỡ xe không hợp lệ.</summary>
                public const string VehicleSizeUnitInvalid = Prefix.Exception + "vehicleSizeUnitInvalid";

                /// <summary>Ngưỡng chiều dài phải là số dương.</summary>
                public const string VehicleSizeThresholdInvalid = Prefix.Exception + "vehicleSizeThresholdInvalid";

                /// <summary>Chu kỳ đếm phải là ước của 60 phút.</summary>
                public const string CountPeriodInvalid = Prefix.Exception + "countPeriodInvalid";

                /// <summary>Chưa cấu hình danh sách camera VDS.</summary>
                public const string EquipmentCatalogNotConfigured = Prefix.Exception + "equipmentCatalogNotConfigured";

                /// <summary>Không tìm thấy bản ghi VDS theo khóa đã cho.</summary>
                public const string RecordNotFound = Prefix.Exception + "recordNotFound";

                /// <summary>Thiếu mốc phát hiện trong khóa tra bản tin.</summary>
                public const string DetectTimeRequired = Prefix.Exception + "detectTimeRequired";

                /// <summary>Id bản tin ngoài miền giá trị mà nguồn dữ liệu sinh ra.</summary>
                public const string EventIdInvalid = Prefix.Exception + "eventIdInvalid";

                /// <summary>Chu kỳ hoặc phạm vi tổng hợp không hợp lệ.</summary>
                public const string SummaryPeriodScopeInvalid = Prefix.Exception + "summaryPeriodScopeInvalid";

                /// <summary>Nguồn dữ liệu VDS tạm thời không khả dụng (HTTP 503).</summary>
                public const string DataTemporarilyUnavailable = Prefix.Exception + "dataTemporarilyUnavailable";

                /// <summary>Hướng dẫn thử lại kèm theo lỗi 503; số giây nằm ở header Retry-After.</summary>
                public const string RetryAfterSeconds = Prefix.Exception + "retryAfterSeconds";

                /// <summary>Một nhịp SSE đọc dữ liệu thất bại; client nên chờ rồi nhận nhịp sau.</summary>
                public const string SseUnavailable = Prefix.Exception + "sseUnavailable";

                /// <summary>Không xác định được phạm vi dữ liệu của kết nối SSE nên luồng bị dừng.</summary>
                public const string SseScopeUnavailable = Prefix.Exception + "sseScopeUnavailable";

                /// <summary>Ghi chú vượt quá độ dài cho phép.</summary>
                public const string RemarkTooLong = Prefix.Exception + "remarkTooLong";

                /// <summary>shortMax phải nhỏ hơn mediumMax.</summary>
                public const string ShortMaxGreaterThanMedium = Prefix.Exception + "shortMaxGreaterThanMedium";

                /// <summary>Camera A/B hoặc thời gian di chuyển không hợp lệ.</summary>
                public const string SegmentEndpointsInvalid = Prefix.Exception + "segmentEndpointsInvalid";

                /// <summary>Bộ lọc thống kê chu kỳ không hỗ trợ biển số.</summary>
                public const string StatisticPlateNotSupported = Prefix.Exception + "statisticPlateNotSupported";

                /// <summary>Snapshot thống kê vượt trần thời gian/thiết bị.</summary>
                public const string StatisticSnapshotTooLarge = Prefix.Exception + "statisticSnapshotTooLarge";

                /// <summary>Kết quả thống kê quá lớn, cần thu hẹp bộ lọc.</summary>
                public const string StatisticResultTooLarge = Prefix.Exception + "statisticResultTooLarge";

                /// <summary>Cột sắp xếp không hợp lệ.</summary>
                public const string SortColumnInvalid = Prefix.Exception + "sortColumnInvalid";

                /// <summary>Chiều sắp xếp không hợp lệ.</summary>
                public const string SortDirectionInvalid = Prefix.Exception + "sortDirectionInvalid";

                /// <summary>LengthFrom phải nhỏ hơn hoặc bằng LengthTo.</summary>
                public const string LengthFromGreaterThanTo = Prefix.Exception + "lengthFromGreaterThanTo";

                /// <summary>SpeedFrom phải nhỏ hơn hoặc bằng SpeedTo.</summary>
                public const string SpeedFromGreaterThanTo = Prefix.Exception + "speedFromGreaterThanTo";

                /// <summary>Hành trình không tồn tại.</summary>
                public const string JourneyNotFound = Prefix.Exception + "journeyNotFound";

                /// <summary>Báo cáo tổng hợp không có biển số.</summary>
                public const string SummaryPlateNotSupported = Prefix.Exception + "summaryPlateNotSupported";

                /// <summary>Bộ lọc phải phù hợp với ScopeType.</summary>
                public const string SummaryScopeFilterMismatch = Prefix.Exception + "summaryScopeFilterMismatch";

                /// <summary>ScopeType phải là equipment hoặc zone.</summary>
                public const string ScopeTypeInvalid = Prefix.Exception + "scopeTypeInvalid";

                /// <summary>Loại media không hợp lệ.</summary>
                public const string MediaKindInvalid = Prefix.Exception + "mediaKindInvalid";

                /// <summary>Chưa cấu hình kho media.</summary>
                public const string MediaStorageNotConfigured = Prefix.Exception + "mediaStorageNotConfigured";

                /// <summary>Media chưa được tải hoặc không tồn tại.</summary>
                public const string MediaNotFound = Prefix.Exception + "mediaNotFound";
            }

            /// <summary>Thông báo trạng thái (không phải lỗi) gửi kèm frame SSE hoặc làm cảnh báo trong kết quả API.</summary>
            public class Message
            {
                /// <summary>Sinh đoạn/sơ đồ tuyến: danh sách <c>vds_list_equipment</c> rỗng.</summary>
                public const string TopologyNoEquipment = Prefix.Message + "topologyNoEquipment";

                /// <summary>Camera thiếu IP bị loại khỏi bộ sinh đoạn; tham số {0} mã camera.</summary>
                public const string TopologyCameraMissingIp = Prefix.Message + "topologyCameraMissingIp";

                /// <summary>Nhiều camera trùng IP; tham số {0} IP, {1} số camera, {2} danh sách mã, {3} mã được giữ.</summary>
                public const string TopologyDuplicateIp = Prefix.Message + "topologyDuplicateIp";

                /// <summary>Nhãn thay cho ID camera rỗng, dùng làm tham số của thông báo khác.</summary>
                public const string TopologyEmptyId = Prefix.Message + "topologyEmptyId";

                /// <summary>Cả camera đầu và cuối của đoạn không có trong danh sách; tham số {0}, {1} ID camera.</summary>
                public const string TopologyBothCamerasMissing = Prefix.Message + "topologyBothCamerasMissing";

                /// <summary>Camera đầu của đoạn không có trong danh sách; tham số {0} ID camera.</summary>
                public const string TopologyFromCameraMissing = Prefix.Message + "topologyFromCameraMissing";

                /// <summary>Camera cuối của đoạn không có trong danh sách; tham số {0} ID camera.</summary>
                public const string TopologyToCameraMissing = Prefix.Message + "topologyToCameraMissing";

                /// <summary>Camera của đoạn thiếu km/tuyến; tham số {0} danh sách mã camera.</summary>
                public const string TopologyCameraUnplaced = Prefix.Message + "topologyCameraUnplaced";

                /// <summary>Không có nút giao nối hai tuyến; tham số {0}, {1} mã tuyến.</summary>
                public const string TopologyNoJunction = Prefix.Message + "topologyNoJunction";

                /// <summary>Nhiều nút giao cùng nối hai tuyến; tham số {0} số nút, {1}, {2} mã tuyến, {3} danh sách mã nút.</summary>
                public const string TopologyAmbiguousJunction = Prefix.Message + "topologyAmbiguousJunction";

                /// <summary>Camera thiếu km/tuyến nên không vẽ được trên sơ đồ; tham số {0} mã camera.</summary>
                public const string TopologyCameraNotDrawable = Prefix.Message + "topologyCameraNotDrawable";

                /// <summary>Tuyến không thuộc nút giao nào; tham số {0} mã tuyến.</summary>
                public const string TopologyRouteWithoutJunction = Prefix.Message + "topologyRouteWithoutJunction";

                /// <summary>Tuyến vừa là tuyến a vừa là tuyến b của các nút khác nhau; tham số {0} mã tuyến.</summary>
                public const string TopologyRouteAxisConflict = Prefix.Message + "topologyRouteAxisConflict";

                /// <summary>Không dựng được đường đi của đoạn; tham số {0} mã đoạn, {1} lý do đã dịch.</summary>
                public const string TopologySegmentUnresolved = Prefix.Message + "topologySegmentUnresolved";

                /// <summary>Cấu hình sinh đoạn có lỗi nên bỏ đoạn tự sinh khỏi sơ đồ; tham số {0} số lỗi, {1} lỗi đầu.</summary>
                public const string TopologyGeneratedSegmentsSkipped = Prefix.Message + "topologyGeneratedSegmentsSkipped";

                /// <summary>Văn bản đã dựng sẵn, giữ nguyên khi dịch; tham số {0} văn bản.</summary>
                public const string TopologyRawText = Prefix.Message + "topologyRawText";

                /// <summary>Dòng nhóm vds_segment sai tiền tố; tham số {0} mã dòng, {1}, {2}, {3} các tiền tố hợp lệ.</summary>
                public const string TopologyConfigCodeInvalid = Prefix.Message + "topologyConfigCodeInvalid";

                /// <summary>Dòng chiều km sai dạng mã/giá trị; tham số {0} mã dòng, {1} giá trị.</summary>
                public const string TopologyKmDirInvalid = Prefix.Message + "topologyKmDirInvalid";

                /// <summary>Hai dòng chiều km cùng tuyến/hướng; tham số {0} mã dòng.</summary>
                public const string TopologyKmDirDuplicate = Prefix.Message + "topologyKmDirDuplicate";

                /// <summary>Camera thiếu km/tuyến/hướng bị bỏ khỏi bộ sinh đoạn; tham số {0} mã camera.</summary>
                public const string TopologyCameraIncomplete = Prefix.Message + "topologyCameraIncomplete";

                /// <summary>Một cặp camera có nhiều đường; tham số {0}, {1} mã camera, {2}, {3} mã hai đoạn.</summary>
                public const string TopologyPairMultiplePaths = Prefix.Message + "topologyPairMultiplePaths";

                /// <summary>Hai camera trùng km trên cùng làn; tham số {0}, {1} mã camera, {2} tuyến, {3} hướng.</summary>
                public const string TopologyCameraSameKm = Prefix.Message + "topologyCameraSameKm";

                /// <summary>Lượt rẽ thiếu Code.</summary>
                public const string TopologyTurnMissingCode = Prefix.Message + "topologyTurnMissingCode";

                /// <summary>Lượt rẽ trùng mã; tham số {0} mã dòng lượt rẽ.</summary>
                public const string TopologyTurnDuplicate = Prefix.Message + "topologyTurnDuplicate";

                /// <summary>Value lượt rẽ không phải JSON object; tham số {0} mã dòng lượt rẽ.</summary>
                public const string TopologyTurnInvalidJson = Prefix.Message + "topologyTurnInvalidJson";

                /// <summary>Lượt rẽ thiếu/sai kiểu trường; tham số {0} mã dòng lượt rẽ.</summary>
                public const string TopologyTurnInvalidFields = Prefix.Message + "topologyTurnInvalidFields";

                /// <summary>Lượt rẽ trỏ tới nút không tồn tại/lỗi; tham số {0} mã dòng lượt rẽ, {1} mã dòng nút.</summary>
                public const string TopologyTurnJunctionNotFound = Prefix.Message + "topologyTurnJunctionNotFound";

                /// <summary>Tuyến vào/ra của lượt rẽ không phải hai tuyến khác nhau của nút; tham số {0} mã dòng lượt rẽ, {1} mã nút.</summary>
                public const string TopologyTurnRoutesInvalid = Prefix.Message + "topologyTurnRoutesInvalid";

                /// <summary>Làn vào chạy ra xa nút; tham số {0} mã dòng lượt rẽ, {1} tuyến, {2} hướng.</summary>
                public const string TopologyTurnInboundMismatch = Prefix.Message + "topologyTurnInboundMismatch";

                /// <summary>Làn ra chạy về nút; tham số {0} mã dòng lượt rẽ, {1} tuyến, {2} hướng.</summary>
                public const string TopologyTurnOutboundMismatch = Prefix.Message + "topologyTurnOutboundMismatch";

                /// <summary>Làn vào/ra của lượt rẽ không có camera; tham số {0} mã dòng lượt rẽ, {1} tuyến, {2} hướng.</summary>
                public const string TopologyTurnNoCamera = Prefix.Message + "topologyTurnNoCamera";

                /// <summary>Không đọc được dòng lượt rẽ; tham số {0} mã dòng lượt rẽ, {1} loại lỗi.</summary>
                public const string TopologyTurnUnreadable = Prefix.Message + "topologyTurnUnreadable";

                /// <summary>Mã đoạn sinh ra trùng; tham số {0} mã đoạn, {1} số cặp camera.</summary>
                public const string TopologySegmentCodeDuplicate = Prefix.Message + "topologySegmentCodeDuplicate";

                /// <summary>Tốc độ trần sai; tham số {0} mã cấu hình, {1} giá trị, {2}, {3} khoảng cho phép.</summary>
                public const string TopologyPlausibleSpeedInvalid = Prefix.Message + "topologyPlausibleSpeedInvalid";

                /// <summary>Tốc độ trần quá cao (chỉ để test); tham số {0} mã cấu hình, {1} giá trị, {2} mức thực tế.</summary>
                public const string TopologyPlausibleSpeedUnrealistic = Prefix.Message + "topologyPlausibleSpeedUnrealistic";

                /// <summary>Value nút giao không phải JSON object; tham số {0} mã dòng nút.</summary>
                public const string TopologyJunctionInvalidJson = Prefix.Message + "topologyJunctionInvalidJson";

                /// <summary>Nút giao thiếu trường hoặc hai tuyến trùng nhau; tham số {0} mã dòng nút.</summary>
                public const string TopologyJunctionInvalidFields = Prefix.Message + "topologyJunctionInvalidFields";

                /// <summary>Nút giao trùng mã; tham số {0} mã dòng nút.</summary>
                public const string TopologyJunctionDuplicate = Prefix.Message + "topologyJunctionDuplicate";

                /// <summary>Không đọc được dòng nút giao; tham số {0} mã dòng nút, {1} loại lỗi.</summary>
                public const string TopologyJunctionUnreadable = Prefix.Message + "topologyJunctionUnreadable";

                /// <summary>Danh mục thiết bị đang rỗng — stream kết thúc.</summary>
                public const string SseNoEquipment = Prefix.Message + "sseNoEquipment";

                /// <summary>Số thiết bị hiện có; có tham số {0}.</summary>
                public const string SseEquipmentCount = Prefix.Message + "sseEquipmentCount";
            }
        }
    }
}
