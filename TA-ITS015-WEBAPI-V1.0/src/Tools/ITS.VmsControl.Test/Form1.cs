using Furion.JsonSerialization;
using Microsoft.Extensions.Localization;
using Modules.Integration.VMS.Base.Core.Constants;
using Modules.Integration.VMS.Base.Core.Dtos;
using Modules.Integration.VMS.Base.Core.Enums;
using Modules.Integration.VMS.Infrastructure.Services;
using NewLife.Serialization;
using Newtonsoft.Json;

namespace ITS.VmsControl.Test
{
    public partial class Form1 : Form
    {
        private IntgVmsManager _vmsManager;
        IStringLocalizer _localizer;
        public Form1()
        {
            InitializeComponent();
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            //_vmsManager.ConnectStatusChanged -= VmsManagerOnConnectStatusChanged;
            _vmsManager?.UnRegisterModule();
            //Register the device
            _vmsManager = new IntgVmsManager();
            _vmsManager.ConnectChanged += VmsManagerOnConnectStatusChanged;
            _vmsManager.ErrorOccurred += VmsManagerOnConnectStatusChanged;
            _vmsManager.RegisterModule(cbSdk.Text);

            //Configure the device

            switch (cbSdk.Text)
            {
                default:
                    _vmsManager.InitConfig(new IntgVmsConfigureInput()
                    {
                        Vendor = cbSdk.Text,
                        Ip = txtIp.Text,
                        Port = Int32.Parse(txtPort.Text),
                        Metadata = new Dictionary<string, object>()
                        {
                           {"PortApi", 123},
                           {"Width", 1920},
                           {"Height", 1080}
                        }
                    });


                    break;

            }

            //txtConnect.Text = _vmsManager.GetStatus()?.ToString();

        }

        private void ConnectDevice(string vendor, string config)
        {
            _vmsManager?.UnRegisterModule();
            _vmsManager = new IntgVmsManager();
            _vmsManager.RegisterModule(vendor);
            _vmsManager.InitConfig(new IntgVmsConfigureInput()
            {
                Vendor = vendor,
                Ip = txtIp.Text,
                Port = Int32.Parse(txtPort.Text),
                Metadata = JsonConvert.DeserializeObject<Dictionary<string, object>>(config) ?? new Dictionary<string, object>()

            });
        }




        private void VmsManagerOnConnectStatusChanged(object? sender, object e)
        {
            txtInfo.AppendText(Environment.NewLine + e.ToString());
        }

        private void btnGetData_Click(object sender, EventArgs e)
        {

            var rs =_vmsManager.GetStats(new IntgVmsGetStatInput()
                { Vendor = cbSdk.Text, Type = new string[] { IntgVmsEnums.IntgVmsStatType.Brightness, IntgVmsEnums.IntgVmsStatType.DoorOpen, IntgVmsEnums.IntgVmsStatType.HumidityInside } });
            txtInfo.AppendText(Environment.NewLine + JsonConvert.SerializeObject(rs));

        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var rs =_vmsManager.SendImage(new IntgVmsSendImageInput()
                { Vendor = cbSdk.Text, Url = txtImage.Text });
            txtInfo.AppendText(Environment.NewLine + JsonConvert.SerializeObject(rs));
        }
    }
}
