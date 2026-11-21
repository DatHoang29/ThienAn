using System;

using SqlSugar;
using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.FMS.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Lịch sử trạng thái thông tin bên trong thiết bị (telemetry) của phân hệ FMS.
    /// Created date: 2026-08-27
    /// </summary>
    [SugarTable("FmsEquipmentTelemetry", TableDescription = "Lịch sử trạng thái thông tin bên trong thiết bị (FMS)")]
    public class FmsEquipmentTelemetry : EntityTenant
    {
        /// <summary> ID thiết bị (TmsEquipment.ID) </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID thiết bị")]
        public string? EquipmentId { get; set; }

        /// <summary> Mã thiết bị (lưu kèm để hiển thị/tra cứu nhanh, không phải khóa ngoại) </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Mã thiết bị")]
        public string? EquipmentCode { get; set; }

        /// <summary> Tên thiết bị </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64, ColumnDescription = "Tên thiết bị")]
        public string? EquipmentName { get; set; }

        /// <summary> ID loại thiết bị (TmsEquipmentType.ID) </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength, ColumnDescription = "ID loại thiết bị")]
        public string? EquipmentTypeId { get; set; }

        /// <summary> Hãng thiết bị (dùng để nạp adapter tích hợp khi cần) </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Hãng thiết bị")]
        public string? Vendor { get; set; }

        /// <summary> JSON thông số tài nguyên/vận hành </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Thông số tài nguyên/vận hành (JSON)")]
        public string? ResourceMetric { get; set; }

        /// <summary> JSON thông số môi trường </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Thông số môi trường (JSON)")]
        public string? EnvironmentMetric { get; set; }

        /// <summary> JSON thông số chẩn đoán/sức khỏe </summary>
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString, ColumnDescription = "Thông số chẩn đoán/sức khỏe (JSON)")]
        public string? DiagnosticMetric { get; set; }

        /// <summary> Nguồn lấy: manual (thủ công) | auto (theo chu kỳ) </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16, ColumnDescription = "Nguồn lấy: manual/auto")]
        public string? Source { get; set; }

        /// <summary> Lấy thành công hay không </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Lấy thành công hay không")]
        public bool? Succeeded { get; set; }

        /// <summary> Thời điểm lấy trạng thái </summary>
        [SugarColumn(IsNullable = true, ColumnDescription = "Thời điểm lấy trạng thái")]
        public DateTime? CollectedTime { get; set; }

        /// <summary> Thông báo lỗi (nếu có) khi lấy thất bại </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length512, ColumnDescription = "Thông báo lỗi (nếu có)")]
        public string? Message { get; set; }
    }
}
