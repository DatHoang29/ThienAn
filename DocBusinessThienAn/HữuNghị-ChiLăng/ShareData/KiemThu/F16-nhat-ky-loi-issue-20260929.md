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
| ✅ Đã xử đợt 30/09/2026 | 5, 8, 10, 13, 15, 18, 19, 21, 22, 27 |
| ✅ Đã xử đợt 01/10/2026 | 1, 2, 3, 4, 6, 11, 12, 14, 16, 20, 28 |
| ⚠️ Code xong, **chờ chạy script trên CSDL** | 7, 25 — dropdown vẫn **4 lựa chọn** cho tới khi chạy `sql/20261001-bo-sung-sharedata-value-type.sql`. Đo trên **staging `10.10.8.30`** ngày 02/10: danh mục `sharedata_value_type` vẫn đúng 4 dòng. ⚠️ Môi trường khác chưa kiểm được. 📌 Tự kiểm: *Cấu hình gói tin → Thêm Trường gói tin* → dropdown **Loại** ra **4** = chưa chạy, **9** = đã chạy |
| ⚠️ Chờ quyết định nghiệp vụ của TuyenHTN | 9, 17, 23, 24, 26 — xem mục **F** của MasterPlan |

📌 Issue **14 và 16 là cùng một lỗi**, xử một lần.
📌 Issue **7 và 25** là **thiếu dữ liệu danh mục** trong CSDL, ⛔ không phải lỗi mã nguồn.
📌 Issue **9** — chẩn đoán cũ "giá trị `SysConfig`" đã bị **bác bỏ** (02/10/2026). `globalTitle` là dòng chữ to đang đúng; ô có viền bên trái là thành phần khác. Chưa xác định nguồn — cần chạy F12 Console + 4 câu truy vấn ở mục **3.1** của `Prompt/sharedata-chot-so-phien-ra-soat-0210-prompt.md`, trên **đúng môi trường tester**.

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

## Tổng quan 28 issue

| Trục | Phân bố |
|---|---|
| Màn hình | Ánh xạ dữ liệu **11** · Cấu hình gói tin **6** · Cấu hình (Đối tác/Đăng ký) **5** · Bộ mã chuẩn hóa **4** · Lịch sử chia sẻ **2** |
| Nhóm | Chức năng **18** · UI **6** · Dịch thuật **4** |
| Phân loại | Lỗi **13** · Hiệu chỉnh UI **8** · Hiệu chỉnh chức năng **7** |
| Ưu tiên | 🔴 Cao **3** (16, 17, 24) · Trung bình **14** · Thấp **11** |
| Phụ trách | HieuNV **9** · DatHQ **10** · *chưa gán* **9** |

⚠️ **Issue 14 và 16 là cùng một lỗi** (sao chép hồ sơ ánh xạ — ô Mã để enable rồi báo trùng) ghi thành hai dòng, khác mức ưu tiên (Trung bình vs Cao). Nên gộp.

---

## Chi tiết từng issue

📌 Mỗi mục gồm: **nội dung bản gốc** · **những gì ảnh cho thấy** (chép từ ảnh) · liên kết ảnh.

### Cấu hình — Đối tác & Đăng ký

**1** ✅ Đã fix 01/10 · Chức năng · Hiệu chỉnh chức năng · 26/09 · Thấp · HieuNV · [ảnh](images/issue-01.png)
Sửa gói tin gởi đi: nên đặt trần cho chu kỳ để người dùng không set quá lớn.
> **Ảnh**: modal *Sửa đăng ký chia sẻ dữ liệu*, đối tác `DDTL - Đồng Đăng - Trà Lĩnh`, gói `103 - Dữ liệu thiết bị dò xe (VDS)`, kiểu lịch `Liên tục`, ô **Chu kỳ (giây)** bị nhập một dãy dài toàn chữ số 9 (≈60 ký tự). Khung giờ 06:00 → 06:10.
> 🔧 **Fix**: thêm `:max="86400"` vào `el-input-number` (`editSubscription.vue:117`).

