# ShareData — Chỉ mục tài liệu

Kho tài liệu nghiệp vụ, đặc tả kỹ thuật và mapping gói tin của phân hệ **Chia sẻ Dữ liệu (ShareData / TMC-PM-ITS-ESHARE)** — Dự án Cao tốc Hữu Nghị – Chi Lăng.

> 🔴 **Single Source of Truth (SSOT)**: File này là điểm vào duy nhất quản lý toàn bộ danh mục tài liệu của phân hệ ShareData. Mọi bổ sung hoặc thay đổi tài liệu của phân hệ ShareData chỉ cần cập nhật tại đây.

---

## Danh mục tài liệu

| File | Nội dung chính | Khi nào đọc |
|---|---|---|
| [`01-yeu-cau-nghiep-vu.md`](01-yeu-cau-nghiep-vu.md) | Yêu cầu hệ thống phần mềm chia sẻ dữ liệu (TMC-PM-ITS-ESHARE / TA-ShareData), trích từ Chỉ dẫn kỹ thuật HN-CL. Kiến trúc ITS Core, các chuẩn giao tiếp, chu kỳ dữ liệu. | Nguồn yêu cầu gốc — đọc trước khi phát triển hoặc sửa đổi logic phân hệ ShareData. |
| [`02-mapping-goi-tin-101-111.md`](02-mapping-goi-tin-101-111.md) | Bảng đặc tả chi tiết ánh xạ từng trường dữ liệu (payload camelCase) ↔ các bảng CSDL WebAPI (`TmsZoneStatus`, `TmsZone`, `TmsIncident`, `TollTransactionOut`...) cho 11 gói tin chia sẻ (101–111). | Khi triển khai, kiểm thử mapping payload hoặc chốt giao tiếp với đối tác. |
| [`sharedata_plan.md`](sharedata_plan.md) | Đặc tả yêu cầu & kế hoạch họp theo các mốc ngày (19/08 và 25/08): luồng gửi/nhận, giao thức, bảo mật, thời hạn bàn giao và các lưu ý kỹ thuật. | Khi cần nắm bối cảnh nghiệp vụ, tiến độ và các quyết định triển khai. |
| `_source/` | File tài liệu gốc dành riêng cho người xem (PDF Chỉ dẫn kỹ thuật, XLSX mapping). AI không cần đọc thư mục này. | Đối chiếu khi cần xác minh với văn bản gốc của chủ đầu tư. |

---

## 📌 Lưu ý kiến trúc quan trọng (Memory Pointer)
- **Định hướng dịch vụ**: Phân hệ `ShareDataWorker` hoạt động như một dịch vụ trích xuất và xuất bản dữ liệu độc lập (`DataPublicationService`) — đọc dữ liệu từ CSDL, ánh xạ và xuất file JSON/XML ra thư mục cục bộ theo đúng đặc tả.
- **Quy tắc cô lập Entity**: Không tự ý sửa đổi các entity dùng chung (`Esh*`) do WebAPI sở hữu; worker chủ động thích ứng bằng DTO/Model độc lập trong phân hệ.
