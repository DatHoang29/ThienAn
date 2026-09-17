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
| A | `Plan/Sd_MasterPlan_16-09-2026.md` | full | 31 KB | Master Plan ShareData FE · BE · Service (chốt 16/09/2026) | Biên soạn nội bộ |
| A | `Plan/sharedata-outbound-kiem-tra-anh-xa-va-dinh-dang.md` | full | 10 KB | Kế hoạch: Đối chiếu ánh xạ với dữ liệu thô + Định dạng đầu ra (luồng GỬI) | Biên soạn nội bộ |
| C | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | never | 57 KB | Toàn bộ danh mục bảng CSDL và mapping trường dữ liệu ESHARE | → bản `.md`: `doc/02-mapping-goi-tin-101-111.md` |
| C | `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 3.m4a` | never | 17 MB | Audio cuộc họp Review ShareData phần 1 (35:36) — nằm ở `Plan/_source/audio/` vì file gốc dùng chung tên đặt trước khi tách theo phân hệ | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |
| C | `../Plan/_source/audio/MakeUp Chi Ngô Gò Vấp 4.m4a` | never | 12 MB | Audio cuộc họp Review ShareData phần 2 (25:41) | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-review-frontend-sharedata.m4a` | never | 60.5 MB | Audio cuộc họp Review Frontend & Ánh xạ dữ liệu ShareData (32:41) | → bản `.md`: `doc/transcript/16-09-2026-review-frontend-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-sua-ui-sharedata.m4a` | never | 11.8 MB | Audio cuộc họp Recap & Thống nhất danh mục Sửa UI ShareData (06:13) | → bản `.md`: `doc/transcript/16-09-2026-sua-ui-sharedata.md` |
| C | `../Plan/_source/audio/16-09-2026-refactor-backend-sharedata-worker.m4a` | never | 8.0 MB | Audio cuộc họp Định hướng Refactor Backend Worker ShareData (04:12) | → bản `.md`: `doc/transcript/16-09-2026-refactor-backend-sharedata-worker.md` |

---

## 📌 Lưu ý kiến trúc quan trọng (Memory Pointer)
- **Định hướng dịch vụ**: Phân hệ `ShareDataWorker` hoạt động như một dịch vụ trích xuất và xuất bản dữ liệu độc lập (`DataPublicationService`) — đọc dữ liệu từ CSDL, ánh xạ và xuất file JSON/XML ra thư mục cục bộ theo đúng đặc tả.
- **Quy tắc cô lập Entity**: Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu; worker chủ động thích ứng bằng DTO/Model độc lập trong phân hệ.
