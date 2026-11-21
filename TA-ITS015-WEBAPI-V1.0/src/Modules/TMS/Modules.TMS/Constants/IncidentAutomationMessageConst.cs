using Modules.TMS.Core.Constants;

namespace Modules.TMS.Constants;

/// <summary>
/// Tập trung các giá trị text/format dùng trong luồng automation.
///
/// ErrorCode là mã máy đọc được và được lưu vào ErrorCode/Remark khi cần.
/// ErrorMessage, Remark và Log chỉ là nội dung hiển thị/diagnostic; không dùng
/// thay cho mã lỗi. Các giá trị dùng chung giữa module TMS/VMS vẫn đặt ở
/// Modules.TMS.Core.Constants hoặc Modules.TMS.Core.Dto.
/// </summary>
internal static class IncidentAutomationMessageConst
{
    public const string AutomaticExecutionSource = "Automatic";
    public const string AutomaticWorkType = "control";
    public const string AutomaticPlanState = "2";
    public const string ConvertToManualReason = "ConvertToManual";
    public const string IdempotencyKeyFormat = "incident:{0}:run:{1}";
    public const string ReasonSeparator = ":";
    public const string ListSeparator = ",";
    public const string TokenPattern = @"\{(?<key>[A-Za-z0-9_.-]+)\}";
    public const string ExecuteDebugMarker = "ExecuteAutoAsync";
    public const int DefaultBrightness = 100;
    public const int DefaultPriority = Modules.TMS.Core.Constants.IncidentControlConst.DefaultPriority;
    public const string DefaultVmsSize = "384x64";
    public const string DefaultLcsSize = "128x128";

    public static class VideoWallSceneCode
    {
        public const string Incident = "sc-incident";
        public const string Normal = "sc-normal";
    }

    // Mã cấu hình phạm vi gợi ý thiết bị đã chuyển sang EquipmentSuggestionConst.OpConfig
    // (Modules.TMS.Core.Constants) để backend và FE dùng chung một nguồn + một giá trị mặc định.

    public static class ContentField
    {
        public const string Row1 = "Row1";
        public const string Row1Lower = "row1";
        public const string Row2 = "Row2";
        public const string Row2Lower = "row2";
        public const string ImageUrl = "ImageUrl";
        public const string Url = "Url";
        public const string UrlLower = "url";
        public const string Size = "Size";
        public const string SizeLower = "size";
        public const string Color = "Color";
        public const string ColorLower = "color";
        public const string BackColor = "BackColor";
        public const string BackColorLower = "backColor";
        public const string Font = "Font";
        public const string FontLower = "font";
        public const string FontSize = "FontSize";
        public const string FontSizeLower = "fontSize";
    }

    public static class ErrorCode
    {
        public const string ConfigSnapshotInvalid = "ConfigSnapshotInvalid";
        public const string IncidentNotUsable = "IncidentNotUsable";
        public const string AutoVerifyFailed = "AutoVerifyFailed";
        public const string InProgressFailed = "InProgressFailed";
        public const string AutoWorkExecuteFailed = "AutoWorkExecuteFailed";
        public const string ResetIncidentToDefaultFailed = "ResetIncidentToDefaultFailed";
        public const string FinishIncidentFailed = "FinishIncidentFailed";
        public const string AutoDeviceControlFailed = "AutoDeviceControlFailed";
        public const string UnsupportedControlType = "UnsupportedControlType";
        public const string NoRecommendedEquipment = "NoRecommendedEquipment";
        public const string NoRecommendedContent = "NoRecommendedContent";
        public const string NoEventDefault = "NoEventDefault";
        public const string EquipmentTypeNotConfigured = "EquipmentTypeNotConfigured";
        public const string EquipmentEventRuleNotUsable = "EquipmentEventRuleNotUsable";
        public const string IncidentEventTypeRequired = "IncidentEventTypeRequired";
        public const string NoEquipmentSelected = "NoEquipmentSelected";
        public const string IncidentControlOccupied = "IncidentControlOccupied";
        public const string WorkExecuteStateUpdateFailed = "WorkExecuteStateUpdateFailed";
        public const string StuckTimeout = IncidentAutomationConst.StuckTimeoutErrorCode;

