using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Dtos.Base
{
    /// <summary>
    /// Dto các trường dữ liệu dùng chung.
    /// Áp dụng cho cả Request & Response
    /// Author: SonTH
    /// </summary>
    public class BaseEntityDto : BaseIdEntityDto
    {
        public virtual string Code { get; set; }
        /// <summary>
        /// Creation time
        /// </summary>
        public virtual DateTime CreateTime { get; set; }

        /// <summary>
        /// Creator ID
        /// </summary>
        public virtual string CreateUId { get; set; }

        /// <summary>
        /// Update time
        /// </summary>
        public virtual DateTime? UpdateTime { get; set; }

        /// <summary>
        /// Modifier ID
        /// </summary>
        public virtual string UpdateUId { get; set; }

        public virtual string RowStatus { get; set; }

        /// <summary>
        /// Soft deletion
        /// </summary>
        public virtual DateTime? IsDelete { get; set; }
    }
}
