---
tier: A
read: full
source:
  - DocBusinessThienAn/videowall.m4a (Toàn văn 45:10)
  - DocBusinessThienAn/duong_so_8_2.m4a (16:00 - 19:16)
  - DocBusinessThienAn/duong_so_8_3.m4a (06:12)
date: 22-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: VideoWall — Cuộc Họp Kỹ Thuật Toàn Diện: Tái Cấu Trúc Kiến Trúc (Decoupling Thiết Bị Equipment), Phân Quyền Multi-User & Đa Cụm Màn Hình, Tối Ưu UX Luồng Chia Vùng, Chuẩn Hóa Bố Cục Trực Quan Ma Trận & Sửa Lỗi Giao Diện
participants:
  - Anh Sơn (Tech Lead / Chủ trì)
  - Đạt (Dev / Frontend & Backend)
---

# Kịch Bản & Biên Bản Cuộc Họp Kỹ Thuật: VideoWall — Tái Cấu Trúc Kiến Trúc, Phân Quyền Multi-User & Tối Ưu Toàn Diện UX/UI
### (Nguồn gốc: Tệp ghi âm `videowall.m4a` — Thời lượng: ~45 phút 10 giây)

- **Ngày ghi nhận:** 22/10/2026
- **Thời lượng:** ~45 phút 10 giây (chia 7 phân đoạn kiểm chứng)
- **Người tham gia:** Anh Sơn (Tech Lead / Chủ trì), Đạt (Dev)
- **Mục đích:** Review toàn diện hiện trạng phân hệ VideoWall (Bộ điều khiển, Màn hình hiển thị, Chia vùng, Kịch bản, Nguồn tín hiệu, Phân quyền); xác định các điểm nghẽn kiến trúc và trải nghiệm người dùng; thống nhất các quyết định kỹ thuật và nguyên tắc refactor chuẩn mực.

---

## 1. Tóm Tắt Quyết Định Kỹ Thuật Cốt Lõi (Executive Summary)

### 1.1. Luồng Trải Nghiệm "Chia Vùng Màn Hình" (Screen Division UX)
- **Bất cập hiện tại:** Bấm nút "Chia vùng" lập tức kích hoạt chế độ kéo chuột tự do trên ma trận màn hình. Người dùng lúng túng không biết kéo thế nào, thao tác bị khựng; khi muốn đổi kiểu chia (1-1, 2x2, 3x3, tùy chỉnh) thì không có bước chọn rõ ràng, phải hủy bỏ hoặc thao tác lòng vòng.
- **Quyết định chốt:**
  - Bấm "Chia vùng" $\rightarrow$ **Mở popup/dialog bộ điều khiển chọn chế độ chia trước** (Chế độ mẫu: 1-1, 2x2 [4 vùng], 3x3 [9 vùng], Tùy chỉnh [Custom]).
  - Sau khi chọn chế độ, giao diện mới kích hoạt vùng thao tác để người dùng trỏ vào cụm màn hình/vị trí muốn áp dụng.
  - Cho phép lưu lại các mẫu bố cục chia vùng tùy chỉnh để tái sử dụng nhanh cho các màn hình khác mà không phải vẽ lại từng ô.

### 1.2. Phân Quyền Multi-User & Mở Rộng Hỗ Trợ Đa Cụm Màn Hình (Multi-Cluster)
- **Phân quyền người dùng (Multi-User Assignment):**
  - Hiện tại: Mỗi dòng cấu hình chỉ gán được 1 người dùng đơn lẻ. Khi có 5, 10, hay 100 người dùng cần xem/điều khiển cụm màn hình thì thao tác gán từng người gây tốn công và rối rắm.
  - Chốt: Nâng cấp cơ chế gán quyền hỗ trợ **chọn theo Nhóm người dùng (Group/Tổ chức) hoặc chọn nhiều người dùng (`Multi-select` dạng tag/dropdown)** cho một cụm/vùng màn hình. Thiết lập cấu hình 1 lần áp dụng chung cho toàn bộ danh sách được chọn.
