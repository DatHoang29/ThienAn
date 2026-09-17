# ShareData (ESHARE) — Master Plan FE · BE · Service (chốt 16/09/2026)

> Tài liệu SỐNG. Cập nhật trực tiếp file này khi trạng thái đầu việc thay đổi, không tạo file mới.
>
> **Nguồn — 4 biên bản ngày 16/09/2026:**
> [Review Frontend](../doc/transcript/16-09-2026-review-frontend-sharedata.md) ·
> [Recap sửa UI](../doc/transcript/16-09-2026-sua-ui-sharedata.md) ·
> [Refactor Backend Worker](../doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md) ·
> [Định danh đối tác & test tải](../doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md)

## Bối cảnh

Ngày 16/09/2026 có 4 phiên làm việc chốt toàn bộ đầu việc còn lại của phân hệ Chia sẻ dữ liệu. Hai mục tiêu song song:

1. **Dứt điểm UI/UX để bàn giao Tester (chị Như) trong tuần** — chấm dứt task test treo ~60%, để Tester hiểu đúng luồng *Đối tác → Gói tin → Lịch gửi → Ánh xạ* và không hiểu nhầm chức năng chưa lập trình là "lỗi".
2. **Chốt dứt điểm ShareData Worker trước** để bàn giao API chuẩn cho FE ghép giao diện thật, chấm dứt mock data; sau đó cả đội mới chuyển sang VideoWall.

### Nguyên tắc thứ tự — "đáy phụ thuộc" (anh Sơn, biên bản định danh đối tác)

> Tìm module nằm ở tầng sâu nhất (Service xử lý dữ liệu lõi) hoàn thiện & chốt dứt điểm trước, sau đó mới tới các tầng gọi phụ thuộc (WebAPI → Frontend). **Không sửa lắt nhắt từng phần rồi ghép chắp vá** gây lãng phí thời gian và lỗi chéo.

### Ba tuyến công việc

| Tuyến | Phạm vi mã nguồn | Phụ trách | Ưu tiên |
|---|---|---|---|
| **SERVICE** — `SV-*` | `TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker` (Outbound + Inbound) | **Đạt** (luồng gửi) · **Kiên** (luồng nhận) | **1 — chốt luồng chuẩn trước** |
| **BACKEND** — `BE-*` | `TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData[.Core]` | **Đạt** | 2 — mở API cho FE ghép thật |
| **FRONTEND** — mục A–E | `TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata` | **Kiên** | 3 — chạy song song phần không bị chặn |

> ⚠️ Phân công luồng nhận suy từ biên bản (*"cái luồng nhận của em chắc nhiều gấp đôi luồng gửi của anh Đạt"* — Kiên, 03:11 biên bản refactor) — **cần anh Sơn xác nhận** nếu thực tế khác.

**Nguyên tắc FE đợt này (đã chốt với người dùng):** hạng mục chưa có API backend thì **dựng sẵn UI, disable nút gọi API + tooltip "chờ API"**, kèm `TODO(BE)` tại chỗ để ghép sau — không mock dữ liệu giả.

---

# PHẦN I — SERVICE: ShareDataWorker (`SV-*`)

*Nguồn: biên bản [Refactor Backend Worker](../doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md) + [Định danh đối tác & test tải](../doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md) + mục 19 [Recap sửa UI](../doc/transcript/16-09-2026-sua-ui-sharedata.md).*

## Checklist SV

- [ ] **SV-1** Bỏ "vỏ" envelope khi gửi đi 🔴 *chốt gấp — chặn cả 2 chiều*
- [ ] **SV-2** Bỏ phân nhánh xử lý theo Version
- [ ] **SV-3** Định danh đối tác đi trong payload (metadata) ⚠️ *giải pháp tạm — chờ anh Sơn chốt tên khoá*
- [ ] **SV-4** Cấu hình vai trò instance + mã định danh của mình (`ShareData:Role`, `ShareData:SelfPartnerCode`) 🔑
- [ ] **SV-5** Kịch bản test đa đối tác bằng clone service *(sau khi SV-1 xong)*
- [ ] **SV-6** Đo tải & rủi ro nghẽn CSDL *(sau khi luồng 1-1 chuẩn)*
- [ ] **SV-7** Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự)
- [ ] **SV-8** Ghi log 2 bước cha–con *(chặn bởi BE-5)*
- [ ] **SV-9** *(Giai đoạn sau)* Xác thực đối tác bằng key / token
- [ ] **SV-10** Rà soát pipeline Outbound sau refactor *(khung đã xong, cần xác nhận)*
- [ ] **SV-11** Thiết kế luồng nhận (Inbound) *(chưa họp chốt với anh Sơn)*

> 💡 Bug fix đã làm ngoài kế hoạch — **17/09/2026**: `MapCode` empty-string bypass `IsDefault` → đã fix + 12 unit test pass.

---



Pipeline Outbound 3 bước mà biên bản refactor yêu cầu **về cơ bản đã dựng xong** — không phải làm lại:

