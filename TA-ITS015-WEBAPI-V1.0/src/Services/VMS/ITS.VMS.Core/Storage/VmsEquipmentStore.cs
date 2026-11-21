using System.Text.Json;
using ITS.VMS.Core.Logging;
using ITS.VMS.Core.Messaging;

namespace ITS.VMS.Core.Storage
{
    /// <summary>
    /// Giữ danh sách thiết bị VMS/LCS mà dịch vụ dùng cho vòng lặp đọc telemetry.
    /// Dữ liệu nằm trong RAM để đọc nhanh, đồng thời ghi ra file JSON để sống qua lần khởi động lại:
    /// dịch vụ bật lên là có ngay danh sách cũ mà chạy, không phải chờ BE trả lời.
    ///
    /// LƯU Ý kiến trúc: đây là chỗ DUY NHẤT dịch vụ giữ state. Vẫn KHÔNG chạm CSDL —
    /// nguồn chân lý vẫn là DB của BE, file này chỉ là bản sao để chạy độc lập.
    ///
    /// Author: ChuongLP
    /// Created date: 23/09/2026
    /// </summary>
    public sealed class VmsEquipmentStore
    {
        private readonly VmsTelemetryOption _options;
        private readonly VmsLogger _logger;
        private readonly object _gate = new();

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

        private List<VmsEquipmentInfo> _equipments = new();
        private DateTime? _syncedAt;

        public VmsEquipmentStore(VmsTelemetryOption options, VmsLogger logger)
        {
            _options = options;
            _logger = logger;
        }

        /// <summary>Đường dẫn tuyệt đối của file lưu danh sách thiết bị.</summary>
        private string FilePath => ResolveFromBase(_options.EquipmentFile, "State/vms_equipment.json");

        /// <summary>Thời điểm BE lấy dữ liệu của lần đồng bộ gần nhất. Null = chưa có dữ liệu.</summary>
        public DateTime? SyncedAt
        {
            get { lock (_gate) return _syncedAt; }
        }

        /// <summary>Số thiết bị đang giữ.</summary>
        public int Count
        {
            get { lock (_gate) return _equipments.Count; }
        }

        /// <summary>Đã có dữ liệu để chạy vòng lặp hay chưa.</summary>
        public bool HasData => Count > 0;

        /// <summary>
        /// Lấy danh sách thiết bị ĐỦ THÔNG TIN để gọi ra thiết bị (có Vendor + Ip).
        /// Trả bản sao để người gọi duyệt thoải mái mà không sợ bị sync ghi đè giữa lúc đang chạy.
        /// </summary>
        public List<VmsEquipmentInfo> GetReadyEquipments()
        {
            lock (_gate)
            {
                return _equipments.Where(e => e.IsReady()).ToList();
            }
        }

        /// <summary>Lấy toàn bộ danh sách, kể cả thiết bị thiếu cấu hình (dùng để báo cáo).</summary>
        public List<VmsEquipmentInfo> GetAll()
        {
            lock (_gate) return _equipments.ToList();
        }

        /// <summary>Danh sách thiết bị thiếu Vendor/Ip nên không đọc được.</summary>
        public List<VmsEquipmentInfo> GetNotReadyEquipments()
        {
            lock (_gate) return _equipments.Where(e => !e.IsReady()).ToList();
        }

        /// <summary>
        /// Cập nhật danh sách từ gói BE trả về và ghi xuống file.
        /// Bỏ qua khi danh sách null để một lần BE lỗi không xoá mất dữ liệu đang chạy tốt.
        /// </summary>
        public void Update(List<VmsEquipmentInfo>? equipments, DateTime syncedAt)
        {
            if (equipments == null)
            {
                _logger.Warning(LogCategory.Config, "[Equipment] Bỏ qua cập nhật: BE trả danh sách null, giữ dữ liệu hiện có.");
                return;
            }

            lock (_gate)
            {
                _equipments = equipments.Where(e => e != null && !string.IsNullOrWhiteSpace(e.EquipmentId)).ToList();
                _syncedAt = syncedAt;
            }

            Save();

            var notReady = GetNotReadyEquipments().Count;
            _logger.Success(LogCategory.Config,
                $"[Equipment] Đã đồng bộ {Count} thiết bị" +
                (notReady > 0 ? $" ({notReady} thiết bị thiếu Vendor/Ip nên bỏ qua khi đọc)" : string.Empty));
        }

        /// <summary>
        /// Nạp danh sách từ file lúc dịch vụ khởi động. Không có file hoặc file lỗi thì bỏ qua,
        /// vòng lặp sẽ chờ lần đồng bộ đầu tiên với BE.
        /// </summary>
        public void Load()
        {
            try
            {
                var path = FilePath;
                if (!File.Exists(path))
                {
                    _logger.Info(LogCategory.Config, $"[Equipment] Chưa có file '{path}', chờ đồng bộ từ BE.");
                    return;
                }

                var snapshot = JsonSerializer.Deserialize<EquipmentSnapshot>(File.ReadAllText(path), Json);
                if (snapshot?.Equipments == null) return;

                lock (_gate)
                {
                    _equipments = snapshot.Equipments;
                    _syncedAt = snapshot.SyncedAt;
                }

                _logger.Info(LogCategory.Config,
                    $"[Equipment] Đã nạp {Count} thiết bị từ file (đồng bộ lúc {snapshot.SyncedAt:yyyy-MM-dd HH:mm:ss}).");
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Config, $"[Equipment] Không nạp được file thiết bị: {ex.Message}");
            }
        }

        /// <summary>Ghi danh sách hiện tại xuống file. Lỗi ghi file không được làm gãy luồng đọc telemetry.</summary>
        private void Save()
        {
            try
            {
                var path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                EquipmentSnapshot snapshot;
                lock (_gate)
                {
                    snapshot = new EquipmentSnapshot { SyncedAt = _syncedAt ?? DateTime.Now, Equipments = _equipments };
                }

                File.WriteAllText(path, JsonSerializer.Serialize(snapshot, Json));
            }
            catch (Exception ex)
            {
                _logger.Error(LogCategory.Config, $"[Equipment] Không ghi được file thiết bị: {ex.Message}");
            }
        }

        private static string ResolveFromBase(string? path, string fallback)
        {
            var value = string.IsNullOrWhiteSpace(path) ? fallback : path.Trim();
            return Path.IsPathRooted(value) ? value : Path.Combine(AppContext.BaseDirectory, value);
        }

        /// <summary>Cấu trúc file: kèm mốc đồng bộ để biết dữ liệu cũ bao lâu.</summary>
        private sealed class EquipmentSnapshot
        {
            public DateTime SyncedAt { get; set; }
            public List<VmsEquipmentInfo> Equipments { get; set; } = new();
        }
    }
}
