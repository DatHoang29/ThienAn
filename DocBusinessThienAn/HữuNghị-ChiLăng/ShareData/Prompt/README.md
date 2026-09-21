# ShareData — Prompt

Thư mục này chứa **prompt thực thi** cho phân hệ ShareData (`<task-slug>-prompt.md`). Sau khi một task đã triển khai, kiểm thử và nghiệm thu xong, prompt được giữ lại để lập trình viên review và đối chiếu sau khi code change hoàn tất (chỉ xóa khi người dùng trực tiếp yêu cầu theo `.agents/rules/thienan_rules.md`).

Tài liệu sống nằm ở [`../Plan/`](../Plan/), không đặt trong thư mục này.

## Đang chờ thực thi — Backend Worker Outbound

| Thứ tự | Prompt | Trạng thái | Phạm vi |
|---:|---|---|---|
| **1** | [Outbound gửi nối đuôi theo checkpoint](sharedata-outbound-gui-noi-duoi-prompt.md) | 🟢 **Sẵn sàng thực thi** | Bảng mốc riêng `(PartnerCode, PacketCode, LastTime, LastKey)`; cursor kép; paging có budget; commit theo page có lease guard; 106 lọc `Source` WIM từ cấu hình. Không làm NATS/CDC/Inbound/`SendOnNewData` |
| **2** | [Cờ "chỉ gửi khi có dữ liệu mới"](sharedata-event-gui-khi-co-du-lieu-moi-prompt.md) | 🟠 **Tạm hoãn — không thực thi** | Chờ bàn và chốt kiến trúc phát hiện thay đổi bằng **NATS hay CDC**. Nội dung cũ còn trộn với nối đuôi, chỉ dùng làm hồ sơ tham khảo |

### Quyết định hiện hành

- Gửi nối đuôi phải hoàn thiện trước; cơ chế phát hiện dữ liệu mới là task riêng.
- Cursor lưu ở bảng `ShareDataOutboundCheckpoint`, không thêm `LastDataId` vào `ShareDataSubscription`.
- Gói 106 **không bị gỡ**: policy `AlwaysIncremental`, 4 field tải trọng thiếu giữ `null`, dữ liệu chỉ được lấy khi `TmsTrafficData.Source` thuộc allow-list cấu hình. Mặc định allow-list rỗng.
- Bảo đảm giao hàng là **at-least-once**; tin xóa và `Idempotency-Key` vẫn hoãn.
- Snapshot 101 và 105 còn vấn đề khối lượng query độc lập; prompt nối đuôi không tuyên bố đã sửa hai vấn đề đó.

## Đã thực thi và xoá theo Auto-Cleanup

| Prompt | Kết quả |
|---|---|
| `sharedata-codeset-cau-truc-moi-prompt.md` | 🟢 **18/09/2026** · việc **C1** |
| `sharedata-khung-gio-lich-gui-prompt.md` | 🟢 **18/09/2026** · việc **S1** |
| `sharedata-go-duong-nap-packetfield-prompt.md` | 🟢 **18/09/2026** · việc **6b** |
| `sharedata-http-quyet-trang-thai-ghi-tep-im-lang-prompt.md` | 🟢 **18/09/2026** · việc **A2** |
| `sharedata-bo-canh-bao-truong-thieu-prompt.md` | 🟢 **18/09/2026** · việc **C2** |
| `sharedata-dong-bo-extend-theo-ban-giao-prompt.md` | 🟢 **18/09/2026** · việc **N1** |
| `sharedata-worker-giai-meta-prompt.md` | 🟢 **18/09/2026** · việc **M1** |
| `sharedata-bo-vo-httppayload-prompt.md` | 🟢 **18/09/2026** · việc **SV-1a** |

*(Loạt prompt Frontend nhóm A–F từ review 16/09/2026, `16-09-2026-prompt-flatten-datapublication.md` và `sharedata-bo-qua-khi-khong-co-kenh-prompt.md` cũng đã được xóa theo Auto-Cleanup.)*