- **Hệ tọa độ hiển thị chuẩn (Coordinate System & Origin):**
  - Hiện tại: Tọa độ (0, 0) hiển thị không nhất quán (không rõ gốc tọa độ là góc trên bên trái, góc dưới bên trái, hay từ tâm). Khi người dùng nhập tọa độ "1" hoặc "10" thì màn hình bị nhảy lung tung, vỡ bố cục.
  - Chốt: Xác định rõ gốc tọa độ chuẩn (Top-Left tiêu chuẩn theo ma trận Display), chuẩn hóa tọa độ vật lý sang tỷ lệ trực quan.
- **Hỗ trợ Đa Cụm Màn Hình (Multi-Cluster Support):**
  - Thực tế trung tâm điều hành có nhiều cụm màn hình: Bức tường chính (8x4 gồm 32 màn hình), Cụm phụ SCADA/Giám sát riêng (2x2), các cụm mở rộng Giai đoạn 2, Giai đoạn 3.
  - Hệ thống phải quản lý độc lập từng cụm màn hình (`ClusterId` / `WallId`), không được giả định toàn bộ hệ thống chỉ có 1 bức tường màn hình đơn lẻ.
- **Tách Component Trực Quan Dùng Chung & Thống Nhất Ký Hiệu:**
  - Trích xuất phần hiển thị ma trận màn hình thành Component dùng chung (`ScreenLayoutRenderer`), có các lớp overlay (lớp hiển thị trạng thái, lớp thao tác chia vùng, lớp gán kịch bản).
  - Thống nhất quy ước tên gọi và tọa độ: Loại bỏ sự bất nhất giữa `H1C1`, `1-1`, `CD` (Hàng/Cột vs Row/Col) $\rightarrow$ quy về chuẩn hiển thị rõ ràng `Hàng 1 - Cột 1` (`H1-C1`) thân thiện cho người vận hành.

### 1.3. Chuẩn Hóa Thuật Ngữ, Dashboard Trạng Thái & Tab Bố Cục Trực Quan
- **Thuật ngữ & Ngôn ngữ (i18n):**
  - Không pha trộn tiếng Anh và tiếng Việt bừa bãi.
  - Chuẩn hóa: "Topology" $\rightarrow$ **Sơ đồ kết nối** hoặc **Bố cục màn hình**; "Controller" $\rightarrow$ **Bộ điều khiển**.
  - 100% đưa vào bộ từ điển i18n với tiền tố ngắn gọn thống nhất `vw_`.
- **Dashboard Thống Kê Trạng Thái (Status Dashboard):**
  - Bất cập: Số liệu Controller và Screen bị gộp lẫn lộn, nằm tít bên phải trong khi danh sách ở bên trái, khiến mắt người dùng phải liếc ngang màn hình rất mỏi ("bị lé mắt").
  - Chốt: Tách bạch rõ 2 khối thống kê thành 2 cột/panel trực quan:
    1. **Khối Bộ Điều Khiển (Controller):** Tổng số bộ điều khiển | Đang hoạt động | Mất kết nối / Ngừng hoạt động.
    2. **Khối Màn Hình (Screen):** Tổng số màn hình | Đang bật | Đang tắt / Chờ.
- **Tab Thiết Lập Bố Cục Trực Quan (Visual Screen Setup Tab):**
  - Bổ sung tab/giao diện thiết lập bố cục trực quan theo lưới (Grid layout, ví dụ ma trận 8x4, 2x2).
  - Khi ở chế độ chỉnh sửa, người dùng bấm trực tiếp vào từng ô màn hình trên lưới trực quan để cấu hình tham số thay vì phải tìm kiếm trong danh sách bảng dữ liệu khô khan.

### 1.4. Tìm Kiếm Đa Tiêu Chí, Bản Chất Model & Tách Khỏi Cấu Hình Thiết Bị Trùng Lặp
- **Tìm kiếm (Search):** Ô tìm kiếm phải hỗ trợ tìm kiếm nhanh theo cả **Mã** (Code) và **Tên** (Name), không bắt người dùng phải chọn chính xác một loại trường.
- **Bản chất trường Model:** `Model` là thông tin định danh/chủng loại thiết bị (do người dùng khai báo hoặc hệ thống đọc về từ thiết bị), **không phải là cấu hình phân nhánh cứng**. Backend đọc model/phiên bản giao thức của thiết bị để kích hoạt adapter/driver tương ứng (ví dụ: giao thức Hikvision v1 hay v2).
- **Loại bỏ cấu hình dư thừa (Single Source of Truth với Module Equipment):**
  - VideoWall đang cấu hình lặp lại: Địa chỉ IP, Cổng, Tài khoản đăng nhập, Mật khẩu của thiết bị controller.
  - Trong khi đó, module **Equipment (Thiết bị)** đã quản lý toàn bộ thông tin này!
  - Việc lưu duplicate thông tin kết nối trong VideoWall dẫn đến tình trạng: đổi mật khẩu/IP ở bên Thiết bị thì bên VideoWall bị lệch kết nối, sinh lỗi đồng bộ.
  - **Quyết định:** VideoWall liên kết thiết bị thông qua khóa ngoại (`EquipmentId`), nạp thông tin kết nối từ Module Equipment, loại bỏ toàn bộ các ô nhập IP/User/Pass dư thừa trên form VideoWall.
