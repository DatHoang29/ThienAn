using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modules.DataTransporter.Base.Core.Abstractions;
using Modules.DataTransporter.Base.Infrastructure;

namespace Modules.DataTransporter.ITS.Infrastructure.Services
{
    public class ItsDataTransporterChannel
    {
        // TODO sắp xếp lại thứ tự 
        public const string Test = "ta.test";
        public const string CountVehicle = "ta.count";
        public const string VmsUpdate = "ta.its.data.vms";
        public const string Incident = "ta.its.data.incident";
        public const string GetStatus = "ta.its.status.fms";
        public const string DeviceStatus = "ta.its.status.device";
        public const string TrafficStatus = "ta.its.status.traffic";
        public const string DeviceData = "ta.its.data.device";
        public const string VideoWallData = "ta.its.data.videowall";
        public const string VideoWallControl = "ta.its.data.videowall.control";
        public const string EquipmentVisible = "ta.its.data.equipment.visible";
        public const string VdsStatistic = "ta.its.data.vds.statistic";
        public const string VdsCount = "ta.its.data.vds.count";
        public const string VdsTraffic = "ta.its.data.vds.traffic";
        public const string VdsZone = "ta.its.data.vds.zone";
        // Cảnh báo biển số danh sách đen/trắng do VDS phát (ITS.VDS.Core/Common/Messaging/VdsSubjects.Filter).
        // Cùng tiền tố ta.its.data.vds. với traffic/zone/count (đổi 13/08/2026 từ ta.its.vds.filter).
        public const string VdsFilter = "ta.its.data.vds.filter";
        public const string GetData = "ta.its.data";
        public const string VmsUpdateImage = "ta.its.data.vms";    // TODO: dieu chinh theo rule moi
        public const string Heartbeat = "ta.its.heartbeat";
        public const string HeartbeatPrefix = "ta.its.heartbeat";
        public const string HeartbeatVms = "ta.its.heartbeat.vms";
        public const string HeartbeatToll = "ta.its.heartbeat.toll";
        public const string HeartbeatVds = "ta.its.heartbeat.vds";
        public const string HeartbeatFms = "ta.its.heartbeat.fms";
        public const string HeartbeatFa = "ta.its.heartbeat.fa";
        public const string HeartbeatPa = "ta.its.heartbeat.pa";
        // Trạng thái điểm báo cháy và cửa do dịch vụ FA phát (ITS.FA.Core/Messaging/FaSubjects.Data).
        public const string FaData = "ta.its.data.fa";
        public const string HeartbeatSample = "ta.its.heartbeat.sample";
        public const string ControlFms = "ta.its.control.fms";
        public const string ControlToll = "ta.its.control.toll";
        public const string ControlVms = "ta.its.control.vms";
        // Kênh điều khiển PA dùng chung 2 chiều, phân biệt bằng field "Type" (ITS.PA.Core/Messaging/PaSubjects.Control).
        public const string ControlPa = "ta.its.control.pa";
        public const string VmsData = "ta.its.data.vms";
        // Kênh điều khiển lấy dữ liệu Topology thủ công (dùng chung 2 chiều, phân biệt bằng field "Type").
        // - Type = "request": BE phát lệnh -> dịch vụ DeviceMonitor thu thập lại topology mới nhất và ghi file.
        // - Type = "result" : dịch vụ phản hồi kết quả (thành công/lỗi + số bản ghi) để FE (subscribe trực tiếp) hiển thị + nạp lại sơ đồ.
        public const string ControlTopology = "ta.its.control.topology";

        // Kênh thông báo (module Notification) — chỉ phát TÍN HIỆU NHẸ (MessageId + danh sách UserId
        // khi nhỏ + severity), KHÔNG kèm nội dung. Client thấy UserId của mình mới gọi REST lấy chi tiết.
        public const string Notification = "ta.its.noti";
    }

    public class ItsDataTransporter : BaseDataTransporter
    {
        public static void RegisterChannel()
        {
            if (handlers == null || !handlers.Any()) return;

            Parallel.ForEach(handlers, handler =>
            {
                handler.RegisterChannel(ItsDataTransporterChannel.CountVehicle);
                handler.RegisterChannel(ItsDataTransporterChannel.Test);
                handler.RegisterChannel(ItsDataTransporterChannel.Incident);
                handler.RegisterChannel(ItsDataTransporterChannel.VmsUpdateImage);
                handler.RegisterChannel(ItsDataTransporterChannel.GetStatus);
                handler.RegisterChannel(ItsDataTransporterChannel.DeviceStatus);
                handler.RegisterChannel(ItsDataTransporterChannel.TrafficStatus);
                handler.RegisterChannel(ItsDataTransporterChannel.DeviceData);
                handler.RegisterChannel(ItsDataTransporterChannel.VideoWallData);
                handler.RegisterChannel(ItsDataTransporterChannel.VideoWallControl);
                handler.RegisterChannel(ItsDataTransporterChannel.EquipmentVisible);
                handler.RegisterChannel(ItsDataTransporterChannel.VdsCount);
                handler.RegisterChannel(ItsDataTransporterChannel.VdsStatistic);
                handler.RegisterChannel(ItsDataTransporterChannel.VdsTraffic);
                handler.RegisterChannel(ItsDataTransporterChannel.VdsZone);
                handler.RegisterChannel(ItsDataTransporterChannel.VdsFilter);
                handler.RegisterChannel(ItsDataTransporterChannel.FaData);
                handler.RegisterChannel(ItsDataTransporterChannel.ControlToll);
                handler.RegisterChannel(ItsDataTransporterChannel.ControlFms);
                handler.RegisterChannel(ItsDataTransporterChannel.ControlVms);
                handler.RegisterChannel(ItsDataTransporterChannel.ControlPa);
                handler.RegisterChannel(ItsDataTransporterChannel.ControlTopology);
                handler.RegisterChannel(ItsDataTransporterChannel.Notification);
            });
        }

        public static void BroadcastData(string subject, object data)
        {
            if (handlers == null || !handlers.Any() || data == null) return;

            Parallel.ForEach(handlers, handler =>
            {
                handler.PublishData(subject, data);
            });
        }

        public static Dictionary<string, bool> BroadcastDataWithResult(string subject, object data)
        {
            var results = new Dictionary<string, bool>();
            if (handlers == null || !handlers.Any() || data == null) return results;

            foreach (var handler in handlers)
            {
                var handlerName = handler.GetType().Name;
                results[handlerName] = handler.PublishData(subject, data);
            }
            return results;
        }

        public static void ConsumeData(string subject, Action<object> onDataReceived)
        {
            BaseDataTransporter.ConsumeData(subject, (_, data) => onDataReceived(data));
        }
    }

    /// <summary>Publish only after the database commit. Sent is transport acceptance, not service acknowledgement.</summary>
    public static class ItsRefresh
    {
        public const string Subject = "ta.its.refresh";
        public static Dictionary<string, bool> Send(int code)
        {
            if (code is < 0 or > 6) throw new ArgumentOutOfRangeException(nameof(code));
            try { return ItsDataTransporter.BroadcastDataWithResult(Subject, new { code }); }
            catch { return new Dictionary<string, bool> { ["transport"] = false }; }
        }
    }
}
