using Shared.Infrastructure.Persistence.SqlSugar;

namespace Modules.TMS.Core.Dto.Incident
{
    /// <summary>
    /// Author: ThangNN
    /// Description: DTO trang danh sách sự cố — trang dữ liệu kèm số tổng hợp của TOÀN BỘ tập lọc.
    ///
    /// <para>Đây là chỗ duy nhất trong hệ bọc thêm dữ liệu quanh <see cref="SqlSugarPagedList{T}"/>:
    /// <see cref="Statistics"/> đếm trên toàn bộ kết quả lọc chứ không phải trên trang đang xem, nên
    /// người dùng đổi trang thì ba con số đó không được đổi theo.</para>
    /// Created date: 09/08/2024
    /// </summary>
    public class TmsIncidentPageOutput : SqlSugarPagedList<PageTmsIncidentOutput>
    {
        public TmsIncidentPageStatisticsOutput Statistics { get; set; } = new();
    }

    /// <summary>
    /// Author: ThangNN
    /// Description: Số tổng hợp của toàn bộ tập lọc, độc lập với trang đang xem.
    /// Created date: 09/08/2024
    /// </summary>
    public class TmsIncidentPageStatisticsOutput
    {
        /// <summary>Tổng số hồ sơ khớp điều kiện lọc.</summary>
        public long Total { get; set; }

        /// <summary>Số hồ sơ đang ở trạng thái mới.</summary>
        public int New { get; set; }

        /// <summary>Số hồ sơ đang xử lý.</summary>
        public int Executing { get; set; }
    }
}
