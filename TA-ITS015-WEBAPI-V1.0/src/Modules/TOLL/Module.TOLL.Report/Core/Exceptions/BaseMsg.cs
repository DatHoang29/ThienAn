using Shared.DTO.Constants.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Tcs.Tcs23.BaoCao.Core.Exceptions
{
    public class BaseMsg : BaseLocaleManager
    {
        public class RptBase
        {
            public class Prefix
            {
                public const string Message = "lz.message.rptBase.";
                public const string Validation = "lz.validation.rptBase.";
                public const string Exception = "lz.exception.rptBase.";
                public const string Entity = "lz.entity.rptBase.";
            }


            public class Exception
            {
                internal const string FromDateGreaterThanToDate = Prefix.Exception + "fromDateGreaterThanToDate";
            }
        }
    }
}
