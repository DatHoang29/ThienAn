---
tier: A
read: full
source:
  - ../_source/Đường Số 10.m4a (42:00)
date: 12-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData & Chuẩn hóa Component Lập Lịch (Schedule) dùng chung toàn hệ thống (Bóc tách tầng tích hợp kết nối API/Kafka/File, Cấu hình Type trong SystemConfig/SystemConfigData, Truy vấn động cho SuperAdmin có Audit Log, Rà soát i18n Anh-Việt)
participants:
  - Anh Sơn (Tech Lead)
  - Hiếu (Dev)
  - Đạt (Dev)
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ ShareData & Component Lập Lịch Dùng Chung
### (Nguồn từ tệp ghi âm: Đường Số 10.m4a — Phần trích đoạn ShareData & Scheduler)

- **Ngày ghi nhận:** 12/10/2026
- **Thời lượng phần ShareData & Scheduler:** ~16 phút (trích xuất từ tệp 42:00 tại các mốc `19:35 - 25:35` và `31:24 - 42:00`; các đoạn còn lại thuộc phân hệ VideoWall)
- **Người tham gia:** Anh Sơn, Hiếu, Đạt
- **Chủ đề chính:**
  1. **Chuẩn hóa Component Lập Lịch (Scheduler) dùng chung:** Chấm dứt tình trạng phân mảnh (mỗi phân hệ Bảo trì, Email, ShareData, VideoWall tự code một kiểu); gom thành 1 component duy nhất dùng `RadioButton` (`group button`) có props bật/tắt các mode (liên tục, hàng ngày, hàng tuần, định kỳ).
  2. **Chuẩn hóa Cấu trúc Tab Phân Cấp:** Phân biệt rõ Tab cấp trên (theo chuẩn TMS quản lý) và Tab gạch chân (Sub-tab); yêu cầu vẽ Wireframe trước khi code.
  3. **Bóc tách tầng xử lý kết nối ShareData (`HandleConnection`):** Tách biệt phần xử lý dữ liệu (đầu ra thống nhất là `JSON`) và service tích hợp xử lý kết nối (`API`, `Kafka`, `File`...); bên trong tự convert `JSON` sang `byte[]` khi đẩy qua channel `Kafka`.
  4. **Khai báo loại kết nối (`Type`) và cấu hình trong `SystemConfig` & `SystemConfigData`:** Đầu vào tầng kết nối gồm 3 phần (Data JSON, Thông tin bảo mật, Cấu hình kết nối dạng `Dictionary` Key-Value như IP, Port, Username). Danh mục `Type` khai báo trong `SystemConfig` / `SystemConfigData` để không fix cứng trên UI.
  5. **Truy vấn động (Dynamic Query) load danh sách cột & Bảo mật:** Chức năng sửa câu query động trên màn hình chỉ mở cho tài khoản `SuperAdmin`, bắt buộc có code check/validate câu lệnh và ghi Audit Log người sửa.
  6. **Chuẩn hóa Đa ngôn ngữ (i18n):** Rà soát lỗi lệch ngôn ngữ Anh - Việt và thiếu rule `empty` giữa các màn hình.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Thống nhất xây dựng 1 Component Lập Lịch (Common Scheduler) duy nhất
- **Thực trạng phân mảnh:** 
  - Phân hệ Gửi Email (4 màn hình/component), phân hệ ShareData, phân hệ Bảo trì, phân hệ VideoWall đều có chức năng lập lịch.
  - Hiện trạng mỗi nơi tự viết giao diện và logic một kiểu: chỗ dùng nút switch, chỗ dùng checkbox, dẫn tới thiếu đồng nhất.
