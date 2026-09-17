---
tier: A
read: full
source: ../../../Plan/_source/audio/16-09-2026-refactor-backend-sharedata-worker.m4a
date: 16-09-2026
duration: ~04 phút 12 giây
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData (Chia sẻ Dữ liệu - ESHARE) - Thống nhất Kiến trúc & Định hướng Refactor Backend Worker (Outbound / Inbound Pipeline)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Đạt (Dev Backend / Web API / Worker)
  - Kiên (Dev Frontend)
---

# Kịch bản & Biên bản Cuộc họp: Định Hướng Tái Cấu Trúc (Refactor) Mã Nguồn Backend ShareData Worker

> **Ghi chú:** Bản ghi được bóc tách trực tiếp bằng phương thức **Gemini Multimodal Native Audio Understanding** từ tệp âm thanh cuộc họp trưa ngày 16/09/2026 (`16-09-2026-refactor-backend-sharedata-worker.m4a`). Cuộc họp diễn ra giữa Anh Sơn (Tech Lead), Đạt (Backend) và Kiên (Frontend) ngay trước giờ nghỉ trưa, nhằm chốt phương án bẻ nhỏ kiến trúc xử lý (decoupling) trong phân hệ **ShareData Worker** (cả luồng Outbound xuất dữ liệu và Inbound nhận dữ liệu).

---

## 1. Tóm tắt tổng quan & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Bối cảnh & Vấn đề mã nguồn hiện tại (Tight Coupling)
- **Tình trạng hiện tại:** Mã nguồn của ShareData Worker đang bị "dính code nhiều quá" (tightly coupled), gom toàn bộ các công đoạn xử lý từ truy vấn CSDL, kiểm tra cấu hình, ánh xạ biến đổi (mapping) đến gửi bản tin HTTP vào chung trong một class lớn.
- **Hệ quả:** Code khó bảo trì, khó viết unit test độc lập. Mỗi khi muốn sửa đổi logic lấy dữ liệu hoặc mapping, lập trình viên buộc phải lội vào một class cồng kềnh với nguy cơ gây lỗi dây chuyền cho toàn bộ luồng gửi.

### 1.2. Quyết định Kiến trúc: Phân rã Pipeline 3 bước chuyên biệt (Separation of Concerns)
Toàn bộ luồng xử lý xuất bản dữ liệu (Data Publication / Outbound) được phân rã rành mạch thành 3 process độc lập theo nguyên lý Đơn nhiệm (Single Responsibility Principle):
1. **Bước 1 — Trích xuất dữ liệu (Data Extraction / Acquisition):**
   - Chịu trách nhiệm duy nhất là kết nối CSDL và đọc dữ liệu thô tương ứng với cấu hình gói tin.
2. **Bước 2 — Xử lý & Ánh xạ dữ liệu (Data Processing & Mapping):**
   - Chịu trách nhiệm biến đổi dữ liệu, áp dụng bộ mã quy đổi (CodeSet), format ngày tháng/số và đóng gói payload JSON chuẩn.
   - **Xử lý lỗi tại nguồn:** Nếu việc xử lý/mapping gặp lỗi (fail), hệ thống ngắt ngay tại bước này, ghi nhận log lỗi chi tiết và **không chuyển dữ liệu sang bước gửi**.
3. **Bước 3 — Xuất bản / Gửi dữ liệu đi (Data Publication / Sender):**
   - Bước này chỉ nhận payload đã được xử lý hoàn thiện từ Bước 2 và thực hiện gửi đi (qua REST API, Socket, NATS...).
   - **Tuyệt đối không can thiệp nghiệp vụ:** Tầng gửi đi chỉ làm đúng chức năng vận chuyển (transport), không cần quan tâm dữ liệu bên trong là gì hay kiểm tra lại logic nghiệp vụ.

### 1.3. Chuẩn hóa Cấu trúc DTO Context (Wrapper Data)
- **Vấn đề:** Trong quá trình xử lý qua nhiều bước, các hàm thường bị thiếu thông tin ngữ cảnh hoặc phải truyền rời rạc nhiều tham số (`packetCode`, `partnerCode`...).
- **Giải pháp thống nhất:** Xây dựng một đối tượng bao bọc dữ liệu chung (**`WrapperData`** hoặc **`PublicationContext`**) mang đầy đủ thông tin định danh xuyên suốt chuỗi pipeline:
  ```json
  {
    "PacketCode": "101",
    "PartnerCode": "PARTNER_A",
    "Data": { ...payload cần truyền... }
  }
  ```
