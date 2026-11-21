using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Description: Cổng ra (output) của bộ điều khiển và ô nó được gán trên tường — đồng bộ từ thiết bị
    ///              (GET /ISAPI/DisplayDev/VideoWall/{wall}/outputs + /Video/outputs/channels).
    ///              Thiết bị chỉ biết "cổng nào gán ở vị trí nào", không biết màn treo ở đâu ⇒ cửa sổ hiển thị
    ///              theo cổng; màn LCD đang cắm (VwScreen.ControllerOutputId) chỉ là thông tin bảo trì.
    ///              Trường thiết bị: không sửa trên web. Trường web: DisplayName, Remark, Status.
    /// Created date: 06/11/2026
    /// </summary>
    [SugarTable("VwControllerOutput")]
    public class VwControllerOutput : EntityTenant
    {
        /// <summary>Bộ điều khiển sở hữu cổng (master hoặc slave).</summary>
        [SugarColumn(IsNullable = false, Length = EntityConst.KeyFieldLength)]
        public string ControllerId { get; set; } = null!;

        /// <summary>outputID của thiết bị (VD 16842753). Khoá đồng bộ cùng ControllerId.</summary>
        [SugarColumn(IsNullable = true)]
        public int? DeviceOutputId { get; set; }

        /// <summary>Tên cổng trên thiết bị (VD "Output 7-1").</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Name { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? BoardNo { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? PortNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length32)]
        public string? PortType { get; set; }

        /// <summary>Độ phân giải cổng (VD "1920x1080@60Hz").</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Resolution { get; set; }

        /// <summary>Tường trên thiết bị chứa cổng.</summary>
        [SugarColumn(IsNullable = true)]
        public int? WallNo { get; set; }

        /// <summary>Ô của cổng trên tường của CHÍNH bộ điều khiển này (uniformCoordinate, bước baseOutputSize).</summary>
        [SugarColumn(IsNullable = true)]
        public int? RectX { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? RectY { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? RectW { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? RectH { get; set; }

        /// <summary>Cột / hàng của cổng trên tường của bộ điều khiển (RectX / baseOutputSize), 0-based.</summary>
        [SugarColumn(IsNullable = true)]
        public int? LocalCol { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? LocalRow { get; set; }

        /// <summary>
        /// Cổng của bộ phân phối: cấp tín hiệu cho bộ hiển thị nào (bộ con của chính bộ phân phối). Khai báo tay.
        /// Created date: 07/11/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? FeedsControllerId { get; set; }

        /// <summary>
        /// Vị trí trong bố cục đang áp dụng (khối + ô trong khối), người dùng gán tay vào ô do bộ của cổng phụ trách
        /// (VwWallBlock.ControllerConfig). Cùng null ⇒ cổng chưa nằm trong lưới.
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? WallBlockId { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? BlockNo { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? GridCol { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? GridRow { get; set; }

        /// <summary>Tên hiển thị do web đặt (bỏ trống ⇒ dùng Name).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? DisplayName { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }

        /// <summary>Dùng / Không dùng (web).</summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status { get; set; }

        /// <summary>Trạng thái trên thiết bị ở lần đồng bộ gần nhất (VwOutputDeviceState).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DeviceState { get; set; }

        [SugarColumn(IsNullable = true)]
        public DateTime? LastSyncedAt { get; set; }
    }
}
