namespace ShareDataWorker.Core.Models.DataOutbound
{
    /// <summary>
    /// Description: Kết quả trích xuất dữ liệu thô từ CSDL kèm mốc khóa dòng cuối (LastKey).
    ///              LastKeyValue là giá trị __lastKey dòng cuối giữ nguyên kiểu (số, GUID…) để chốt chặn ghi mốc so đúng kiểu như câu SQL.
    ///              Groups: mốc của từng nhóm mốc có tên lấy ở dòng cuối có __lastKey_&lt;nhóm&gt; của trang (nhóm không có dòng nào thì vắng).
    ///              DuplicatedKey: giá trị cột mốc bị trùng giữa nhiều dòng (trong trang hoặc giữa dòng cuối trang với dòng đầu trang kế); có giá trị thì không được gửi vì trang kế sẽ bỏ sót dòng.
    /// Created date: 16/09/2026
    /// Modified date: 05/11/2026
    /// </summary>
    public sealed record DataOutboundExtractionResult(
        List<object> RawRows,
        string? LastKey,
        object? LastKeyValue = null,
        IReadOnlyDictionary<string, CursorGroupPosition>? Groups = null,
        string? DuplicatedKey = null);

    /// <summary>
    /// Description: Mốc của một nhóm mốc có tên ở dòng cuối của nhóm trong trang: __lastKey_&lt;nhóm&gt; dạng chuỗi và giữ nguyên kiểu.
    /// Created date: 29/10/2026
    /// Modified date: 03/11/2026
    /// </summary>
    public sealed record CursorGroupPosition(string LastKey, object? LastKeyValue);
}
