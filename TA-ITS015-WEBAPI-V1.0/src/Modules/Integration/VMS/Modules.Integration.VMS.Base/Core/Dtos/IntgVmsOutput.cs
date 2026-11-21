namespace Modules.Integration.VMS.Base.Core.Dtos
{
    public class IntgVmsBaseOutput
    {
        public bool Succeeded { get; set; } = true;
        public string? Vendor { get; set; }
        public string? Message { get; set; }
        
        public Dictionary<string,object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class IntgVmsConfigureOutput : IntgVmsBaseOutput
    {

    }

    public class IntgVmsSendImageOutput : IntgVmsBaseOutput
    {

    }

    public class IntgVmsReadImageOutput : IntgVmsBaseOutput
    {
        public string Base64 { get; set; }
    }

    public class IntgVmsSendTextOutput : IntgVmsBaseOutput
    {

    }

    public class IntgVmsSetBrightnessOutput : IntgVmsBaseOutput
    {
        public object? Brightness { get; set; }
    }

    public class IntgVmsGetBrightnessOutput : IntgVmsBaseOutput
    {
        public string Brightness { get; set; }
    }

    public class IntgVmsGetStatOutput : IntgVmsBaseOutput
    {
        public Dictionary<string, object> Value { get; set; } = new Dictionary<string, object>();
    }

}
