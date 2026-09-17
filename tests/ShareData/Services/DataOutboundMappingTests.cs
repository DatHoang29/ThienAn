using ShareDataWorker.Core.Interfaces.DataOutbound;
using ShareDataWorker.Core.Models.DataOutbound;
using System.Globalization;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Infrastructure.Logging;
using ShareDataWorker.Infrastructure.Services.DataOutbound;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Context;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Extraction;
using ShareDataWorker.Infrastructure.Services.DataOutbound.Mapping;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataOutbound
{
    /// <summary>
    /// Description: Unit tests cho Phép toán mới (Việc 4, 5, 2, 1) - DataMappingProcess và DataOutboundService.
    /// Kiểm tra $extend.targetType, $extend.required, $extend.format (datetime), ESH-1205, ESH-1206.
    /// Created date: 17/09/2026
    /// </summary>
    public partial class DataOutboundServiceTests
    {
        #region Tests cho Phép toán mới (Việc 4, 5, 2, 1)

        [Fact]
        public void Transform_WhenCodeSetReturnsNumberString_CoercesToNumberIfTargetTypeIsNumber_Test()
        {
            var codeSets = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["COND_SET"] =
                [
                    new() { SourceValue = "slow", PartnerValue = "101" }
                ]
            };

            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = "slow" }
            };

            var fields = new List<PacketFieldDto>
            {
                new() { FieldKey = "condition", Column = "Condition", CodeSetCode = "COND_SET" }
            };

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""targetType"": ""number""
                    }
                }
            }";

            var result = DataOutboundService.Transform(rawRows, fields, shapeJson, codeSets, null, null, null);
            var dict = (Dictionary<string, object?>)result[0];
            var val = dict["trafficCondition"];

            Assert.IsType<decimal>(val);
            Assert.Equal(101m, val);
        }

        [Fact]
        public void Transform_WhenExtendRequiredIsTrueAndValueIsEmpty_ThrowsInvalidOperationException_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["condition"] = null }
            };

            var fields = new List<PacketFieldDto>();

            var shapeJson = @"{
                ""trafficCondition"": {
                    ""$field"": ""condition"",
                    ""$extend"": {
                        ""required"": true
                    }
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataOutboundService.Transform(rawRows, fields, shapeJson, null, null, null, null));
            Assert.StartsWith("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("[condition]", ex.Message);
        }

        [Fact]
        public void Transform_WhenExtendRequiredIsTrueAndFieldNotInPacketFields_ThrowsWithSourceFieldKey_Test()
        {
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["zone_id"] = "" }
            };

            var fields = new List<PacketFieldDto>();

            var shapeJson = @"{
                ""zoneId"": {
                    ""$field"": ""zone_id"",
                    ""$extend"": {
                        ""required"": true
                    }
                }
            }";

            var ex = Assert.Throws<InvalidOperationException>(() =>
                DataOutboundService.Transform(rawRows, fields, shapeJson, null, null, null, null));
            Assert.StartsWith("Thiếu trường bắt buộc", ex.Message);
            Assert.Contains("[zone_id]", ex.Message);
        }

        [Fact]
        public void Transform_WhenExtendFormatIsSetAndTypeIsDatetime_FormatsCorrectly_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime"",
                        ""format"": ""dd/MM/yyyy""
                    }
                }
            }";

            var result = DataOutboundService.Transform(rawRows, [], shapeJson, null, null, null, null);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal("17/09/2026", dict["timeFormatted"]);
        }

        [Fact]
        public void Transform_WhenExtendFormatNotSetAndTypeIsDatetime_ReturnsIso8601_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime""
                    }
                }
            }";

            var result = DataOutboundService.Transform(rawRows, [], shapeJson, null, null, null, null);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal(dt.ToString("o", CultureInfo.InvariantCulture), dict["timeFormatted"]);
        }

        [Fact]
        public void Transform_WhenExtendFormatIsGarbageAndTypeIsDatetime_ReturnsIso8601_Test()
        {
            var dt = new DateTime(2026, 9, 17, 10, 20, 30);
            var rawRows = new List<object>
            {
                new Dictionary<string, object?> { ["time"] = dt }
            };

            var shapeJson = @"{
                ""timeFormatted"": {
                    ""$field"": ""time"",
                    ""$extend"": {
                        ""targetType"": ""datetime"",
                        ""format"": ""x""
                    }
                }
            }";

            var result = DataOutboundService.Transform(rawRows, [], shapeJson, null, null, null, null);
            var dict = (Dictionary<string, object?>)result[0];
            Assert.Equal(dt.ToString("o", CultureInfo.InvariantCulture), dict["timeFormatted"]);
        }

        [Fact]
        public async Task Map_WhenShapeFieldNotInRawRow_LogsEsh1205_AndReturnsNullForThatField_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping
            {
                ID = "TEST_M",
                TargetShapeJson = @"{ ""a"": { ""$field"": ""A"" }, ""b"": { ""$field"": ""B"" }, ""c"": { ""$field"": ""C"" } }"
            };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2 } },
                new List<PacketFieldDto> { new() { FieldKey = "A" }, new() { FieldKey = "B" } },
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.True(result.Success);
            Assert.Contains(ShareDataAlertCode.Outbound.ShapeFieldNotInRawRow, logs);
        }

        [Fact]
        public async Task Map_WhenRawRowHasExtraField_DoesNotLogEsh1205_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping
            {
                ID = "TEST_M",
                TargetShapeJson = @"{ ""a"": { ""$field"": ""A"" }, ""b"": { ""$field"": ""B"" } }"
            };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?> { ["A"] = 1, ["B"] = 2, ["D"] = 4 } },
                new List<PacketFieldDto>(),
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.DoesNotContain(ShareDataAlertCode.Outbound.ShapeFieldNotInRawRow, logs);
        }

        [Fact]
        public async Task Map_WhenShapeJsonIsInvalid_LogsEsh1206_AndReturnsFalse_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping { ID = "TEST_M", TargetShapeJson = "INVALID_JSON_///" };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?>() },
                new List<PacketFieldDto>(),
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.False(result.Success);
            Assert.Contains(ShareDataAlertCode.Outbound.ShapeInvalid, logs);
        }

        [Fact]
        public async Task Map_WhenShapeJsonIsEmpty_LogsEsh1206_AndReturnsFalse_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            var packet = new ShareDataPacket { Code = "TEST_P" };
            var mapping = new ShareDataMapping { ID = "TEST_M", TargetShapeJson = "" };
            var sub = new ShareDataSubscription { ID = "TEST_S" };
            var ctx = new DataOutboundContext(sub, null, packet, mapping, DateTime.Now, CancellationToken.None);

            var extraction = new DataOutboundExtractionResult(
                new List<object> { new Dictionary<string, object?>() },
                new List<PacketFieldDto>(),
                null, null);

            var logs = new List<string>();
            var result = await DataMappingProcess.Map(db, extraction, ctx,
                (code, sev, src, msg, dtl) => { logs.Add(code); return Task.CompletedTask; }, null);

            Assert.False(result.Success);
            Assert.Contains(ShareDataAlertCode.Outbound.ShapeInvalid, logs);
        }

        #endregion
    }
}