- **Tổ chức Form Nguồn Tín Hiệu (Signal Source):** Gom các trường liên quan đến tín hiệu đầu vào: Loại nguồn tín hiệu, Cổng tín hiệu vào (Input Port) thành một cụm logic liên tục. ⛔ Cấm chèn trường chọn Bộ điều khiển nằm kẹp vào giữa Loại nguồn tín hiệu và Cổng tín hiệu.

### 1.5. Bố Cục Form, Thứ Tự Tab Phím & Lỗi Màu Sắc Giao Diện
- **Luồng Form & Phím Tab (Tab Index Order):**
  - Bố cục các ô nhập liệu hiện tại đang bị zíc zắc, nhảy hình chữ L ("L-shape"), bấm phím Tab nhảy lung tung không theo thứ tự tự nhiên.
  - Chuẩn hóa: Luồng nhìn và thứ tự phím Tab phải tuân theo nguyên tắc từ trên xuống dưới, từ trái sang phải.
  - Sử dụng `<el-divider>` để ngăn cách rõ ràng giữa các phân nhóm thông tin thay vì nhồi nhét ô trống vào các góc form.
  - Gom nhóm chuẩn: Độ phân giải (`Resolution`) phải thuộc về thông số màn hình / luồng tín hiệu, trong khi Trạng thái và Thứ tự hiển thị thuộc về nhóm cấu hình vận hành.
- **Lỗi hiển thị màu sắc (Solid Blue Overlay Bug):**
  - Bảng dữ liệu / cây danh mục đang bị lỗi: các dòng bị phủ một lớp màu xanh đặc quánh (`bị phủ màu xanh / mù màu`), làm che mất chữ và mất hiệu ứng sọc vằn (zebra stripes) chuẩn của hệ thống.
  - Cần kiểm tra xung đột CSS class giữa VideoWall và module dùng chung (như ShareData hoặc User layout).
- **Tỷ lệ độ rộng cột trong bảng (Table Column Proportions):** Một số cột chiếm độ rộng quá lớn (ví dụ cột Mã bộ điều khiển quá dài), trong khi các cột quan trọng khác bị bóp méo, co cụm. Điều chỉnh lại tỷ lệ độ rộng (`min-width` / flex) cho cân đối, hài hòa thị giác.

### 1.6. Tinh Chỉnh Giao Diện Kịch Bản, Độ Tương Phản Màu & Thanh Thống Kê
- **Màu sắc & Kích thước font chữ:** Giao diện mở lên bị ám một màu xám xịt từ trong ra ngoài (màu nền xám, màn hình xám, viền ngoài xám), trùng màu nhau nhìn rất đơn điệu và tối tăm. Nâng kích thước chữ (font-size) cho dễ đọc; điều chỉnh độ tương phản màu sắc để phân tách rõ vùng thao tác và vùng nền (phù hợp người vận hành lớn tuổi).
- **Bổ sung Tiêu đề & Nhãn mô tả (Labels & Titles):**
  - Bổ sung tiêu đề Header rõ ràng (ví dụ: "Kịch bản hiển thị màn hình", "Bố cục ma trận").
  - Trạng thái không để chữ "Bình thường" trơ trọi; bổ sung nhãn ngữ cảnh đầy đủ (ví dụ: "Kịch bản: Bình thường", "Trạng thái: Đang hoạt động").
