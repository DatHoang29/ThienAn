namespace Shared.QuestHa.Helper;

/// <summary>
/// Author: KienLT
/// Description: Lớp cha đăng ký helper dùng chung của Shared.QuestHa theo mẫu <c>BaseHelper</c> (CODING_RULES §4):
/// <c>QuestHaBaseHelper.ErrorHp.Classify(ex)</c>, <c>QuestHaBaseHelper.PayloadHp.ToRow(...)</c>…
/// Created date: 27/10/2026
/// </summary>
internal class QuestHaBaseHelper
{
    /// <summary>
    /// Author: KienLT
    /// Description: Phân loại lỗi QuestDB (kết nối / node / dữ liệu).
    /// Created date: 27/10/2026
    /// </summary>
    internal class ErrorHp : QuestErrorClassifierHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Chuyển payload journal ↔ dòng QuestDB, khoá, version, fingerprint.
    /// Created date: 27/10/2026
    /// </summary>
    internal class PayloadHp : QuestPayloadHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Gộp dòng cùng khoá trong lô trước khi ghi node.
    /// Created date: 27/10/2026
    /// </summary>
    internal class BatchMergeHp : QuestBatchMergeHelper
    {
    }

    /// <summary>
    /// Author: KienLT
    /// Description: Đọc số dòng trong metadata file Parquet và băm SHA-256 file xuất.
    /// Created date: 02/11/2026
    /// </summary>
    internal class ParquetHp : QuestParquetHelper
    {
    }
}
