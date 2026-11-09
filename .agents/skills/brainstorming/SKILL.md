---
name: brainstorming
description: Làm rõ yêu cầu và chọn phương án khi yêu cầu mơ hồ hoặc cần quyết định thiết kế. Đưa 2-3 phương án kèm đánh đổi và đề xuất, chốt xong ghi vào mục ADR của MasterPlan phân hệ.
---

# Làm rõ yêu cầu, chọn phương án

## Khi nào hỏi
- Chỉ hỏi khi yêu cầu mơ hồ hoặc thiếu thông tin quyết định thiết kế ([core-protocol.md](../../rules/core-protocol.md) §6). Rõ thì làm ngay.
- Trước khi hỏi, đọc memory, transcript và MasterPlan của phân hệ. Điều đã chốt thì không hỏi lại, không làm ngược.
- Mỗi câu hỏi phải loại được một hướng làm, kèm cách mặc định sẽ theo nếu người dùng không chọn.

## Đưa phương án
1. Nêu vấn đề và ràng buộc: luồng nghiệp vụ, dữ liệu, tải, rule P0 liên quan, kiến trúc sẵn có (Wolverine CQRS, SqlSugar CodeFirst, `getAPI`).
2. Đưa 2-3 phương án:

| Phương án | Được | Mất | Độ phức tạp | Hợp khi |
|---|---|---|---|---|

3. Đề xuất một phương án, lý do gắn với ràng buộc. Chọn cách đơn giản nhất chạy được; chỉ thêm phức tạp khi đã thấy cần, vì thêm thì dễ, gỡ thì khó.
4. Chờ người dùng chốt rồi mới làm.

## Ghi quyết định
- Ghi vào mục quyết định kiến trúc (ADR) của `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Plan/<PhânHệ>_MasterPlan.md` (Rule 19.52). Không tạo `docs/architecture/` hay tệp ADR rời.
- Mỗi quyết định gồm: bối cảnh, quyết định, lý do, đánh đổi chấp nhận, phương án bị bác, khi nào xét lại.
- Căn cứ nghiệp vụ lấy transcript trước MasterPlan; transcript không nói tới thì ghi rõ.
- Chốt xong mà việc có nhiều bước thì lập prompt theo skill `plan-writing`.
