using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Controllers;

/// <summary>
/// Description: Kiểm thử bảng tham số câu truy vấn của gói tin (GetSqlVariables / UpdateSqlVariables) — mẫu chung, ghi đè và biến riêng của đối tác
/// Created date: 24/10/2026
/// </summary>
public partial class ShareDataPacketControllerTests
{
    private async Task<(ShareDataPacket Packet, ShareDataPartner Partner)> SeedSqlVariablePacketAsync(ISqlSugarClient db, string unique)
    {
        var packet = NewPartnerSqlPacket($"PKT_VAR_{unique}", unique);
        var partner = NewPartnerSqlPartner($"PT_VAR_{unique}", unique);
        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        return (packet, partner);
    }

    private static PacketSqlVariableItemInput ConstVar(string code, string dataType, string value) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindConst, Value = value };

    private static PacketSqlVariableItemInput BuiltinVar(string code, string dataType, string source) =>
        new() { Code = code, DataType = dataType, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source };

    /// <summary>
    /// Description: Lưu mẫu chung rồi đọc lại: đúng thứ tự, hằng số ngày giờ được chuẩn hóa, nguồn hệ thống giữ mã nguồn
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenCommonScope_SavesAndNormalizesValues_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        var saved = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items =
            [
                ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500"),
                ConstVar("@fromDate", ShareDataConst.SqlVariableTypeDateTime, "2027-01-01"),
                BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, "startofyear"),
                ConstVar("rate", "decimal", "-12.50"),
                ConstVar("isActive", ShareDataConst.SqlVariableTypeBool, "1"),
                ConstVar("fromDay", ShareDataConst.SqlVariableTypeDate, "2027-03-05 08:30:00")
            ]
        });

        Assert.Equal(["pageSize", "fromDate", "yearStart", "rate", "isActive", "fromDay"], saved.Items.Select(i => i.Code).ToArray());
        Assert.All(saved.Items, i => Assert.Equal(ShareDataConst.SqlVariableScopeCommon, i.Scope));
        Assert.Equal("2027-01-01 00:00:00", saved.Items[1].Value);
        Assert.Equal("startOfYear", saved.Items[2].Value);
        Assert.Equal(ShareDataConst.SqlVariableTypeDecimal, saved.Items[3].DataType);
        Assert.Equal("-12.50", saved.Items[3].Value);
        Assert.Equal("true", saved.Items[4].Value);
        Assert.Equal("2027-03-05", saved.Items[5].Value);

        var read = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(6, read.Items.Count);
    }

    /// <summary>
    /// Description: Biến đi theo câu SQL. Tạo SQL riêng thì chép bộ biến mẫu chung sang làm bộ biến của đối tác; sửa bộ biến của đối tác
    ///              không đụng mẫu chung (và ngược lại); "Dùng lại mẫu chung" thì xóa bộ biến của đối tác.
    /// Created date: 24/10/2026
    /// Modified date: 25/10/2026 (Biến đi theo câu SQL thay cho ghi đè)
    /// </summary>
    [Fact]
    public async Task PartnerSqlVariables_FollowPartnerSql_CopiedOnCreateIndependentAndRemovedOnReset_Test()
    {
        // Arrange: mẫu chung có biến @yearStart
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
        });

        // Act 1: tạo SQL riêng dùng @yearStart (chép từ mẫu chung)
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @yearStart"
        });

        // Assert 1: đối tác có bộ biến riêng chép từ mẫu chung
        var partnerView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
        Assert.Equal(ShareDataConst.SqlVariableSourceStartOfYear, Assert.Single(partnerView.Items, i => i.Code == "yearStart").Value);
        Assert.Equal(1, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID && v.PartnerId == partner.ID).CountAsync());

        // Act 2: đổi bộ biến của đối tác sang hằng số
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items = [ConstVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, "2027-01-01")]
        });

        // Assert 2: mẫu chung giữ nguyên nguồn "Đầu năm"
        var common = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal(ShareDataConst.SqlVariableSourceStartOfYear, Assert.Single(common.Items).Value);

        // Act 3: đối tác xóa hết biến rồi lưu lại SQL riêng không còn dùng biến
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID, Items = [] });
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            PartnerId = partner.ID!,
            PacketCode = packet.Code!,
            ExtractSql = "SELECT ID AS zoneId FROM TmsZone"
        });

        // Assert 3: bộ biến của đối tác vẫn rỗng — không hiện lại biến mẫu chung, không chép lại khi lưu SQL riêng
        var emptyView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code!, PartnerId = partner.ID });
        Assert.Empty(emptyView.Items);

        // Act 4: dùng lại mẫu chung
        await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code! });

        // Assert 4: bộ biến của đối tác bị xóa
        Assert.Equal(0, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID && v.PartnerId == partner.ID).CountAsync());
    }

    /// <summary>
    /// Description: Xóa dòng ở mẫu chung rồi khai lại cùng tên không bị chặn bởi chỉ mục duy nhất
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenRemovedThenRedeclared_SucceedsWithoutUniqueConflict_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        var input = new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500")] };
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code!, Items = [] });
        var again = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(input);

        Assert.Single(again.Items);
    }

    /// <summary>
    /// Description: Các dòng không hợp lệ đều bị từ chối: tên sai, trùng tên, nguồn hệ thống sai kiểu, hằng số sai kiểu, nguồn lạ
    /// Created date: 24/10/2026
    /// </summary>
    [Theory]
    [InlineData("1abc", "Int", "Const", "5")]
    [InlineData("from date", "DateTime", "Const", "2027-01-01")]
    [InlineData("lastKey", "DateTime", "Builtin", "lastKey")]
    [InlineData("pageSize", "Int", "Const", "abc")]
    [InlineData("fromDate", "DateTime", "Const", "01/02/2027")]
    [InlineData("yearStart", "DateTime", "Builtin", "tenant")]
    [InlineData("pageSize", "Guid", "Const", "5")]
    [InlineData("rate", "Decimal", "Const", "1,5")]
    [InlineData("isActive", "Bool", "Const", "yes")]
    [InlineData("fromDay", "Date", "Const", "2027-13-01")]
    public async Task UpdateSqlVariables_WhenItemInvalid_ThrowsAndSavesNothing_Test(string code, string dataType, string valueKind, string value)
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [new PacketSqlVariableItemInput { Code = code, DataType = dataType, ValueKind = valueKind, Value = value }]
        }));

        Assert.Equal(0, await db.Queryable<ShareDataSqlVariable>().Where(v => v.PacketId == packet.ID).CountAsync());
    }

    /// <summary>
    /// Description: Hai dòng trùng tên (không phân biệt hoa thường) bị từ chối
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenDuplicateCodes_Throws_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [ConstVar("pageSize", "Int", "5"), ConstVar("PAGESIZE", "Int", "6")]
        }));
    }

    /// <summary>
    /// Description: Không phải SuperAdmin thì không xem và không lưu được bảng tham số
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task SqlVariables_WhenNotSuperAdmin_Throws_Test()
    {
        SetUserContext(isSuperAdmin: false, account: $"user_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! }));
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packet.Code! }));
    }

    /// <summary>
    /// Description: Lưu câu SQL (mẫu chung hoặc SQL riêng của đối tác) có dùng biến @yearStart: chỉ lưu được khi biến đã khai ở phạm vi mà câu SQL nhìn thấy.
    ///              Mẫu chung chỉ thấy biến của mẫu chung; SQL riêng thấy biến của mẫu chung và biến riêng của đối tác đó.
    /// Created date: 25/10/2026
    /// </summary>
    [Theory]
    [InlineData("common", "common", true)]
    [InlineData("common", "partner", false)]
    [InlineData("common", "none", false)]
    [InlineData("partner", "common", true)]
    [InlineData("partner", "partner", true)]
    [InlineData("partner", "none", false)]
    public async Task SaveExtractSql_WhenSqlUsesVariable_AcceptsOnlyDeclaredVariables_Test(string sqlScope, string declaredAt, bool expectSaved)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);
        if (declaredAt != "none")
        {
            await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
            {
                PacketCode = packet.Code!,
                PartnerId = declaredAt == "partner" ? partner.ID : null,
                Items = [BuiltinVar("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear)]
            });
        }
        const string sql = "SELECT ID AS zoneId FROM TmsZone WHERE CreateTime >= @yearStart";

        // Act
        var error = await Record.ExceptionAsync(() => sqlScope == "common"
            ? scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
                new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql })
            : scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(
                new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        var partnerId = partner.ID;
        var savedSql = await db.Queryable<ShareDataSql>()
            .Where(s => s.PacketId == packet.ID)
            .WhereIF(sqlScope == "common", s => s.PartnerId == null)
            .WhereIF(sqlScope != "common", s => s.PartnerId == partnerId)
            .Select(s => s.ExtractSql)
            .FirstAsync();
        if (expectSaved)
        {
            Assert.Null(error);
            Assert.Equal(sql, savedSql);
        }
        else
        {
            Assert.NotNull(error);
            Assert.Null(savedSql);
        }
    }

    /// <summary>
    /// Description: Lưu câu SQL nối đuôi: ORDER BY ngoài cùng phải theo __lastTime rồi __lastKey (viết bằng bí danh hoặc đúng biểu thức trong SELECT);
    ///              sắp theo cột khác thì dòng cuối trang không phải mốc lớn nhất, Worker sẽ bỏ sót dòng, nên bị từ chối.
    /// Created date: 25/10/2026
    /// </summary>
    [Theory]
    [InlineData("CreateTime, ID", true)]
    [InlineData("__lastTime, __lastKey", true)]
    [InlineData("ID", false)]
    [InlineData("ID, CreateTime", false)]
    public async Task SaveExtractSql_WhenIncrementalOrderBy_RequiresLastTimeThenLastKey_Test(string orderBy, bool expectSaved)
    {
        // Arrange
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, _) = await SeedSqlVariablePacketAsync(db, unique);
        var sql = "SELECT TOP (@pageSize) CreateTime AS __lastTime, ID AS __lastKey FROM TmsZone "
            + "WHERE @lastTime IS NULL OR CreateTime > @lastTime OR (CreateTime = @lastTime AND ID > @lastKey) "
            + $"ORDER BY {orderBy}";

        // Act
        var error = await Record.ExceptionAsync(() => scope.ServiceProvider.CallApiAsync<PacketExtractSqlOutput>(
            new UpdatePacketExtractSqlInput { PacketCode = packet.Code!, ExtractSql = sql }));

        // Assert
        if (expectSaved)
        {
            Assert.Null(error);
            Assert.Equal(sql, (await ShareDataSqlSeed.GetCommonAsync(db, packet.ID))?.ExtractSql);
        }
        else
        {
            Assert.NotNull(error);
        }
    }
}
