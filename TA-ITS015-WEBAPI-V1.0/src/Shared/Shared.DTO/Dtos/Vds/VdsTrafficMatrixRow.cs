using System;
using Shared.DTO.Dtos.Base;

namespace Shared.DTO.Dtos.Vds
{
    /// <summary>
    /// Author: KienLT
    /// Description: Request ma trận thô VDS (QuestDB) — cross-module in-process qua message bus. TMS gọi để lấy số theo
    /// <c>(zone_id, direction, hour)</c> rồi TỰ gom cây zone + thang màu (quyết định 3b).
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficMatrixRequest : BaseEntityDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        /// <summary>Cỡ xe (short/medium/long). Null ⇒ mọi cỡ. Map sang cột <c>vehicle_size</c>.</summary>
        public string? VehicleSize { get; set; }

        /// <summary>Hướng (DirectionId). Null ⇒ mọi hướng.</summary>
        public string? Direction { get; set; }

        /// <summary>true ⇒ đếm mọi loại bản tin.</summary>
        public bool CountAllEventTypes { get; set; }
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Một ô ma trận thô: số liệu của một <c>(zone, direction)</c> trong một giờ.
    /// Trung bình vận tốc để bên nhận tự tính (SpeedSum/SpeedSampleCount) — gom cây zone cần cộng dồn có
    /// trọng số nên KHÔNG trả sẵn avg đã chia.
    /// Created date: 03/09/2026
    /// </summary>
    public class VdsTrafficMatrixRow
    {
        public string? ZoneId { get; set; }
        public string? Direction { get; set; }

        /// <summary>Giờ trong ngày (0–23), theo giờ server.</summary>
        public int Hour { get; set; }
        public DateTime BucketTime { get; set; }

        public int VehicleCount { get; set; }

        /// <summary>Tổng vận tốc các lượt xe CÓ tốc độ; null khi không có mẫu.</summary>
        public double? SpeedSum { get; set; }

        /// <summary>Số lượt xe có tốc độ — trọng số khi cộng dồn.</summary>
        public int SpeedSampleCount { get; set; }
    }
}
