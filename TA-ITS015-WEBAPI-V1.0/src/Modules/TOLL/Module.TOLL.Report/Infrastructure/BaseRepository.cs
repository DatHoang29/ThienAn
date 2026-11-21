using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.TOLL.Report.Infrastructure
{
    public class BaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public BaseRepository() : base() { }
    }
}
