using System.Text.Json;
using Module.ShareData.Core.Entities;
using ShareDataWorker.Core.Dto;
using ShareDataWorker.Core.Entities;
using ShareDataWorker.Core.Enums;
using ShareDataWorker.Core.Interfaces;
using ShareDataWorker.Infrastructure.Services.DataExport;
using ShareDataWorker.Infrastructure.Services.DataImport;
using SqlSugar;
using Xunit;

namespace Tests.Modules.ShareData.Infrastructure.Services.DataImport
{
    [Collection("api")]
    public class DataImportServiceTests(Host host)
    {
        private readonly Host _host = host;

        [Fact]
        public void MapCodeReverse_ReturnsExpectedValues_Test()
        {
            var codeValues = new List<CodeValueDto>
            {
                new() { SourceValue = "1", StandardValue = "slow" },
                new() { SourceValue = "2", StandardValue = "normal" },
                new() { SourceValue = "default_val", StandardValue = "unknown", IsDefault = true }
            };

            var warningLogged = false;

            // 1. Khớp standardValue -> trả sourceValue
            var res1 = DataImportService.MapCodeReverse(codeValues, "slow");
            Assert.Equal("1", res1);

            var res2 = DataImportService.MapCodeReverse(codeValues, "normal");
            Assert.Equal("2", res2);

            // 2. Không khớp nhưng có isDefault -> trả sourceValue của default
            var res3 = DataImportService.MapCodeReverse(codeValues, "congested");
            Assert.Equal("default_val", res3);

            // 3. Không khớp và không có isDefault -> trả nguyên value + cảnh báo
            var codeValuesNoDefault = new List<CodeValueDto>
            {
                new() { SourceValue = "1", StandardValue = "slow" }
            };
            var res4 = DataImportService.MapCodeReverse(codeValuesNoDefault, "unknown_code", "CodeSetA", (c, v) => warningLogged = true);
            Assert.Equal("unknown_code", res4);
            Assert.True(warningLogged);
        }

        [Fact]
        public void Transform_And_TransformInbound_Packet101_Roundtrip_Test()
        {
            // 1. Khai báo metadata fields cho gói 101 (TmsZoneStatus)
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId", DataType = "string", Required = true },
                new() { FieldKey = "condition", Column = "Condition", DataType = "string", CodeSetCode = "TrafficCondition" },
                new() { FieldKey = "averageSpeed", Column = "AverageSpeed", DataType = "decimal", Unit = "km/h" },
                new() { FieldKey = "occupancy", Column = "Occupancy", DataType = "decimal" }
            };

