/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Microsoft.Extensions.Configuration;
using SqlSugar;
using Lab.Domain.Entities;

namespace Lab.Infrastructure.Persistence
{
    /// <summary>
    /// Description: Dịch vụ cấu hình và khởi tạo SqlSugar Client, xử lý tự động tạo DB và bảng (Code First)
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    public class cls_SqlSugarDb
    {
        /// <summary>
        /// Description: Đối tượng SqlSugarScope quản lý kết nối và phiên làm việc với DB
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public readonly SqlSugarScope vClient;

        /// <summary>
        /// Description: Hàm khởi tạo, đọc cấu hình kết nối và thiết lập SqlSugarScope
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        /// <param name="objConfiguration">Đối tượng Configuration chứa cấu hình từ appsettings.json</param>
        public cls_SqlSugarDb(IConfiguration objConfiguration)
        {
            var strConnectionString = objConfiguration.GetConnectionString("DefaultConnection");

            vClient = new SqlSugarScope(new ConnectionConfig()
            {
                ConnectionString = strConnectionString,
                DbType = DbType.SqlServer,
                IsAutoCloseConnection = true
            },
            objDb =>
            {
                // Cấu hình AOP để log SQL câu lệnh phục vụ việc gỡ lỗi
                objDb.Aop.OnLogExecuting = (sql, pars) =>
                {
                    Console.WriteLine($"[SQL Sugar - Query]: {sql}");
                };
            });
        }

        /// <summary>
        /// Description: Phương thức khởi tạo cơ sở dữ liệu và bảng (Code First)
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public void fn_InitDb()
        {
            // Tự động tạo cơ sở dữ liệu nếu chưa tồn tại
            vClient.DbMaintenance.CreateDatabase();

            // Tự động tạo bảng tbl_User từ thực thể cls_User
            vClient.CodeFirst.InitTables(typeof(cls_User));
        }
    }
}
