using Module.ShareData.Core.Constants;
using Module.ShareData.Core.Entities;
using Tests.ShareData.Common;

namespace Tests.ShareData.Infrastructure.Services.DataOutbound
{
    public partial class DataOutboundServiceTests
    {
        #region 10. Biến ở bảng tham số câu truy vấn (ShareDataSqlVariable) khi Worker gửi

        /// <summary>
        /// Câu SQL dùng đủ 6 kiểu biến (String, Int, Decimal, Bool, Date, DateTime) và đủ nguồn "Lấy từ" (hằng số, thời điểm chạy, đầu năm,
        /// lần chạy trước, mốc thời gian / mốc khóa lần trước). Không dùng @lastTime/@lastKey: chế độ nối đuôi nhận ra qua @since/@afterKey.
        /// </summary>
        private const string AllTypesSql =
            "SELECT TOP (@pageSize) UpdateTime AS __lastTime, ID AS __lastKey, Code AS code FROM ShareDataPacket "
            + "WHERE ID = @onlyId AND Status >= @minStatus AND @ratio > 1 AND @enabled = 1 "
            + "AND UpdateTime >= @fromDay AND UpdateTime < @until AND UpdateTime <= @runAt AND @yearStart <= @runAt "
            + "AND (@prevRun IS NULL OR @prevRun < @runAt) "
            + "AND (@since IS NULL OR UpdateTime > @since OR (UpdateTime = @since AND ID > @afterKey)) "
            + "ORDER BY UpdateTime, ID";

        /// <summary>
        /// Description: Một lượt ProcessSubscriptions với câu SQL dùng nhiều biến, nhiều kiểu và nhiều nguồn giá trị:
        ///              - common-all-types: bộ biến mẫu chung đúng hết → gửi đúng dòng, nhận là nối đuôi nên mốc ShareDataLastSend tiến tới dòng vừa gửi.
        ///              - common-filter-out: một hằng số (@minStatus = 99) loại dòng → không gửi, mốc giữ nguyên (chứng minh giá trị biến được gắn thật).
        ///              - partner-own-set: mẫu chung trỏ sai dòng, đối tác có SQL riêng với bộ biến riêng trỏ đúng → gửi được (Worker dùng bộ biến của SQL riêng).
        /// Created date: 25/10/2026
        /// </summary>
        [Theory]
        [InlineData("common-all-types", true)]
        [InlineData("common-filter-out", false)]
        [InlineData("partner-own-set", true)]
        public async Task ProcessSubscriptions_WhenSqlUsesDeclaredVariables_BindsEveryTypeAndSource_Test(string scenario, bool expectSent)
        {
            // Arrange
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var unique = Guid.NewGuid().ToString("N")[..8];
            var packetId = $"pkt-var-{unique}";
            var packetCode = $"PKT_VAR_{unique}";
            var initialLastTime = new DateTime(2026, 10, 20, 8, 0, 0);
            var rowTime = initialLastTime.AddMinutes(5);

            await db.Insertable(new ShareDataPacket
            {
                ID = packetId,
                Code = packetCode,
                Name = $"Packet {packetCode}",
                PacketVersion = "1.0",
                Status = BaseEnums.StatusEnum.Enable
            }).ExecuteCommandAsync();
            await db.Updateable<ShareDataPacket>()
                .SetColumns(p => p.UpdateTime == rowTime)
                .Where(p => p.ID == packetId)
                .ExecuteCommandAsync();
            await ShareDataSqlSeed.SetCommonAsync(db, packetId, AllTypesSql, "UpdateTime");

            var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
            {
                PartnerCode = $"P_VAR_{unique}",
                SubCode = $"SUB_VAR_{unique}",
                DatatypeId = packetId,
                MappingShape = new Dictionary<string, object> { ["code"] = new Dictionary<string, string> { ["$field"] = "code" } },
                ConfigureSub = s => s.LastTimeRun = initialLastTime
            });

