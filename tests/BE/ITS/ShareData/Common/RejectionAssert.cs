using Furion.FriendlyException;

namespace Tests.ShareData.Common;

/// <summary>
/// Author: DatHQ
/// Description: Kiểm thao tác bị từ chối đúng mã lỗi và câu dịch của mã đó hiện được ở cả hai đường:
///              máy chủ đã nạp câu dịch thì Oops.Oh gọi string.Format, chưa nạp thì màn hình tự dịch không có giá trị.
/// Created date: 05/11/2026
/// </summary>
internal static class RejectionAssert
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Lỗi mang đúng mã, mã có câu dịch ở tệp seed và câu dịch qua được string.Format.
    ///              Lỗi nối đuôi không được có {0}: màn hình tự dịch không có giá trị thì hiện trống như "[] > @".
    /// Created date: 05/11/2026
    /// </summary>
    public static void Rejected(Exception? ex, string errorKey)
    {
        Assert.NotNull(ex);
        var carriesKey = ex.ToString().Contains(errorKey) || (ex is AppFriendlyException afe && afe.ErrorMessage?.ToString() == errorKey);
        Assert.True(carriesKey, $"Kỳ vọng từ chối với mã {errorKey}, nhận được: {ex}");

        var (vi, en) = SeedScriptReader.ReadTerminology(errorKey);
        _ = string.Format(vi, "a", "b", "c");
        _ = string.Format(en, "a", "b", "c");

        if (errorKey.Contains("sqlIncremental", StringComparison.Ordinal))
        {
            Assert.DoesNotMatch(@"\{\d\}", vi);
            Assert.DoesNotMatch(@"\{\d\}", en);
        }
    }
}
