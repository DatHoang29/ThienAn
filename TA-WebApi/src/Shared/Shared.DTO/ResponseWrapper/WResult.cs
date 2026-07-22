using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTO.ResponseWrapper
{
   
    public class WResult<T> : IWResult<T>
    {
        public string Message { get; set; }
        public int Code { get; set; }
        public bool Succeeded { get; set; }
        public DateTime Time { get; set; }
        public T Result { get; set; }

    }
}
