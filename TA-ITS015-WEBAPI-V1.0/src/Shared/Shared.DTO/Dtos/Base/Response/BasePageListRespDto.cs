using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.Dtos.Base.Response
{

    /// <summary>
    /// Dto cho response dữ liệu có phân trang
    /// Dùng cho response
    /// Author: SonTH
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class BasePageListRespDto<TEntity>
    {

        /// <summary>
        /// Page number
        /// </summary>
        public int Current { get; set; }

        /// <summary>
        /// Page size
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Total number of items
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// Collection of items in the current page
        /// </summary>
        public IEnumerable<TEntity> Records { get; set; }

        /// <summary>
        /// Indicates if there is a previous page
        /// </summary>
        public bool HasPrevPage { get; set; }

        /// <summary>
        /// Indicates if there is a next page
        /// </summary>
        public bool HasNextPage { get; set; }

    }
}
