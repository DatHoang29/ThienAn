---
name: tdd
description: Cách viết test của dự án theo vòng red-green, gồm xUnit đi qua điểm vào luồng thật trên CSDL local và Playwright E2E, không mock. Dùng khi làm tính năng, sửa lỗi, hoặc viết, sửa, rà test.
---

# TDD theo luồng nghiệp vụ

Luật gốc: [testing-rules_thienan.md](../../rules/testing-rules_thienan.md) (§1 triết lý, §2 BE, §3 FE, §5 tệp test, §6 phạm vi chạy). Rule thắng khi trùng.

## Vòng red-green
1. Chọn điểm vào của luồng (§1.4, §5.3): API qua `IMessageBus.InvokeAsync(input)` như controller; Worker qua hàm public Worker gọi (`DataOutboundService.ProcessSubscriptions`); FE qua màn hình thật.
2. Viết một test kiểm kết quả nghiệp vụ (dữ liệu CSDL, gói tin gửi đi, lỗi trả về, điều người dùng thấy), chạy thấy đỏ đúng lý do. Sửa lỗi thì bắt buộc đỏ trước (Rule 19.59).
3. Viết mã tối thiểu cho xanh. Mỗi vòng một test; không viết hết test rồi mới làm.
4. Chạy theo phạm vi đã sửa; trước commit chạy cả bộ phân hệ một lần (§6).

Sửa nhanh giao diện trong lúc review thì sửa trước, nhưng trước khi chốt phải có test (Rule 19.67, 19.109).

## Backend (`tests/BE/`)
- Thêm case vào tệp test sẵn có của lớp hoặc luồng; chỉ tạo tệp khi có lớp mới (Rule 19.84).
- SQL Server local, không `--no-build`. Không Moq, NSubstitute, fake service, fake NATS; lấy bản thật từ `host.Services`. Chỉ giả máy chủ HTTP đối tác bằng `HttpListener` (`MockHttpClientFactoryTest`).
- AAA phẳng, không `try-catch` quanh Act, Assert. Cô lập dữ liệu bằng mã riêng, không xóa bảng trong test.
- Cùng luồng khác dữ liệu: một `[Theory]` với nhiều `[InlineData]`.
- Dữ liệu tiền đề và giá trị mong đợi lấy từ nguồn production dùng (seed, script, lệnh Add qua bus).
- Validator làm đỏ test cũ: sửa dữ liệu test theo chuẩn mới, không nới validator (Rule 19.113).

## Frontend (`tests/FE/views/<module>/`)
- Bốn bước: `page.route` giả API, `page.goto`, thao tác, kiểm điều người dùng thấy hoặc payload gửi đi.
- Mỗi trang có smoke (Rule 20.31): mở URL, khối chính nạp dữ liệu, bấm dòng đầu mở chi tiết, nghe `page.on('pageerror')`, không còn `@` trần trong `vi-vn.json`, `en-us.json`.
- Cùng luồng khác dữ liệu: một mảng và vòng `for`.
- Chạy `pnpm test:e2e` trong `tests/FE/`; không build FE. Xanh thì xóa `test-results/` (Rule 19.64).

## Cổng trước khi thêm test
Thiếu câu trả lời nào thì chưa viết:
1. Test bảo vệ hành vi hay hợp đồng nào thấy được từ ngoài?
2. Lỗi hồi quy cụ thể nào làm nó đỏ?
3. Vì sao test sẵn có chưa bắt được? Mỗi hợp đồng một test chủ ở biên mạnh nhất; thêm `[InlineData]` hơn là viết test gần trùng.
4. Có phải mở thêm cửa trong mã thật (export, cờ, interface, hàm `Reset*`) chỉ để test không? Có thì chuyển test về biên thật.

## Mẫu test rác
- Không assert, chỉ kiểm "không lỗi", "khác null", hoặc trùng kịch bản test khác.
- Gọi thẳng helper, static, validator, handler; kiểm hằng, enum, DI.
- Giá trị mong đợi tính lại theo cách mã tính, hoặc danh sách chép tay.
- Đọc mã `.vue`, `.ts` rồi so chuỗi (trừ khóa dịch và `@` chưa thoát).
- Đồ giả tự làm hành vi đang kiểm, hoặc tự đưa kết quả lẽ ra luồng phải sinh.
- Test từ chối đỏ vì một bộ chặn khác bộ đang kiểm.
- Tên test hứa nhiều hơn điều assert kiểm.
- Test cho thay đổi chỉ là cấu trúc CSDL (index, độ dài cột).