```
Extraction/DataExtractionProcess → Mapping/DataMappingProcess → Transport/{RestPacketSender, FileExportSender}
           ↑ điều phối: Orchestration/DataPublicationService   ↑ context xuyên suốt: DataPublicationContext
```

`DataPublicationContext` đã mang `PacketCode` / `PartnerCode` / dữ liệu — đúng tinh thần `WrapperData` anh Sơn yêu cầu. Luồng nhận đã tách partial 4 file: `DataInboundService{.cs,.Parse,.WriteSql,.Logging}`.

## SV-1. Bỏ "vỏ" envelope khi gửi đi 🔴 *chốt gấp — chặn cả 2 chiều*

`Transport/RestPacketSender.cs` (khối `httpPayload`) đang bọc dữ liệu đã mapping vào một lớp vỏ tự chế:

```csharp
partnerCode, datatypeId, packetVersion, serialNbr, pduType, format, rawContent
```

- Anh Sơn chốt: **bỏ vỏ, gửi thẳng nội dung bên trong** — *"cấu trúc dữ liệu thống nhất giữa hai bên như thế nào thì gửi y chang vậy"*. Tự thêm lớp ngoài thì lại phải đi thống nhất lớp ngoài đó với từng đối tác, sinh thêm việc vô ích.
- Chiều nhận phải bỏ tương ứng: `DataInbound/DataInboundService.Parse.cs` hiện **bắt buộc** khoá `payload` (`"Gói tin thiếu khoá payload"`, `"payload phải là một mảng"`) → parse thẳng theo cấu trúc thống nhất; sai cấu trúc thì ghi nhận lỗi, **không** cố dò nhiều kiểu.

## SV-2. Bỏ phân nhánh xử lý theo Version

- Không viết nhánh *"version 1 parse kiểu 1 / version 2 parse kiểu 2"*. Gói đến định dạng nào parse thẳng định dạng đó, sai thì báo lỗi.
- `PacketVersion` **chỉ còn dùng để ghi log** (`ShareDataTransferLog`, `DataPublicationService.LogExportResult`) — tuyệt đối không dùng làm điều kiện rẽ nhánh xử lý. Rà lại ghi chú ở `DataInboundService.cs:361`.

## SV-3. Định danh đối tác đi trong payload (metadata) ⚠️ *giải pháp tạm*

- Mã đối tác truyền trong **metadata / trường cố định của body** (ví dụ `partnerCode: "A101"`), **không** đặt ở header HTTP — vì gói qua socket/broker cần dữ liệu nằm trọn trong payload.
- Mã lấy từ **cấu hình định danh của chính instance** (SV-4), không set cứng rải rác trong code.
- Anh Sơn nói rõ đây là *tạm*: dựa vào dữ liệu thì đối tác XYZ biết mã ABC có thể sửa mã, spam giả danh → hướng đúng ở SV-9.

## SV-4. Cấu hình vai trò instance + mã định danh của mình 🔑 *nền cho test tải*

- `appsettings.json` bổ sung:
  - `ShareData:SelfPartnerCode` — mã đối tác của chính mình khi gửi đi (ví dụ `A101`).
  - `ShareData:Role` — `SendOnly` | `ReceiveOnly` | **không khai báo = Both** (vừa gửi vừa nhận, đúng ý anh Sơn).
- `Extensions/ShareDataWorkerExtensions.AddWorkerInfrastructure` hiện đăng ký **cứng** cả `DataPublicationWorker` lẫn `DataInboundWorker` → đổi sang đăng ký có điều kiện theo `Role`.
- Bản clone phải đổi `C2CGateway:ListenPort` (mặc định `2148`) để không đụng cổng.
- **Production vẫn chạy 1 service duy nhất, 2 chiều** — cờ chỉ phục vụ kiểm thử.

## SV-5. Kịch bản test đa đối tác bằng clone service

- 4 instance `SendOnly` đóng vai A101–A104, mỗi bản khai bộ gói riêng (A1: 101/102/103; A2: 104/105/106; A3: 101/105/103…) + **1 instance `ReceiveOnly`** nhận cả 4.
- **Dùng chung 1 CSDL** — không clone DB (anh Sơn: *"Cần gì clone DB, vẫn xài chung DB hết!"*).
- Tiêu chí đạt: instance nhận parse đúng và ghi đủ dữ liệu của cả 4 đối tác ⇒ coi như luồng đã chuẩn.
- Chỉ thêm cờ **sau khi luồng 1-1 đã chạy chuẩn**.

## SV-6. Đo tải & rủi ro nghẽn CSDL

- Nhiều đối tác cùng chu kỳ **29–30s** cùng truy vấn/insert một bảng → đo CPU / RAM / lock / contention trên `DEV_ITS10` (10.10.8.30) khi 3+ instance cùng lấy gói 101 với điều kiện tìm kiếm khác nhau.
- Nếu DB chính bị ảnh hưởng → **tách CSDL phụ chuyên nhận ShareData rồi đồng bộ có kiểm soát sang DB chính**. Nền đã có: kết nối `InboundConnection` → `DEV_ITS10_Inbound` (`ShareDataWorkerExtensions.InboundConnectionKey`).
- Lưu ý: chiều gửi **chỉ lấy dữ liệu ra xử lý + ghi log**, không ghi ngược xuống DB.

