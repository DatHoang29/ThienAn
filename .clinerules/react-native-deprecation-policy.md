## Brief overview
Hướng dẫn xử lý các React Native API deprecated warnings trong dự án aicc_mobile. Quy tắc này áp dụng cho các cảnh báo deprecation đến từ React Native core hoặc dependencies bên thứ ba.

## Nguyên tắc chung
- **KHÔNG tự ý refactor** các API deprecated khi chúng đến từ React Native core hoặc dependencies mà không có instructions cụ thể từ user.
- Kiểm tra xem source code của dự án có sử dụng trực tiếp API đó không trước khi thực hiện bất kỳ thay đổi nào.
- Các cảnh báo deprecation từ React Native (như SafeAreaView, InteractionManager) là do version React Native và sẽ được fix trong các bản cập nhật tiếp theo.

## Quy trình xử lý deprecation warnings

### Bước 1: Xác định nguồn gốc
- Tìm kiếm trong thư mục `src/` của dự án để xác định xem có đang sử dụng trực tiếp API deprecated không.
- Nếu không tìm thấy trong source code → Đây là deprecation từ React Native hoặc dependencies.

### Bước 2: Áp dụng đúng phương án

**Khi user YÊU CẦU explicitly:**
- SafeAreaView deprecated → Chuyển sang `react-native-safe-area-context`
- InteractionManager deprecated → Refactor thành `requestIdleCallback` hoặc chia nhỏ tasks

**Khi user KHÔNG yêu cầu:**
- Không tự ý upgrade các dependencies để tránh breaking changes
- Ghi nhận lại warning để theo dõi

### Trigger cases
- Cảnh báo: "SafeAreaView has been deprecated" → Kiểm tra xem có dùng trong src/ không
- Cảnh báo: "InteractionManager has been deprecated" → Kiểm tra xem có dùng trong src/ không

## Ví dụ tìm kiếm
```bash
# Tìm SafeAreaView trong source code
search_files(path="aicc_mobile/src", regex="SafeAreaView", file_pattern="*.tsx")

# Tìm InteractionManager trong source code  
search_files(path="aicc_mobile/src", regex="InteractionManager", file_pattern="*.tsx")