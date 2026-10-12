---
tier: A
read: full
source:
  - ../../ShareData/_source/Đường Số 10.m4a (42:00)
date: 12-10-2026
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: VideoWall (Kiến trúc Controller Cha - Con, Mô hình hóa Topology & Cổng In/Out, Ma trận 32 màn hình 8x4, Phân quyền xem/điều khiển và Cơ chế Reset Default Layout)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Fullstack / VideoWall)
  - Đạt (Dev Backend / WebAPI & Database)
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ Tường Màn Hình (VideoWall)
### (Nguồn từ tệp ghi âm: Đường Số 10.m4a — Phần trích đoạn VideoWall)

- **Ngày ghi nhận:** 12/10/2026
- **Thời lượng phần VideoWall:** ~22 phút (trích xuất từ tệp 42:00)
- **Người tham gia:** Anh Sơn, Hiếu, Đạt
- **Chủ đề chính:**
  1. **Mô hình Controller Cha - Con:** Phân tầng quản lý, controller cha điều phối, controller con gắn trực tiếp cổng vật lý.
  2. **Topology & Gán cổng In/Out:** Bỏ cách gán cổng cứng cũ, mô hình hóa số lượng cổng in/out trên từng controller, đánh ID cổng để tự động nhận diện màn hình.
  3. **Ma trận 32 màn hình ($8 \times 4$):** Ghép lưới chuẩn 8 cột $\times$ 4 hàng, scale ngang/dọc, chia 2 cụm controller độc lập bên trái/phải.
  4. **Phân quyền & Điều khiển cửa sổ (Window Roaming):** Chặn điều khiển đối với tài khoản chỉ xem; bổ sung nút **Reset về Default Layout** khi kéo thả cửa sổ bị lỗi/lệch.
  5. **Ghi log thao tác:** Ghi nhận log (Signal/Socket) cho mọi thao tác điều khiển trên tường màn hình.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Mô hình Controller Cha - Con & Quản lý Cổng vật lý
- **Vấn đề cấu hình cũ:** Trước đây khai báo input/output cứng trong bảng riêng, gộp chung một đống cổng không có phân cấp, dẫn đến việc đổi cổng hoặc thêm controller làm rối toàn bộ logic.
- **Thống nhất mô hình mới:**
  - Khai báo danh sách Controller có quan hệ Cha - Con (Master - Slave).
  - Controller cha quản lý điều phối chung. Controller con gắn trực tiếp vào các card/slot output của màn hình vật lý.
  - Mỗi Controller khai báo rõ: Số lượng cổng vào (Input), số lượng cổng ra (Output), cổng mạng điều khiển.
  - **Bắt buộc đánh ID định danh cho từng cổng Output:** Giúp phần mềm tự động map chính xác cổng vật lý nào đang cắm vào màn hình (Screen ID) nào trong ma trận.

### 1.2. Ma trận màn hình ghép 32 màn ($8 \times 4$) & Bố cục 2 cụm
- **Quy chuẩn kích thước:** Ma trận chuẩn của dự án là 32 màn hình ($8 \text{ cột} \times 4 \text{ hàng}$).
- **Xử lý số lượng lẻ:** Nếu phát sinh số lượng lẻ (ví dụ 33 màn) thì giao diện bị vỡ khung, bắt buộc phải tăng thêm cả hàng hoặc cả cột chứ không để lẻ từng màn.
- **Tách 2 cụm độc lập:** Cho phép cấu hình chia tường màn hình thành 2 cụm controller độc lập (cụm bên trái và cụm bên phải, ví dụ mỗi bên 16 màn hoặc quản lý theo khu vực riêng biệt). Hai cụm này hoạt động độc lập hoàn toàn.