**2** ✅ Đã fix 01/10 · Chức năng · Lỗi · 26/09 · Trung bình · HieuNV · [ảnh](images/issue-02.png)
Thông báo lỗi khó hiểu; nhập số quá lớn thì hệ thống quay về một con số khác.
> **Ảnh**: toast lỗi hiện **nguyên văn exception .NET**:
> `{\"$.intervalSeconds\":[\"The JSON value could not be converted to System.Nullable`1[System.Int32]. Path: $.intervalSeconds | LineNumber: 0 | BytePositionInLine: 303.\"]}`
> Toast này hiện **2 lần chồng nhau**. Ô Chu kỳ sau đó tự nhảy về `86400` (= 24h × 3600 giây — trần đã áp `:max="86400"`); **frontend làm tròn trước khi gửi**, không phải backend.
> 🔧 **Fix**: cùng `:max="86400"` (issue 1) + bỏ `ElMessage.error` cục bộ trong `catch`.

**3** ✅ Đã fix 01/10 · UI · Lỗi · 27/09 · Trung bình · DatHQ · [ảnh](images/issue-03.png)
Sao chép "Đối tác" báo lỗi khi trùng mã/tên đã có.
> **Ảnh**: toast `lz.entity.base.code đã tồn tại trong hệ thống!` (hiện 2 lần). Khung template `{0} đã tồn tại trong hệ thống!` **đã dịch**, nhưng tham số `{0}` trả về nguyên key.
> 🔧 **Fix**: `openDialog` xóa `code` khi `copy`/`add`; bỏ `catch ElMessage.error`; chuyển `closeDialog()` vào nhánh thành công (`editPartner.vue`).

**4** ✅ Đã fix 01/10 · Chức năng · Lỗi · 27/09 · Trung bình · HieuNV · [ảnh](images/issue-04.png)
Sửa đối tác báo lỗi khi trùng mã; **ô Mã đối tác không bị khóa** khi Chỉnh sửa.
> **Ảnh**: cùng toast `lz.entity.base.code đã tồn tại trong hệ thống!`.
> 🔧 **Fix**: bọc `el-tooltip` + `:disabled="operateType === 'edit'"` + key `lz.tooltip.sharedataPartner.codeLocked` (`editPartner.vue`).

**19** ✅ Đã fix 30/09 · Dịch thuật · Hiệu chỉnh UI · 29/09 · Thấp · DatHQ · [ảnh](images/issue-19.png)
Xóa hồ sơ ánh xạ trong "Gởi đi"/"Nhận về" của mục Cấu hình báo lỗi khó hiểu.
> **Ảnh**: toast `lz.exception.sharedata.subscriptionMustPauseBeforeDelete`. Màn *Cấu hình* → đối tác `Partner Name` → tab **Gửi đi**, lưới đăng ký có 2 dòng (gói 102 *Chưa có hồ sơ*, gói 101 *Đang áp dụng* `PARTNER_101COMMONDATA_BOTH`), cả hai công tắc **Bật**.
> ⚠️ **Thao tác thật là xóa ĐĂNG KÝ, không phải xóa hồ sơ ánh xạ** — tiêu đề issue ghi chưa chính xác.
> 🔧 **Fix**: dịch key → hiện câu thân thiện.

### Cấu hình gói tin

**5** ✅ Đã fix 30/09 · UI · Lỗi · 27/09 · Trung bình · DatHQ · [ảnh](images/issue-05.png)
Thêm gói tin trùng mã báo lỗi nửa code nửa thông báo.
> **Ảnh**: toast `lz.entity.sharedata.packetCode đã tồn tại trong hệ thống!`. Modal *Tạo mới Gói tin*: Mã `109_etcData`, Tên `Gói tin mới 109`, Thứ tự 100.
> 🔧 **Fix**: dịch key → hiện câu thân thiện.

