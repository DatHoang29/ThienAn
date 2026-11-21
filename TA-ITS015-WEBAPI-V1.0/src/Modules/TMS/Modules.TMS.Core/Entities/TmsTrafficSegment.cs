using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.TMS.Core.Entities
{
    /// <summary>
    /// Description: Đoạn giữa 2 camera (A→B) kèm khoảng cách khai tay — nguồn để tính tốc độ đoạn.
    /// Created date: 27/07/2026
    /// </summary>
    [SugarTable("TmsTrafficSegment")]
    public class TmsTrafficSegment : EntityTenant
    {
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? Name { get; set; }

        /// <summary>Camera điểm A (TmsEquipment.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? FromEquipmentId { get; set; }

        /// <summary>Camera điểm B (TmsEquipment.ID).</summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? ToEquipmentId { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Direction { get; set; }

        /// <summary>Khoảng cách khai tay (mét).</summary>
        [SugarColumn(IsNullable = true)]
        public int? DistanceMeter { get; set; }

        /// <summary>Biên dưới hợp lệ của thời gian di chuyển (giây).</summary>
        [SugarColumn(IsNullable = true)]
        public int? MinTravelSeconds { get; set; }

        /// <summary>Biên trên hợp lệ của thời gian di chuyển (giây).</summary>
        [SugarColumn(IsNullable = true)]
        public int? MaxTravelSeconds { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.Length16)]
        public string? Status { get; set; } = StatusEnabled;

        /// <summary>
        /// Giá trị cột <see cref="Status"/> nghĩa là đoạn ĐANG DÙNG.
        ///
        /// <para>Cột là chuỗi chứ không phải <c>BaseEnums.StatusEnum</c> như phần lớn entity khác, nên bộ mã
        /// phải khai tường minh. Khai ở đây vì đây là bộ mã của CHÍNH cột này: worker lọc đoạn đang dùng bằng
        /// <c>Status == "1"</c> (<c>Services.Vds/Catalog/VdsCatalogRepository</c>), còn module VDS dùng khi
        /// ghi và khi kiểm đầu vào — ba nơi trước 23/09/2026 mỗi nơi tự gõ lại chuỗi.</para>
        /// </summary>
        public const string StatusEnabled = "1";

        /// <summary>Giá trị cột <see cref="Status"/> nghĩa là đoạn BỊ TẮT.</summary>
        public const string StatusDisabled = "0";

        [SugarColumn(IsNullable = true)]
        public int? OrderNo { get; set; }

        [SugarColumn(IsNullable = true, Length = EntityConst.RemarkFieldLength)]
        public string? Remark { get; set; }
    }
}