## SV-7. Rà soát cắt cụt chuỗi dài (giới hạn 4000 ký tự)

- **Đã an toàn:** các cột JSON trong module dùng `StaticConfig.CodeFirst_BigString`; tham số `@records` của luồng nhận đã đặt `Size = -1` (`DataInboundService.WriteSql.cs`).
- **Còn phải rà:** các `SugarParameter` chuỗi khác trong câu ghi động, và **cột phía bảng đích của module khác**. Anh Sơn cảnh báo đã có trường hợp bị cắt đuôi (bảng hải quan) — cắt xong parse JSON hỏng mà không báo lỗi rõ ràng.
- Bổ sung kiểm tra độ dài + log lỗi tường minh thay vì để cắt âm thầm.

## SV-8. Ghi log 2 bước cha–con *(đi kèm BE-5 và mục D của FE)*

Một phiên truyền nhận = **1 log cha + 2 log con**:

| Chiều | Step 1 | Step 2 |
|---|---|---|
| Gửi đi (Outbound) | Trích xuất dữ liệu từ CSDL | Xử lý mapping & gửi đi đối tác |
| Nhận về (Inbound) | Tiếp nhận payload gói tin | Xử lý mapping & ghi vào CSDL |

- Chỗ ghi log hiện tại: `Infrastructure/Logging/ShareDataTransferLog.cs`, `DataInboundService.Logging.cs`, `DataPublicationService.LogExportResult`.
- Chặn bởi **BE-5** (`ShareDataActivityLog` chưa có `ParentId` / `StepNo`).

## SV-9. *(Giai đoạn sau)* Xác thực đối tác bằng key / token

- Bản chất đúng: cấp key/token cho từng đối tác, xác định đối tác **từ thông tin đăng nhập** chứ không tin mã nằm trong dữ liệu — chống giả danh & spam hàng loạt.
- Chỉ làm sau khi luồng cơ bản chạy đúng (anh Sơn: *"cái đó để qua sau, trước mắt cho đúng luồng cho nó nhanh trước"*).

## SV-10. Rà soát pipeline Outbound sau refactor *(khung đã xong)*

- Xác nhận tầng gửi (`RestPacketSender` / `FileExportSender`) **không còn logic nghiệp vụ**, chỉ vận chuyển — *"vô trong trỏng chỉ care dữ liệu vào là cái gì rồi gửi đi thôi"*.
- Xác nhận lỗi mapping **dừng ngay bước 2**, ghi log và không đẩy sang bước 3 (nhánh `mapResult` Failed trong `DataPublicationService.cs`).
- Giữ `DataPublicationContext` là context xuyên suốt duy nhất — không truyền tham số rời rạc (`packetCode`, `partnerCode`…).

## SV-11. Thiết kế luồng nhận (Inbound) — **chưa họp chốt**

- Khối lượng ước tính gấp đôi luồng gửi.
- Áp cùng khung tách bước: **Tiếp nhận → Parse & Mapping → Ghi CSDL**; cân nhắc tách thư mục `Processes/` giống Outbound thay vì để partial 4 file.
- **Cần 1 phiên riêng với anh Sơn** (đã hẹn chiều 16/09, ước ~20 phút) — đây là việc chốt còn thiếu lớn nhất của tuyến Service.

---

# PHẦN II — BACKEND WebAPI: `Module.ShareData` (`BE-*`)

*Nguồn: mục 2 biên bản [Review Frontend](../doc/transcript/16-09-2026-review-frontend-sharedata.md) + [Recap sửa UI](../doc/transcript/16-09-2026-sua-ui-sharedata.md) + phụ thuộc từ FE.*

> Mọi thay đổi Entity đi theo **SqlSugar Code-First** (`EnableInitTable` / `EnableIncreTable`) — tuyệt đối không chạy DDL tay (rule 19.4). Script danh mục chỉ **ghi ra file `.sql`**, người dùng tự chạy (rule 9).


