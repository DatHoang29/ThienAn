namespace Module.CCTV.Controllers.CctvGetDevice
{
    /// <summary>
    /// Mã trạng thái thiết bị theo chuẩn ITS.
    /// Chỉ dùng các giá trị thực tế — bỏ các giá trị tạm chưa dùng.
    /// </summary>
    public enum CctvItsStatus
    {
        KhongHoatDong = 0,
        HoatDong = 1,
        Loi = 2,
        KhongTheKetNoi = 4,
    }

    /// <summary>
    /// Map trạng thái chuỗi NxVMS → mã int ITS.
    /// Tập trung 1 chỗ: khi NxVMS thêm trạng thái mới chỉ cần sửa đây.
    /// Author: ChuongLP
    /// Date: 2026-06-04
    /// </summary>
    public static class DeviceEnum
    {
        public static int MapToItsStatus(string? nxVmsState) =>
            nxVmsState?.Trim().ToLower() switch
            {
                "online" => (int)CctvItsStatus.HoatDong,       // 1
                "recording" => (int)CctvItsStatus.HoatDong,       // 1
                "offline" => (int)CctvItsStatus.KhongTheKetNoi, // 4
                _ => (int)CctvItsStatus.Loi,            // 2 — mọi trạng thái còn lại
            };
    }



    /// <summary>
    /// 8 hướng di chuyển PTZ liên tục.
    /// FE truyền 1 trong các giá trị này — BE tự tính panSpeed/tiltSpeed tương ứng.
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public enum PtzDirection
    {
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4,
        UpLeft = 5,
        UpRight = 6,
        DownLeft = 7,
        DownRight = 8
    }

    /// <summary>
    /// Hướng zoom liên tục.
    /// Author: ChuongLP
    /// Date: 2026-06-10
    /// </summary>
    public enum ZoomDirection
    {
        In = 1,
        Out = 2
    }

    public enum FocusDirection { 
        In = 1,
        Out = 2
    }
}