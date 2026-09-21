# ShareData — Prompt

Thư mục này chỉ chứa **prompt thực thi từng bước, dùng 1 lần** cho phân hệ ShareData
(`<task-slug>-prompt.md`) — mỗi task/fix riêng = đúng 1 file. Sau khi thực thi xong, file prompt bị
**xoá tự động** theo quy ước Auto-Cleanup (`thienan_rules.md` mục 13/19.2).

Tài liệu SỐNG (plan tổng thể, review tiến độ — không bị xoá) nằm ở [`../Plan/`](../Plan/), không
đặt ở đây.

## Đang chờ thực thi — Backend Worker (luồng GỬI)

🟢 **Ba prompt M1 · N1 · SV-1a đã chạy xong 18/09** nhờ bản bàn giao
[`TargetShapeJson.md`](../doc/TargetShapeJson.md). **Còn đúng một prompt**, và nó cũng vừa hết chờ.

| Thứ tự | Prompt | Việc | Ghi chú |
|---|---|---|---|
| **1** | [Cờ "chỉ gửi khi có dữ liệu mới"](sharedata-event-gui-khi-co-du-lieu-moi-prompt.md) | **SV-12** | 🟢 **HẾT CHỜ — sẵn sàng chạy.** Hiếu đã bàn giao cột **`ShareDataSubscription.SendOnNewData`** (`bool?`) ở commit `7f035e63`. 🔴 **Ngữ nghĩa chốt 20/09: lịch quyết định KHI NÀO chạy, cờ chỉ quyết định LẤY BAO NHIÊU** — `null`/`false` = lấy toàn bộ, `true` = chỉ dòng đổi sau cursor; cả `continuous` và `daily` đều dùng được cờ · **chỉ có nghĩa với chiều GỬI** |

⚠️ Điều kiện còn lại của prompt **vẫn phải kiểm trước khi chạy**: cột `LastDataId` chưa được duyệt (thêm
qua **CodeFirst**, không tệp `.sql`) · đối tác chưa có contract biểu diễn soft-delete · chưa thỏa thuận
`Idempotency-Key` nên chấp nhận **at-least-once** · gói 110 `NotReady`, gói **106** và 111 `Disabled`.

🔴 **Gói 106 đã CHỐT BỎ (20/09)** — nguồn dữ liệu tải trọng **không tồn tại** trong CSDL: bản khai
`ShareDataTable` trỏ vào `TmsTrafficData` (bảng dò xe VDS của gói 103), 4/11 trường cốt lõi không có
cột, 3 trạm cân `WOS01`·`WOS02`·`WOS03` đã khai nhưng 0 dòng dữ liệu, và không bảng nào trong 150
entity chứa tải trọng. Việc phải làm là **gỡ `QueryPacket106`**, không sửa cursor cho nó.

🔴 **Hai việc phải làm TRƯỚC khi bật cờ** — xem mục *Hiện trạng* trong prompt: gói **101** hiện ra
≈ **449.000 dòng** (JOIN `TmsTrafficStatistic` không chọn dòng mới nhất) và gói **105** lấy trọn
`TollTransactionOut` (chưa đo). Phân trang không cứu được hai gói này.

📌 **Phân kỳ (chốt 18/09):** việc liên quan **mã đối tác** hoặc **clone service** là **kỳ cuối** — xem
khối *Phân kỳ* trong [`Sd_MasterPlan_16-09-2026.md`](../Plan/Sd_MasterPlan_16-09-2026.md).

## Đã thực thi và xoá theo quy ước Auto-Cleanup

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

*(Ghi chú: loạt prompt Frontend nhóm A–F từ review 16/09/2026,
`16-09-2026-prompt-flatten-datapublication.md` và `sharedata-bo-qua-khi-khong-co-kenh-prompt.md`
đều đã xoá theo quy tắc Auto-Cleanup.)*
