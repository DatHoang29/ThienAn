namespace Module.TOLL.Core.Dto.HaukiemTransaction
{
    /// <summary>
    /// Input yêu cầu lấy ảnh hậu kiểm của MỘT lượt xe.
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    public class TransactionImageInput
    {
        /// <summary>Đường dẫn ảnh làn vào.</summary>
        public string? ImageLaneIn { get; set; }

        /// <summary>Đường dẫn ảnh làn ra.</summary>
        public string? ImageLane { get; set; }

        /// <summary>Đường dẫn ảnh biển số ra.</summary>
        public string? ImagePlate { get; set; }

        /// <summary>Đường dẫn ảnh biển số vào.</summary>
        public string? ImagePlateIn { get; set; }
    }
}
