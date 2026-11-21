using ITS.FA.Core.Models;

namespace ITS.FA.Core.Messaging;

/// <summary>
/// Điểm phát event realtime của FA. Thay ItsEventTransporter (Socket.IO) và SocketIOEventHandler của ITS008.
/// Các hàm không ném lỗi nghiệp vụ: dữ liệu đã ghi DB xong, mất một bản tin không được làm đứt vòng quét.
/// Author: ChuongLP
/// Created date: 29/10/2026
/// </summary>
public interface IFaEventPublisher
{
    /// <summary>
    /// Sự cố vừa TẠO MỚI. Cũ: OnAlarmActived chờ 3 giây rồi phát; nay ghi DB trước rồi mới phát nên không cần chờ.
    /// Bản tin trùng không phát vì bên nhận hiểu mỗi bản tin là một sự cố mới.
    /// </summary>
    Task PublishIncidentAsync(FaCreatedIncident created, CancellationToken ct);

    /// <summary>Điểm vừa báo cháy. Cũ chỉ phát sự cố; bản tin này giúp màn hình giám sát thấy đúng điểm.</summary>
    Task PublishAlarmAsync(FaAlarmEvent alarm, string? incidentId, CancellationToken ct);

    /// <summary>Cửa đổi trạng thái. Cũ: SocketIOEventHandler.OnDoorActived (EquipmentState, EquipmentType = "door").</summary>
    Task PublishDoorAsync(FaAlarmEvent door, CancellationToken ct);
}