**6** ✅ Đã fix 01/10 · UI · Hiệu chỉnh UI · 27/09 · Thấp · HieuNV · [ảnh](images/issue-06.png)
Đổi nhãn ô tìm kiếm cho khớp tên cột trên lưới.
> **Ảnh**: thanh tìm kiếm có ô **Mã** và **Loại**; lưới bên dưới lại có cột **Khóa field** và **Kiểu**. Ghi chú của tester: *"Mã = Khóa field. Loại = ? ⇒ không có gợi ý hoặc không thấy field tương ứng, người dùng khó biết cách để nhập tìm"*.
> 🔧 **Fix**: đổi `:label` + `:placeholder` sang `lz.entity.sharedataDataSource.aliasFieldKey` / `fieldType` (`dataSource/index.vue`).

**7** ✅ FE fix 01/10 · ⚠️ Chờ chạy SQL · Chức năng · Hiệu chỉnh chức năng · 27/09 · Thấp · HieuNV · [ảnh](images/issue-07.png)
Bổ sung kiểu `double`/`decimal`; thống nhất nhãn "Loại" / "Kiểu".
> **Ảnh**: modal *Tạo mới Trường gói tin*, dropdown **Loại** chỉ có đúng **4 lựa chọn**: `string`, `int`, `dateTime`, `bool`.
> 🔧 **Fix FE**: thêm nhánh `guid` vào `previewCoerce` (`editMapping.vue`).
> ⚠️ **Chờ chủ dự án chạy** `sql/20261001-bo-sung-sharedata-value-type.sql` để thêm 5 kiểu vào `SysConfigData`.

**8** ✅ Đã fix 30/09 · Chức năng · Lỗi · 27/09 · Trung bình · HieuNV · [ảnh](images/issue-08.png)
Thêm trường gói tin trùng mã, báo lỗi khó hiểu.
> **Ảnh**: toast `lz.entity.sharedata.aliasFieldKey đã tồn tại trong hệ thống!`. Modal *Tạo mới Trường gói tin* cho gói `109_etcData`: Tên `Lưu lượng xe đầu vào`, Khóa field `TransactionID`, Loại `int`, Nhóm trường `Thống kê`.
> 🔧 **Fix**: dịch key → hiện câu thân thiện.

**21** ✅ Đã fix 30/09 · Dịch thuật · Hiệu chỉnh UI · 29/09 · Trung bình · DatHQ · [ảnh](images/issue-21.png)
Đang có ánh xạ dữ liệu thì không cho sửa ⇒ chỉnh câu thông báo.
> **Ảnh**: toast `lz.exception.sharedata.packetFieldLocked`. Modal *Chỉnh sửa Trường gói tin*: Tên `Mã trạng thái`, Khóa field `status`, Loại `int`, Nhóm trường `Header`.
> ⚠️ Màn thật là **Trường gói tin**, không phải hồ sơ ánh xạ.
> 🔧 **Fix**: dịch key + cập nhật thuật ngữ → "hồ sơ ánh xạ".

**22** ✅ Đã fix 30/09 · Dịch thuật · Hiệu chỉnh UI · 29/09 · Trung bình · DatHQ · [ảnh](images/issue-22.png)
Gói tin đã có hồ sơ ánh xạ thì không sửa/xóa được ⇒ nên ẩn/mờ hoặc báo lỗi dễ hiểu.
> **Ảnh**: 🔴 **hai toast cùng lúc** — `lz.exception.sharedata.packetFieldInUse` (backend) và `Thực hiện thất bại` (frontend). Lưới *Trường gói tin* của gói `104_weatherData`, nhóm Header 3 trường, trong đó `status` và `sessionid` gắn nhãn đỏ **Bắt buộc**.
> ⚠️ Thao tác thật là xóa **Trường gói tin**, không phải xóa gói tin.
> 🔧 **Fix**: bỏ `ElMessage.error` cục bộ trong `catch` — interceptor `axios-utils.ts` đã hiện message BE.

### Bộ mã chuẩn hóa

