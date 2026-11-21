using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Dtos.Base.Request
{
    /// <summary>
    /// Dto dùng cho Request gọi phân trang
    /// Dùng cho request
    /// Author: SonTH
    /// </summary>
    public class BasePageReqDto
    {
        /// <summary>
        /// Current page number
        /// </summary>
        public virtual int Page { get; set; } = 1;

        /// <summary>
        /// Page size
        /// </summary>
        public virtual int PageSize { get; set; } = 20;

        /// <summary>
        /// Sorting field
        /// </summary>
        public virtual string Field { get; set; }

        /// <summary>
        /// Sorting direction
        /// </summary>
        public virtual string Order { get; set; }

        /// <summary>
        /// Descending sorting
        /// </summary>
        public virtual string DescStr { get; set; } = "descending";

    }
}