        /// <summary>Lỗi ngoài dự kiến sau khi run đã Running (V08) - không để run treo tới StuckTimeout.</summary>
        public const string UnexpectedError = "UnexpectedError";
    }

    public static class ErrorMessage
    {
        public const string ConfigSnapshotInvalid = "Không đọc được cấu hình đã chốt của lần chạy tự động.";
        public const string IncidentNotUsable = "Incident không còn hợp lệ để tự động thực thi.";
        public const string AutoVerifyFailed = "Tự động xác thực incident thất bại.";
        public const string InProgressFailed = "Chuyển incident sang InProgress thất bại (có thể do sinh work-execute lỗi).";
        public const string AutoWorkExecuteFailedFormat = "Thực thi work tự động thất bại: {0}";
        public const string ResetIncidentToDefaultFailedFormat = "Gọi luồng reset manual cho incident thất bại: {0}";
        public const string FinishIncidentFailed = "Chuyển incident sang Finished thất bại.";
        public const string EquipmentTypeNotConfiguredFormat = "Chưa cấu hình loại thiết bị {0}.";
        public const string EquipmentEventRuleNotUsableFormat = "Rule chọn thiết bị {0} không dùng được (đã xóa hoặc đang tắt).";
        public const string EquipmentEventRuleEventTypeMismatchFormat = "Rule chọn thiết bị {0} thuộc loại sự kiện {1}, không khớp loại sự kiện {2} của sự cố.";
        public const string IncidentEventTypeRequired = "Thiếu EventTypeId để lấy nội dung recommend.";
        public const string NoRecommendedContentFormat = "Không có nội dung recommend {0} cho loại sự kiện {1}.";
        public const string NoEquipmentSelected = "Không có thiết bị hợp lệ để điều khiển tự động.";
        public const string WorkExecuteStateUpdateFailedFormat = "Cập nhật trạng thái work execute thất bại: {0} -> {1}.";
        public const string StuckTimeoutFormat = "Run bị treo ở trạng thái {0} quá {1:0} phút nên bị hệ thống cưỡng chế đóng.";
    }

    public static class ReasonFormat
    {
        public const string CodeMessage = "{0}: {1}";
        public const string NoRecommendedEquipment = "{0}:{1}:{2}:range={3}";
        public const string NoEventDefault = "{0}:{1}:{2}";
        public const string IncidentControlOccupied = "{0}:{1}";
        public const string UnsupportedControlType = "{0}: control type '{1}' for work {2}.";
        public const string AutoDeviceControlFailed = "{0}: {1}";
    }

    public static class Remark
    {
        public const string PlanStateMissing = "TmsTrafficPlanWorkDetail.State is missing; skipped by automatic run";
        public const string PlanStateFormat = "TmsTrafficPlanWorkDetail.State={0}; skipped by automatic run";
        public const string WorkCompleted = "Automatic work completed";
        public const string DeviceControlAppliedFormat = "Automatic {0} control applied";

        // KienLT - 14/10/2026: bỏ riêng thiết bị bị sự cố khác giữ — ghi rõ thiết bị nào đã gửi, thiết bị nào bị giữ.
        public const string DeviceControlAppliedToFormat = "Automatic {0} control applied: {1}";
        public const string DeviceControlOccupiedSuffixFormat = "; occupied: {0}";

        // KienLT - 16/10/2026: run lỗi giữa chừng thì trả bước lỗi về Chờ để tổ chức được giao làm tay. Tiền tố máy đọc
        // được (FE dịch theo nó), khác AutoDeviceControlFailed để bước người trực tự Bỏ qua không bị trả lại lần nữa.
        public const string HandOverFormat = "AutoHandOver: {0}";
    }

