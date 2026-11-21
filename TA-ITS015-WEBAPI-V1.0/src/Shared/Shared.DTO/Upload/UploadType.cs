
using System.ComponentModel;

namespace Shared.DTO.Upload
{
    public enum UploadType
    {
        [Description(@"Images\Avatar")]
        Avatar,
        [Description(@"Images\Image")]
        Image,
        [Description(@"Images\File")]
        File,
        [Description(@"Images\Document")]
        Document
    }
}