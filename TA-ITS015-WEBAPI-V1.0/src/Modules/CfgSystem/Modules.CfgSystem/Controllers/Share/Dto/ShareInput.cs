using Shared.Utility.Utilities;

namespace Modules.CfgSystem.Controllers.Share.Dto
{
    public class ShareInput
    {
        public string FilePath { get; set; } = string.Empty;
        public string Type { get; set; } = FileServerUtil.PathTypeEnum.Absolute;
    }
}
