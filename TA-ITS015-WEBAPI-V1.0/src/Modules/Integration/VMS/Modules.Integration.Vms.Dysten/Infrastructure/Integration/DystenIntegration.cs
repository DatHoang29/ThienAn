using System.Diagnostics;
using Modules.Integration.VMS.Base.Core.Abstracts;
using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Interfaces;
using Modules.Integration.Vms.Dysten.Core.Exceptions;
using static Modules.Integration.VMS.Base.Core.Enums.IntgVmsEnums;

namespace Modules.Integration.Vms.Dysten.Infrastructure.Integration
{
    public class DystenIntegration : BaseIntegration, IVmsIntegration
    {


        /// <summary>
        /// Constructor
        /// </summary>
        public DystenIntegration()
        {
        }

        public override IntgVmsConfigureOutput Configure(IntgVmsConfigureInput input)
        {

            
            if (string.IsNullOrEmpty(input.Ip))
            {
                throw new Exception(BaseMsg.BaseException.NotExist);
            }


            IntgVmsConfigureOutput output = new IntgVmsConfigureOutput();
            if (!input.Metadata.ContainsKey("Type"))
            {
                throw new Exception(BaseMsg.BaseException.NotExist);
            }

            switch (input.Metadata["Type"])
            {
                case IntgVmsIntegrationType.Sdk:
                    if (input.Port == null || input.Port < 0)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }

                    if (!input.Metadata.ContainsKey("Width") || !input.Metadata.ContainsKey("Height"))
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }

                    var width = -1;
                    var height = -1;
                    var rs = int.TryParse(input.Metadata["Width"] + "", out width);
                    rs = rs && int.TryParse(input.Metadata["Height"] + "", out height);
                    if (!rs)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }

                    //output.Succeeded = _adapter.Initialize(input.Ip, input.Port.Value, width, height);
                    Debug.WriteLine("Init Sdk: " + JsonConvert.SerializeObject(input));
                    break;
                case IntgVmsIntegrationType.Api:

                    if (!input.Metadata.ContainsKey("PortApi"))
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }
                    int portApi = -1;
                    int.TryParse((input.Metadata["PortApi"]).ToString(), out portApi);
                    if (portApi < 0)
                    {
                        throw new Exception(BaseMsg.BaseException.IncorrectAcceptValue);
                    }
                    //output.Succeeded = _adapter.InitializeApi(input.Ip, (uint)portApi);
                    Debug.WriteLine("Init Api: " + JsonConvert.SerializeObject(input));

                    break;
            }

            return output;
        }

        public override IntgVmsSendImageOutput SendImage(IntgVmsSendImageInput input)
        {
           
            IntgVmsSendImageOutput output = new IntgVmsSendImageOutput();

            if (!input.Metadata.ContainsKey("Type"))
            {
                throw new Exception(BaseMsg.BaseException.NotExist);
            }


            switch (input.Metadata["Type"])
            {
                case IntgVmsIntegrationType.Sdk:
                    //output.Succeeded = _adapter.SendImage(input.Url);
                    break;
                case IntgVmsIntegrationType.Api:
                    //output.Succeeded = _adapter.SendImageApi(input.Url);
                    break;
            }

            return output;
        }



        public override IntgVmsReadImageOutput ReadImage(IntgVmsReadImageInput input )
        {

            IntgVmsReadImageOutput output = new IntgVmsReadImageOutput();

            if (!input.Metadata.ContainsKey("Type"))
            {
                throw new Exception(BaseMsg.BaseException.NotExist);
            }

            switch (input.Metadata["Type"])
            {
                case IntgVmsIntegrationType.Sdk:
                    //output.Value = _adapter.ReadImage();
                    output.Succeeded = !string.IsNullOrEmpty(output.Base64);
                    break;
                case IntgVmsIntegrationType.Api:
                    //output.Succeeded = _adapter.ReadImageApi(input.Url);
                    break;
            }
            return output;

        }

        public override IntgVmsSetBrightnessOutput SetBrightness(IntgVmsSetBrightnessInput input)
        {
            //if (brightnessParams is not IntgVmsSetBrightnessInput input || brightnessParams == null)
            //{
            //    throw new Exception("BaseMsg.IntgVms.Exception.InvalidParam");
            //}
            IntgVmsSetBrightnessOutput output = new IntgVmsSetBrightnessOutput();

            //output.Succeeded = _adapter.AdjustBrightnessApi((ushort)input.Brightness);
            return output;

        }


        public override IntgVmsGetBrightnessOutput GetBrightness(IntgVmsGetBrightnessInput input)
        {

            IntgVmsGetBrightnessOutput output = new IntgVmsGetBrightnessOutput();

            //output.Value = _adapter.ReadBrightnessApi();
            output.Succeeded = output.Brightness != null;

            return output;
        }


        public override IntgVmsGetStatOutput GetStat(IntgVmsGetStatInput input)
        {
            //if (statParams is not IntgVmsGetStatInput input || statParams == null)
            //{
            //    throw new Exception("BaseMsg.IntgVms.Exception.InvalidParam");
            //}

            IntgVmsGetStatOutput output = new IntgVmsGetStatOutput();
            //if (!input.Metadata.ContainsKey("Type"))
            //{
            //    throw new Exception(BaseMsg.BaseException.NotExist);
            //}

            //output.Value = _adapter.ReadStat_API((int[])input.Metadata["Type"]);
            output.Value.Add("Brightness",new Random().Next(1, 5000));
            output.Succeeded = output.Value != null;

            return output;
        }

    }

}