- **Quyết định kỹ thuật:**
  - **Gom về 1 Component Lập Lịch duy nhất:** Chuẩn hóa giao diện chọn chế độ bằng `RadioButton` (`group button`), áp dụng chung cho toàn bộ các phân hệ có hẹn giờ/lập lịch.
  - **Thiết kế linh hoạt qua props:** Component hỗ trợ đầy đủ các chế độ:
    - Chạy liên tục (Continuous mode).
    - Chạy lặp hàng ngày (Daily).
    - Chạy lặp hàng tuần (Weekly).
    - Chạy định kỳ (Interval).
  - Cứ xây dựng bản đầy đủ (full option); phân hệ nào không có chế độ nào (ví dụ không có chạy liên tục) thì truyền prop/option (`continuous = false` hoặc `type` tương ứng) để ẩn chế độ đó đi.

### 1.2. Bóc tách Tầng xử lý dữ liệu và Tầng tích hợp kết nối (`HandleConnection`)
- **Kiến trúc phân tầng ShareData:**
  - **Tầng xử lý dữ liệu (Data Processor):** Đọc dữ liệu từ DB, xử lý ánh xạ (Mapping), quy đổi mã và trả ra đầu ra thống nhất là **JSON**.
  - **Tầng tích hợp xử lý kết nối (`HandleConnection` Service/Module riêng trong code):**
    - Tách khỏi luồng xử lý dữ liệu để khi thêm/sửa giao thức kết nối chỉ sửa bên trong module này, không ảnh hưởng phần xử lý data ở ngoài.
    - **3 tham số đầu vào chuẩn:**
      1. **Cục data sau khi đã xử lý** (thống nhất đầu vào là `JSON`).
      2. **Thông tin bảo mật** (Security / Authentication).
      3. **Cấu hình kết nối** (IP, Port, Username... tùy mỗi đối tác khác nhau, có thể truyền dạng **`Dictionary` Key-Value**).
    - **Chuyển đổi định dạng theo `Type` bên trong tầng kết nối:** Đầu vào nhận vào là `JSON`, nhưng bên trong `HandleConnection` sẽ dựa vào `Type` để convert sang định dạng tương thích với kênh truyền của đối tác:
      - Nếu `Type` là **`API` (WebAPI REST):** gửi qua HTTP API.
      - Nếu `Type` là **`Kafka`:** bên trong tự chuyển `JSON` thành mảng **`byte[]`** rồi đẩy vào kênh (channel/topic) của đối tác, tầng xử lý bên ngoài không cần quan tâm.
      - Nếu xuất **`File`:** ghi ra file theo cấu hình.
- **Quản lý danh mục `Type` kết nối trong `SystemConfig` & `SystemConfigData`:**
  - Không tách màn hình giao diện quản lý riêng cho `Type`, mà quản lý trên màn hình cấu hình đối tác hiện có.
  - Để danh sách lựa chọn `Type` kết nối không bị fix cứng trên UI, khai báo danh mục và thông tin cấu hình đi kèm trong **`SystemConfig` và `SystemConfigData`**, kết hợp một hàm service ở backend để xử lý thông tin cấu hình mở rộng; phần phương thức gửi thực thi ứng với từng `Type` thì cài đặt trong code backend.

### 1.3. Quản lý Truy vấn động (Dynamic Query) & Audit Log
- **Bối cảnh nghiệp vụ:** Câu truy vấn (Query) dùng để lấy dữ liệu từ CSDL và load lên danh sách các cột cần được hỗ trợ chỉnh sửa động qua màn hình quản lý thay vì chỉ để cứng.
- **3 chốt chặn an toàn bắt buộc:**
  1. **Phân quyền:** Chức năng quản lý/chỉnh sửa câu query động chỉ mở cho tài khoản **SuperAdmin** truy cập.
  2. **Validate / Check code câu truy vấn:** Dù là SuperAdmin cũng có thể gõ nhầm hoặc viết câu lệnh làm gãy hệ thống, nên bắt buộc phải viết code kiểm tra/validate câu query chặt chẽ trước khi lưu hoặc thực thi.
  3. **Ghi nhận nhật ký (Audit Log):** Bắt buộc ghi nhận lại tài khoản nào (ví dụ `Hiếu admin`) đã vào sửa câu query gì, vào thời điểm nào để truy vết trách nhiệm khi xảy ra lỗi.

