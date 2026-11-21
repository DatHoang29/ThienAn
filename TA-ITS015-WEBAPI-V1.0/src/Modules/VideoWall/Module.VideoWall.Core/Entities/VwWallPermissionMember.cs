using Shared.Core.Domain;
using Shared.DTO.Constants.Application;
using SqlSugar;

namespace Module.VideoWall.Core.Entities
{
    /// <summary>
    /// Description: Đối tượng được gán quyền vùng màn hình (VwWallPermission).
    ///              Mỗi dòng thuộc đúng 1 đối tượng: có UserId (người dùng) HOẶC chỉ có OrgId (cả đơn vị).
    ///              Một quyền có nhiều dòng; một người dùng / đơn vị có thể thuộc nhiều quyền.
    /// Created date: 22/10/2026
    /// </summary>
    [SugarTable("VwWallPermissionMember")]
    public class VwWallPermissionMember : EntityTenant
    {
        /// <summary>
        /// Description: Quyền được gán (VwWallPermission.ID).
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? PermissionId { get; set; }

        /// <summary>
        /// Description: ID người dùng được gán (rỗng nếu dòng gán cho cả đơn vị).
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.KeyFieldLength)]
        public string? UserId { get; set; }

        /// <summary>
        /// Description: ID đơn vị được gán (rỗng nếu dòng gán cho người dùng).
        /// Created date: 22/10/2026
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string? OrgId { get; set; }
    }
}
