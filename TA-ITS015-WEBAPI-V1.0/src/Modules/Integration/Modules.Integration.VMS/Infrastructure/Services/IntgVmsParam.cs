using System.Diagnostics;
using System.Reflection;
using Modules.Integration.VMS.Base.Core.Constants;
using Modules.Integration.VMS.Base.Core.Dtos.Base;
using Modules.Integration.VMS.Base.Core.Dtos.Chainzone;
using Modules.Integration.VMS.Base.Core.Interfaces;
using IContainer = Autofac.IContainer;

namespace Modules.Integration.VMS.Infrastructure.Services
{
    public class IntgVmsParam
    {
        /// <summary>
        /// Tạo param cấu hình
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static dynamic? GenerateConfigureParam(dynamic param)
        {
            switch (param.Vendor)
            {
                case IntgVmsVendor.Chainzone:
                    return new IntgChainzoneConfigureInput()
                    {
                        Ip = param.Ip,
                        PortApi = param.Port,
                        Port = param.Port,
                        Type = param.Type,
                        Width = param.Width,
                        Height = param.Height

                    };
                    break;
                case IntgVmsVendor.Dysten:
                    break;
            }

            return null;
        }


        /// <summary>
        /// Tạo param gửi ảnh
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static object GenerateSendImageParam(dynamic param)
        {
            switch (param.Vendor)
            {
                case IntgVmsVendor.Chainzone:

                    return new IntgChainzoneSendImageInput()
                    {
                        
                        Type = param.Type,
                        Url = param.Url

                    };
                    break;
                case IntgVmsVendor.Dysten:
                    break;
            }

            return null;
        }

        /// <summary>
        /// Tạo param đọc ảnh
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public static object GenerateReadImageParam(dynamic param)
        {
            switch (param.Vendor)
            {
                case IntgVmsVendor.Chainzone:
                    return new IntgChainzoneReadImageInput()
                    {
                        
                        Type = param.Type,
                        Url = param.Url

                    };
                    break;

                case IntgVmsVendor.Dysten:


                    break;
            }

            return null;
        }


    }
}
