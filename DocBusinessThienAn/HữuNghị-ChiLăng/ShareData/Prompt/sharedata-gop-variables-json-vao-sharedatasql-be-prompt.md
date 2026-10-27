<!-- Đường dẫn tệp: DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Prompt/sharedata-gop-variables-json-vao-sharedatasql-be-prompt.md -->

# Prompt Backend: Gom bảng `ShareDataSqlVariable` thành cột `VariablesJson` trong `ShareDataSql`

## Bối cảnh & Mục tiêu
Bộ biến tham số câu truy vấn (`@lastKey`, `@pageSize`, `@now`...) đi liền 1–1 theo câu SQL (mẫu chung có 1 bộ, khi tạo SQL riêng cho đối tác thì clone nguyên bộ SQL + Fields + Variables, khi đặt lại về mẫu chung thì xóa nguyên bộ của đối tác) — hoàn toàn tương đồng với cột `FieldsJson` trên bảng `ShareDataSql`.

Thực hiện gộp `ShareDataSqlVariable` vào cột `VariablesJson` trên `ShareDataSql` và loại bỏ entity `ShareDataSqlVariable` để:
- Gom trọn cấu hình SQL của gói × đối tác về đúng **1 bảng duy nhất `ShareDataSql`** (`ExtractSql`, `FieldsJson`, `VariablesJson`, `WriteSql`, `AffectedTables`).
- Giữ **nguyên vẹn 100% hợp đồng API / DTO** (`GetPacketSqlVariablesInput`, `UpdatePacketSqlVariablesInput`, `PacketSqlVariablesOutput`, `ResetLastSendInput`) để **Frontend (`sqlVariablePanel.vue`) không phải sửa bất kỳ dòng nào**.

---

## Phạm vi thực hiện (Chỉ Backend, Script `.sql` & Test BE)

### 1. Entity & Model JSON (`Module.ShareData.Core`)
1. **`TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataSql.cs`**:
   - Bổ sung cột `VariablesJson` ngay cạnh `FieldsJson`:
     ```csharp
     /// <summary>Mảng SqlVariableItem (JSON): bộ biến @tên khai cho câu SQL của dòng này. Dòng đối tác chưa khai riêng (null) thì kế thừa mẫu chung; "[]" là đã lưu rỗng.</summary>
     [SugarColumn(ColumnDescription = "Danh sách biến tham số của câu SQL (JSON); ở dòng đối tác null = chưa tách riêng", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
     public string? VariablesJson { get; set; }
     ```
2. **Xóa tệp entity cũ** `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Entities/ShareDataSqlVariable.cs`.
3. **Khai báo DTO phần tử JSON `SqlVariableItem`** (đặt trong `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Dto/Packet/PacketSqlVariableDto.cs`, tương tự `SqlFieldItem` của `FieldsJson`):
   - Gồm các thuộc tính: `Code`, `Name`, `DataType`, `ValueKind`, `Value`, `DefaultValue`, `Remark`, `OrderNo`.
4. **Helper đọc/ghi JSON & nạp biến (`TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Helpers/SqlTextHelper.cs`)**:
   - Bổ sung hàm đọc/ghi tương tự `SqlFieldHelper`:
     - `ReadVariables(string? variablesJson)` $\rightarrow$ `List<SqlVariableItem>` (chuỗi rỗng / `null` / JSON lỗi trả `[]`, lọc bỏ phần tử trống `Code`, sắp theo `OrderNo`).
     - `WriteVariables(IReadOnlyCollection<SqlVariableItem>? items)` $\rightarrow$ `string` (`JsonConvert.SerializeObject(items ?? [])` — lưu `"[]"` khi danh sách rỗng đã lưu chủ động, phân biệt rõ với `null` là chưa từng lưu ở dòng đối tác).
   - Cập nhật `SqlVariableSet.From(IEnumerable<SqlVariableItem> items)` (thay cho `IEnumerable<ShareDataSqlVariable>`).
   - Đơn giản hóa các hàm trong `SqlTextHelper`:
     - `ResolveVariablesAsync(db, packetId, partnerId)`: đọc `LoadSqlConfigAsync(db, packetId, partnerId)` **chỉ trong 1 lần truy vấn `ShareDataSql`**; nếu `sqlConfig.IsCustom` thì đọc `sqlConfig.Partner!.VariablesJson`, ngược lại đọc `sqlConfig.Common?.VariablesJson`.
     - `LoadEditingVariablesAsync(db, packetId, partnerId)`:
       - `partnerId` trống $\rightarrow$ `ReadVariables(common?.VariablesJson)`.
       - Có `partnerId`: nếu `partner?.VariablesJson != null` hoặc `IsCustom(partner)` $\rightarrow$ `ReadVariables(partner?.VariablesJson)` (giữ nguyên kể cả khi đối tác chủ động lưu rỗng `"[]"`); ngược lại chưa tách riêng $\rightarrow$ trả `ReadVariables(common?.VariablesJson)` làm điểm xuất phát.
     - Xóa bỏ `CopyCommonVariablesToPartnerAsync` và `DeletePartnerVariablesAsync` không còn cần thiết (hoặc dọn sạch mọi tham chiếu liên đới theo nguyên tắc Cascading Cleanup).