- [x] **BE-1** **CRUD Gói tin** — `Controllers/Packet` hiện chỉ có `Queries/` → bổ sung `Commands/` (Add/Update/Delete/BatchDelete) + `Dto/` + `Validators/` (Thin controller + MessBus + Mapster + FluentValidation). Kèm chặn Sửa/Xoá khi `ShareDataSubscription.State = Active`. | *Chặn: Mục B*
- [x] **BE-2** **API đối tác trả đủ Mã + Tên** — `Partner/Queries` Output phải có `code` để FE hiển thị `[Mã] Tên đối tác`. | *Chặn: Mục A*
- [ ] **BE-3** **Danh mục "Trường Meta hệ thống"** — Cấp danh mục key meta (`meta.now`, `request_id`, `partner_code`…) để FE gom nhóm chọn nhanh ở màn Ánh xạ; Backend/Service tự inject khi gửi/nhận. | *Chặn: Mục C* =  Xem xét cấu hình FE 
- [x] **BE-4** **Seed danh mục gói tin 101–111** — Rà & seed đủ `ShareDataPacket` + danh mục `shareData_type` 101–111 để Tester không tự thêm 113/114. Ghi file `.sql`. | *Chặn: DB Danh mục*
- [ ] **BE-5** **Log 2 bước cha–con** — `ShareDataActivityLog` thiếu `ParentId` và `StepNo`. Thêm 2 cột qua Code-First + Query trả cấu trúc cha–con. | *Chặn: Mục D · SV-8*
- [ ] **BE-6** **CodeSet: Default Value + Chiều** — `ShareDataCodeSet.ValuesJson` (DTO chỉ có `sourceValue/partnerValue/displayName/isDefault`) → thêm `direction` (Gửi / Nhận / Cả hai). *(Tạm hoàn tác 17/09/2026 — chờ hỏi lại nghiệp vụ với anh Sơn.)* | *Chặn: Mục C*
- [ ] **BE-7** **Bảng mã lỗi hệ thống** — Khai báo danh mục Error Code chuẩn, phục vụ ghi log và hiển thị trạng thái khi gói tin thất bại. | *Chặn: Mục D*
- [ ] **BE-8** **Endpoint lấy dữ liệu mẫu thật** — Nút "Lấy dữ liệu mẫu" / "Gửi thử" truy vấn bản ghi thật dưới CSDL thay vì dữ liệu giả lập. | *Chặn: Mục C*

- [ ] **BE-10** **Rào Port khi sửa đối tác** — Validator chặn đổi `Port` khi đối tác đang kết nối. SMTP/SSE: giai đoạn sau. | *Chặn: Mục A*
- [ ] **BE-11** **Độ ưu tiên (Priority) — CHƯA LÀM** — Backend chưa có hàng đợi ưu tiên. Giữ field, không xử lý; FE ẩn/disable. | *Chặn: Mục A*



---

# PHẦN III — FRONTEND (`TA-ITS015-WEBVUE-V1.0`)

*Mục A–E. Nguồn: biên bản [Review Frontend](../doc/transcript/16-09-2026-review-frontend-sharedata.md) + [Recap sửa UI](../doc/transcript/16-09-2026-sua-ui-sharedata.md).*

## Phát hiện quan trọng trước khi code

1. **Nhãn tiếng Anh (`Active`, `Connect`, `Disconnect`, `Pending`…) KHÔNG nằm trong file `.vue`.** Chúng được `TagInfo` đọc từ danh mục `base_config` qua `getConfigItemByCode(...)`, khoá danh mục khai trong `types/enums/baseEnum.ts:354-362`: `sharedata_session_state`, `sharedata_sub_state`, `sharedata_sub_mode`, `shareData_type`. ⇒ Sửa nhãn = **cập nhật dữ liệu danh mục `base_config`**, không phải sửa template.
   - ✅ **Đã xử lý trên FE**: Tạo bộ ánh xạ fallback tại `views/sharedata/shared/shareDataLabels.ts` và bổ sung key bản dịch vào `src/i18n/lang/vi-vn.json` để giao diện hiển thị ngay tiếng Việt chuẩn (`Ngắt kết nối` / `Hoạt động` / `Chờ kích hoạt` / `Tạm dừng` / `Từ chối` / Gói tin `101`–`111`...) mà không cần chờ DB seed.
2. **API gói tin chưa có CRUD:** `ShareDataPacketApi` chỉ có `List / Page / Byid / Bycode / Fields` ⇒ CRUD **gói tin** phải disable chờ **BE-1**.
   **⚠️ Đính chính:** `ShareDataPacketFieldApi` tuy có đủ `Add/Update/Delete/Batchdelete` nhưng **KHÔNG** quản lý đám trường đang hiển thị ở màn Cấu hình gói tin. Đó là 2 nguồn khác nhau: trường trên màn hiện tại đến từ `ShareDataTable.FieldsJson` (DTO `ShareDataPacketFieldDto`, **không có `id`**, không có API ghi), còn `ShareDataPacketFieldApi` quản lý thực thể `ShareDataPacketField` (`aliasFieldKey` + `datatypeId`) mà màn **Ánh xạ** đang dùng. ⇒ CRUD trường **chưa làm ngay được**, phải chốt phương án P1/P2 trước.
3. **`ShareDataActivityLogOutput` chưa có `parentId`/`step`** ⇒ log cha–con 2 bước chỉ dựng được khung UI (chờ **BE-5** + **SV-8**) Chưa làm . 
4. **`ShareDataCodeSetValueDto` chỉ có `sourceValue/partnerValue/displayName/isDefault`** — chưa có `direction` ⇒ Default Value theo chiều chờ **BE-6** Chưa làm .
5. **Bug đang có:** `history/component/activityDetailDrawer.vue` so sánh enum dạng **chuỗi** (`'TRANSFER'`, `'FAILED'`, `'SEND'`) trong khi API trả **số** (chỗ đã sửa đúng: `history/index.vue:145-175`) ⇒ panel chi tiết luôn hiện sai Nhóm / Kết quả / Hành động. Phải sửa khi chuyển Drawer → Modal => Để sau .