- Nhờ có DTO wrapper này, mọi bước trong pipeline và hệ thống ghi log đều dễ dàng truy xuất thông tin `PacketCode` và `PartnerCode` để theo dõi và quản lý.

### 1.4. Tổ chức thư mục & Class trong Backend Worker
- Trong thư mục chức năng xuất bản (ví dụ `Publication/` hoặc `ShareDataWorker/`), tổ chức thành thư mục con (ví dụ `Processes/` hoặc `Steps/`).
- Mỗi công đoạn là một class riêng biệt thể hiện đúng vai trò. Khi cần tối ưu hoặc sửa đổi cách lấy dữ liệu, chỉ cần mở đúng class lấy dữ liệu để sửa, không ảnh hưởng đến class gửi hay class mapping.

### 1.5. Kế hoạch cho Luồng nhận (Inbound)
- Luồng nhận dữ liệu (Inbound) có độ phức tạp cao hơn rất nhiều (ước tính khối lượng nghiệp vụ gấp đôi luồng gửi Outbound).
- Sau khi thống nhất khung kiến trúc tách bước cho luồng gửi, đội ngũ sẽ tiếp tục họp chi tiết vào buổi chiều để chốt thiết kế cho luồng nhận.

---

## 2. Ma trận phân công công việc & Checklist kỹ thuật (Action Items Checklist)

| STT | Nhiệm vụ kỹ thuật | Người phụ trách | Chi tiết thực hiện |
|---|---|---|---|
| 1 | Bẻ nhỏ Pipeline Outbound thành 3 Process riêng | **Đạt (Backend)** | Tách class worker to thành 3 class độc lập: `DataExtractionProcess`, `DataMappingProcess`, `DataSenderProcess`. |
| 2 | Xây dựng DTO `WrapperData` / Context | **Đạt (Backend)** | Đóng gói `PacketCode`, `PartnerCode` và `Data` vào một model context truyền xuyên suốt các step. |
| 3 | Cô lập lỗi ở tầng Mapping | **Đạt (Backend)** | Nếu quá trình biến đổi dữ liệu thất bại, ghi log lỗi ngay tại Bước 2 và ngắt luồng, không đẩy sang Bước 3 (Sender). |
| 4 | Tối ưu hóa cấu trúc thư mục Worker | **Đạt (Backend)** | Gom các class xử lý vào thư mục con (`Processes/` hoặc `Steps/`), đặt tên tường minh theo đúng vai trò. |
| 5 | Họp thiết kế luồng Nhận (Inbound) | **Anh Sơn & Đạt & Kiên** | Tiếp tục phiên làm việc buổi chiều để bóc tách luồng Inbound (nhận gói tin đối tác $\to$ xử lý mapping $\to$ lưu CSDL). |

---

## 3. Toàn văn nội dung đối thoại (Full Verbatim Transcript)

