using System.Diagnostics;
using Modules.Integration.VMS.Base.Core.Abstracts;
using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Interfaces;
using Modules.Integration.VMS.Chainzone.Adapter.Infrastructure.Adapter;
using Modules.Integration.VMS.Chainzone.Core.Exceptions;
using Newtonsoft.Json;
using Shared.Utility.Helpers;
using static Modules.Integration.VMS.Base.Core.Enums.IntgVmsEnums;

namespace Modules.Integration.VMS.Chainzone.Infrastructure.Integration
{
    /// <summary>
    /// Tích hợp thiết bị VMS - Hãng Chainzone
    /// Author: SonTH
    /// Create Date: 17/2/2025
    /// </summary>
    public class ChainzoneIntegration : BaseIntegration, IVmsIntegration
    {

        private readonly ChainzoneAdapter _adapter;

        /// <summary>
        /// Constructor
        /// </summary>
        public ChainzoneIntegration()
        {
            _adapter = new ChainzoneAdapter();
        }

        /// <summary>
        /// Cấu hình kết nối thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override IntgVmsConfigureOutput Configure(IntgVmsConfigureInput input)
        {

            if (string.IsNullOrEmpty(input.Ip))
            {
                throw new Exception(BaseMsg.BaseException.NotExist);
            }


            IntgVmsConfigureOutput output = new IntgVmsConfigureOutput();
            var connectionType = input.Metadata.GetDictionaryValue("ConnectionType") ?? throw new Exception(BaseMsg.BaseException.NotExist);
            
           
            switch (connectionType)
            {
                case IntgVmsIntegrationType.Sdk:
                    if (input.Port == null || input.Port < 0)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }

                    var width = -1;
                    var height = -1;
                    var rs = int.TryParse(input.Metadata.GetDictionaryValue("Width") + "", out width);
                    rs = rs && int.TryParse(input.Metadata.GetDictionaryValue("Height") + "",out height);
                    if (!rs || height < 0 || width < 0)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }

                    output.Succeeded = _adapter.Initialize(input.Ip, input.Port.Value, width, height);
                    output.Message = "Init SDK: " + input.Ip + ":" + input.Port;
                    break;
                case IntgVmsIntegrationType.Api:

                    var portApi = -1;
                    rs = int.TryParse(input.Metadata.GetDictionaryValue("PortApi") + "", out portApi);
                    if (!rs || portApi <0)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }
                    output.Succeeded = _adapter.InitializeApi(input.Ip, (uint)portApi);
                    output.Message = "Init Api: " + input.Ip + ":" + input.Port;

                    break;
            }

            return output;
        }

        /// <summary>
        /// Gửi nội dung hình ảnh ra thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public override IntgVmsSendImageOutput SendImage(IntgVmsSendImageInput input)
        {
           
            IntgVmsSendImageOutput output = new IntgVmsSendImageOutput();

            var connectionType = input.Metadata.GetDictionaryValue("ConnectionType") ??
                                 throw new Exception(BaseMsg.BaseException.NotExist);

            switch (connectionType)
            {
                case IntgVmsIntegrationType.Sdk:
                    output.Succeeded = _adapter.SendImage(input.Url);
                    break;
                case IntgVmsIntegrationType.Api:
                    output.Succeeded = _adapter.SendImageApi(input.Url);
                    break;
            }

            return output;
        }


        /// <summary>
        /// Đọc nội dung đang hiển thị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Nội dung trả về là Base64</returns>
        /// <exception cref="Exception"></exception>
        public override IntgVmsReadImageOutput ReadImage(IntgVmsReadImageInput input)
        {
            IntgVmsReadImageOutput output = new IntgVmsReadImageOutput();

            var connectionType = input.Metadata.GetDictionaryValue("ConnectionType") ??
                                 throw new Exception(BaseMsg.BaseException.NotExist);
            
            switch (connectionType)
            {
                case IntgVmsIntegrationType.Sdk:
                    var img = _adapter.ReadImage();
                    output.Base64 = BaseHelper.ImageHp.ToBase64(img);
                    output.Succeeded = !string.IsNullOrEmpty(output.Base64);
                    break;
                case IntgVmsIntegrationType.Api:
                    output.Succeeded = _adapter.ReadImageApi(input.Url);
                    output.Base64 = BaseHelper.ImageHp.ToBase64FromUrl(input.Url);
                    break;
            }
            return output;

        }

        /// <summary>
        /// Thiết lập độ sáng
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override IntgVmsSetBrightnessOutput SetBrightness(IntgVmsSetBrightnessInput input)
        {
            IntgVmsSetBrightnessOutput output = new IntgVmsSetBrightnessOutput();

            output.Succeeded = _adapter.AdjustBrightnessApi((ushort)input.Brightness);
            return output;

        }


        /// <summary>
        /// Đọc độ sáng hiện tại
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override IntgVmsGetBrightnessOutput GetBrightness(IntgVmsGetBrightnessInput input)
        {
           
            IntgVmsGetBrightnessOutput output = new IntgVmsGetBrightnessOutput();

            output.Brightness = _adapter.ReadBrightnessApi() + "";
            output.Succeeded = string.IsNullOrEmpty(output.Brightness);

            return output;
        }


        /// <summary>
        /// Đọc thông số hoạt động cửa thiết bị
        /// Author: SonTH
        /// Create Date: 17/2/2025
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Dictionary<IntgVmsStatType,value>: dữ liệu trả về là danh sách thông số</returns>
        /// <exception cref="Exception"></exception>
        public override IntgVmsGetStatOutput GetStat(IntgVmsGetStatInput input)
        {
            IntgVmsGetStatOutput output = new IntgVmsGetStatOutput();

            var type = input.Metadata.GetDictionaryValue<IntgChainzoneStatType[]?>("Type") ??
                       throw new Exception(BaseMsg.BaseException.NotExist);
            
            var value = _adapter.ReadStat_API(type);
            output.Value = value;
            output.Succeeded = output.Value != null;

            return output;
        }

    }

}
