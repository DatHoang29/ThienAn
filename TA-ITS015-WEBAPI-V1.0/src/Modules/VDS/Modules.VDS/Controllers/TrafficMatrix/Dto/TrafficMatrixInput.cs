namespace Modules.VDS.Controllers.TrafficMatrix.Dto
{
    /// <summary>
    /// Author: KienLT
    /// Description: Đầu vào ma trận thô VDS. FromDate/ToDate bắt buộc + trần cửa sổ (validator).
    /// Created date: 03/09/2026
    /// </summary>
    public class TrafficMatrixRawInput
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        /// <summary>Cỡ xe (short/medium/long) → cột <c>vehicleSize</c>. Null ⇒ mọi cỡ.</summary>
        public string? VehicleSize { get; set; }

        /// <summary>Hướng (DirectionId). Null ⇒ mọi hướng.</summary>
        public string? Direction { get; set; }

        /// <summary>true ⇒ đếm mọi loại bản tin.</summary>
        public bool CountAllEventTypes { get; set; }
    }
}
