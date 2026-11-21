namespace ITS.PA.Core.Messaging;

/// <summary>
/// Subject NATS của PA. Nguồn chân lý là ItsDataTransporterChannel (Modules.DataTransporter.ITS); khai lại thành
/// const vì module đó kéo theo toàn bộ stack WebAPI mà service không mang. ĐỔI GIÁ TRỊ Ở ĐÂY PHẢI ĐỔI CẢ HAI BÊN.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public static class PaSubjects
{
    /// <summary>Kênh điều khiển dùng chung 2 chiều, phân biệt bằng trường Type.</summary>
    public const string Control = "ta.its.control.pa";

    /// <summary>Heartbeat { data = "ok" } để FE tính PA Online/Offline.</summary>
    public const string Heartbeat = "ta.its.heartbeat.pa";
}
