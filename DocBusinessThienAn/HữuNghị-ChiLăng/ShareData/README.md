# ShareData — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ, đặc tả kỹ thuật và mapping gói tin của phân hệ **Chia sẻ Dữ liệu (ShareData / TMC-PM-ITS-ESHARE)** — Dự án Cao tốc Hữu Nghị – Chi Lăng.

> **Quyết định hiện hành 14/10/2026:** **105 mặc định Snapshot** theo [mapping `All/update`](doc/02-mapping-goi-tin-101-111.md); setting gói cho chọn Snapshot/Incremental và Worker dùng setting. **110 được tạo/cấu hình bình thường với bảng/dữ liệu mô phỏng**. Viết sẵn code Kafka producer, CSV/FTP, mã hóa, cleanup để dùng khi cấu hình áp dụng; phải chạy Playwright sau sửa FE. Tiến độ ở [MasterPlan](Plan/Sharedata_MasterPlan.md). Các prompt/biên bản rà soát cũ là lịch sử, không thay quyết định này.

> 🔴 **Single Source of Truth (SSOT)**: File này là điểm vào duy nhất quản lý toàn bộ danh mục tài liệu của phân hệ ShareData. Mọi bổ sung hoặc thay đổi tài liệu của phân hệ ShareData chỉ cần cập nhật tại đây.

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

