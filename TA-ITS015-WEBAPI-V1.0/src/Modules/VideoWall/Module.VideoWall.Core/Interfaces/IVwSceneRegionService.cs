using Module.VideoWall.Core.Helper;

namespace Module.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Description: Interface cho VwSceneRegionService — tách phụ thuộc để ITS.VideoWall
    ///              có thể inject thông qua interface mà không cần reference trực tiếp Module.VideoWall.
    /// Created date: 10/09/2026 — cập nhật 06/11/2026 (quy đổi theo cổng ra VwControllerOutput)
    /// </summary>
    public interface IVwSceneRegionService
    {
        /// <summary>
        /// Dữ liệu quy đổi cửa sổ trên tường sang toạ độ uniform của master: khối của bố cục đang áp dụng,
        /// kích thước tường vật lý (cổng ra bộ con) và canvas master (cổng ra bộ trung tâm).
        /// </summary>
        Task<VwMasterProjection> GetMasterProjectionAsync();
    }
}
