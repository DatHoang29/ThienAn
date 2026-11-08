using System.Globalization;
using Modules.CfgSystem.Core.Entities;
using Modules.TMS.Core.Entities;
using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Dto.Packet;
using Module.ShareData.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShareDataWorker.Core.Models.DataOutbound;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.WorkerConfig;
using Tests.ShareData.Common;
using Xunit;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Author: DatHQ
    /// Description: Lượt gửi sau khi đổi cấu hình: sửa tab Mẫu chung, chuyển giữa mẫu chung và cấu hình riêng; dừng lượt khi hết thời gian khóa;
    ///              cảnh báo bộ khung sai, thiếu bộ mã; dòng chưa có thời gian.
    /// Created date: 05/11/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        /// <summary>
        /// Author: DatHQ
        /// Description: Câu nối đuôi trên TmsZone lọc theo tiền tố mã, cột mốc ghép timeExpr với khóa, dùng bí danh alias.
        /// Created date: 05/11/2026
        /// </summary>
        private static string ZoneCursorSql(string alias = "z", string? timeExpr = null)
        {
            var time = timeExpr ?? $"COALESCE({alias}.UpdateTime, {alias}.CreateTime)";
            var key = $"CONVERT(varchar(23), {time}, 121) + '_' + {alias}.ID";
            return $"SELECT TOP (@pageSize) {alias}.Code AS rowCode, {key} AS __lastKey FROM TmsZone {alias} WHERE {alias}.Code LIKE @prefix AND {key} > @lastKey ORDER BY __lastKey ASC";
        }

        /// <summary>Bộ khung chỉ lấy cột rowCode của câu test.</summary>
        private static readonly Dictionary<string, object> RowCodeShape = new()
        {
            ["rowCode"] = new Dictionary<string, string> { ["$field"] = "rowCode" }
        };

        /// <summary>
        /// Author: DatHQ
        /// Description: Thêm một dòng TmsZone có mã theo tiền tố, đặt giờ cập nhật theo tham số.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task SeedZoneRowAsync(ISqlSugarClient db, string rowPrefix, string unique, string name, DateTime updateTime)
        {
            var id = $"{name}_{unique}";
            await db.Insertable(new TmsZone { ID = id, Code = $"{rowPrefix}{name}", Name = name }).ExecuteCommandAsync();
            await db.Updateable<TmsZone>().SetColumns(z => z.UpdateTime == updateTime).Where(z => z.ID == id).ExecuteCommandAsync();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Lưu câu mẫu chung qua API như thao tác thật nên có danh sách trường; bảng TmsZone phải nằm trong danh sách được phép đọc.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task SaveCommonSqlAsync(IServiceProvider services, ISqlSugarClient db, string packetCode, string sql)
        {
            var zoneIsQueryable = await db.Queryable<ShareDataSourceTable>().Where(t => t.TableName == "TmsZone" && t.IsQueryable == true).AnyAsync();
            if (!zoneIsQueryable)
            {
                await db.Insertable(new ShareDataSourceTable { ID = Guid.NewGuid().ToString("N"), TableName = "TmsZone", Name = "TmsZone", IsQueryable = true }).ExecuteCommandAsync();
            }

            await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packetCode, ExtractSql = sql, Force = true });
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đặt trạng thái các đăng ký và cho đến hạn chạy ngay.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task SetSubscriptionStateAsync(ISqlSugarClient db, BaseEnums.SubSubscriptionState state, params string[] subscriptionIds)
        {
            var dueNow = await db.Ado.GetDateTimeAsync("SELECT GETDATE()");
            var nextRun = dueNow.AddSeconds(-10);
            await db.Updateable<ShareDataSubscription>()
                .SetColumns(s => new ShareDataSubscription { State = state, NextTimeRun = nextRun })
                .Where(s => subscriptionIds.Contains(s.ID))
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Tạo DataWorkerConfigService đọc nhóm cấu hình cô lập với các giá trị do test đặt, không ảnh hưởng cấu hình dùng chung của các bài test khác.
        /// Created date: 05/11/2026
        /// </summary>
        private static async Task<DataWorkerConfigService> CreateWorkerConfigAsync(IServiceScope scope, ISqlSugarClient db, params (string Code, int Value)[] values)
        {
            var isolatedGroup = $"sd_outbound_cfg_{Guid.NewGuid():N}";
            foreach (var (code, value) in values)
            {
                await db.Insertable(new SysOpConfig
                {
                    ID = Guid.NewGuid().ToString("N"),
                    GroupCode = isolatedGroup,
                    Code = code,
                    Value = value.ToString(CultureInfo.InvariantCulture),
                    Status = BaseEnums.StatusEnum.Enable
                }).ExecuteCommandAsync();
            }

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { [ShareDataConst.ConfigGroupOverrideKey] = isolatedGroup })
                .Build();
            var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

            return new DataWorkerConfigService(
                scopeFactory,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DataWorkerConfigService>.Instance,
                config);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bộ biến của ZoneCursorSql ở dạng lưu trong VariablesJson: @pageSize, @prefix và @lastKey có Mốc lần đầu.
        /// Created date: 05/11/2026
        /// </summary>
        private static List<SqlVariableItem> ZoneVariables(int pageSize, string prefix, string? start) =>
        [
            new() { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = pageSize.ToString(CultureInfo.InvariantCulture), OrderNo = 1 },
            new() { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{prefix}%", OrderNo = 2 },
            new() { Code = "lastKey", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, DefaultValue = start, OrderNo = 3 }
        ];

        /// <summary>
        /// Author: DatHQ
        /// Description: Bộ biến của ZoneCursorSql ở dạng gửi lên API lưu bảng tham số.
        /// Created date: 05/11/2026
        /// </summary>
        private static List<PacketSqlVariableItemInput> ZoneVariableInputs(int pageSize, string prefix, string? start) =>
        [
            new() { Code = "pageSize", DataType = ShareDataConst.SqlVariableTypeInt, ValueKind = ShareDataConst.SqlVariableKindConst, Value = pageSize.ToString(CultureInfo.InvariantCulture) },
            new() { Code = "prefix", DataType = ShareDataConst.SqlVariableTypeString, ValueKind = ShareDataConst.SqlVariableKindConst, Value = $"{prefix}%" },
            new() { Code = "lastKey", ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = ShareDataConst.SqlVariableSourceLastKey, DefaultValue = start }
        ];

        /// <summary>
        /// Author: DatHQ
        /// Description: Sửa tab Mẫu chung mà giữ công thức cột mốc (đổi Mốc lần đầu, @pageSize, bí danh bảng) rồi chạy lượt gửi:
        ///              đối tác chưa có mốc lấy từ Mốc lần đầu mới theo @pageSize mới; đối tác đã có mốc đi tiếp từ mốc của mình theo @pageSize mới, không có ESH-1308;
        ///              đối tác có cấu hình riêng giữ bản chép lúc tạo nên vẫn theo Mốc lần đầu và @pageSize cũ.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCommonConfigChangedKeepingFormula_AppliesOnlyToPartnersOnCommon_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            UseSuperAdmin(unique);
            var packetId = $"pkt-cfg-{unique}";
            var packetCode = $"PKT_CFG_{unique}";
            var rowPrefix = $"ZC_CFG_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            foreach (var (name, hour) in new[] { ("R1", 0), ("R2", 1), ("R3", 2), ("R4", 3) })
            {
                var rowTime = baseTime.AddHours(hour);
                await SeedZoneRowAsync(db, rowPrefix, unique, name, rowTime);
            }

            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var startVariables = ZoneVariables(50, rowPrefix, "2026-10-27 09:30:00.000_");
            var commonSql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, startVariables);
            await SaveCommonSqlAsync(services, db, packetCode, commonSql);

            async Task<ShareDataSubscription> SeedPartner(string name, bool active)
            {
                var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
                {
                    PartnerCode = $"P{name}_{unique}",
                    SubCode = $"SUB_{name}_{unique}",
                    DatatypeId = packetId,
                    MappingShape = RowCodeShape,
                    ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/cfg-{name}-{unique}",
                    ConfigureSub = s => s.State = active ? BaseEnums.SubSubscriptionState.Active : BaseEnums.SubSubscriptionState.Paused
                });
                return sub;
            }

            var subA = await SeedPartner("a", active: true);
            var subB = await SeedPartner("b", active: false);
            var subC = await SeedPartner("c", active: false);
            await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = subC.PartnerId!, PacketCode = packetCode, ExtractSql = commonSql });

            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);
            var firstBodiesA = PartnerBodies($"cfg-a-{unique}");
            var firstBodyA = Assert.Single(firstBodiesA);

            // Act: tắt đăng ký, sửa bảng tham số và đổi bí danh bảng ở mẫu chung, thêm dòng mới rồi bật lại cả ba
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Paused, subA.ID);
            var changedVariables = ZoneVariableInputs(1, rowPrefix, "2026-10-27 11:30:00.000_");
            var aliasedSql = ZoneCursorSql("k");
            var newRowTime = baseTime.AddHours(4);
            await services.CallApiAsync<PacketSqlVariablesOutput>(new UpdatePacketSqlVariablesInput { PacketCode = packetCode, Items = changedVariables });
            var savedSql = await services.CallApiAsync<PacketExtractSqlOutput>(new UpdatePacketExtractSqlInput { PacketCode = packetCode, ExtractSql = aliasedSql });
            await SeedZoneRowAsync(db, rowPrefix, unique, "R5", newRowTime);
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Active, subA.ID, subB.ID, subC.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);
            await MakeSubscriptionDue(db, subB.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.False(savedSql.RequiresConfirm);
            Assert.Contains($"\"{rowPrefix}R2\"", firstBodyA);
            Assert.DoesNotContain($"\"{rowPrefix}R1\"", firstBodyA);

            var bodiesA = PartnerBodies($"cfg-a-{unique}");
            var hasFormulaAlert = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == subA.ID && a.AlertCode == ShareDataAlertCode.Outbound.CursorFormulaChanged).AnyAsync();
            Assert.Equal(2, bodiesA.Count);
            Assert.Contains($"\"{rowPrefix}R5\"", bodiesA[1]);
            Assert.DoesNotContain($"\"{rowPrefix}R4\"", bodiesA[1]);
            Assert.False(hasFormulaAlert);

            var bodiesB = PartnerBodies($"cfg-b-{unique}");
            Assert.Equal(2, bodiesB.Count);
            Assert.Contains($"\"{rowPrefix}R4\"", bodiesB[0]);
            Assert.Contains($"\"{rowPrefix}R5\"", bodiesB[1]);
            Assert.DoesNotContain(bodiesB, b => b.Contains($"\"{rowPrefix}R3\""));

            var bodiesC = PartnerBodies($"cfg-c-{unique}");
            var bodyC = Assert.Single(bodiesC);
            Assert.Contains($"\"{rowPrefix}R2\"", bodyC);
            Assert.Contains($"\"{rowPrefix}R5\"", bodyC);
            Assert.DoesNotContain($"\"{rowPrefix}R1\"", bodyC);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Mốc gắn với đối tác và gói, không gắn với câu SQL: tạo cấu hình riêng hoặc dùng lại mẫu chung cùng công thức thì lượt sau gửi tiếp từ mốc cũ;
        ///              dùng lại mẫu chung khác công thức, xác nhận xóa mốc thì lượt sau gửi lại từ Mốc lần đầu của mẫu chung.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("taoCauHinhRieng")]
        [InlineData("dungLaiMauChung")]
        [InlineData("dungLaiMauChungKhacCongThuc")]
        public async Task ProcessSubscriptions_WhenPartnerSwitchesConfig_CursorFollowsUntilFormulaChanges_Test(string action)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            UseSuperAdmin(unique);
            var packetId = $"pkt-sw-{unique}";
            var packetCode = $"PKT_SW_{unique}";
            var rowPrefix = $"ZC_SW_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            var secondRowTime = baseTime.AddHours(1);
            await SeedZoneRowAsync(db, rowPrefix, unique, "R1", baseTime);
            await SeedZoneRowAsync(db, rowPrefix, unique, "R2", secondRowTime);

            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(50, rowPrefix, null);
            var partnerSql = ZoneCursorSql();
            var commonSql = action == "dungLaiMauChungKhacCongThuc" ? ZoneCursorSql(timeExpr: "z.CreateTime") : partnerSql;
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await SaveCommonSqlAsync(services, db, packetCode, commonSql);

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SW_{unique}",
                SubCode = $"SUB_SW_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/sw-{unique}",
                ConfigureSub = s => s.State = BaseEnums.SubSubscriptionState.Paused
            });
            if (action != "taoCauHinhRieng")
            {
                await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput
                {
                    PartnerId = partner.ID!,
                    PacketCode = packetCode,
                    ExtractSql = partnerSql,
                    Force = true
                });
            }

            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Active, sub.ID);
            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);
            var firstBodies = PartnerBodies($"sw-{unique}");
            var firstBody = Assert.Single(firstBodies);

            // Act
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Paused, sub.ID);
            PartnerExtractSqlOutput first;
            if (action == "taoCauHinhRieng")
            {
                first = await services.CallApiAsync<PartnerExtractSqlOutput>(new UpdatePartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packetCode, ExtractSql = partnerSql });
            }
            else
            {
                first = await services.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packetCode });
                if (action == "dungLaiMauChungKhacCongThuc")
                {
                    await services.CallApiAsync<PartnerExtractSqlOutput>(new ResetPartnerExtractSqlInput { PartnerId = partner.ID!, PacketCode = packetCode, Force = true });
                }
            }

            var newRowTime = baseTime.AddHours(2);
            await SeedZoneRowAsync(db, rowPrefix, unique, "R3", newRowTime);
            await SetSubscriptionStateAsync(db, BaseEnums.SubSubscriptionState.Active, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert
            Assert.Contains($"\"{rowPrefix}R2\"", firstBody);
            var bodies = PartnerBodies($"sw-{unique}");
            Assert.Equal(2, bodies.Count);
            Assert.Contains($"\"{rowPrefix}R3\"", bodies[1]);
            if (action == "dungLaiMauChungKhacCongThuc")
            {
                Assert.True(first.RequiresConfirm);
                Assert.Contains($"\"{rowPrefix}R1\"", bodies[1]);
                Assert.Contains($"\"{rowPrefix}R2\"", bodies[1]);
            }
            else
            {
                Assert.False(first.RequiresConfirm);
                Assert.DoesNotContain($"\"{rowPrefix}R1\"", bodies[1]);
                Assert.DoesNotContain($"\"{rowPrefix}R2\"", bodies[1]);
            }
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Đặt mốc bằng ô chọn ngày giờ (chuỗi kết thúc bằng dấu gạch dưới): lượt tới gửi cả dòng đúng thời điểm đó, không gửi dòng trước đó, kể cả chỉ sớm hơn vài mili giây.
        ///              Mốc lần đầu chọn bằng ô ngày giờ cho kết quả như vậy.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ProcessSubscriptions_WhenStartPickedByTime_IncludesRowAtExactlyThatTime_Test(bool setViaCursorApi)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            UseSuperAdmin(unique);
            var packetId = $"pkt-pick-{unique}";
            var packetCode = $"PKT_PICK_{unique}";
            var rowPrefix = $"ZC_PICK_{unique}_";
            var picked = new DateTime(2026, 10, 31, 8, 0, 0);
            var beforePicked = picked.AddMilliseconds(-3);
            var afterPicked = picked.AddMinutes(1);
            const string pickedKey = "2026-10-31 08:00:00.000_";
            await SeedZoneRowAsync(db, rowPrefix, unique, "TRUOC", beforePicked);
            await SeedZoneRowAsync(db, rowPrefix, unique, "DUNG", picked);
            await SeedZoneRowAsync(db, rowPrefix, unique, "SAU", afterPicked);
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(50, rowPrefix, setViaCursorApi ? null : pickedKey);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_PICK_{unique}",
                SubCode = $"SUB_PICK_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/pick-{unique}"
            });
            if (setViaCursorApi)
            {
                await SetCursorWhilePausedAsync(services, db, sub.ID!, new SetLastSendInput { PacketCode = packetCode, PartnerId = partner.ID, LastKey = pickedKey });
                await MakeSubscriptionDue(db, sub.ID);
            }

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"pick-{unique}");
            var body = Assert.Single(bodies);
            Assert.DoesNotContain($"\"{rowPrefix}TRUOC\"", body);
            Assert.Contains($"\"{rowPrefix}DUNG\"", body);
            Assert.Contains($"\"{rowPrefix}SAU\"", body);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Khóa __lastKey kiểu số nguyên hoặc GUID: chốt chặn ghi mốc so theo đúng kiểu đó, nên mốc vẫn tiến khi so chuỗi cho kết quả ngược ("9" và "10"; GUID theo thứ tự của SQL Server).
        ///              Trang 1 dòng, mỗi dòng tới đối tác đúng một lần và mốc dừng ở dòng cuối.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("soNguyen")]
        [InlineData("guid")]
        public async Task ProcessSubscriptions_WhenCursorKeyIsTypedNumberOrGuid_AdvancesByKeyType_Test(string keyType)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-typ-{unique}";
            var packetCode = $"PKT_TYP_{unique}";
            var rowPrefix = $"ROW_TYP_{unique}_";
            // Hai GUID xếp ngược giữa SQL Server (so nhóm cuối trước) và so chuỗi
            string[] guids = ["ffffffff-0000-0000-0000-000000000001", "00000000-0000-0000-0000-000000000002"];
            var rows = keyType == "soNguyen"
                ? new[] { 9, 10 }.Select(n => new ShareDataPacket { ID = $"t{n}_{unique}", Code = $"{rowPrefix}{n}", Name = $"Row {n}", OrderNo = n, PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable }).ToList()
                : guids.Select((g, i) => new ShareDataPacket { ID = $"t{i}_{unique}", Code = $"{rowPrefix}{i}", Name = g, PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Disable }).ToList();
            await db.Insertable(rows).ExecuteCommandAsync();
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();

            var key = keyType == "soNguyen" ? "p.OrderNo" : "TRY_CAST(p.Name AS uniqueidentifier)";
            var start = keyType == "soNguyen" ? "0" : Guid.Empty.ToString();
            var variables = ZoneVariables(1, rowPrefix, start);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId,
                $"SELECT TOP (@pageSize) p.Code AS rowCode, {key} AS __lastKey FROM ShareDataPacket p WHERE p.Code LIKE @prefix AND {key} > @lastKey ORDER BY __lastKey ASC");
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_TYP_{unique}",
                SubCode = $"SUB_TYP_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/typ-{unique}"
            });

            // Act: 2 lượt gửi tuần tự (mỗi lượt 1 trang theo pageSize = 1)
            var worker = CreateWorker(scope);
            await worker.ProcessSubscriptions(CancellationToken.None);
            await MakeSubscriptionDue(db, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"typ-{unique}");
            Assert.Equal(2, bodies.Count);
            Assert.Contains($"\"{rows[0].Code}\"", bodies[0]);
            Assert.Contains($"\"{rows[1].Code}\"", bodies[1]);
            var cursor = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode).FirstAsync();
            Assert.Equal(keyType == "soNguyen" ? "10" : guids[1], cursor.LastKey, ignoreCase: true);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu đã lưu từ trước có cột __lastKey nhưng không dùng biến mốc (nay lưu qua API bị từ chối) vẫn chạy: Worker gửi như câu gửi toàn bộ,
        ///              không ghi mốc và lượt sau gửi lại đúng các dòng đó.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenSqlHasCursorColumnButNoCursorVariable_SendsSameRowsEveryRun_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-nov-{unique}";
            var packetCode = $"PKT_NOV_{unique}";
            var rowPrefix = $"ZC_NOV_{unique}_";
            await SeedZoneRowAsync(db, rowPrefix, unique, "R1", new DateTime(2026, 10, 27, 9, 0, 0));
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var sql = $"SELECT TOP 10 z.Code AS rowCode, z.ID AS __lastKey FROM TmsZone z WHERE z.Code LIKE '{rowPrefix}%' AND z.ID > '0' ORDER BY z.ID ASC";
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_NOV_{unique}",
                SubCode = $"SUB_NOV_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/nov-{unique}"
            });
            var worker = CreateWorker(scope);

            // Act
            await worker.ProcessSubscriptions(CancellationToken.None);
            await MakeSubscriptionDue(db, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"nov-{unique}");
            var cursor = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode).FirstAsync();
            Assert.Equal(2, bodies.Count);
            Assert.All(bodies, body => Assert.Contains($"\"{rowPrefix}R1\"", body));
            Assert.True(string.IsNullOrEmpty(cursor?.LastKey));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bộ khung sai cú pháp hoặc rỗng: ghi ESH-1204, không gửi, mốc giữ nguyên.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("{ không phải json")]
        [InlineData("   ")]
        public async Task ProcessSubscriptions_WhenShapeInvalid_LogsEsh1204AndKeepsCursor_Test(string shape)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-shp-{unique}";
            var packetCode = $"PKT_SHP_{unique}";
            var rowPrefix = $"ZC_SHP_{unique}_";
            await SeedZoneRowAsync(db, rowPrefix, unique, "R1", new DateTime(2026, 10, 27, 9, 0, 0));
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(10, rowPrefix, null);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_SHP_{unique}",
                SubCode = $"SUB_SHP_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/shp-{unique}"
            });
            await db.Updateable<ShareDataMapping>().SetColumns(m => m.TargetShapeJson == shape).Where(m => m.PartnerId == partner.ID).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"shp-{unique}");
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).ToListAsync();
            var cursor = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode).FirstAsync();
            Assert.Empty(bodies);
            Assert.Contains(alerts, a => a.AlertCode == ShareDataAlertCode.Outbound.ShapeInvalid);
            Assert.True(string.IsNullOrEmpty(cursor?.LastKey));
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Bộ khung trỏ tới bộ mã không có trong CSDL: ghi ESH-1201 mức cảnh báo, vẫn gửi giá trị thô và tiến mốc.
        /// Created date: 05/11/2026
        /// </summary>
        [Fact]
        public async Task ProcessSubscriptions_WhenCodeSetMissing_LogsEsh1201AndSendsRawValue_Test()
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-csm-{unique}";
            var packetCode = $"PKT_CSM_{unique}";
            var rowPrefix = $"ZC_CSM_{unique}_";
            await SeedZoneRowAsync(db, rowPrefix, unique, "R1", new DateTime(2026, 10, 27, 9, 0, 0));
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(10, rowPrefix, null);
            var sql = ZoneCursorSql();
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, sql);
            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_CSM_{unique}",
                SubCode = $"SUB_CSM_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object>
                {
                    ["rowCode"] = new Dictionary<string, object> { ["$field"] = "rowCode", ["$extend"] = new Dictionary<string, string> { ["codeSet"] = $"CS_THIEU_{unique}" } }
                },
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/csm-{unique}"
            });

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"csm-{unique}");
            var body = Assert.Single(bodies);
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID && a.AlertCode == ShareDataAlertCode.Outbound.CodeSetNotFound).ToListAsync();
            var alert = Assert.Single(alerts);
            var cursor = await db.Queryable<ShareDataLastSend>().Where(ls => ls.PartnerCode == partner.Code && ls.PacketCode == packetCode).FirstAsync();
            Assert.Contains($"\"{rowPrefix}R1\"", body);
            Assert.Equal(BaseEnums.AlertSeverity.Warning, alert.Severity);
            Assert.EndsWith($"_R1_{unique}", cursor.LastKey);
        }

        /// <summary>
        /// Author: DatHQ
        /// Description: Câu đã lưu từ trước mà phần thời gian không có CreateTime (nay lưu qua API bị từ chối) vẫn chạy: dòng có cột đó trống ghép ra NULL nên chưa được gửi,
        ///              Worker ghi rủi ro vào nhật ký cấu hình, không cảnh báo; có thời gian thì lượt sau gửi đúng dòng đó.
        /// Created date: 05/11/2026
        /// </summary>
        [Theory]
        [InlineData("s.DetectWay")]
        [InlineData("COALESCE(s.UpdateTime, s.DetectWay)")]
        public async Task ProcessSubscriptions_WhenCursorTimeEmpty_SkipsRowUntilTimeAppears_Test(string timeExpr)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-emp-{unique}";
            var packetCode = $"PKT_EMP_{unique}";
            var rowPrefix = $"ZC_EMP_{unique}_";
            var baseTime = new DateTime(2026, 10, 27, 9, 0, 0);
            var emptyId = $"R2_{unique}";
            await db.Insertable(new List<TmsZoneStatus>
            {
                new() { ID = $"R1_{unique}", ZoneId = $"{rowPrefix}R1", DetectWay = baseTime },
                new() { ID = emptyId, ZoneId = $"{rowPrefix}R2" }
            }).ExecuteCommandAsync();

            var key = $"CONVERT(varchar(23), {timeExpr}, 121) + '_' + s.ID";
            await db.Insertable(new ShareDataPacket { ID = packetId, Code = packetCode, Name = $"Packet {packetCode}", PacketVersion = "1.0", Status = BaseEnums.StatusEnum.Enable }).ExecuteCommandAsync();
            var variables = ZoneVariables(10, rowPrefix, null);
            await ShareDataSqlSeed.SetVariablesAsync(db, packetId, null, variables);
            await ShareDataSqlSeed.SetCommonAsync(db, packetId,
                $"SELECT TOP (@pageSize) s.ZoneId AS rowCode, {key} AS __lastKey FROM TmsZoneStatus s WHERE s.ZoneId LIKE @prefix AND {key} > @lastKey ORDER BY __lastKey ASC");
            var (_, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_EMP_{unique}",
                SubCode = $"SUB_EMP_{unique}",
                DatatypeId = packetId,
                MappingShape = RowCodeShape,
                ConfigurePartner = p => p.EndPointApiUrl = $"/api/sharedata/emp-{unique}"
            });
            var worker = CreateWorker(scope);

            // Act
            await worker.ProcessSubscriptions(CancellationToken.None);
            var firstBodies = PartnerBodies($"emp-{unique}");
            var firstBody = Assert.Single(firstBodies);
            var filledTime = baseTime.AddHours(1);
            await db.Updateable<TmsZoneStatus>().SetColumns(s => s.DetectWay == filledTime).Where(s => s.ID == emptyId).ExecuteCommandAsync();
            await MakeSubscriptionDue(db, sub.ID);
            await worker.ProcessSubscriptions(CancellationToken.None);

            // Assert
            var bodies = PartnerBodies($"emp-{unique}");
            var alertCount = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).CountAsync();
            var hasRiskLog = await db.Queryable<ShareDataActivityLog>().Where(l => l.TargetId == sub.ID && l.Description == ShareDataEnum.ExportMessage.CursorSqlRisk).AnyAsync();
            Assert.Contains($"\"{rowPrefix}R1\"", firstBody);
            Assert.DoesNotContain($"\"{rowPrefix}R2\"", firstBody);
            Assert.Equal(2, bodies.Count);
            Assert.Contains($"\"{rowPrefix}R2\"", bodies[1]);
            Assert.DoesNotContain($"\"{rowPrefix}R1\"", bodies[1]);
            Assert.Equal(0, alertCount);
            Assert.True(hasRiskLog);
        }
    }
}
