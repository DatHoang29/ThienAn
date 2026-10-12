---
name: Ưu tiên transcript hơn MasterPlan khi đối chiếu nghiệp vụ
description: Khi kiểm tra hành vi code/test so với tài liệu, transcript họp là nguồn đáng tin hơn MasterPlan
type: feedback
---

Khi đối chiếu nghiệp vụ (ví dụ phân hệ ShareData), KHÔNG dùng `Plan/*_MasterPlan.md` làm căn cứ chính vì có thể sai.
Ưu tiên đọc `doc/transcript/` (nội dung trao đổi trước khi làm). MasterPlan chỉ tham khảo thêm.

**Why:** User xác nhận MasterPlan có thể sai so với thực tế bàn bạc; transcript là nguồn gốc yêu cầu.
**How to apply:** Trích dẫn transcript (file + giờ phát biểu) khi kết luận code đúng/sai tài liệu; nếu transcript không nói tới thì ghi rõ "transcript không đề cập" thay vì lấy MasterPlan làm bằng chứng.