### 1.3. Phân quyền hiển thị & Cơ chế Reset Layout (Window Roaming)
- **Phân quyền thao tác:** Phân biệt rõ nhóm tài khoản **Chỉ xem (View-only)** và nhóm **Điều khiển (Operator/Admin)**. Tài khoản chỉ xem bị chặn cứng quyền mở/đóng và kéo thả cửa sổ.
- **Cơ chế Reset về Default Layout:**
  - Khi người vận hành kéo thả, phóng to thu nhỏ cửa sổ camera/nguồn hình ảnh trên ma trận màn hình, nếu bị lỗi hoặc muốn quay lại bố cục chuẩn ban đầu thì có nút **Reset về mặc định (Reset to Default)** để bung lại toàn bộ layout ban đầu ngay lập tức.
  - Giới hạn kéo thả cửa sổ trong phạm vi màn hình được phân quyền, không cho kéo vượt qua ranh giới controller khác nếu không có quyền.
- **Ghi log kiểm toán thao tác:** Bắt buộc ghi log (cả phía Signal và Socket) mọi hành động bật/tắt nguồn, mất tín hiệu, gọi kịch bản, di chuyển cửa sổ để phục vụ hậu kiểm.

---

## 2. Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript)

---

### [00:00 - 05:00] Rà soát VideoWall Controller & Bố cục hiển thị
- **Hiếu:** Hàng này không biết gì hết... Bữa hôm nọ kêu đổi chuyển hết cái này cho anh Đạt anh dễ thấy. Cắt cái này chuyển lên phía trước này...
- **Đạt:** Xong rồi đổi cái gì?
- **Hiếu:** Lúc trước anh kêu em đổi thành controller, nhưng mà em để lộn cái này ra phía trước nha...
- **Đạt:** Làm sao?
- **Hiếu:** Cái này em bỏ đúng không? Để em đẩy xuống dưới...
- **Đạt:** Ừ, bỏ nó đi.
- **Hiếu:** Ví dụ như người ta bỏ 6 cái, 8 cái, người ta bỏ 2 cái... Bấm vô double click lại.
- **Đạt:** Có nút mũi tên đi qua không? Kiểu lùi bước, tiến bước như Ctrl+Z á?
- **Hiếu:** Thì chọn rồi chọn lại thôi. Vừa chọn cái đó, bấm chọn lại lần nữa là nó hủy.
- **Đạt:** Cái này chưa thao tác nhiều đúng không?
- **Hiếu:** Dạ, cái kịch bản hôm nọ em tính làm qua luồng server VideoWall Controller. Giờ mình làm qua luồng này luôn, FE kết nối controller luôn, server chuyển tiếp qua...
- **Đạt:** Cái này không có hiện thông tin gì hả? Chỉ hiện mỗi cái mã với cái tên hả?
- **Hiếu:** Dạ, ý anh là cái này hả? Chỗ này nó có hiện nguồn tín hiệu, nếu mất tín hiệu thì báo đỏ, có tín hiệu thì xanh.
- **Đạt:** Thử test bỏ tín hiệu thử coi.
- **Hiếu:** Nó đổi thành màu đen liền. Bây giờ bấm vô video controller này...

---

### [05:00 - 10:00] Ma trận màn hình ghép (32 màn hình: 8 cột x 4 hàng) & Bố cục 2 cụm
- **Hiếu:** Hoặc là cái đống này nè, đống data này... Cái này là scale màn hình ngang, scale màn hình dọc. Nhưng mà cẩn thận nha, cái này nó sẽ có tình huống là màn hình bị vuông hoặc bị lệch.
- **Đạt:** Bị lệch là sao?
- **Hiếu:** 24, 26 gì đó. Xong ở đây một cụm, ở đây một cụm đúng không?
- **Đạt:** Đúng rồi, hai bên nó sắp thành hai cụm này.
- **Hiếu:** Thì hai cái này nó có riêng lẻ nhau không?
- **Đạt:** Riêng lẻ chứ, nó là 2 cái controller riêng mà.
- **Hiếu:** Ý là nó có điều khiển qua lại, nó có switch được không hay là đứng độc lập hoàn toàn luôn?
- **Đạt:** Độc lập hoàn toàn chứ. Cứ bình thường thôi, có gì đâu. Quan trọng là lúc em khai báo á, em khai báo sao cho đúng. Có giới hạn số lượng ngang, dọc...
- **Hiếu:** Ủa nếu vậy thì khai báo controller cha làm sao?
- **Đạt:** Controller cha mốt bố trí lên trên cái kia á. Mấy cái này coi chừng cắm lộn cáp mạng với nhau.
- **Hiếu:** Giới hạn 8x4 là 32 màn, chứ không có lẻ được. Lỡ nó lên 33 màn là nó bị lẻ liền, lúc đó bắt buộc phải thêm 1 hàng hoặc 1 cột nữa.
- **Đạt:** Ừ thì phải tính toán trước chứ.

