using Shared.Core.Domain;
using Shared.DTO.Constants.Application;

namespace Modules.Fms.Core.Entities
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Bảng lưu cấu hình Topology - lưu đường dẫn thư mục chứa file config (JSON)
    ///     mô tả quan hệ cha - con (parent/child) của thiết bị bên Nagios.
    /// Created date: 08/07/2026
    /// </summary>
    [SugarTable("FmsTopology")]
    public class FmsTopology : EntityTenant
    {
        /// <summary>
        /// Tên topology
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length64)]
        public string Name { get; set; }

        /// <summary>
        /// Đường dẫn thư mục chứa file config Nagios (parent-child topology của thiết bị)
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Value { get; set; }

        /// <summary>
        /// Ghi chú
        /// </summary>
        [SugarColumn(IsNullable = true, Length = EntityConst.Length256)]
        public string Remark { get; set; }
    }
}
