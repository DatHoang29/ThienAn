# ShareData — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ, đặc tả kỹ thuật và mapping gói tin của phân hệ **Chia sẻ Dữ liệu (ShareData / TMC-PM-ITS-ESHARE)** — Dự án Cao tốc Hữu Nghị – Chi Lăng.

> 🔴 **Single Source of Truth (SSOT)**: File này là điểm vào duy nhất quản lý toàn bộ danh mục tài liệu của phân hệ ShareData. Mọi bổ sung hoặc thay đổi tài liệu của phân hệ ShareData chỉ cần cập nhật tại đây.

---

## Tier Table

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| A | `doc/01-yeu-cau-nghiep-vu.md` | full | 27 KB | Yêu cầu hệ thống phần mềm chia sẻ dữ liệu (TMC-PM-ITS-ESHARE / TA-ShareData) | `_source/pdf/chi-dan-ky-thuat.pdf` (chưa có trong repo) |
| A | `doc/02-mapping-goi-tin-101-111.md` | full | 18 KB | Bảng đặc tả chi tiết ánh xạ từng trường dữ liệu (payload camelCase) ↔ các bảng CSDL WebAPI | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` |
| A | `doc/transcript/00-catalog.md` | full | 2 KB | Mục lục toàn bộ bản ghi cuộc họp ShareData | Biên soạn nội bộ |
| A | `doc/transcript/2026-09-09-review-sharedata.md` | full | 17 KB | Review ShareData toàn diện: Luồng gửi nhận, cấu hình gói tin, SQL alias mapping, xử lý theo giờ/time (9h sáng), switch gửi khi có data mới, Socket wrapper & xóa log đầu ngày | `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 3.m4a`, `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 4.m4a` |
| A | `doc/transcript/2026-09-11-sharedata-script.md` | full | 20 KB | Chuẩn hóa cấu hình gói tin (gói 101,...): Metadata trường động, mapping 2 chiều, CodeSet, hàm SUM/AVG, NATS/bảng đệm | `../Plan/_source/audio/2026-09-11-sharedata-videowall-{1,2}.m4a` |
| A | `doc/transcript/16-09-2026-review-frontend-sharedata.md` | full | 34 KB | Review & Hoàn thiện Frontend ShareData: Đồng bộ tiếng Việt, ẩn Cảnh báo/Ưu tiên, Mã đối tác, Tooltip Ant Design, fix scroll STT, luồng Đối tác -> Gói tin (101-111) -> Lịch gửi, engine Ánh xạ dữ liệu, badge CodeSet/Format, auto map | `../Plan/_source/audio/16-09-2026-review-frontend-sharedata.m4a` |
| A | `doc/transcript/16-09-2026-sua-ui-sharedata.md` | full | 17 KB | Tổng hợp & Thống nhất Danh mục Sửa UI ShareData: Thêm Mã đối tác, rào port, checkbox gửi khi có data mới, bỏ lịch ở chiều nhận, CRUD gói tin/cột, gom nhóm theo Tệp dữ liệu, Default Value cho CodeSet, DateTime picker, modal chi tiết thay sidebar, log 2 bước cha-con | `../Plan/_source/audio/16-09-2026-sua-ui-sharedata.m4a` |
| A | `doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` | full | 12 KB | Định hướng Refactor Backend Worker: tách pipeline Outbound thành 3 Process (Extraction → Mapping → Transport), DTO ngữ cảnh xuyên suốt, cô lập lỗi tại tầng Mapping | `../Plan/_source/audio/16-09-2026-refactor-backend-sharedata-worker.m4a` |
| A | `doc/transcript/16-09-2026-dinh-danh-doi-tac-va-test-tai.md` | full | 24 KB | Định danh đối tác qua PartnerCode trong body, **bỏ phong bì PDU**, **bỏ rẽ nhánh theo version**, clone service + cờ chỉ-gửi/chỉ-nhận để test tải đa đối tác, rủi ro nghẽn CSDL | `../Plan/_source/audio/13.48, 16 thg 9__1.m4a` |
| A | `doc/transcript/19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md` | full | 6 KB | Bổ sung thông tin định danh (PartnerCode, Mapping) vào HTTP Header khi gọi REST API luồng Gửi; phía nhận bóc tách Header và tải danh mục Mapping lên RAM để tra cứu nhanh; giữ nguyên Body là mảng payload thuần túy | `_source/MakeUp Chi Ngô Gò Vấp.m4a`, `_source/MakeUp Chi Ngô Gò Vấp 2.m4a` |
| A | `Plan/Sd_MasterPlan_16-09-2026.md` | full | 44 KB | Master Plan ShareData FE · BE · Service (chốt 16/09/2026) | Biên soạn nội bộ |
| A | `doc/TargetShapeJson.md` | full | 14 KB | 🔴 **TÀI LIỆU BÀN GIAO của Hiếu (18/09) — NGUỒN SỰ THẬT cho bộ khung.** Toàn bộ ký hiệu `TargetShapeJson` và cách đọc cho 3 chiều. §3.4 bảng **6 khoá `$extend`** thật (`codeSet`·`targetType`·`dateFormat`·`numberFormat`·`defaultPartnerValue`·`defaultSourceValue`) — **không có** `format`/`defaultValue`, `expression` **đã chết**. §3.5 **4 token `$meta`**, không bao giờ kèm `$extend`. §4.1 thứ tự: **bộ mã → mặc định → ép kiểu**, qua bộ mã thì **không ép kiểu nữa**. §9.3 mẫu thân bản tin đích | Ngô Văn Hiếu bàn giao |
| A | `Plan/sharedata-outbound-kiem-tra-anh-xa-va-dinh-dang.md` | full | 77 KB | 🔴 **TÀI LIỆU SỐNG — đọc tệp này là hiểu trọn luồng GỬI, không cần mở tệp khác.** Chú giải dấu (🔴 🟠 🟡 🟢 ⚠️) · Mục 0 luồng chuẩn (sơ đồ + 4 giai đoạn + ví dụ đi trọn một trường) · 1 nguồn cấu hình (`ShareDataMapping.TargetShapeJson`, bảng khoá `$extend`) · 2 biến đổi một trường · 3 đang kẹt (**P1–P4**; **P3 đã gỡ 18/09** cùng việc 6b) · 4 việc còn lại · 5 chưa chốt · 6 ngoài phạm vi · 7 nhật ký. 🟢 **M1 · N1 · SV-1a xong 18/09**, chỉ còn **SV-12** đang mở | Biên soạn nội bộ |
| A | `Prompt/sharedata-worker-giai-meta-prompt.md` | full | 9 KB | 🟢 **ĐÃ THỰC THI 18/09** — việc **M1**. Nền: staging đã khai `$meta` (`Now`·`PartnerCode`·`PacketCode`·`Serial`) nhưng worker **0 dòng** hỗ trợ ⇒ renderer nhả nguyên văn, đối tác đang nhận `{"$meta":"Now"}` thay vì giá trị thật. ⚠️ `$value` **không phải nguồn giá trị**, chỉ là giá trị hiển thị trên giao diện | Biên soạn nội bộ |
| A | `Prompt/sharedata-bo-vo-httppayload-prompt.md` | full | 9 KB | 🟢 **ĐÃ THỰC THI 18/09** — việc **SV-1a** — bỏ vỏ `httpPayload` 7 khoá, thân HTTP **luôn là mảng bản ghi** (`rawContent` hiện là **chuỗi**, bên nhận phải parse JSON hai lần). Dính nhóm **mã đối tác** nên xếp cuối; tiên quyết: luồng 1-1 ổn · **M1 xong** · **4 khoá meta đã chuyển từ `header` xuống `data`** | Biên soạn nội bộ |
| A | `Prompt/sharedata-event-gui-khi-co-du-lieu-moi-prompt.md` | full | 16 KB | ⏳ **ĐANG MỞ — sẵn sàng chạy.** Việc **SV-12**. Cột **`SendOnNewData`** đã có (Hiếu, commit `7f035e63`) — khả thi **không cần NATS/CDC**: cờ thật do WebAPI cung cấp, worker polling theo `IntervalSeconds`; cờ bật gửi phần **mới · cập nhật · soft-delete** theo policy từng gói. Cursor dùng `LastTimeRun + LastDataId`, mọi query chuẩn hóa `__watermark`/`__rowid`/`__operation`; không dùng `LastPayloadHash`, không còn giả định "6 gói ảnh chụp + MAX watermark". Gói 110 `NotReady`, 111 `Disabled/skip` | Biên soạn nội bộ |
| C | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | never | 57 KB | Toàn bộ danh mục bảng CSDL và mapping trường dữ liệu ESHARE | → bản `.md`: `doc/02-mapping-goi-tin-101-111.md` |
| C | `_source/MakeUp Chi Ngô Gò Vấp.m4a` | never | 1.6 MB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 1 (03:13) | → bản `.md`: `doc/transcript/19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md` (hợp nhất) |
| C | `_source/MakeUp Chi Ngô Gò Vấp 2.m4a` | never | 200 KB | Audio cuộc họp thảo luận đưa thông tin định danh vào HTTP Header phần 2 (00:23) | → bản `.md`: `doc/transcript/19-09-2026-truyen-thong-tin-qua-http-header-luong-gui.md` (hợp nhất) |
| C | `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 3.m4a` | never | 17 MB | Audio cuộc họp Review ShareData phần 1 (35:36) — nằm ở `Plan/_source/audio/` vì file gốc dùng chung tên đặt trước khi tách theo phân hệ | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |
| C | `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 4.m4a` | never | 12 MB | Audio cuộc họp Review ShareData phần 2 (25:41) | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-review-frontend-sharedata.m4a` | never | 60.5 MB | Audio cuộc họp Review Frontend & Ánh xạ dữ liệu ShareData (32:41) | → bản `.md`: `doc/transcript/16-09-2026-review-frontend-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-sua-ui-sharedata.m4a` | never | 11.8 MB | Audio cuộc họp Recap & Thống nhất danh mục Sửa UI ShareData (06:13) | → bản `.md`: `doc/transcript/16-09-2026-sua-ui-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-refactor-backend-sharedata-worker.m4a` | never | 8.0 MB | Audio cuộc họp Định hướng Refactor Backend Worker ShareData (04:12) | → bản `.md`: `doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` |

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



