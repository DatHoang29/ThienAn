namespace Modules.VDS.Core.Helper;

/// <summary>
/// Author: KienLT
/// Description: Đăng ký các helper dùng chung của VDS (theo mẫu <c>BaseHelper</c> của Shared.Utility và
/// <c>RptBaseHelper</c> của TMS.Report). Nơi gọi dùng <c>VdsBaseHelper.{Xxx}Hp.…</c>; chỉ khai alias, không có hàm xử lý.
/// Created date: 20/10/2026
/// </summary>
public class VdsBaseHelper
{
    /// <summary>
    /// Author: KienLT
    /// Description: Quy ước thư mục/tên file ảnh VDS trên đĩa.
    /// Created date: 20/10/2026
    /// </summary>
    public class MediaPathHp : VdsMediaPathHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Nhập/sao ảnh đăng ký VDS cho hồ sơ sự cố.
    /// Created date: 20/10/2026
    /// </summary>
    public class RegistrationImageFileHp : VdsRegistrationImageFileHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Chiều lý trình theo tuyến và mã hướng.
    /// Created date: 20/10/2026
    /// </summary>
    public class RouteOrientationHp : VdsRouteOrientationHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Tự sinh đoạn A→B từ km + tuyến + hướng camera.
    /// Created date: 20/10/2026
    /// </summary>
    public class SegmentTopologyHp : VdsSegmentTopologyHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Định nghĩa bảng QuestDB VDS cho QuestDB HA (journal + replicator).
    /// Created date: 27/10/2026
    /// </summary>
    public class QuestTableHp : VdsQuestTableHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc mã VdsConfig <c>vds_questha_*</c> thành tham số vận hành QuestDB HA (Worker + WebAPI dùng chung).
    /// Created date: 31/10/2026
    /// </summary>
    public class QuestHaConfigHp : VdsQuestHaConfigHelper
    {
    }
}
