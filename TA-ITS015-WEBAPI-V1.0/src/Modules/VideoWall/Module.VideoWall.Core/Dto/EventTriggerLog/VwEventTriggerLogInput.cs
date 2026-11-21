using Module.VideoWall.Core.Entities;
using Shared.Core.Utilities;
using Shared.DTO.Enums;

namespace Module.VideoWall.Core.Dto.EventTriggerLog
{
    /// <summary>
    /// Author: Đạt
    /// Description: DTO phân trang nhật ký kích hoạt kịch bản và thao tác thiết bị Video Wall.
    ///              Bảng CHỈ GHI THÊM nên không có DTO Update / Delete / BatchDelete.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwPageEventTriggerLogInput : BasePageInput
    {
        /// <summary>Lọc theo luật sự kiện (RuleId != null là dòng nghiệp vụ, null là dòng bước thiết bị).</summary>
        public string? RuleId { get; set; }

        public string? EventTypeId { get; set; }

        /// <summary>Bộ điều khiển liên quan (VwController.ID).</summary>
        public string? ControllerId { get; set; }

        /// <summary>Kịch bản liên quan (VwScene.ID).</summary>
        public string? SceneId { get; set; }

        public string? SceneCode { get; set; }

        /// <summary>Ping | Probe | SyncSources | SetupScene | Isapi | ResetDeviceAuthFailure | ActiveScene.</summary>
        public string? Action { get; set; }

        public BaseEnums.SuccessEnums? Success { get; set; }

        /// <summary>Lọc riêng các bước chạy thử (DryRun) hoặc riêng các bước đã thật sự gửi lệnh.</summary>
        public bool? Skipped { get; set; }

        /// <summary>Mốc đầu khoảng thời gian, so theo TriggeredAt (bao gồm chính mốc này).</summary>
        public DateTime? TriggeredAtFrom { get; set; }

        /// <summary>Mốc cuối khoảng thời gian, so theo TriggeredAt (bao gồm chính mốc này).</summary>
        public DateTime? TriggeredAtTo { get; set; }

        /// <summary>
        /// Lọc theo đơn vị quản lý (tùy chọn).
        /// </summary>
        public string? OrgId { get; set; }
    }

    /// <summary>
    /// Author: Đạt
    /// Description: DTO tìm kiếm nhật ký kích hoạt kịch bản và thao tác thiết bị (không phân trang).
    ///              Bảng ghi rất nhiều dòng nên endpoint này CÓ giới hạn số bản ghi trả về — xem Take.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwEventTriggerLogInput
    {
        public string? ID { get; set; }

        public string? RuleId { get; set; }

        public string? EventTypeId { get; set; }

        public string? ControllerId { get; set; }

        public string? SceneId { get; set; }

        public string? SceneCode { get; set; }

        public string? Action { get; set; }

        public BaseEnums.SuccessEnums? Success { get; set; }

        public bool? Skipped { get; set; }

        public DateTime? TriggeredAtFrom { get; set; }

        public DateTime? TriggeredAtTo { get; set; }

        public string? OrgId { get; set; }

        /// <summary>
        /// Trần số bản ghi trả về. Bảng chỉ-ghi-thêm và mỗi bước ISAPI một dòng nên không giới hạn
        /// là có ngày kéo cả trăm nghìn dòng kèm hai cột payload lớn về client.
        /// </summary>
        public int Take { get; set; } = 200;
    }

    /// <summary>
    /// Author: Đạt
    /// Description: DTO ghi 1 dòng nhật ký thao tác thiết bị hoặc kích hoạt nghiệp vụ.
    ///
    ///              OrgId / ControllerName do SERVER đóng dấu theo bộ điều khiển (chỉ mang tính mô tả,
    ///              không tham gia phân quyền). Xem VwEventTriggerLogService.Write.
    /// Created date: 26/08/2026
    /// </summary>
    public class VwAddEventTriggerLogInput : VwEventTriggerLog { }
}
