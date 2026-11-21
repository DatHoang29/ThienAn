using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Description: (6) Sự kiện VDS — lấy từ TmsTrafficData khi đạt điều kiện của handler theo loại.
    ///              Phục vụ tra cứu và xuất cho bên thứ 3 (CSGT, lãnh đạo).
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("TmsEventData")]
    public class TmsEventData : EntityTenant
    {
        /// <summary>id bản tin phía VDS — khoá idempotency.</summary>
        [SugarColumn(IsNullable = false)]
        public long VendorEventId { get; set; }

        /// <summary>ID bản ghi raw (TmsTrafficData.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SourceId { get; set; }

        /// <summary>type của VDS.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? EventType { get; set; }

        /// <summary>TmsEventType.ID tương ứng.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventTypeId { get; set; }

        [SugarColumn(IsNullable = true)]
        public byte? Severity { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? OccurredUtc { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? EquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? IpAddress { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ZoneId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? KmNumber { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? MetNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Location { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Lane { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PlateColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PlateType { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? PlateConfidence { get; set; }

        /// <summary>Cỡ xe S/M/L của hệ ITS.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? VehicleType { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? VehicleColor { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? VehicleBrand { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? Speed { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? SpeedThreshold { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? AlarmType { get; set; }

        [SugarColumn(IsNullable = true)]
        public double? Confidence { get; set; }

        // Không có MediaCount: ảnh/clip chỉ lưu ra file theo quy ước đường dẫn
        // (VdsImages/event/{yyyyMM}/{ddHH}/{time}_{V|P|F}.{ext} — sự cố nằm ở nhánh "event",
        // lượt xe bình thường ở nhánh "data"), không ghi bản ghi nào vào DB nên không có nguồn nào để đếm.

        /// <summary>TmsIncident.ID nếu đã nâng thành sự cố.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? IncidentId { get; set; }

        /// <summary>0 mới / 1 đã xem / 2 đã xác nhận / 9 bỏ qua.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? State { get; set; } = "0";

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? ExportState { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? ExportedAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ExportedBy { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? ExportRef { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length512)]
        public string? Remark { get; set; }

        /// <summary>Khoá điều kiện đã khớp, do handler của loại đặt.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ConditionCode { get; set; }

        /// <summary>Hash khoá gộp (EquipmentId + EventType + định danh phụ) — dùng chống trùng.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ConditionHash { get; set; }

        /// <summary>Đã đạt điều kiện lên sự cố chưa.</summary>
        [SugarColumn(IsNullable = true)]
        public bool? IncidentEligible { get; set; } = false;

        /// <summary>Vì sao chưa lên sự cố (thiếu điều kiện / trùng trong cửa sổ).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? IncidentBlockReason { get; set; }
    }
}
