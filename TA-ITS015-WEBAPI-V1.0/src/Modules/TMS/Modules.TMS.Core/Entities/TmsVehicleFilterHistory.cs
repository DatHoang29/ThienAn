using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 25/11/2024
    /// </summary>
    [SugarTable("TmsVehicleFilterHistory")]
    public class TmsVehicleFilterHistory : EntityTenant
    {
        //TmsVehicleFilterHistory {
        // ID varchar(64) pk
        // LicensePlate varchar(32) null
        // Type string(16) null
        // DetectTime datetime null
        // Direction string(16) null
        // LocationId string(16) null
        // LaneId varchar(64) null > TmsLane.ID
        //}

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? LicensePlate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Type { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? DetectTime { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? EndDate { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Direction { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? LocationId { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Vùng phát hiện (<c>TmsZone.ID</c>) — lấy từ <c>TmsTrafficAnalysis.ZoneId</c> của
        ///              chính lượt xe. KHÔNG dùng <see cref="LocationId"/> cho việc này: cột đó là MÃ TUYẾN
        ///              (<c>tms_zone_location</c>) và chỉ dài 16 ký tự nên không chứa được GUID của vùng.
        ///              Dòng ghi trước 13/08/2026 để trống.
        /// Created date: 13/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ZoneId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? LaneId { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Camera bắt được xe (<c>TmsEquipment.ID</c>) — cho biết lý trình (Km + mét) và dựng lộ trình
        ///              A → B → C của chuyến. Dòng ghi trước 23/10/2026 để trống (không có camera).
        /// Created date: 23/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? EquipmentId { get; set; }

        /// <summary>
        /// Author: KienLT
        /// Description: Chuyến của xe: các lượt cùng biển đi TIẾN theo tuyến, cách nhau không quá khoảng nghỉ cấu
        ///              hình (<c>vds_vehicle_filter_trip_gap</c>), có cùng mã. Mỗi chuyến chỉ thông báo một lần, giao
        ///              diện gộp một thẻ. Dòng ghi trước 23/10/2026 để trống.
        /// Created date: 23/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? TripId { get; set; }
    }
}
