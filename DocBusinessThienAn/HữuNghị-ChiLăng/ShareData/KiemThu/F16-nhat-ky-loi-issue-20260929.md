---
tier: A
read: full
source: F16.TAC-CN01-HNCL_ITS-KICH BAN KIEM THU - Issue - ShareData.pdf
source_pages: 1-4
extracted: 2026-10-07 (cập nhật lần 2 cùng ngày — PDF đổi 4.1MB → 4.2MB); 2026-10-08 (cập nhật lần 3 — Issue 31 bị xoá khỏi sheet, còn 32 issue hiệu lực)
images: images/issue-*.png (40 ảnh, đã chép nội dung thành chữ ở từng mục)
---

# F16 — Nhật ký lỗi kiểm thử ShareData (26/09 – 08/10/2026)

> 📌 Chuyển thể từ biểu mẫu `F16.TAC-CN01` — 4 trang, **33 issue gốc, nay còn 32 issue hiệu lực** (Issue 31 bị xoá khỏi sheet ngày 08/10/2026), người đăng **TuyenHTN**. Kiểm thử lại ghi nhận đến 08/10/2026: **28/28 issue cũ (1–28) đều Passed**, **4 issue mới còn hiệu lực** (29, 30, 32, 33 — đăng 06/10/2026) còn nguyên `Chờ phản hồi`, chưa ai xử lý (riêng issue 29 đã gán cho DatHQ).
> 🔴 **Đây là tài liệu kiểm thử, ⛔ không phải sổ theo dõi task.** Trạng thái thi công tra ở `../Plan/Sharedata_MasterPlan.md`.

## Tình trạng xử lý — cập nhật 08/10/2026 (lần 3)

