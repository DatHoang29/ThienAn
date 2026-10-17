namespace Tests.ShareData.Common;

using System.Reflection;
using System.Text.RegularExpressions;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Utils;
using Module.ShareData.Infrastructure.Persistence.SeedData;
using Module.ShareData.Infrastructure.Services.ShareDataExtraction;
using Shared.DTO.Enums;
using Shared.Infrastructure.Persistence.SqlSugar;
using Xunit;

/// <summary>
/// Author: DatHQ
/// Description: Bộ kiểm thử khóa không cần CSDL cho ShareDataPacketSeedData: kiểm tra toàn vẹn
///              11 gói tin khởi tạo, danh sách ID đóng băng, SyncMode, trạng thái gói,
///              đối chiếu với LegacyPacketSql và kịch bản 02-sharedata-seed-goi-tin.sql.
/// Created date: 16/10/2026
/// </summary>
public class ShareDataPacketSeedDataTests
{
    private static readonly string[] FrozenIds =
    [
        "1500000000101",
        "1500000000102",
        "1500000000103",
        "1500000000104",
        "1500000000105",
        "1500000000106",
        "1500000000107",
        "1500000000108",
        "1500000000109",
        "1500000000110",
        "1500000000111"
    ];

    private static readonly string[] ExpectedPacketCodes =
    [
        PacketCodeConst.CommonData,
        PacketCodeConst.CctvData,
        PacketCodeConst.VdsData,
        PacketCodeConst.WeatherData,
        PacketCodeConst.RfidData,
        PacketCodeConst.WimData,
        PacketCodeConst.IncidentData,
        PacketCodeConst.VmsInfo,
        PacketCodeConst.EtcData,
        PacketCodeConst.WpData,
        PacketCodeConst.InterCenter
    ];

    [Fact]
    public void SeedData_ImplementsInterface_AndHasParameterlessConstructor_Test()
    {
        var type = typeof(ShareDataPacketSeedData);
        Assert.True(type.IsPublic, "Lớp ShareDataPacketSeedData phải là public.");
        Assert.True(
            typeof(ISqlSugarEntitySeedData<ShareDataPacket>).IsAssignableFrom(type),
            "Lớp ShareDataPacketSeedData phải triển khai ISqlSugarEntitySeedData<ShareDataPacket>.");

        var ctor = type.GetConstructor(Type.EmptyTypes);
        Assert.NotNull(ctor);
    }

    [Fact]
    public void HasData_ReturnsElevenUniquePackets_WithFrozenIdsAndCodes_Test()
    {
        var seeder = new ShareDataPacketSeedData();
        var packets = seeder.HasData().ToList();

        Assert.Equal(11, packets.Count);

        var ids = packets.Select(p => p.ID).ToList();
        Assert.Equal(11, ids.Distinct().Count());
        Assert.Equal(FrozenIds, ids);

        var codes = packets.Select(p => p.Code).ToList();
        Assert.Equal(11, codes.Distinct().Count());
        Assert.Equal(ExpectedPacketCodes, codes);

        for (var i = 0; i < packets.Count; i++)
        {
            var p = packets[i];
            Assert.Equal(101 + i, p.OrderNo);
            Assert.Equal("1.0", p.PacketVersion);
            Assert.False(string.IsNullOrWhiteSpace(p.Name));
            Assert.False(string.IsNullOrWhiteSpace(p.Description));
        }
    }

    [Fact]
    public void PacketCodeConst_ModuleAndWorker_AreIdentical_Test()
    {
        var moduleType = typeof(global::Module.ShareData.Core.Constants.PacketCodeConst);
        var workerType = typeof(ShareDataWorker.Core.Constants.PacketCodeConst);

        var moduleFields = moduleType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);