### 1.4. Rà soát chuẩn hóa Đa ngôn ngữ (i18n)
- Kiểm tra hiện tượng lệch ngôn ngữ Anh - Việt và thiếu rule `empty` giữa các file đa ngôn ngữ/cấu hình (ví dụ bên VMS có nhưng qua màn hình thiết bị `Equipment` bị thiếu hoặc bị dữ liệu cấu hình từ backend trả về ghi đè).

### 1.5. Chuẩn hóa Cấu trúc Tab Phân Cấp (Tab Cấp Trên vs Tab Con) & Quy trình Wireframe
- **Vấn đề tồn tại:** Bố cục Tab đang thiếu thống nhất (như màn hình Outbox quên set thuộc tính).
- **Quy chuẩn thống nhất:**
  - Phải đặt **Tab ở trên (Tab gạch chân / Tab cấp trên)** giống chuẩn của phân hệ **TMS quản lý**.
  - Xây dựng thành quy chuẩn/component dùng chung, tránh việc mỗi người tự quăng một kiểu làm hỏng bố cục giao diện.
  - **Bắt buộc làm Wireframe trước khi code:** Với các màn hình/tính năng phức tạp, phải vẽ Wireframe trước để duyệt luồng chạy đúng ý rồi mới viết code, tránh code xong phải đập đi sửa lại.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

---

### [19:35 - 24:55] Chuẩn hóa Component Lập Lịch (Scheduler) dùng chung toàn hệ thống
- **Đạt:** Rồi sau đó là tới cái phần Lập lịch (Schedule). Hiện tại cái tab lập lịch này nè, ví dụ như tab lập lịch theo ngày, theo tuần... chia ra một file riêng, một module riêng sẽ gọn hơn đúng không?
- **Hiếu:** Trong một cái directory của anh, cái này anh đang chia theo menu đúng không?
- **Đạt:** Đúng rồi, chia theo màn hình con. Chứ không chia rời rạc nữa, nó đỡ rối hơn là chia theo dạng chức năng nhỏ lẻ. Nhưng mà cái này vô bấm một cái xong rồi không biết bấm tiếp cái nào nữa. Thì bấm công tắc tắt đi, bật tắt hiển thị thôi.
- **Hiếu:** Cái lịch chạy này mình thấy ổn, chắc không sửa một lần nữa đâu.
- **Đạt:** Thì làm cái mẫu chung thôi, xong rồi apply hết! Như bên Email là 4 cái component đó, qua bên kia làm cái mẫu y chang vậy thôi, chứ mỗi cái mình đi làm chi cho cực. Nhiều khi thằng này cần nhưng thằng kia mai mốt cần y chang vậy thì bật tắt lại thôi.
- **Hiếu:** Ủa thì nó cũng quanh đi quẩn lại là theo ngày nè, đúng không? Rồi theo định kỳ cái gì đó. Ví dụ như thằng này không có cái đó thì ẩn cái chức năng đó đi thôi. À là nguyên cái component cha á hả anh?
- **Đạt:** Ừ, viết một cái component chung thôi! Chứ mỗi một cái mà ngồi mình viết lại làm gì? Ý anh Sơn nói là thằng này nó đang là một component nè, này là một kiểu lập lịch nè, cái Email cũng làm y chang vậy luôn. Email nó cũng là một cái lịch, ShareData nó cũng có một phần lập lịch riêng nè, thì nó giống nhau thì chia nó ra làm một cái component chung!
- **Hiếu:** Thì anh Sơn ảnh đang mong muốn là viết thành một cái lịch chung đó.
- **Đạt:** Nhưng mà bên này lại dùng cái nút kiểu này, bên kia lại dùng cái checkbox... Hai kiểu đó đều không được, gom lại sửa lại! Gom một component xuống xong rồi sẽ làm: mở cái ấy ra cho hết, bật tắt theo type. Truyền `type` vô đúng không: `type` nào cho phép theo ngày, theo tuần đi thì mấy cái này bật lên, còn bên kia chỉ theo ngày thì tắt nó đi.
- **Hiếu:** Nhưng mà anh thấy cái mẫu lịch của mình hiện tại trong mấy tính năng phân hệ, cái nào đang ổn nhất để em dựa vô đó em build cái component chuẩn luôn?
- **Đạt:** Mail á... hoặc ShareData. Chuẩn thì nó dùng `group button` (`RadioButton`). Ý anh Sơn ảnh hỏi là cái logic nào hợp lý: liên tục, hàng ngày, định kỳ. Thì cứ làm hết, cái nào full thì để hết. Rồi sau đó ví dụ như thằng này hiện hết thì để cả, còn thằng kia không có liên tục đi thì `@continuous` / `option` gì đó bằng `false` thì sẽ mất cái này đi thôi.
- **Hiếu:** Ok. Vậy thôi em confirm lại hiểu chỗ đó là build cái component chung ha, optimize lại.
- **Đạt:** Build component chứ không phải optimize! Optimize là có rồi mới gọi là optimize.
- **Hiếu:** Không, thì hiện tại nó đang là 3 cái component riêng thì bây giờ mình optimize gom lại thành 1 cái tập trung.

