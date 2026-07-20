## Brief overview
Hướng dẫn đồng bộ tính năng và sửa lỗi API giữa hai phiên bản Desktop (aicc-fslk) và Mobile (aicc_mobile).

## Quy trình phát triển (Development workflow)
- Khi thực hiện sửa lỗi API hoặc đồng bộ hóa tính năng cho phiên bản di động (`aicc_mobile`), luôn luôn đọc và đối chiếu với mã nguồn của phiên bản desktop (`aicc-fslk`) trước để nắm được cơ chế hoạt động thực tế trên bản web/desktop (ví dụ: cách gửi header, cookie, query params, JWT tokens).
- Đối chiếu các mã `curl` chạy thành công của bản web/desktop để áp dụng chính xác cho bản di động.
- **CHỈ ĐỌC** thư mục `aicc-fslk` để tham chiếu logic, tuyệt đối không chỉnh sửa hoặc thay đổi mã nguồn bên trong thư mục này.

## Cấu trúc và Quy chuẩn gọi API (API Conventions)
- Đảm bảo cơ chế xác thực đồng bộ (như dual-token: Bearer token kết hợp Cookie `auth_token`) được tái lập đúng cách trên môi trường di động (nơi không tự động đính kèm Cookie).
- Cần chú ý cách mã hóa query parameters trên thiết bị di động (tránh việc tự động encode các ký tự đặc biệt như dấu phẩy `,` sang `%2C` nếu backend yêu cầu định dạng thô).