## A. Cấu hình Đối tác & Đăng ký chia sẻ

**`sharing/component/partnerList.vue`**
- [x] Hiện **Mã đối tác** cạnh trạng thái ở dòng meta (dòng 31-34): Đã render `partner.code` và bọc `el-tooltip effect="dark"` cạnh Tag trạng thái phiên.
- [x] Bỏ `:title="partner.name"` (dòng 12 — đúng chỗ anh Sơn phàn nàn "không xài Tooltip của thư viện"): Đã thay bằng `el-tooltip effect="dark" placement="top"`.

**`sharing/index.vue`**
- [x] **Ẩn thẻ "Cảnh báo"** (dòng 41-49): `alerts` khai ở dòng 210 nhưng không bao giờ được đổ dữ liệu ⇒ luôn hiện 0. Đã comment lại dạng `<!-- ... -->` theo đúng cách 2 thẻ "Gói gửi/nhận" đã ẩn.
- [x] **Tinh giản khối `el-descriptions`** (dòng 85-107): Giữ nguyên UI component chuẩn `el-descriptions` với `:column="4"`; hiển thị 4 trường chính trên 1 dòng gồm **Địa chỉ**, **Lần kết nối gần nhất**, **Ngày tạo**, **Cập nhật gần nhất**. Đã comment ẩn 3 trường không liên quan (**Lần gửi gần nhất**, **Lần nhận gần nhất**, **Lần ngắt gần nhất**) và tắt 2 request API thừa `reloadLastTransfer` khi chọn đối tác để tối ưu hiệu năng. => Xem lại 

**`sharing/component/subscriptionTable.vue`**
- [x] Bỏ cột **Định dạng** (dòng 143): Đã loại bỏ hoàn toàn khỏi cấu hình bảng.
- [x] Thay `:title="toggleTitle(row)"` (dòng 32) bằng `el-tooltip effect="dark" placement="top"`.
- [x] Thu cột Hành động từ `width: 240` → `100` (chỉ còn 2 icon Sửa & Xóa bọc `el-tooltip effect="dark" auto-close="1000"` sau khi các nút Xem trước/Xuất/Duyệt đã comment).

**`sharing/component/editSubscription.vue`**
- [x] Thay `el-switch` **"Theo sự kiện"** (dòng 115-126) bằng **checkbox "Gửi ngay khi có dữ liệu mới"** + `el-tooltip effect="dark"` giải thích cơ chế dò `UpdateTime`/`CreateTime`. Giữ nguyên computed `sendOnEvent` ánh xạ sang `mode = Event/Periodic`. => chưa có logic xử lý trên BE + mong muốn có muồng trường đánh dấu. 
- [x] Bỏ ô **Định dạng** (dòng 162-170) và **ẩn ô Ưu tiên** (dòng 171-176): Đã comment ẩn trên template, giữ nguyên `format: Data`, `priority: 5` trong `defaultForm()` để payload không đổi khi gửi lên API.
- [x] **Chiều nhận (Inbound): ẩn toàn bộ khối lịch** (`v-if="!isInbound"` bọc Kiểu lịch / Chu kỳ / Khung giờ / Ngày trong tuần); `buildSchedule()` trả `undefined` khi `direction = Inbound`.
- [x] Dọn bảng hardcode `DATATYPES` 101–111 (dòng 251-263): Đã xóa bỏ mảng `DATATYPES` tĩnh và Map `DATATYPE_BY_ID`; `onDatatypeChange` chuyển sang đọc `extraValue` từ `datatypeOptions` danh mục cấu hình hệ thống hoặc mặc định `Periodic`. => Check lại các trường hardcode. 

**`sharing/component/editPartner.vue`**
- [x] **Rào Port khi sửa**: `:disabled="props.operateType === 'edit'"` cho ô Cổng + `el-tooltip effect="dark"` "Ngắt kết nối và tạo lại cấu hình nếu cần đổi cổng". Giữ validate 1–65535. => Cân nhắc sửa BE (Validation)
- [ ] SMTP/SSE: **không làm đợt này** (đã chốt để giai đoạn sau).

## B. Cấu hình Gói tin — `dataSource/index.vue`

**Trừu tượng hoá CSDL (quyết định quan trọng của anh Sơn):**
- [x] Bỏ cột **Bí danh** (dòng 299) — đã xóa khỏi danh sách cột của `eshPacketTableGrid`.
- [x] ⚠️ *(đề xuất — cần anh Sơn chốt)* Bỏ nốt các cột lộ cấu trúc DB nội bộ: **Vai trò** (300), **Kiểu nối** (301), **Điều kiện nối** (302-308).
- [x] Đổi cột "Bảng dữ liệu" (dòng 290-298, slot `row_table` dòng 108-110) thành **"Tệp dữ liệu"**, hiển thị tên nghiệp vụ thay `tableName` vật lý. `ShareDataTableOutput` hiện chỉ có `tableName/alias/schemaName` ⇒ tạm dùng `remark`/`alias` làm nhãn kèm `TODO(BE-9)`.

