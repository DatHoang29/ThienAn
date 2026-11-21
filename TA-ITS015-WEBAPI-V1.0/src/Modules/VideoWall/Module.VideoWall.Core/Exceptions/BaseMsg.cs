using Shared.DTO.Constants.Localization;

namespace Module.VideoWall.Core.Exceptions
{
    /// <summary>
    /// Author: HieuNV
    /// Description: Nội dung dịch thuật riêng của phân hệ Video Wall
    /// Created date: 2026-08-01
    /// </summary>
    public class BaseMsg : BaseLocaleManager
    {
        /// <summary>
        /// Author: HieuNV
        /// Description: 
        /// Created date: 14/08/2026
        /// </summary>

        public class Vw
        {
            /// <summary>
            /// Author: HieuNV
            /// Description: Prefix
            /// Created date: 14/08/2026
            /// </summary>
            protected class Prefix
            {
                internal const string Message = "lz.message.vw.";
                internal const string Validation = "lz.validation.vw.";
                internal const string Exception = "lz.exception.vw.";
                internal const string Entity = "lz.entity.vw.";
            }

            /// <summary>
            /// Author: HieuNV
            /// Description: Tên hiển thị các trường thực thể, dùng trong message lỗi khóa ngoại.
            /// Created date: 14/08/2026
            /// </summary>
            public class Entity
            {
                /// <summary>Bộ điều khiển (VwController)</summary>
                public const string ControllerId = Prefix.Entity + "controllerId";

                /// <summary>Cổng ra của bộ điều khiển (VwControllerOutput)</summary>
                public const string ControllerOutputId = Prefix.Entity + "controllerOutputId";

                /// <summary>Serial màn hình</summary>
                public const string SerialNumber = Prefix.Entity + "serialNumber";

                /// <summary>Màn hình (VwScreen)</summary>
                public const string ScreenId = Prefix.Entity + "screenId";

                /// <summary>Nguồn tín hiệu (VwSource)</summary>
                public const string SourceId = Prefix.Entity + "sourceId";

                /// <summary>Kịch bản hiển thị (VwScene)</summary>
                public const string SceneId = Prefix.Entity + "sceneId";

                /// <summary>Kịch bản đích của lịch / luật sự kiện</summary>
                public const string TargetSceneId = Prefix.Entity + "targetSceneId";

                /// <summary>Loại sự kiện (TmsEventType)</summary>
                public const string EventTypeId = Prefix.Entity + "eventTypeId";

                /// <summary>
                /// "Người dùng (UserId)"
                /// </summary>
                public const string UserId = Prefix.Entity + "userId";

                /// <summary>
                /// "Đơn vị (OrgId)"
                /// </summary>
                public const string OrgId = Prefix.Entity + "orgId";

                /// <summary>
                /// "Tỉ lệ ô"
                /// </summary>
                public const string CellRatio = Prefix.Entity + "cellRatio";

                /// <summary>
                /// "Số ô"
                /// </summary>
                public const string CellCount = Prefix.Entity + "cellCount";
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Exception
            /// Created date: 14/08/2026
            /// </summary>

            public class Exception
            {
                /// <summary>
                /// "Kịch bản đang được sử dụng, không thể xóa."
                /// </summary>
                public const string SceneInUse = Prefix.Exception + "sceneInUse";

                /// <summary>
                /// "Bộ điều khiển đang được sử dụng, không thể xóa."
                /// </summary>
                public const string ControllerInUse = Prefix.Exception + "controllerInUse";

                /// <summary>
                /// "Không tìm thấy luật sự kiện đang bật cho loại sự kiện [{0}]."
                /// </summary>
                public const string EventRuleNotFound = Prefix.Exception + "eventRuleNotFound";

                /// <summary>
                /// "Luật sự kiện [{0}] chưa cấu hình kịch bản đích."
                /// </summary>
                public const string RuleMissingTargetScene = Prefix.Exception + "ruleMissingTargetScene";

                /// <summary>
                /// "Kịch bản [{0}] đang ở trạng thái không sử dụng, không thể kích hoạt."
                /// </summary>
                public const string SceneDisabled = Prefix.Exception + "sceneDisabled";

