namespace ShareDataWorker.Core.Models.DataOutbound
{
    /// <summary>
    /// Kết quả xử lý và ánh xạ dữ liệu (Data Mapping) - cô lập lỗi trước khi xuất bản
    /// Created date: 16/09/2026
    /// </summary>
    public sealed record DataMappingResult(
        bool Success,
        byte[]? FinalBytes,
        int RecordCount,
        string? ErrorMessage = null);
}

