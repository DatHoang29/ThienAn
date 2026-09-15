---
tier: A
read: full
source:
  - "Biên soạn nội bộ — tổng hợp từ ShareData/doc/transcript/2026-09-09-review-sharedata.md"
  - "ShareData/doc/transcript/2026-09-11-sharedata-script.md"
  - "Đối chiếu code thật trên nhánh feat/20260819-sharedata-worker (TA-ITS015-WEBAPI-V1.0), commit d458817f + 66dcec93"
  - "Đối chiếu code thật trên nhánh feat/20260908-XD001.5.5-Service-tich-hop-du-lieu (TA-ITS015-WEBVUE-V1.0), commit 4323f1e"
  - "Đối chiếu bộ test có sẵn tại tests/ShareData/Services/DataPublicationServiceTests.cs (Author: Đạt)"
date: "2026-09-15 (cập nhật lần 6 — xác nhận vị trí test thật, PacketMetadataCatalogTest làm chuẩn cho registry)"
status: "approved — chờ triển khai"
owner: "Đạt (Service Worker, tự triển khai) — Hiếu là người viết các commit refactor entity/FE nhắc tới trong plan này, không phải người triển khai plan"
---

# Kế hoạch: Hoàn thiện luồng GỬI (Outbound) của ShareData Worker

---

## 1. Bối cảnh & Hiện trạng kỹ thuật

Hai transcript họp (09/09 và 11/09/2026) chốt định hướng ShareData: bỏ ghi file trung gian rồi đọc log để gửi, thay bằng gửi thẳng qua REST cho đối tác; SQL Alias Mapping tách khỏi tầng DB; 2 chế độ lịch (định kỳ + giờ cố định hàng ngày) thay cho "sự kiện"/"gửi 1 lần"; thêm switch tự động gửi khi có data mới; so khớp field/CodeSet không phân biệt hoa-thường.

Project `TA-ITS015-WebAPI-ShareData-V1.0` (tham khảo) là bản TRƯỚC khi họp — vẫn ghi file rồi đọc log, 9 gói tin hard-code trong `PacketQueryRegistry`, chưa có CodeSet/expression, không có HTTP client cho luồng gửi. **Đã chốt: project này sẽ BỎ HẲN** — chỉ dùng để lấy đúng CÁCH LÀM `PacketQueryRegistry` (hard-code theo gói tin), không phải để chép nguyên luồng ghi-file-rồi-đọc-log, và không tiếp tục tham chiếu nó sau đợt này.

### 1.1. Phát hiện quan trọng 09/15 — Đã có sẵn bộ test + dữ liệu chuẩn cho cả 11 gói tin, không cần dò DB
File test `tests/ShareData/Services/DataPublicationServiceTests.cs` (Author: Đạt, ~70 test, integration test thật chạy trên DB `test` local — vị trí đúng theo quy ước `tests/<TênModule>/` ở gốc repo, KHÔNG phải trong `TA-ITS015-WEBAPI-V1.0/tests/` như bản plan trước đoán nhầm). File này có sẵn:
- **`PacketMetadataCatalogTest`** (dòng 2739+): Định nghĩa đầy đủ `ShareDataPacket` + `ShareDataTable` (bảng, alias, JOIN, cột, expression, kiểu, bắt buộc) cho ĐỦ 11 gói (101-111).
- **`GoldenSqlCatalog`** (dòng 2665+): Câu SQL SELECT/FROM-JOIN/WHERE ĐÃ ĐƯỢC XÁC MINH ĐÚNG cho từng gói qua các test `BuildQuery_All11Packets_SelectAndFromJoinMatchGoldenSql_Test` và `BuildQuery_Packet110_MatchesNewStructure_Test`.

> [!NOTE]
> Đây CHÍNH LÀ đặc tả chuẩn để viết `PacketQueryRegistry` — không cần đọc `dev_its10` qua MCP/SSMS như bản plan trước đề xuất.

