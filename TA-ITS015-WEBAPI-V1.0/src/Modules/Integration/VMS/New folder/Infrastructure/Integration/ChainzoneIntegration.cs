using Modules.Integration.VMS.Base.Core.Abstracts;
using Modules.Integration.VMS.Base.Core.Dtos.Chainzone;
using Modules.Integration.VMS.Base.Core.Interfaces;

namespace Modules.Integration.VMS.Chainzone.Infrastructure
{
    public class ChainzoneIntegration : BaseIntegration,IVmsIntegration
    {
        private bool _isConnected = false;

        public override event EventHandler<object>? ConnectionStatusChanged;

        public override event EventHandler<object>? ErrorOccurred;

        public override event EventHandler<object>? EventTriggered;

        public override object Connect(object? connectParams)
        {
            _isConnected = true;
            OnConnectionStatusChanged("Connected");
            return _isConnected;
        }


        public override object Disconnect(object? connectParams)
        {
            _isConnected = false;
            OnConnectionStatusChanged("Disconnected");
            return _isConnected;
        }


        public override object GetStatus(object? statusParams)
        {
            return _isConnected ? "Connected" : "Disconnected";
        }

       
        public override object GetConfig(object? configParams)
        {
            if(configParams is not IntgChainzoneGetConfigInput config || configParams == null)
            {
                OnErrorOccurred("Invalid request.");
                return false;
            }
            IntgChainzoneGetConfigOutput output = new IntgChainzoneGetConfigOutput();

            switch (config.ConfigType)
            {
                case "Brightness":
                    output.Value = new Random(DateTime.Now.Millisecond).Next(0, 100) +"";
                    break;
                case "Size":
                    output.Value = "1920x1080";
                    break;
                case "Auto":
                    for (int i = 0; i < 1000; i++)
                    {
                        output.Value = new Random(DateTime.Now.Millisecond).Next(0, 100) +"";
                        OnConnectionStatusChanged(i+" - " + output.Value);
                    }
                    
                    break;
            }

            return output;
        }

        public override object Configure(object? configParams = null)
        {

            if (configParams is not IntgChainzoneConfigureInput config || configParams == null)
            {
                OnErrorOccurred("Invalid request.");
                return false;

            }

            OnConnectionStatusChanged($"Configured Chainzone Ip: {config.Ip}, Port: {config.Port}");


            return true;
        }

        protected void OnConnectionStatusChanged(string message)
        {
            ConnectionStatusChanged?.Invoke(this, message);
        }

        protected virtual void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }
    }

}
