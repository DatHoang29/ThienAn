using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.WP.Infrastructure
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Repository cơ sở của module WP (WebPortal), kế thừa SqlSugarRepository
    /// Created date: 03/07/2026
    /// </summary>
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base()
        {

        }
        
    }
    
}