**9** ⚠️ Chờ chẩn đoán môi trường tester · UI · Lỗi · 28/09 · Thấp · HieuNV · [ảnh](images/issue-09.png)
Đổi hoặc ẩn thông tin "Quản lý dự án TCP - V2.0".
> **Ảnh**: nhãn `Quản lý dự án TCP - V2.0` nằm ngay cạnh tiêu đề `HỆ THỐNG GIÁM SÁT GIAO THÔNG` trên thanh đầu trang.
> ⚠️ **Chưa fix** — cần HieuNV/TuyenHTN chạy F12 Console (`document.title` + `window.__env__?.VITE_APP_NAME`) trên **đúng môi trường tester** để chốt nguồn, rồi TuyenHTN quyết đổi hay ẩn. Quy trình đầy đủ + 4 câu truy vấn: mục **3.1** của `Prompt/sharedata-chot-so-phien-ra-soat-0210-prompt.md`.

**10** ✅ Đã fix 30/09 · Chức năng · Lỗi · 28/09 · Trung bình · HieuNV · [ảnh](images/issue-10.png)
Xóa bản ghi bộ mã chuẩn hóa báo lỗi khó hiểu.
> **Ảnh**: toast `lz.exception.sharedata.codeSetInUse`. Lưới Bộ mã Chuẩn hóa, **cả 5 dòng được tick chọn** (`SCS01`, `condition`, `testtest`, `direction_codeSet`, `test`) rồi bấm nút **Xoá** hàng loạt.
> 🔧 **Fix**: dịch key + cập nhật thuật ngữ → "hồ sơ ánh xạ".

**11** ✅ Đã fix 01/10 · Chức năng · Hiệu chỉnh chức năng · 28/09 · Thấp · HieuNV · [ảnh](images/issue-11.png)
Làm rõ nút "Làm mới" là tìm lại theo điều kiện hay nạp lại toàn bộ.
> **Ảnh**: đang lọc Mã = `scs01`, lưới còn 1 dòng `SCS01`; nút **Làm mới** được khoanh đỏ. Câu hỏi: bấm Làm mới thì giữ bộ lọc hay bỏ bộ lọc.
> 🔧 **Fix**: bọc `el-tooltip` cho nút refresh trong `table-header-operation.vue` + key `lz.tooltip.base.refreshKeepFilter` — *"Nạp lại dữ liệu, giữ nguyên điều kiện lọc hiện tại"*.

**12** ✅ Đã fix 01/10 · Dịch thuật · Hiệu chỉnh UI · 28/09 · Thấp · DatHQ · [ảnh](images/issue-12.png)
Sao chép bộ mã báo lỗi trùng mã, chưa dịch.
> **Ảnh**: toast `lz.entity.base.code đã tồn tại trong hệ thống!`. Modal *Sao chép bộ mã* giữ nguyên Mã `SCS01` của bản gốc — **ô Mã vẫn sửa được nhưng không được xoá sẵn**, nên bấm Xác nhận là chắc chắn trùng.
> 🔧 **Fix**: `openDialog` xóa `id` + `code` sau `GetById` khi `copy`/`add` (`editCodeSet.vue`).

### Ánh xạ dữ liệu

**13** ✅ Đã fix 30/09 · Dịch thuật · Hiệu chỉnh UI · 29/09 · Thấp · DatHQ · [ảnh](images/issue-13.png)
Sao chép ánh xạ dữ liệu báo lỗi khó hiểu.
> **Ảnh**: toast `lz.exception.sharedata.mappingConflictInUse`. Modal *Sao chép hồ sơ ánh xạ*: Đối tác `TTCSDL`, Chiều **Hai chiều**, Gói tin `101`, Mã `TTCSDL_101COMMONDATA_BOTH`, Tên `NHU101COMMONDATA_BOTH`, công tắc **Đang dùng = bật**.
> 🔧 **Fix**: dịch key → hiện câu thân thiện.