## Tier Table

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| A | `doc/01-yeu-cau-nghiep-vu.md` | full | 27 KB | Yêu cầu hệ thống phần mềm chia sẻ dữ liệu (TMC-PM-ITS-ESHARE / TA-ShareData) | `_source/pdf/chi-dan-ky-thuat.pdf` (chưa có trong repo) |
| A | `doc/02-mapping-goi-tin-101-111.md` | full | 18 KB | Bảng đặc tả chi tiết ánh xạ từng trường dữ liệu (payload camelCase) ↔ các bảng CSDL WebAPI | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` |
| A | `doc/03-bao-cao-tong-the-entity-csdl-sharedata.md` | full | 25 KB | Báo cáo tổng thể toàn bộ 14 Entity và bảng CSDL phân hệ ShareData (cũ & mới, cấu trúc, vai trò, CSDL Tenant) | Biên soạn nội bộ |
| A | `doc/08-thiet-ke-luong-noi-duoi-toan-trinh-ui-be-worker.md` | full | 41 KB | Đặc tả thiết kế cơ chế nối đuôi (`@lastKey` / `__lastKey`) & luồng xử lý toàn trình UI + BE + WORKER (cô lập con trỏ đối tác, kiểm định AST, phân trang có phanh, chống mất dữ liệu khi crash) | Tổng hợp cuộc họp 26–27/10/2026, 2 bản vẽ tay `50d2fc5f676ae734be7b.jpg` · `b7af82ba478cc7d29e9d.jpg` & mã nguồn |
| A | `doc/transcript/00-catalog.md` | full | 2 KB | Mục lục toàn bộ bản ghi cuộc họp ShareData | Biên soạn nội bộ |
| A | `doc/transcript/09-09-2026-review-sharedata.md` | full | 17 KB | Review ShareData toàn diện: Luồng gửi nhận, cấu hình gói tin, SQL alias mapping, xử lý theo giờ/time (9h sáng), switch gửi khi có data mới, Socket wrapper & xóa log đầu ngày | `../Plan/_source/audio/09-09-2026/09-09-2026-review-sharedata-phan-{1,2}.m4a` |
| A | `doc/transcript/11-09-2026-sharedata-script.md` | full | 20 KB | Chuẩn hóa cấu hình gói tin (gói 101,...): Metadata trường động, mapping 2 chiều, CodeSet, hàm SUM/AVG, NATS/bảng đệm | `../Plan/_source/audio/11-09-2026/11-09-2026-sharedata-videowall-{1,2}.m4a` |
| A | `doc/transcript/16-09-2026-review-frontend-sharedata.md` | full | 34 KB | Review & Hoàn thiện Frontend ShareData: Đồng bộ tiếng Việt, ẩn Cảnh báo/Ưu tiên, Mã đối tác, Tooltip Ant Design, fix scroll STT, luồng Đối tác -> Gói tin (101-111) -> Lịch gửi, engine Ánh xạ dữ liệu, badge CodeSet/Format, auto map | `../Plan/_source/audio/16-09-2026/16-09-2026-review-frontend-sharedata.m4a` |
| A | `doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` | full | 12 KB | Định hướng Refactor Backend Worker: tách pipeline Outbound thành 3 Process (Extraction → Mapping → Transport), DTO ngữ cảnh xuyên suốt, cô lập lỗi tại tầng Mapping | `../Plan/_source/audio/16-09-2026/16-09-2026-refactor-backend-sharedata-worker.m4a` |
| A | `doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md` | full | 24 KB | Định danh đối tác qua PartnerCode trong body, **bỏ phong bì PDU**, **bỏ rẽ nhánh theo version**, clone service + cờ chỉ-gửi/chỉ-nhận để test tải đa đối tác, rủi ro nghẽn CSDL | `../Plan/_source/audio/16-09-2026/16-09-2026-dinh-danh-doi-tac-va-test-tai.md` |
| A | `doc/transcript/12-10-2026-sharedata-va-scheduler-chung.md` | full | 16 KB | Chuẩn hóa Component Lập lịch (`Scheduler`) dùng chung, chuẩn hóa Tab phân cấp, tách tầng tích hợp kết nối `HandleConnection` (`API` / `Kafka` convert `JSON` → `byte[]` / `File`), khai báo `Type` trong `SystemConfig` & `SystemConfigData`, truy vấn động cho `SuperAdmin` có Validator & Audit Log | `_source/Đường Số 10.m4a` |
| A | `Plan/Sharedata_MasterPlan.md` | full | 70 KB | 🔴 **SỔ THEO DÕI TASK DUY NHẤT của ShareData — mở đúng tệp này là biết task nào xong, task nào chưa** (checklist `SV-*` / `BE-*` / FE). Đồng thời là tài liệu sống: kiến trúc bảng `ShareDataLastSend` và cursor kép, phân trang có phanh, luồng phát hiện dữ liệu mới (Change Tracking + NATS), 3 tầng ghi nhận nhật ký, danh mục bảng nguồn `ShareDataSourceTable` (mốc Change Tracking riêng từng bảng), bảng 13 edge case, cơ chế tương tranh nhiều worker, và `SV-12 §10` giữ **bảng quyết định đã chốt + phương án bị bác**. 📌 Báo cáo `Sharedata_Review_TongThe_20260927.md` đã gộp trọn vào đây và **xoá ngày 28/09/2026** ⇒ ⛔ không còn tệp review nào trong `Plan/` | Biên soạn nội bộ |
| A | `sql/seed-goi-105-rfiddata.sql` | full | 7 KB | Script seed bản ghi gói tin **105_rfidData** vào `ShareDataPacket` (`OrderNo = 5`). Bản ghi `104_rfidData` đã bị xoá 24/09 nên gói 105 không phân giải được. ⛔ Lập trình viên tự chạy tay (rule 9); chạy lại nhiều lần an toàn. Kèm mẫu đăng ký và ghi chú 2 việc còn lại | Biên soạn nội bộ từ đo MCP staging 24/09/2026 |
| A | `doc/TargetShapeJson.md` | full | 14 KB | 🔴 **TÀI LIỆU BÀN GIAO của Hiếu (18/09) — NGUỒN SỰ THẬT cho bộ khung.** Toàn bộ ký hiệu `TargetShapeJson` và cách đọc cho 3 chiều. §3.4 bảng **6 khoá `$extend`** thật (`codeSet`·`targetType`·`dateFormat`·`numberFormat`·`defaultPartnerValue`·`defaultSourceValue`) — **không có** `format`/`defaultValue`, `expression` **đã chết**. §3.5 **4 token `$meta`**, không bao giờ kèm `$extend`. §4.1 thứ tự: **bộ mã → mặc định → ép kiểu**, qua bộ mã thì **không ép kiểu nữa**. §9.3 mẫu thân bản tin đích | Ngô Văn Hiếu bàn giao |
| A | `Prompt/README.md` | full | 24 KB | Mục lục prompt thực thi ShareData: danh sách prompt **chưa làm**, các quyết định hiện hành, và bảng lịch sử **toàn bộ prompt đã thực thi rồi xoá** (kèm kết quả từng prompt). 📌 Tra một prompt cũ đã làm gì thì mở tệp này, ⛔ đừng tìm tệp prompt — phần lớn đã xoá sau khi nghiệm thu | Biên soạn nội bộ |
| C | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | never | 57 KB | Toàn bộ danh mục bảng CSDL và mapping trường dữ liệu ESHARE | → bản `.md`: `doc/02-mapping-goi-tin-101-111.md` |
| C | `../Plan/_source/audio/09-09-2026/09-09-2026-review-sharedata-phan-1.m4a` | never | 17 MB | Audio cuộc họp Review ShareData phần 1 (35:36) | → bản `.md`: `doc/transcript/09-09-2026-review-sharedata.md` |
| C | `../Plan/_source/audio/09-09-2026/09-09-2026-review-sharedata-phan-2.m4a` | never | 12 MB | Audio cuộc họp Review ShareData phần 2 (25:41) | → bản `.md`: `doc/transcript/09-09-2026-review-sharedata.md` |
| C | `../Plan/_source/audio/11-09-2026/11-09-2026-sharedata-videowall-1.m4a` | never | 55 MB | Audio cuộc họp Chuẩn hóa cấu hình gói tin & mapping 2 chiều phần 1 (01:01:37) | → bản `.md`: `doc/transcript/11-09-2026-sharedata-script.md` |
| C | `../Plan/_source/audio/11-09-2026/11-09-2026-sharedata-videowall-2.m4a` | never | 30 MB | Audio cuộc họp Chuẩn hóa cấu hình gói tin & mapping 2 chiều phần 2 (50:11) | → bản `.md`: `doc/transcript/11-09-2026-sharedata-script.md` |
| C | `../Plan/_source/audio/16-09-2026/16-09-2026-review-frontend-sharedata.m4a` | never | 60.5 MB | Audio cuộc họp Review Frontend & Ánh xạ dữ liệu ShareData (32:41) | → bản `.md`: `doc/transcript/16-09-2026-review-frontend-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026/16-09-2026-sua-ui-sharedata.m4a` | never | 11.8 MB | Audio cuộc họp Recap & Thống nhất danh mục Sửa UI ShareData (06:13) | _(đã gộp nội dung, xóa bản `.md` trùng)_ |
| C | `../Plan/_source/audio/16-09-2026/16-09-2026-refactor-backend-sharedata-worker.m4a` | never | 8.0 MB | Audio cuộc họp Định hướng Refactor Backend Worker ShareData (04:12) | → bản `.md`: `doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` |
| C | `../Plan/_source/audio/16-09-2026/16-09-2026-dinh-danh-doi-tac-va-test-tai.m4a` | never | 81.3 MB | Audio cuộc họp Định danh đối tác, bỏ phong bì PDU, test tải đa đối tác (16:47) | → bản `.md`: `doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md` |
| C | `../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-1.m4a` | never | 1.6 MB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 1 (03:13) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/19-09-2026/19-09-2026-sharedata-http-header-phan-2.m4a` | never | 200 KB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 2 (00:23) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-1-ui-mapping.m4a` | never | 2.8 MB | Audio cuộc họp Phần 1: UI & Luồng Mapping (05:53) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-2-gui-noi-duoi-va-du-lieu-moi.m4a` | never | 8.3 MB | Audio cuộc họp Phần 2: Cơ chế gửi nối đuôi theo mốc & xử lý dữ liệu mới (17:15) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-3-mapping-profile.m4a` | never | 4.0 MB | Audio cuộc họp Phần 3: Sinh mã hồ sơ & luồng 1 chiều (08:17) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-phan-4-tong-ket.m4a` | never | 1.6 MB | Audio cuộc họp Phần 4: Tổng kết & chốt luồng (03:21) | _(đã dọn dẹp / không lưu transcript .md)_ |
| C | `../Plan/_source/audio/21-09-2026/21-09-2026-sharedata-clip-gui-noi-duoi.m4a` | never | 17.0 MB | Audio trích đoạn giải thích cơ chế gửi nối đuôi (09:09) | _(đã dọn dẹp / không lưu transcript .md)_ |