### 1.2. Hiện trạng Codebase thật
Code thật (`TA-ITS015-WEBAPI-V1.0\src\Services\ShareData\`, nhánh `feat/20260819-sharedata-worker`) đã có nhiều phần vượt transcript: metadata `ShareDataPacket`/`ShareDataTable`, phễu ánh xạ cây (`ShareDataMapping.TargetShapeJson` với `$field`/`$each`/`$extend.codeSet`/`$extend.expression`), CodeSet, watermark tăng dần, lease chống trùng, so khớp `OrdinalIgnoreCase` xuyên suốt.

**NHƯNG tầng LẤY DỮ LIỆU THÔ hiện đang sai hướng:** Nó dựng câu SQL động bằng cách nối chuỗi từ `ShareDataTable.FieldsJson`/`JoinCondition` lúc chạy (`BuildQuery`, xem mục 3.1) — trong khi comment trong chính entity `ShareDataSubscription` ghi rõ ý định ban đầu: nguồn dữ liệu phải do **`IPacketProvider` trong mã nguồn** quyết định (hard-code kiểu `PacketQueryRegistry` của project tham khảo), KHÔNG cấu hình động trong CSDL. Đã xác nhận: việc đầu tiên phải sửa là quay tầng lấy-dữ-liệu-thô này về hard-code, rồi mới làm tiếp phần thiếu của luồng gửi.

### 1.3. Cập nhật 09/14 (commit `d458817f`)
Hiếu refactor loạt entity ShareData, cắt bỏ toàn bộ field mô phỏng giao thức C2C/ISO 14827 đầy đủ (`ProtocolProfile`, `Username`, `PasswordHash`, `InitiatorMode`, `HeartbeatMaxSec`, `DatagramSize`, `ResponseTimeoutSec`, `InboundApiUrl`, `UseTls` trên `ShareDataPartner`) — **đúng hướng đơn giản hoá về REST đã chốt trong họp**, không phải xây C2C socket thật. Thay vào đó `ShareDataPartner` giờ chỉ còn `Address`, `Port`, `EndPointApiUrl` (1 field endpoint chung, dùng cho CẢ 2 chiều gửi/nhận, thay cho `InboundApiUrl` cũ). Cùng commit này, Hiếu thêm entity mới `ShareDataPacketField` (xem chi tiết ở mục 3.1 — đây hoá ra là thay đổi quan trọng nhất cho kế hoạch này).

### 1.4. Cập nhật 09/14 tối (commit `66dcec93`)
Hiếu thêm validator cho `EndPointApiUrl` (`ShareDataPartnerValidator.cs`) — xác nhận rõ quy ước: **`EndPointApiUrl` chỉ là PHẦN ĐƯỜNG DẪN** (vd `/sharedata/inbox`), bắt buộc bắt đầu bằng `/` nếu có nhập, KHÔNG PHẢI URL đầy đủ. Comment trong code ghi rõ: *"ghép cùng Address/Port khi cần dùng tới (xem preview 'Địa chỉ đầy đủ' ở FE)"* — nhưng preview đó cũng CHƯA có ở FE. Vậy: đã biết CHẮC `EndPointApiUrl` là path-only, nhưng phần ghép `{scheme}://{Address}:{Port}{EndPointApiUrl}` thành 1 URL gọi được vẫn CHƯA có code nào làm thật — `DataInboundService.Fetch.cs.PostAsync` (chiều nhận) vẫn đang stub cứng `string url = "http";`.

Cũng đã xác nhận: cơ chế "gửi trực tiếp" tương lai sẽ là **HTTP REST POST** (đúng lời Anh Sơn trong họp). Việc VIẾT HTTP client thật **không nằm trong đợt này** — sẽ tích hợp ở đợt sau (xem mục 3.2), đợt này chỉ để lại chỗ đánh dấu, nhưng giờ có thể trỏ đúng tên field + quy ước ghép URL thật thay vì field giả định.

### 1.5. Đính chính 09/15 — Tầng Mapping/Transform KHÔNG đổi cú pháp, giữ `$field`/`$exclude`/`$extend`
Bản cập nhật trước của plan này (dựa trên đọc `ShareData/_source/html/10-wireframe-man-hinh-anh-xa.html`, mục W7) đoán rằng `TargetShapeJson` sắp chuyển sang cú pháp 1-dòng-1-khoá dựa trên đường dẫn `/`,`*`. Đối chiếu code FE thật trên nhánh `feat/20260908-XD001.5.5-Service-tich-hop-du-lieu` (`TA-ITS015-WEBVUE-V1.0`), file `src/src/views/sharedata/mapping/component/editMapping.vue` (623 dòng vừa sửa cùng ngày 09/14, commit `4323f1e`) xác nhận đoán đó SAI: màn "Ánh xạ" ĐANG SỐNG, vẫn dùng nguyên `$field`/`$extend`/`$exclude` (`MARK_FIELD`/`MARK_EXTEND`/`MARK_EXCLUDE` — dòng 415-417) để dựng và đọc lại `TargetShapeJson`. Wireframe `/`,`*` chỉ là ý tưởng khảo sát, KHÔNG được chọn. Tầng Transform (`RenderShapeNode`/`ReadShape`/CodeSet trong `DataPublicationService.cs`) giữ nguyên cú pháp — không sửa gì trong kế hoạch này.

### 1.6. Xác nhận 09/15 — `ShareDataPacketField` MỚI là nguồn field-metadata thật đang vận hành
Đã rà soát cả BE lẫn FE:
- `src/src/views/sharedata/dataSource/index.vue` (FE) — màn hiển thị `ShareDataPacket`/`ShareDataTable` — xác nhận là **màn chỉ xem, không sửa**: `<TableHeader :show-add="false" :show-delete="false">` cho cả 2 lưới, không có dialog thêm/sửa nào, chỉ gọi API `...ListbypacketGet` (GET). Không có màn nào khác trong `views/sharedata/` cho phép sửa `ShareDataTable`/`FieldsJson`.
- `src/src/views/sharedata/mapping/component/editMapping.vue` (FE) — màn "Ánh xạ" ĐANG SỐNG, người dùng thật sự thao tác hàng ngày — ô chọn field cho `$field` (`state.packetFields`) nạp từ `ShareDataPacketFieldApi` (bảng `ShareDataPacketField`), lọc `Status = Enable`. Nhãn "Bắt buộc" hiển thị cho người dùng cũng lấy từ `ShareDataPacketField.IsRequired`, KHÔNG phải từ `ShareDataTable.FieldsJson`.
- Không tìm thấy màn CRUD nào cho `ShareDataPacketField` hay `ShareDataTable` trong FE — cả 2 khả năng đang được nạp bằng cách khác (seed/migration, hoặc đúng như comment trong entity `ShareDataPacket.cs`: *"sinh từ mã nguồn `[EshPacket]` trên lớp `PacketNNN`"* — tức có ý định đồng bộ từ code, chưa chắc đã làm).

> [!IMPORTANT]
> **Kết luận đổi thiết kế:** Vì màn Ánh xạ (nơi người dùng THẬT SỰ chọn field để gửi) lấy field từ `ShareDataPacketField`, Worker phải đọc field-metadata cho tầng gửi từ **`ShareDataPacketField`**, KHÔNG PHẢI `ShareDataTable.FieldsJson` — nếu không, field người dùng chọn trên màn Ánh xạ (có trong `ShareDataPacketField`) rất dễ không khớp bộ alias SQL Worker tạo ra (nếu vẫn lấy theo `FieldsJson`), gây lỗi âm thầm `null`.

### 1.7. Đính chính 15/09 (tối) — Đảo ngược lại quyết định mục 3.1.b: quay về `ShareDataTable.FieldsJson`

> [!IMPORTANT]
> Nhận thông tin mới trực tiếp: `ShareDataPacketField` chỉ là bảng **để xem/hiển thị** (FE), KHÔNG phải nguồn vận hành cho luồng gửi. Kết luận ở mục 1.6 (dựa trên rà `editMapping.vue`) bị **đảo ngược lại** — code Worker đã revert về đọc `ShareDataTable.FieldsJson` qua hàm `ParseFields` có sẵn (tác giả Đạt, 22/08/2026), KHÔNG đọc `ShareDataPacketField` nữa.

Bằng chứng độc lập ủng hộ quyết định đảo ngược (không chỉ dựa lời kể): entity `ShareDataPacketField` chỉ có `Name/DatatypeId/AliasFieldKey/Type/Status/IsRequired/Remark` — thiếu hẳn `CodeSet/Expression/Column/Unit/NoSource/IsKey` mà `PacketFieldDto` (kiểu JSON bên trong `FieldsJson`) đã có sẵn. Nếu giữ đọc từ `ShareDataPacketField`, `CodeSetCode` luôn `null` cho mọi field → mapping `$extend.codeSet` không bao giờ hoạt động cho bất kỳ gói tin nào — lỗi âm thầm còn nghiêm trọng hơn rủi ro ban đầu mục 1.6 từng lo ngại.

**Trạng thái hiện tại (đã triển khai, xác nhận qua code + test thật):**
- `LoadPacketFields` (`DataPublicationService.PacketRegistry.cs`) đọc `ShareDataTable` theo `PacketCode`, gọi `ParseFields(table.FieldsJson)` cho từng bảng, lọc `InternalOnly != true`, sắp theo `OrderNo` — y hệt logic cũ trong `BuildQuery` bước 3, chỉ tách riêng khỏi phần dựng SQL (giờ do `PacketQueryRegistry` đảm nhiệm).
- Không còn chỗ nào trong `ShareDataWorker`/`ShareDataWorker.Core` đọc `ShareDataPacketField` (đã grep xác nhận toàn bộ `src/Services/ShareData`).
- `dotnet test tests/test.csproj --filter "FullyQualifiedName~ShareData"` đã chạy thật, 81/81 pass — bao gồm cả CodeSet mapping.

> [!CAUTION]
> **Rủi ro còn treo, CHƯA xác nhận lại:** nếu màn Ánh xạ FE (`editMapping.vue`) vẫn đang lấy field cho `$field` từ `ShareDataPacketField` (như mô tả ở mục 1.6), thì field người dùng chọn trên UI có thể không khớp field Worker đọc từ `FieldsJson` — đúng lỗ hổng mục 1.6 từng cảnh báo, chỉ đổi chiều. Cần xác nhận lại với người đưa "thông tin mới" hoặc rà lại FE trước khi coi đây là xong hẳn.

**Giới hạn phạm vi:** Chỉ luồng **GỬI (Outbound)**. Không đụng luồng NHẬN (`DataInboundService`) — dù luồng đó hiện đang ở trạng thái build-tạm, không thuộc phạm vi kế hoạch này. Không đụng UI/FE (thuộc Kiên).

---

## 2. Đã có sẵn — Không cần làm lại

| Yêu cầu họp | Trạng thái |
|---|---|
| **So khớp field/CodeSet không phân biệt hoa-thường** | **ĐÃ CÓ** — `StringComparer.OrdinalIgnoreCase` xuyên suốt `DataPublicationService.cs` và `MapCode` (`DataPublicationService.Validation.cs:425`, không đổi qua các commit mới). |
| **Bỏ chế độ "gửi 1 lần" như một lịch định kỳ** | **COI NHƯ ỔN** — `Mode.Single` vẫn còn trong `BaseEnums.SubMode` (enum dùng chung, không tự xoá được), nhưng worker tự chuyển subscription sang `Expired` sau đúng 1 lần chạy (`TryPersistExportResult:363-370`). Khớp ý "nút Test" họp muốn — không cần sửa Worker. |
| **Cú pháp mapping `$field`/`$exclude`/`$extend`** | **ĐÃ CÓ, GIỮ NGUYÊN** — cơ chế thật, đang sống (FE `editMapping.vue` vừa sửa 09/14, vẫn dùng nguyên cú pháp này). `Transform`/`RenderShapeNode`/CodeSet không đổi trong kế hoạch này. |
| **Nguồn field-metadata cho tầng gửi** | **ĐẢO NGƯỢC LẠI, GIỮ `ShareDataTable.FieldsJson`** — mục 3.1.b bên dưới mô tả hướng đổi sang `ShareDataPacketField` đã bị đảo ngược, xem lý do + trạng thái triển khai thật ở mục 1.7. |
| **URL endpoint đối tác cho luồng gửi** | **CÓ FIELD + QUY ƯỚC RỒI, CHƯA CÓ CODE GHÉP** — `ShareDataPartner.EndPointApiUrl` (path-only, phải bắt đầu `/`, đã có validator xác nhận) + `Address`/`Port` đã đủ để ghép URL. Nhưng hàm ghép `{scheme}://{Address}:{Port}{EndPointApiUrl}` thành 1 chuỗi gọi được vẫn CHƯA ai viết (xem mục 3.2). |

> [!NOTE]
> **Đổi so với bản trước:** Dòng "Bỏ chế độ gửi theo sự kiện — ĐÃ CÓ" đã bị RÚT LẠI (xem mục 3.4) vì commit 09/14 của Hiếu không loại `Mode=Event` khỏi vòng lặp gửi định kỳ nữa. Dòng "field-metadata" đổi từ "rủi ro, chờ quyết" sang "sửa luôn trong mục 3.1".

---

## 3. Nội dung triển khai chi tiết (Thực hiện tuần tự — Mục 3.1 là nền tảng)

### 3.1. [NỀN TẢNG] Thay `BuildQuery` bằng `PacketQueryRegistry` hard-code & Đổi nguồn field-metadata sang `ShareDataPacketField`

Mục này gồm 2 thay đổi bắt buộc phải làm CÙNG NHAU (tách riêng sẽ để lại đúng lỗ hổng "field chọn trên FE không khớp SQL Worker"):

#### a. Vấn đề gốc — Thay `BuildQuery` (Dynamic SQL runtime)
- `BuildQuery` (`DataPublicationService.Validation.cs:113-315`) dựng câu SQL bằng cách nối chuỗi động từ `ShareDataTable.FieldsJson`/`Alias`/`JoinCondition` mỗi lần chạy, kèm khoảng 250 dòng regex chống injection.
- Đây là hướng sai — cần quay về hard-code kiểu `PacketQueryRegistry` của project tham khảo (`TA-ITS015-WebAPI-ShareData-V1.0\...\ShareDataWorkerService.cs:22-213`), đúng ý định ban đầu ghi trong entity `ShareDataSubscription`.
- **Đã chốt 09/15:** Wireframe `10-wireframe-man-hinh-anh-xa.html` (W0.2) từng đề xuất cho người quản trị tự gõ SQL, nhưng đó chỉ là ý tưởng UI chưa có backend. Đã chốt theo `PacketQueryRegistry` hard-code — xuất ra "cục dữ liệu thô" (`List<object>`), các bước sau (Transform/CodeSet/Shape) xử lý tiếp thành cấu trúc đối tác mong muốn.

#### b. Vấn đề mới — Nguồn field-metadata phải đổi sang `ShareDataPacketField`

> [!NOTE]
> **Đã đảo ngược lại — xem mục 1.7.** Toàn bộ phần b bên dưới là hướng thiết kế BAN ĐẦU, không còn là trạng thái cuối. Code thật đã quay lại đọc `ShareDataTable.FieldsJson` qua `ParseFields`. Giữ nguyên nội dung dưới đây để lưu lại lý do/bối cảnh lúc quyết định, không dùng để triển khai.

FE (`editMapping.vue`) chọn field từ `ShareDataPacketField` (`Name`/`DatatypeId`/`AliasFieldKey`/`Type`/`Status`/`IsRequired`/`Remark`), không phải `ShareDataTable.FieldsJson`. Do đó, `FetchDataForSubscription` (`DataPublicationService.cs:407+`) phải đổi CÁCH DỰNG `AllFields`/`fieldsDict`:
- **Hiện tại:** Gộp `ParseFields(table.FieldsJson)` của mọi dòng `ShareDataTable` cùng `PacketCode`.
- **Đổi thành:**
  ```csharp
  db.Queryable<ShareDataPacketField>()
    .Where(f => f.DatatypeId == packet.Code && f.Status == BaseEnums.Status.Enable)
    .ToListAsync()
  ```
  Map mỗi dòng sang `PacketFieldDto` Worker đang dùng:
  - `FieldKey = AliasFieldKey`, `Name = Name`, `DataType = Type`
  - `Required = (IsRequired == BaseEnums.IsRequired.IsRequired)`
  - `Column`/`Expression`/`CodeSetCode`/`NoSource`/`OrderNo`/`IsKey` để mặc định (`null`/`false`/`0`) — `ShareDataPacketField` không có các cột này, và Worker cũng không cần chúng nữa: `Column`/`Expression`/`OrderNo` chỉ phục vụ `BuildQuery` (đang bị thay); `CodeSetCode` mức field-level chỉ là fallback hiếm dùng khi KHÔNG có `TargetShapeJson` — CodeSet thật sự áp dụng theo từng binding qua `$extend.codeSet` (do màn Ánh xạ ghi).
- **Chỉ đổi phía GỬI:** `ParseFields`/`ShareDataTable.FieldsJson` là hàm dùng chung; `DataInboundService` (luồng nhận) có thể vẫn gọi `ParseFields(table.FieldsJson)` ở chỗ khác của chính nó. KHÔNG sửa `ParseFields` hay chỗ nào Inbound đang gọi — chỉ thêm 1 đường nạp field MỚI, riêng cho Outbound.
- **Lưu ý kiểu:** `IsRequired` là enum (`NoRequired = 0 | IsRequired = 1`, `BaseEnums.IsRequired`), không phải `bool` — nhớ so sánh đúng kiểu khi map.

#### c. Pipeline dữ liệu sau khi khớp chuẩn
Khớp đúng luồng đã bàn trong họp (SqlSugar → object → Field Metadata Schema → Mapping Engine → CodeSet → Payload):
1. `PacketQueryRegistry[packet.Code](...)` → `List<object>` — "cục dữ liệu thô", cột đặt tên theo alias khớp `ShareDataPacketField.AliasFieldKey`.
2. `FetchDataForSubscription` đưa qua `Transform(...)` (`DataPublicationService.cs:567`) — Mapping Engine: đọc `TargetShapeJson` (`$field`/`$each`/`$extend`), CodeSet áp qua `MapCode` (`Validation.cs:412`).
3. Nếu khung đích có khối lặp (`$each`): `ShapeHasRepeatBlock` phát hiện, `RenderShapeAggregate` dựng 1 object gốc chứa cả 2 dạng: nhánh `$each:true` nhân theo từng dòng (ra mảng = "Records"), nhánh `$field` thường lấy giá trị từ dòng đầu tiên (`allRows[0]` = "Info").
4. Đóng gói thêm 1 lớp PDU hệ thống (`pduType`/`serialNbr`/`sender`/`destination`/`timestamp`/`hash`/`payload: data`) trước khi lưu/gửi.

#### d. Thiết kế & Triển khai
- Registry hard-code, key = `ShareDataPacket.Code` (vd `"101"`), value = `Func<ISqlSugarClient, DateTime?, string?, CancellationToken, Task<List<object>>>`, SELECT ra đúng bộ alias khớp `ShareDataPacketField.AliasFieldKey` của gói đó, cộng 2 cột kỹ thuật `__watermark`/`__rowid` như `BuildQuery` đang tự thêm.
- Lấy entity nguồn thuận lợi: `ShareDataWorker.Core.csproj` đã reference `Modules.TMS.Core`, `Module.TOLL.Core`, `Modules.VMS.Core`, `Module.CCTV` — dùng entity thật, không cần copy.
- `FetchDataForSubscription` thay `BuildQuery` bằng registry và thay nguồn field sang `ShareDataPacketField`.
- Gói tin chưa kịp hard-code trong registry → log lỗi rõ ràng "chưa hỗ trợ", giống cách project tham khảo chủ động báo FAILED cho gói 111 thay vì âm thầm fallback.
- `BuildQuery`/`Validate*` (Validation.cs) có thể xoá hẳn sau khi registry thay thế xong, hoặc giữ tạm để đối chiếu trong giai đoạn chuyển đổi.
- Dùng `PacketMetadataCatalogTest`/`GoldenSqlCatalog` trong `tests/ShareData/Services/DataPublicationServiceTests.cs` (dòng 2665-3403) làm đặc tả — đã có sẵn bảng, alias, JOIN, cột, expression, watermark, TopN cho cả 11 gói (101-111).

**File cần sửa/thêm:**
- **Mới:** `Infrastructure/Services/DataPublication/DataPublicationService.PacketRegistry.cs` (partial class chứa `PacketQueryRegistry` cho 11 gói).
- **Sửa:** `DataPublicationService.cs` (`FetchDataForSubscription`, dòng 407+) — gọi registry thay vì `BuildQuery`; đổi nguồn `AllFields`/`fieldsDict` sang `ShareDataPacketField`.
- **Không sửa:** Entity `ShareDataTable`/`ShareDataPacket`/`ShareDataPacketField` (bên `Module.ShareData.Core`), `Transform`/CodeSet/Shape/`ParseFields`.
- **Sửa test có sẵn (`tests/ShareData/Services/DataPublicationServiceTests.cs`):** Không cần seed `ShareDataPacketField` nữa (xem mục 1.7 — đã đảo ngược). Thay vào đó đã thêm `DataPublicationService.RegisterTestHandler`/`UnregisterTestHandler` (`PacketRegistry.cs`) — cho phép test đăng ký tạm 1 handler theo mã gói bất kỳ (kể cả mã tự bịa như `PKT_A1_...`) trực tiếp vào `PacketQueryRegistry`, dùng cho các test cơ chế chung (thiếu field bắt buộc, thiếu CodeSet, lỗi expression) không cần gắn với 1 trong 11 mã thật. Các test `BuildQuery_*`/`QueryPacket_*` cũ (test thẳng hàm `BuildQuery`, giờ là dead code) — MỘT phần đã dọn (`BuildQuery_Packet110_MatchesNewStructure_Test` đã comment `[DEAD CODE TEST]`), phần còn lại (`BuildQuery_All11Packets_...`, `QueryPacket_KeysetPagination_...`, vài test khác) vẫn gọi thẳng `BuildQuery` — chưa viết lại, còn treo.

---

### 3.2. Gửi trực tiếp tới đối tác qua REST (Đánh dấu chỗ nối cho đợt sau)

`ExecuteExportForSubscription` (`DataPublicationService.cs:184-350`) hiện LUÔN LUÔN chỉ ghi file (`SaveExportFileAsync`, dòng 330), bất kể `sub.Format` là `Data` hay `File`. Không có `HttpClient`/POST nào trong luồng gửi.

**Đã chốt: KHÔNG viết logic gọi HTTP thật trong đợt này** — HTTP client sẽ được tích hợp ở một đợt sau. Việc cần làm bây giờ chỉ là để lại 1 comment TODO rõ ràng ngay tại chỗ ghi file trong `ExecuteExportForSubscription` (ngay sau dòng gọi `SaveExportFileAsync`, dòng 330):

```csharp
// TODO: [Gửi trực tiếp qua HTTP] — khi viết client thật, ghép URL theo đúng quy ước đã xác nhận
// (commit 66dcec93, validator ShareDataPartnerValidator.EndPointApiUrl):
//   EndPointApiUrl CHỈ là phần đường dẫn (vd "/sharedata/inbox", luôn bắt đầu bằng "/"),
//   ghép cùng Address + Port để ra URL đầy đủ. Scheme (http/https) chưa được chốt ở đâu cả —
//   xác nhận trước khi code, và đối chiếu DataInboundService.Fetch.cs.PostAsync (chiều nhận) —
//   vẫn đang stub cứng "http" tại thời điểm viết plan này, kẻo mỗi chiều ghép một kiểu.
// Rồi POST finalBytes tới đối tác ngay tại đây, sau khi ghi file.
// Xem thêm: biên bản họp 09/09 & 11/09/2026 — mục "gửi trực tiếp qua WebAPI".
```

> [!WARNING]
> **Phát hiện thêm, KHÔNG thuộc phạm vi sửa đợt này nhưng cần lưu ý — ĐÃ XỬ LÝ:**
> `useXml` đã được đổi `true → false` (giờ là `var useXml = false; // Mặc định xuất JSON theo chuẩn REST hiện hành`) — mọi export giờ ra JSON, không còn XML mặc định. Hệ quả: test `ProcessBatchSubscriptions_WhenPartnerProtocolIsXmlA_ExportsWellFormedXmlWithSha256Hash_Test` (cố ý mong `.xml`) đã được comment tạm với ghi chú `[TẠM REMCODE THEO YÊU CẦU]: chờ chốt phương án cấu hình phân biệt XML/JSON từ tầng Entity/API`. Test `...WhenPartnerProtocolIsAsn_FallsBackToJson_Test` (mong `.json`) vẫn active và PASS.

---

### 3.3. Lịch "theo giờ cố định hàng ngày" (Daily) — Đã mô hình, chưa được đọc

`ShareDataScheduleDto` (`Module.ShareData.Core/Dto/Subscription/`) đã mô hình đủ `Kind: continuous|daily`, `DaysOfWeek`, `StartTime`, `DurationMinutes`, `StartDate/EndDate`; `ShareDataEnum.ScheduleKind`/`ScheduleDayCode` cũng có sẵn ở `ShareDataWorker.Core`. NHƯNG `TryPersistExportResult` (`DataPublicationService.cs:356-380`) chỉ cộng thẳng `IntervalSeconds` (dòng 366) — hoàn toàn không đọc `sub.ScheduleJson`. "Chạy đúng 9h sáng mỗi ngày" hiện **không hoạt động**.

#### Thiết kế — `ComputeNextTimeRun(sub, now)`
- `ScheduleJson` rỗng hoặc `Kind != "daily"` → giữ nguyên hành vi hiện tại (`now + IntervalSeconds`), TRỪ trường hợp `sub.Mode == Event` (xem mục 3.4 — có ưu tiên riêng, không đi qua nhánh này).
- `Kind == "daily"` → parse `StartTime` (HH:mm) + `DaysOfWeek`, tìm thời điểm hợp lệ gần nhất (hôm nay nếu `StartTime` chưa qua và đúng thứ; nếu không thì ngày kế tiếp thoả `DaysOfWeek`), có xét biên `StartDate/EndDate`. `DurationMinutes` không ảnh hưởng `NextTimeRun` — chỉ 1 lần chạy đúng giờ, không phải cửa sổ lặp lại nhiều lần trong ngày.

> [!CAUTION]
> **Bẫy dễ dính:** Parse `ScheduleJson` bằng `System.Text.Json.JsonDocument` thủ công (như cách `TargetShapeJson` đang được đọc trong file này), **KHÔNG** dùng `System.Text.Json.JsonSerializer.Deserialize<ShareDataScheduleDto>` trực tiếp — DTO này dùng `Newtonsoft.Json.JsonProperty("updateDelaySec")` cho phần tương thích ngược mà STJ không đọc attribute đó (`ShareDataWorker.csproj` cũng chưa reference Newtonsoft.Json) → deserialize thẳng bằng STJ sẽ âm thầm bỏ qua field `updateDelaySec` của các bản ghi cũ.

**File cần sửa/thêm:**
- **Mới:** `Infrastructure/Services/DataPublication/DataPublicationService.Schedule.cs` (partial) — `ComputeNextTimeRun`, để `public static` cho dễ viết unit test không cần DB. Hàm này gộp luôn logic mục 3.4 (Mode=Event).
- **Sửa:** `DataPublicationService.cs:366` — thay `DateTime.Now.AddSeconds(intervalSec)` bằng gọi `ComputeNextTimeRun(sub, DateTime.Now)`.

---

### 3.4. Switch "Tự động gửi khi có dữ liệu mới" — Thiết kế lại sau commit 09/14

Bản kế hoạch trước từng đề xuất thêm 1 giá trị `Kind="auto"` mới vào `ScheduleJson`. **Không cần nữa** — commit 09/14 đã tự sửa đúng hướng này rồi, chỉ chưa hoàn thiện: `ProcessBatchSubscriptions` VÀ `DataInboundService` (`DataInboundService.cs:46`) đều vừa BỎ điều kiện loại trừ `Mode != Event` (comment tại đó: *"Từ 2026-09-12: Mode = EVENT không còn loại trừ lịch gửi định kỳ — nó chỉ bổ sung việc gửi ngay khi có sự kiện"*). Tức là `Mode = Event` trên `ShareDataSubscription` CHÍNH LÀ cái switch họp muốn.

#### Thiết kế tối giản, không cần hạ tầng event mới:
- Trong `ComputeNextTimeRun` (mục 3.3), khi `sub.Mode == BaseEnums.SubMode.Event`, bỏ qua `IntervalSeconds`/`ScheduleJson` và luôn trả về `now + CheckInterval` (5 giây — đúng bằng chu kỳ quét của `DataPublicationWorker`).
- Vì `FetchDataForSubscription` vốn ĐÃ bỏ qua — không ghi gì — khi không có bản ghi mới (watermark rỗng), việc quét mỗi 5 giây cho subscription `Mode=Event` sẽ tự nhiên cho ra đúng ngữ nghĩa "gửi ngay khi có data mới", dùng đúng field `Mode` đã có sẵn trên UI/DB, không cần sửa gì ở FE hay thêm quy ước `ScheduleJson.Kind` mới.

---

## 4. Ngoài phạm vi (Không làm trong kế hoạch này)

- **Không đụng `DataInboundService`/luồng nhận:** Dù đang có code build-tạm (`string url = "http";` trong `DataInboundService.Fetch.cs.PostAsync`, guard `ApiNotConfigured` bị comment out), không sửa ở đây vì ngoài phạm vi "chỉ luồng gửi" — chỉ đối chiếu quy ước ghép URL từ đó khi làm mục 3.2 sau này. Cũng KHÔNG đổi cách Inbound nạp field (nếu nó tự đọc `ShareDataTable.FieldsJson` ở đâu đó) — mục 3.1 chỉ thêm đường nạp field mới, riêng cho Outbound.
- **Không sửa UI/FE:** Dropdown chọn field, tinh gọn giao diện, nút Test, preview "Địa chỉ đầy đủ" thuộc Kiên.
- **Không tự thêm cột vào Entity `Module.ShareData.Core`:** Đề nghị Hiếu/Đạt thêm qua `EnableIncreTable` nếu cần.
- **Không xây transport C2C qua socket TCP thật:** Đã cắt các trường C2C khỏi `ShareDataPartner`, đi thẳng REST.
- **Không viết logic gọi HTTP thật cho luồng gửi trực tiếp:** Đợt sau mới tích hợp (mục 3.2 chỉ để lại comment đánh dấu).
- ~~Không tự sửa `bool useXml = true;`~~ — **đã xử lý theo yêu cầu trực tiếp** (đổi `false`, mặc định JSON), xem mục 3.2.
- **Không xây dựng cơ chế event-driven qua `EventSourceId`/`ShareDataEventSource`/NATS:** Mục 3.4 dùng giải pháp poll 5s thay thế, đủ cho yêu cầu họp, không cần hạ tầng event phức tạp.
- **Không sửa/xoá `ShareDataTable`/`FieldsJson` hay màn `dataSource/index.vue`:** Chỉ ngừng ĐỌC `FieldsJson` ở phía Outbound Worker (mục 3.1), dữ liệu/UI cũ vẫn giữ nguyên làm tham khảo.

---

## 5. Kế hoạch kiểm thử (Test Plan)

> [!NOTE]
> **Đã có sẵn bộ test đầy đủ, không phải tạo mới:** Bản plan trước tưởng chưa có test ShareData nên đề nghị tạo `TA-ITS015-WEBAPI-V1.0\tests\ShareDataWorker.Tests` — SAI vị trí. Test thật nằm ở gốc repo: `tests/ShareData/Services/DataPublicationServiceTests.cs` (Author: Đạt), theo đúng quy ước chung `tests/<TênModule>/` đã dùng cho VideoWall — dùng chính file này.
>
> Quy ước viết test xem `tests/README.MD` (mục "Quy ước viết test") — 1 file test/1 class nguồn, XML summary 3 dòng bắt buộc (`Author: Đạt`/`Description`/`Created date`), tự dọn dữ liệu theo prefix riêng.

> [!NOTE]
> **Trạng thái build:** Lỗi VideoWall (`ITS.VideoWall...Interfaces`) trước đây đã được xử lý xong; lỗi CS0176 do `TryPersistExportResult` bị chuyển thành `static` cũng đã được sửa dứt điểm về instance method (`public async Task<int> TryPersistExportResult(...)`). Lệnh `dotnet build tests/test.csproj` hiện đã biên dịch hoàn toàn thành công (0 Error).

> [!NOTE]
> **Hiện tượng "lạ" trước đây (ProtocolProfile/InboundApiUrl compile qua dù entity không có field) không còn tái hiện** sau khi 2 test liên quan được xử lý (xem mục 3.2) — không cần Đạt tự kiểm tra lại nữa, coi như đã khép lại cùng lúc với việc xử lý `useXml`.

### Các hạng mục kiểm thử cụ thể:
1. **Mục 3.1 (PacketQueryRegistry + nguồn field-metadata):**
   - Dùng lại các test `BuildQuery_*`/`Transform_Packet101_*` có sẵn làm cơ sở đối chiếu (số dòng, giá trị từng field) — vẫn còn 1 phần test `BuildQuery_*` gọi thẳng hàm cũ chưa dọn, xem mục 3.1 "File cần sửa/thêm".
   - Test case cơ chế chung (thiếu field bắt buộc, thiếu CodeSet, lỗi expression, watermark null) giờ dùng `DataPublicationService.RegisterTestHandler`/`UnregisterTestHandler` để gắn handler tạm theo mã gói bất kỳ — không cần alias khớp registry thật.
2. **Unit test không cần DB (`ComputeNextTimeRun`):**
   - Chế độ continuous giữ nguyên hành vi cũ (`now + IntervalSeconds`).
   - Chế độ daily tính đúng thứ/giờ/biên `StartDate-EndDate`, kể cả trường hợp `StartTime` hôm nay đã trôi qua.
   - `ScheduleJson` rỗng/hỏng → fallback về continuous.
   - `Mode=Event` → luôn trả về `now + 5s` bất kể `ScheduleJson`/`IntervalSeconds`.
3. **Regression Tests (Đảm bảo an toàn hệ thống):**
   - Toàn bộ các test `Transform_*`, `ExecuteExport_*Esh12xx*`, `ProcessBatchSubscriptions_*` — **đã xác nhận PASS thật** (`dotnet test tests/test.csproj --filter "FullyQualifiedName~DataPublicationServiceTests"` → 81/81 pass, 0 fail).
4. **Kiểm thử thủ công:**
   - Chạy `dotnet test tests/test.csproj --filter "FullyQualifiedName~ShareData"` — đã chạy, PASS toàn bộ.
   - Đối chiếu `ShareDataActivityLog`/`ShareDataAlertLog` (đọc qua MCP `mssql_dev`, read-only) khớp kỳ vọng cho cả 3 mục đã làm (3.1, 3.3, 3.4). Mục 3.2 (HTTP) chỉ cần xác nhận comment TODO đã nằm đúng chỗ — đã xác nhận.

---

## 6. Quan sát thêm (Không thuộc phạm vi, chỉ ghi nhận)

- **Trạng thái Inbound (`DataInboundService.Fetch.cs.PostAsync`):** Hiện có `string url = "http";` — stub cứng, build được nhưng KHÔNG chạy đúng (không hề ghép `Address`/`Port`/`EndPointApiUrl`, dù quy ước path-only đã được validator xác nhận). Guard `ApiNotConfigured` cũng đang bị comment out ở `DataInboundService.cs`. Luồng NHẬN hiện chưa hoàn thiện.
- **Cơ chế nạp danh mục Packet/Field:** Không tìm thấy màn CRUD nào cho `ShareDataPacket`/`ShareDataTable`/`ShareDataPacketField` trong FE hiện tại. Cần làm rõ với team xem bảng này sẽ được nạp qua seed/migration hay cơ chế reflection `[EshPacket]`/`IPacketProvider` từ code.
