using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// For: SqlSugarRepository
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.CfgSystem.Infrastructure
{
    /// <summary>
    /// Author: Ngọc Thắng
    /// Description:
    ///     - Lớp thực hiện interface và định nghĩa các phương thức liên quan ORM để phục vụ các lớp khác
    ///     - Lớp trung gian giữa tầng data và business, chứa đựng các phương thức thao tác với tầng data
    /// Created date: 09/08/2024
    /// Note: SimpleClient<T>, ISqlSugarRepository<T>, ISugarRepository, ISimpleClient<T>
    /// </summary>
    /// <typeparam name="T">Đối tượng generic</typeparam>
    public class BaseRepository<T>: SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository(): base() { }
    }
}
