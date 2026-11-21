using Module.VideoWall.Core.Dto.DeviceSetup;
using Module.VideoWall.Core.Entities;

namespace Module.VideoWall.Core.Interfaces
{
    /// <summary>
    /// Author: Đạt
    /// Description: Ghi nhật ký kích hoạt kịch bản và thao tác thiết bị Video Wall vào VwEventTriggerLog.
    ///
    ///              MỘT DÒNG = MỘT BƯỚC ISAPI (1:1 với <see cref="VwSetupSceneStep"/>) hoặc MỘT LƯỢT KÍCH HOẠT NGHIỆP VỤ,
    ///              kể cả bước Skipped của chế độ chạy thử — nhờ vậy tra lại được nguyên văn XML gửi/nhận của
    ///              từng bước, thay vì chỉ biết cả lượt gọi thành công hay thất bại.
    ///
    ///              NGUYÊN TẮC: ghi log KHÔNG ĐƯỢC làm hỏng nghiệp vụ chính. Mọi lỗi phát sinh khi
    ///              ghi đều bị nuốt và chỉ đẩy ra log hệ thống.
    /// Created date: 26/08/2026
    /// </summary>
    public interface IVwEventTriggerLogService
    {
        /// <summary>
        /// Ghi nhiều bước của cùng một lượt gọi. Danh sách rỗng thì không làm gì.
        /// </summary>
        Task WriteSteps(
            string action,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            IReadOnlyList<VwSetupSceneStep>? steps,
            CancellationToken ct = default);

        /// <summary>
        /// Ghi một bước đơn lẻ (Ping / Isapi trả về đúng một bước).
        /// </summary>
        Task WriteStep(
            string action,
            string? controllerId,
            string? sceneId,
            string? sceneCode,
            VwSetupSceneStep? step,
            CancellationToken ct = default);

        /// <summary>
        /// Ghi một dòng đã dựng sẵn. Server vẫn đóng dấu lại OrgId / ControllerName / TriggeredAt /
        /// OperatorName — giá trị client gửi lên ở các trường đó bị bỏ qua.
        /// </summary>
        Task Write(VwEventTriggerLog log, CancellationToken ct = default);
    }
}
