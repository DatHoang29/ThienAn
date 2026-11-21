using Modules.TMS.Core.Dto.Equipment;
using Modules.TMS.Core.Entities;
using Modules.VMS.Controllers.EquipmentTelemetry.Dto;
using Modules.VMS.Controllers.Process.Dto;
using Modules.VMS.Core.Constants;
using Modules.VMS.Core.Entities;
using Modules.VMS.Core.Exceptions;
using Modules.VMS.Infrastructure.Helpers;
using Modules.VMS.Infrastructure.Services.Process;
using Shared.Utility.Utilities;

namespace Modules.VMS.Infrastructure.Services.EquipmentTelemetry
{
    /// <summary>
    /// Author: ChuongLP
    /// Description: Đọc ảnh hiện tại và trạng thái bên trong thiết bị VMS/LCS.
    /// Created date: 2026-08-20
    /// Trước đây service này tự nạp IntgVmsManager để nói chuyện với thiết bị, nên SDK native
    /// (czVMSII / czJetFileII / Snmp) vẫn nằm trong WebAPI và có thể làm sập tiến trình.
    /// Nay mọi lệnh đọc đi qua kênh NATS ta.its.control.vms tới dịch vụ ITS.VMS.Worker:
    ///   - ReadImageAsync  -> Action "read-image"
    ///   - GetStatAsync    -> Action "get-stat"
    /// Đây là bước cuối để SDK native ra khỏi WebAPI hoàn toàn.
    /// </summary>
    public class VmsEquipmentTelemetryService : IScoped
    {
        private readonly IMessageBus _messBus;

        public VmsEquipmentTelemetryService(IMessageBus messBus)
        {
            _messBus = messBus;
        }

        #region Public - dùng cho Controller (thủ công)

        /// <summary>
        /// Đọc ảnh đang hiển thị trên thiết bị (trả base64 để FE đối chiếu với DB).
        /// Author: ChuongLP
        /// Created date: 2026-08-20
        /// </summary>
        public async Task<ReadDeviceImageOutput> ReadImageAsync(ReadDeviceImageInput input)
        {
            var equipment = await GetEquipmentByIdAsync(input.EquipmentId);
            EnsureDeviceReady(equipment);   // Luồng thủ công: thiếu cấu hình -> ném Oops.Oh (trả lỗi cho FE) như các module khác.

            if (IsVmsLocal())
            {
                return new ReadDeviceImageOutput
                {
                    Succeeded = false,
                    Vendor = equipment.Vendor,
                    Message = "Đang bật giả lập thiết bị (DeviceIntegration:VmsLocal = true) nên không đọc được ảnh thật.",
                };
            }

            var request = BuildRequest(VmsControlAction.ReadImage, new List<TmsEquipment> { equipment });
            var result = await VmsControlBridge.SendAsync(request);
            var one = FindResult(result, equipment.ID);

            return new ReadDeviceImageOutput
            {
                Succeeded = one?.Success == true,
                Vendor = equipment.Vendor,
                Base64 = ToBase64String(one?.Data),
                ContentType = "image/bmp",
                Message = one?.Message ?? result?.Message,
            };
        }

        /// <summary>
        /// Đọc trạng thái bên trong thiết bị và gom nhóm theo nội dung.
        /// Author: ChuongLP
        /// Created date: 2026-08-20
        /// </summary>
        public async Task<GetDeviceStatOutput> GetStatAsync(GetDeviceStatInput input)
        {
            var equipment = await GetEquipmentByIdAsync(input.EquipmentId);
            EnsureDeviceReady(equipment);   // Luồng thủ công: thiếu cấu hình -> ném Oops.Oh (trả lỗi cho FE) như các module khác.

            var statTypes = (input.StatTypes != null && input.StatTypes.Length > 0)
                ? input.StatTypes
                : VmsStatGroupHelper.DefaultStatTypes;

            if (IsVmsLocal())
            {
                return new GetDeviceStatOutput
                {
                    Succeeded = false,
                    Vendor = equipment.Vendor,
                    Message = "Đang bật giả lập thiết bị (DeviceIntegration:VmsLocal = true) nên không đọc được thông số thật.",
                    Groups = VmsStatGroupHelper.ToGroupedStats(new Dictionary<string, object>()),
                };
            }

            var request = BuildRequest(VmsControlAction.GetStat, new List<TmsEquipment> { equipment }, statTypes);
            var result = await VmsControlBridge.SendAsync(request);
            var one = FindResult(result, equipment.ID);

            return new GetDeviceStatOutput
            {
                Succeeded = one?.Success == true,
                Vendor = equipment.Vendor,
                Message = one?.Message ?? result?.Message,
                Groups = VmsStatGroupHelper.ToGroupedStats(ToStatDictionary(one?.Data)),
            };
        }

        #endregion