            var commonOnlyId = scenario == "partner-own-set" ? "NOT_EXIST" : packetId;
            var minStatus = scenario == "common-filter-out" ? "99" : "1";
            await db.Insertable(BuildVariableSet(packetId, null, commonOnlyId, minStatus)).ExecuteCommandAsync();
            if (scenario == "partner-own-set")
            {
                await db.Insertable(new ShareDataSql { ID = Guid.NewGuid().ToString("N"), PacketId = packetId, PartnerId = partner.ID, ExtractSql = AllTypesSql })
                    .ExecuteCommandAsync();
                await db.Insertable(BuildVariableSet(packetId, partner.ID, packetId, "1")).ExecuteCommandAsync();
            }

            await db.Insertable(new ShareDataLastSend
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerCode = partner.Code,
                PacketCode = packetCode,
                LastTime = initialLastTime,
                LastKey = string.Empty
            }).ExecuteCommandAsync();

            // Act
            await CreateWorker(scope).ProcessSubscriptions(CancellationToken.None);

            // Assert
            var received = _host.PartnerServer.ReceivedRequests.Count(r => r.Body.Contains(packetCode));
            var lastSend = await db.Queryable<ShareDataLastSend>()
                .Where(c => c.PartnerCode == partner.Code && c.PacketCode == packetCode)
                .FirstAsync();
            var alerts = await db.Queryable<ShareDataAlertLog>().Where(a => a.SubscriptionId == sub.ID).Select(a => a.Message).ToListAsync();

            if (expectSent)
            {
                Assert.True(received == 1, $"Đối tác phải nhận đúng 1 lần gửi, nhận {received}. Cảnh báo: {string.Join(" | ", alerts)}");
                Assert.Equal(rowTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
                Assert.Equal(packetId, lastSend.LastKey);
            }
            else
            {
                Assert.Equal(0, received);
                Assert.Equal(initialLastTime, lastSend.LastTime!.Value, TimeSpan.FromSeconds(1));
                Assert.Empty(alerts);
            }
        }

        /// <summary>Bộ biến cho AllTypesSql của một câu SQL (partnerId trống = mẫu chung).</summary>
        private static List<ShareDataSqlVariable> BuildVariableSet(string packetId, string? partnerId, string onlyId, string minStatus)
        {
            ShareDataSqlVariable Const(string code, string type, string value, int order) => new()
            {
                ID = Guid.NewGuid().ToString("N"), PacketId = packetId, PartnerId = partnerId, Code = code,
                DataType = type, ValueKind = ShareDataConst.SqlVariableKindConst, Value = value, OrderNo = order
            };
            ShareDataSqlVariable Builtin(string code, string type, string source, int order) => new()
            {
                ID = Guid.NewGuid().ToString("N"), PacketId = packetId, PartnerId = partnerId, Code = code,
                DataType = type, ValueKind = ShareDataConst.SqlVariableKindBuiltin, Value = source, OrderNo = order
            };

            return
            [
                Const("onlyId", ShareDataConst.SqlVariableTypeString, onlyId, 1),
                Const("minStatus", ShareDataConst.SqlVariableTypeInt, minStatus, 2),
                Const("ratio", ShareDataConst.SqlVariableTypeDecimal, "1.5", 3),
                Const("enabled", ShareDataConst.SqlVariableTypeBool, "true", 4),
                Const("fromDay", ShareDataConst.SqlVariableTypeDate, "2026-01-01", 5),
                Const("until", ShareDataConst.SqlVariableTypeDateTime, "2099-01-01 00:00:00", 6),
                Builtin("runAt", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceNow, 7),
                Builtin("yearStart", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceStartOfYear, 8),
                Builtin("prevRun", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceLastRunTime, 9),
                Builtin("since", ShareDataConst.SqlVariableTypeDateTime, ShareDataConst.SqlVariableSourceLastTime, 10),
                Builtin("afterKey", ShareDataConst.SqlVariableTypeString, ShareDataConst.SqlVariableSourceLastKey, 11)
            ];
        }

        #endregion
    }
}
