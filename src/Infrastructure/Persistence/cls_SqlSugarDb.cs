/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

namespace Infrastructure.Persistence
{
    /// <summary>
    /// Description: Dịch vụ cấu hình và khởi tạo SqlSugar Client (Kế thừa ISingleton để tự động đăng ký Singleton DI)
    /// Author: Antigravity
    /// Create Date: 17/07/2026
    /// </summary>
    public class cls_SqlSugarDb : ISingleton
    {
        /// <summary>
        /// Description: Đối tượng SqlSugarScope quản lý kết nối và phiên làm việc với DB
        /// Author: Antigravity
        /// Create Date: 17/07/2026
        /// </summary>
        public readonly SqlSugarScope vClient;

        /// <summary>
        /// Description: Hàm khởi tạo, cấu hình chuỗi kết nối và đăng ký Mapping ORM ngoài (External Mapping) cho POCO Entities
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
                IsAutoCloseConnection = true,
                ConfigureExternalServices = new ConfigureExternalServices
                {
                    // Cấu hình Mapping ORM tách biệt hoàn toàn khỏi Domain Entities (Fluent / Code-based Mapping)
                    EntityService = (objProperty, objColumn) =>
                    {
                        // 1. Cấu hình bảng và cột cho Thực thể cls_User
                        if (objProperty.DeclaringType == typeof(cls_User))
                        {
                            objColumn.DbTableName = "tbl_User";

                            if (objProperty.Name == nameof(cls_User.intId))
                            {
                                objColumn.IsPrimarykey = true;
                                objColumn.IsIdentity = true;
                                objColumn.DbColumnName = "Id";
                            }
                            else if (objProperty.Name == nameof(cls_User.strUserName))
                            {
                                objColumn.DbColumnName = "UserName";
                                objColumn.Length = 50;
                                objColumn.IsNullable = false;
                            }
                            else if (objProperty.Name == nameof(cls_User.strEmail))
                            {
                                objColumn.DbColumnName = "Email";
                                objColumn.Length = 100;
                                objColumn.IsNullable = true;
                            }
                            else if (objProperty.Name == nameof(cls_User.dtCreateDate))
                            {
                                objColumn.DbColumnName = "CreateDate";
                                objColumn.IsNullable = true;
                            }
                        }

                        // 2. Cấu hình chia bảng (SplitTable) cho Thực thể cls_SplitTestTable
                        if (objProperty.DeclaringType == typeof(cls_SplitTestTable))
                        {
                            objColumn.DbTableName = "tbl_SplitTestTable_{year}{month}{day}";

                            if (objProperty.Name == nameof(cls_SplitTestTable.vId))
                            {
                                objColumn.IsPrimarykey = true;
                                objColumn.DbColumnName = "Id";
                            }
                            else if (objProperty.Name == nameof(cls_SplitTestTable.vName))
                            {
                                objColumn.DbColumnName = "Name";
                                objColumn.Length = 100;
                                objColumn.IsNullable = true;
                            }
                            else if (objProperty.Name == nameof(cls_SplitTestTable.vCreateTime))
                            {
                                objColumn.DbColumnName = "CreateTime";
                            }
                        }
                    }
                }
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