**14** ✅ Đã fix 01/10 · Chức năng · Lỗi · 29/09 · Trung bình · *chưa gán* · [ảnh](images/issue-14.png)
Sao chép ánh xạ không cho nhập lại "Mã" nhưng để enable; giữ nguyên thì báo trùng.
> **Ảnh**: modal *Sao chép hồ sơ ánh xạ*, ô **Mã** `TTCSDL_101COMMONDATA_BOTH` đang được bôi chọn (enable, sửa được).
> 🔧 **Fix**: thay `code = ''` bằng `syncMappingCode()` để dựng lại Mã theo công thức `{ĐỐI_TÁC}_{GÓI_TIN}_{CHIỀU}` + thêm hint dưới ô Mã khi `copy` (`editMapping.vue`).

**15** ✅ Đã fix 30/09 · UI · Hiệu chỉnh UI · 29/09 · Thấp · DatHQ · [ảnh](images/issue-15.png)
Điều chỉnh nhãn "mã gói tin" nếu nó là mã định danh của gói tin; làm mờ nếu không cho nhập.
> **Ảnh**: 🔴 **điểm mấu chốt** — với đối tác `Partner Name`, ô Mã tự thành `PARTNER_101COMMONDATA_BOTH`. Ghi chú của tester: *"Mã đang ăn theo Gói tin chọn? Vậy đây là mã gói tin, không phải mã hồ sơ?"*
> ⇒ Mã hồ sơ được **sinh tự động theo công thức `{MÃ_ĐỐI_TÁC}_{MÃ_GÓI_TIN}_{CHIỀU}`**.
> 🔧 **Fix**: thêm hint `lz.label.sharedataMapping.codeAutoHint` giải thích công thức sinh mã.

**16** ✅ Đã fix 01/10 (cùng issue 14) · Chức năng · Lỗi · 29/09 · 🔴 **Cao** · *chưa gán* · [ảnh a](images/issue-16-a.png) · [ảnh b](images/issue-16-b.png)
Trùng nội dung issue 14, thêm ý "thông tin lỗi gây khó hiểu".
> **Ảnh a**: modal sao chép, Mã `TTCSDL_101COMMONDATA_BOTH`, Ghi chú `test sao chép`.
> **Ảnh b**: tab **Ánh xạ** — toast `lz.exception.sharedata.mappingConflictInUse`. Cây ánh xạ 6 trường đã khớp 6: `ID→zoneId`, `name→zoneName`, `writedate→Now`, `serial→Serial`, `packagecode→PacketCode`, `partnercode→PartnerCode`.

**17** ✅ Đã fix 01/10 · Chức năng · Lỗi · 29/09 · 🔴 **Cao** · DatHQ · [ảnh a](images/issue-17-a.png) · [ảnh b](images/issue-17-b.png)
Chỉnh sửa ánh xạ, lấy dữ liệu mẫu không thành công, báo lỗi khó hiểu.
> **Ảnh a**: toast `lz.exception.sharedata.mappingInUse` trên tab Ánh xạ, hồ sơ 20 trường đã khớp 19. Ghi chú tester: *"Điều chỉnh xóa bỏ bớt dữ liệu trong body của json Đối tác"*.
> **Ảnh b**: tab **Dữ liệu gửi thử** — ⚠️ **lấy dữ liệu mẫu THÀNH CÔNG** (2 bản ghi thật của `101_commonData`, payload dựng đủ `header` + `data`). Lỗi `mappingInUse` xuất hiện khi **bấm Xác nhận để lưu**, không phải khi lấy dữ liệu mẫu.
> ⇒ Tiêu đề issue mô tả **sai nguyên nhân**.
> 🔧 **Fix**:
> - **FE**: `mapping/index.vue` tận dụng cột "Đang dùng" (`row.isActive`): làm mờ (`disabled`) nút **Sửa** và **Xoá** ngay từ danh sách ngoài khi `row.isActive == true`, bọc trong `el-tooltip` hiển thị *"Hồ sơ đang dùng, vui lòng tắt công tắc trước khi sửa hoặc xóa"*. Bổ sung `checkboxConfig: { checkMethod: ({ row }) => !row.isActive }` ngăn tick chọn dòng đang bật để xóa hàng loạt.
> - Muốn sửa, người dùng chỉ cần tắt công tắc tại chỗ (nếu có Subscription đang chạy ngầm, Backend sẽ chặn ngay tại bước gạt công tắc, bảo vệ an toàn cho worker mà không cần sửa Backend).

