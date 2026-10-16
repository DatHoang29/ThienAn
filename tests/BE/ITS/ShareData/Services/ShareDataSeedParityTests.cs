namespace Tests.ShareData.Services;

using System.Text.RegularExpressions;
using Module.ShareData.Core.Utils;
using Tests.ShareData.Common;
using Xunit;

/// <summary>
/// Description: Bộ kiểm thử đối chiếu tính tương đồng (Parity) giữa câu lệnh SQL trích xuất
///              được nạp từ kịch bản seed SQL (02-sharedata-seed-goi-tin.sql) với mã SQL hardcode gốc
///              tại commit 56bb4bc3 (LegacyPacketSql) của các gói tin ShareData 101-109.
/// </summary>
public class ShareDataSeedParityTests
{
    private static string NormalizeSql(string sql) => Regex.Replace(sql, @"\s+", " ").Trim();

    /// <summary>
    /// Đối chiếu chuỗi SQL trích xuất giữa bản seed và bản hardcode cũ (101..109).
    /// </summary>
    [Theory]
    [InlineData("101")]
    [InlineData("102")]
    [InlineData("103")]
    [InlineData("104")]
    [InlineData("105")]
    [InlineData("106")]
    [InlineData("107")]
    [InlineData("108")]
    [InlineData("109")]
    public void SeedSql_Matches_LegacyExtractionSql_Parity(string packetCode)
    {
        // Arrange
        var legacySql = LegacyPacketSql.GetLegacySql(packetCode);

        // Ngoại lệ DUY NHẤT: Gói 105 — trước khi chuẩn hóa và so sánh, loại bỏ mệnh đề "TOP (@snapshotTop)" ở bản cũ (LegacyPacketSql);
        // Quyết định bỏ trần cứng gói Snapshot theo phê duyệt chủ dự án.
        if (packetCode == "105")
        {
            legacySql = legacySql.Replace("TOP (@snapshotTop)", string.Empty);
        }

        var seedSql = SeedScriptReader.GetExtractSql(packetCode);

        // Act
        var normalizedLegacy = NormalizeSql(legacySql);
        var normalizedSeed = NormalizeSql(seedSql);

        // Assert
        Assert.Equal(normalizedLegacy, normalizedSeed, ignoreCase: true);
    }

    /// <summary>
    /// Kiểm tra các gói Snapshot (101, 102, 105, 108) có SyncMode == 1.
    /// </summary>
    [Theory]
    [InlineData("101", 1)]
    [InlineData("102", 1)]
    [InlineData("105", 1)]
    [InlineData("108", 1)]
    public void SeedSyncMode_SnapshotPackets_HaveSyncMode1(string packetCode, int expectedSyncMode)
    {
        // Act
        var syncMode = SeedScriptReader.GetSyncMode(packetCode);

        // Assert
        Assert.Equal(expectedSyncMode, syncMode);
    }

    /// <summary>
    /// Kiểm tra các gói Incremental (103, 104, 106, 107, 109) có SyncMode == 2.
    /// </summary>
    [Theory]
    [InlineData("103", 2)]
    [InlineData("104", 2)]
    [InlineData("106", 2)]
    [InlineData("107", 2)]
    [InlineData("109", 2)]
    public void SeedSyncMode_IncrementalPackets_HaveSyncMode2(string packetCode, int expectedSyncMode)
    {
        // Act
        var syncMode = SeedScriptReader.GetSyncMode(packetCode);

        // Assert
        Assert.Equal(expectedSyncMode, syncMode);
    }

    /// <summary>
    /// Negative test: Hai chuỗi SQL chỉ khác nhau 1 alias hoặc 1 cột thì Assert.NotEqual
    /// (chứng minh test so sánh hoạt động nhạy bén và không báo pass giả).
    /// </summary>
    [Fact]
    public void ParityCheck_DifferentAliasOrColumn_AssertsNotEqual()
    {
        // Arrange
        var baseSql = SeedScriptReader.GetExtractSql("101");
        var modifiedAliasSql = baseSql.Replace("zoneStatusId", "zoneStatusIdentifier");
        var modifiedColumnSql = baseSql.Replace("zs.UpdateTime AS dataTime,", string.Empty);

        // Act
        var normalizedBase = NormalizeSql(baseSql);
        var normalizedModifiedAlias = NormalizeSql(modifiedAliasSql);
        var normalizedModifiedColumn = NormalizeSql(modifiedColumnSql);

        // Assert
        Assert.NotEqual(normalizedBase, normalizedModifiedAlias, StringComparer.OrdinalIgnoreCase);
        Assert.NotEqual(normalizedBase, normalizedModifiedColumn, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kiểm tra SqlValidator.ValidateAndExtractColumns trên seed SQL 101-109 mà không cần schema DB,
    /// xác nhận hợp lệ và trả về danh sách cột alias không rỗng.
    /// </summary>
    [Theory]
    [InlineData("101")]
    [InlineData("102")]
    [InlineData("103")]
    [InlineData("104")]
    [InlineData("105")]
    [InlineData("106")]
    [InlineData("107")]
    [InlineData("108")]
    [InlineData("109")]
    public void SeedSql_Passes_SqlValidator_WithoutDatabase(string packetCode)
    {
        // Arrange
        var seedSql = SeedScriptReader.GetExtractSql(packetCode);

        // Act
        var (isValid, errorMessage, columns) = SqlValidator.ValidateAndExtractColumns(null, seedSql);

        // Assert
        Assert.True(isValid, $"Gói {packetCode} không hợp lệ: {errorMessage}");
        Assert.Null(errorMessage);
        Assert.NotNull(columns);
        Assert.NotEmpty(columns);
    }

    /// <summary>
    /// Kiểm tra cấu trúc câu lệnh phân trang Incremental (TOP @pageSize, ORDER BY ASC, __lastTime, __rowid)
    /// cho các gói đồng bộ nối đuôi.
    /// </summary>
    [Theory]
    [InlineData("103")]
    [InlineData("104")]
    [InlineData("106")]
    [InlineData("107")]
    [InlineData("109")]
    public void IncrementalSeedSql_Passes_IncrementalStructureValidation(string packetCode)
    {
        // Arrange
        var seedSql = SeedScriptReader.GetExtractSql(packetCode);
        var (_, _, columns) = SqlValidator.ValidateAndExtractColumns(null, seedSql);

        // Act
        var (isValid, errorMessage) = SqlValidator.ValidateIncrementalStructure(seedSql, columns);

        // Assert
        Assert.True(isValid, $"Gói {packetCode} không hợp lệ cấu trúc Incremental: {errorMessage}");
        Assert.Null(errorMessage);
    }
}
