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
| A | `doc/transcript/2026-09-09-review-sharedata.md` | full | 8 KB | Review ShareData toàn diện: Luồng gửi nhận, cấu hình gói tin, SQL alias mapping, chu kỳ gửi, sự kiện khẩn cấp & Socket wrapper | `_source/audio/2026-09-09-review-1.m4a`, `_source/audio/2026-09-09-review-2.m4a` |
| C | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | never | 57 KB | Toàn bộ danh mục bảng CSDL và mapping trường dữ liệu ESHARE | → bản `.md`: `doc/02-mapping-goi-tin-101-111.md` |
| C | `_source/audio/2026-09-09-review-1.m4a` | never | 17 MB | Audio cuộc họp Review ShareData phần 1 (35:36) | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |
| C | `_source/audio/2026-09-09-review-2.m4a` | never | 12 MB | Audio cuộc họp Review ShareData phần 2 (25:41) | → bản `.md`: `doc/transcript/2026-09-09-review-sharedata.md` |

---

## 📌 Lưu ý kiến trúc quan trọng (Memory Pointer)
- **Định hướng dịch vụ**: Phân hệ `ShareDataWorker` hoạt động như một dịch vụ trích xuất và xuất bản dữ liệu độc lập (`DataPublicationService`) — đọc dữ liệu từ CSDL, ánh xạ và xuất file JSON/XML ra thư mục cục bộ theo đúng đặc tả.
- **Quy tắc cô lập Entity**: Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu; worker chủ động thích ứng bằng DTO/Model độc lập trong phân hệ.