**Layout / cuộn ngang:**
- [x] Bảng trường con (dòng 123-157): giảm `min-width` cột **Mã trường** 150 → 120, cột Tên trường 180 → 160, thêm cột Hành động `fixed="right"` gồm 2 icon Sửa & Xóa bọc tooltip chuẩn `effect="dark"`.
- [x] Cột STT của `optionsTableGrid` đã `fixed: 'left'`: Đã cấu hình `showOverflow: 'tooltip'`, `showHeaderOverflow: 'tooltip'` và `scrollX: { enabled: true, gt: 0 }` cho cả `eshPacketGrid` và `eshPacketTableGrid`; bổ sung rule CSS `:deep(.full-table)` với `min-height: 0` và `flex: 1` để đảm bảo vùng phân trang không bị đè lệch khi bảng cuộn ngang.

**CRUD:**
- [x] **Trường dữ liệu** — ⚠️ **cần chốt phương án trước**: **P1** đổi cột phải sang thực thể `ShareDataPacketField` (CRUD chạy thật ngay, bỏ hẳn grid bảng vật lý — đúng tinh thần "bỏ tên bảng CSDL") hay **P2** giữ bảng vật lý chỉ đọc, hoãn CRUD.
- [x] **Gói tin — dựng UI, disable submit**: `component/editPacket.vue` (Mã, Tên, Phiên bản, Mô tả, Thứ tự) + nút Thêm/Sửa/Xoá trên grid trái; nút Lưu `disabled` + tooltip `Chờ API ShareDataPacket/Add|Update|Delete` (**BE-1**).
- [x] **Khoá sửa khi gói tin đang chạy**: Gọi `ShareDataSubscriptionApi.List`, thu thập các gói tin có subscription `state = Active` vào `activePacketCodes`; cột Hành động trên grid gói tin disable nút Sửa/Xoá + hiển thị `el-tooltip effect="dark"`: *"Gói tin đang chạy, tắt đăng ký trước khi sửa"*.

## C. Ánh xạ dữ liệu

**`mapping/index.vue`**
- [ ] Cột "Gói tin" (slot dòng 82-83) hiển thị `mã - tên` thay vì chỉ tên: tạm hoãn, giữ nguyên hiển thị ban đầu (do tên danh mục đã có sẵn mã `101 - ...`, cần rà soát lại quy ước mã nội bộ `101_commonData` và mã hiển thị). => Đang sửa lại dữ iệu trường datatypeID
- [x] **Bỏ bộ lọc Định dạng** cùng với việc bỏ khái niệm format: đã xóa dropdown Định dạng khỏi `queryForm`, xóa `format` khỏi `state.queryParams` và `resetQuery()`, dọn sạch biến `FORMAT_LIST`. 
- [ ] **Bỏ phiên bản gói tin**  trong modal edit/add mapping.
- [x] **Nhãn menu "Mapping" → "Ánh xạ dữ liệu"**: Đã định nghĩa key i18n (`lz.router.eshMapping` và `"Mapping"`) trong `vi-vn.json` và `en-us.json`, hỗ trợ song ngữ tự động ("Ánh xạ dữ liệu" / "Data Mapping") cho cả Menu, Thẻ Tab TagsView và Breadcrumb mà không cần can thiệp DB hay sửa file `.vue`. Đã chuẩn hoá rule vào `thienan_rules.md` mục 20.5.

**`mapping/component/editMapping.vue`** *(trọng tâm đợt này — 1585 dòng)*
- [X] Tab Thông tin chung: bỏ **Định dạng** (dòng 71-79) 
- [ ] **Phiên bản gói tin** (dòng 80-84); dropdown Gói tin (dòng 30-34) hiện `code - name`. Mã hồ sơ tự sinh đã đúng (`buildMappingCode`, dòng 527-533) — giữ nguyên.
- [ ] Nút **"…"** (dòng 213-215) → icon `ele-Setting` + tooltip "Cấu hình trường".
- [ ] Chuyển `el-popover` thiết lập lá (dòng 210-280) thành **modal chồng** (`el-dialog` `append-to-body`, `:z-index` cao hơn dialog cha).
- [ ] ⚠️ *(cần anh Sơn chốt)* Tuỳ chọn **"Bỏ khoá này khi gửi đi"** (dòng 220-223): anh Sơn nói *"đã nhập nội dung mong muốn gửi đi rồi lại vô bỏ chọn — vậy lúc đầu đừng nhập"* ⇒ bỏ hẳn, hay chỉ ẩn ở chiều gửi và giữ cho chiều nhận?
- [ ] **Badge trạng thái CodeSet / Định dạng** ở khoảng trống giữa 2 cột (thêm sau `el-select` dòng 192-205): icon sáng khi `cfgOf(path).codeSet` / `targetType|defaultValue` có giá trị, hover hiện tên bộ mã / chuỗi format (`el-tooltip effect="dark"`).
- [ ] Đổi nút **"Nạp lại trường gói tin"** (dòng 149-152) → **"Tự động ánh xạ"**: tách khỏi `handlePacketChange` (dòng 671), chỉ gọi `autoMatchLeaves()` + `refreshShape()`, rồi báo `Đã tự động ánh xạ khớp {matched}/{state.leaves.length} trường dữ liệu`.
- [ ] Gom **"Trường Meta hệ thống"** trong dropdown chọn trường (dòng 192-200): `el-option-group` với 2 nhóm — *Trường gói tin* (`state.packetFields`) và *Trường Meta hệ thống* (hằng FE: `meta.now`, `request_id`, `partner_code`…), kèm `TODO(BE-3)`.
- [ ] Tab "Dữ liệu gửi thử": thêm nút **"Lấy dữ liệu mẫu"** (cạnh "Thêm bản ghi", dòng 319-321) — `disabled` + tooltip "Chờ API lấy bản ghi thật từ CSDL" (**BE-8**).

