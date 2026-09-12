---
tier: A
read: full
source:
  - DocBusinessThienAn/MakeUp Chi Ngô Gò Vấp 5.m4a (10:37)
date: 2026-09-09
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: Thảo luận VideoWall (Phân quyền Khu vực Màn hình Ma trận, Ghép nối Service qua NATS, và Tối ưu SqlSugar ToTree cho Zone)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Backend / Service Worker / VideoWall)
  - Đạt (Dev Backend / Web API / Database)
---

# Kịch bản & Biên bản Cuộc họp: VideoWall — Phân Quyền Khu Vực Màn Hình, Ghép Nối NATS & Cây Phân Cấp Zone
### (Nguồn từ: MakeUp Chi Ngô Gò Vấp 5)

- **Thời lượng:** 10 phút 37 giây
- **Người tham gia:** Anh Sơn, Hiếu, Đạt
- **Chủ đề chính:** Đây chính là buổi họp **trọng tâm và chuyên sâu về VideoWall** trong chuỗi họp ngày 09/09/2026. Thảo luận về: Ghép nối VideoWall Service với Web qua NATS, chuẩn hóa cấu trúc Message NATS, cơ chế phân quyền theo khu vực hiển thị (ma trận lưới màn hình), lưu phân quyền theo User/Org và dựng cây danh mục Zone bằng SqlSugar `ToTree()`.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Ghép nối VideoWall Service với Web qua NATS (Bỏ Mock Data)
- **Hiện trạng:** Đợt demo trước hệ thống dùng mock service ở local để kiểm thử giao diện.
- **Quyết định:**
  - Ghép nối chính thức giữa Backend WebAPI và VideoWall Background Service thông qua **NATS Message Broker**.
  - Chuẩn hóa cấu trúc Message trên NATS: phân định rõ kênh gửi lệnh điều khiển (`videowall.command.*`) và kênh nhận trạng thái (`videowall.status.*`).
  - Gói tin trao đổi phải có Header/Metadata quy chuẩn (chứa ID controller, mã dịch vụ) để các service nhận diện đúng gói tin của mình, tránh xử lý nhầm khi có nhiều service cùng kết nối chung một topic.

### 1.2. Phân quyền Hiển thị theo Khu vực Màn hình (Display Area Bounding / Matrix Layout)
- **Nguyên tắc:** Không phân quyền trên thiết bị phần cứng controller; phân quyền hoàn toàn được quản lý và kiểm soát ở tầng phần mềm.
- **Mô hình Ma trận Lưới Màn hình:**
  - Tường màn hình lớn (VideoWall) được chia thành lưới tọa độ theo hàng ngang và cột dọc (ví dụ ma trận 8x5 hoặc 8x4).
  - Từng ô màn hình được đánh chỉ số tọa độ: `1-1, 1-2, 1-3, 1-4... 2-1, 2-2...`
  - Người dùng hoặc nhóm người dùng được rào phạm vi phân quyền theo một tập hợp các ô màn hình cụ thể (ví dụ User A chỉ được thao tác trong vùng ô `[1-1 -> 2-3]`).
  - Giao diện kéo thả và API sẽ tự động chặn nếu người dùng cố tình kéo camera hoặc bật scene ra ngoài phạm vi ô màn hình được cấp quyền.

### 1.3. Cấu trúc Bảng CSDL Phân quyền (User vs Organization Priority)
- Bảng phân quyền lưu trữ thông tin:
  - Cột `UserId`: Định danh người dùng cụ thể.
  - Cột `OrgId`: Định danh tổ chức / phòng ban.
  - Cột `DisplayRegion` / `ScreenMatrix`: Tọa độ hoặc danh sách các ô màn hình được phép điều khiển.
- **Quy tắc ưu tiên (Priority Rule):**
  1. Phân quyền theo `UserId` có **độ ưu tiên cao nhất**, ghi đè (override) cấu hình của `OrgId`.
  2. Nếu không cấu hình riêng theo `UserId`, người dùng kế thừa phân quyền từ `OrgId` của họ.
  3. Nếu không có bất kỳ cấu hình nào cho cả User và Org, hệ thống mặc định cấp **Full quyền** (điều khiển toàn bộ tường màn hình).

### 1.4. Tối ưu dựng cây danh mục Zone bằng SqlSugar `ToTree()`
- Đối với danh mục phân cấp trạm / vị trí (`Zone`), bắt buộc dùng phương thức `ToTree()` có sẵn của thư viện **SqlSugar** (`db.Queryable<Zone>().ToTree(...)`).
- Tuyệt đối không tự viết hàm đệ quy thủ công (recursive loop) vì vừa chậm, vừa tiềm ẩn rủi ro lặp vô tận (infinite loop) gây nghẽn CPU server.

---

## 2. Ma trận phân công công việc (Action Items)

| Người phụ trách | Hạng mục công việc | Chi tiết kỹ thuật |
|---|---|---|
| **Hiếu** | VideoWall Service & NATS | - Bỏ mock service, ghép nối VideoWall Background Service thật vào NATS.<br>- Đóng gói message NATS theo cấu trúc chuẩn (bổ sung metadata controller/service).<br>- Tích hợp luồng lệnh điều khiển thiết bị ISAPI từ NATS. |
| **Đạt** | Backend WebAPI & DB | - Thiết kế bảng CSDL phân quyền theo `UserId` và `OrgId` gắn với tọa độ ma trận màn hình.<br>- Viết API kiểm tra phân quyền khu vực khi người dùng thực hiện thao tác kéo thả/chuyển scene.<br>- Dùng SqlSugar `ToTree()` cho API lấy danh sách Zone phân cấp. |
| **Kiên** | Frontend UI/UX | - Móc giao diện VideoWall với API Backend mới qua NATS.<br>- Hiển thị bố cục ma trận lưới, thể hiện trực quan vùng rào quyền của người dùng (vùng bị khóa / vùng được phép kéo thả). |