    public static class Log
    {
        public const string ServicePrefix = "[TmsIncidentAutomationService]";
        public const string WorkerPrefix = "[TmsIncidentAutomationWorker]";
        public const string ScheduledFormat = ServicePrefix + " Đã lập lịch tự động cho sự cố {IncidentId}: run {RunId}, kịch bản {PlanId}, thực thi lúc {ExecuteDueAt}.";
        public const string SkipScheduleFormat = ServicePrefix + " Không lập lịch tự động: {ReasonCode}. Sự cố {IncidentId}, loại sự kiện {EventTypeId}, nguồn {Source}, trigger {TriggerState}, cấu hình {ConfigId}.";
        public const string StuckRecoveredFormat = ServicePrefix + " Cưỡng chế đóng run treo {RunId} của sự cố {IncidentId}: trạng thái {PreviousStatus} không đổi từ {UpdateTime}.";
        public const string PurgedFormat = ServicePrefix + " Đã dọn {Count} lần chạy tự động đã kết thúc trước {Cutoff} (giữ {RetentionDays} ngày).";
        public const string RetentionReadFailed = ServicePrefix + " Đọc cấu hình số ngày giữ lần chạy tự động thất bại, tạm tắt việc dọn.";
        public const string SuggestionRangeReadFailed = ServicePrefix + " Không đọc được cấu hình phạm vi gợi ý thiết bị, dùng mặc định {DefaultRange}m.";
        public const string ContinueWorkFailure = ServicePrefix + " Bỏ qua lỗi work {WorkId} theo failure policy Continue.";
        public const string SkipLcsNoEquipment = ServicePrefix + " Bỏ qua bước LCS {WorkId}: không có LCS trong phạm vi gợi ý ({Reason}).";
        public const string UnexpectedRunFailure = ServicePrefix + " Lỗi ngoài dự kiến khi chạy run {RunId} của sự cố {IncidentId}; đánh Failed và bàn giao.";
        public const string HandedOver = ServicePrefix + " Bàn giao sự cố {IncidentId} cho xử lý thủ công sau khi run {RunId} lỗi: {Count} bước về Chờ, reset thiết bị = {ResetDevices}.";
        public const string HandOverFailed = ServicePrefix + " Trả bước lỗi về Chờ cho sự cố {IncidentId} (run {RunId}) thất bại; worker sẽ thử lại.";
        public const string HandOverResetFailed = ServicePrefix + " Reset thiết bị về mặc định khi bàn giao sự cố {IncidentId} (run {RunId}) thất bại; thiết bị sẽ được reset khi Hoàn thành.";
        public const string WorkerHandOverRepairedFormat = WorkerPrefix + " Đã trả nốt bước lỗi về Chờ cho {Count} run bàn giao dở.";
        public const string ProcessModeUpdateFailed = ServicePrefix + " Cập nhật ProcessMode={ProcessMode} cho sự cố {IncidentId} thất bại.";
        public const string VideoWallSceneActivationFailed = ServicePrefix + " Gọi active scene {SceneCode} cho sự cố {IncidentId} thất bại.";
        public const string BroadcastFailed = ServicePrefix + " Phát realtime cho run {RunId} thất bại.";
        public const string WorkerScanFailed = WorkerPrefix + " Lỗi trong nhịp quét automation run.";
        public const string WorkerStuckScanFailed = WorkerPrefix + " Lỗi trong nhịp quét run bị treo.";
        public const string WorkerPurgeFailed = WorkerPrefix + " Lỗi trong nhịp dọn run đã kết thúc.";
        public const string WorkerRecoveredFormat = WorkerPrefix + " Đã cưỡng chế đóng {Count} run bị treo.";
        public const string WorkerRunFailedFormat = WorkerPrefix + " Lỗi xử lý automation run {RunId} (isFinish={IsFinish}).";
    }
}
