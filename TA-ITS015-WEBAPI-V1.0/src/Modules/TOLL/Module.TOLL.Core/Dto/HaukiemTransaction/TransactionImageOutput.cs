namespace Module.TOLL.Core.Dto.HaukiemTransaction
{
    /// <summary>
    /// Kết quả ảnh hậu kiểm trả về cho FE.
    /// Mỗi trường là chuỗi data URI base64 ("data:image/jpeg;base64,...") tương ứng 1 vị trí ảnh,
    /// null nếu TCS không có ảnh cho đường dẫn đó.
    /// Author: ChuongLP
    /// Created date: 25/08/2026
    /// </summary>
    public class TransactionImageOutput
    {
        /// <summary>Ảnh làn vào (base64 data URI).</summary>
        public string? ImageLaneIn { get; set; }

        /// <summary>Ảnh làn ra (base64 data URI).</summary>
        public string? ImageLane { get; set; }

        /// <summary>Ảnh biển số ra (base64 data URI).</summary>
        public string? ImagePlate { get; set; }

        /// <summary>Ảnh biển số vào (base64 data URI).</summary>
        public string? ImagePlateIn { get; set; }

        /// <summary>True nếu gọi TCS thành công.</summary>
        public bool Success { get; set; }

        /// <summary>Thông tin lỗi khi gọi TCS thất bại (nếu có).</summary>
        public string? Error { get; set; }
    }
}