        // Phần đọc telemetry THEO LÔ đã BỎ ngày 23/09/2026 (trước là BuildTelemetryRecordsAsync,
        // dùng cho job Hangfire). Việc thu thập định kỳ chuyển sang dịch vụ ITS.VMS.Worker tự giữ nhịp;
        // BE nhận số liệu qua kênh ta.its.data.vms và lưu bằng VmsTelemetryDataService.
        // Lớp này giờ chỉ còn phục vụ 2 API ĐỌC THỦ CÔNG cho FE ở trên.

        #region Private - dựng gói tin & đọc kết quả

        /// <summary>
        /// Đóng gói danh sách thiết bị thành gói lệnh gửi cho dịch vụ.
        /// Thông tin kết nối (Vendor/Ip/Port/Config) lấy trực tiếp từ DB — dịch vụ STATELESS, không tự tra.
        /// </summary>
        private static VmsControlRequest BuildRequest(string action, List<TmsEquipment> equipments, string[]? statTypes = null)
        {
            return new VmsControlRequest
            {
                Type = VmsControlType.Request,
                RequestId = VmsControlBridge.NewRequestId(),
                Action = action,
                Simulate = GetBoolConfig("DeviceIntegration:VmsSimulate"),
                RequestTime = DateTime.Now,
                StatTypes = statTypes,
                Targets = equipments.Select(e => new VmsControlTarget
                {
                    EquipmentId = e.ID,
                    Vendor = e.Vendor,
                    Ip = e.Ip,
                    // Port trong DB là string và thường để trống -> parse an toàn, dịch vụ tự dùng 80 khi null.
                    Port = int.TryParse(e.Port, out var port) && port > 0 ? port : (int?)null,
                    Config = e.Config,
                }).ToList(),
            };
        }

        /// <summary>Tìm kết quả của một thiết bị trong gói result.</summary>
        private static VmsTargetResult? FindResult(VmsControlResult? result, string? equipmentId)
            => result?.Results?.FirstOrDefault(r => r.EquipmentId == equipmentId);

        /// <summary>
        /// Đổi trường Data của kết quả (get-stat) về dictionary thông số.
        /// Data đi qua JSON nên tới đây là JObject, không phải Dictionary — phải chuyển lại.
        /// </summary>
        private static Dictionary<string, object> ToStatDictionary(object? data)
        {
            if (data == null) return new Dictionary<string, object>();
            if (data is Dictionary<string, object> dict) return dict;

            try
            {
                var json = data is string text ? text : JsonConvert.SerializeObject(data);
                return JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }
            catch
            {
                // Dữ liệu trả về không đúng dạng object -> coi như không đọc được thông số nào.
                return new Dictionary<string, object>();
            }
        }

        /// <summary>Đổi trường Data của kết quả (read-image) về chuỗi Base64.</summary>
        private static string? ToBase64String(object? data)
        {
            if (data == null) return null;
            if (data is string text) return string.IsNullOrWhiteSpace(text) ? null : text;

            var value = data.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // GetTelemetryTimeoutSeconds đã bỏ cùng phần đọc theo lô: chu kỳ và timeout của luồng tự động
        // giờ do dịch vụ ITS.VMS.Worker quản lý (section "Telemetry" trong appsettings của dịch vụ).

        /// <summary>Đọc cờ cấu hình bool, thiếu khóa thì coi như false.</summary>
        private static bool GetBoolConfig(string key)
        {
            try
            {
                return App.GetConfig<bool>(key);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Có đang bật giả lập thiết bị ngay tại BE hay không (lúc đó không phát NATS).</summary>
        private static bool IsVmsLocal() => GetBoolConfig("DeviceIntegration:VmsLocal");

        #endregion

        #region Private - tra cứu thiết bị

        /// <summary>
        /// Lấy thông tin thiết bị theo ID qua MessBus (chỉ thiết bị Enable, chưa xóa).
        /// Author: ChuongLP
        /// Created date: 2026-08-20
        /// </summary>
        private async Task<TmsEquipment> GetEquipmentByIdAsync(string equipmentId)
        {
            var list = await _messBus.InvokeAsync<List<TmsEquipment>>(
                            new GetEquipmentByIdsInput { EquipmentIds = new List<string> { equipmentId } })
                        ?? new List<TmsEquipment>();

            var equipment = list.FirstOrDefault();
            if (equipment == null)
                throw Oops.Oh(BaseMsg.BaseException.NotExist, BaseMsg.BaseEntity.ID);

            return equipment;
        }

        /// <summary>
        /// Kiểm tra thiết bị đã đủ cấu hình để tích hợp (Vendor + IP). Thiếu -> ném Oops.Oh theo chuẩn chung.
        /// Author: ChuongLP
        /// Created date: 2026-08-26
        /// </summary>
        private static void EnsureDeviceReady(TmsEquipment equipment)
        {
            if (string.IsNullOrWhiteSpace(equipment.Vendor))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.Vms.Entity.Vendor);

            if (string.IsNullOrWhiteSpace(equipment.Ip))
                throw Oops.Oh(BaseMsg.BaseValidation.Required, BaseMsg.BaseEntity.IpAddress);
        }

        #endregion
    }
}
