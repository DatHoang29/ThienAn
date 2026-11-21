# Ảnh mặc định của WPF VDS

Ba ảnh `full.jpeg`, `vehicle.jpeg`, `plate.jpeg` dùng cùng nguyên bản ảnh do người
dùng chọn: https://media-cdn-v2.laodong.vn/Storage/NewsPortal/2023/2/7/1145417/Kham-Xe.jpg
Không cắt riêng biển số hoặc xe. Ba tên tài nguyên tương ứng ba `kind` của contract;
nội dung JPEG giống nhau. WPF không tải lại từ Internet mỗi lần gửi.
Ảnh được nhúng vào assembly WPF nên không cần thư mục nguồn trên máy chạy.

`Simulator:IncludeDefaultImages` mặc định `true`. WPF gắn ba ảnh khi gửi lượt xe,
sự kiện hoặc mẫu vùng chưa có media. Không gắn vào Statistic và
không thay media đã khai. Đặt `false` để thử bản tin thiếu ảnh hoặc đang chờ full.

Chỉ HTTP client tải media của WPF trả ảnh đóng gói cho đường dẫn
`/__wpf-test-images__/{full,vehicle,plate}.jpeg`. Các URL khác giữ cách tải hiện tại.
VdsMediaStorage vẫn ghi ảnh theo thư mục cấu hình và quy ước camera/thời gian/
data hoặc event. Worker và WebAPI không đăng ký handler hoặc injector ảnh mẫu.

`media` là mảng cùng cấp với `metadata` trong bản tin VDS, không phải
`metadata.media`. Pipeline đọc `VdsSseEnvelope.Media`, đưa vào `VdsMediaQueue`,
rồi `VdsMediaStorage` tải ba loại và lưu thành `_f.jpeg`, `_v.jpeg`, `_p.jpeg`.
Ảnh đăng ký Incident lấy bản full đã lưu qua importer hiện tại; không ghi thẳng
ảnh mẫu vào thư mục Incident hoặc đổi rule chọn nguồn ảnh.

Ảnh HTTP tải được không chứng minh kho UNC ghi được. Lỗi Windows 1326 tại
`IncidentImageState/locks` vẫn là lỗi xác thực share, độc lập với nội dung ảnh mẫu.

Ảnh minh họa cố định; biển số và loại xe trong ảnh không đổi theo metadata nhập
trên form hoặc sinh ngẫu nhiên. Không dùng ảnh mẫu để kiểm độ chính xác OCR.
