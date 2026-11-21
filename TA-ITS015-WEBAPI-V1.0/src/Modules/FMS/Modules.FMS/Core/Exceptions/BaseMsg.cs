using Shared.DTO.Constants.Localization;

namespace Modules.FMS.Core.Exceptions
{
    /// <summary>
    /// Author: ThangNN
    /// Created date: 19/08/2024
    /// </summary>
    internal class BaseMsg : BaseLocaleManager
    {
        // ─────────────────────────────────────────────────────────────
        // FMS — thông báo chung toàn module FMS
        // ─────────────────────────────────────────────────────────────
        internal class Fms
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.fms.";
                internal const string Validation = "lz.validation.fms.";
                internal const string Exception = "lz.exception.fms.";
                internal const string Entity = "lz.entity.fms.";
            }

            internal class Entity
            {
                internal const string EquipmentId = Prefix.Entity + "equipmentId";
                internal const string EquipmentType = Prefix.Entity + "equipmentType";
                internal const string TopologyId = Prefix.Entity + "topologyId";
                internal const string TopologyName = Prefix.Entity + "topologyName";
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FmsTopology — thông báo riêng cho chức năng Topology
        // ─────────────────────────────────────────────────────────────
        internal class FmsTopology
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.fmsTopology.";
                internal const string Validation = "lz.validation.fmsTopology.";
                internal const string Exception = "lz.exception.fmsTopology.";
                internal const string Entity = "lz.entity.fmsTopology.";
            }

            internal class Entity
            {
                internal const string Code = Prefix.Entity + "code";
                internal const string Name = Prefix.Entity + "name";
                internal const string Value = Prefix.Entity + "value";
                internal const string FileName = Prefix.Entity + "fileName";
            }

            internal class Message
            {
                /// <summary>
                /// "Đã gửi yêu cầu lấy dữ liệu Topology. Vui lòng chờ kết quả xử lý."
                /// </summary>
                internal const string FetchRequested = Prefix.Message + "fetchRequested";
            }

            internal class Exception
            {
                internal const string ConfigFolderNotFound = Prefix.Exception + "configFolderNotFound";
                internal const string ConfigFileNotFound = Prefix.Exception + "configFileNotFound";
                internal const string FetchFailed = Prefix.Exception + "fetchFailed";
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FmsEquipmentTelemetry — thông báo riêng cho chức năng Telemetry
        // ─────────────────────────────────────────────────────────────
        internal class FmsEquipmentTelemetry
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.fmsEquipmentTelemetry.";
                internal const string Validation = "lz.validation.fmsEquipmentTelemetry.";
                internal const string Exception = "lz.exception.fmsEquipmentTelemetry.";
                internal const string Entity = "lz.entity.fmsEquipmentTelemetry.";
            }

            internal class Entity
            {
                internal const string EquipmentId = Prefix.Entity + "equipmentId";
                internal const string EquipmentCode = Prefix.Entity + "equipmentCode";
                internal const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
                internal const string Source = Prefix.Entity + "source";
                internal const string CollectedTime = Prefix.Entity + "collectedTime";
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FmsEquipmentHistory — thông báo riêng cho chức năng EquipmentHistory
        // ─────────────────────────────────────────────────────────────
        internal class FmsEquipmentHistory
        {
            protected class Prefix
            {
                internal const string Message = "lz.message.fmsEquipmentHistory.";
                internal const string Validation = "lz.validation.fmsEquipmentHistory.";
                internal const string Exception = "lz.exception.fmsEquipmentHistory.";
                internal const string Entity = "lz.entity.fmsEquipmentHistory.";
            }

            internal class Entity
            {
                internal const string EquipmentId = Prefix.Entity + "equipmentId";
                internal const string NewState = Prefix.Entity + "newState";
                internal const string OldState = Prefix.Entity + "oldState";
            }
        }

        // ─────────────────────────────────────────────────────────────
        // FmsEquipmentHistoryChart — thông báo riêng cho biểu đồ lịch sử
        // ─────────────────────────────────────────────────────────────
        internal class FmsEquipmentHistoryChart
        {
            protected class Prefix
            {
                internal const string Label = "lz.label.fmsEquipmentHistoryChart.";
                internal const string Message = "lz.message.fmsEquipmentHistoryChart.";
                internal const string Validation = "lz.validation.fmsEquipmentHistoryChart.";
                internal const string Exception = "lz.exception.fmsEquipmentHistoryChart.";
                internal const string Entity = "lz.entity.fmsEquipmentHistoryChart.";
            }

            internal class Label
            {
                internal const string InvalidEquipmentHistoryChart = Prefix.Label + "invalidEquipmentHistoryChart";
            }

            internal class Entity
            {
                internal const string EquipmentTypeId = Prefix.Entity + "equipmentTypeId";
            }
        }
    }
}
