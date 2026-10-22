using Furion.FriendlyException;
using Microsoft.Extensions.DependencyInjection;
using Module.ShareData.Core.Dto.Mapping;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Module.ShareData.Core.Exceptions;
using Module.ShareData.Core.Helpers;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Helpers;
using Shared.DTO.Enums;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Controllers;

public partial class ShareDataPacketControllerTests
{
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
    public async Task GetPartnerSqlList_ReturnsOnlyActiveOutboundMappings_WithAccurateCustomAndOutdatedFlags_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_LST_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql,
        };
        await db.Insertable(packet).ExecuteCommandAsync();

        // 1. Partner A: Active Outbound mapping, standard (no custom SQL)
        var partnerA = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_A_{unique}", Name = $"Partner A {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mappingA = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerA.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = null
        };

        // 2. Partner B: Active Outbound mapping, custom SQL, not outdated (BaseExtractSql == stdSql)
        var partnerB = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_B_{unique}", Name = $"Partner B {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mappingB = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerB.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID, Code FROM TmsZone",
            BaseExtractSql = stdSql
        };

        // 3. Partner C: Active Outbound mapping, custom SQL, outdated (BaseExtractSql != stdSql)
        var partnerC = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_C_{unique}", Name = $"Partner C {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mappingC = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerC.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID, Name FROM TmsZone",
            BaseExtractSql = "SELECT ID FROM TmsZone" // Old base
        };

        // 4. Partner D: Inbound-only mapping -> must NOT appear
        var partnerD = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_D_{unique}", Name = $"Partner D {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mappingD = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerD.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Inbound,
            IsActive = true
        };

        // 5. Partner E: Disabled mapping -> must NOT appear
        var partnerE = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_E_{unique}", Name = $"Partner E {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mappingE = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partnerE.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = false
        };

        await db.Insertable(new[] { partnerA, partnerB, partnerC, partnerD, partnerE }).ExecuteCommandAsync();
        await db.Insertable(new[] { mappingA, mappingB, mappingC, mappingD, mappingE }).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = packetCode });

        // Assert
        Assert.NotNull(result);
        var codes = result.Select(r => r.PartnerCode).ToList();
        Assert.Contains(partnerA.Code, codes);
        Assert.Contains(partnerB.Code, codes);
        Assert.Contains(partnerC.Code, codes);
        Assert.DoesNotContain(partnerD.Code, codes);
        Assert.DoesNotContain(partnerE.Code, codes);

        var itemA = result.First(r => r.PartnerCode == partnerA.Code);
        Assert.False(itemA.IsCustom);
        Assert.False(itemA.IsOutdated);

        var itemB = result.First(r => r.PartnerCode == partnerB.Code);
        Assert.True(itemB.IsCustom);
        Assert.False(itemB.IsOutdated);

        var itemC = result.First(r => r.PartnerCode == partnerC.Code);
        Assert.True(itemC.IsCustom);
        Assert.True(itemC.IsOutdated);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_FirstTime_RecordsBaseExtractSql_AndSuperAdminRequired_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_UPD_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql,
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = null,
            BaseExtractSql = null
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        var customSql = "SELECT ID, Code FROM TmsZone";

        // Act 1: Non-SuperAdmin call must throw
        SetUserContext(isSuperAdmin: false, account: "normal_user");
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = customSql
        }));

        // Act 2: SuperAdmin first time update -> BaseExtractSql snapshot of packet.ExtractSql
        SetUserContext(isSuperAdmin: true, account: superAdmin);
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = customSql
        });

        // Assert
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.Equal(customSql, output.CustomSql);
        Assert.Equal(stdSql, output.BaseSql);
        Assert.False(output.IsOutdated);

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal(customSql, inDb.ExtractSql);
        Assert.Equal(stdSql, inDb.BaseExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenNormalEdit_DoesNotOverwriteBaseExtractSql_UntilReconciled_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_RC_{unique}";
        var oldStdSql = "SELECT ID FROM TmsZone";
        var newStdSql = "SELECT ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = oldStdSql
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID, Code FROM TmsZone",
            BaseExtractSql = oldStdSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Packet template is updated to newStdSql -> Mapping becomes outdated
        packet.ExtractSql = newStdSql;
        await db.Updateable(packet).UpdateColumns(p => new { p.ExtractSql }).ExecuteCommandAsync();

        // Act 1: Sửa thường (MarkReconciled = false) -> không thay đổi BaseExtractSql cũ
        var editedSql = "SELECT ID, Code, UpdateTime FROM TmsZone";
        var out1 = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = editedSql,
            MarkReconciled = false
        });
        Assert.True(out1.IsOutdated);
        Assert.Equal(oldStdSql, out1.BaseSql);

        // Act 2: MarkReconciled = true -> BaseExtractSql được cập nhật thành newStdSql
        var out2 = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = editedSql,
            MarkReconciled = true
        });
        Assert.False(out2.IsOutdated);
        Assert.Equal(newStdSql, out2.BaseSql);

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal(newStdSql, inDb.BaseExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenIdenticalToPacketStandard_AutoResetsPartnerSql_Test()
    {
        // Arrange: Sửa SQL đối tác trùng khớp với mẫu chuẩn của gói tin -> tự động Reset
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EQ_{unique}";
        var stdSql = "SELECT ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID FROM TmsZone",
            BaseExtractSql = stdSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act: Update với SQL trùng khớp chuẩn (khác khoảng trắng nhẹ)
        var outEq = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = "  SELECT   ID,   Code,   Name   FROM   TmsZone  "
        });

        // Assert: Tự Reset
        Assert.False(outEq.IsCustom);
        Assert.Null(outEq.CustomSql);
        Assert.Null(outEq.BaseSql);

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Null(inDb.ExtractSql);
        Assert.Null(inDb.BaseExtractSql);
    }

    [Fact]
    public async Task ResetPartnerExtractSql_ClearsAllThreeColumns_Test()
    {
        // Arrange
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_RST_{unique}";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = "SELECT ID FROM TmsZone"
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID, Code FROM TmsZone",
            BaseExtractSql = "SELECT ID FROM TmsZone"
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput
        {
            MappingId = mapping.ID
        });

        // Assert
        Assert.False(result.IsCustom);
        Assert.Null(result.CustomSql);
        Assert.Null(result.BaseSql);

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Null(inDb.ExtractSql);
        Assert.Null(inDb.BaseExtractSql);
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

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = "SELECT TOP (@pageSize) ID FROM TmsZone",
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act & Assert: SQL có @lastTime nhưng thiếu __lastKey, __lastTime, ORDER BY -> Throws
        var ex = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = "SELECT TOP (@pageSize) ID, Code FROM TmsZone WHERE CreateTime > @lastTime"
        }));

        Assert.NotNull(ex);
    }

    [Fact]
    public async Task SqlTextHelper_SelectActiveOutboundMapping_PicksCustomSqlWhenAvailable_Test()
    {
        // Arrange: Worker ưu tiên câu SQL riêng của đối tác qua SqlTextHelper
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_HLP_{unique}";
        var stdSql = "SELECT TOP (@pageSize) ID FROM TmsZone";
        var customSql = "SELECT TOP (@pageSize) ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql,
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_HLP_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = customSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act
        var selected = await SqlTextHelper.SelectActiveOutboundMappingAsync(db, partner.ID, packet);

        // Assert
        Assert.NotNull(selected);
        Assert.Equal(customSql, selected.ExtractSql);
        Assert.True(SqlTextHelper.IsCustom(selected));
    }

    [Fact]
    public async Task GetPartnerSqlList_WhenMultipleMappingsForSamePartner_PicksLatestMapping_Test()
    {
        // Arrange: 2 hồ sơ cùng đối tác -> chỉ hiển thị 1 dòng đại diện lấy theo bản mới nhất
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_MLT_{unique}";
        var stdSql = "SELECT ID, Code FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_MLT_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };

        var olderMapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = null,
            CreateTime = DateTime.UtcNow.AddMinutes(-20),
            UpdateTime = DateTime.UtcNow.AddMinutes(-20)
        };

        var newerMapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID, Code, Name FROM TmsZone",
            BaseExtractSql = stdSql,
            CreateTime = DateTime.UtcNow.AddMinutes(-10),
            UpdateTime = DateTime.UtcNow
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(new[] { olderMapping, newerMapping }).ExecuteCommandAsync();

        // Đảm bảo newerMapping có UpdateTime mới hơn olderMapping sau khi qua AOP Insert
        await db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.UpdateTime == DateTime.UtcNow.AddMinutes(5))
            .Where(m => m.ID == newerMapping.ID)
            .ExecuteCommandAsync();
        await db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.UpdateTime == DateTime.UtcNow.AddMinutes(-5))
            .Where(m => m.ID == olderMapping.ID)
            .ExecuteCommandAsync();

        // Act
        var result = await scope.ServiceProvider.CallApiAsync<List<PartnerSqlListItemOutput>>(new GetPartnerSqlListInput { PacketCode = packetCode });

        // Assert
        Assert.NotNull(result);
        var partnerRows = result.Where(r => r.PartnerCode == partner.Code).ToList();
        Assert.Single(partnerRows);
        Assert.True(partnerRows[0].IsCustom);
        Assert.Equal(newerMapping.ID, partnerRows[0].MappingId);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenEmptySqlAndNotReconciled_ThrowsPartnerExtractSqlRequired_Test()
    {
        // Arrange: SQL rỗng và MarkReconciled = false -> ném lỗi PartnerExtractSqlRequired
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EMP_{unique}";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = "SELECT ID FROM TmsZone"
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_EMP_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = "   ",
            MarkReconciled = false
        }));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenEmptySqlAndReconciled_WithoutExistingCustomSql_ThrowsPartnerExtractSqlRequired_Test()
    {
        // Arrange: SQL rỗng và MarkReconciled = true nhưng hồ sơ chưa từng có SQL riêng -> cũng ném PartnerExtractSqlRequired
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_EMPRC_{unique}";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = "SELECT ID FROM TmsZone"
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_EMPRC_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = null
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = string.Empty,
            MarkReconciled = true
        }));
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenEmptySqlAndReconciled_WithExistingCustomSql_KeepsCustomSqlAndReconcilesBase_Test()
    {
        // Arrange: SQL rỗng và MarkReconciled = true khi đã có SQL riêng -> giữ câu riêng, chỉ cập nhật BaseExtractSql = root
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_KEEPRC_{unique}";
        var oldStdSql = "SELECT ID FROM TmsZone";
        var newStdSql = "SELECT ID, Code FROM TmsZone";
        var customSql = "SELECT ID, Code, Name FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = newStdSql
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_KEEPRC_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = customSql,
            BaseExtractSql = oldStdSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = null,
            MarkReconciled = true
        });

        // Assert
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.Equal(customSql, output.CustomSql);
        Assert.Equal(newStdSql, output.BaseSql);
        Assert.False(output.IsOutdated);

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.Equal(customSql, inDb.ExtractSql);
        Assert.Equal(newStdSql, inDb.BaseExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenDifferentSql_KeepsCustomMapping_Test()
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

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql,
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_DM_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act: Gửi câu SQL riêng nối đuôi
        var output = await scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = customSql
        });

        // Assert: Lưu riêng, chế độ suy từ câu SQL riêng khác câu chuẩn
        Assert.NotNull(output);
        Assert.True(output.IsCustom);
        Assert.True(SqlValidator.IsIncrementalSql(output.CustomSql));
        Assert.False(SqlValidator.IsIncrementalSql(output.StandardSql));

        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.NotNull(inDb.ExtractSql);
        Assert.Equal(customSql, inDb.ExtractSql);
    }

    [Fact]
    public async Task UpdatePartnerExtractSql_WhenMappingDirectionInbound_ThrowsPartnerSqlDirectionInvalid_Test()
    {
        // Arrange: Hồ sơ có chiều nhận (Inbound) -> không được phép cấu hình SQL trích xuất
        var superAdmin = $"super_{Guid.NewGuid():N}";
        SetUserContext(isSuperAdmin: true, account: superAdmin);

        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_INB_{unique}";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = "SELECT ID FROM TmsZone"
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_INB_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Inbound,
            IsActive = true
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
        {
            MappingId = mapping.ID,
            ExtractSql = "SELECT ID, Code FROM TmsZone"
        }));
    }

    [Fact]
    public async Task UpdateShareDataMapping_PreservesPartnerCustomSqlColumns_Test()
    {
        // Arrange: Màn hình Ánh xạ cập nhật hồ sơ qua API cũ không được ghi đè/xóa ExtractSql, BaseExtractSql
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_MAP_UPD_{unique}";
        var customSql = "SELECT ID, Code FROM TmsZone";
        var baseSql = "SELECT ID FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = baseSql
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_MAP_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
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
            TargetShapeJson = targetShape,
            ExtractSql = customSql,
            BaseExtractSql = baseSql
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

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

        // Assert: 2 cột SQL riêng vẫn giữ nguyên 100%
        var inDb = await db.Queryable<ShareDataMapping>().InSingleAsync(mapping.ID);
        Assert.NotNull(inDb);
        Assert.Equal($"Mapping Updated {unique}", inDb.Name);
        Assert.Equal(customSql, inDb.ExtractSql);
        Assert.Equal(baseSql, inDb.BaseExtractSql);
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
    public void SqlTextHelper_IsOutdated_And_HasDifferences_Behaviors_Test()
    {
        var packet = new ShareDataPacket { ExtractSql = "SELECT ID, Code FROM TmsZone" };

        // 1. Null mapping
        Assert.False(SqlTextHelper.IsCustom(null));
        Assert.False(SqlTextHelper.IsOutdated(null, packet));
        Assert.False(SqlTextHelper.HasDifferences(null, packet));

        // 2. Mapping không có ExtractSql
        var stdMapping = new ShareDataMapping { ExtractSql = null };
        Assert.False(SqlTextHelper.IsCustom(stdMapping));
        Assert.False(SqlTextHelper.IsOutdated(stdMapping, packet));
        Assert.False(SqlTextHelper.HasDifferences(stdMapping, packet));

        // 3. Mapping có ExtractSql trùng chuẩn
        var sameMapping = new ShareDataMapping { ExtractSql = "  SELECT   ID,   Code   FROM   TmsZone;  ", BaseExtractSql = packet.ExtractSql };
        Assert.True(SqlTextHelper.IsCustom(sameMapping));
        Assert.False(SqlTextHelper.HasDifferences(sameMapping, packet));
        Assert.False(SqlTextHelper.IsOutdated(sameMapping, packet));

        // 4. Mapping có ExtractSql khác chuẩn
        var customMapping = new ShareDataMapping { ExtractSql = "SELECT ID FROM TmsZone", BaseExtractSql = packet.ExtractSql };
        Assert.True(SqlTextHelper.IsCustom(customMapping));
        Assert.True(SqlTextHelper.HasDifferences(customMapping, packet));
        Assert.False(SqlTextHelper.IsOutdated(customMapping, packet));

        // 5. Mapping có BaseExtractSql khác chuẩn -> Outdated
        var outdatedMapping = new ShareDataMapping { ExtractSql = "SELECT ID FROM TmsZone", BaseExtractSql = "SELECT Old FROM TmsZone" };
        Assert.True(SqlTextHelper.IsCustom(outdatedMapping));
        Assert.True(SqlTextHelper.IsOutdated(outdatedMapping, packet));
    }

    [Fact]
    public async Task SqlTextHelper_SelectActiveOutboundMappingsAsync_DedupesPerPartner_PicksLatest_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_DEDUP_{unique}";
        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable
        };

        var p1 = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT1_{unique}", Name = $"P1 {unique}", Status = BaseEnums.StatusEnum.Enable };
        var p2 = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT2_{unique}", Name = $"P2 {unique}", Status = BaseEnums.StatusEnum.Enable };

        var m1Old = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = p1.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            CreateTime = DateTime.UtcNow.AddMinutes(-30),
            UpdateTime = DateTime.UtcNow.AddMinutes(-30)
        };
        var m1New = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = p1.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = "SELECT ID FROM TmsZone",
            CreateTime = DateTime.UtcNow.AddMinutes(-10),
            UpdateTime = DateTime.UtcNow
        };
        var m2Active = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = p2.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Both,
            IsActive = true
        };
        var m2Disabled = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = p2.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = false
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(new[] { p1, p2 }).ExecuteCommandAsync();
        await db.Insertable(new[] { m1Old, m1New, m2Active, m2Disabled }).ExecuteCommandAsync();

        // Đảm bảo m1New có UpdateTime mới hơn m1Old sau khi qua AOP Insert
        await db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.UpdateTime == DateTime.UtcNow.AddMinutes(5))
            .Where(m => m.ID == m1New.ID)
            .ExecuteCommandAsync();
        await db.Updateable<ShareDataMapping>()
            .SetColumns(m => m.UpdateTime == DateTime.UtcNow.AddMinutes(-5))
            .Where(m => m.ID == m1Old.ID)
            .ExecuteCommandAsync();

        // Act
        var result = await SqlTextHelper.SelectActiveOutboundMappingsAsync(db, packet);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        var mapP1 = result.First(m => m.PartnerId == p1.ID);
        Assert.Equal(m1New.ID, mapP1.ID);
        var mapP2 = result.First(m => m.PartnerId == p2.ID);
        Assert.Equal(m2Active.ID, mapP2.ID);
    }

    [Fact]
    public async Task SqlTextHelper_SelectActiveOutboundMapping_FallsBackToPacketStandard_WhenNoCustomSql_Test()
    {
        // Arrange
        using var scope = _host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var packetCode = $"PKT_DEF_{unique}";
        var stdSql = "SELECT TOP (@pageSize) ID FROM TmsZone";

        var packet = new ShareDataPacket
        {
            ID = Guid.NewGuid().ToString("N"),
            Code = packetCode,
            Name = $"Packet {unique}",
            Status = BaseEnums.StatusEnum.Enable,
            ExtractSql = stdSql,
        };
        var partner = new ShareDataPartner { ID = Guid.NewGuid().ToString("N"), Code = $"PT_DEF_{unique}", Name = $"Partner {unique}", Status = BaseEnums.StatusEnum.Enable };
        var mapping = new ShareDataMapping
        {
            ID = Guid.NewGuid().ToString("N"),
            PartnerId = partner.ID,
            DatatypeId = packetCode,
            Direction = BaseEnums.Direction.Outbound,
            IsActive = true,
            ExtractSql = null
        };

        await db.Insertable(packet).ExecuteCommandAsync();
        await db.Insertable(partner).ExecuteCommandAsync();
        await db.Insertable(mapping).ExecuteCommandAsync();

        // Act
        var selected = await SqlTextHelper.SelectActiveOutboundMappingAsync(db, partner.ID, packet);

        // Assert
        Assert.NotNull(selected);
        Assert.False(SqlTextHelper.IsCustom(selected));
        Assert.Null(selected.ExtractSql);

        // Worker phân giải chính sách theo câu SQL sẽ chạy thật:
        var effectiveSql = SqlTextHelper.IsCustom(selected) ? selected.ExtractSql!.Trim() : packet.ExtractSql?.Trim();
        var effectivePolicy = PacketHelper.GetPolicyFromSql(effectiveSql);

        Assert.Equal(stdSql, effectiveSql);
        Assert.Equal(ShareDataEnum.PacketPolicy.Snapshot, effectivePolicy);
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
