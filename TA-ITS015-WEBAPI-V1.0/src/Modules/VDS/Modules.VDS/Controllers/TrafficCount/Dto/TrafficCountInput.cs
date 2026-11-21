using Shared.Utility.Counting;
using Shared.Core.Utilities;

namespace Modules.VDS.Controllers.TrafficCount.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: DTO đầu vào cho số đếm xe (tính từ QuestDB bằng SAMPLE BY). Implement
    /// <see cref="ICountQueryFilter"/> + <see cref="CountSliceFolder.IEquipmentScope"/> để truyền THẲNG
    /// vào builder/folder dùng chung — WebAPI và worker cùng một công thức.
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficCountSliceInput : ICountQueryFilter, CountSliceFolder.IEquipmentScope
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Hạt thời gian (phút); chuẩn hoá về bội số của 5, tối thiểu 5.</summary>
        public int PeriodMinutes { get; set; } = TrafficCountMath.DefaultPeriodMinutes;

        /// <summary>Phạm vi gộp: <c>equipment</c> (mặc định) hoặc <c>zone</c>.</summary>
        public string ScopeType { get; set; } = TrafficCountScopes.Equipment;

        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }

        /// <summary>Đếm mọi loại bản tin (bỏ lọc Normal).</summary>
        public bool CountAllEventTypes { get; set; }

        /// <summary>Chỉ đếm lượt xe đọc được biển.</summary>
        public bool RequirePlate { get; set; }

        /// <summary>
        /// Giới hạn theo danh sách camera; rỗng = mọi camera.
        ///
        /// <para><b>Setter chuẩn hoá <c>null</c> thành danh sách rỗng.</b> Giá trị khởi tạo <c>= new()</c>
        /// KHÔNG bảo vệ được: body gửi <c>"equipmentIds": null</c> sẽ ghi null lên property, và
        /// <c>EquipmentIds.Count</c> ở <c>IsCountingEquipment</c> ném NullReferenceException TRƯỚC khi vào DB
        /// (xác nhận 22/09/2026). "Không truyền" và "truyền null" đều có nghĩa là không lọc, nên chuẩn hoá
        /// là đúng hợp đồng chứ không phải che lỗi.</para>
        /// </summary>
        public List<string> EquipmentIds
        {
            get => _equipmentIds;
            set => _equipmentIds = value ?? new();
        }

        /// <summary>Danh sách loại bản tin được đếm; rỗng = chỉ đếm Normal. Xem ghi chú null ở <see cref="EquipmentIds"/>.</summary>
        public List<string> CountedEventTypes
        {
            get => _countedEventTypes;
            set => _countedEventTypes = value ?? new();
        }

        private List<string> _equipmentIds = new();
        private List<string> _countedEventTypes = new();

        // ICountQueryFilter: chỉ đếm lượt xe bình thường (Normal) trừ khi CountAllEventTypes.
        IReadOnlyList<string> ICountQueryFilter.EffectiveEventTypes =>
            CountAllEventTypes ? Array.Empty<string>() : CountedEventTypes.Count == 0 ? TrafficCountFilter.DefaultEventTypes : CountedEventTypes;

        // IEquipmentScope: giới hạn theo EquipmentId nếu có; rỗng = mọi camera.
        bool CountSliceFolder.IEquipmentScope.IsCountingEquipment(string? equipmentId)
            => (EquipmentIds.Count == 0 || EquipmentIds.Contains(equipmentId ?? "", StringComparer.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(EquipmentId)
               || string.Equals(EquipmentId.Trim(), equipmentId?.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Số đếm GỘP một cửa sổ về một khối (short/medium/long/other + tốc độ).</summary>
    public class TrafficCountSummaryInput : TrafficCountSliceInput
    {
    }

    /// <summary>Chuỗi 24 giờ của một ngày cho một phạm vi.</summary>
    public class TrafficCountHourlyInput : ICountQueryFilter, CountSliceFolder.IEquipmentScope
    {
        /// <summary>Ngày cần dựng chuỗi 24 giờ (mốc bất kỳ trong ngày, theo giờ server sau convert).</summary>
        public DateTime? ValueDate { get; set; }

        public string ScopeType { get; set; } = TrafficCountScopes.Equipment;
        public string? EquipmentId { get; set; }
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }
        public bool CountAllEventTypes { get; set; }
        public bool RequirePlate { get; set; }

        /// <summary>Giới hạn theo danh sách camera; setter chuẩn hoá null — xem <see cref="TrafficCountSliceInput.EquipmentIds"/>.</summary>
        public List<string> EquipmentIds
        {
            get => _equipmentIds;
            set => _equipmentIds = value ?? new();
        }

        /// <summary>Danh sách loại bản tin được đếm; setter chuẩn hoá null.</summary>
        public List<string> CountedEventTypes
        {
            get => _countedEventTypes;
            set => _countedEventTypes = value ?? new();
        }

        private List<string> _equipmentIds = new();
        private List<string> _countedEventTypes = new();

        IReadOnlyList<string> ICountQueryFilter.EffectiveEventTypes =>
            CountAllEventTypes ? Array.Empty<string>() : CountedEventTypes.Count == 0 ? TrafficCountFilter.DefaultEventTypes : CountedEventTypes;

        bool CountSliceFolder.IEquipmentScope.IsCountingEquipment(string? equipmentId)
            => (EquipmentIds.Count == 0 || EquipmentIds.Contains(equipmentId ?? "", StringComparer.OrdinalIgnoreCase)) && (string.IsNullOrWhiteSpace(EquipmentId)
               || string.Equals(EquipmentId.Trim(), equipmentId?.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
