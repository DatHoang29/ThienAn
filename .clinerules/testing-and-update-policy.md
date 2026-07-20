## Brief overview
Hướng dẫn kiểm thử và cập nhật test cho mọi bug fix hoặc tính năng mới trong dự án. Quy tắc này bắt buộc áp dụng cho toàn bộ codebase React Native/LiveKit hiện tại.

## Testing requirements
- Bất cứ khi nào sửa bug hoặc thêm mới tính năng mà mã nguồn chưa có test unit/integration thì PHẢI bổ sung test tương ứng cho logic/chức năng vừa sửa.
- Nếu test đã tồn tại nhưng không còn đúng hoặc không phản ánh hành vi thực tế (sai assert/test fail khi so với code mới), PHẢI cập nhật, sửa lại test để phù hợp với hành vi hiện tại.
- Test phải nằm đúng thư mục quy chuẩn (`__tests__/unit/` hoặc `__tests__/integration/`) và đặt tên theo chuẩn module/feature.
- Không được push bugfix/feature lên production nếu chưa bổ sung hoặc cập nhật test.

## Development workflow
- Khi xử lý bất kỳ bug report nào hoặc phát triển tính năng mới, kiểm tra xem đã tồn tại test chưa.
- Nếu chưa có, viết test mới sát logic thực tế theo scope bug hoặc feature đó.
- Luôn ưu tiên gộp test mới vào các file test hiện có liên quan (trong cùng module hoặc feature). Chỉ tạo file test mới nếu logic/module chưa có file test hoặc test của feature đó hoàn toàn độc lập.
- Nếu test có sẵn nhưng fail hoặc assertion đã lỗi thời, sửa lại test cho đúng với hành vi mong đợi của code hiện tại.
- Test phải đặt tên rõ ràng, tuân thủ chuẩn đặt tên test theo module/feature, dễ tìm lại.
- Luôn chạy lại toàn bộ test sau khi sửa bug hoặc phát triển tính năng.

## Coding best practices
- Ưu tiên viết test granularity theo từng unit logic hoặc hành vi UI đủ nhỏ giúp phát hiện regression sớm.
- Đảm bảo test dễ đọc, đúng chuẩn, không over-mock, không test implementation detail.
- Khi cập nhật test, ghi chú ngắn gọn lý do sửa đổi (bằng tiếng Việt, technical, tối giản).

## Enforcement policy
- Mọi PR, commit merge lên main/master bắt buộc phải kèm test hợp lệ cho từng bugfix/feature.
- Reviewer và CI có quyền từ chối hoặc block nếu thiếu test hoặc test fail.
- Quy tắc này áp dụng song song cho code backend lẫn frontend, ưu tiên cho các path code dễ gây side effect hoặc đụng chạm tới flows realtime (WebRTC/Call/Room).