---

## 3. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

#### [00:00 - 02:30] Rà soát cấu trúc gán Input - Output & Module Sự cố
- **Hiếu:** Hôm bữa có bữa anh nói là bên thằng Incident (sự cố) cũng phải làm theo cấu trúc đúng không anh?
- **Anh Sơn:** Ừ, Incident đó, trong trỏng có cái gán input vô output... cấu hình gói tin.
- **Hiếu:** Vậy là cái thằng Incident của em cũng phải làm theo cấu trúc chuẩn đó đúng không? Chứ như bữa gửi là bị chửi thấy mẹ luôn.
- **Anh Sơn:** Thì phải có cấu trúc chứ! Gửi để sau này bên Frontend hoặc các service khác họ còn biết cái gói đó là gói nào, do service nào xử lý. Chứ nhiều service cùng gửi chung một topic mà không có định danh thì làm sao biết gói tin của ai?

#### [02:30 - 05:30] Ghép Service với Web qua NATS & Quy chuẩn Message
- **Anh Sơn:** Bây giờ công việc của VideoWall cần làm những gì, rà soát lại:
  1. Ghép lại Background Service với lại Web thông qua NATS. Format message NATS theo đúng cấu trúc quy chuẩn (kênh điều khiển, kênh trạng thái).
  2. Bổ sung cái gì? Thêm trường cấu hình để biết được controller nào đang gọi API.
  3. Bỏ mock service đi. Hôm nọ demo là dùng mock service chạy local để test form, bây giờ phải ghép với service thật để chạy.
- **Hiếu:** Dạ, hôm nọ em có chạy thử mock để test giao diện với Đạt, giờ chuyển sang service thật.
- **Anh Sơn:** Ghép lại với service thật và kiểm tra kỹ lại cái luồng.

#### [05:30 - 08:30] Phân quyền Khu vực Màn hình theo Ma trận Lưới (Matrix Layout)
- **Hiếu:** Còn cái vụ phân quyền màn hình hôm nọ anh nói á, là phân quyền theo cái gì anh?
- **Anh Sơn:** **Phân quyền theo khu vực hiển thị của màn hình!**
- **Hiếu:** Tức là chia ma trận hả anh?
- **Anh Sơn:** Đúng! Em chia cái tường màn hình lớn ra thành các hàng ngang và cột dọc. Đánh số tọa độ cho nó: `1-1, 1-2, 1-3, 1-4, 2-1, 2-2, 2-3, 2-4...` Đánh ma trận từng màn hình cụ thể luôn!
- **Hiếu:** Đánh ma trận từng màn hình luôn hả anh?
- **Anh Sơn:** Ừ! Rồi sau đó ví dụ User A vô, thì em phân cho User A được quyền trên những ô nào trong ma trận đó (ví dụ ô 1-1 đến 2-3). Khi người ta kéo thả nguồn tín hiệu hoặc chuyển kịch bản, phần mềm chỉ cho phép thao tác trong vùng ô đó thôi.
- **Hiếu:** Lưu phân quyền này theo người dùng hay theo tổ chức anh?
- **Anh Sơn:** Tạo bảng lưu có cả 2 cột: `UserId` và `OrgId`.
  - Nếu lưu theo tổ chức thì điền `OrgId`.
  - Nếu lưu theo người dùng cụ thể thì điền `UserId`.
  - **Quy tắc:** Ưu tiên phân quyền của `UserId` hơn là `OrgId`. Nếu User có cấu hình riêng thì ăn theo User, nếu không có thì kế thừa quyền của Org. Còn nếu cả hai đều không cấu hình thì mặc định là **Full quyền**!
- **Anh Sơn:** Hôm bữa đi demo là mình làm tạm, gán cứng trong controller để kịp demo. Bây giờ có thời gian thì phải làm bảng phân quyền này trong CSDL cho nó chuẩn chỉnh!

#### [08:30 - 10:37] SqlSugar ToTree cho danh mục Zone & Kết thúc
- **Hiếu:** Em hiểu rồi. Còn cái chỗ danh mục Zone vị trí thì sao anh?
- **Anh Sơn:** Trong CSDL bảng `Zone` á, em dùng cái hàm `ToTree()` của SqlSugar (`ToTreeAsync`) nha!
- **Hiếu:** Dạ, SqlSugar nó có sẵn hàm `ToTree` rồi.
- **Anh Sơn:** Xài cái hàm đó cho nó nhanh, viết một dòng là nó tự dựng cây cha - con. Đừng có ngồi tự viết hàm đệ quy tay nha, đệ quy tay vừa chậm vừa dễ bị lặp vô tận (infinite loop) đó!
- **Hiếu:** Dạ hôm nọ em làm cái menu Zone đó là dùng `ToTree` của SqlSugar rồi anh, chạy mượt lắm.
- **Anh Sơn:** Rồi, vậy là nắm rõ các đầu việc của VideoWall rồi đó:
  1. Ghép Service qua NATS.
  2. Bảng phân quyền màn hình theo ma trận tọa độ (User > Org).
  3. Dùng SqlSugar `ToTree()` cho Zone.
  Tập trung làm cho xong trong tuần này nhé.
- **Hiếu / Đạt:** Dạ ok anh!\n