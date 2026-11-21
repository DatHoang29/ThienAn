using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using Shared.DTO.Enums;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Event Rule
    /// Created date: 14/08/2026
    /// </summary>
    [SugarTable("VwEventRule")]
    public class VwEventRule : EntityTenant
    {
        /// <summary>
        /// Description: Mã loại sự kiện EventTypeId liên kết.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? EventTypeId { get; set; }

        /// <summary>
        /// Description: Mã kịch bản đích TargetSceneId sẽ được kích hoạt khi sự kiện xảy ra.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? TargetSceneId { get; set; }

        /// <summary>
        /// Description: Nguồn phát sinh sự kiện (EventSource).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length128)]
        public string? EventSource { get; set; }

        /// <summary>
        /// Description: Mức độ ưu tiên xử lý sự kiện (Priority).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? Priority { get; set; }

        /// <summary>
        /// Description: Trạng thái kích hoạt của quy tắc sự kiện (Status).
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true)]
        public BaseEnums.StatusEnum? Status{ get; set; }

        /// <summary>
        /// Description: Ghi chú hoặc thông tin bổ sung về quy tắc sự kiện.
        /// Created date: 14/08/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string? Remark { get; set; }
    }
}