using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.Samplev2.Infrastructure
{
    public class SampleBaseRepository<T> : SqlSugarRepository<T> where T : class, new()
    {
        public SampleBaseRepository() : base()
        {

        }
        
    }
    
}