**18** ✅ Đã fix 30/09 · UI · Hiệu chỉnh UI · 29/09 · Thấp · DatHQ · [ảnh a](images/issue-18-a.png) · [ảnh b](images/issue-18-b.png)
Hai đối tác trùng tên khác mã ⇒ nên hiện [mã đối tác] kèm tên.
> **Ảnh a**: danh sách đối tác có **hai dòng cùng tên** `Đồng Đăng - Trà Lĩnh`, mã `DDTL` và `DDTL-2` (danh sách này **có** hiện mã dưới tên).
> **Ảnh b**: dropdown *Chọn đối tác* trong modal Tạo mới hồ sơ ánh xạ hiện **hai dòng chữ giống hệt nhau**, ⛔ không có mã ⇒ không phân biệt được.
> 🔧 **Fix**: hiện `[mã] tên` trong dropdown chọn đối tác.

**20** ✅ Đã fix 01/10 · Chức năng · Hiệu chỉnh chức năng · 29/09 · Trung bình · *chưa gán* · [ảnh a](images/issue-20-a.png) · [ảnh b](images/issue-20-b.png)
"Lấy dữ liệu mẫu" báo `weatherId` và `Status` rỗng mặc dù đã map.
> **Ảnh a**: gói `104_weatherData`, lưới bản ghi thật cho thấy `weatherId = NULL`, `status = NULL` (chỉ `weatherDescription = Nắng`, `rainfall = 0`, `temperature = 30` có giá trị). Cảnh báo: *"Trường bắt buộc weatherId đang rỗng — service sẽ chặn bản ghi này"*.
> **Ảnh b**: tab Ánh xạ xác nhận **hai trường ĐÃ được map** (`body.id → weatherId`, `body.status → status`).
> ⇒ 🔴 **Không phải lỗi ánh xạ — dữ liệu nguồn trong CSDL đang NULL.**
> 🔧 **Fix**: cập nhật key `lz.message.sharedataMapping.requiredFieldEmptyWillBlock` — nói rõ *"giá trị nguồn trong CSDL đang rỗng"* thay vì để người dùng tưởng chưa map.

**23** ✅ Đã fix 01/10 · Chức năng · Hiệu chỉnh chức năng · 29/09 · Trung bình · DatHQ · [ảnh](images/issue-23.png)
Nhóm header chỉ nên cho chọn thuộc tính hệ thống cố định, ẩn các thuộc tính khác.
> **Ảnh**: dropdown *Chọn trường gói tin* cho khoá `header.sessionId` liệt kê **cả hai nhóm** — nhóm `Meta` (`Now`, `Serial`, `PacketCode`, `PartnerCode`) **và** nhóm `Trường gói tin` (`message`, `status`...). Tester muốn ẩn nhóm thứ hai khi đang ở header.
> 🔧 **Fix**: `editMapping.vue` bổ sung hàm `isHeaderPath(data.path)` kiểm tra các node thuộc nhánh `header`, tự động ẩn nhóm `Trường gói tin` qua `v-if="!isHeaderPath(data.path)"`, chỉ cho phép chọn nhóm `Meta — giá trị hệ thống`.

**24** ✅ Đã fix 01/10 · Chức năng · Lỗi · 29/09 · 🔴 **Cao** · DatHQ · [ảnh](images/issue-24.png)
Giá trị mặc định và kiểu dữ liệu không có tác dụng khi "Lấy dữ liệu mẫu".
> **Ảnh**: popup *Thiết lập cho khoá header.sessionId* — `Kiểu dữ liệu phía đối tác = string`, `Giá trị nội bộ mặc định = 123456`. Hai thiết lập này không được áp khi dựng payload mẫu.
> 📌 **Bản chất**: Tester cấu hình giá trị mặc định / kiểu dữ liệu cho một node không liên kết với trường dữ liệu nào của gói tin (`fieldKey` rỗng). Ở phiên bản trước, nhánh logic xử lý mẫu bỏ qua các thuộc tính này nếu không có trường liên kết.
> 🔧 **Fix**: `editMapping.vue` cập nhật `buildPreviewNode` để với các node lá chưa map hoặc map tĩnh, nếu có khai `defaultPartnerValue`/`defaultSourceValue` hoặc `targetType` thì vẫn tự động chuyển đổi kiểu dữ liệu và đưa giá trị mặc định vào payload dựng thử.