---

### [10:00 - 15:00] Mô hình Controller Cha - Con & Khai báo cổng Output
- **Hiếu:** Một controller ví dụ như 1 màn, tùy theo người quy định là người sử dụng sẽ chọn một cái danh sách controller tương ứng.
- **Đạt:** Cái này là màn hình cứng đúng không?
- **Hiếu:** Đúng rồi, màn hình vật lý cứng. Ví dụ như anh chia 7 cái đường, chia ra 2 cái khác nhau.
- **Đạt:** Nó có số lượng 8, 16, 24... Cái controller này là quản trị được 32 màn hình. Nếu mà em để nó bị sai cái tên thì sao?
- **Hiếu:** Dạ nó đâu có nhận diện được đâu anh.
- **Đạt:** Như cái màn hình này là chơi kiểu trực quan hóa luôn. Ví dụ như cái input này có 8 đầu ra, thì 8 đầu ra này sẽ tương ứng với 8 màn hình. Nếu mà đầu ra của nó là cái đầu ra khác thì sao?
- **Hiếu:** Thì mô hình hóa lại...
- **Đạt:** Mô hình hóa lại phải thay cả phần cứng nữa, tốn tiền lắm.
- **Hiếu:** Em cũng đang suy nghĩ là mô hình hóa lại đó, nhưng mà cái này có thằng nào phụ trách mô hình hóa không?
- **Đạt:** Cái đó là dạng mô hình controller rồi, cái đó để tính sau. Giờ cái sơ đồ topology này nó cũng gần tương tự như vậy á.
- **Hiếu:** Nhưng mà cái này switch qua lại được nè. Cái kia là cố định rồi.
- **Đạt:** Giờ add màn hình vô, add kết nối vô. 16 cổng chia ra 8 output đi. Nó phải chừa chỗ cho 8 output đó em chọn chứ!

---

### [15:00 - 17:30] Khai báo nguồn & Bỏ cơ chế gán cổng cũ
- **Hiếu:** Cái này em vô DB giùm em coi, coi lại cái bảng cái...
- **Đạt:** Bảng nào?
- **Hiếu:** Tại vì trước ở trong repo, trong backend á là nó có một cái thằng output. Nó sẽ khai báo cấu hình input với output của controller ở một bảng riêng. Nhưng mà khả năng nếu mà làm như vậy á thì...
- **Đạt:** Là bảng controller hả?
- **Hiếu:** Không phải. Khai báo nguồn gắn vào. Hiện tại nó đang làm cứng mấy cái output luôn rồi. Sơ đồ gắn cổng hồi trước là khai báo output input...
- **Đạt:** Cái khai báo output input đó bỏ đi rồi! Bỏ rồi. Nó có cái cụm cọng gì đó, xong rồi nó quất nguyên một nùi trong đó, rối quá trời rối!
- **Hiếu:** Nếu bây giờ làm theo hướng đó em thấy cũng được mà?
- **Đạt:** Thì bây giờ sẽ khai báo danh sách controller. Ứng với controller mình sẽ khai báo nó là có bao nhiêu cổng in, bao nhiêu cổng out. Có cổng mạng, ngay trong cấu hình luôn chứ không tách ra riêng nữa.
- **Hiếu:** Mà lúc đó có phải đánh ID cho từng controller, lộn, từng cái output không? Tại cổng ra...
- **Đạt:** Phải đánh chứ! Nó ra như thế nào phải sắp màn hình chứ. Không đánh ID sao biết màn hình nào cắm vô cổng nào? Mốt ở ngoài gắn như thế nào mình khai báo y chang vậy.