- **Thanh chỉ số tóm tắt (Summary Metrics Bar):** Bổ sung thanh thống kê trực quan ở đầu/chân trang: Tổng số nguồn tín hiệu (Input) đang hiển thị trên màn hình | Tổng số màn hình đang sử dụng | Số màn hình còn trống / chưa sử dụng (ví dụ: "Còn 2 màn hình chưa sử dụng"). Người dùng không phải ngồi đếm thủ công từng ô.
- **Cân đối Layout Quản lý Kịch bản (Scenario Management Layout):** Khối phía dưới: Thông tin kịch bản bị kéo dài toàn bộ chiều ngang màn hình (full-width), trong khi phần thuộc tính cửa sổ (`Window Properties`) bị dồn ép thành một cụm tí hon ở góc trái $\rightarrow$ giao diện mất cân đối nghiêm trọng. Cần thiết kế lại bố cục cân bằng giữa thông tin kịch bản tổng quan và thuộc tính chi tiết của cửa sổ hiển thị.
- **Làm rõ Khái niệm "Thứ tự lớp" (Layer / Z-Index):** Trường "Thứ tự lớp" (bản chất là CSS/Layout `z-index`) đang để người dùng tự nhập số mà không có hướng dẫn, không biết số lớn đè lên số nhỏ hay ngược lại. Thêm tooltip/chú thích giải thích rõ quy tắc xếp lớp ("Giá trị càng lớn, cửa sổ hiển thị càng nổi lên trên các lớp khác") hoặc nút trực quan "Đưa lên trên cùng" / "Đưa xuống dưới cùng".
- **Chuẩn hóa Tọa độ lưới hiển thị:** Bắt đầu từ ô (1, 1) rõ ràng theo quy tắc Hàng - Cột (`H1-C1`), đồng bộ với tất cả các màn hình để người vận hành dễ hình dung.

### 1.7. Vị Trí Nút Chức Năng, Thu Gọn Panel & Highlight Liên Kết Tín Hiệu
- **Bố trí Nút Thao Tác (Action Buttons Placement):**
  - Nút "Thêm mới" đang đặt xa tít một góc đối diện với danh sách kịch bản $\rightarrow$ chuột phải di chuyển quãng đường dài qua toàn bộ màn hình; người dùng dễ bấm nhầm giữa nút Thêm mới và nút Sửa/Điều chỉnh. Đưa nút "Thêm mới" về đúng cụm quản lý kịch bản.
  - Nút "Lưu" (Save) phải đặt ở vị trí cố định, luôn nhìn thấy (Persistent/Sticky action bar) để khi người dùng chỉnh sửa xong có thể lưu ngay lập tức mà không phải cuộn tìm kiếm.
- **Tính năng Thu Gọn Panel (Collapsible / Splitter):** Thêm splitters / collapse toggles (`thu gọn`) cho các panel bên cạnh (danh sách kịch bản, thuộc tính). Khi người dùng muốn tập trung quan sát toàn cảnh bức tường màn hình lớn, họ có thể bấm thu gọn panel để mở rộng tối đa không gian cho khung trực quan.
- **Highlight Liên Kết Trực Quan (Linked Active State):** Bấm chọn một kịch bản hoặc nguồn tín hiệu ở danh sách bên trái $\rightarrow$ cửa sổ/màn hình tương ứng trên khung bố cục phải sáng lên (Highlight). Ngược lại, bấm chọn một cửa sổ trên màn hình $\rightarrow$ dòng tương ứng ở danh sách bên cạnh phải được active/focus.
- **Ràng Buộc Trạng Thái Tín Hiệu (Signal In-Use Constraint):** Nếu hệ thống không cho phép 1 nguồn tín hiệu mở đồng thời trên nhiều cửa sổ: Khi một nguồn tín hiệu đã được gán vào màn hình, tự động rào lại (disable/gray out) nguồn đó trong danh sách và đánh dấu trạng thái "Đang sử dụng", tránh kéo trùng sinh lỗi.

---

## 2. Toàn Văn Đối Thoại Kỹ Thuật Theo Dòng Thời Gian (Detailed Transcript)