---

### [24:55 - 25:35 & 26:40 - 27:30] Bố cục Tab Phân Cấp (Theo chuẩn TMS quản lý) & Rút kinh nghiệm làm Wireframe
- **Hiếu:** Sao cái chỗ này nó sai vậy? À, hôm nay nó đang bị a đù vậy, nó phải tab ở trên đúng không? Đây là tab chứ, phải tab ở trên. Tab gạch chân á! Tab như thằng TMS á, TMS quản lý á!
- **Đạt:** Nè, kiểu này kiểu quăng lên đây ai mỗi thằng làm một kiểu là nát bét luôn!
- **Hiếu:** Đúng rồi, hôm nọ thằng Outbox tự nhiên cái quên set...
- **Đạt:** Mấy thằng này build component dùng chung đàng hoàng đi chứ không là nó nát bét!
- **Hiếu:** Cái này hôm nọ em tính hình dung trước cái màn hình... hôm nọ tính làm rồi mà bận quá chưa làm. Tại vì nếu khai báo cha con ở đây mà làm như thế này là sai.
- **Đạt:** Ừ, thì làm cái Wireframe trước! Thiết kế cái màn hình trước, xem nó chạy đúng ý mình chưa rồi mới ngồi viết code. Chứ giờ vô nhét ý này ý nọ mà cuối cùng không hình dung ra nó là cái gì, làm sai lại phải sửa lại từ đầu.

---

### [31:24 - 37:26] Phân hệ ShareData: Bóc tách tầng xử lý kết nối (`HandleConnection` — `API`, `Kafka`, `File`) & Cấu hình `SystemConfigData`
> 📌 *Lưu ý âm thanh gốc:* Trong khoảng `31:50 - 35:30`, có tiếng đồng nghiệp nữ ở bàn bên cạnh nói chuyện riêng về việc tải file Excel / dữ liệu offline / gửi email. Phần dưới đây lọc bỏ hoàn toàn tạp âm bàn bên, chỉ ghi lại đúng cuộc thảo luận kỹ thuật giữa Đạt và Hiếu.