                /// <summary>
                /// "Hiện chưa có kịch bản nào được kích hoạt."
                /// </summary>
                public const string NoActiveScene = Prefix.Exception + "noActiveScene";

                /// <summary>
                /// "Vui lòng chọn bộ điều khiển cho kịch bản."
                /// Dùng khi tài khoản không toàn quyền bỏ trống bộ điều khiển mà hệ thống không
                /// suy ra được (đơn vị không quản lý controller nào, hoặc quản lý nhiều hơn một).
                /// </summary>
                public const string SceneControllerRequired = Prefix.Exception + "sceneControllerRequired";

                /// <summary>
                /// "Cửa sổ [{0}] nằm ngoài vùng màn hình của bộ điều khiển [{1}]."
                /// </summary>
                public const string WindowOutsideSceneRegion = Prefix.Exception + "windowOutsideSceneRegion";

                /// <summary>
                /// "Bộ điều khiển không được tự làm bộ điều khiển cha của chính nó."
                /// </summary>
                public const string ControllerCannotSelfParent = Prefix.Exception + "controllerCannotSelfParent";

                /// <summary>
                /// "Bộ điều khiển cha không tồn tại."
                /// </summary>
                public const string ParentControllerNotExist = Prefix.Exception + "parentControllerNotExist";

                /// <summary>
                /// "Vai trò bộ điều khiển không hợp lệ (bộ phân phối / bộ hiển thị / bộ độc lập)."
                /// </summary>
                public const string ControllerRoleInvalid = Prefix.Exception + "controllerRoleInvalid";

                /// <summary>
                /// "Bộ hiển thị phải chọn bộ phân phối cấp tín hiệu."
                /// </summary>
                public const string DisplayControllerRequiresParent = Prefix.Exception + "displayControllerRequiresParent";

                /// <summary>
                /// "Bộ phân phối / bộ độc lập không được có bộ cha."
                /// </summary>
                public const string ControllerRoleCannotHaveParent = Prefix.Exception + "controllerRoleCannotHaveParent";

                /// <summary>
                /// "Đã có bộ phân phối, hệ thống chỉ hỗ trợ 1 bộ phân phối."
                /// </summary>
                public const string DistributionControllerExists = Prefix.Exception + "distributionControllerExists";

                /// <summary>
                /// "Bộ cha phải là bộ phân phối."
                /// </summary>
                public const string ParentControllerMustBeDistribution = Prefix.Exception + "parentControllerMustBeDistribution";

                /// <summary>
                /// "Bộ phân phối đang cấp tín hiệu cho {0} bộ hiển thị, không thể đổi vai trò hoặc xoá."
                /// </summary>
                public const string DistributionControllerHasChildren = Prefix.Exception + "distributionControllerHasChildren";

                /// <summary>
                /// "Chưa cấu hình bộ điều khiển trung tâm (bộ điều khiển có bộ con)."
                /// </summary>
                public const string CentralControllerNotConfigured = Prefix.Exception + "centralControllerNotConfigured";

                /// <summary>
                /// "Nguồn cục bộ chỉ gắn được vào bộ điều khiển trung tâm, không gắn vào bộ con."
                /// </summary>
                public const string LocalSourceOnlyCentralController = Prefix.Exception + "localSourceOnlyCentralController";

                /// <summary>
                /// "Tài khoản chưa được phân vùng màn hình Video Wall, vui lòng liên hệ quản trị viên."
                /// </summary>
                public const string WallPermissionNotAssigned = Prefix.Exception + "wallPermissionNotAssigned";

                /// <summary>
                /// "Chỉ SuperAdmin được phép đặt kịch bản mặc định."
                /// </summary>
                public const string SetDefaultSceneSuperAdminOnly = Prefix.Exception + "setDefaultSceneSuperAdminOnly";

                /// <summary>
                /// "Cửa sổ [{0}] nằm ngoài khu vực màn hình được phân quyền của người dùng."
                /// </summary>
                public const string WindowOutsideAllowedRegion = Prefix.Exception + "windowOutsideAllowedRegion";

                /// <summary>
                /// "Lịch trùng thời điểm chạy với lịch [{0}] đang bật."
                /// </summary>
                public const string ScheduleTimeConflict = Prefix.Exception + "scheduleTimeConflict";