### Phân đoạn 1: Luồng Chia Vùng Màn Hình (00:00 - 08:00)
- **[00:15] Anh Sơn:** Giờ nói về cái chia vùng nè. Hiện tại bấm vô chia vùng một cái là nó bắt kéo liền luôn đúng không?
- **[00:25] Đạt:** Dạ đúng rồi, bấm chia vùng là nó cho kéo rê chuột chọn vùng trên màn hình.
- **[00:32] Anh Sơn:** Kéo vậy người ta đâu có biết là chia mấy vùng! Nhiều khi người ta muốn chia đều 2x2, hay chia 3x3, hoặc là để nguyên 1-1. Tự nhiên nhào vô bắt kéo, kéo xong rồi muốn đổi kiểu khác thì làm sao?
- **[00:50] Đạt:** Hiện tại muốn đổi thì phải hủy đi làm lại từ đầu anh.
- **[00:55] Anh Sơn:** Vậy là dở rồi. Bấm "Chia vùng" thì phải bật cái dialog hoặc là một cái control chọn chế độ chia trước. Cho người ta chọn: kiểu 1-1 nè, 2x2 ra 4 vùng nè, 3x3 ra 9 vùng nè, hay là chế độ tùy chỉnh (custom).
- **[01:30] Anh Sơn:** Chọn chế độ xong xuôi rồi mới click vô cái màn hình hoặc kéo vùng áp dụng. Với lại ví dụ người ta chia tùy chỉnh được một cái layout đẹp cho màn hình này rồi, người ta muốn áp cái layout đó cho các màn hình khác thì sao? Phải cho lưu lại thành mẫu layout để tái sử dụng chứ! Không lẽ màn hình nào cũng bắt ngồi vẽ lại từng ô một?
- **[02:15] Đạt:** Dạ em hiểu rồi, tức là chọn template chia vùng trước, rồi mới áp lên màn hình, và có nút lưu custom template.

### Phân đoạn 2: Phân Quyền Multi-User & Đa Cụm Màn Hình (08:00 - 16:00)
- **[08:15] Anh Sơn:** Cái phần phân quyền màn hình hôm trước anh nói đó. Giờ một cụm màn hình mà có cả phòng ban 10 người, 20 người hay cả 100 người dùng chung một quyền xem hoặc điều khiển, mà em cho tạo mỗi người một dòng là chết người ta rồi!
- **[08:45] Đạt:** Dạ hiện tại đang lưu mỗi dòng một UserId.
- **[08:52] Anh Sơn:** Phải cho chọn nhiều người dùng, dạng multi-select hoặc chọn theo Nhóm/Phòng ban. Cấu hình vùng hiển thị một lần thôi rồi gán cho nguyên một danh sách người dùng.
- **[09:20] Anh Sơn:** Còn cái tọa độ hiển thị nữa. Tọa độ (0, 0) hiện tại em tính từ đâu? Góc trên bên trái, góc dưới hay ở giữa? Bữa anh thấy gõ số 1 với số 10 vô cái màn hình nó nhảy loạn xạ, vỡ hết khung.
- **[09:50] Đạt:** Dạ do lúc trước lấy tọa độ pixel thô chưa chuẩn hóa theo tỷ lệ khung Display.
- **[10:15] Anh Sơn:** Phải chuẩn hóa lại gốc tọa độ Top-Left chuẩn ma trận. Rồi hệ thống mình sau này đâu phải chỉ có một bức tường 8x4 duy nhất! Ví dụ trung tâm điều hành có bức tường chính 8x4, rồi bên cạnh có cụm màn hình phụ 2x2 cho giám sát SCADA riêng, rồi sau này mở rộng giai đoạn 2, giai đoạn 3 nữa. Hệ thống phải quản lý tách biệt từng Cụm màn hình (Cluster), đừng có gom cứng lại tưởng chỉ có một cái tường.
- **[11:10] Anh Sơn:** Tách cái khung vẽ màn hình thành một Component dùng chung đi. Rồi thống nhất lại cách gọi tên tọa độ: chỗ thì H1C1, chỗ thì 1-1, chỗ thì CD. Người ta nhìn vô không hiểu gì hết. Cứ ghi rõ ràng Hàng 1 - Cột 1 hoặc H1-C1 cho đồng bộ từ trên xuống dưới.

