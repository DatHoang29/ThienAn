using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Samplev2.Infrastructure.Services.Category
{
    public class SampCategoryService:IScoped
    {
        public SampCategoryService() { }


        public string Calculate()
        {
            return "Hello world";
        }
    }
}
