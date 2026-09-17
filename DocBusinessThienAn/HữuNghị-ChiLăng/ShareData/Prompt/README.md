# ShareData — Prompt

Thư mục này chỉ chứa **prompt thực thi từng bước, dùng 1 lần** cho phân hệ ShareData
(`<task-slug>-prompt.md`) — mỗi task/fix riêng = đúng 1 file. Sau khi thực thi xong, file prompt bị
**xoá tự động** theo quy ước Auto-Cleanup (`thienan_rules.md` mục 13/19.2).

Tài liệu SỐNG (plan tổng thể, review tiến độ — không bị xoá) nằm ở [`../Plan/`](../Plan/), không
đặt ở đây.

## Đang chờ thực thi

Loạt prompt Frontend chốt từ buổi review 16/09/2026 — làm theo đúng thứ tự dưới, mỗi nhánh 1 prompt.
Plan tổng thể: [`../Plan/Sd_MasterPlan_16-09-2026.md`](../Plan/Sd_MasterPlan_16-09-2026.md).

| Thứ tự | Prompt | Ghi chú |
|---|---|---|
| 1 | [Nhóm A — Đối tác & Đăng ký](fe-sharedata-nhom-a-doi-tac-dang-ky-prompt.md) | Rủi ro thấp, Tester thấy ngay |
| 2 | [Nhóm D — Lịch sử](fe-sharedata-nhom-d-lich-su-prompt.md) | Có 1 bug thật (enum chuỗi/số) |
| 3 | [Nhóm B — Gói tin](fe-sharedata-nhom-b-goi-tin-prompt.md) | Phải chốt phương án P1/P2 trước |
| 4 | [Nhóm C — Ánh xạ & Bộ mã](fe-sharedata-nhom-c-anh-xa-prompt.md) | Rủi ro cao — đụng engine sinh `targetShapeJson` |
| 5 | [Nhóm E+F — Tooltip & Danh mục SQL](fe-sharedata-nhom-ef-tooltip-danh-muc-prompt.md) | Làm sau cùng, sửa hook dùng chung toàn app |

*(Ghi chú: Prompt `16-09-2026-prompt-flatten-datapublication.md` cho backend Worker đã thực thi hoàn tất và tự động xoá theo quy tắc Auto-Cleanup).*
