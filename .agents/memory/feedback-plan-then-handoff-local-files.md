---
name: feedback-plan-then-handoff-local-files
description: User wants Claude to research/plan only (not implement) for VideoWall work, and wants plan files saved locally in the repo, not in the global ~/.claude/plans directory
metadata:
  type: feedback
---

Khi làm việc trên module VideoWall (và có thể các module lớn khác), người dùng muốn Claude
**chỉ nghiên cứu + lên plan**, không tự thực thi code — họ giao việc code lại cho "bạn khác"
(người khác hoặc 1 AI agent khác) thực hiện, rồi quay lại nhờ Claude review kết quả sau.

**Why:** Người dùng nói rõ nhiều lần trong 1 phiên dài: "hãy lên plan đi rồi tôi sẽ nhờ bạn khác
làm bạn đừng sửa code". Ngay cả sau khi `ExitPlanMode` được approve (hệ thống báo "You can now
start coding"), người dùng vẫn không muốn Claude tự implement — approve chỉ có nghĩa "plan đã
chốt, sẵn sàng giao việc", không phải "Claude cứ code luôn".

**How to apply:**
- Sau khi 1 plan VideoWall được approve qua `ExitPlanMode`, KHÔNG tự động bắt đầu sửa code trừ
  khi người dùng nói rõ ràng "làm đi"/"code đi"/tương tự — mặc định coi như plan xong là bàn giao.
- Nếu lỡ bắt đầu sửa vài file trước khi nhận ra điều này, dừng lại ngay khi được nhắc, và hỏi rõ
  có nên giữ nguyên phần đã sửa dở làm điểm xuất phát hay revert — đừng tự quyết.
- **Plan file phải lưu LOCAL trong repo**, không lưu ở `C:\Users\This PC\.claude\plans\` (global).
  Vị trí đã dùng: `DocBusinessThienAn/HữuNghị-ChiLăng/Plan/<tên-mô-tả>.md` (đặt tên theo nội dung
  plan, không dùng tên file ngẫu nhiên do hệ thống sinh ra). Sau khi ghi bản local, xoá bản global
  đã tạo lúc `ExitPlanMode` (dùng `rm`) để không tồn tại 2 bản.
- Khi người dùng nói "tạo plan"/"tạo prompt" mà không nói thêm, khả năng cao họ chỉ đang nhắc lại
  yêu cầu lưu-local ở trên (đã từng nhầm hiểu là muốn 1 file "prompt" theo format riêng — không
  phải vậy, họ chỉ muốn nói "lưu local" gọn hơn).
- Xem thêm [[videowall-nats-device-access-redesign]] và [[videowall-userarea-permission-model]]
  — cả 2 plan lớn trong phiên 2026-09-11 đều theo đúng quy trình này (research → hỏi kỹ → viết
  plan local → chờ người khác implement → Claude quay lại review).