                /// <summary>
                /// "Lịch chạy cách lịch [{0}] đang bật không quá {1} phút. Các lịch phải cách nhau hơn {1} phút"
                /// Dùng khi khoảng đệm cấu hình > 0.
                /// </summary>
                public const string ScheduleTimeConflictBuffer = Prefix.Exception + "scheduleTimeConflictBuffer";

                /// <summary>
                /// "Bộ điều khiển gán vào ô phải là bộ hiển thị / bộ độc lập đang dùng: {0}"
                /// </summary>
                public const string BlockControllerInvalid = Prefix.Exception + "blockControllerInvalid";

                /// <summary>
                /// "Bộ điều khiển đang được gán vào ô của bố cục đang áp dụng, gỡ khỏi bố cục trước."
                /// </summary>
                public const string ControllerAssignedInLayout = Prefix.Exception + "controllerAssignedInLayout";

                /// <summary>
                /// "Chỉ khai báo được cho cổng của bộ phân phối, cấp tới bộ hiển thị là bộ con của chính bộ phân phối đó."
                /// </summary>
                public const string FeedsControllerInvalid = Prefix.Exception + "feedsControllerInvalid";

                /// <summary>
                /// "Màn hình [{0}] đang cắm ở cổng ra [{1}] — không xoá được. Tháo màn rồi đồng bộ lại từ thiết bị."
                /// </summary>
                public const string ScreenAttachedToOutput = Prefix.Exception + "screenAttachedToOutput";

                /// <summary>
                /// "Cửa sổ [{0}] nằm ngoài khối màn hình [{1}] của tường."
                /// </summary>
                public const string WindowOutsideWallBlock = Prefix.Exception + "windowOutsideWallBlock";

                /// <summary>
                /// "Nguồn [{0}] đã được dùng ở một cửa sổ khác trong kịch bản — mỗi nguồn chỉ hiển thị ở 1 cửa sổ."
                /// </summary>
                public const string SourceUsedInScene = Prefix.Exception + "sourceUsedInScene";

                /// <summary>
                /// "Kịch bản [{0}] thuộc bố cục tường khác bố cục đang dùng — không kích hoạt / chỉnh sửa được."
                /// </summary>
                public const string SceneLayoutNotCurrent = Prefix.Exception + "sceneLayoutNotCurrent";

                /// <summary>
                /// "Chưa khởi tạo bố cục tường. Vui lòng khởi tạo ở trang Bố cục tường trước."
                /// </summary>
                public const string WallLayoutNotSaved = Prefix.Exception + "wallLayoutNotSaved";

                /// <summary>
                /// "Bố cục đã có {0} kịch bản — không đổi được tỉ lệ ô vì cửa sổ đã dựng theo hình dạng ô cũ. Hãy nhân bản thành bố cục mới rồi đổi tỉ lệ."
                /// </summary>
                public const string WallCellRatioLocked = Prefix.Exception + "wallCellRatioLocked";

                /// <summary>
                /// "Tổng số ô của các khối ({0}) phải đúng bằng số ô của bố cục ({1})."
                /// </summary>
                public const string WallCellCountMismatch = Prefix.Exception + "wallCellCountMismatch";

                /// <summary>
                /// "Không thu nhỏ được bố cục: phần lưới bị cắt vẫn còn dữ liệu. Màn hình: {0}. Kịch bản: {1}. Phân quyền: {2}. Hãy gỡ các mục này khỏi phần bị cắt trước."
                /// </summary>
                public const string WallShrinkBlocked = Prefix.Exception + "wallShrinkBlocked";

                /// <summary>
                /// "Bố cục [{0}] đang dùng — không xoá được. Hãy đặt bố cục khác làm bố cục đang dùng trước."
                /// </summary>
                public const string WallTopologyCurrentCannotDelete = Prefix.Exception + "wallTopologyCurrentCannotDelete";

                /// <summary>
                /// "Phân quyền [{0}] thuộc bố cục tường khác bố cục đang dùng — không chỉnh sửa được. Hãy nhân bản sang bố cục hiện tại."
                /// </summary>
                public const string WallPermissionLayoutNotCurrent = Prefix.Exception + "wallPermissionLayoutNotCurrent";

