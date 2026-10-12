namespace Tests.ShareData.Configuration;

using System.Reflection;
using System.Text.RegularExpressions;
using ShareDataWorker.Infrastructure.Logging;
using Xunit;

public class ShareDataAlertCodeTests
{
    [Fact]
    [Trait("Feature", "BE-7")]
    public void ShareDataAlertCode_AllConstants_MustMatchRegex_NoDuplicates_AndTotal26()
    {
        // Arrange
        var nestedTypes = typeof(ShareDataAlertCode).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
        var codes = new List<(string Group, string FieldName, string Value)>();

        // Act: Lấy mọi const string trong ShareDataAlertCode.{Outbound, Inbound, Tracking} (trừ Tracking.Prefix)
        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                if (type == typeof(ShareDataAlertCode.Tracking) && field.Name == nameof(ShareDataAlertCode.Tracking.Prefix))
                {
                    continue;
                }

                var val = field.GetValue(null) as string;
                if (!string.IsNullOrEmpty(val))
                {
                    codes.Add((type.Name, field.Name, val));
                }
            }
        }

        // Assert: 1. Tổng số mã lỗi đúng 26 (13 Outbound + 9 Inbound + 4 Tracking)
        Assert.Equal(26, codes.Count);

        // Assert: 2. Không có mã lỗi nào bị trùng lặp
        var distinctValues = codes.Select(c => c.Value).Distinct().ToList();
        Assert.Equal(codes.Count, distinctValues.Count);

        // Assert: 3. Mọi mã lỗi phải đúng định dạng ^ESH-\d{4}$
        var regex = new Regex(@"^ESH-\d{4}$");
        foreach (var item in codes)
        {
            Assert.Matches(regex, item.Value);
        }

        // Assert: 4. Phân bổ chính xác theo từng nhóm
        var outboundCount = codes.Count(c => c.Group == nameof(ShareDataAlertCode.Outbound));
        var inboundCount = codes.Count(c => c.Group == nameof(ShareDataAlertCode.Inbound));
        var trackingCount = codes.Count(c => c.Group == nameof(ShareDataAlertCode.Tracking));

        Assert.Equal(13, outboundCount);
        Assert.Equal(9, inboundCount);
        Assert.Equal(4, trackingCount);
    }
}
