using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
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
    /// Description: Theo đối tác: đổi giá trị biến mẫu chung là ghi đè, thêm tên mới là biến riêng; trả giá trị cũ bằng mẫu chung thì bỏ ghi đè
    /// Created date: 24/10/2026
    /// </summary>
    [Fact]
    public async Task UpdateSqlVariables_WhenPartnerScope_SplitsOverrideAndPartnerOwnVariables_Test()
    {
        SetUserContext(isSuperAdmin: true, account: $"super_{Guid.NewGuid():N}");
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (packet, partner) = await SeedSqlVariablePacketAsync(db, unique);

        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            Items = [ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500")]
        });

        var partnerView = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items =
            [
                ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "200"),
                ConstVar("fromDate", ShareDataConst.SqlVariableTypeDateTime, "2027-01-01")
            ]
        });

        var pageSize = Assert.Single(partnerView.Items, i => i.Code == "pageSize");
        Assert.Equal(ShareDataConst.SqlVariableScopeOverride, pageSize.Scope);
        Assert.Equal("200", pageSize.Value);
        Assert.Equal("500", pageSize.CommonValue);
        Assert.Equal(ShareDataConst.SqlVariableScopePartner, Assert.Single(partnerView.Items, i => i.Code == "fromDate").Scope);

        // Mẫu chung không bị ảnh hưởng
        var common = await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new GetPacketSqlVariablesInput { PacketCode = packet.Code! });
        Assert.Equal("500", Assert.Single(common.Items).Value);

        // Đặt lại bằng mẫu chung thì không còn dòng ghi đè trong CSDL
        await scope.ServiceProvider.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput
        {
            PacketCode = packet.Code!,
            PartnerId = partner.ID,
            Items = [ConstVar("pageSize", ShareDataConst.SqlVariableTypeInt, "500")]
        });
        var partnerRows = await db.Queryable<ShareDataSqlVariable>()
            .Where(v => v.PacketId == packet.ID && v.PartnerId == partner.ID)
            .CountAsync();
        Assert.Equal(0, partnerRows);
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
}
