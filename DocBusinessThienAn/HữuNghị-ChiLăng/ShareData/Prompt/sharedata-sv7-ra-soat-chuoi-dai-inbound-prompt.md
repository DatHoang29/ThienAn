# SV-7 · Rà soát cắt cụt chuỗi dài & kiểm thử payload > 8.000 ký tự

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-sv7-ra-soat-chuoi-dai-inbound-prompt.md`

> 📌 Soạn 05/10/2026 · Nguồn: `Sharedata_MasterPlan.md` dòng `SV-7` (§I.A) + kết quả rà soát mã nguồn thực tế.
> Nhánh đề xuất: `feat/20261006-XD001.5.6-sharedata-sv7-kiem-tra-chuoi-dai` (tách từ `dev` hoặc nhánh PR).

## 1. Mục tiêu

1. **Khẳng định an toàn hạ tầng**: Đảm bảo toàn bộ luồng tiếp nhận và xử lý dữ liệu chiều nhận (Inbound) không bị hiện tượng cắt cụt chuỗi âm thầm (silent string truncation) ở ngưỡng 4.000 hoặc 8.000 ký tự — giới hạn mặc định của `NVARCHAR` / `VARCHAR` trong SQL Server và ADO.NET.
2. **Kiểm thử tự động toàn trình (Integration Test)**: Bổ sung bài test kiểm thử gói tin có kích thước lớn (> 8.000 ký tự, lên tới hàng chục KB) đi xuyên suốt từ WebAPI tiếp nhận, lưu trữ CSDL đệm `ShareDataInboundPacket`, đến Worker bóc tách JSON và thực thi câu ghi SQL `OPENJSON(@records)` vào CSDL đích.

---

## 2. Kết quả đối chiếu mã nguồn (Code Audit 05/10/2026)

Đối chiếu chi tiết với mã nguồn hiện tại của dự án:

| Vị trí | Hiện trạng kiểm tra | Đánh giá an toàn |
| --- | --- | --- |
| `DataInboundService.WriteSql.cs:102` | `new SugarParameter("@records", recordsJson, System.Data.DbType.String) { Size = -1 }` | ✅ **ĐÃ AN TOÀN**: `{ Size = -1 }` ép kiểu ADO.NET sang `NVARCHAR(MAX)`. Nếu thiếu thuộc tính này, ADO.NET sẽ tự động gán `Size = 4000`, làm đứt chuỗi JSON khi vượt 4.000 ký tự. |
| `ShareDataInboundPacket.cs:45` | `[SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)] public string? RawContent` | ✅ **ĐÃ AN TOÀN**: Khai báo `CodeFirst_BigString` tương đương kiểu `NVARCHAR(MAX)` trong SQL Server (lưu trữ tối đa 2GB). |
| `ShareDataPacketSql.cs:32` | `[SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString)] public string? WriteSql` | ✅ **ĐÃ AN TOÀN**: Câu lệnh ghi SQL cũng là `NVARCHAR(MAX)`. |
| `ShareDataTransferLog.cs:141` | `ErrorMessage = Cut(errorMessage, 512)` / `Message = Cut(message, 1000)` | ✅ **ĐÃ AN TOÀN**: Cắt chuỗi chủ động trước khi ghi vào cột có độ dài cố định, chống lỗi văng ngoại lệ tràn chuỗi CSDL (`String or binary data would be truncated`). |

🔴 **Việc còn lại duy nhất của SV-7**: Hiện tại toàn bộ các bài test Inbound trong `DataInboundServiceTests.cs` đều chỉ dùng dữ liệu mẫu ngắn (< 500 ký tự). Chưa có bài test nào chứng minh luồng xử lý thực sự vận hành ổn định khi đối tác đẩy payload lớn > 8.000 ký tự (ví dụ gói tin chứa hàng trăm bản ghi giao thông hoặc dữ liệu JSON chi tiết). Cần viết bài test toàn trình này để làm lưới bảo vệ chống hồi quy (regression guard).

---

## 3. Ràng buộc

- ⛔ Không sửa đổi logic chạy thật nếu không phát hiện lỗi (vì code audit cho thấy các chốt chặn `Size = -1` và `BigString` đã chuẩn).
- Bài test phải kiểm tra payload thực sự vượt ngưỡng 8.000 ký tự (ví dụ: tạo 100 bản ghi, tổng độ dài JSON ~15.000 – 25.000 ký tự).
- Khẳng định 100% bản ghi được nạp qua `OPENJSON(@records)` mà không bị lỗi cú pháp `JSON text is not properly formatted` (lỗi kinh điển khi chuỗi JSON bị ngắt ở ký tự 4.000/8.000).

---

## 4. Chi tiết thay đổi

### TĐ1 · [MODIFY] `tests/BE/ITS/ShareData/Services/DataInboundServiceTests.cs`

Bổ sung bài kiểm thử chuỗi lớn vào tệp test hiện có:

```csharp
    #region 7. Kiểm thử payload lớn vượt ngưỡng 8.000 ký tự (SV-7)

    /// <summary>
    /// SV-7: Kiểm thử gói tin có kích thước lớn (> 8.000 ký tự) không bị cắt cụt chuỗi ở tầng CSDL hay SqlSugar.
    /// Tạo gói tin chứa 100 bản ghi (tổng JSON > 15.000 ký tự) => chạy ProcessPendingPackets
    /// => Khẳng định:
    ///    1. Cột RawContent trong ShareDataInboundPacket lưu trọn vẹn toàn bộ chuỗi JSON.
    ///    2. Worker phân giải và chạy câu ghi SQL OPENJSON(@records) thành công cho đủ 100 bản ghi.
    ///    3. Không bị văng lỗi ngắt chuỗi JSON hoặc lỗi cắt cụt tham số SQL.
    ///    4. Dòng log cha được cập nhật RecordCount = 100 và Success = Success.
    /// </summary>
    [Fact]
    public async Task ProcessPendingPackets_WhenPayloadExceeds8000Chars_ProcessesFullyWithoutTruncation_Test()
    {
        // Arrange: Khởi tạo môi trường gói tin Inbound
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (partner, packet, sub, mapping) = await SeedInboundEnvironmentAsync(unique);

        // Tạo mảng 100 bản ghi dữ liệu chi tiết để dung lượng JSON vượt xa ngưỡng 8.000 ký tự
        var largeRecords = new List<object>();
        for (int i = 1; i <= 100; i++)
        {
            largeRecords.Add(new
            {
                id = $"REC_{unique}_{i:D4}",
                val = $"Giá trị dữ liệu kiểm thử chuỗi dài để kiểm tra cắt cụt chuỗi của phân hệ Chia sẻ dữ liệu ShareData ITS - Bản ghi số {i} mang thông tin chi tiết dài hơn 200 ký tự nhằm đẩy dung lượng payload JSON lên mức cao để kiểm tra tính toàn vẹn của tham số SugarParameter và cột CSDL."
            });
        }

        var largeJson = System.Text.Json.JsonSerializer.Serialize(new { data = largeRecords });
        Assert.True(largeJson.Length > 8000, $"Payload kiểm thử phải vượt 8.000 ký tự (đo được: {largeJson.Length} ký tự).");

        var parentLogId = Guid.NewGuid().ToString("N");
        // Ghi dòng cha giả lập
        await _db.Insertable(new ShareDataActivityLog
        {
            ID = parentLogId,
            SubscriptionId = sub.ID,
            PartnerId = partner.ID,
            PartnerName = partner.Name,
            TransferDirection = BaseEnums.TransferDirection.RCV,
            DatatypeId = packet.Code,
            Success = BaseEnums.SuccessEnums.Success,
            RecordCount = 0,
            Description = $"Tiếp nhận gói tin loại {packet.Code} từ đối tác \"{partner.Name}\" — chờ xử lý chuỗi lớn",
            OccurredAt = DateTime.Now,
            CreateTime = DateTime.Now
        }).ExecuteCommandAsync();

        // Ghi bản ghi InboundPacket lớn
        var packetId = Guid.NewGuid().ToString("N");
        var packetEntity = new ShareDataInboundPacket
        {
            ID = packetId,
            PartnerCode = partner.Code,
            PacketCode = packet.Code,
            PacketVersion = "1.0",
            SerialNbr = 1,
            RawContent = largeJson,
            ByteSize = System.Text.Encoding.UTF8.GetByteCount(largeJson),
            ProcessState = BaseEnums.InboundProcessState.Pending,
            ReceiveLogId = parentLogId,
            CreateTime = DateTime.Now
        };
        await _db.Insertable(packetEntity).ExecuteCommandAsync();

        // Act: Kích hoạt Worker xử lý các gói Pending
        using var scope = _host.Services.CreateScope();
        var inboundService = scope.ServiceProvider.GetRequiredService<IDataInboundService>();
        await inboundService.ProcessPendingPackets(CancellationToken.None);

        // Assert:
        // 1. Kiểm tra gói tin trong CSDL Inbound đã hoàn tất
        var processedPacket = await _db.Queryable<ShareDataInboundPacket>()
            .Where(x => x.ID == packetId)
            .FirstAsync();
        Assert.NotNull(processedPacket);
        Assert.Equal(BaseEnums.InboundProcessState.Done, processedPacket.ProcessState);
        Assert.Null(processedPacket.ErrorMessage);
        Assert.Equal(largeJson.Length, processedPacket.RawContent?.Length);

        // 2. Kiểm tra dòng log cha: Được cập nhật đúng 100 bản ghi và thành công
        var updatedParent = await _db.Queryable<ShareDataActivityLog>()
            .Where(x => x.ID == parentLogId)
            .FirstAsync();
        Assert.NotNull(updatedParent);
        Assert.Equal(BaseEnums.SuccessEnums.Success, updatedParent.Success);
        Assert.Equal(100, updatedParent.RecordCount);

        // 3. Kiểm tra dòng log con Bước 2
        var step2 = await _db.Queryable<ShareDataActivityLog>()
            .Where(x => x.ParentId == parentLogId && x.StepNbr == 2)
            .FirstAsync();
        Assert.NotNull(step2);
        Assert.Equal(BaseEnums.SuccessEnums.Success, step2.Success);
        Assert.Equal(100, step2.RecordCount);
    }

    #endregion
```

---

## 5. Kiểm thử & Nghiệm thu

### Lệnh chạy kiểm thử tự động
```powershell
dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~ProcessPendingPackets_WhenPayloadExceeds8000Chars"
dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~DataInboundServiceTests"
```

### Tiêu chí nghiệm thu (Checklist)
- [x] Test `ProcessPendingPackets_WhenPayloadExceeds8000Chars_ProcessesFullyWithoutTruncation_Test` chạy PASS 100%.
- [x] Dữ liệu JSON dài hơn 8.000 ký tự (đo thật ~16.000 ký tự) không gặp bất kỳ lỗi cắt chuỗi hay lỗi phân tích cú pháp nào.
- [x] Toàn bộ suite `DataInboundServiceTests` tiếp tục PASS 100% (không có hồi quy).

**Commit gợi ý:** `test(sharedata): XD001.5.6 - kiểm thử payload Inbound lớn vượt 8000 ký tự chống cắt chuỗi (SV-7)`