**25** ✅ FE fix 01/10 · ⚠️ Chờ chạy SQL · Chức năng · Hiệu chỉnh chức năng · 29/09 · Trung bình · *chưa gán* · [ảnh](images/issue-25.png)
Bổ sung GUID, float, double, decimal.
> **Ảnh**: dropdown *Kiểu dữ liệu phía đối tác* cũng chỉ có **4 lựa chọn**: `string`, `int`, `dateTime`, `bool` — giống hệt issue 7.
> 🔧 **Fix**: cùng lần với issue 7 — xem issue 7.

**26** ⚠️ Chờ quyết định nghiệp vụ · Chức năng · Lỗi · 29/09 · Trung bình · *chưa gán* · [ảnh](images/issue-26.png)
Gói tin "Bắt buộc" gặp lỗi khi lấy dữ liệu mẫu ⇒ nên tự sinh GUID.
> **Ảnh**: gói `104_weatherData`, 2 bản ghi mẫu có `message`, `status`, `sessionid` **đều NULL**. Cảnh báo *"Trường bắt buộc sessionid đang rỗng — service sẽ chặn bản ghi này"*. Payload dựng ra `"sessionId": null`. Ghi chú tester: *"đang cấu hình gói tin Bắt buộc không cho rỗng"*.
> 📌 Cùng họ với issue 20 — dữ liệu nguồn NULL, không phải lỗi ánh xạ.
> ⚠️ **Chưa fix** — tự sinh GUID là tính năng mới, FE và service phải sinh y hệt nhau. Xem MasterPlan §F mục 26.

### Lịch sử chia sẻ

**27** ✅ Đã fix 30/09 · Chức năng · Lỗi · 29/09 · Thấp · DatHQ · [ảnh](images/issue-27.png)
Không reset thời gian bắt đầu/kết thúc khi bấm "Đặt lại".
> **Ảnh**: tab *Nhật ký cấu hình*, `Thời gian bắt đầu = 2020-01-01 00:00:00` (khoanh đỏ) còn nguyên sau khi bấm **Đặt lại**; `Thời gian kết thúc = 2026-09-29 23:59:59`. Lưới trả 178 bản ghi.
> 🔧 **Fix**: đã fix logic reset bộ lọc thời gian.

**28** ✅ Đã fix 01/10 · Chức năng · Lỗi · 29/09 · Trung bình · *chưa gán* · [ảnh a](images/issue-28-a.png) · [ảnh b](images/issue-28-b.png)
Tìm kiếm/Đặt lại/Làm mới cùng một lỗi: kết quả không thuộc phạm vi; không chặn bắt đầu > kết thúc; không chặn thời gian tương lai.
> **Ảnh a**: 🔴 `Thời gian bắt đầu = 2030-01-01`, `Thời gian kết thúc = 2028-09-30` — **bắt đầu sau kết thúc, cả hai ở tương lai** — vẫn trả về **đủ 178 bản ghi**, các dòng đều ngày `29/09/2026`.
> **Ảnh b**: tab *Nhật ký truyền nhận*, đối tác `TTCSDL`, lọc `2026-09-29` → `2026-09-30`, nhưng lưới trả các dòng ngày **22/09/2026**. Tổng 22 517 dòng / 451 trang.
> ⇒ 🔴 **Bộ lọc thời gian bị bỏ qua hoàn toàn**, không chỉ là lỗi nút Đặt lại.
> 🔧 **Fix**: thêm `:disabled-date="disableFutureDate"` + hàm `disableFutureDate` dùng `dayjs().endOf('day')` (`history/index.vue`).

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
