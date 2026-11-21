using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.TrafficStatistic.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: DTO phân trang số liệu thống kê chu kỳ (QuestDB). FromDate/ToDate BẮT BUỘC + trần cửa
    /// sổ (validator) vì QuestDB phân vùng theo ngày, thiếu filter là quét cả bảng.
    /// Created date: 03/09/2026
    /// </summary>
    public class PageTrafficStatisticInput : BasePageInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }
        public string? Lane { get; set; }
    }

    /// <summary>Danh sách (không phân trang) — cùng bộ lọc với phân trang.</summary>
    public class TrafficStatisticListInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }
        public string? Lane { get; set; }
    }

    /// <summary>
    /// Thống kê theo khoảng ngày — thay <c>tmsTrafficStatistic/ListByDate</c> cho 2 report FastReport.
    /// </summary>
    public class TrafficStatisticByDateInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? ZoneId { get; set; }
        public string? EquipmentId { get; set; }
    }

    /// <summary>
    /// Bản ghi thống kê MỚI NHẤT của một thiết bị trong cửa sổ — thay <c>GetByEquipment</c> của TMS.
    ///
    /// TMS làm <c>ORDER BY ToTime DESC TOP 1</c> KHÔNG có filter thời gian; trên QuestDB không được (quét
    /// cả bảng). Buộc client truyền cửa sổ (validator bắt buộc + trần).
    /// </summary>
    public class TrafficStatisticByEquipmentInput
    {
        public string? EquipmentId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Bộ lọc snapshot statistic — bản ghi MỚI NHẤT theo camera/làn trong cửa sổ.
    ///
    /// <para>Kế thừa <c>VdsReadInput</c> nên nhận chung cửa sổ thời gian và trần phân trang với các DTO đọc
    /// khác của module.</para>
    /// Created date: 08/09/2026
    /// </summary>
    public class StatisticSnapshotInput : VdsReadInput
    {
        /// <summary>
        /// Giới hạn theo danh sách camera; rỗng = mọi camera.
        ///
        /// <para><b>Setter chuẩn hoá <c>null</c> thành mảng rỗng.</b> Giá trị khởi tạo <c>= []</c> không bảo
        /// vệ được: body gửi <c>"equipmentIds": null</c> ghi null lên property và
        /// <c>command.EquipmentIds.Length</c> trong query handler ném NullReferenceException TRƯỚC khi vào DB
        /// (xác nhận 22/09/2026).</para>
        /// </summary>
        public string[] EquipmentIds
        {
            get => _equipmentIds;
            set => _equipmentIds = value ?? [];
        }

        private string[] _equipmentIds = [];
    }
}