### Phân đoạn 3: Thuật Ngữ, Dashboard Thống Kê & Tab Bố Cục (16:00 - 24:00)
- **[16:20] Anh Sơn:** Thuật ngữ trên giao diện phải rà lại hết. Chỗ thì tiếng Anh, chỗ thì tiếng Việt lộn xộn. Cái chữ "Topology" ai mà hiểu? Đổi thành "Sơ đồ kết nối" hoặc "Bố cục màn hình". Chữ "Controller" thì dịch ra "Bộ điều khiển". Đưa hết vô i18n đàng hoàng.
- **[17:15] Anh Sơn:** Nhìn cái Dashboard thống kê trạng thái coi. Bên trái là cái danh sách, mà mấy cái số đếm trạng thái lại thảy tút luốt qua góc phải màn hình! Mắt người ta nhìn bên này xong phải liếc qua bên kia, bị lé mắt luôn á!
- **[18:00] Đạt:** Dạ để em gom lại gần nhau.
- **[18:10] Anh Sơn:** Tách thành 2 cột rõ ràng: Cột 1 là Bộ điều khiển (Tổng số bộ điều khiển | Đang hoạt động | Mất kết nối), Cột 2 là Màn hình (Tổng số màn hình | Đang bật | Đang tắt). Nhìn vô cái là nắm được tổng thể liền.
- **[19:05] Anh Sơn:** Thêm một cái tab Bố cục trực quan nữa. Trên đó thể hiện cái lưới màn hình 8x4 hoặc 2x2. Khi muốn cấu hình màn hình nào, người ta bấm thẳng vô ô màn hình đó trên lưới để chỉnh thông số, trực quan hơn nhiều so với việc ngồi tìm trong cái bảng danh sách dài ngoằng.

### Phân đoạn 4: Tìm Kiếm, Model & Decouple Khỏi Cấu Hình Thiết Bị (24:00 - 32:00)
- **[24:10] Anh Sơn:** Ô tìm kiếm phải cho tìm theo cả Mã và Tên. Gõ từ khóa vô là tìm được cả hai, chứ đừng bắt người ta chọn loại tìm kiếm chi cho mệt.
- **[25:00] Anh Sơn:** Cái trường `Model` này nè, bản chất nó là thuộc tính của thiết bị. Người ta nhập model vô hoặc hệ thống đọc về từ thiết bị, rồi dưới code Backend sẽ dựa vào model đó để biết nó xài giao thức Hikvision v1 hay v2 mà gọi API cho đúng. Nó không phải là một cái cấu hình phân nhánh cứng trên form.
- **[26:30] Anh Sơn:** Với lại tại sao trên form VideoWall này lại có nhập Địa chỉ IP, Cổng, Tài khoản, Mật khẩu nữa? Bên module Thiết bị (Equipment) người ta đã khai báo quản lý hết mấy thông tin này rồi mà?
- **[27:10] Đạt:** Dạ tại lúc trước làm riêng nên lưu luôn thông tin kết nối vô bảng Controller.
- **[27:25] Anh Sơn:** Lưu trùng như vậy là bậy rồi! Bên Thiết bị người ta đổi mật khẩu hoặc đổi dải IP, bên VideoWall này không cập nhật là mất kết nối liền. Chỗ này VideoWall chỉ cần chọn thiết bị từ Equipment qua khóa ngoại thôi, rồi tự lấy IP, user, pass bên Equipment qua. Bỏ hết mấy ô nhập IP, mật khẩu dư thừa trên form này đi.
- **[28:40] Anh Sơn:** Còn cái form Nguồn tín hiệu: Loại nguồn tín hiệu với Cổng tín hiệu vào phải đi liền một cụm. Tự nhiên chen cái ô chọn Bộ điều khiển vô chính giữa chi vậy? Bố trí lại cho đúng cụm logic.

### Phân đoạn 5: Bố Cục Form, Phím Tab & Lỗi Màu Sắc UI (32:00 - 40:00)
- **[32:15] Anh Sơn:** Coi lại luồng hiển thị của form nè. Bấm phím Tab nó nhảy zíc zắc hình chữ L, ô trên nhảy tút qua ô dưới rồi nhảy ngược lên lại, không có theo thứ tự gì hết!
- **[32:50] Đạt:** Dạ do em chia cột flex mà chưa set lại tabindex với thứ tự DOM.
- **[33:10] Anh Sơn:** Form phải đi từ trên xuống dưới, từ trái qua phải theo đúng thói quen người dùng. Lấy cái `el-divider` ngăn cách ra từng phần rõ ràng: Thông tin chung, Cấu hình tín hiệu, Thông số hiển thị.
- **[34:00] Anh Sơn:** Cái trường "Độ phân giải" nó thuộc về thông số màn hình với luồng tín hiệu, tự nhiên thảy chung với Trạng thái hoạt động. Gom lại cho đúng chỗ.
- **[35:15] Anh Sơn:** Rồi nhìn cái bảng danh mục này coi, bị cái gì mà các dòng bị phủ nguyên một lớp màu xanh đặc quánh vậy nè? Mù màu luôn á! Mất hết sọc vằn zebra của bảng rồi, chữ chìm nghỉm luôn.
- **[36:00] Đạt:** Chắc bị dính CSS đè của module khác qua rồi anh, để em kiểm tra lại class.
- **[37:20] Anh Sơn:** Coi lại độ rộng mấy cột trong bảng nữa. Cột Mã bộ điều khiển gì mà chiếm nửa màn hình, trong khi mấy cột thông tin quan trọng khác thì bị bóp dẹp lép. Chỉnh lại tỷ lệ cột cho nó cân đối, nhìn cho nó thoáng mắt.

