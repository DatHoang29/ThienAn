---
name: feedback-no-async-suffix-naming
description: User's naming convention for async methods in this codebase — do not append "Async" suffix, even though the existing codebase does this pervasively
metadata:
  type: feedback
---

Người dùng có quy tắc riêng: đặt tên method async **không thêm hậu tố "Async"** ở cuối tên (VD
`GetOutputChannels` thay vì `GetOutputChannelsAsync`).

**Why:** Người dùng nói thẳng khi đang xem `VwISAPIDeviceClient.cs` (file có rất nhiều method
`XxxAsync`) — đây là rule cá nhân/nhóm của họ, khác với convention .NET tiêu chuẩn (Microsoft
khuyến nghị suffix `Async`) mà codebase hiện tại đang dùng phổ biến ở khắp mọi module
(VideoWall, TMS, VMS...).

**How to apply:** Khi tự viết code/method MỚI trong plan/code (service, handler, controller, helper, seed method...), BẮT BUỘC đặt tên KHÔNG có suffix `Async` theo đúng quy tắc người dùng (VD `GetScope()`, `GetOutputChannels()`, `SeedWall()`).

**Phạm vi áp dụng (Đã chốt rõ ràng từ người dùng):**
- **Code mới mình viết**: BẮT BUỘC KHÔNG thêm hậu tố `Async`.
- **Code của thư viện hoặc của người khác viết trước đó**: CỨ KỆ, GIỮ NGUYÊN (kể cả họ có đặt `XxxAsync`), không tự ý refactor hay sửa đổi hàng loạt gây diff rác hoặc phá vỡ tương thích.
