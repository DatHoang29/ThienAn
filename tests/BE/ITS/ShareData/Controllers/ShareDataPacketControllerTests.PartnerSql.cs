using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Helpers;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Helpers;
using Shared.DTO.Enums;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
    private static ShareDataPacket NewPartnerSqlPacket(string packetCode, string unique) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        Code = packetCode,
        Name = $"Packet {unique}",
        Status = BaseEnums.StatusEnum.Enable
    };

    private static ShareDataPartner NewPartnerSqlPartner(string code, string unique) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        Code = code,
        Name = $"Partner {code} {unique}",
        Status = BaseEnums.StatusEnum.Enable
    };

    private static ShareDataSubscription NewPartnerSqlSubscription(ShareDataPartner partner, ShareDataPacket packet, BaseEnums.Direction direction, BaseEnums.SubSubscriptionState state) => new()
    {
        ID = Guid.NewGuid().ToString("N"),
        PartnerId = partner.ID,
        DatatypeId = packet.ID,
        Direction = direction,
        State = state
    };

    [Fact]
    public async Task GetPartnerSqlList_WhenNotSuperAdmin_ThrowsOopsException_Test()
    {
        // Arrange
        var normalAccount = $"user_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: false, account: normalAccount);

        using var scope = _host.Services.CreateScope();
        var input = new GetPartnerSqlListInput { PacketCode = "101" };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(input));
    }

    [Fact]
    public async Task GetPartnerSqlList_ListsSubscribedPartnersAndPartnersWithCustomSql_WithFlags_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_LST_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);

        // A: có đăng ký gửi, dùng SQL chung, có hồ sơ ánh xạ đang bật
        var partnerA = NewPartnerSqlPartner($"PT_A_{unique}", unique);
        // B: có đăng ký gửi, có SQL riêng, chưa có hồ sơ ánh xạ
        var partnerB = NewPartnerSqlPartner($"PT_B_{unique}", unique);
        // C: có đăng ký gửi, có SQL riêng khác mẫu chung
        var partnerC = NewPartnerSqlPartner($"PT_C_{unique}", unique);
        // D: chỉ có đăng ký nhận -> không được liệt kê
        var partnerD = NewPartnerSqlPartner($"PT_D_{unique}", unique);
        // E: đăng ký gửi đã hủy -> không được liệt kê
        var partnerE = NewPartnerSqlPartner($"PT_E_{unique}", unique);
        // F: không có đăng ký nhưng đã có SQL riêng -> vẫn liệt kê để quản lý
        var partnerF = NewPartnerSqlPartner($"PT_F_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(new[] { partnerA, partnerB, partnerC, partnerD, partnerE, partnerF }).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            NewPartnerSqlSubscription(partnerA, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerB, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Paused),
            NewPartnerSqlSubscription(partnerC, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerD, packet, BaseEnums.Direction.Inbound, BaseEnums.SubSubscriptionState.Active),
            NewPartnerSqlSubscription(partnerE, packet, BaseEnums.Direction.Outbound, BaseEnums.SubSubscriptionState.Cancelled),
        }).ExecuteCommandAsync();
        await db.Insertable(new[]
        {
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerB.ID, PacketId = packet.ID, ExtractSql = "SELECT ID, Code FROM TmsZone" },
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerC.ID, PacketId = packet.ID, ExtractSql = "SELECT ID, Name FROM TmsZone" },
            new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PartnerId = partnerF.ID, PacketId = packet.ID, ExtractSql = "SELECT ID FROM TmsZone" },
        }).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = packetCode });

        // Assert
        Assert.NotNull(result);
        var codes = result.Select(r => r.PartnerCode).ToList();
        Assert.Contains(partnerA.Code, codes);
        Assert.Contains(partnerB.Code, codes);
        Assert.Contains(partnerC.Code, codes);
        Assert.Contains(partnerF.Code, codes);
        Assert.DoesNotContain(partnerD.Code, codes);
        Assert.DoesNotContain(partnerE.Code, codes);

        var itemA = result.First(r => r.PartnerCode == partnerA.Code);
        Assert.False(itemA.IsCustom);

        var itemB = result.First(r => r.PartnerCode == partnerB.Code);
        Assert.True(itemB.IsCustom);

        var itemC = result.First(r => r.PartnerCode == partnerC.Code);
        Assert.True(itemC.IsCustom);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_FirstTime_CreatesRowWithoutMapping_AndSuperAdminRequired_Test()
    {
        // Arrange: chưa có hồ sơ ánh xạ nào, SQL riêng vẫn soạn trước được
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_UPD_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();

        var customSql = "SELECT ID, Code FROM TmsZone";

        // Act 1: Non-SuperAdmin call must throw
        SetUserContext(isSuperAdmin: false, account: "normal_user");
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        }));

        // Act 2: SuperAdmin lưu lần đầu -> tạo dòng SQL riêng
        SetUserContext(isSuperAdmin: true, account: superAdmin);
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        });

        // Assert
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.Equal(customSql, output.CustomSql);

        var rows = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).ToListAsync();
        Assert.Single(rows);
        Assert.Equal(customSql, rows[0].ExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenIdenticalToPacketStandard_StillKeepsPartnerRow_Test()
    {
        // Arrange: tạo SQL riêng giống hệt mẫu chuẩn -> vẫn là SQL riêng (bản đóng băng), không tự xóa
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EQ_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = "SELECT ID FROM TmsZone"
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act: lưu SQL trùng chuẩn (khác khoảng trắng)
        var outEq = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "  SELECT   ID,   Code,   Name   FROM   TmsZone  "
        });

        // Assert
        Assert.True(outEq.IsCustom);
        Assert.False(outEq.HasDifferences);

        var remaining = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).CountAsync();
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task ResetPartnerExtractSql_DeletesPartnerRow_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_RST_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode
        });

        // Assert
        Assert.False(result.IsCustom);
        Assert.Null(result.CustomSql);

        var remaining = await db.Queryable<ShareDataSql>().Where(s => s.ID == partnerSql.ID).CountAsync();
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_IncrementalWithoutStructure_ThrowsOops_Test()
    {
        // Arrange: Câu SQL nối đuôi nhưng thiếu __lastKey hoặc __lastTime
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_INC_ERR_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT TOP (@pageSize) ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert: SQL có @lastTime nhưng thiếu __lastKey, __lastTime, ORDER BY -> Throws
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone WHERE CreateTime > @lastTime"
        }));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenPartnerOrPacketNotExist_Throws_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_NOEX_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = Guid.NewGuid().ToString("N"),
            PacketCode = packetCode,
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        }));
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = $"NOT_EXIST_{unique}",
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        }));
    }

    [Fact]
    public async Task SqlTextHelper_SelectPartnerSqlAsync_ReturnsCustomRow_OrNullWhenUsingStandard_Test()
    {
        // Arrange: Worker ưu tiên câu SQL riêng của đối tác qua SqlTextHelper
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_HLP_{unique}";
        var stdSql = "SELECT TOP (@pageSize) ID FROM TmsZone";
        var customSql = "SELECT TOP (@pageSize) ID, Code, Name FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partnerCustom = NewPartnerSqlPartner($"PT_HLP_{unique}", unique);
        var partnerStd = NewPartnerSqlPartner($"PT_STD_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(new[] { partnerCustom, partnerStd }).ExecuteCommandAsync();
        await db.Insertable(new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerCustom.ID,
            PacketId = packet.ID,
            ExtractSql = customSql
        }).ExecuteCommandAsync();

        // Act
        var selectedCustom = await SqlTextHelper.SelectPartnerSqlAsync(db, partnerCustom.ID, packet);
        var selectedStd = await SqlTextHelper.SelectPartnerSqlAsync(db, partnerStd.ID, packet);

        // Assert
        Assert.NotNull(selectedCustom);
        Assert.Equal(customSql, selectedCustom.ExtractSql);
        Assert.True(SqlTextHelper.IsCustom(selectedCustom));

        // Đối tác không có dòng riêng -> Worker dùng câu chuẩn của gói, chính sách suy từ câu chuẩn
        Assert.Null(selectedStd);
        var effectiveSql = (await SqlTextHelper.ResolveSqlConfigAsync(db, packet, partnerStd.ID)).ExtractSql;
        Assert.Equal(stdSql, effectiveSql);
        Assert.Equal(ShareDataEnum.PacketPolicy.Snapshot, PacketHelper.GetPolicyFromSql("SELECT ID, Code FROM TmsZone"));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenEmptySql_ThrowsPartnerExtractSqlRequired_Test()
    {
        // Arrange: SQL rỗng -> ném lỗi PartnerExtractSqlRequired
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EMP_{unique}";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_EMP_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, "SELECT ID FROM TmsZone");
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = "   "
        }));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenDifferentIncrementalSql_KeepsCustomRow_Test()
    {
        // Arrange: SQL khác chuẩn và có @lastTime -> VẪN LƯU RIÊNG
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_DIFF_MODE_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";
        var customSql = "SELECT TOP (@pageSize) ID AS __lastKey, CreateTime AS __lastTime, Code FROM TmsZone WHERE CreateTime >= @lastTime ORDER BY ID ASC";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_DM_{unique}", unique);

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, stdSql);
        await db.Insertable(partner).ExecuteCommandAsync();

        // Act: Gửi câu SQL riêng nối đuôi
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packetCode,
            ExtractSql = customSql
        });

        // Assert: Lưu riêng, chế độ suy từ câu SQL riêng khác câu chuẩn
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.True(SqlValidator.IsIncrementalSql(output.CustomSql));
        Assert.False(SqlValidator.IsIncrementalSql(output.StandardSql));

        var inDb = await db.Queryable<ShareDataSql>().Where(s => s.PartnerId == partner.ID && s.PacketId == packet.ID).FirstAsync();
        Assert.NotNull(inDb);
        Assert.Equal(customSql, inDb.ExtractSql);
    }

    [Fact]
    public async Task UpdateShareDataMapping_DoesNotTouchPartnerCustomSqlRow_Test()
    {
        // Arrange: Màn hình Ánh xạ cập nhật hồ sơ không được ghi đè hay xóa SQL riêng của đối tác
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_MAP_UPD_{unique}";
        var customSql = "SELECT ID, Code FROM TmsZone";
        var baseSql = "SELECT ID FROM TmsZone";
        var packet = NewPartnerSqlPacket(packetCode, unique);
        var partner = NewPartnerSqlPartner($"PT_MAP_{unique}", unique);
        var targetShape = "{\"now\":{\"$meta\":\"Now\"},\"serial\":{\"$meta\":\"Serial\"},\"packetCode\":{\"$meta\":\"PacketCode\"},\"partnerCode\":{\"$meta\":\"PartnerCode\"}}";
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = $"MAP_{unique}",
            Name = $"Mapping {unique}",
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            TargetShapeJson = targetShape
        };
        var partnerSql = new ShareDataSql
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            PacketId = packet.ID,
            ExtractSql = customSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await ShareDataSqlSeed.SetCommonAsync(db, packet.ID, baseSql);
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();
        await db.Insertable(partnerSql).ExecuteCommandAsync();

        // Act: Cập nhật thông tin chung qua API Ánh xạ
        var updateInput = new ShareDataUpdateMappingInput
        {
            ID = mapping.ID,
            Code = mapping.Code,
            Name = $"Mapping Updated {unique}",
            PartnerId = mapping.PartnerId,
            DatatypeId = mapping.DatatypeId,
            Direction = mapping.Direction,
            TargetShapeJson = targetShape,
            IsActive = true
        };
        await scope.ServiceProvider.CallApiAsync<ShareDataMappingOutput>(updateInput);

        // Assert: dòng SQL riêng vẫn giữ nguyên 100%
        var mappingInDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal($"Mapping Updated {unique}", mappingInDb.Name);
        var sqlInDb = await db.Queryable<ShareDataSql>().InSingleAsync(partnerSql.ID);
        Assert.NotNull(sqlInDb);
        Assert.Equal(customSql, sqlInDb.ExtractSql);
    }

    [Fact]
    public void SqlTextHelper_Normalize_CleansWhitespaceAndTrailingSemicolons_Correctly_Test()
    {
        // Assert: Kiểm tra đầy đủ các kịch bản chuẩn hóa
        Assert.Equal(string.Empty, SqlTextHelper.Normalize(null));
        Assert.Equal(string.Empty, SqlTextHelper.Normalize(string.Empty));
        Assert.Equal(string.Empty, SqlTextHelper.Normalize("   \r\n\t  "));
        Assert.Equal(string.Empty, SqlTextHelper.Normalize(";;;;"));
        Assert.Equal(string.Empty, SqlTextHelper.Normalize("   ; ; ; ;   "));
        Assert.Equal("SELECT a, b FROM table", SqlTextHelper.Normalize("  SELECT   a,   b \r\n FROM table;;; "));
        Assert.Equal("SELECT 1; SELECT 2", SqlTextHelper.Normalize("SELECT 1; SELECT 2;;"));
    }

    [Fact]
    public void SqlTextHelper_HasDifferences_Behaviors_Test()
    {
        var packet = "SELECT ID, Code FROM TmsZone";

        // 1. Không có dòng SQL riêng
        Assert.False(SqlTextHelper.IsCustom(null));
        Assert.False(SqlTextHelper.HasDifferences(null, packet));

        // 2. Dòng không có ExtractSql
        var stdSql = new ShareDataSql { ExtractSql = null };
        Assert.False(SqlTextHelper.IsCustom(stdSql));
        Assert.False(SqlTextHelper.HasDifferences(stdSql, packet));

        // 3. ExtractSql trùng chuẩn (khác khoảng trắng, dấu chấm phẩy cuối)
        var sameSql = new ShareDataSql { ExtractSql = "  SELECT   ID,   Code   FROM   TmsZone;  " };
        Assert.True(SqlTextHelper.IsCustom(sameSql));
        Assert.False(SqlTextHelper.HasDifferences(sameSql, packet));

        // 4. ExtractSql khác chuẩn
        var customSql = new ShareDataSql { ExtractSql = "SELECT ID FROM TmsZone" };
        Assert.True(SqlTextHelper.IsCustom(customSql));
        Assert.True(SqlTextHelper.HasDifferences(customSql, packet));
    }

    [Theory]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE Time > @lastTime", true)]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE Time > @LASTTIME", true)]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE ID > @lastKey", true)]
    [InlineData("SELECT TOP (@pageSize) ID AS __lastKey, Time AS __lastTime FROM T WHERE ID > @LASTKEY", true)]
    [InlineData("SELECT ID, Code, Name FROM TmsZone", false)]
    [InlineData("SELECT ID, Code FROM TmsZone -- WHERE Time > @lastTime", false)]
    [InlineData("SELECT ID, Code FROM TmsZone /* @lastKey in comment */", false)]
    [InlineData("SELECT ID, '@lastTime' AS DummyText FROM TmsZone", false)]
    [InlineData("SELECT ID, '@lastKey' AS DummyText FROM TmsZone", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    [InlineData("SELECT FROM WHERE INVALID SQL @lastTime ???", false)]
    public void SqlValidator_IsIncrementalSql_RecognizesIncrementalCorrectly_Test(string? sql, bool expected)
    {
        var result = SqlValidator.IsIncrementalSql(sql);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Worker_PolicyResolution_FollowsEffectiveSql_Test()
    {
        // 1. Gói Toàn bộ (Snapshot) + Đối tác có SQL riêng nối đuôi -> Worker nhận AlwaysIncremental
        var incrementalCustomSql = "SELECT TOP (@pageSize) ID AS __lastKey, CreateTime AS __lastTime FROM TmsZone WHERE CreateTime > @lastTime ORDER BY CreateTime ASC";

        var policyForPartner1 = PacketHelper.GetPolicyFromSql(incrementalCustomSql);
        Assert.Equal(ShareDataEnum.PacketPolicy.AlwaysIncremental, policyForPartner1);

        // 2. Gói Nối đuôi (Incremental) + Đối tác có SQL riêng toàn bộ -> Worker nhận Snapshot
        var snapshotCustomSql = "SELECT ID, Code FROM TmsZone";

        var policyForPartner2 = PacketHelper.GetPolicyFromSql(snapshotCustomSql);
        Assert.Equal(ShareDataEnum.PacketPolicy.Snapshot, policyForPartner2);
    }
}