---

### [24:00 - 27:00] Trực quan hóa giao diện & Ghi log thao tác VideoWall
- **Hiếu:** Sao cái chỗ này nó sai vậy ta? Nó phải tab ở trên đúng không? Tab ở trên, tab con ở dưới...
- **Đạt:** Kiểu này quăng lên đây ai mỗi người làm một kiểu là nát bét luôn á!
- **Hiếu:** Mấy cái này build component dùng chung đàng hoàng lại.
- **Đạt:** Nát hết rồi, bây giờ làm sao? Bên VideoWall có rồi đúng không?
- **Hiếu:** Có rồi, nhưng mà nó đi qua một cái server riêng, xong bắn đi qua cái kênh này...
- **Đạt:** Giờ phải ghi log lại. Thiết bị được hay không được thì ghi nhận log lại.
- **Hiếu:** Ghi log bên Signal hả anh, hay bên Socket?
- **Đạt:** Bên Signal cũng ghi mà bên Socket cũng ghi. Nói chung có thao tác gì xảy ra là phải ghi log hết.
- **Hiếu:** Nhìn phần ghi log có gì coi lại sau. Cái này hôm nọ em tính làm Wireframe trước để hình dung. Nhưng hôm nọ chưa có làm Wireframe, đùng một phát bay vô code luôn.
- **Đạt:** Thấy chưa! Không làm Wireframe trước, bay vô code xong giờ quay lại sửa có phải cực không? Vẽ cái Wireframe ra trước, xem nó chạy đúng ý chưa rồi hẳn nhảy vô code.

---

### [27:00 - 33:00] Phân quyền VideoWall & Cơ chế Reset Default Layout
- **Hiếu:** Sợ sai hả anh?
- **Đạt:** Em hỏi ban đầu em làm cái đợt mấy trước đi, anh kêu con này có mở rộng không? Kêu không, làm cứng. Tự làm khó mình thôi chứ gì đâu!
- **Hiếu:** Bây giờ cái sơ đồ gán cổng này là sẽ trực quan hóa lại. Màn hình này sẽ cho tính theo cột với hàng. Trước có topology nhưng mà thấy không ok, nhiều quá nên bỏ luôn. Cho phân quyền test thử xem.
- **Đạt:** Phân quyền thì nó chặn luôn đúng không? Mấy cái thằng quản lý kịch bản này nọ mà theo phân quyền á.
- **Hiếu:** Đúng rồi, ví dụ như người ta chỉ có quyền xem thôi thì không cho điều khiển.
- **Đạt:** Tình huống này đi, account đó nó chỉ cho phân quyền 2 màn hình đầu thôi, thì người ta kéo từ màn 1 qua màn 6 có được không?
- **Hiếu:** Tại vì bản chất màn hình nó dính liền nhau...
- **Đạt:** Chưa test đúng không?
- **Hiếu:** Chưa test. Nhưng ví dụ nếu nó cho kéo đi, rồi giờ người ta muốn khôi phục lại như cũ, kéo lại thấy ghê quá, muốn bung lại chế độ mặc định ban đầu thì có cơ chế bung lại không?
- **Đạt:** Reset về default đúng không?
- **Hiếu:** Dạ, để em test lại cái này. Hỏi ý kiến được rồi.
- **Đạt:** Thao tác trong bộ điều khiển bên này có màn hình thì kệ nó. Kéo thả cửa sổ nhưng mà không có đổi được controller.