- **Đạt:** Hôm bữa là mình đang chỉ dừng lại ở bước là xử lý truyền qua API đúng không?
- **Hiếu:** Đúng rồi. Tách cái phần kết nối nó ra thành một cái module luôn. Cái tình huống này ví dụ như cái cục dữ liệu này là xử lý rồi đúng không, như là mình sẽ gửi qua API, sau đó gửi qua File...
- **Đạt:** Từ từ, nói lại nói lại, ồn quá anh, replay lại ba! Rồi: cái cục dữ liệu đúng không, là xử lý rồi, đầu ra là nó là **JSON** đúng không? Tại lúc trước là mình đang fix là API thôi, là mình cầm cái này mình gửi qua một cái API đúng không? Cầm gửi qua luôn, thì bây giờ sẽ tách cái này ra bằng một cái phần tích hợp cho nó — là tích hợp về xử lý kết nối. Khi mà có cục dữ liệu này, nó sẽ gửi vô thằng đó kèm với lại cái thông tin kết nối tương ứng của cái thằng được cấu hình. Thì trong này bắt đầu nó sẽ tổng hợp nó xử lý các kết nối khác nhau: có thể là **API**, có thể là **Kafka**, có thể là các kiểu mà đang xây dựng ở trong này.
- **Hiếu:** Đảm bảo là nó kết nối được với cái thằng khai báo, và cái dữ liệu này tùy thằng: ví dụ nó là **API** nó sẽ gửi vô API, **Kafka** nó sẽ gửi vào trong kênh channel của thằng đó.
- **Đạt:** Thì cái thằng đầu vào của thằng này là: **một là cái cục data sau khi đã xử lý, với lại hai là thông tin bảo mật, ba là cấu hình kết nối**. Cấu hình kết nối có thể là IP, có thể là Port, có thể là Username, tùy theo mỗi thằng khác nhau. Thì cái này cho dễ thì có thể dùng **`Dictionary`** cũng được (`Key-Value`). Cái cục mà xử lý mấy cái vấn đề data này là sẽ build ra một cái module riêng — chung cũng được nữa, nhưng mà làm sao tách nó ra thành cái phần xử lý để đảm bảo là ví dụ mai mốt mình có thêm cái xử lý gì đó thì vô đây sửa thôi, không có ảnh hưởng tới phần xử lý ở ngoài. Rồi tương ứng nè: do mình đã tách vậy rồi nên cái cục data này mình cũng phải xử lý theo. Về bản chất là đang... giờ JSON là mình đang quy định chuẩn của mình thôi.
- **Hiếu:** Đầu vào ở đây là đang câu chuyện **JSON** nè, nhưng mà nhiều khi vô trong này đi **Kafka** nó sẽ chuyển JSON này thành **byte (`byte[]`)** đi, mảng byte gửi đi, mình đâu quan tâm. Nên mình sẽ đẩy cái thằng này vô đây luôn, và chỗ này đầu vào thì vẫn thống nhất là JSON cho nó dễ. Rồi cục JSON đó mình biến tấu: lúc đẩy qua đây thằng này là JSON, nhưng mà vô đây mình sẽ dựa vô cái **`Type`** mình convert cái JSON này lại tương thích với lại mấy cái kiểu **Kafka** hay gì đó.
- **Hiếu:** Thằng bên đây là một cái `Type` đúng không? Mình chọn cái `Type` xong rồi cấu hình của nó là cái gì, nó sẽ quy định là biến đổi thành cái gì với lại thông tin cấu hình đi kèm đúng không? Nếu mà nó là một cái `Type`, thì bây giờ tách cái thằng `Type` này ra một cái folder hay một cái gì để quản lý, ví dụ như một cái service... cái gì đó `HandleConnection` hay cái gì đó, một cái service của nó. Quản lý trong code thôi anh, chứ không phải quản lý ở ngoài giao diện đâu. Giao diện quản lý chi ba?
- **Đạt:** Không, quản lý giao diện ý em là sao? Giao diện quản lý là quản lý ở đây rồi mà, tách gì nữa?
- **Hiếu:** Ý em là lúc mà cấu hình thằng đối tác gửi đi này là phải biết nó gửi theo kênh nào đúng không?
- **Đạt:** Thì đây nè!
- **Hiếu:** Thì vậy là mấy cái lựa chọn đó sẽ fix cứng?
- **Đạt:** Sao fix cứng được? Nếu không fix cứng thì phải có màn hình quản lý nó, hiểu không? Mấy thằng này về bản chất là mình lưu được ở trong cái config được mà! Ở trong mục config của mình, xong rồi khai báo trong config... **`SystemConfig` với lại `SystemConfigData`** á!
- **Hiếu:** Xong rồi cho một hàm service để nó xử lý thông tin thêm đúng không?
- **Đạt:** Rồi ok, hiểu hiểu. Còn cái nào cứng ở trong code thì bản chất là fix cứng phương thức còn xử lý.

