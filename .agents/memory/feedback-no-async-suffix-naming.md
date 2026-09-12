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

**How to apply:** Khi tự đề xuất method MỚI trong plan/code (VD các `BackgroundService`/handler
mới viết cho VideoWall), đặt tên KHÔNG có suffix `Async` theo đúng ý người dùng — dù các method
CŨ xung quanh (viết trước, không phải do mình đề xuất) vẫn giữ nguyên `Async` như hiện trạng.
**Chưa rõ phạm vi**: không chắc người dùng muốn áp dụng cho toàn bộ method cũ đã có sẵn (đổi tên
hàng loạt, phạm vi rất lớn, ảnh hưởng mọi module) hay chỉ cho code MỚI mình viết thêm — cần hỏi
lại nếu ngữ cảnh chưa rõ, đừng tự suy diễn thành 1 đợt refactor lớn nếu họ chỉ đang nói về code
mới.