---

## 📁 Thư mục con của phân hệ

| Thư mục | Chứa gì | Ghi chú |
|---|---|---|
| `doc/` | Tài liệu **nghiệp vụ & đặc tả** — xem Tier Table ở trên | |
| `sql/` | Kịch bản CSDL (`.sql`) nội bộ của phân hệ ShareData | Rule 16 |
| `Plan/` | 🔴 `Sharedata_MasterPlan.md` — **sổ theo dõi task duy nhất** | rule 19.24 |
| `Prompt/` | Prompt thực thi từng bước | rule 13 |
| `KiemThu/` | 🧪 **Kiểm thử & nghiệm thu** — nhật ký lỗi của QA, biểu mẫu F16, ảnh chụp màn hình từng issue | ⛔ Tách bạch khỏi `doc/`, ⛔ không trộn vào Tier Table nghiệp vụ |
| `_source/` | Bản gốc Tier C (xlsx, pdf) — 👤 human-only | rule 16 |

📌 Điểm vào của `KiemThu/`: [`KiemThu/README.md`](KiemThu/README.md).

---

## 📌 Lưu ý kiến trúc quan trọng (Memory Pointer)

- **Pipeline 3 Process (chốt 16/09/2026)**: luồng GỬI chạy Extraction → Mapping → Transport, ngữ cảnh dùng chung xuyên suốt. Lỗi ở tầng Mapping **ngắt ngay**, không sang tầng gửi. ✅ Phân hệ đã được đổi tên `DataPublication` → **`DataOutbound`** cho đối xứng với `DataInbound`.
- **Đã bỏ phong bì PDU và XML**: nội dung xuất ra = **y hệt kết quả ánh xạ**, không bọc thêm. `hash` bỏ theo. Chỉ còn JSON.
- **Hai kênh xuất bản KHÔNG ngang hàng (chốt 18/09/2026)**: **gửi HTTP quyết trạng thái lô** — luôn gọi, kể cả khi đối tác chưa khai `EndPointApiUrl`, và **thành hay bại đều ghi lịch sử**. **Ghi tệp chỉ chạy ở local/dev**, là bản lưu để xem — **im lặng hoàn toàn, hỏng cũng không ghi log và không ảnh hưởng trạng thái**.
- **Câu truy vấn nằm CỨNG trong mã C#** (11 hàm `QueryPacketNNN`), **không** trong CSDL. Bí danh sau `AS` chính là tên trường mà bộ khung ánh xạ trỏ vào.
- **Nguồn cấu hình ánh xạ lúc chạy (chốt 17/09/2026)**: worker đọc **DUY NHẤT `ShareDataMapping.TargetShapeJson`**, mọi cấu hình cấp trường nằm trong `$extend`. `ShareDataPacketField` là **danh mục lúc thiết kế** cho giao diện CRUD, **không phải nguồn của worker** — nó chỉ là chỗ lấy dữ liệu để **điền** vào `$extend`.
- ⚠️ **`$extend` đổi PHẠM VI cấu hình**: `FieldsJson` cũ khai một lần áp cho **mọi đối tác**; `$extend` nằm trong `ShareDataMapping` nên khai **riêng từng đối tác, từng chiều**. Thêm đối tác mới là phải khai lại từ đầu.
- ✅ **`ShareDataTable` đã bị xoá khỏi mã 17/09/2026** (commit `fa60436d`, Văn Hiếu). Worker từng gãy 8 lỗi `CS0246`, **đã gỡ xong, bản dịch xanh**.
- 🔴 **Bộ khung hiện gần như rỗng**: phễu lọc chiều Gửi đang sống khai `$extend` cho **1/16 trường** (gói 101). Cơ chế đã đủ — **thiếu dữ liệu**, việc điền thuộc bên giao diện/module. Chi tiết: mục 3.2 của tài liệu sống.
- **Quy tắc cô lập Entity**: Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu; worker chủ động thích ứng bằng DTO/Model độc lập trong phân hệ.
- **Luồng NHẬN không đọc `ShareDataTable`** — gói tin (`ShareDataPacket` + `ShareDataPacketWrite` + phễu lọc chiều nhận) là nguồn cấu hình duy nhất.



