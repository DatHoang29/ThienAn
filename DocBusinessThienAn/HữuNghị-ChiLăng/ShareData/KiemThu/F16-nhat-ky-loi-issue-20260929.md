---
tier: A
read: full
source: F16.TAC-CN01-HNCL_ITS-KICH BAN KIEM THU - Issue - ShareData.pdf
source_pages: 1-4
extracted: 2026-10-01
images: images/issue-*.png (33 ảnh, đã chép nội dung thành chữ ở từng mục)
---

# F16 — Nhật ký lỗi kiểm thử ShareData (26/09 – 29/09/2026)

> 📌 Chuyển thể từ biểu mẫu `F16.TAC-CN01` — 4 trang, **28 issue**, người đăng **TuyenHTN**, toàn bộ đang `Chờ phản hồi`.
> 🔴 **Đây là tài liệu kiểm thử, ⛔ không phải sổ theo dõi task.** Trạng thái thi công tra ở `../Plan/Sharedata_MasterPlan.md`.

## Tình trạng xử lý — cập nhật 02/10/2026

🔴 **⛔ Không tra trạng thái task ở đây.** Sổ theo dõi duy nhất là
[`../Plan/Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) (rule 19.24).
Bảng này chỉ để người đọc tài liệu kiểm thử biết mục nào đã được đụng tới.

| Tình trạng | Issue |
|---|---|
| ✅ DatHQ đã xử lý (FE & BE, danh mục dữ liệu) | **1, 2, 4, 6, 7, 11, 15, 18, 25, 27, 28** |
| ✅ HieuNV đã xử lý (Dịch thuật CSDL `SysTerminology`) | **3, 5, 8, 10, 12, 13, 19, 21, 22** |
| ⚠️ HieuNV phụ trách chức năng / Chờ phản hồi | **9, 14, 16, 17, 20, 23, 24, 26** (Đã revert code FE về nguyên mẫu; HieuNV tiếp nhận xử lý) |

📌 Issue **14 và 16** (Sao chép ánh xạ: mã không cho sửa + trùng mã gây lỗi): Đã revert toàn bộ code FE về nguyên mẫu ban đầu, chuyển giao cho HieuNV xử lý đồng bộ theo phân công.
📌 Issue **17, 23, 24**: Đã revert toàn bộ code FE về nguyên mẫu ban đầu, chuyển giao cho HieuNV tiếp nhận xử lý theo đúng phân công Sheet Bug.
📌 Issue **7 và 25** là **thiếu dữ liệu danh mục** trong CSDL, ⛔ không phải lỗi mã nguồn. Đã chạy script SQL bổ sung đủ 9 kiểu ngày 02/10/2026.
📌 Các issue dịch thuật (**3, 5, 8, 10, 12, 13, 19, 21, 22** do HieuNV phụ trách): Bản dịch được nạp trên CSDL `SysTerminology`. Khi kiểm thử lại trên môi trường Web, **bắt buộc bấm nút icon Làm mới 🔄 cạnh menu Ngôn ngữ ở TopBar** (hoặc gọi API `GET /api/system/sysconfig/loadserverterm?language=vi-VN`) để hệ thống đồng bộ dữ liệu từ CSDL vào Backend & Frontend. Nếu chưa làm mới, hệ thống vẫn giữ cache cũ và sẽ tiếp tục hiện mã lỗi thô `lz.*`.

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

## Tổng quan 28 issue

| Trục | Phân bố |
|---|---|
| Màn hình | Ánh xạ dữ liệu **11** · Cấu hình gói tin **6** · Cấu hình (Đối tác/Đăng ký) **5** · Bộ mã chuẩn hóa **4** · Lịch sử chia sẻ **2** |
| Nhóm | Chức năng **18** · UI **6** · Dịch thuật **4** |
| Phân loại | Lỗi **13** · Hiệu chỉnh UI **8** · Hiệu chỉnh chức năng **7** |
| Ưu tiên | 🔴 Cao **3** (16, 17, 24) · Trung bình **14** · Thấp **11** |
| Phụ trách | HieuNV **9** · DatHQ **10** · *chưa gán* **9** |

📌 **Phân định độc lập giữa Issue 13, 14 và 16**: Issue 13 giải quyết câu thông báo lỗi chưa dịch; Issue 14 giải quyết việc khóa ô Mã và hiển thị tooltip; Issue 16 giải quyết logic nghiệp vụ khi sao chép (tự động tắt `isActive`, xóa ID và sinh mã mới tránh xung đột).

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

🔴 **Issue 22 là bằng chứng trực quan của lỗi 2 toast**: ảnh bắt được **đồng thời** `lz.exception.sharedata.packetFieldInUse` (của backend) và `Thực hiện thất bại` (toast generic của frontend) chồng lên nhau.

🔴 **Issue 28 là bằng chứng bộ lọc thời gian bị vô hiệu**: tester nhập `Thời gian bắt đầu = 2030-01-01`, `Thời gian kết thúc = 2028-09-30` — **bắt đầu sau kết thúc, và cả hai đều ở tương lai** — hệ thống vẫn trả về **đủ 178 bản ghi**. Ở tab Nhật ký truyền nhận, lọc `29/09 → 30/09` nhưng lưới trả về các dòng ngày **22/09**.

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
| **1** | Sửa gói tin gửi đi: nên đặt trần cho chu kỳ để người dùng không set quá lớn ([ảnh](images/issue-01.png): ô chu kỳ bị nhập dãy 60 chữ số 9) | Ô nhập `el-input-number` chưa cấu hình `:max="86400"`, cho phép người dùng nhập số tùy ý | **FE**: thêm `:max="86400"` vào `el-input-number` (`editSubscription.vue`), chặn giá trị vượt quá 86400s (24h) | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: FE đã giới hạn chu kỳ tối đa 86400s (24h).` |
| **2** | Nhập chu kỳ quá lớn báo lỗi .NET JsonException và hiện 2 popup lỗi ([ảnh](images/issue-02.png): toast exception .NET hiện đè 2 lần) | Số nhập vượt quá `int.MaxValue` làm văng lỗi deserialization JSON của ASP.NET Core; FE gọi thêm `ElMessage.error` trùng lặp trong `catch` | **BE**: Giữ kiểu `int?` chuẩn cho DTO/Entity, bổ sung rule FluentValidation `InclusiveBetween(5, 86400)` (`SubscriptionValidator.cs`).<br>**FE**: thêm `:max="86400"` trên `el-input-number` và bỏ `ElMessage.error` trong `catch` (`editSubscription.vue`) | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: BE validate dải 5-86400s; FE giới hạn max 86400s và loại bỏ toast lỗi kép.` |
| **3** | Sao chép "Đối tác" báo lỗi khi trùng mã, tên đối tác đã có trong hệ thống ([ảnh](images/issue-03.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **4** | Sửa đối tác báo lỗi khi trùng mã; ô Mã đối tác không bị khóa khi Chỉnh sửa ([ảnh](images/issue-04.png)) | Ô nhập Mã đối tác ở chế độ Chỉnh sửa vẫn cho phép nhập (`enable`), dẫn đến sửa trùng mã định danh | **FE**: Khóa ô Mã đối tác (`:disabled="operateType === 'edit'"`), bọc `el-tooltip` giải thích mã định danh không thể thay đổi (`editPartner.vue`) | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: Đã khóa ô Mã đối tác khi chỉnh sửa kèm tooltip giải thích mã định danh cố định không thể đổi.` |
| **5** | Thêm mới gói tin mới - trùng mã gói tin đã có báo lỗi gây khó hiểu cho người dùng (nửa code nửa thông báo) ([ảnh](images/issue-05.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **6** | Đổi tên label cần tìm cho giống tên cột trên lưới đang có, gợi ý cho người dùng biết đang tìm theo thuộc tính nào ([ảnh](images/issue-06.png)) | Nhãn label và placeholder ở form tìm kiếm chưa đồng bộ với tiêu đề cột của bảng dữ liệu | **FE**: Đổi `:label` và `:placeholder` sang "Khóa field" (`aliasFieldKey`) và "Kiểu" (`fieldType`) (`dataSource/index.vue`) | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: Đã đổi nhãn ô tìm kiếm thành 'Khóa field' và 'Kiểu' đồng bộ chuẩn 100% với các cột trên lưới.` |
| **7** | Cần bổ sung thêm kiểu dữ liệu dạng double hoặc decimal; thống nhất nhãn "Kiểu" thay vì "Loại" ([ảnh](images/issue-07.png)) | CSDL danh mục `sharedata_value_type` chưa seed kiểu số thực; modal `editPacketField.vue` dùng nhãn `lz.entity.base.type` ("Loại") | **FE**: đổi `:label` sang `fieldType` ("Kiểu") (`editPacketField.vue`).<br>**DB**: script SQL `sql/20261001-bo-sung-sharedata-value-type.sql` bổ sung 5 kiểu dữ liệu vào `SysConfigData` (dùng chung cho cả Issue 7 và 25, đã chạy DB) | DatHQ | ✅ Đã fix (02/10) | `02102026-DatHQ: FE đã đổi nhãn modal thành 'Kiểu' đồng bộ với lưới; CSDL đã chạy script bổ sung đủ 5 kiểu dữ liệu (long, float, double, decimal, guid).` |
| **8** | Khi thêm mới trường gói tin bị trùng mã, thông báo lỗi gây khó hiểu và không nhận diện được đang sai thông tin nào ([ảnh](images/issue-08.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **9** | Cần đổi hoặc ẩn thông tin "Quản lý dự án TCP - V2.0" trên giao diện của ITS ([ảnh](images/issue-09.png)) | Cần xác định chính xác nguồn chuỗi text qua F12 Console trên đúng môi trường tester | Chờ PO/BA quyết định phương án đổi tên hay ẩn bỏ | HieuNV | ⚠️ Chờ phản hồi | `01102026-HieuNV: Đang rà soát nguồn text qua F12 trên môi trường tester, chờ PO/BA xác nhận phương án.` |
| **10** | Xuất hiện thông báo lỗi gây khó hiểu khi dùng chức năng "Xóa" các bản ghi đang có trên các bộ mã chuẩn hóa ([ảnh](images/issue-10.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **11** | Kiểm tra chức năng của "Làm mới" là tìm kiếm lại dữ liệu đã nhập hay refresh và hiển thị lại toàn bộ dữ liệu đang có trong hệ thống? ([ảnh](images/issue-11.png)) | Nút làm mới trên thanh công cụ bảng chưa có tooltip giải thích hành vi tải dữ liệu | **FE**: Bổ sung `el-tooltip` cho nút Làm mới: *"Làm mới danh sách bảng, giữ nguyên điều kiện lọc hiện tại"* (`table-header-operation.vue`) | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: Đã bổ sung tooltip giải thích rõ hành vi nút Làm mới: Làm mới danh sách bảng và giữ nguyên điều kiện lọc.` |
| **12** | Sao chép Bộ mã chuẩn hóa báo lỗi khi trùng mã đã có trong hệ thống. Thông tin lỗi chưa được dịch thuật ([ảnh](images/issue-12.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **13** | Sao chép Ánh xạ dữ liệu thông báo lỗi gây khó hiểu cho người dùng; không nhận diện được thao tác sai là gì ([ảnh](images/issue-13.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **14** | Sao chép Ánh xạ dữ liệu không cho người dùng nhập lại "Mã" nhưng lại để trạng thái enable; giữ nguyên thì thông báo lỗi trùng mã ([ảnh](images/issue-14.png)) | Bản gốc không cho sửa ô Mã nhưng khi sao chép giữ nguyên thông tin thì trùng mã định danh | Đã revert code FE về nguyên mẫu; chuyển giao HieuNV xử lý đồng bộ cùng Issue 16 | HieuNV | ⚠️ Chờ phản hồi | `Đã revert code FE về nguyên mẫu; chuyển giao HieuNV tiếp nhận xử lý.` |
| **15** | Điều chỉnh label "mã gói tin" nếu nó là mã định danh của gói tin đang chọn. Làm mờ nếu không cho chỉnh/nhập "mã" ([ảnh](images/issue-15.png)) | Nhãn dùng key chung `lz.entity.base.code` ("Mã") chưa rõ ràng; thiếu chú thích công thức sinh mã định danh | **FE**: Bổ sung dòng hint `lz.label.sharedataMapping.codeAutoHint` ngay dưới ô Mã giải thích rõ công thức sinh mã định danh (`editMapping.vue`) | DatHQ | ✅ Đã fix (30/09) | `30092026-DatHQ: Đã thêm dòng chú thích dưới ô nhập liệu giải thích rõ công thức sinh mã hồ sơ tự động.` |
| **16** | Chức năng Sao chép Ánh xạ dữ liệu không cho người dùng nhập lại "Mã" nhưng lại để trạng thái enable; giữ nguyên thì thông báo lỗi trùng mã. Hệ thống thông tin lỗi gây khó hiểu cho người dùng ([ảnh a](images/issue-16-a.png) · [ảnh b](images/issue-16-b.png)) | Bản gốc không cho sửa ô Mã nhưng khi sao chép giữ nguyên thông tin thì trùng mã; thông báo lỗi chưa rõ ràng | Đã revert code FE về nguyên mẫu; chuyển giao HieuNV xử lý đồng bộ cùng Issue 14 | HieuNV | ⚠️ Chờ phản hồi | `Đã revert code FE về nguyên mẫu; chuyển giao HieuNV tiếp nhận xử lý.` |
| **17** | Chức năng Chỉnh sửa Ánh xạ dữ liệu, lấy dữ liệu mẫu không thành công, thông báo lỗi gây khó hiểu ([ảnh a](images/issue-17-a.png) · [ảnh b](images/issue-17-b.png)) | Lấy dữ liệu mẫu không thành công khi đang mở chỉnh sửa ánh xạ | Đã revert code FE về nguyên mẫu; chuyển giao HieuNV xử lý | HieuNV | ⚠️ Chờ phản hồi | `Đã revert code FE về nguyên mẫu; chuyển giao HieuNV tiếp nhận xử lý.` |
| **18** | Nếu có hơn 2 đối tác trùng tên nhưng khác mã, thì khi tạo mới Ánh xạ dữ liệu nên load đính kèm [mã đối tác] trước tên đối tác ([ảnh a](images/issue-18-a.png) · [ảnh b](images/issue-18-b.png)) | Dropdown chỉ render thuộc tính tên `item.name` mà không kèm mã `item.code` | **FE**: Cập nhật template dropdown hiển thị định dạng `[Mã] Tên đối tác` (`editMapping.vue`) | DatHQ | ✅ Đã fix (30/09) | `30092026-DatHQ: Đã cập nhật dropdown hiển thị định dạng [Mã] Tên đối tác giúp phân biệt chính xác đối tác trùng tên.` |
| **19** | Xóa hồ sơ ánh xạ trong "Gởi đi" hoặc "Nhận về" của mục Cấu hình thông báo lỗi gây khó hiểu cho người dùng ([ảnh](images/issue-19.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **20** | Mục "Lấy dữ liệu mẫu" không thành công. Thông báo lỗi weatherId và Status đang rỗng mặc dù 2 trường này đã map ([ảnh a](images/issue-20-a.png) · [ảnh b](images/issue-20-b.png)) | Bản ghi dữ liệu nguồn trong CSDL có giá trị NULL ở 2 trường này | Chuyển giao HieuNV tiếp nhận xử lý | HieuNV | ⚠️ Chờ phản hồi | `Chuyển giao HieuNV tiếp nhận xử lý.` |
| **21** | Đang có ánh xạ dữ liệu không cho chỉnh sửa => chỉnh câu thông báo lỗi cho người dùng dễ hiểu ([ảnh](images/issue-21.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **22** | Bộ gói tin đã cấu hình và có hồ sơ ánh xạ dữ liệu thì không thể sửa, xóa => Nên ẩn/mờ 2 chức năng này trên nhóm "Hành động" hoặc hiển thị thông báo lỗi cho người dùng dễ hiểu ([ảnh](images/issue-22.png)) | Trước đó chưa thêm dịch ngôn ngữ lên Database | **DB**: Thêm bản dịch ngôn ngữ lên database | HieuNV | ✅ Đã fix (01/10) | `01102026-HieuNV: Bổ sung dịch ngôn ngữ.` |
| **23** | Tab "Ánh xạ" dữ liệu thuộc nhóm header nếu chỉ được chọn các thuộc tính cố định của hệ thống thì Nên ẩn/dấu các thuộc tính khác không cho người dùng thấy và chọn ([ảnh](images/issue-23.png)) | Cần quy định bộ lọc thuộc tính theo ngữ cảnh header | Đã revert code FE về nguyên mẫu; chuyển giao HieuNV xử lý | HieuNV | ⚠️ Chờ phản hồi | `Đã revert code FE về nguyên mẫu; chuyển giao HieuNV tiếp nhận xử lý.` |
| **24** | Tab "Ánh xạ", cấu hình giá trị mặc định, kiểu dữ liệu không vô hiệu khi thử chức năng "Lấy dữ liệu mẫu" ([ảnh](images/issue-24.png)) | Cần xác nhận hành vi ép kiểu và giá trị mặc định khi lấy dữ liệu mẫu | Đã revert code FE về nguyên mẫu; chuyển giao HieuNV xử lý | HieuNV | ⚠️ Chờ phản hồi | `Đã revert code FE về nguyên mẫu; chuyển giao HieuNV tiếp nhận xử lý.` |
| **25** | Bổ sung thêm các kiểu dữ liệu cơ bản khác như GUID, float, double, decimal ([ảnh](images/issue-25.png)) | Cùng nguyên nhân với Issue 7 — danh mục `sharedata_value_type` trong CSDL chỉ có 4 dòng | Dùng chung danh mục và script SQL với Issue 7 (`sql/20261001-bo-sung-sharedata-value-type.sql`) | DatHQ | ✅ Đã fix (02/10) | `02102026-DatHQ: Đã chạy script CSDL bổ sung đủ 5 kiểu (long, float, double, decimal, guid) vào danh mục sharedata_value_type; dropdown hiển thị đầy đủ 9 kiểu.` |
| **26** | Cấu hình gói tin "Bắt buộc" gặp lỗi khi "Lấy dữ liệu mẫu" trong Ánh xạ dữ liệu => Gói tin bắt buộc thì khi tạo dữ liệu mẫu, nên tự sinh giá trị kiểu GUID ([ảnh](images/issue-26.png)) | Cần quy định cơ chế tự sinh GUID cho gói tin bắt buộc khi dựng dữ liệu mẫu | Chờ PO/BA chốt phương án và chuyển giao HieuNV xử lý | HieuNV | ⚠️ Chờ phản hồi | `Chuyển giao HieuNV tiếp nhận xử lý theo quyết định PO/BA.` |
| **27** | Hệ thống chưa reset thời gian bắt đầu, kết thúc về mặc định khi nhấn nút "Đặt lại" trong Giao diện Lịch sử chia sẻ ([ảnh](images/issue-27.png)) | Hàm reset bộ lọc bỏ quên việc gán lại giá trị mặc định cho 2 trường thời gian | **FE**: `history/index.vue` cập nhật hàm reset bộ lọc để xóa và đưa 2 trường thời gian về khoảng mặc định của ngày hôm nay | DatHQ | ✅ Đã fix (30/09) | `30092026-DatHQ: Đã sửa nút Đặt lại: xóa sạch điều kiện và đưa khoảng thời gian về mặc định chính xác.` |
| **28** | Tìm kiếm/Đặt lại/Làm mới: hệ thống tìm dữ liệu kết quả không thuộc phạm vi cần tìm; không thông báo lỗi khi thời gian bắt đầu > kết thúc; chưa rào hạn chế nhập thời gian tương lai ([ảnh a](images/issue-28-a.png) · [ảnh b](images/issue-28-b.png)) | Component date picker chưa cấu hình `:disabled-date` và form chưa validate mối quan hệ thời gian | **FE**: `history/index.vue` thêm `:disabled-date="disableFutureDate"` chặn ngày tương lai, validate kiểm tra bắt đầu <= kết thúc | DatHQ | ✅ Đã fix (01/10) | `01102026-DatHQ: Đã chặn chọn ngày tương lai trên lịch và validate ràng buộc thời gian bắt đầu phải trước thời gian kết thúc.` |

---

## Ghi chú chuyển thể

**Cột rỗng ở cả 28 dòng** trong bản gốc (đã lược): `Nguyên nhân`, `Phương án xử lý`, `Thời gian lỗi`, `Thời gian hoàn thành`, `Hình ảnh`, `Ghi chú`, `Passed / Failed`.
**Cột đồng nhất** (đã nêu ở đầu tài liệu): `Người đăng` = TuyenHTN · `Tình trạng` = Chờ phản hồi.

⚠️ **Tiêu đề cột không chắc**: bản gốc có **hai** cột phân loại liền nhau nhưng hàng tiêu đề PDF chỉ ghi một nhãn `Phân loại`. Cột trước `Module` (`Chức năng`/`UI`/`Dịch thuật`) được gọi là **Nhóm**, cột sau `Module` (`Lỗi`/`Hiệu chỉnh UI`/`Hiệu chỉnh chức năng`) giữ tên **Phân loại**. Đặt tên theo nội dung thật, ⛔ không suy đoán tiêu đề gốc.

**Quy trình bóc tách** (để lần sau làm lại được):
1. Văn bản: `pdftotext -table -enc UTF-8` (xpdf 4.06). ⛔ **Không dùng `-layout`** — chế độ đó trộn lẫn dòng của các issue liền kề.
2. Ảnh: `pypdfium2` + `Pillow` — duyệt `page.get_objects()`, lọc `obj.type == 3`, lấy `obj.get_bounds()` để biết toạ độ, `obj.get_bitmap().to_pil().save()` để ghi PNG ở độ phân giải gốc.
3. Gán ảnh ↔ issue: lấy toạ độ `y` của số STT ở cột trái (`x ≈ 57 pt`) rồi so tâm ảnh rơi vào khoảng nào.
4. Đọc từng ảnh bằng thị giác rồi **chép nội dung thành chữ** vào đúng mục. ⛔ **Không nhúng base64** — mô hình không giải mã base64 từ văn bản, và nó làm phình tệp hàng MB vô ích.

33 ảnh gốc nằm ở [`images/`](images/) (2.5 MB) — Tier C, giữ để người đọc đối chiếu.
