using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Event Trigger Log
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwEventTriggerLog")]
    public class VwEventTriggerLog : EntityTenant
    {
        /// <summary>
        /// Description: Mã định danh luật sự kiện đã kích hoạt (RuleId).
        /// Created date: 10/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? RuleId { get; set; }

        /// <summary>
        /// Description: Mã loại sự kiện EventTypeId kích hoạt log.
        /// Created date: 10/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Description: Mã định danh kịch bản (Scene) liên quan tới dòng log này, nếu có — dùng chung
        /// cho mọi loại Action (EventRule trigger, DeviceSetup, ActivateScene...), không chỉ riêng
        /// event rule. Giữ tên cột TargetSceneId (không đổi) để tránh phát sinh cột mới khi chạy
        /// EnableIncreTable.
        /// Created date: 10/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TargetSceneId { get; set; }

        /// <summary>
        /// Description: Thời điểm chính xác sự kiện kích hoạt hoặc thao tác phát sinh (TriggeredAt).
        /// Created date: 10/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? TriggeredAt { get; set; }

        /// <summary>
        /// Description: Kết quả thực thi Success (Thành công / Thất bại).
        /// Created date: 10/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.SuccessEnums? Success { get; set; }

        // ── Thuộc tính mở rộng: Audit thao tác & bước gọi ISAPI thiết bị ───────

        /// <summary>
        /// Description: Hành động Action (Ping, Probe, SyncSources, SetupScene, Isapi, ActiveScene...).
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? Action { get; set; }

        /// <summary>
        /// Description: Mã định danh đơn vị OrgId quản lý bộ điều khiển tại thời điểm ghi log.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OrgId { get; set; }

        /// <summary>
        /// Description: Mã định danh ControllerId của bộ điều khiển liên quan.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ControllerId { get; set; }

        /// <summary>
        /// Description: Tên bộ điều khiển ControllerName tại thời điểm ghi log.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? ControllerName { get; set; }

        /// <summary>
        /// Description: Mã Code của kịch bản tại thời điểm ghi log.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SceneCode { get; set; }

        /// <summary>
        /// Description: Thứ tự bước StepOrder trong lượt gọi (bắt đầu từ 1).
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? StepOrder { get; set; }

        /// <summary>
        /// Description: Tên bước StepName chi tiết (ví dụ: "Kích hoạt kịch bản").
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? StepName { get; set; }

        /// <summary>
        /// Description: Phương thức HTTP Method của lệnh ISAPI (GET, POST, PUT, DELETE).
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Method { get; set; }

        /// <summary>
        /// Description: Đường dẫn Endpoint ISAPI đã gọi trên thiết bị phần cứng.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Endpoint { get; set; }

        /// <summary>
        /// Description: Cờ đánh dấu bước bị bỏ qua Skipped do cấu hình (DryRun, v.v.).
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public bool? Skipped { get; set; }

        /// <summary>
        /// Description: Mã trạng thái HttpStatus trả về từ thiết bị phần cứng ISAPI.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public int? HttpStatus { get; set; }

        /// <summary>
        /// Description: Thông điệp phản hồi Message hoặc thông báo lỗi chi tiết.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Message { get; set; }

        /// <summary>
        /// Description: Dữ liệu Payload gửi đi (RequestPayload) dạng XML hoặc JSON.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Payload gửi đi dạng XML/JSON")]
        public string? RequestPayload { get; set; }

        /// <summary>
        /// Description: Dữ liệu Payload phản hồi (ResponsePayload) dạng XML hoặc JSON.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Payload phản hồi dạng XML/JSON")]
        public string? ResponsePayload { get; set; }

        /// <summary>
        /// Description: Thời gian thực thi thao tác tính bằng mili-giây (DurationMs).
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public long? DurationMs { get; set; }

        /// <summary>
        /// Description: Tên người dùng hoặc tiến trình thực hiện thao tác OperatorName.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorName { get; set; }

        /// <summary>
        /// Description: Địa chỉ IP của máy thực hiện thao tác OperatorIp.
        /// Created date: 26/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OperatorIp { get; set; }
    }
}