---
name: feedback_test-flow-value
description: "Chỉ viết test đi trọn một luồng nghiệp vụ và có giá trị; cấm unit test helper, test FE so chuỗi mã nguồn; cùng luồng khác dữ liệu dùng [Theory]/[InlineData]"
metadata:
  type: feedback
---

Test chỉ viết khi đi qua điểm vào của một luồng nghiệp vụ và kiểm kết quả nghiệp vụ. Mẫu chuẩn người dùng nêu: `DataOutboundService.ProcessSubscriptions` trong `DataOutboundServiceTests` (một lượt gửi trọn vẹn). API thì qua `CallApiAsync`/`IMessageBus`.
- Không viết unit test gọi thẳng helper/static/validator/handler, kiểm hằng, kiểm DI, test trùng kịch bản.
- Cùng luồng khác dữ liệu → một `[Theory]` + nhiều `[InlineData]`, không nhiều `[Fact]`.
- FE: không viết test đọc mã nguồn `.vue` rồi so chuỗi. Kiểm tĩnh chỉ cho khóa dịch vi/en và `@` chưa thoát.

**Why:** 24/10/2026 người dùng yêu cầu rà bỏ test không giá trị, "tránh over test case". Bộ ShareData lúc đó có 755 ca, nhiều lớp test thẳng helper. Chính tôi vừa viết 7 ca FE chỉ so chuỗi mã nguồn, và tự thêm test khi không được yêu cầu (trái Rule 19.67).
**How to apply:** trước khi viết hay giữ một test, tự hỏi "xóa test này thì lỗi nào lọt qua mà không test nào khác bắt?". Luật chi tiết ở `.agents/rules/testing-rules_thienan.md` §1.4.