### 2. Handlers CQRS & Nhật ký hoạt động (`Module.ShareData`)
1. **`PacketSqlVariableCommandHandler.cs`**:
   - Trong `HandleAsync(UpdatePacketSqlVariablesInput)`:
     - Giữ nguyên toàn bộ logic kiểm tra quyền SuperAdmin và hàm `Normalize(command.Items)` (trả về `List<SqlVariableItem>` kèm `OrderNo = i + 1`).
     - Tìm dòng `ShareDataSql` theo `(PacketId == packet.ID, PartnerId == partnerId)` (`PartnerId == null` cho mẫu chung):
       - Chưa có dòng $\rightarrow$ `InsertWithDiffLogAsync` dòng `ShareDataSql` mới với `VariablesJson = SqlTextHelper.WriteVariables(normalized)` và ghi `_activityLogger.LogCreateAsync`.
       - Đã có dòng $\rightarrow$ cập nhật `row.VariablesJson = SqlTextHelper.WriteVariables(normalized)`, gọi `UpdateWithDiffLogAsync` và ghi `_activityLogger.LogUpdate(before, row)`.
     - Không còn vòng lặp `BeginTranAsync` thêm/sửa/xóa từng dòng bảng con.
2. **`PacketSqlVariableQueryHandler.cs`**:
   - Đọc từ `SqlTextHelper.LoadEditingVariablesAsync(_db, packet.ID, partnerId)` và ghép `SavedValue` từ `ShareDataLastSend` như hiện tại.
3. **`PacketExtractSqlCommandHandler.cs`**:
   - Khi lưu SQL riêng của đối tác (`UpdatePartnerExtractSqlInput`):
     - Nếu tạo mới dòng `partnerSql == null`: gán luôn `VariablesJson = commonSql?.VariablesJson`.
     - Nếu `partnerSql != null` nhưng `!wasCustom` và `partnerSql.VariablesJson == null` (chưa lưu biến riêng ở bước trước): gán `partnerSql.VariablesJson = commonSql?.VariablesJson`. (Nếu `partnerSql.VariablesJson != null` do UI vừa gọi `UpdatePacketSqlVariablesInput` ngay trước đó thì giữ nguyên giá trị vừa lưu!).
   - Khi đặt lại về mẫu chung (`ResetPartnerExtractSqlInput`):
     - Nhánh xóa dòng `ShareDataSql`: tự động mất luôn `VariablesJson`.
     - Nhánh giữ dòng (còn `CursorTimeColumn`): gán thêm `partnerSql.VariablesJson = null;`.
   - Trong `CloneForLog`: chép thêm `VariablesJson = source.VariablesJson`.
4. **`PacketCommandHandler.cs`**:
   - Khi tạo gói mới chép từ `sourceSql`: chép thêm `VariablesJson = sourceSql.VariablesJson` để gói mới kế thừa trọn bộ cả SQL + Fields + Variables.
5. **`ActivityLoggerService.cs`**:
   - Xóa 2 nhánh pattern-matching `ShareDataSqlVariable` trong `Describe(object? target)` (vì mọi thao tác lưu biến nay ghi log qua `ShareDataSql`).

### 3. Kịch bản SQL Seed (`sharedata-seed-goi-tin-ShareDataSql.sql`)
- Cập nhật `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/sharedata-seed-goi-tin-ShareDataSql.sql` (đảm bảo idempotent, chỉ ghi trên đúng bảng `[dbo].[ShareDataSql]` theo Rule 19.103):
  - Bỏ khối `ALTER TABLE [dbo].[ShareDataSqlVariable]` ở đầu tệp; thêm kiểm tra `COL_LENGTH('dbo.ShareDataSql', 'VariablesJson') IS NULL` để bổ sung cột `[VariablesJson] NVARCHAR(MAX) NULL` nếu CodeFirst chưa chạy.
  - Thêm bước chuyển dữ liệu tự động từ bảng cũ `[dbo].[ShareDataSqlVariable]` (nếu bảng cũ còn tồn tại trong DB) sang cột `[VariablesJson]` của dòng `[dbo].[ShareDataSql]` tương ứng (`PacketId` và `ISNULL(PartnerId, '')` khớp nhau, chỉ điền vào dòng có `VariablesJson IS NULL`) bằng `FOR JSON PATH` theo thứ tự `OrderNo ASC`.

### 4. Kiểm thử Backend (`tests/BE/`)
- Cập nhật `tests/BE/ITS/ShareData/Common/ShareDataSqlSeed.cs`, `ShareDataPacketControllerTests.cs`, `DataOutboundServiceTests.cs` thay các chỗ đọc/ghi trực tiếp bảng `ShareDataSqlVariable` sang đọc/ghi `VariablesJson` qua helper (giữ nguyên 100% kịch bản kiểm thử qua API `IMessageBus` và `DataOutboundService.ProcessSubscriptions`).
- Build và chạy kiểm thử xác nhận xanh 100%:
  - `dotnet build TA-ITS015-WEBAPI-V1.0/src/TAC_WebAPI/TAC_WebAPI.csproj`
  - `dotnet build TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/ShareDataWorker.csproj`
  - `dotnet test tests/BE/test.csproj --filter "FullyQualifiedName~ShareDataPacketControllerTests|FullyQualifiedName~DataOutboundServiceTests"`

---

## Ràng buộc bắt buộc
- Tuân thủ Gortex-First (Rule 19.62), Minimal Diff (Rule 19.6, 19.89), `using` đầu tệp không viết namespace đầy đủ trong thân hàm (Rule 19.65), không gán tay `ID` ở WebAPI khi Insert (Rule 19.44), không `git add` / `git commit` (Rule 19.58), không ghi mã prompt vào comment code (Rule 19.80).

## Việc cuối — Cập nhật lại tài liệu gốc
Sau khi hoàn tất sửa mã và chạy pass bộ test:
1. Cập nhật `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/Plan/Sharedata_MasterPlan.md` và `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/README.md`: ghi nhận bộ biến câu truy vấn lưu tại `ShareDataSql.VariablesJson` (bỏ bảng rời `ShareDataSqlVariable`).
2. Giữ nguyên tệp prompt này để lập trình viên review (không tự xóa).