        var workerFields = workerType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly)
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);

        Assert.Equal(moduleFields.Count, workerFields.Count);
        foreach (var kvp in moduleFields)
        {
            Assert.True(workerFields.ContainsKey(kvp.Key), $"Worker thiếu hằng {kvp.Key}.");
            Assert.Equal(kvp.Value, workerFields[kvp.Key]);
        }

        var moduleShort = typeof(global::Module.ShareData.Core.Constants.PacketCodeConst.Short)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);
        var workerShort = typeof(ShareDataWorker.Core.Constants.PacketCodeConst.Short)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .ToDictionary(f => f.Name, f => (string)f.GetValue(null)!);

        Assert.Equal(moduleShort.Count, workerShort.Count);
        foreach (var kvp in moduleShort)
        {
            Assert.True(workerShort.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, workerShort[kvp.Key]);
        }
    }

    [Fact]
    public void SyncMode_AndStatus_MatchBusinessSpecification_Test()
    {
        var packets = new ShareDataPacketSeedData().HasData().ToDictionary(p => p.OrderNo ?? 0);

        // Snapshot packets: 101, 102, 105, 108, 110
        int[] snapshotPackets = [101, 102, 105, 108, 110];
        foreach (var order in snapshotPackets)
        {
            var p = packets[order];
            Assert.Equal(BaseEnums.SyncMode.Snapshot, p.SyncMode);
            Assert.Null(p.CursorTimeColumn);
            Assert.Null(p.MaxLookBackDays);
            Assert.Equal(BaseEnums.StatusEnum.Enable, p.Status);
        }

        // Incremental packets: 103, 104, 106, 107, 109
        int[] incrementalPackets = [103, 104, 106, 107, 109];
        foreach (var order in incrementalPackets)
        {
            var p = packets[order];
            Assert.Equal(BaseEnums.SyncMode.Incremental, p.SyncMode);
            Assert.Equal("UpdateTime", p.CursorTimeColumn);
            Assert.Equal(1, p.MaxLookBackDays);
            Assert.Equal(BaseEnums.StatusEnum.Enable, p.Status);
        }

        // Gói 111: Tạm thời vô hiệu hoá (Disable)
        var p111 = packets[111];
        Assert.Null(p111.ExtractSql);
        Assert.Null(p111.SyncMode);
        Assert.Null(p111.CursorTimeColumn);
        Assert.Null(p111.MaxLookBackDays);
        Assert.Equal(BaseEnums.StatusEnum.Disable, p111.Status);
    }

    [Fact]
    public void ExtractSql_MatchesLegacy_And_SeedScript02_Test()
    {
        // 101 - 109 đối chiếu với LegacyPacketSql
        Assert.True(PacketDefaultTemplate.TryGet("101", out var tpl101));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet101, tpl101.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("102", out var tpl102));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet102, tpl102.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("103", out var tpl103));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet103, tpl103.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("104", out var tpl104));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet104, tpl104.ExtractSql!);

        // 105: Ngoại lệ bỏ trần cứng TOP (@snapshotTop)
        var legacy105WithoutTop = LegacyPacketSql.Packet105.Replace(
            "TOP (@snapshotTop)",
            string.Empty,
            StringComparison.OrdinalIgnoreCase);
        Assert.True(PacketDefaultTemplate.TryGet("105", out var tpl105));
        AssertEqualNormalizedSql(legacy105WithoutTop, tpl105.ExtractSql!);

        Assert.True(PacketDefaultTemplate.TryGet("106", out var tpl106));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet106, tpl106.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("107", out var tpl107));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet107, tpl107.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("108", out var tpl108));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet108, tpl108.ExtractSql!);
        Assert.True(PacketDefaultTemplate.TryGet("109", out var tpl109));
        AssertEqualNormalizedSql(LegacyPacketSql.Packet109, tpl109.ExtractSql!);

        // 110: đối chiếu với script 02
        var script110Sql = SeedScriptReader.GetExtractSql("110");
        Assert.True(PacketDefaultTemplate.TryGet("110", out var tpl110));
        AssertEqualNormalizedSql(script110Sql, tpl110.ExtractSql!);

        // 111: không có template
        Assert.False(PacketDefaultTemplate.TryGet("111", out _));
    }

    [Fact]
    public void SeedData_MatchesScript02_UpdateAndInsert_Test()
    {
        var seedPackets = new ShareDataPacketSeedData().HasData().ToList();
        var scriptUpdates = SeedScriptReader.ReadAll();

        // 1. Kiểm tra đối chiếu với khối UPDATE của 101-110 trong script 02
        foreach (var p in seedPackets.Where(p => p.OrderNo <= 110))
        {
            var codeNumber = (p.OrderNo ?? 0).ToString();
            Assert.True(scriptUpdates.ContainsKey(codeNumber), $"Script 02 thiếu cấu hình gói {codeNumber}.");
            var updateInfo = scriptUpdates[codeNumber];

            AssertEqualNormalizedSql(updateInfo.ExtractSql, p.ExtractSql!);
            Assert.Equal((int?)p.SyncMode, updateInfo.SyncMode);
            Assert.Equal(p.CursorTimeColumn, updateInfo.CursorTimeColumn);
            Assert.Equal(p.MaxLookBackDays, updateInfo.MaxLookBackDays);
        }

        // 2. Kiểm tra đối chiếu với khối INSERT của 11 gói trong script 02
        var insertedPackets = SeedScriptReader.ReadInsertedPackets();
        Assert.Equal(11, insertedPackets.Count);

        for (var i = 0; i < seedPackets.Count; i++)
        {
            var sp = seedPackets[i];
            var ip = insertedPackets[i];

            Assert.Equal(sp.ID, ip.Id);
            Assert.Equal(sp.Code, ip.Code);
            Assert.Equal(sp.Name, ip.Name);
            Assert.Equal(sp.PacketVersion, ip.PacketVersion);
            Assert.Equal(sp.Description, ip.Description);
            Assert.Equal(sp.OrderNo, ip.OrderNo);
            Assert.Equal((int)sp.Status!, ip.Status);
        }
    }

    [Fact]
    public void ParityComparator_FailsWhenSingleCharacterDiffers_NegativeTest()
    {
        Assert.True(PacketDefaultTemplate.TryGet("101", out var tpl101));
        var originalSql = tpl101.ExtractSql!;
        var tamperedSql = originalSql.Replace("zoneStatusId", "zoneStatusId_tampered");

        Assert.NotEqual(
            NormalizeSql(originalSql),
            NormalizeSql(tamperedSql));
    }

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
    public void SeedSql_Passes_SqlValidator_WithoutDatabase_Test(string packetCode)
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

    [Theory]
    [InlineData("103")]
    [InlineData("104")]
    [InlineData("106")]
    [InlineData("107")]
    [InlineData("109")]
    public void IncrementalSeedSql_Passes_IncrementalStructureValidation_Test(string packetCode)
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

    private static void AssertEqualNormalizedSql(string expected, string actual)
    {
        var normExpected = NormalizeSql(expected);
        var normActual = NormalizeSql(actual);
        Assert.Equal(normExpected, normActual);
    }

    private static string NormalizeSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        return Regex.Replace(sql, @"\s+", " ", RegexOptions.Singleline).Trim();
    }
}
