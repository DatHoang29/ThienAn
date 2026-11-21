namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: MỘT Ô SỐ LIỆU ĐẾM của một phạm vi trong một chu kỳ (mặc định 5 phút) — đơn vị dữ liệu
    /// trung tính mà cả VDS và WebAPI đều làm việc trên đó.
    ///
    /// <para>Từ 12/09/2026 chỉ còn MỘT nguồn dựng ra kiểu này: <see cref="CountSliceFolder.Fold"/> gộp kết
    /// quả <c>SAMPLE BY</c> đọc từ bảng thô trên QuestDB. Bộ đếm trong RAM và bảng chốt
    /// <c>TmsTrafficCount</c> của mô hình cũ đã bị bỏ, nên hai bên không còn đường nào để lệch số.</para>
    /// Created date: 04/08/2026
    /// </summary>
    /// <param name="SpeedSampleCount">
    /// Số lượt xe CÓ tốc độ. Là trọng số đúng khi gộp nhiều ô: ô 10 xe mà chỉ 1 xe có tốc độ thì con số
    /// đó chỉ nặng bằng 1. Nguồn là <c>count(speed)</c> của <c>SAMPLE BY</c>.
    /// </param>
    public readonly record struct TrafficCountSlice(
        TrafficCountScope Scope,
        DateTime From,
        DateTime To,
        int Short,
        int Medium,
        int Long,
        int Other,
        double? AvgSpeed,
        double? MinSpeed,
        double? MaxSpeed,
        int SpeedSampleCount,
        string? ZoneId)
    {
        public TrafficSpeedSamples ShortSpeed { get; init; }
        public TrafficSpeedSamples MediumSpeed { get; init; }
        public TrafficSpeedSamples LongSpeed { get; init; }
        public TrafficSpeedSamples OtherSpeed { get; init; }
        /// <summary>Tổng số xe = short + medium + long + Other. Cũng là LƯU LƯỢNG của ô.</summary>
        public int Total => Short + Medium + Long + Other;
    }

    /// <summary>
    /// Kết quả GỘP nhiều ô lại thành số của một khoảng (1 giờ, 24 giờ, một giờ trong chuỗi...).
    /// Có <see cref="Total"/> tường minh vì số đếm bao gồm cả xe không suy được cỡ
    /// (<see cref="Other"/>) — bên nhận cộng short+medium+long sẽ thiếu.
    ///
    /// Tên bốn nhóm dùng ĐÚNG bộ mã cỡ xe của hệ cũ (<c>TmsVehicleLengthEnum</c>: short/medium/long) —
    /// cùng giá trị ghi trong cột <c>TmsTrafficData.Type</c> và cùng tên trường trên payload NATS.
    /// </summary>
    public readonly record struct TrafficCountBlock(
        int Short,
        int Medium,
        int Long,
        int Other,
        int Total,
        double? AvgSpeed,
        double? MinSpeed,
        double? MaxSpeed)
    {
        /// <summary>Khoảng không có xe: đếm 0 nhưng tốc độ là <c>null</c> (không có mẫu), KHÔNG phải 0.</summary>
        public static readonly TrafficCountBlock Empty = new(0, 0, 0, 0, 0, null, null, null);
    }

    public readonly record struct TrafficSpeedSamples(double Sum, int Count)
    {
        public double? Average => Count > 0 ? Sum / Count : null;
    }
}
