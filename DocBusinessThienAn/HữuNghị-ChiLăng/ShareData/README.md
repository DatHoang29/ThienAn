# ShareData — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ, đặc tả kỹ thuật và mapping gói tin của phân hệ **Chia sẻ Dữ liệu (ShareData / TMC-PM-ITS-ESHARE)** — Dự án Cao tốc Hữu Nghị – Chi Lăng.

> 🔴 **Single Source of Truth (SSOT)**: File này là điểm vào duy nhất quản lý toàn bộ danh mục tài liệu của phân hệ ShareData. Mọi bổ sung hoặc thay đổi tài liệu của phân hệ ShareData chỉ cần cập nhật tại đây.

---

## Tier Table

| Tier | File | Read | Size | Nội dung | Nguồn gốc |
|---|---|---|---|---|---|
| A | `doc/01-yeu-cau-nghiep-vu.md` | full | 27 KB | Yêu cầu hệ thống phần mềm chia sẻ dữ liệu (TMC-PM-ITS-ESHARE / TA-ShareData) | `_source/pdf/chi-dan-ky-thuat.pdf` (chưa có trong repo) |
| A | `doc/02-mapping-goi-tin-101-111.md` | full | 18 KB | Bảng đặc tả chi tiết ánh xạ từng trường dữ liệu (payload camelCase) ↔ các bảng CSDL WebAPI | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` |
| A | `doc/sharedata_plan.md` | full | 15 KB | Đặc tả yêu cầu & kế hoạch họp (19/08 và 25/08): luồng gửi/nhận, giao thức, bảo mật | Kế hoạch nội bộ |
| C | `_source/xlsx/ESHARE_TOAN_BO_BANG.xlsx` | never | 57 KB | Toàn bộ danh mục bảng CSDL và mapping trường dữ liệu ESHARE | → bản `.md`: `doc/02-mapping-goi-tin-101-111.md` |

---

## 📌 Lưu ý kiến trúc quan trọng (Memory Pointer)
- **Định hướng dịch vụ**: Phân hệ `ShareDataWorker` hoạt động như một dịch vụ trích xuất và xuất bản dữ liệu độc lập (`DataPublicationService`) — đọc dữ liệu từ CSDL, ánh xạ và xuất file JSON/XML ra thư mục cục bộ theo đúng đặc tả.
- **Quy tắc cô lập Entity**: Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu; worker chủ động thích ứng bằng DTO/Model độc lập trong phân hệ.
