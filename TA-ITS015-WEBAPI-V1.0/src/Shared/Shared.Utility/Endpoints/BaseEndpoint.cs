using Shared.Utility.Endpoints.CfgSystem;
using Shared.Utility.Endpoints.Fms;
using Shared.Utility.Endpoints.Sample;
using Shared.Utility.Endpoints.System;
using Shared.Utility.Endpoints.Tms;
using Shared.Utility.Endpoints.Vms;

namespace Shared.Utility.Endpoints
{
    /// <summary>
    /// Base endpoint class
    /// </summary>
    public class BaseEndpoint
    {

        /// <summary>
        /// System modules endpoints
        /// </summary>
        public class SystemEp : SystemEndpoint
        {

        }

        /// <summary>
        /// Config System modules endpoints
        /// </summary>
        public class CfgSystemEp : CfgSystemEndpoint
        {

        }

        /// <summary>
        /// Tms modules endpoints
        /// </summary>
        public class TmsEp : TmsEndpoint
        {

        }

        /// <summary>
        /// Vms modules endpoints
        /// </summary>
        public class VmsEp : VmsEndpoint
        {

        }

        /// <summary>
        /// Sample modules endpoints
        /// </summary>
        public class SampleEp : SampleEndpoint
        {

        }

        /// <summary>
        /// Author: Ngọc Thắng
        /// Description: Fms modules endpoints
        /// </summary>
        public class FmsEp : FmsEndpoint
        {

        }
    }
}
