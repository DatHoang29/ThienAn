---
name: feedback-verify-root-cause-before-fix
description: When user flags an architecture smell (e.g. wrong dependency direction), investigate the actual root cause and full real scope in code before proposing a fix — surface patches get pushed back on
metadata:
  type: feedback
---

Người dùng có kỹ năng đọc kiến trúc tốt và sẽ chủ động chỉ ra vấn đề bằng 1 câu hỏi ngắn (ví dụ:
"tại sao module lại ProjectReference là service") thay vì yêu cầu trực tiếp — kỳ vọng Claude tự
đào sâu tìm nguyên nhân THẬT và phạm vi ẢNH HƯỞNG THẬT trước khi đề xuất cách sửa, không chỉ vá
bề mặt.

**Why:** Trong phiên 2026-09-11, Claude đề xuất fix đầu tiên cho `Module.VideoWall.csproj` có
`ProjectReference` ngược tới `ITS.VideoWall` là "chuyển 7 file ISAPIDevice vào Core để dùng
chung" — nghe hợp lý nhưng KHÔNG trả lời đúng câu hỏi gốc của người dùng ("giao tiếp thiết bị là
việc của Service, sao WebAPI lại cần biết"). Khi đọc sâu hơn (`VwDeviceSetupCommandHandler`,
`VwDeviceHandlerRunner`, `VwDeviceController`), lộ ra quy mô thật lớn hơn nhiều lần ước tính ban
đầu (23 endpoint diagnostic, không phải 5 thao tác wizard) và 1 phát hiện quan trọng bị bỏ sót
(nhóm diagnostic là công cụ chẩn đoán hiện trường CÓ CHỦ Ý gọi thẳng, ghi rõ trong docstring) —
nếu chốt fix đầu tiên ngay thì đã sai cả về quy mô lẫn bản chất.

**How to apply:**
- Khi phát hiện code "trông sai" (dependency ngược, gọi tắt qua tầng trung gian, v.v.), đọc TOÀN
  BỘ các nơi liên quan (không chỉ 1-2 file đại diện) trước khi chốt hướng sửa — đặc biệt đọc
  docstring/comment gốc, vì nhiều "vi phạm nguyên tắc" hoá ra là ngoại lệ có chủ đích đã ghi rõ lý
  do (xem thêm bài học tương tự ở [[videowall-nats-device-access-redesign]] — nhóm 18 endpoint
  `VwDeviceController` tưởng là lỗi kiến trúc nhưng thực ra là công cụ debug hiện trường cố ý).
- Khi báo lại cho người dùng, chủ động nói rõ nếu ước tính quy mô ban đầu sai lệch (ví dụ "tôi
  nói ~5 thao tác nhưng thực ra là ~23") — đừng âm thầm mở rộng plan mà không xác nhận lại, người
  dùng có quyền chọn lại phạm vi khi biết chi phí thật.
- Sau khi người dùng xác nhận hướng đi (dù đã đổi nhiều lần trong 1 phiên), chốt lại plan theo
  đúng quyết định cuối cùng — không tự ý quay lại phương án cũ.