| Mốc thời gian | Người nói | Lời thoại chi tiết |
|---|---|---|
| `00:00` | **Anh Sơn** | Nó ra vậy đó, từ cái cấu hình đó, với lại cái đầu vào. Nó sẽ có cái mã gói đó để mình biết, mình xử lý cho nó đúng. |
| `00:09` | **Đạt** | Dạ. |
| `00:10` | **Anh Sơn** | Đúng không? Thì cái phần xử lý đó nó sẽ rất là nhiều, nó sẽ là nhiều chỗ. Thì xem xét là tách... có thể là tách từng cái ra những cái nhỏ đó, chứ không có đi chung như vậy. Nó đang hơi bị dính... dính code nhiều quá. |
| `00:23` | **Đạt** | Dạ. |
| `00:24` | **Anh Sơn** | Mục tiêu là cho nó gom gom lại một tí. Nó gom lại một tí thì ví dụ như là đến cái phần liên quan đến gửi đi, thì trong trỏng là nó chỉ có liên quan đến hành động gửi thôi, nó không còn xử lý dữ liệu nữa. Với lại phía trước mà xử lý dữ liệu nó bị fail cái gì đó là nó đã ghi nhận là xử lý dữ liệu fail rồi. Còn vô trong trỏng là nó chỉ care là dữ liệu vào nó là cái gì, là nó lấy dữ liệu đó nó gửi đi thôi. |
| `00:46` | **Đạt** | Dạ. |
| `00:47` | **Anh Sơn** | Chứ nó không cần care nữa là trong trỏng có chính xác hay là phải kiểm tra lại cái gì nữa. Cái này xuyên suốt trong trỏng là chắc chắn là cái thông tin liên quan đến: cái mã gói tin nè, cái đối tác nè... Đúng không? Cái đối tác là bắt buộc là phải có truyền theo đó, nói chung phải truyền theo đó. Thì có thể làm một cái thằng wrapper data cái gì đó ở ngoài, nó sẽ có cái mã gói tin chẳng hạn, cái đối tác. Rồi cái dữ liệu gì đó, dữ liệu truyền gì đó thì nó sẽ nằm ở trong data. |
| `01:18` | **Đạt** | Dạ. |
| `01:19` | **Anh Sơn** | Kiểu như vậy. Thì lúc mà truyền xuyên suốt thì nó sẽ đi... nó sẽ đi theo cái thông tin nó đi theo như vậy thì mình sẽ dễ quản lý hơn. |
| `01:28` | **Đạt** | Dạ ok anh. |
| `01:29` | **Anh Sơn** | Còn hiện tại thì nó đang hơi... |
| `01:31` | **Đạt** | Hơi dính, gom nhiều quá anh. |
| `01:33` | **Anh Sơn** | Nó hơi dính nhiều quá, thì sau này mốt sửa lại cái phần lấy dữ liệu đi, là cũng phải vô trong này để sửa phần đó. Thì lúc đó là mình chia từng hàng nữa, nó xử lý là mình vô đúng chỗ xử lý dữ liệu mình sửa thôi. Còn phía dưới, nguyên cái mấy cái luồng phía dưới mình không có sửa. Nó sẽ đỡ hơn cho mình. Thì có thể chia từng từng từng file, ví dụ như trong cái thằng Publication đi, thì có thể tạo cái thư mục gì đó: Process hay cái gì đó, thì nó sẽ chia từng process. Ví dụ process liên quan đến lấy dữ liệu đi, process liên quan đến xử lý dữ liệu, mapping, process liên quan đến là gửi ra... Thì thằng chính là thằng class này gọi là class kia... |
| `02:18` | **Kiên** | Loose coupling á hả? |
| `02:19` | **Anh Sơn** | Hả? |
| `02:20` | **Kiên** | Loose coupling á? |
| `02:21` | **Đạt** | Đúng rồi, nói chung là cái class đó thể hiện đúng vai trò của nó. Khi mà mình sửa chữa là mình vô nhanh được, còn không là... |
| `02:29` | **Anh Sơn** | Đúng! Ví dụ như là giờ... giờ mình cần thay đổi lại lấy dữ liệu đi, là mình sẽ biết vô đúng cái thằng class lấy dữ liệu thôi, mình sửa thôi. Chứ không có ngồi vô một cái class chà bá rồi mình phải đi tìm. |
| `02:40` | **Đạt** | Giống như cái phần mà ghi log á, hiện tại em đang để ở đây nè, để biết ở đây là có log thôi á. |
| `02:45` | **Anh Sơn** | Đi tìm nó sẽ hơi khó, tại cái luồng cái luồng lớn của mình nó hơi rối. |
| `02:50` | **Kiên** | Cho em ấy đi. |
| `02:51` | **Anh Sơn** | Nó hơi nhiều bước. Thôi đi ăn đi! |
| `02:53` | **Kiên** | Thôi ngồi... thôi ngồi nốt đi! Chiều em còn làm một đống việc nữa! |
| `02:56` | **Anh Sơn** | Người ta đói dưới! |
| `02:57` | **Đạt** | Mới nạp kẹo mà em? |
| `02:58` | **Anh Sơn** | Làm cái gì? |
| `03:00` | **Kiên** | Chiều làm ShareData sửa lại, mai qua VideoWall nè! |
| `03:03` | **Anh Sơn** | Ý là giờ nói cái gì? |
| `03:04` | **Kiên** | Giờ nói cái luồng nhận của em. |
| `03:06` | **Anh Sơn** | Thôi nhiều lắm! |
| `03:07` | **Kiên** | Em làm em cũng thấy nhiều lắm... Má, cái luồng nhận... |
| `03:09` | **Anh Sơn** | Tao thấy ngồi nói cũng phải 20 phút á! |
| `03:11` | **Kiên** | Cái luồng nhận của em chắc nó nhiều gấp đôi cái luồng gửi của anh Đạt luôn á! |
| `03:14` | **Đạt** | Thôi để chiều đi, xong rồi... |
| `03:20` | **Kiên** | Phải tắt cái kia rồi lát mới làm... |
| `03:22` | **Anh Sơn** | Anh có cái đồ bấm... |
| `03:30` | *(Mọi người tắt máy và chuẩn bị đi ăn trưa)* | — |