**`codeSet/component/editCodeSet.vue`**
- [ ] **Thêm cột "Chiều áp dụng"** (Gửi đi / Nhận về / Cả hai) cho dòng Giá trị mặc định — control `disabled` + tooltip "Chờ BE bổ sung Direction cho CodeSetValue" (**BE-6**), **không gửi lên payload**. *(Tạm hoàn tác 17/09/2026 — chờ hỏi lại nghiệp vụ với anh Sơn trước khi làm lại.)*


## D. Lịch sử chia sẻ — `history/index.vue` Mai coi (18/09/2026)

- [x] Đổi nhãn radio header: "Cấu hình" → **"Nhật ký cấu hình"**, "Truyền nhận" → **"Nhật ký truyền nhận"** (hoàn tất 17/09/2026).
- [ ] Bộ lọc thời gian: `type="daterange"` → **`type="datetimerange"`**, `value-format="YYYY-MM-DD HH:mm:ss"`, chuẩn hóa `toIsoRange()` và `defaultRange()` (hoàn tất 17/09/2026).
- [x] **Bỏ ô lọc "Nội dung"** và bỏ `keyword` khỏi payload + `resetQuery` (hoàn tất 17/09/2026).
- [ ] **Bỏ ô lọc "Nội dung"** chưa sửa BE       
- [x] Chuẩn hoá nhãn ô lọc còn lại: Đối tác / Hành động / Kết quả (hoàn tất 17/09/2026). Đang hardcode
- [x] Rút gọn danh sách Hành động tab Cấu hình: bỏ Duyệt/Từ chối, chuẩn hóa Tắt / Bật (hoàn tất 17/09/2026).
- [x] **Double-click mở Modal thay Sidebar**: `onCellDblclick` mở component mới `component/activityDetailDialog.vue` (`el-dialog` width 900px, draggable) (hoàn tất 17/09/2026).

**Component chi tiết mới (`activityDetailDialog.vue`)** Mai coi (18/09/2026)
- [x] **Sửa bug enum chuỗi → số**: `row.logType === 1`, `row.success === 0/1`, `row.transferDirection === 0/1`, `ACTION_LABELS` theo dạng số (hoàn tất 17/09/2026).
- [x] Thêm khối **2 bước cha–con** (`el-steps`): *Gửi đi:* Trích xuất DB → Xử lý & gửi; *Nhận về:* Tiếp nhận → Xử lý & ghi DB + ghi chú `TODO(BE-5 / SV-8)` (hoàn tất 17/09/2026).

## E. Tooltip đồng bộ toàn module

- [x] Tất cả tooltip dùng `el-tooltip` mặc định (`effect="dark"` = nền đen chữ trắng). Xoá các chỗ dùng `title` HTML và bọc `el-tooltip` cho các nút thao tác Sửa/Xóa/Làm mới/Chi tiết trong phân hệ `sharedata` (`partnerList.vue`, `subscriptionTable.vue`, `history/index.vue`, `eventSource/index.vue`) (hoàn tất 17/09/2026).

---

## Phụ thuộc FE → BE / Service

| Hạng mục FE | Chặn bởi | Task | Trạng thái FE đợt này |
|---|---|---|---|
| CRUD Gói tin | `ShareDataPacketApi` thiếu Add/Update/Delete | **BE-1** | UI dựng sẵn, submit disable |
| Gom theo Tệp dữ liệu nghiệp vụ | `ShareDataTableOutput` chưa có tên tệp dữ liệu | **BE-9** | Ẩn cột bảng vật lý/bí danh, nhãn tạm |
| Lấy dữ liệu mẫu thật | Chưa có endpoint sample record | **BE-8** | Nút disable + tooltip |
| DefaultValue theo chiều (CodeSet) | `ShareDataCodeSetValueDto` chưa có `direction` | **BE-6** | Control disable, không gửi payload |
| Log cha–con 2 bước | `ShareDataActivityLog` chưa có `ParentId`/`StepNo` | **BE-5** + **SV-8** | Khung `el-steps` rỗng |
| Nhóm trường Meta hệ thống | BE chưa cấp danh mục meta | **BE-3** | Hằng số FE tạm + TODO |
| Khoá sửa khi gói tin đang chạy (tầng API) | BE chưa chặn | **BE-1** | FE suy từ subscription Active |
| Mã lỗi hiển thị ở Lịch sử | Chưa có danh mục Error Code | **BE-7** | Hiện `errorMessage` thô |

