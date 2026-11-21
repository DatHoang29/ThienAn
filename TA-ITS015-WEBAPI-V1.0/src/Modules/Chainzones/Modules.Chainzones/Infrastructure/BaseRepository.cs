using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Infrastructure.Persistence.SqlSugar;
using SqlSugar;

namespace Modules.Chainzones.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
        /// Author: KhoaND
        /// Description:
        ///     -thêm Insert và update cho vmsCurrent
        /// Created date: 23/10/2024
        ITenant iTenant = SqlSugarSetup.ITenant;
        
    }
    
}
