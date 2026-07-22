using Shared.Utility.Endpoints.CfgSystem;
using Shared.Utility.Endpoints.Sample;
using Shared.Utility.Endpoints.System;

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
        /// Sample modules endpoints
        /// </summary>
        public class SampleEp : SampleEndpoint
        {

        }

        
    }
}
