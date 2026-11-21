namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: Hợp đồng TỐI THIỂU mà <see cref="CountQueryBuilder"/> cần để dựng câu lệnh đếm.
    ///
    /// <para>Trích ra để builder dùng CHUNG cho cả worker (<c>Services.Vds</c> — <c>CountingOptions</c>) và
    /// WebAPI (<c>Modules.VDS</c> — DTO input). Lý do đã ghi trong <c>QuestCountReader</c>: "dùng chung với
    /// WebAPI để phép gộp chạy đúng MỘT công thức ở cả hai bên"; hai bản builder là hai bản sẽ lệch số.</para>
    /// </summary>
    public interface ICountQueryFilter
    {
        string? ZoneId => null;
        string? Direction => null;
        string? EquipmentId => null;
        /// <summary>Danh sách loại bản tin được đếm; RỖNG nghĩa là đếm mọi loại (không thêm điều kiện).</summary>
        IReadOnlyList<string> EffectiveEventTypes { get; }

        /// <summary>true ⇒ bỏ qua lọc loại, đếm mọi bản tin.</summary>
        bool CountAllEventTypes { get; }

        /// <summary>true ⇒ chỉ đếm lượt xe đọc được biển số.</summary>
        bool RequirePlate { get; }
    }
}
