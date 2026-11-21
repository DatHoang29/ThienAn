using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SixLabors.ImageSharp;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Màn hình LCD (tài sản) — người dùng khai báo, đặt lên ô của bố cục rồi gán cổng ra cho màn.
    ///              Vị trí trên tường thuộc về MÀN (WallBlockId/GridCol/GridRow); cổng gán vào màn lấy vị trí của màn
    ///              (VwControllerOutput.WallBlockId/... chỉ là bản sao, ghi lại mỗi lần lưu sơ đồ).
    ///              Đồng bộ thiết bị chỉ ghi thông số (nhiệt độ, giờ chạy, phiên bản, serial thiết bị) vào màn đang gán cổng.
    /// Created date: 14/08/2026 — cập nhật 07/11/2026 (vị trí thuộc màn, cổng gán vào màn)
    /// </summary>
    [SugarTable("VwScreen")]
    public class VwScreen : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        /// <summary>
        /// Description: Cổng ra gán cho màn (VwControllerOutput.ID). Null ⇒ màn chưa có cổng.
        ///              Người dùng gán trên sơ đồ (VwScreen/SaveWallMap); đồng bộ thiết bị không đổi trường này.
        /// Created date: 06/11/2026 — cập nhật 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? ControllerOutputId { get; set; }

        /// <summary>
        /// Description: Vị trí màn trên bố cục đang áp dụng: khối (VwWallBlock.ID) + cột / hàng trong khối. Null ⇒ màn trong kho.
        /// Created date: 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallBlockId { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? BlockNo { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? GridCol { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? GridRow { get; set; }

        /// <summary>Serial người dùng khai báo.</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Description: Serial thiết bị báo cho cổng đang gán (GetWallScreenInfo.serialNumber) — chỉ đọc, khác SerialNumber ⇒ giao diện cảnh báo lệch.
        /// Created date: 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DeviceSerialNumber { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? SoftwareVersion { get; set; }

        /// <summary>Lần đầu thấy màn cắm vào một cổng.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? InstalledAt { get; set; }

        /// <summary>Lần thấy màn bị thay ra khỏi cổng.</summary>
        [SugarColumn(IsNullable = true)]
        public DateTime? RemovedAt { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Resolution { get; set; }

        /// <summary>
        /// Description: Tỉ lệ panel (rộng:cao), VD "16:9", "3:4" — giá trị hợp lệ: VwWallProfile.CellRatios.
        ///              Tạo từ ô trống thì lấy theo tỉ lệ ô của bố cục; khác tỉ lệ ô của bố cục thì giao diện cảnh báo.
        /// Created date: 27/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? CellRatio { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PanelType { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? PanelModel { get; set; }
        [SugarColumn(IsNullable = true)]
        public int? PanelSize { get; set; }
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ScreenState { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Temperature { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? RunTimeHours { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Brightness { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? LampLifeHours { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
        [SugarColumn(IsNullable = true, ColumnDataType = StaticConfig.CodeFirst_BigString)]
        public string? ScreenConfig { get; set; }
        [SugarColumn(IsNullable = true)]
        public DateTime? LastCheckedAt { get; set; }
    }
}