                /// <summary>
                /// "Thiết bị [{0}] chưa khai báo đủ IP / tài khoản / mật khẩu. Hãy cập nhật ở trang Thiết bị trước."
                /// </summary>
                public const string EquipmentConnectionIncomplete = Prefix.Exception + "equipmentConnectionIncomplete";

                /// <summary>
                /// "Thiết bị đang được bộ điều khiển Video Wall [{0}] sử dụng. Hãy xoá bộ điều khiển trước."
                /// </summary>
                public const string EquipmentUsedByController = Prefix.Exception + "equipmentUsedByController";

                /// <summary>
                /// "Cổng vào này đã được khai cho nguồn [{0}] trên cùng bộ điều khiển. Mỗi cổng chỉ gắn với 1 nguồn."
                /// </summary>
                public const string SourcePortInUse = Prefix.Exception + "sourcePortInUse";
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Validation
            /// Created date: 14/08/2026
            /// </summary>

            public class Validation
            {
                /// <summary>
                /// "Cần truyền SceneId hoặc EventTypeId để xác định kịch bản cần kích hoạt."
                /// </summary>
                public const string SceneIdOrEventTypeIdRequired = Prefix.Validation + "sceneIdOrEventTypeIdRequired";

                /// <summary>
                /// "IntegrationMode phải là 'cascade', 'standalone' hoặc bỏ trống."
                /// </summary>
                public const string IntegrationModeInvalid = Prefix.Validation + "integrationModeInvalid";

                /// <summary>
                /// "Cần truyền SceneId hoặc SceneCode để xác định kịch bản."
                /// </summary>
                public const string SceneIdOrSceneCodeRequired = Prefix.Validation + "sceneIdOrSceneCodeRequired";

                /// <summary>
                /// "Không kích hoạt được khi đang bật chế độ chạy thử (DryRun)."
                /// </summary>
                public const string CannotActivateInDryRun = Prefix.Validation + "cannotActivateInDryRun";

                /// <summary>
                /// "Cần cung cấp ControllerId hoặc thông tin Device."
                /// </summary>
                public const string ControllerIdOrDeviceRequired = Prefix.Validation + "controllerIdOrDeviceRequired";

                /// <summary>
                /// "Method phải là GET, POST, PUT hoặc DELETE."
                /// </summary>
                public const string HttpMethodInvalid = Prefix.Validation + "httpMethodInvalid";

                /// <summary>
                /// "Cần truyền Code hoặc EventTypeId để xác định kịch bản."
                /// </summary>
                public const string SceneCodeOrEventTypeIdRequired = Prefix.Validation + "sceneCodeOrEventTypeIdRequired";

                /// <summary>
                /// "Phải gán quyền cho ít nhất 1 người dùng hoặc 1 đơn vị."
                /// </summary>
                public const string WallPermissionActorRequired = Prefix.Validation + "wallPermissionActorRequired";

                /// <summary>
                /// "Chưa chọn ô màn hình nào được cấp quyền."
                /// </summary>
                public const string WallPermissionNoCellSelected = Prefix.Validation + "wallPermissionNoCellSelected";

                /// <summary>
                /// "Config phải là JSON array của các ô lưới hợp lệ trong phạm vi hiển thị và không trùng lặp."
                /// </summary>
                public const string WallPermissionConfigInvalid = Prefix.Validation + "wallPermissionConfigInvalid";

                /// <summary>
                /// "Danh sách khối của tường không hợp lệ: cần từ 1 đến 8 khối, số thứ tự khối liên tục từ 0."
                /// </summary>
                public const string WallBlocksInvalid = Prefix.Validation + "wallBlocksInvalid";
            }
            /// <summary>
            /// Author: HieuNV
            /// Description: Message
            /// Created date: 14/08/2026
            /// </summary>

            public class Message
            {
                /// <summary>
                /// "Đã kích hoạt kịch bản hiển thị." — khóa dịch gửi kèm bản tin NATS cho FE.
                /// </summary>
                public const string SceneActivated = Prefix.Message + "sceneActivated";
            }
        }
    }
}