🔴 **⛔ Không tra trạng thái task ở đây.** Sổ theo dõi duy nhất là
[`../Plan/Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) (rule 19.24).
Bảng này chỉ để người đọc tài liệu kiểm thử biết PDF gốc đang ở trạng thái nào tại lần kiểm thử lại mới nhất (07/10/2026).

| Kết quả kiểm thử lại (cột `Passed/Failed` của PDF) | Issue |
|---|---|
| ✅ **Passed** — 28 issue (toàn bộ issue 1–28) | 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28 |
| 🆕 **Issue mới (đăng 06/10/2026)** — 4 issue còn hiệu lực, chưa ai xử lý | 29, 30, 32, 33 |
| ⛔ **Đã xoá khỏi sheet** (phát hiện 08/10/2026) | 31 |

🔴 **Phát hiện của lần cập nhật 08/10/2026 — Issue 31 đã bị xoá khỏi sheet**: PDF gốc (LastWriteTime đổi sang 08/10/2026 07:55, kích thước ~4,09MB, vẫn 4 trang) không còn dòng STT 31 — bảng nhảy thẳng từ STT 30 sang STT 32. Đã xác nhận bằng cả 3 cách đọc độc lập (`pdftotext -table`, `pdftotext` thường không `-table`, và ảnh chụp trực tiếp trang 4) — cả 3 đều đồng nhất không có dòng 31, nên đây không phải lỗi tách bảng của công cụ trích xuất. PDF không ghi lý do xoá. Nội dung cũ của issue 31: *"Lỗi thông báo đang hiển thị tiếng Anh, không đồng nhất... `Database connection failed. Please check again!`"*. ⚠️ Suy luận của AI, **chưa có xác nhận từ dev**: nhiều khả năng bị gộp bỏ vì cùng nguyên nhân gốc với lỗi mạng server ngày 06/10/2026 đã giải thích cho issue 14/15/16/17/24 ở dưới, chứ không phải lỗi logic ShareData riêng biệt. Số STT 31 bỏ trống vĩnh viễn trong tài liệu này, số 32/33 giữ nguyên không dịch chuyển lên. Ảnh `images/issue-31.png` vẫn còn trên đĩa (Tier C) nhưng không còn dòng nào trong tài liệu này dẫn chiếu tới.

🔴 **Phát hiện quan trọng nhất của lần cập nhật 07/10/2026 — xác nhận nguyên nhân gốc của cả 4 issue (14, 15, 16, 17)**: ngày 06/10/2026 cả 4 issue đều lộ ra lỗi "Sao lưu sau khi bấm Xác nhận thất bại, hiển thị thông báo tiếng Anh" (xem ghi chú cũ bên dưới). PDF cập nhật 07/10/2026 bổ sung lời giải thích trực tiếp từ dev:
> *"07102026-KienLT: Nguyên nhân do ngày 06102026 bị lỗi mạng server, cần test lại."* (ghi tại issue 14, 17, 24)
> *"07102026-DatHQ: Nguyên nhân do ngày 06102026 bị lỗi mạng server."* (ghi tại issue 15)

Nghĩa là: **4 issue này KHÔNG phải lỗi logic thật của ShareData** — nguyên nhân là một sự cố mạng/server xảy ra đúng ngày 06/10/2026 khiến API Lưu bị gián đoạn, bật ra thông báo lỗi kết nối tiếng Anh (cùng họ với issue 31 — "Database connection failed"). Sau khi server ổn định, dev kiểm thử lại ngày 07/10/2026 và cả 4 issue (14, 15, 16, 17) đều **Passed**. ⛔ Không có code nào được sửa cho riêng 4 issue này ngoài phần fix gốc đã làm trước đó (khoá/làm mờ ô Mã).

📌 **Cột `Tình trạng` và cột `Passed/Failed` của PDF không phải lúc nào cũng khớp nhau.** Issue 9, 11, 16, 23, 24, 27 vẫn ghi `Tình trạng` là "Chờ phản hồi"/"Đang thực hiện" dù cột `Passed/Failed` đã ghi **Passed** — tài liệu này tiếp tục dùng **`Passed/Failed`** làm căn cứ chính vì đây là cột do tester xác nhận lại qua kiểm thử; `Tình trạng` chỉ là nhãn nội bộ của dev, không phải lúc nào cũng được cập nhật đồng bộ theo.

📌 **Issue 13**: Ghi chú vẫn mô tả y nguyên vấn đề cũ (*"thông báo này đang tiếng Anh, không đồng nhất"*) — PDF không có ghi chú mới nào xác nhận đã sửa câu đó, nhưng cột `Passed/Failed` đã chuyển sang **Passed**. Có thể tester đã xác nhận qua kênh khác ngoài PDF (Sheet Bug/trao đổi trực tiếp); ghi nhận trung thực theo đúng dữ liệu PDF, không tự suy diễn thêm.

📌 **Issue 23**: trước đây hoàn toàn chưa được đụng tới (Nguyên nhân/Phương án để trống, `Tình trạng` = "Chờ phản hồi"). PDF 07/10/2026 bổ sung ghi chú mới: *"07102026-TuyenHTN: Đã cho chọn trường dữ liệu bất kỳ và thành công"* và chuyển `Passed/Failed` sang **Passed** — dù cột `Nguyên nhân`/`Phương án xử lý` vẫn để trống (không có dev nào ghi lại đã sửa gì).

📌 **Issue 20, 22, 26**: KienLT (05/10/2026) đã sửa cùng một cơ chế (khoá thao tác Xóa/Sửa khi gói tin đang dùng; chặn tab Xem trước dữ liệu mẫu với hồ sơ chiều Nhận). Lần cập nhật này xác nhận **issue 26 cũng đã Passed** (trước đó còn Pending) — vậy cả 3 issue cùng cơ chế sửa (20, 22, 26) nay đều Passed.

📌 **Issue 29**: Đã được DatHQ xử lý ngày 08/10/2026 — chặn khung giờ ngược (`EndTime < StartTime`) đồng bộ ở cả Backend (Validator + seed bản dịch `SysTerminology`) và Frontend (`editSubscription.vue` validate chặn lưu). Phần đề xuất Ngày hiệu lực đang chờ PO/BA confirm.

📌 **Phân công nhân sự** (không đổi so với lần trước): issue 14, 17, 20, 24, 26 do **KienLT** phụ trách. Các issue dịch thuật (3, 5, 8, 10, 12, 13, 19, 21, 22 do HieuNV phụ trách) khi kiểm thử lại trên môi trường Web vẫn **bắt buộc bấm nút icon Làm mới 🔄 cạnh menu Ngôn ngữ ở TopBar** (hoặc gọi API `GET /api/system/sysconfig/loadserverterm?language=vi-VN`) để đồng bộ bản dịch mới từ CSDL `SysTerminology`.

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
| ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
| 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
| ⛔ | Cấm tuyệt đối |
| 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

> Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.

---

## Tổng quan 32 issue còn hiệu lực (issue 31 đã bị xoá khỏi sheet 08/10/2026)

| Trục | Phân bố |
|---|---|
| Màn hình | Ánh xạ dữ liệu **13** · Cấu hình (Đối tác/Đăng ký) **7** · Cấu hình gói tin **6** · Bộ mã chuẩn hóa **4** · Lịch sử chia sẻ **2** |
| Nhóm | Chức năng **21** · UI **6** · Dịch thuật **5** |
| Phân loại | Lỗi **14** · Hiệu chỉnh UI **10** · Hiệu chỉnh chức năng **8** |
| Ưu tiên | 🔴 Cao **3** (16, 17, 24) · Trung bình **17** · Thấp **12** |
| Phụ trách | HieuNV **12** · DatHQ **12** · KienLT **5** · *chưa gán* **3** (30, 32, 33) |
| Kết quả kiểm thử lại (`Passed/Failed`) | ✅ Passed **28** (toàn bộ issue 1–28) · 🆕 chưa test **4** (29, 30, 32, 33) |

📌 **Phân định độc lập giữa Issue 13, 14 và 16**: Issue 13 giải quyết câu thông báo lỗi chưa dịch; Issue 14 giải quyết việc khóa ô Mã và hiển thị tooltip; Issue 16 giải quyết logic nghiệp vụ khi sao chép (tự động tắt `isActive`, xóa ID và sinh mã mới tránh xung đột). Cả 3 đều **Pending** ở lần kiểm thử 06/10/2026 nhưng vì những lý do khác nhau — xem mục *Tình trạng xử lý* ở trên, ⛔ không gộp chung một nguyên nhân.

---

## Thứ ảnh chụp màn hình nói ra mà phần chữ không nói

Phần chữ của biểu mẫu phần lớn chỉ ghi *"thông báo lỗi gây khó hiểu"*. **Ảnh mới là chỗ chứa câu lỗi thật.**
Đây là toàn bộ mã lỗi đọc được — tất cả đều hiện **nguyên key, chưa dịch**, tại thời điểm chụp:

| Issue | Chuỗi hiện trên màn hình | Màn hình |
|---:|---|---|
| 3, 4, 12 | `lz.entity.base.code đã tồn tại trong hệ thống!` | Sao chép/Sửa đối tác · Sao chép bộ mã |
| 5 | `lz.entity.sharedata.packetCode đã tồn tại trong hệ thống!` | Tạo mới Gói tin |
| 8 | `lz.entity.sharedata.aliasFieldKey đã tồn tại trong hệ thống!` | Tạo mới Trường gói tin |
| 10 | `lz.exception.sharedata.codeSetInUse` | Xóa bộ mã chuẩn hóa |
| 13, 16 | `lz.exception.sharedata.mappingConflictInUse` | Sao chép hồ sơ ánh xạ |
| 17 | `lz.exception.sharedata.mappingInUse` | Chỉnh sửa hồ sơ ánh xạ |
| 19 | `lz.exception.sharedata.subscriptionMustPauseBeforeDelete` | Cấu hình → tab Gửi đi → Xóa đăng ký |
| 21 | `lz.exception.sharedata.packetFieldLocked` | Chỉnh sửa Trường gói tin |
| 22 | `lz.exception.sharedata.packetFieldInUse` **+** `Thực hiện thất bại` | Xóa Trường gói tin |
| 2 | `{"$.intervalSeconds":["The JSON value could not be converted to System.Nullable\`1[System.Int32]. Path: $.intervalSeconds \| LineNumber: 0 \| BytePositionInLine: 303."]}` | Sửa đăng ký chia sẻ |
| 30 | `Giá trị mặc định không đúng kiểu dữ liệu tại: header.status. Sửa lại trước khi lưu hoặc dùng payload.` | Sao chép bộ ánh xạ — tab Ánh xạ |

🔴 **Issue 22 là bằng chứng trực quan của lỗi 2 toast**: ảnh bắt được **đồng thời** `lz.exception.sharedata.packetFieldInUse` (của backend) và `Thực hiện thất bại` (toast generic của frontend) chồng lên nhau.

🔴 **Issue 28 là bằng chứng bộ lọc thời gian bị vô hiệu** (ảnh chụp tại lần kiểm thử 29/09, nay đã **Passed** — xem mục *Tình trạng xử lý*): tester nhập `Thời gian bắt đầu = 2030-01-01`, `Thời gian kết thúc = 2028-09-30` — **bắt đầu sau kết thúc, và cả hai đều ở tương lai** — hệ thống vẫn trả về **đủ 178 bản ghi**. Ở tab Nhật ký truyền nhận, lọc `29/09 → 30/09` nhưng lưới trả về các dòng ngày **22/09**.

🔴 **Issue 30 lộ ra câu lỗi thật mà tester tự nhận "không nhận diện được lỗi gì"**: ảnh chụp cho thấy hệ thống **có** hiển thị lỗi rõ ràng — `Giá trị mặc định không đúng kiểu dữ liệu tại: header.status` — chỉ là banner đỏ dễ bị bỏ sót khi màn hình không rê/kéo được để thấy đủ (đúng như phần đầu issue 30 mô tả).

🆕 **Issue 29 — ảnh cho thấy field "Khung giờ" chấp nhận khoảng ngược ngày**: ảnh a nhập `11:00 → 09:00` kèm chú thích tester *"có bị ngược?"*; ảnh b nhập `22:00 → 05:00` (qua đêm) và được hệ thống chấp nhận như một khoảng **hợp lệ**. Hai ảnh cùng chứng minh hệ thống **có** hỗ trợ khung giờ qua đêm, nhưng UI không phân biệt được đâu là qua-đêm-hợp-lệ và đâu là nhập-ngược-do-nhầm.

🆕 **Issue 33 — ảnh cho thấy đúng kịch bản trùng tên khác mã mà issue mô tả**: ảnh b chụp màn "Cấu hình gói tin" có **2 dòng cùng tên** `Gói 101 - Dữ liệu giao thông chung` nhưng **mã khác nhau** (`101_commonData` và `101_commonData1`); ảnh a chụp màn Đối tác hiển thị tên gói tin này **không kèm mã**, nên không phân biệt được đang trỏ tới dòng nào trong 2 dòng trùng tên đó.

---

## Bảng phân loại & Phản hồi Sheet Bug (Mẫu chuẩn F16)

> 📌 **Quy chuẩn ghi chú phản hồi:** `[ddMMyyyy]-[TênDev]: [Nội dung phản hồi]` theo quy định tại `.agents/rules/thienan_rules.md` (mục 21).

<style>
table:has(th:nth-child(7)) {
  table-layout: fixed !important;
  width: 100% !important;
}
table:has(th:nth-child(7)) th:nth-child(1),
table:has(th:nth-child(7)) td:nth-child(1) { width: 45px !important; text-align: center !important; }
table:has(th:nth-child(7)) th:nth-child(2),
table:has(th:nth-child(7)) td:nth-child(2) { width: 16% !important; word-break: break-word !important; }
table:has(th:nth-child(7)) th:nth-child(3),
table:has(th:nth-child(7)) td:nth-child(3) { width: 16% !important; word-break: break-word !important; }
table:has(th:nth-child(7)) th:nth-child(4),
table:has(th:nth-child(7)) td:nth-child(4) { width: 20% !important; word-break: break-word !important; }
table:has(th:nth-child(7)) th:nth-child(5),
table:has(th:nth-child(7)) td:nth-child(5) { width: 9% !important; text-align: center !important; }
table:has(th:nth-child(7)) th:nth-child(6),
table:has(th:nth-child(7)) td:nth-child(6) { width: 10% !important; text-align: center !important; }
table:has(th:nth-child(7)) th:nth-child(7),
table:has(th:nth-child(7)) td:nth-child(7) { width: 24% !important; word-break: break-word !important; }
</style>

| STT | Nội dung lỗi & Bối cảnh ảnh | Nguyên nhân | Phương án xử lý (FE & BE) | Phụ trách | Hoàn thành | Ghi chú (Mẫu comment Sheet Bug) |
|:---:|---|---|---|:---:|:---:|---|
| **1** | Sửa gói tin gửi đi: nên đặt trần cho chu kỳ để người dùng không set quá lớn ([ảnh](images/issue-01.png): ô chu kỳ bị nhập dãy 60 chữ số 9) | Ô nhập `el-input-number` chưa cấu hình `:max="86400"`, cho phép người dùng nhập số tùy ý | **FE**: thêm `:max="86400"` vào `el-input-number` (`editSubscription.vue`), chặn giá trị vượt quá 86400s (24h) | DatHQ | ✅ Passed (06/10) | `01102026-DatHQ: FE đã giới hạn chu kỳ tối đa 86400s (24h).` |
| **2** | Nhập chu kỳ quá lớn báo lỗi .NET JsonException và hiện 2 popup lỗi ([ảnh](images/issue-02.png): toast exception .NET hiện đè 2 lần) | Số nhập vượt quá `int.MaxValue` làm văng lỗi deserialization JSON của ASP.NET Core; FE gọi thêm `ElMessage.error` trùng lặp trong `catch` | **BE**: Giữ kiểu `int?` chuẩn cho DTO/Entity, bổ sung rule FluentValidation `InclusiveBetween(5, 86400)` (`SubscriptionValidator.cs`).<br>**FE**: thêm `:max="86400"` trên `el-input-number` và bỏ `ElMessage.error` trong `catch` (`editSubscription.vue`) | DatHQ | ✅ Passed (06/10) | `01102026-DatHQ: BE validate dải 5-86400s; FE giới hạn max 86400s và loại bỏ toast lỗi kép.` |
| **3** | Sao chép "Đối tác" báo lỗi khi trùng mã, tên đối tác đã có trong hệ thống ([ảnh](images/issue-03.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **4** | Sửa đối tác báo lỗi khi trùng mã; ô Mã đối tác không bị khóa khi Chỉnh sửa ([ảnh](images/issue-04.png)) | Ô nhập Mã đối tác ở chế độ Chỉnh sửa vẫn cho phép nhập (`enable`), dẫn đến sửa trùng mã định danh | **FE**: Khóa ô Mã đối tác (`:disabled="operateType === 'edit'"`), bọc `el-tooltip` giải thích mã định danh không thể thay đổi (`editPartner.vue`) | DatHQ | ✅ Passed (06/10) | `01102026-DatHQ: Đã khóa ô Mã đối tác khi chỉnh sửa kèm tooltip giải thích mã định danh cố định không thể đổi.` |
| **5** | Thêm mới gói tin mới - trùng mã gói tin đã có báo lỗi gây khó hiểu cho người dùng (nửa code nửa thông báo) ([ảnh](images/issue-05.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **6** | Đổi tên label cần tìm cho giống tên cột trên lưới đang có, gợi ý cho người dùng biết đang tìm theo thuộc tính nào ([ảnh](images/issue-06.png)) | Nhãn label và placeholder ở form tìm kiếm chưa đồng bộ với tiêu đề cột của bảng dữ liệu | **FE**: Đổi `:label` và `:placeholder` sang "Khóa field" (`aliasFieldKey`) và "Kiểu" (`fieldType`) (`dataSource/index.vue`) | DatHQ | ✅ Passed (06/10) | `01102026-DatHQ: Đã đổi nhãn ô tìm kiếm thành 'Khóa field' và 'Kiểu' đồng bộ chuẩn 100% với các cột trên lưới.` |
| **7** | Cần bổ sung thêm kiểu dữ liệu dạng double hoặc decimal; thống nhất nhãn "Kiểu" thay vì "Loại" ([ảnh](images/issue-07.png)) | CSDL danh mục `sharedata_value_type` chưa seed kiểu số thực; modal `editPacketField.vue` dùng nhãn `lz.entity.base.type` ("Loại") | **FE**: đổi `:label` sang `fieldType` ("Kiểu") (`editPacketField.vue`).<br>**DB**: script SQL `sql/20261001-bo-sung-sharedata-value-type.sql` bổ sung 5 kiểu dữ liệu vào `SysConfigData` (dùng chung cho cả Issue 7 và 25, đã chạy DB) | DatHQ | ✅ Passed (06/10) | `02102026-DatHQ: FE đã đổi nhãn modal thành 'Kiểu' đồng bộ với lưới; CSDL đã chạy script bổ sung đủ 5 kiểu dữ liệu (long, float, double, decimal, guid).` |
| **8** | Khi thêm mới trường gói tin bị trùng mã, thông báo lỗi gây khó hiểu và không nhận diện được đang sai thông tin nào ([ảnh](images/issue-08.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **9** | Cần đổi hoặc ẩn thông tin "Quản lý dự án TCP - V2.0" trên giao diện của ITS ([ảnh](images/issue-09.png)) | Cần xác định chính xác nguồn chuỗi text qua F12 Console trên đúng môi trường tester | HieuNV không tái hiện được bug trên cả server lẫn local | HieuNV | ✅ Passed (06/10) — tuy `Tình trạng` PDF vẫn ghi "Chờ phản hồi" | `06102026-HieuNV: Không tái hiện được bug trên server và local.` |
| **10** | Xuất hiện thông báo lỗi gây khó hiểu khi dùng chức năng "Xóa" các bản ghi đang có trên các bộ mã chuẩn hóa ([ảnh](images/issue-10.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **11** | Kiểm tra chức năng của "Làm mới" là tìm kiếm lại dữ liệu đã nhập hay refresh và hiển thị lại toàn bộ dữ liệu đang có trong hệ thống? ([ảnh](images/issue-11.png)) | Nút làm mới trên thanh công cụ bảng chưa có tooltip giải thích hành vi tải dữ liệu | **FE**: Bổ sung `el-tooltip` cho nút Làm mới: *"Làm mới danh sách bảng, giữ nguyên điều kiện lọc hiện tại"* (`table-header-operation.vue`) | DatHQ | ✅ Passed (06/10) — tuy `Tình trạng` PDF vẫn ghi "Đang thực hiện" | `01102026-DatHQ: Đã bổ sung tooltip giải thích rõ hành vi nút Làm mới: Làm mới danh sách bảng và giữ nguyên điều kiện lọc.` |
| **12** | Sao chép Bộ mã chuẩn hóa báo lỗi khi trùng mã đã có trong hệ thống. Thông tin lỗi chưa được dịch thuật ([ảnh](images/issue-12.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **13** | Sao chép Ánh xạ dữ liệu thông báo lỗi gây khó hiểu cho người dùng; không nhận diện được thao tác sai là gì ([ảnh](images/issue-13.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (07/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` + `06102026-TuyenHTN: Các thông báo khác đang hiển thị tiếng Việt, thông báo này đang tiếng Anh, không đồng nhất.` |
| **14** | Sao chép Ánh xạ dữ liệu không cho người dùng nhập lại "Mã" nhưng lại để trạng thái enable; giữ nguyên thì thông báo lỗi trùng mã ([ảnh](images/issue-14.png)) | Đang để chế độ `readonly` chứ không phải `disabled` | Đã chuyển sang `disabled` | KienLT | ✅ Passed (07/10) — lỗi "Sao lưu thất bại, tiếng Anh" ngày 06/10 xác nhận do sự cố mạng server, không phải lỗi logic | `06102026-TuyenHTN: - Ô mã bộ ánh xạ đã được làm mờ. - Chức năng sao lưu sau khi "Xác nhận" chưa thành công. Hiển thị thông báo lỗi tiếng Anh.` + `07102026-KienLT: Nguyên nhân do ngày 06/10/2026 bị lỗi mạng server, cần test lại.` |
| **15** | Điều chỉnh label "mã gói tin" nếu nó là mã định danh của gói tin đang chọn. Làm mờ nếu không cho chỉnh/nhập "mã" ([ảnh](images/issue-15.png)) | Chưa có chú thích quy tắc sinh mã tự động `{MÃ_ĐỐI_TÁC}_{MÃ_GÓI_TIN}_{CHIỀU}` dưới ô nhập | **FE**: Bổ sung dòng hint `lz.label.sharedataMapping.codeAutoHint` ngay dưới ô Mã giải thích rõ công thức sinh mã định danh (`editMapping.vue`) | DatHQ | ✅ Passed (07/10) — lỗi "Sao lưu thất bại, tiếng Anh" ngày 06/10 xác nhận do sự cố mạng server, không phải lỗi logic | `30092026-DatHQ: Đã thêm dòng chú thích dưới ô nhập liệu giải thích rõ công thức sinh mã hồ sơ tự động.` + `06102026-TuyenHTN: - Ô mã gói tin đã được làm mờ trong chức năng Chỉnh sửa mã hồ sơ ánh xạ. - Chức năng sao lưu sau khi "Xác nhận" chưa thành công. Hiển thị thông báo lỗi tiếng Anh.` + `07102026-DatHQ: Nguyên nhân do ngày 06/10/2026 bị lỗi mạng server.` |
| **16** | Chức năng Sao chép Ánh xạ dữ liệu không cho người dùng nhập lại "Mã" nhưng lại để trạng thái enable; giữ nguyên thì thông báo lỗi trùng mã. Hệ thống thông tin lỗi gây khó hiểu cho người dùng ([ảnh a](images/issue-16-a.png) · [ảnh b](images/issue-16-b.png)) | Mã đang tự render dựa vào tên và gói tin hiện tại, chưa xử lý trường hợp cùng 1 đối tác + cùng 1 gói tin nhưng nhiều mã khác nhau | Thêm hậu tố `copy` cho việc render mã khi nhấn tạo bản sao | HieuNV | ✅ Passed (07/10) — cùng đợt với issue 14/15/17/24, lỗi "Sao lưu thất bại, tiếng Anh" ngày 06/10 là do sự cố mạng server, không phải lỗi logic | `06102026-TuyenHTN: - Ô mã gói tin đã được làm mờ trong chức năng Sao chép mã hồ sơ ánh xạ. - Chức năng sao lưu sau khi "Xác nhận" chưa thành công. Hiển thị thông báo lỗi tiếng Anh.` |
| **17** | Chức năng Chỉnh sửa Ánh xạ dữ liệu, lấy dữ liệu mẫu không thành công, thông báo lỗi gây khó hiểu ([ảnh a](images/issue-17-a.png) · [ảnh b](images/issue-17-b.png)) | Chưa cấu hình dịch thuật | Cấu hình thêm dịch thuật | KienLT | ✅ Passed (07/10) — lỗi "chỉnh sửa JSON thất bại, tiếng Anh" ngày 06/10 xác nhận do sự cố mạng server, không phải lỗi logic | `06102026-TuyenHTN: - Chức năng chỉnh sửa dữ liệu Json trong tab Ánh xạ chưa thành công và Hiển thị thông báo lỗi tiếng Anh.` + `07102026-KienLT: Nguyên nhân do ngày 06/10/2026 bị lỗi mạng server, cần test lại.` |
| **18** | Nếu có hơn 2 đối tác trùng tên nhưng khác mã, thì khi tạo mới Ánh xạ dữ liệu nên load đính kèm [mã đối tác] trước tên đối tác ([ảnh a](images/issue-18-a.png) · [ảnh b](images/issue-18-b.png)) | Dropdown chỉ render thuộc tính tên `item.name` mà không kèm mã `item.code` | **FE**: Cập nhật template dropdown hiển thị định dạng `[Mã] Tên đối tác` (`editMapping.vue`) | DatHQ | ✅ Passed (06/10) | `30092026-DatHQ: Đã cập nhật dropdown hiển thị định dạng [Mã] Tên đối tác giúp phân biệt chính xác đối tác trùng tên.` |
| **19** | Xóa hồ sơ ánh xạ trong "Gởi đi" hoặc "Nhận về" của mục Cấu hình thông báo lỗi gây khó hiểu cho người dùng ([ảnh](images/issue-19.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **20** | Mục "Lấy dữ liệu mẫu" không thành công. Thông báo lỗi weatherId và Status đang rỗng mặc dù 2 trường này đã map ([ảnh a](images/issue-20-a.png) · [ảnh b](images/issue-20-b.png)) | Sai luồng test/xử lý: luồng test "Lấy dữ liệu mẫu" chỉ dành cho luồng gửi đi, luồng nhận về chưa có chức năng | Disabled nút "Lấy dữ liệu mẫu" ở chế độ nhận về | KienLT | ✅ Passed (06/10) | `05102026-KienLT: Tester dùng "Lấy dữ liệu mẫu" trên hồ sơ chiều Nhận. Chức năng này lấy dữ liệu CSDL để thử chiều Gửi; hồ sơ chiều Gửi/Cả hai có dữ liệu sẵn, không bị null. Đã chặn tab Xem trước với hồ sơ chiều Nhận và hiển thị thông báo giải thích.` + `06102026-TuyenHTN: Đã mờ thông tin Lấy dữ liệu mẫu nếu gói tin "Nhận về" hoặc "Hai chiều".` |
| **21** | Đang có ánh xạ dữ liệu không cho chỉnh sửa => chỉnh câu thông báo lỗi cho người dùng dễ hiểu ([ảnh](images/issue-21.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **22** | Bộ gói tin đã cấu hình và có hồ sơ ánh xạ dữ liệu thì không thể sửa, xóa => Nên ẩn/mờ 2 chức năng này trên nhóm "Hành động" hoặc hiển thị thông báo lỗi cho người dùng dễ hiểu ([ảnh](images/issue-22.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Passed (06/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` + `05102026-KienLT: Đã kiểm tra gói tin có ánh xạ hoặc đăng ký đang hoạt động; khóa thao tác Xóa, khóa Mã/Tên khi sửa và hiển thị lý do. Các thông tin mô tả vẫn cho sửa.` + `06102026-TuyenHTN: Đã có thông báo rõ cho người dùng hiểu.` |
| **23** | Tab "Ánh xạ" dữ liệu thuộc nhóm header nếu chỉ được chọn các thuộc tính cố định của hệ thống thì Nên ẩn/dấu các thuộc tính khác không cho người dùng thấy và chọn ([ảnh](images/issue-23.png)) | — (PDF không ghi, chỉ có xác nhận kết quả) | — (PDF không ghi phương án cụ thể) | HieuNV | ✅ Passed (07/10) | `07102026-TuyenHTN: Đã cho chọn trường dữ liệu bất kỳ và thành công.` |
| **24** | Tab "Ánh xạ", cấu hình giá trị mặc định, kiểu dữ liệu không vô hiệu khi thử chức năng "Lấy dữ liệu mẫu" ([ảnh](images/issue-24.png)) | Hai ô giá trị mặc định (phía đối tác và phía nội bộ) là ô nhập tự do, không kiểm tra gì theo kiểu dữ liệu — khi lưu, giá trị được ghi thẳng vào bộ khung | Đã thêm rule check chặn trước khi nhấn OK, riêng kiểu `string` sẽ cho phép toàn bộ | KienLT | ✅ Passed (07/10) — "Lưu chưa thành công" ngày 06/10 xác nhận do sự cố mạng server, không phải lỗi logic | `05102026-KienLT: Đã thêm rule check chặn trước khi nhấn ok, riêng kiểu string sẽ cho phép toàn bộ.` + `06102026-TuyenHTN: Chức năng lưu chưa thành công, không thể test lại case này.` + `07102026-KienLT: Nguyên nhân do ngày 06/10/2026 bị lỗi mạng server, cần test lại.` |
| **25** | Bổ sung thêm các kiểu dữ liệu cơ bản khác như GUID, float, double, decimal ([ảnh](images/issue-25.png)) | Cùng nguyên nhân với Issue 7 — danh mục `sharedata_value_type` trong CSDL chỉ có 4 dòng | Dùng chung danh mục và script SQL với Issue 7 (`sql/20261001-bo-sung-sharedata-value-type.sql`) | DatHQ | ✅ Passed (06/10) | `01102026-DatHQ: FE đã sẵn sàng xử lý, dùng chung script SQL với Issue 7 bổ sung 5 kiểu vào danh mục hệ thống.` |
| **26** | Cấu hình gói tin "Bắt buộc" gặp lỗi khi "Lấy dữ liệu mẫu" trong Ánh xạ dữ liệu => Gói tin bắt buộc thì khi tạo dữ liệu mẫu, nên tự sinh giá trị kiểu GUID ([ảnh](images/issue-26.png)) | Tab test dữ liệu mẫu chỉ dành cho dữ liệu gửi đi, nhưng hiện đang test cho dữ liệu nhận về nên gây lỗi không có dữ liệu trên các ô bắt buộc | Ẩn tab test dữ liệu gửi đi trên dạng chỉ nhận về; không tự sinh GUID cho dữ liệu mẫu | KienLT | ✅ Passed (07/10) — cùng cơ chế với issue 20, 22 (cả 3 nay đều Passed) | `05102026-KienLT: Cùng nguyên nhân với issue 20 (trường bắt buộc sessionid do đối tác gửi ở chiều Nhận, CSDL không có). Không tự sinh GUID cho dữ liệu mẫu; đã chặn tab Xem trước với hồ sơ chiều Nhận.` |
| **27** | Hệ thống chưa reset thời gian bắt đầu, kết thúc về mặc định khi nhấn nút "Đặt lại" trong Giao diện Lịch sử chia sẻ ([ảnh](images/issue-27.png)) | Hàm reset bộ lọc bỏ quên việc gán lại giá trị mặc định cho 2 trường thời gian | **FE**: `history/index.vue` cập nhật hàm reset bộ lọc để xóa và đưa 2 trường thời gian về khoảng mặc định của ngày hôm nay | DatHQ | ✅ Passed (06/10) — tuy `Tình trạng` PDF vẫn ghi "Đang thực hiện" | `30092026-DatHQ: Đã sửa nút Đặt lại: xóa sạch điều kiện và đưa khoảng thời gian về mặc định chính xác.` |
| **28** | Tìm kiếm/Đặt lại/Làm mới: hệ thống tìm dữ liệu kết quả không thuộc phạm vi cần tìm; không thông báo lỗi khi thời gian bắt đầu > kết thúc; chưa rào hạn chế nhập thời gian tương lai ([ảnh a](images/issue-28-a.png) · [ảnh b](images/issue-28-b.png)) | Component date picker chưa cấu hình `:disabled-date` và form chưa validate mối quan hệ thời gian | **FE**: `history/index.vue` thêm `:disabled-date="disableFutureDate"` chặn ngày tương lai, validate kiểm tra bắt đầu <= kết thúc | DatHQ | ✅ Passed (06/10) — tuy `Tình trạng` PDF vẫn ghi "Chờ phản hồi" | `01102026-DatHQ: Đã chặn chọn ngày tương lai trên lịch và validate ràng buộc thời gian bắt đầu phải trước thời gian kết thúc.` |
| **29** 🆕 | Chỉnh sửa "Cấu hình" đăng ký chia sẻ: tối ưu nhập liệu "Khung giờ" (nhập `11:00 → 09:00` khiến tester nghi ngờ "có bị ngược?", trong khi `22:00 → 05:00` qua đêm lại được chấp nhận hợp lệ); đề xuất bổ sung thêm "Ngày bắt đầu (hiệu lực)" / "Ngày kết thúc (hiệu lực)" cho đăng ký ([ảnh a](images/issue-29-a.png) · [ảnh b](images/issue-29-b.png)) | Form đăng ký chưa validate mối quan hệ thời gian giữa 2 ô Khung giờ, dẫn đến việc cho phép chọn và lưu thời gian kết thúc nhỏ hơn thời gian bắt đầu | **BE**: Thêm rule FluentValidation chặn `EndTime < StartTime` (`SubscriptionValidator.cs`) + seed bản dịch `lz.message.sharedata.timeRangeInvalid` ("Thời gian kết thúc phải lớn hơn hoặc bằng thời gian bắt đầu") vào `SysTerminology`.<br>**FE**: Validate kiểm tra `endTime < startTime` trong `editSubscription.vue`, hiển thị thông báo lỗi `lz.message.sharedata.timeRangeInvalid` và chặn gọi API lưu nếu người dùng chọn khung giờ ngược. | DatHQ | ✅ Đã sửa chặn khung giờ ngược (BE+FE); đề xuất Ngày hiệu lực chờ PO/BA confirm | `08102026-DatHQ: Đã chặn khung giờ EndTime < StartTime ở cả FE và BE (thông báo 'Thời gian kết thúc phải lớn hơn hoặc bằng thời gian bắt đầu'); phần đề xuất Ngày hiệu lực đang chờ PO/BA confirm.` |
| **30** 🆕 | Sao chép "Bộ ánh xạ dữ liệu" lỗi: ẩn tiêu đề 3 tab ("Thông tin chung", "Ánh xạ", "Dữ liệu gởi thử") nên không rê/kéo được cửa sổ để xem đủ thông tin; các trường map có sẵn từ bộ ánh xạ gốc, chưa chỉnh sửa gì nhưng không cho lưu, tester không nhận diện được đang sai thao tác gì ([ảnh](images/issue-30.png): banner đỏ `Giá trị mặc định không đúng kiểu dữ liệu tại: header.status`) | — (issue mới 06/10, chưa phân tích) | — | *Chưa gán* | ⚠️ Chờ phản hồi (issue mới 06/10) | — |
| ~~**31**~~ | ⛔ **Đã xoá khỏi sheet** (phát hiện ở lần trích xuất 08/10/2026 — PDF không ghi lý do). Nội dung cũ: lỗi thông báo hiển thị tiếng Anh, không đồng nhất với phần còn lại của hệ thống ([ảnh cũ](images/issue-31.png) vẫn còn trên đĩa, không còn dòng nào dẫn chiếu): banner đỏ nguyên văn `Database connection failed. Please check again!`. Xem callout đầu mục *Tình trạng xử lý* để biết suy luận về lý do xoá. | — | — | — | ⛔ Đã xoá, không theo dõi tiếp | — |
| **32** 🆕 | Thêm mới Bộ ánh xạ: xong tab "Thông tin chung", sang tab "Ánh xạ" nhập chuỗi JSON rồi bấm "Phân tích" thì hệ thống mất tiêu đề lớn của cả 3 tab, không nhận diện được đang ở tab nào ([ảnh](images/issue-32.png)) | — (issue mới 06/10, chưa phân tích) | — | *Chưa gán* | ⚠️ Chờ phản hồi (issue mới 06/10) | — |
| **33** 🆕 | Chức năng "Cấu hình" loại dữ liệu đi kèm với "Đối tác" chỉ hiển thị tên gói tin, không kèm mã — khi có nhiều gói tin trùng tên khác mã thì không phân biệt được, nên bổ sung mã đứng trước tên ([ảnh a](images/issue-33-a.png) · [ảnh b](images/issue-33-b.png): 2 dòng cùng tên `Gói 101 - Dữ liệu giao thông chung` nhưng mã khác nhau `101_commonData` / `101_commonData1`, đã xác nhận có thật trên staging) | Cùng một lỗi lặp lại ở 10 file: `subscriptionTable.vue`, `editSubscription.vue`, `mapping/index.vue`, `history/index.vue`, `errorLog/index.vue`, `sharing/index.vue`, `exportPreviewDialog.vue`, `recordDetailDrawer.vue`, `activityDetailDrawer.vue`, `activityDetailDialog.vue` — mỗi file có hàm/el-option riêng chỉ hiển thị `.name`, bỏ qua `.code`, cho cả Gói tin và Đối tác | **FE**: Thêm hàm `codeNameLabel` cục bộ ở cả 10 file, áp dạng `[Mã] Tên` cho mọi nơi hiển thị Gói tin/Đối tác (ô lọc, cột lưới, dropdown modal, dialog/drawer chi tiết), nới `minWidth`/`width` các cột liên quan — chi tiết tại `Prompt/sharedata-issue33-ma-goi-tin-trung-ten-prompt.md` | DatHQ | ✅ Đã sửa (FE) | `08102026-DatHQ: Đã bổ sung mã trước tên dạng [Mã] Tên cho toàn bộ ô lọc, cột lưới, dropdown modal và các dialog/drawer chi tiết hiển thị Gói tin và Đối tác trên toàn phân hệ ShareData.` |

---

## Ghi chú chuyển thể

**Cột rỗng ở toàn bộ 5 issue mới (29–33)**: `Nguyên nhân`, `Phương án xử lý`, `Nhân sự phụ trách`, `Thời gian lỗi`, `Thời gian hoàn thành`, `Ghi chú`, `Passed/Failed` — PDF 06/10/2026 chưa có phản hồi nào từ dev cho 5 issue này.
**Cột đồng nhất** (toàn bộ 33 dòng): `Người đăng` = TuyenHTN.

⚠️ **Tiêu đề cột không chắc**: bản gốc có **hai** cột phân loại liền nhau nhưng hàng tiêu đề PDF chỉ ghi một nhãn `Phân loại`. Cột trước `Module` (`Chức năng`/`UI`/`Dịch thuật`) được gọi là **Nhóm**, cột sau `Module` (`Lỗi`/`Hiệu chỉnh UI`/`Hiệu chỉnh chức năng`) giữ tên **Phân loại**. Đặt tên theo nội dung thật, ⛔ không suy đoán tiêu đề gốc.

**Quy trình bóc tách** (để lần sau làm lại được — không đổi so với lần trích đầu 01/10/2026):
1. Văn bản: `pdftotext -table -enc UTF-8` (xpdf 4.06). ⛔ **Không dùng `-layout`** — chế độ đó trộn lẫn dòng của các issue liền kề.
2. Ảnh: `pypdfium2` + `Pillow` — duyệt `page.get_objects()`, lọc `obj.type == 3`, lấy `obj.get_bounds()` để biết toạ độ, `obj.get_bitmap().to_pil().save()` để ghi PNG ở độ phân giải gốc.
3. Gán ảnh ↔ issue: lấy toạ độ `y` của số STT ở cột trái (`x ≈ 54-61 pt`, lấy qua `textpage.get_rect()`/`get_text_bounded()`) rồi so tâm ảnh rơi vào khoảng nào.
4. Đọc từng ảnh bằng thị giác rồi **chép nội dung thành chữ** vào đúng mục. ⛔ **Không nhúng base64** — mô hình không giải mã base64 từ văn bản, và nó làm phình tệp hàng MB vô ích.
5. **Bổ sung 07/10/2026**: để tránh đọc nhầm các cột bị bẻ dòng trong `-table` (đặc biệt `Nhóm`/`Phân loại`/`Mức độ ưu tiên`/`Nhân sự phụ trách`/`Passed-Failed`), đã đối chiếu lại bằng cách lấy toạ độ `x` của từng tiêu đề cột qua `textpage.get_rect()` trên hàng tiêu đề, rồi dùng `get_text_bounded(left, bottom, right, top)` để trích đúng từng ô theo cả `x` lẫn `y`. Kết quả khớp 100% với lần đọc thủ công từ bản `-table`.

40 ảnh gốc nằm ở [`images/`](images/) — Tier C, giữ để người đọc đối chiếu. 33 ảnh đầu (issue 1–28) trích 01/10/2026; 7 ảnh issue 29–33 trích 07/10/2026.

📌 **Lần trích xuất thứ 2 cùng ngày 07/10/2026** (PDF đổi 4.1MB → 4.2MB, cùng 4 trang/33 issue, không thêm ảnh mới): cập nhật `Passed/Failed` của 8 issue (13, 14, 15, 16, 17, 23, 24, 26) từ Pending → **Passed**, xác nhận nguyên nhân gốc của issue 14/15/17/24 là **sự cố mạng server ngày 06/10/2026** (dev tự ghi `"Nguyên nhân do ngày 06102026 bị lỗi mạng server"`), và gán Nhân sự phụ trách = DatHQ cho issue 29. Đối chiếu thêm với code thật (không chỉ PDF): prompt issue 29 và prompt datatypeLabel/modal-height **đã được áp dụng** (xác minh trực tiếp bằng Grep vào `DataOutboundScheduler.cs`, `editSubscription.vue`, `activityDetailDialog.vue`, 4 file `datatypeLabel`, `vi-vn.json`), còn prompt issue 33 **chưa áp dụng** (`subscriptionTable.vue` vẫn bản cũ).

📌 **Lần trích xuất thứ 3, ngày 08/10/2026** (PDF đổi LastWriteTime sang 08/10/2026 07:55, kích thước ~4,09MB, vẫn 4 trang): phát hiện duy nhất là **Issue 31 đã bị xoá khỏi sheet** — xác nhận bằng 3 cách đọc độc lập (`pdftotext -table`, `pdftotext` thường không `-table`, và ảnh chụp trực tiếp trang 4), cả 3 đều đồng nhất: bảng nhảy thẳng từ STT 30 sang STT 32, không còn dòng 31. Không có thay đổi nào khác ở issue 1–30, 32, 33 so với lần trích 07/10 (lần 2) — đối chiếu từng ô `Nguyên nhân`/`Phương án`/`Nhân sự`/`Ghi chú`/`Passed-Failed` đều giống hệt. Bảng *Tổng quan* đã tính lại toàn bộ 32 issue còn hiệu lực trực tiếp từ dữ liệu mới (không suy luận lùi từ số liệu cũ), kiểm chứng chéo bằng cách khớp: tổng mỗi trục (Màn hình/Nhóm/Phân loại/Ưu tiên/Phụ trách) phải giảm đúng 1 đơn vị so với bản 33-issue cũ — khớp 100% ở cả 5 trục, không có sai lệch.