### Phân đoạn 6: Giao Diện Kịch Bản, Độ Tương Phản & Thống Kê (40:00 - 48:00)
- **[40:05] Anh Sơn:** Mở cái giao diện kịch bản này lên nhìn nó xám xịt nguyên một màu luôn. Nền cũng xám, màn hình cũng xám, nhìn u ám quá. Chỉnh lại màu sắc cho nó sáng sủa, tương phản tốt hơn tí. Chữ thì nhỏ xíu, mắt mấy người lớn tuổi nhìn vô sao thấy được? Tăng kích thước chữ lên một chút.
- **[41:10] Anh Sơn:** Trên đầu này trống trơn nè, thêm cái tiêu đề vô, ví dụ "Kịch bản hiển thị màn hình". Rồi cái trạng thái gì mà ghi mỗi chữ "Bình thường" trơ trọi? Ai biết bình thường là cái gì? Phải ghi rõ "Kịch bản: Bình thường" hay "Trạng thái: Hoạt động".
- **[42:30] Anh Sơn:** Phía dưới này thêm mấy cái thông số tóm tắt vô: Tổng số input đang hiển thị nè, Tổng số màn hình đang sử dụng, rồi Số màn hình còn trống. Ví dụ có 32 màn hình mà đang xài 30 màn, thì hiện rõ "Còn 2 màn hình chưa sử dụng". Chứ không lẽ bắt người ta ngồi đếm từng ô trên tường màn hình?
- **[44:15] Anh Sơn:** Nhìn cái layout kịch bản phía dưới này: Thông tin kịch bản thì kéo dài ngoằng hết chiều ngang màn hình, còn cái khung Thuộc tính cửa sổ thì bị dồn ép chút xíu ở góc trái. Nhìn nó méo mó, không cân đối gì hết trơn! Thiết kế lại cho nó hài hòa.
- **[45:30] Anh Sơn:** Cái chữ "Thứ tự lớp" này là cái gì? Là `z-index` đúng không? Để vậy người dùng sao biết số càng lớn là nằm đè lên trên hay số nhỏ nằm trên? Người ta phải ngồi bấm tăng lên thử xem có nổi lên không, không được lại bấm giảm xuống, mất công thao tác. Thêm cái tooltip giải thích rõ ràng vô: "Số càng lớn thì cửa sổ càng hiển thị đè lên trên", hoặc làm mấy nút "Lên trên cùng", "Xuống dưới cùng" cho người ta bấm.
- **[47:00] Anh Sơn:** Tọa độ ma trận thống nhất bắt đầu từ (1, 1) rõ ràng theo Hàng - Cột, đồng bộ hết các màn hình cho dễ hiểu.

