using System.ComponentModel;

namespace Modules.Integration.Delivery.Base.Enums
{
    /// <summary>
    /// Loại kênh kết nối đối tác (1-API, 2-File, 3-FTP, 4-FTPS, 5-Kafka).
    /// </summary>
    public enum ConnectionType
    {
        [Description("API")]
        Api = 1,

        [Description("File")]
        File = 2,

        [Description("FTP")]
        Ftp = 3,

        [Description("FTPS")]
        Ftps = 4,

        [Description("Kafka")]
        Kafka = 5
    }
}
