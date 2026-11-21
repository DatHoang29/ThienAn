using Modules.Integration.VMS.Base.Core.Enums;

namespace Modules.Integration.VMS.Base.Core.Dtos
{
    public class IntgVmsBaseInput
    {
        public string? Vendor { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

    }

    public class IntgVmsConfigureInput : IntgVmsBaseInput
    {
        public string? Ip { get; set; }
        public int? Port { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }


    }

    public class IntgVmsSendImageInput : IntgVmsBaseInput
    {
        /// <summary>
        /// Đường dẫn hình ảnh gửi ra thiết bị
        /// </summary>
        public string Url { get; set; }
    }

    public class IntgVmsReadImageInput : IntgVmsBaseInput
    {
        /// <summary>
        /// Đường dẫn hình ảnh lưu trữ về
        /// </summary>
        public string Url { get; set; }
    }


    public class IntgVmsSendTextInput : IntgVmsBaseInput
    {
        public string Content { get; set; }
    }


    public class IntgVmsSetBrightnessInput : IntgVmsBaseInput
    {
        public int Brightness { get; set; }
    }

    public class IntgVmsGetBrightnessInput : IntgVmsBaseInput
    {

    }

    public class IntgVmsGetStatInput : IntgVmsBaseInput
    {
        public string[] Type { get; set; }
    }
}
