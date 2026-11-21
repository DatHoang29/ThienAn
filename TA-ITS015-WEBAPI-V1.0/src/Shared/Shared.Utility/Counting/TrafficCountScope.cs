namespace Shared.Utility.Counting
{
    /// <summary>
    /// Author: KienLT
    /// Description: Ba phạm vi tổng hợp của số đếm. Đặt hằng ở đây để VDS và WebAPI dùng CÙNG một bộ mã,
    /// không mỗi bên khai một chuỗi.
    /// Created date: 04/08/2026
    /// </summary>
    public static class TrafficCountScopes
    {
        public const string Equipment = "equipment";
        public const string Direction = "direction";
        public const string Zone = "zone";
    }

    /// <summary>
    /// Phạm vi tổng hợp KHÔNG kèm mốc thời gian: gộp theo camera, theo hướng tuyến, hay theo vùng.
    /// <c>Direction</c> đi kèm để phạm vi camera vẫn biết mình thuộc hướng nào mà không phải tra lại danh mục.
    /// </summary>
    public readonly record struct TrafficCountScope(string ScopeType, string ScopeId, string? Direction);
}