---

## Việc cần anh Sơn chốt trước khi bắt đầu

| Tuyến | Nội dung cần chốt |
|---|---|
| **SV** | Cấu trúc payload thống nhất sau khi bỏ vỏ envelope — chốt đúng 1 bản mẫu cho cả 2 chiều (**SV-1**). |
| **SV** | Tên khoá định danh đối tác trong body (`partnerCode`? nằm trong `meta`?) — phải khớp với danh sách key Meta của **BE-3**. |
| **SV** | Lịch họp chốt thiết kế luồng nhận Inbound (**SV-11**) — đang là khoảng trống lớn nhất. |
| **SV** | Ngưỡng nào thì quyết định tách CSDL phụ (**SV-6**). |
| **FE-A** | Bỏ đúng những trường nào trong khối thông tin phiên đối tác. |
| **FE-B** | **P1** (đổi màn sang thực thể `ShareDataPacketField`, CRUD thật) hay **P2** (giữ bảng vật lý chỉ đọc)? |
| **FE-B** | Có bỏ nốt 3 cột Vai trò / Kiểu nối / Điều kiện nối không? |
| **FE-C** | Tuỳ chọn "Bỏ khoá này khi gửi đi" — bỏ hẳn hay chỉ giữ cho chiều nhận? |
| **FE-C** | Có bỏ luôn bộ lọc Định dạng ở màn danh sách ánh xạ không? |
| **FE-D** | `fromDate`/`toDate` backend hiểu theo UTC hay giờ địa phương? |

---

## Kiểm thử

### Frontend

```bash
cd TA-ITS015-WEBVUE-V1.0/src
pnpm build      # build production, bắt lỗi TS/template
pnpm dev        # dev server, cổng theo .env (mặc định 8888)
```

1. **Đối tác**: danh sách hiện `mã + tên`; hover tên dài ra tooltip nền đen; mở modal Sửa → ô Cổng bị khoá; thẻ Cảnh báo không còn.
2. **Đăng ký**: tạo đăng ký chiều **Gửi đi** → có checkbox "Gửi ngay khi có dữ liệu mới", không còn Định dạng/Ưu tiên; chuyển sang **Nhận về** → khối lịch biến mất; lưu thành công và bản ghi cũ mở lại vẫn đúng chu kỳ.
3. **Gói tin**: chọn gói ở cột trái → bảng phải không còn cột Bí danh/Kiểu nối; kéo ngang không đè phân trang, STT ghim trái; nút CRUD **gói tin** disable có tooltip.
4. **Ánh xạ**: mở hồ sơ có sẵn → icon Cấu hình thay "…", modal chồng mở/đóng không đóng dialog cha; bấm "Tự động ánh xạ" ra thông báo `x/y trường`; badge bộ mã sáng đúng ở dòng có CodeSet; lưu lại và mở lại đối chiếu `targetShapeJson` không đổi cấu trúc.
5. **Lịch sử**: lọc `datetimerange` trả đúng khoảng; không còn ô Nội dung; double-click 1 dòng mở **Modal** hiện đúng Nhóm/Kết quả/Hành động (kiểm cả bản ghi Truyền nhận lẫn Cấu hình — đây là bug cũ).

### Backend & Service

```bash
dotnet test tests/test.csproj
```

> 🛑 **Bắt buộc kiểm tra connection string trước khi chạy** (rule 11): mọi chuỗi kết nối phải là local (`localhost`, `127.0.0.1`, `(localdb)`, `.`). Thấy IP remote (`10.10.8.30`) ⇒ **huỷ ngay và báo lại**.

- Test hiện có của tuyến Service: `tests/ShareData/Services/DataPublicationServiceTests.cs`.
- **17/09/2026 — Bug fix `MapCode` + 12 unit test đã pass:** `DataPublicationService.MapCode` trước đây trả thẳng `value` khi `value` là chuỗi rỗng `""` hoặc whitespace, bỏ qua `IsDefault`. Đã sửa: khi `IsNullOrEmpty(valStr)` → kiểm tra `IsDefault` trước, nếu có thì trả `PartnerValue` của dòng default. Đã bổ sung 12 test case (happy case, no-match, empty string, whitespace, null) vào `#region MapCode` trong `DataPublicationServiceTests.cs`, tất cả **pass** (`dotnet test --filter "FullyQualifiedName~MapCode"`).
- Mỗi task SV/BE hoàn thành phải bổ sung test case tương ứng (chuẩn AAA, gọi qua `IMessageBus`, cô lập dữ liệu bằng GUID) và chạy lại toàn bộ test liên quan trước khi báo hoàn tất.
- Kịch bản **SV-5** (4 SendOnly + 1 ReceiveOnly) chạy tay trên môi trường dev, không đưa vào `dotnet test`.