            // 2. Khai báo quy tắc mapping
            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "zoneId", TargetKey = "MaKhuVuc", TargetEntity = "KhuVuc" },
                new() { FieldKey = "condition", TargetKey = "TrangThai", CodeSetId = "PartnerTrafficCondition" },
                new() { FieldKey = "averageSpeed", TargetKey = "VanToc", TargetUnit = "m/s" },
                new() { FieldKey = "occupancy", TargetKey = "MatDo" }
            };

            // 3. Khai báo bộ mã 2 tầng (Chuẩn & Đối tác)
            var codeSetsDict = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["TrafficCondition"] =
                [
                    new() { SourceValue = "1", StandardValue = "slow" },
                    new() { SourceValue = "2", StandardValue = "normal" }
                ],
                ["PartnerTrafficCondition"] =
                [
                    new() { SourceValue = "slow", StandardValue = "Chậm" },
                    new() { SourceValue = "normal", StandardValue = "Bình thường" }
                ]
            };

            // 4. Dữ liệu nguồn ban đầu
            var rawDatabaseRows = new List<object>
            {
                new Dictionary<string, object?>
                {
                    ["zoneId"] = "ZONE_101_HN",
                    ["condition"] = "1",
                    ["averageSpeed"] = 62.5m,
                    ["occupancy"] = 45.2m
                }
            };

            // 5. CHIỀU GỬI (Outbound Transform)
            var outboundRecords = DataExportService.Transform(
                rawDatabaseRows,
                declaredFields,
                mappingItems,
                targetRootEntity: "TmsZoneStatus",
                codeSets: codeSetsDict);

            Assert.Single(outboundRecords);

            var outboundJson = JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["TmsZoneStatus"] = outboundRecords
            });

            // 6. CHIỀU NHẬN (Inbound Transform từ JSON vừa sinh)
            using var doc = JsonDocument.Parse(outboundJson);
            var inboundResult = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems,
                targetRootEntity: "TmsZoneStatus",
                codeSets: codeSetsDict);

            // 7. KHẲNG ĐỊNH KHỨ HỒI: Kết quả chiều nhận phải phục hồi 100% dữ liệu gốc
            Assert.Equal(1, inboundResult.AcceptedCount);
            Assert.Equal(0, inboundResult.RejectedCount);
            Assert.Empty(inboundResult.Errors);

            var recoveredRow = inboundResult.Rows[0];

            Assert.Equal("ZONE_101_HN", recoveredRow["ZoneId"]);
            Assert.Equal("1", recoveredRow["Condition"]);
            Assert.Equal(45.2m, Convert.ToDecimal(recoveredRow["Occupancy"]));

            // Lưu ý: Quy đổi đơn vị không khả nghịch tuyệt đối ở 4 chữ số thập phân (ví dụ: 80 km/h -> 22.2222 m/s -> 79.9999 km/h). Dung sai < 0.01m bảo đảm độ chính xác ~4 chữ số thập phân thay vì so khớp bit-exact.
            // Tốc độ: 62.5 km/h -> 17.3611 m/s (chiều gửi) -> 62.5 km/h (chiều nhận)
            var recoveredSpeed = Convert.ToDecimal(recoveredRow["AverageSpeed"]);
            Assert.True(Math.Abs(62.5m - recoveredSpeed) < 0.01m, $"Expected 62.5, got {recoveredSpeed}");
        }

        [Fact]
        public void TransformInbound_UnitConversion_ReversesUnits_Test()
        {
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "speed", Column = "SpeedCol", DataType = "decimal", Unit = "km/h" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "speed", TargetKey = "v", TargetUnit = "m/s" }
            };

            // Payload chứa 17.3611 m/s
            var json = "{\"Data\": [{\"v\": 17.3611}]}";
            using var doc = JsonDocument.Parse(json);

            var result = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems,
                targetRootEntity: "Data");

            Assert.Equal(1, result.AcceptedCount);
            var val = Convert.ToDecimal(result.Rows[0]["SpeedCol"]);
            Assert.True(Math.Abs(62.5m - val) < 0.01m);
        }

        [Fact]
        public void TransformInbound_WhenRequiredFieldMissingInOneRow_RejectsOnlyThatRow_Test()
        {
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId", DataType = "string", Required = true },
                new() { FieldKey = "speed", Column = "Speed", DataType = "decimal" }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "zoneId", TargetKey = "z" },
                new() { FieldKey = "speed", TargetKey = "s" }
            };

            // 3 dòng: Dòng 0 hợp lệ, Dòng 1 thiếu Required "z", Dòng 2 hợp lệ
            var json = """
            {
                "Zones": [
                    { "z": "Z01", "s": 50 },
                    { "s": 60 },
                    { "z": "Z03", "s": 70 }
                ]
            }
            """;

            using var doc = JsonDocument.Parse(json);
            var result = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems,
                targetRootEntity: "Zones");

            Assert.Equal(2, result.AcceptedCount);
            Assert.Equal(1, result.RejectedCount);
            Assert.Single(result.Errors);
            Assert.Equal(1, result.Errors[0].RowIndex);
            Assert.Contains("zoneId", result.Errors[0].ErrorMessages[0]);

            Assert.Equal("Z01", result.Rows[0]["ZoneId"]);
            Assert.Equal("Z03", result.Rows[1]["ZoneId"]);
        }

        [Fact]
        public void TransformInbound_WhenFieldIsExcluded_IgnoresFieldAndDoesNotCheckRequired_Test()
        {
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                new() { FieldKey = "secretCode", Column = "SecretCode", Required = true }
            };

            var mappingItems = new List<MappingItemDto>
            {
                new() { FieldKey = "zoneId", TargetKey = "z" },
                new() { FieldKey = "secretCode", IsExcluded = true }
            };

            // Payload chỉ có "z", không có "secretCode"
            var json = "{\"Data\": [{\"z\": \"Z01\"}]}";
            using var doc = JsonDocument.Parse(json);

            var result = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems,
                targetRootEntity: "Data");

            Assert.Equal(1, result.AcceptedCount);
            Assert.Equal(0, result.RejectedCount);
            Assert.False(result.Rows[0].ContainsKey("SecretCode"));
        }

        [Fact]
        public void TransformInbound_WhenFieldHasCodeSet_DoesNotCoerceToNumeric_Test()
        {
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "status", Column = "StatusCode", DataType = "int", CodeSetCode = "StatusSet" }
            };

            var codeSetsDict = new Dictionary<string, List<CodeValueDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["StatusSet"] =
                [
                    new() { SourceValue = "100", StandardValue = "active" }
                ]
            };

            var json = "{\"Data\": [{\"status\": \"active\"}]}";
            using var doc = JsonDocument.Parse(json);

            var result = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems: null,
                targetRootEntity: "Data",
                codeSets: codeSetsDict);

            Assert.Equal(1, result.AcceptedCount);
            Assert.Equal("100", result.Rows[0]["StatusCode"]);
        }

        [Fact]
        public void TransformInbound_WhenPayloadStructureIsInvalid_ReturnsEmptyAndLogsEsh1501_Test()
        {
            var declaredFields = new List<PacketFieldDto>
            {
                new() { FieldKey = "zoneId", Column = "ZoneId" }
            };

            var alertCode = string.Empty;

            // Payload không chứa mảng
            var json = "{\"Data\": \"invalid_string_not_array\"}";
            using var doc = JsonDocument.Parse(json);

            var result = DataImportService.TransformInbound(
                doc.RootElement,
                declaredFields,
                mappingItems: null,
                targetRootEntity: "Data",
                onInvalidStructure: (code, msg) => alertCode = code);

            Assert.Equal(0, result.AcceptedCount);
            Assert.Equal(0, result.RejectedCount);
            Assert.Equal("ESH-1501", alertCode);
        }

        [Fact]
        public async Task ReceivePacket_WithActiveInboundMapping_RecordsReceiveActivityLog_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = scope.ServiceProvider.GetRequiredService<IDataImportService>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partnerId = $"P_IN_{uniqueId}";
            var packetCode = $"PKT_IN_{uniqueId}";

            await db.Insertable(new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = $"Partner Inbound {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet Inbound {uniqueId}",
                PacketVersion = "1.0",
                FilterMode = (int)ShareDataEnum.PacketFilterMode.Snapshot,
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                IsActive = true,
                FieldsJson = JsonSerializer.Serialize(new List<PacketFieldDto>
                {
                    new() { FieldKey = "zoneId", Column = "ZoneId", Required = true },
                    new() { FieldKey = "speed", Column = "AverageSpeed" }
                })
            }).ExecuteCommandAsync();

            var mappingId = Guid.NewGuid().ToString("N");
            await db.Insertable(new ShareDataMapping
            {
                ID = mappingId,
                PartnerId = partnerId,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Inbound,
                TargetRootEntity = "TmsZoneStatus",
                IsActive = true,
                ItemsJson = JsonSerializer.Serialize(new List<MappingItemDto>
                {
                    new() { FieldKey = "zoneId", TargetKey = "zId" },
                    new() { FieldKey = "speed", TargetKey = "vSpeed" }
                })
            }).ExecuteCommandAsync();

            var payload = """
            {
                "TmsZoneStatus": [
                    { "zId": "ZONE_01", "vSpeed": "80" }
                ]
            }
            """;

            var result = await service.ReceivePacket(partnerId, packetCode, payload, CancellationToken.None);

            Assert.Equal(1, result.AcceptedCount);
            Assert.Equal(0, result.RejectedCount);

            var logs = await db.Queryable<ShareDataActivityLog>()
                .Where(l => l.PartnerId == partnerId && l.DatatypeId == packetCode && l.Action == ShareDataEnum.LogAction.Receive)
                .ToListAsync();

            Assert.Single(logs);
            Assert.Equal(ShareDataEnum.TransferDirection.Receive, logs[0].TransferDirection);
            Assert.Equal(1, logs[0].RecordCount);
            Assert.Equal(mappingId, logs[0].MappingId);
            Assert.Equal("1.0", logs[0].PacketVersion);
            Assert.Equal(ShareDataEnum.ExportStatus.Success, logs[0].Status);
        }

        [Fact]
        public async Task ReceivePacket_WhenNoInboundMapping_RejectsAndLogsEsh1301_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = scope.ServiceProvider.GetRequiredService<IDataImportService>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partnerId = $"P_NOMAP_{uniqueId}";
            var packetCode = $"PKT_NOMAP_{uniqueId}";

            await db.Insertable(new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = $"Partner NoMap {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet NoMap {uniqueId}",
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            var payload = "{\"Data\": []}";
            var result = await service.ReceivePacket(partnerId, packetCode, payload, CancellationToken.None);

            Assert.Equal(0, result.AcceptedCount);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.PartnerId == partnerId && a.AlertCode == "ESH-1301")
                .ToListAsync();

            Assert.NotEmpty(alerts);
            Assert.Equal("error", alerts[0].Severity);
            Assert.Contains("Không tìm thấy cấu hình ánh xạ INBOUND", alerts[0].Message);
        }

        [Fact]
        public async Task ReceivePacket_WhenPartnerDisabled_RejectsAndLogsEsh1301_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = scope.ServiceProvider.GetRequiredService<IDataImportService>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partnerId = $"P_DIS_{uniqueId}";
            var packetCode = $"PKT_DIS_{uniqueId}";

            await db.Insertable(new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = $"Partner Disabled {uniqueId}",
                Status = BaseEnums.StatusEnum.Disable,
                SessionState = BaseEnums.SessionState.Disconnected,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            var payload = "{\"Data\": []}";
            var result = await service.ReceivePacket(partnerId, packetCode, payload, CancellationToken.None);

            Assert.Equal(0, result.AcceptedCount);

            var alerts = await db.Queryable<ShareDataAlertLog>()
                .Where(a => a.PartnerId == partnerId && a.AlertCode == "ESH-1301")
                .ToListAsync();

            Assert.NotEmpty(alerts);
            Assert.Contains("vô hiệu hóa", alerts[0].Message);
        }

        [Fact]
        public async Task ReceivePacket_WhenNoWriterRegistered_UsesNullInboundWriter_Test()
        {
            using var scope = _host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var service = scope.ServiceProvider.GetRequiredService<IDataImportService>();

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partnerId = $"P_NULLW_{uniqueId}";
            var packetCode = $"PKT_NULLW_{uniqueId}";

            await db.Insertable(new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = $"Partner NullWriter {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet NullWriter {uniqueId}",
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"ZoneId\"}]"
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partnerId,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Inbound,
                TargetRootEntity = "Data",
                IsActive = true,
                ItemsJson = "[{\"fieldKey\":\"zoneId\",\"targetKey\":\"z\"}]"
            }).ExecuteCommandAsync();

            var payload = "{\"Data\": [{\"z\": \"Z01\"}]}";
            var result = await service.ReceivePacket(partnerId, packetCode, payload, CancellationToken.None);

            Assert.Equal(1, result.AcceptedCount);
            Assert.Equal("Z01", result.Rows[0]["ZoneId"]);
        }

        [Fact]
        public async Task DataImportService_WhenCalledConcurrently_NoSqlSugarThreadingError_Test()
        {
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var partnerId = $"P_CONC_{uniqueId}";
            var packetCode = $"990_{uniqueId}";

            using var setupScope = _host.Services.CreateScope();
            var db = setupScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();

            await db.Insertable(new ShareDataPartner
            {
                ID = partnerId,
                Code = partnerId,
                Name = $"Partner Conc {uniqueId}",
                Status = BaseEnums.StatusEnum.Enable,
                SessionState = BaseEnums.SessionState.Connected,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataPacket
            {
                ID = Guid.NewGuid().ToString("N"),
                Code = packetCode,
                Name = $"Packet Conc {uniqueId}",
                PacketVersion = "1.0",
                IsActive = true
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataTable
            {
                ID = Guid.NewGuid().ToString("N"),
                PacketCode = packetCode,
                Alias = "zs",
                TableName = "TmsZoneStatus",
                IsRoot = true,
                OrderNo = 1,
                IsActive = true,
                FieldsJson = "[{\"fieldKey\":\"zoneId\",\"column\":\"ZoneId\",\"required\":true}]"
            }).ExecuteCommandAsync();

            await db.Insertable(new ShareDataMapping
            {
                ID = Guid.NewGuid().ToString("N"),
                PartnerId = partnerId,
                DatatypeId = packetCode,
                Direction = ShareDataEnum.SubDirection.Inbound,
                TargetRootEntity = "Data",
                IsActive = true,
                ItemsJson = "[{\"fieldKey\":\"zoneId\",\"targetKey\":\"z\"}]"
            }).ExecuteCommandAsync();

            // Lấy IDataImportService một lần từ root provider
            var service = _host.Services.GetRequiredService<IDataImportService>();

            var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
            var results = new System.Collections.Concurrent.ConcurrentBag<InboundResult>();

            var tasks = Enumerable.Range(0, 20).Select(async taskId =>
            {
                try
                {
                    var rowsJson = string.Join(",", Enumerable.Range(0, 5).Select(i => $"{{\"z\": \"Z_{taskId}_{i}\"}}"));
                    var payload = $"{{\"Data\": [{rowsJson}]}}";
                    var res = await service.ReceivePacket(partnerId, packetCode, payload, CancellationToken.None);
                    results.Add(res);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
            Assert.Equal(100, results.Sum(r => r.AcceptedCount));

            using var checkScope = _host.Services.CreateScope();
            var checkDb = checkScope.ServiceProvider.GetRequiredService<ISqlSugarClient>();
            var logs = await checkDb.Queryable<ShareDataActivityLog>()
                .Where(l => l.PartnerId == partnerId)
                .Where(l => l.Action == ShareDataEnum.LogAction.Receive)
                .ToListAsync();
            Assert.Equal(20, logs.Count);
        }
    }
}