---

### [37:26 - 42:00] Truy vấn động (Dynamic Query), SuperAdmin & Rà soát Đa ngôn ngữ (i18n)
- **Hiếu:** Rồi tiếp theo, không biết có sửa chưa, đó chính là cái phần câu truy vấn (Query) đúng không? Câu truy vấn hôm bữa có sửa luôn chưa?
- **Đạt:** Sửa gì ở trong phần từ dữ liệu cơ sở dữ liệu?
- **Hiếu:** Có, câu truy vấn load lên được cái danh sách mấy cái cột.
- **Đạt:** Chưa, hôm bữa mình đang ngưng ở đây nè. Là cái phần này bây giờ là mình sẽ cho nó sửa động luôn đúng không?
- **Hiếu:** Đúng rồi, là sẽ quản lý qua màn hình đó. Nhưng mà bây giờ có cho nó quản lý cái query này không? Hay là đẩy đại query lên luôn?
- **Đạt:** Rồi lỡ nó viết nhăng viết cuội bậy bạ thì sao?
- **Hiếu:** Validate lại! Có phân quyền không mà viết nhăng viết cuội? Cái chức năng đó là chỉ có acc **SuperAdmin** mới vô được thôi anh.
- **Đạt:** Rồi, đó là một ý, note lại. Đúng rồi, SuperAdmin vô được, nhưng mà ví dụ SuperAdmin nó viết nhăng viết cuội thì sao?
- **Hiếu:** Thì em check là do mình, em viết code check được mà!
- **Đạt:** Thằng nào viết nhăng viết cuội thì sao?
- **Hiếu:** Thì check nó thôi, lock tài khoản nó lại! Ghi nhận lại như ông Hiếu admin đi, vô sửa cái gì đó, sửa sai thì log tên ổng lại, vậy thôi!
- **Đạt:** Rồi hiểu, thêm UI nữa.
- **Hiếu:** [Đ... má] vậy mà tưởng anh Đạt phát của em, ai ngờ anh Đạt chuyển giao công nghệ!
- **Đạt:** Thôi mày làm VideoWall đi, để tao làm cái này một mình cho! Mà cái này sửa nhanh nhanh nha.
- **Hiếu:** Tùy anh, nhanh hay chậm tùy vô anh chứ.
- **Đạt:** Sửa nhanh thôi, biết nói định làm cái Wireframe cho xong hết. Giờ coi lại mấy cái chỗ ngôn ngữ tiếng Anh tiếng Việt nè, hình như đang bị lỗi.
- **Hiếu:** Ở bên file VMS thì nó không có, mà không biết qua đây nó bị. Cái này là chưa có cái rule... đang để tiếng Anh, chưa có import cái rule `empty`. Anh check trong file là có cả hai luôn rồi.
- **Đạt:** Từ từ, anh gặp cái case này rồi. Hình như đang khai báo thằng kia là tiếng Việt, trong kia là tiếng Anh. Nếu mà cái kia mà tiếng Việt thì cái ấy viết cứng.
- **Hiếu:** Cho nó đi vô cái từ `empty` đi. Ủa sao bên này không có?
- **Đạt:** Cái này đang ở backend mà, cấu hình bên này bên backend mà, liên quan gì frontend? Quan trọng là đang sợ nó đè dữ liệu. Ở bên `equipment` á, nó bắt cái code... gán kiểu gì xong nó ra `TF15` đi, dính luôn cái này của thằng kia. Component, không cứng, đó là hai cái tổng rồi!