### Phân đoạn 7: Vị Trí Nút Chức Năng, Thu Gọn Panel & Khóa Nguồn Tín Hiệu (48:00 - 53:09)
- **[48:10] Anh Sơn:** Nút "Thêm mới" kịch bản tại sao lại nằm xa tít mù khơi bên góc này? Danh sách kịch bản một bên, nút thêm mới một bên, rê chuột mỏi tay luôn! Đưa cái nút Thêm mới về sát danh sách kịch bản. Để xa vậy người ta tưởng nút sửa hay nút gì bấm lộn hoài.
- **[49:15] Anh Sơn:** Còn nút "Lưu" nữa, phải để ở vị trí cố định luôn thấy (sticky bar). Người ta sửa xong là bấm Lưu liền được, chứ đừng bắt cuộn chuột lên xuống tìm nút Lưu.
- **[50:00] Anh Sơn:** Mấy cái panel bên cạnh (danh sách kịch bản, thuộc tính) làm thêm nút thu gọn (splitter / collapse) đi. Khi người ta muốn coi toàn cảnh bức tường màn hình lớn, người ta bấm thu gọn lại cho màn hình nó bung to ra coi cho sướng mắt.
- **[51:10] Anh Sơn:** Khi bấm vô một kịch bản hoặc một nguồn tín hiệu ở danh sách bên trái, thì trên cái khung màn hình bên phải cái ô tương ứng nó phải sáng lên (highlight) để người ta biết là đang chọn ô nào. Ngược lại bấm vô ô màn hình thì danh sách bên kia cũng phải sáng lên theo.
- **[52:00] Anh Sơn:** Còn cái nguồn tín hiệu nữa: một nguồn tín hiệu (ví dụ camera) có được kéo vô 2 ô màn hình cùng lúc không? Nếu hệ thống không cho phép, thì khi nguồn đó đã được kéo vô màn hình rồi, ở danh sách nguồn phải rào lại (disable/gray out) và ghi trạng thái "Đang sử dụng". Chứ không rào lại, người ta kéo trùng vô rồi hệ thống báo lỗi không hiển thị được, người ta lại tưởng phần mềm bị hư!
- **[52:45] Đạt:** Dạ em hiểu rồi, rào nguồn đã dùng lại để tránh lỗi hiển thị.
- **[53:05] Anh Sơn:** Ừ, làm cho chuẩn chỉ lại hết mấy chỗ đó đi. Rồi, nghỉ thôi!

---

## 3. Bảng Phân Công Nhiệm Vụ & Định Tuyến Prompt Thực Thi

Để tuân thủ quy tắc SSOT (Rule 19.98 & Rule 19.52), toàn bộ các yêu cầu trên được tách thành 2 Prompt thực thi độc lập:

| # | Prompt File | Tầng | Phạm vi triển khai chính |
|---|---|---|---|
| 1 | [`videowall-decoupling-equipment-va-phan-quyen-multi-cluster-be-prompt.md`](../Prompt/videowall-decoupling-equipment-va-phan-quyen-multi-cluster-be-prompt.md) | **Backend WebAPI & CSDL** | - Decouple `VwController` khỏi thông tin IP/User/Pass, chuyển sang liên kết khóa ngoại `EquipmentId` (SSOT với Module Equipment).<br>- Mở rộng `VwWallPermission` và API phân quyền hỗ trợ gán hàng loạt `UserIds` / Nhóm người dùng.<br>- Bổ sung mô hình dữ liệu đa cụm màn hình `ClusterId` / `WallId`.<br>- Tối ưu hóa đọc `Model` kích hoạt adapter ISAPI versioning.<br>- API kiểm tra trạng thái nguồn tín hiệu đang sử dụng (`IsUsed` / `ActiveWindowCount`). |
| 2 | [`videowall-visual-screen-layout-ux-flow-va-i18n-fe-prompt.md`](../Prompt/videowall-visual-screen-layout-ux-flow-va-i18n-fe-prompt.md) | **Frontend WebVue 3** | - Tái thiết kế luồng "Chia vùng": Popup chọn chế độ mẫu (1-1, 2x2, 3x3, Custom) trước khi áp dụng + Lưu template.<br>- Component trực quan dùng chung `ScreenLayoutRenderer`, tọa độ Top-Left chuẩn `H1-C1`.<br>- Form phân quyền Multi-user (`el-select multiple`) và cân đối tỷ lệ cột.<br>- Dashboard 2 cột (Controller vs Screen) chống "lé mắt".<br>- Sắp xếp Tab index top-to-bottom, divider chia nhóm, gom nhóm nguồn tín hiệu.<br>- Sửa lỗi Solid Blue Overlay Bug và điều chỉnh độ tương phản, font-size.<br>- Cân đối layout kịch bản, tooltip "Thứ tự lớp" (z-index), nút Lưu cố định, thu gọn panel (Splitter).<br>- Highlight liên kết trực quan giữa danh sách và khung màn hình, rào nguồn tín hiệu đã dùng.<br>- Chuẩn hóa 100% i18n với tiền tố `vw_`. |

