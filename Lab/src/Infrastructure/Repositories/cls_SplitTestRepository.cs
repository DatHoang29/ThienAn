/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Infrastructure.Persistence;

namespace Infrastructure.Repositories
{
    /// <summary>
    /// Description: Lớp cài đặt cụ thể Repository xử lý chia bảng SplitTable (Kế thừa IScoped để tự động đăng ký Scoped DI)
    /// Author: Antigravity
    /// Create Date: 20/07/2026
    /// </summary>
    public class cls_SplitTestRepository : Icls_SplitTestRepository, IScoped
    {
        /// <summary>
        /// Description: Instance SqlSugar Database Context
        /// </summary>
        private readonly cls_SqlSugarDb vSqlDb;

        /// <summary>
        /// Description: Hàm khởi tạo SplitTestRepository tiếp nhận DB Context qua DI
        /// </summary>
        /// <param name="objSqlDb">Instance dịch vụ database SqlSugar</param>
        public cls_SplitTestRepository(cls_SqlSugarDb objSqlDb)
        {
            vSqlDb = objSqlDb;
        }

        /// <summary>
        /// Description: Thêm bản ghi mới vào bảng phân chia và tự động sinh Snowflake ID
        /// </summary>
        /// <param name="objRecord">Thực thể bản ghi</param>
        /// <returns>Snowflake ID dạng long</returns>
        public long fn_InsertSplitData(cls_SplitTestTable objRecord)
        {
            return vSqlDb.vClient.Insertable(objRecord).SplitTable().ExecuteReturnSnowflakeId();
        }

        /// <summary>
        /// Description: Truy vấn toàn bộ dữ liệu từ các bảng phân chia
        /// </summary>
        /// <returns>Danh sách thực thể cls_SplitTestTable</returns>
        public List<cls_SplitTestTable> fn_GetAllSplitData()
        {
            return vSqlDb.vClient.Queryable<cls_SplitTestTable>()
                                 .SplitTable()
                                 .OrderBy(it => it.vCreateTime, OrderByType.Desc)
                                 .ToList();
        }

        /// <summary>
        /// Description: Truy vấn dữ liệu phân chia theo khoảng thời gian chỉ định
        /// </summary>
        /// <param name="dtBegin">Thời gian bắt đầu</param>
        /// <param name="dtEnd">Thời gian kết thúc</param>
        /// <returns>Danh sách thực thể cls_SplitTestTable</returns>
        public List<cls_SplitTestTable> fn_GetByTimeRange(DateTime dtBegin, DateTime dtEnd)
        {
            return vSqlDb.vClient.Queryable<cls_SplitTestTable>()
                                 .SplitTable(dtBegin, dtEnd)
                                 .Where(it => it.vCreateTime >= dtBegin && it.vCreateTime <= dtEnd)
                                 .ToList();
        }

        /// <summary>
        /// Description: Truy vấn tên các bảng phân chia đã sinh ra trong DB
        /// </summary>
        /// <returns>Danh sách tên bảng</returns>
        public List<string> fn_GetCreatedTables()
        {
            var lstAllTables = vSqlDb.vClient.DbMaintenance.GetTableInfoList();
            return lstAllTables.Where(it => it.Name.StartsWith("tbl_SplitTestTable_"))
                               .Select(it => it.Name)
                               .ToList();
        }
    }
}
