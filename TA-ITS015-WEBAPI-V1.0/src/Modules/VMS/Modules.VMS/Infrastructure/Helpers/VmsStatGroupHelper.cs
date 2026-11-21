using System.Collections.Generic;
using Modules.VMS.Core.Constants;
using static Modules.Integration.VMS.Base.Core.Enums.IntgVmsEnums;

namespace Modules.VMS.Infrastructure.Helpers
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Hỗ trợ gom các thông số trạng thái thiết bị (IntgVmsStatType) thành 3 nhóm loại dữ liệu
    ///              (resource/environment/diagnostic) — tương ứng 3 cột JSON của VmsEquipmentTelemetry:
    ///              ResourceMetric / EnvironmentMetric / DiagnosticMetric.
    /// Created date: 2026-08-20
    /// </summary>
    public static class VmsStatGroupHelper
    {
        /// <summary>
        /// Danh sách thông số mặc định sẽ lấy khi đọc trạng thái thiết bị.
        /// Bám theo các field mà hàm GetStat hiện có hỗ trợ (IntgVmsStatType).
        /// </summary>
        public static readonly string[] DefaultStatTypes =
        {
            IntgVmsStatType.TemperatureInside,
            IntgVmsStatType.TemperatureOutside,
            IntgVmsStatType.HumidityInside,
            IntgVmsStatType.HumidityOutside,
            IntgVmsStatType.Gradient,
            IntgVmsStatType.WindDirection,
            IntgVmsStatType.WindSpeed,
            IntgVmsStatType.Brightness,
            IntgVmsStatType.PixelErrorCount,
            IntgVmsStatType.FirmwareVersion,
            IntgVmsStatType.DoorOpen,
            IntgVmsStatType.PowerStatus,
        };

        /// <summary>
        /// Ánh xạ một thông số về nhóm loại dữ liệu tương ứng (resource/environment/diagnostic).
        /// Thông số chưa phân loại mặc định xếp vào nhóm chẩn đoán (diagnostic).
        /// </summary>
        public static string GetGroup(string statType) => statType switch
        {
            // Môi trường xung quanh thiết bị
            IntgVmsStatType.TemperatureInside => VmsStatConst.Group.Environment,
            IntgVmsStatType.TemperatureOutside => VmsStatConst.Group.Environment,
            IntgVmsStatType.HumidityInside => VmsStatConst.Group.Environment,
            IntgVmsStatType.HumidityOutside => VmsStatConst.Group.Environment,
            IntgVmsStatType.Gradient => VmsStatConst.Group.Environment,
            IntgVmsStatType.WindDirection => VmsStatConst.Group.Environment,
            IntgVmsStatType.WindSpeed => VmsStatConst.Group.Environment,

            // Tài nguyên/vận hành hiển thị (độ sáng, nguồn cấp)
            IntgVmsStatType.Brightness => VmsStatConst.Group.Resource,
            IntgVmsStatType.PowerStatus => VmsStatConst.Group.Resource,

            // Chẩn đoán/sức khỏe thiết bị (lỗi pixel, firmware, cửa tủ)
            IntgVmsStatType.PixelErrorCount => VmsStatConst.Group.Diagnostic,
            IntgVmsStatType.FirmwareVersion => VmsStatConst.Group.Diagnostic,
            IntgVmsStatType.DoorOpen => VmsStatConst.Group.Diagnostic,

            _ => VmsStatConst.Group.Diagnostic,
        };

        /// <summary>
        /// Gom dictionary thông số (key = IntgVmsStatType) thành cấu trúc lồng theo nhóm:
        /// { "resource": {...}, "environment": {...}, "diagnostic": {...} }.
        /// </summary>
        public static Dictionary<string, Dictionary<string, object>> ToGroupedStats(Dictionary<string, object> stats)
        {
            var grouped = new Dictionary<string, Dictionary<string, object>>();
            if (stats == null) return grouped;

            foreach (var item in stats)
            {
                var group = GetGroup(item.Key);
                if (!grouped.TryGetValue(group, out var bucket))
                {
                    bucket = new Dictionary<string, object>();
                    grouped[group] = bucket;
                }
                bucket[item.Key] = item.Value;
            }

            return grouped;
        }

        /// <summary>
        /// Lấy ra dictionary thông số thuộc MỘT nhóm cụ thể (dùng để serialize vào từng cột).
        /// Trả null nếu nhóm không có thông số nào -> để cột JSON được lưu null thay vì "{}".
        /// </summary>
        public static Dictionary<string, object>? GetGroupStats(Dictionary<string, Dictionary<string, object>> grouped, string group)
        {
            if (grouped != null && grouped.TryGetValue(group, out var bucket) && bucket.Count > 0)
                return bucket;
            return null;
        }
    }
}
