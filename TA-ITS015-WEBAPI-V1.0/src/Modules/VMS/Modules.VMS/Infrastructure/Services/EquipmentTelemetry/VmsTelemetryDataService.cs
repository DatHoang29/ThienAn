using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Entities;
using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Modules.VMS.Core.Constants;
using Modules.VMS.Core.Entities;
using Modules.VMS.Infrastructure.Helpers;
using Shared.DTO.Constants.Module;
using Shared.Utility.Utilities;

namespace Modules.VMS.Infrastructure.Services.EquipmentTelemetry
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Phía BE của luồng telemetry do DỊCH VỤ tự giữ nhịp (kênh ta.its.data.vms).
    ///              BE ở đây chỉ làm 2 việc, đều bị động — không tự lập lịch gì:
    ///                1. <see cref="BuildEquipmentListAsync"/> : dịch vụ hỏi -> đọc DB trả danh sách thiết bị.
    ///                2. <see cref="SaveTelemetryAsync"/>      : dịch vụ đẩy số liệu -> lưu bảng VmsEquipmentTelemetry.
    ///
    ///              Thay cho job Hangfire cũ (VmsEquipmentTelemetryCollector): trước đây BE giữ nhịp và
    ///              phải chờ một gói tin cho toàn bộ thiết bị nên vỡ timeout khi số biển tăng.
    /// Created date: 23/09/2026
    /// </summary>
    public class VmsTelemetryDataService : IScoped
    {
        private readonly IMessageBus _messBus;
        private readonly BaseRepository<VmsEquipmentTelemetry> _repository;

        /// <summary>Loại thiết bị mặc định khi dịch vụ không chỉ định.</summary>
        private static readonly string[] DefaultTypeCodes = { VmsDeviceTypeConst.VMS, VmsDeviceTypeConst.LCS };

        public VmsTelemetryDataService(IMessageBus messBus, BaseRepository<VmsEquipmentTelemetry> repository)
        {
            _messBus = messBus;
            _repository = repository;
        }

        #region 1. Trả danh sách thiết bị cho dịch vụ

        /// <summary>
        /// Đọc DB trả danh sách thiết bị VMS/LCS kèm đủ thông tin kết nối để dịch vụ gọi được ra thiết bị.
        /// Lấy theo TỪNG mã loại để biết thiết bị thuộc loại nào (vms/lcs) mà gửi kèm cho dịch vụ.
        /// KHÔNG ném exception: lỗi được gói vào Success/Message để dịch vụ biết mà giữ danh sách cũ.
        /// </summary>
        public async Task<VmsEquipmentSyncResult> BuildEquipmentListAsync(VmsEquipmentSyncRequest request)
        {
            var result = new VmsEquipmentSyncResult
            {
                RequestId = request?.RequestId,
                SyncedAt = BaseUtil.TimeServer.GetTimeServer(),
            };

            try
            {
                var typeCodes = (request?.TypeCodes ?? new List<string>())
                                    .Where(c => !string.IsNullOrWhiteSpace(c))
                                    .Select(c => c.Trim())
                                    .Distinct(StringComparer.OrdinalIgnoreCase)
                                    .ToList();

                if (typeCodes.Count == 0) typeCodes = DefaultTypeCodes.ToList();

                foreach (var typeCode in typeCodes)
                {
                    var equipments = await _messBus.InvokeAsync<List<TmsEquipment>>(
                                            new GetEquipmentByTypeCodesInput { TypeCodes = new List<string> { typeCode } })
                                        ?? new List<TmsEquipment>();

                    foreach (var equipment in equipments)
                    {
                        result.Equipments.Add(new VmsEquipmentInfo
                        {
                            EquipmentId = equipment.ID,
                            Code = equipment.Code,
                            Name = equipment.Name,
                            EquipmentTypeId = equipment.EquipmentTypeId,
                            EquipmentTypeCode = typeCode,
                            Vendor = equipment.Vendor,
                            Ip = equipment.Ip,
                            // Port trong DB là string và thường để trống -> parse an toàn, dịch vụ tự dùng 80 khi null.
                            Port = int.TryParse(equipment.Port, out var port) && port > 0 ? port : (int?)null,
                            Config = equipment.Config,
                        });
                    }
                }

                result.Success = true;
                result.Message = $"Đã lấy {result.Equipments.Count} thiết bị ({string.Join(", ", typeCodes)}).";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi đọc danh sách thiết bị: {ex.Message}";
                return result;
            }
        }

        #endregion

        #region 2. Lưu telemetry do dịch vụ đẩy về

        /// <summary>
        /// Lưu một gói telemetry dịch vụ đẩy về. Mỗi thiết bị là một dòng trong VmsEquipmentTelemetry.
        ///
        /// Gói chỉ mang EquipmentId + số liệu, nên phải tra lại thiết bị trong DB để lấy Code/Name/TypeId
        /// cho bản ghi lịch sử. Thiết bị đã bị xoá khỏi DB thì bỏ qua, không lưu rác.
        /// </summary>
        /// <returns>Số bản ghi đã lưu.</returns>
        public async Task<int> SaveTelemetryAsync(VmsTelemetryPush push)
        {
            var items = push?.Items?.Where(i => i != null && !string.IsNullOrWhiteSpace(i.EquipmentId)).ToList();
            if (items == null || items.Count == 0) return 0;

            // Tra thiết bị theo ID để lấy thông tin mô tả cho bản ghi lịch sử.
            var equipmentIds = items.Select(i => i.EquipmentId!).Distinct().ToList();
            var equipments = await _messBus.InvokeAsync<List<TmsEquipment>>(
                                    new GetEquipmentByIdsInput { EquipmentIds = equipmentIds })
                                ?? new List<TmsEquipment>();

            var lookup = equipments.Where(e => !string.IsNullOrEmpty(e.ID)).ToDictionary(e => e.ID!, e => e);

            // Dùng giờ SERVER cho CollectedTime để lịch sử thống nhất một nguồn thời gian:
            // máy chạy dịch vụ có thể lệch giờ, mà khoảng cách giữa lúc đọc và lúc đẩy chỉ vài giây.
            var collectedTime = BaseUtil.TimeServer.GetTimeServer();
            var source = string.IsNullOrWhiteSpace(push!.Source) ? VmsStatConst.Source.Auto : push.Source!.Trim();

            var records = new List<VmsEquipmentTelemetry>();
            foreach (var item in items)
            {
                if (!lookup.TryGetValue(item.EquipmentId!, out var equipment)) continue;

                records.Add(BuildRecord(equipment, source, item.Success, item.Message, item.Stats, collectedTime));
            }

            if (records.Count == 0) return 0;

            await _repository.InsertRangeAsync(records);
            return records.Count;
        }

        /// <summary>Dựng một bản ghi lịch sử telemetry từ số liệu đọc được.</summary>
        private static VmsEquipmentTelemetry BuildRecord(TmsEquipment equipment, string source, bool succeeded,
                                                        string? message, Dictionary<string, object>? stats,
                                                        DateTime collectedTime)
        {
            var grouped = VmsStatGroupHelper.ToGroupedStats(stats ?? new Dictionary<string, object>());

            return new VmsEquipmentTelemetry
            {
                EquipmentId = equipment.ID,
                EquipmentCode = equipment.Code,
                EquipmentName = equipment.Name,
                EquipmentTypeId = equipment.EquipmentTypeId,
                Vendor = equipment.Vendor,
                // Tách JSON theo từng loại dữ liệu; nhóm rỗng -> lưu null thay vì chuỗi "{}"
                ResourceMetric = SerializeGroup(grouped, VmsStatConst.Group.Resource),
                EnvironmentMetric = SerializeGroup(grouped, VmsStatConst.Group.Environment),
                DiagnosticMetric = SerializeGroup(grouped, VmsStatConst.Group.Diagnostic),
                Source = source,
                Succeeded = succeeded,
                Message = message,
                CollectedTime = collectedTime,
            };
        }

        /// <summary>
        /// Serialize thông số của một nhóm thành JSON để lưu vào cột tương ứng.
        /// Nhóm rỗng -> trả null để cột được lưu null (không lưu chuỗi "{}").
        /// </summary>
        private static string? SerializeGroup(Dictionary<string, Dictionary<string, object>> grouped, string group)
        {
            var bucket = VmsStatGroupHelper.GetGroupStats(grouped, group);
            return bucket == null ? null : JsonConvert.SerializeObject(bucket);
        }

        #endregion
    }
}
