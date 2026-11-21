namespace ITS.FA.Core.Messaging;

/// <summary>
/// Subject NATS của FA. Nguồn chân lý là ItsDataTransporterChannel (Modules.DataTransporter.ITS); khai lại thành
/// const vì module đó kéo theo toàn bộ stack WebAPI mà service không mang (cùng lý do với VdsSubjects).
/// ĐỔI GIÁ TRỊ Ở ĐÂY PHẢI ĐỔI CẢ HAI BÊN.
/// Author: ChuongLP
/// Created date: 02/11/2026
/// </summary>
public static class FaSubjects
{
    /// <summary>Sự cố dùng CHUNG subject của ITS, FE và WP_Sync đã subscribe sẵn. Cũ: TypeOfItsEvent.Incident.</summary>
    public const string Incident = "ta.its.data.incident";

    /// <summary>Trạng thái điểm báo cháy và cửa, phân biệt bằng trường "type". Cũ: TypeOfItsEvent.EquipmentState.</summary>
    public const string Data = "ta.its.data.fa";

    /// <summary>Heartbeat { data = "ok" } để FE tính FA Online/Offline.</summary>
    public const string Heartbeat = "ta.its.heartbeat.fa";
}
