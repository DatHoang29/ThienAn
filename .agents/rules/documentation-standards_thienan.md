---
name: documentation-standards
version: 1.0.0
priority: P1
trigger: model_decision
description: Documentation standards, single MasterPlan per subsystem, executable prompt lifecycle, and bug sheet reporting.
---

# 📄 Quy Chuẩn Tài Liệu, Plan & Báo Cáo Nghiệp Vụ — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định vị trí MasterPlan, cấu trúc file Prompt, báo cáo rà soát, phản hồi Sheet Bug và tài liệu đặc tả nghiệp vụ.

---

## 🗺️ 1. Quy Tắc MasterPlan & File Prompt (P0 Safeguards)

### Duy Nhất Một File MasterPlan Cho Mỗi Phân Hệ (Rule 19.52)
* Mỗi phân hệ (`ShareData`, `VideoWall`, `TMS`, `WOS`...) **CHỈ CÓ DUY NHẤT 1 FILE MASTERPLAN SỐNG**:
  - `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Plan/<PhânHệ>_MasterPlan.md`
* ⛔ **TUYỆT ĐỐI CẤM** tạo các file kế hoạch rời rạc theo ngày tháng (`Plan_20261001.md`, `MasterPlan_v2.md`).
* Mọi tiến độ, quyết định kiến trúc và phương án bị bác BẮT BUỘC cập nhật trực tiếp tại chỗ vào file MasterPlan này.

### Cấu Trúc File Prompt Thực Thi (`<task-slug>-prompt.md` - Section 13 & Rule 19.34)
* Vị trí lưu trữ: `DocBusinessThienAn/<DựÁn>/<PhânHệ>/Prompt/<task-slug>-prompt.md`.
* **Đầu file Markdown**: BẮT BUỘC ghi rõ đường dẫn và tên tệp prompt.
* **Mục cuối cùng của Prompt (Rule 19.23)**: BẮT BUỘC có mục `## Việc cuối — Cập nhật lại tài liệu gốc` (chỉ định cập nhật lại MasterPlan và tài liệu liên quan sau khi code change hoàn tất).
* ⛔ **CẤM tự động xóa file Prompt**: File prompt sau khi thực thi phải được giữ lại để lập trình viên review và đối chiếu; chỉ xóa khi người dùng yêu cầu trực tiếp.

---

## 📊 2. Quy Chuẩn Báo Cáo Rà Soát (Review Reports - Rule 19.14, 19.21)

1. **Viết Hướng Về Người Đọc, Không Hướng Về Người Viết (Rule 19.21)**:
   - Mở đầu nêu ngay kết luận, người đọc xem xong nắm được việc cần làm mà không cần phải hỏi lại AI.
   - ⛔ **CẤM** chèn các đoạn code thô, biểu thức nội bộ vào phần mở đầu để tự biện luận cho cách làm của mình.
2. **Khung Chuẩn 4 Trục**: Hiện trạng -> Đã làm -> Chưa làm -> Rủi ro & Đề xuất.
3. **Trạng Thái Công Việc (Rule 19.15)**:
   - CHỈ CÓ hai trạng thái: **"Đã làm"** hoặc **"Chưa làm"**. ⛔ KHÔNG CÓ nhãn "Làm sau".
   - Lý do chưa làm ghi rõ ràng ở cột riêng (`Vì sao chưa làm · Có chặn luồng không`).
4. **CẤM Viết Phụ Lục (Rule 19.14)**:
   - ⛔ TUYỆT ĐỐI KHÔNG tạo `Phụ lục A/B/C`, không tạo mục "nhật ký việc đã xử lý" thừa thãi làm loãng nội dung.
   - Quyết định quan trọng và phương án bị bác BẮT BUỘC ghi thẳng vào MasterPlan, không để trong báo cáo rà soát.

---

## 🐞 3. Quy Chuẩn Phản Hồi Sheet Bug Kiểm Thử (Section 21 & Rule 19.30–19.32)

1. **Cú Pháp Chuẩn Trong Cột "Ghi chú"**:
   ```text
   [ddMMyyyy]-[TênDev]: [Nội dung phản hồi kỹ thuật rõ ràng]
   ```
   * *Ví dụ:* `07102026-DatHP: Khóa ô mã đối tác khi sửa, bổ sung tooltip cảnh báo theo Issue 3.`
2. **CẤM Từ Ngữ Mơ Hồ, Tiếp Thị (Rule 19.31)**:
   - ⛔ KHÔNG dùng từ chung chung: "đã tối ưu", "đã fix lỗi", "đã xử lý xong".
   - BẮT BUỘC ghi rõ kỹ thuật: **bị gì, fix ở đâu, phương án xử lý cụ thể ra sao**.
3. **CẤM Gộp Ghi Chú Issue (Rule 19.32)**:
   - Mỗi Issue kiểm thử phải ghi độc lập, riêng biệt. Không gom 3-4 issue vào chung 1 dòng phản hồi.

---

## 📐 4. Soạn Thảo Đặc Tả Nghiệp Vụ (Specification Standards - Section 22)

1. **Khảo Sát Thực Tế Trước Khi Viết**: Kiểm tra schema DB thật trên staging qua DAB MCP và codebase trước khi viết spec.
2. **Khung Chuẩn 4 Giai Đoạn**: Khởi tạo/Cấu hình -> Thu thập/Kích hoạt -> Xử lý/Biến đổi -> Đồng bộ/Xuất dữ liệu.
3. **Trực Quan Hóa Bằng Mermaid Flowchart**: Dùng `flowchart TD` (chiều dọc), bo tròn node xử lý, quote các nhãn có ký tự đặc biệt.

---

## 🎙️ 5. Quy Chuẩn Biên Soạn Transcript Cuộc Họp / Ghi Âm (Transcript Standards)

1. **Cấu Trúc Tệp Transcript Chuẩn (Tier A)**:
   - **Frontmatter**: `tier: A`, `read: full`, `source` (đường dẫn audio gốc), `date`, `model`, `status`, `topic`, `participants`.
   - **Quy tắc ghi `participants`**: Chỉ giữ vai trò của **Tech Lead** (ví dụ: `Anh Sơn (Tech Lead)`), còn các lập trình viên chỉ ghi ngắn gọn là **`(Dev)`** (ví dụ: `Hiếu (Dev)`, `Đạt (Dev)`). ⛔ **CẤM** tự chia nhỏ vai trò của Dev thành `Dev Backend / Service Worker & Frontend` hay `Dev Backend / WebAPI & Database`, vì mọi Dev đều cover full toàn bộ các tầng.
   - **Mục 1**: Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary theo các chủ đề chính).
   - **Mục 2**: Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript từng mốc timeline, giữ nguyên ngữ cảnh và lời thoại thực tế của các thành viên).
2. ⛔ **CẤM / KHÔNG CẦN TẠO MỤC "Ma trận phân công công việc (Action Items)"**:
   - Trong transcript KHÔNG tạo bảng phân chia "Người phụ trách - Hạng mục công việc - Chi tiết kỹ thuật".
   - Chỉ cần tóm tắt quyết định kỹ thuật cốt lõi (Mục 1) và giữ kịch bản đối thoại chi tiết (Mục 2).
3. **Tách Riêng Theo Từng Phân Hệ**:
   - Khi một cuộc họp/tệp ghi âm thảo luận nhiều phân hệ (ví dụ `VideoWall`, `ShareData`...), **BẮT BUỘC bóc tách thành các file transcript độc lập** và lưu vào thư mục `doc/transcript/` của từng phân hệ tương ứng, kèm theo cập nhật `00-catalog.md`.
4. **Quy Trình Kỹ Thuật Bắt Buộc Khi Nghe & Trích Xuất Audio (High-Precision Audio Transcription)**:
   - **Cắt nhỏ file âm thanh (`Audio Chunking` $\le 10\text{–}15$ phút/đoạn):**
     - ⛔ **CẤM** nạp nguyên một file ghi âm dài ($> 15$ phút) vào công cụ multimodal (`view_file`) trong một lần gọi (gây loãng sự chú ý / attention dilution ở nửa cuối file và làm giảm độ chính xác nhận diện từ).
     - **BẮT BUỘC** cắt file gốc thành các đoạn ngắn **10–15 phút** (bằng `ffmpeg` hoặc `swift` `AVAssetExportSession` trên macOS) lưu vào thư mục tạm `scratch/`, nghe từng đoạn ở độ phân giải từng giây, và **xóa sạch các file audio cắt tạm** ngay sau khi hoàn tất.
   - **Phân tách kênh người nói & Lọc tạp âm nền (Speaker Separation & Background Chatter Filtering):**
     - Khi băng ghi âm có tiếng ồn văn phòng hoặc có cuộc hội thoại khác ở bàn bên cạnh lọt vào mic (overlapping speech), **BẮT BUỘC** đối chiếu giọng nói và mạch câu chuyện để tách riêng lời thoại của nhóm họp chính; ⛔ **TUYỆT ĐỐI KHÔNG** chép nhầm câu nói của người ở bàn khác vào lời thoại cuộc họp.
   - **Chống suy diễn thuật ngữ theo quán tính ngữ cảnh (Zero Context-Bias Hallucination):**
     - Nghe chính xác từng âm tiết các thuật ngữ kỹ thuật (`Kafka`, `NATS`, `API`, `byte[]`, `Dictionary`, `SystemConfigData`, `RadioButton`...). ⛔ **CẤM** tự ý thay thế thuật ngữ nghe được bằng thuật ngữ quen thuộc trong tài liệu cũ (như nghe `Kafka` tự đổi thành `NATS`), và không biến câu lấy ví dụ sang phân hệ khác (như phân hệ `Mail`) thành yêu cầu kỹ thuật của phân hệ đang xét.

---

## 📋 6. Phân Biệt Rõ 3 Loại: Báo Cáo Rà Soát vs. Mô Tả Công Việc vs. Kết Quả Công Việc

> 🔴 **TRÁNH NHẦM LẪN KHI VIẾT:** Khi người dùng yêu cầu viết "báo cáo", "mô tả công việc" hoặc "kết quả công việc", AI **BẮT BUỘC** đối chiếu bảng dưới đây để dùng đúng cấu trúc và định dạng của từng loại:

| Tiêu chí | Loại 1: Báo cáo rà soát (Review Report) | Loại 2: Mô tả công việc (Task Description) | Loại 3: Kết quả công việc (Work Results) |
| :--- | :--- | :--- | :--- |
| **Nơi sử dụng** | Tài liệu Markdown kỹ thuật hoặc trả lời phân tích trên khung chat. | Ô **MÔ TẢ CÔNG VIỆC** trên hệ thống quản lý Task (Task Portal). | Ô **KẾT QUẢ CÔNG VIỆC** trên hệ thống quản lý Task (Task Portal). |
| **Mục đích** | Đối chiếu hiện trạng code/DB với Spec & Transcript, chỉ ra việc đã làm / chưa làm / rủi ro. | Nêu đề bài, phạm vi và danh sách các đầu mục tính năng cần thực hiện của Task/Sprint. | Nhật ký cập nhật kết quả thực hiện cụ thể theo **từng mốc ngày hoàn thành**. |
| **Chia mốc ngày (`dd/MM/yyyy` + `===`)** | ❌ Không chia theo kiểu nhật ký Task Portal. | ❌ **KHÔNG** ghi ngày tháng `dd/MM/yyyy`, **KHÔNG** dùng dòng `==============================`. | ✅ **BẮT BUỘC** có dòng ngày `dd/MM/yyyy` ở đầu mỗi đợt và ngăn cách giữa các ngày bằng `==============================`. |
| **Cách trình bày** | Khung 4 trục: Hiện trạng → Đã làm → Chưa làm → Rủi ro & Đề xuất (Bảng Markdown). | Đánh số đầu mục (`1.`, `2.`, `3.`...) kèm phạm vi (`FE`, `BE`, `Service`) và gạch đầu dòng `- ` tóm tắt yêu cầu. | Dưới mỗi ngày `dd/MM/yyyy`: đánh số đầu mục liên tiếp (`1.`, `2.`, `3.`...) kèm phạm vi (`FE`, `BE`, `Service`) và gạch đầu dòng `- ` chi tiết kết quả; hoặc `- ` trực tiếp cho việc Support/Verify. |
| **Thứ tự sắp xếp ưu tiên** | Theo luồng kiến trúc hoặc mức độ rủi ro. | ✅ **Việc quan trọng để ở ĐẦU** (`Service / Worker`, Kiến trúc kết nối, Nghiệp vụ/Bảo mật `BE`); **FE giao diện & Đa ngôn ngữ để ở CUỐI**. | ✅ **Việc quan trọng để ở ĐẦU** (`Service / Worker`, Kiến trúc kết nối, Nghiệp vụ/Bảo mật `BE`); **FE giao diện, Đa ngôn ngữ & Support/Verify để ở CUỐI**. |
| **Mã nội bộ (`R1`, `R2`, `BE-7`...) & Việc dọn code** | Được phép dẫn chiếu mã task của MasterPlan khi phân tích kỹ thuật. | ⛔ **CẤM** ghi mã nội bộ (`R1`, `R2`, `R5`, `R7`, `BE-7`...) và **CẤM** liệt kê việc dọn dẹp code. | ⛔ **CẤM** ghi mã nội bộ (`R1`, `R2`, `R5`, `R7`, `BE-7`...) và **CẤM** liệt kê việc dọn dẹp code. |

---

### 6.1. Quy Chuẩn Chi Tiết Cho Loại 2: Mô Tả Công Việc (Ô "MÔ TẢ CÔNG VIỆC")
1. **Bản chất**: Tóm tắt phạm vi và danh sách các đầu mục tính năng sẽ triển khai trong Task/Tuần.
2. **Thứ tự sắp xếp bắt buộc (Quan trọng lên đầu — Giao diện/Ngôn ngữ xuống cuối)**:
   - **Đặt ở ĐẦU**: Các tính năng cốt lõi về **Service / Worker**, **Tầng tích hợp kết nối (API / Kafka / NATS / File)**, **Phân quyền / Bảo mật / Nghiệp vụ Backend trọng tâm**.
   - **Đặt ở CUỐI**: Các hạng mục **Giao diện Frontend (UI / Component / Tab)** và **Đa ngôn ngữ (i18n / thuật ngữ)**.
3. **Định dạng & Văn phong ngắn gọn**:
   - Tiêu đề đầu mục ngắn gọn (`1.`, `2.`, `3.`...) kèm phạm vi (`(Service / BE)`, `(BE)`, `(FE)`).
   - Mỗi gạch đầu dòng `- ` chỉ **1 câu ngắn gọn** (15–25 từ), **KHÔNG** liệt kê tên file, tên biến code hoặc danh sách tham số dài dòng trong ngoặc.
   - **Viết sát dòng (KHÔNG để dòng trống ở giữa)**: Các dòng viết liền kề nhau (chỉ xuống dòng đơn `\n`) để khi copy-paste vào trình soạn thảo của Task Portal không bị sinh ra 2 lần ngắt dòng thừa.
   - **KHÔNG** chia mốc ngày `dd/MM/yyyy`, **KHÔNG** có vạch `==============================`.
   - ⛔ **CẤM** ghi mã ký hiệu nội bộ (`R1`, `R2`, `R5`...) và **CẤM** đưa việc dọn dẹp code.
4. **Mẫu chuẩn Mô tả công việc**:
   ```text
   1. Tách tầng tích hợp xử lý kết nối (Service / BE)
   - Xây dựng tầng điều phối kết nối độc lập hỗ trợ gửi dữ liệu qua API, Kafka hoặc xuất File theo cấu hình hệ thống.
   2. Cấu hình truy vấn nguồn dữ liệu động cho SuperAdmin (BE / FE)
   - Xây dựng giao diện và API cấu hình truy vấn nguồn dữ liệu riêng cho SuperAdmin, tích hợp kiểm tra an toàn SQL và ghi nhận Audit Log.
   3. Xây dựng Component Lập lịch dùng chung và chuẩn hóa giao diện, đa ngôn ngữ (FE)
   - Tách component lập lịch dùng chung, đồng bộ cấu trúc Tab phân cấp và chuẩn hóa đa ngôn ngữ Anh - Việt trên toàn phân hệ.
   ```

---

### 6.2. Quy Chuẩn Chi Tiết Cho Loại 3: Kết Quả Công Việc (Ô "KẾT QUẢ CÔNG VIỆC")
1. **Bản chất**: Báo cáo kết quả đã thực hiện được tách bạch theo từng mốc ngày hoàn thành.
2. **Thứ tự sắp xếp & Chia đều theo từng ngày trong tuần**:
   - **Đặt ở ĐẦU**: Các đầu mục trọng tâm về **Service / Worker**, **Tầng kết nối**, **Phân quyền / Bảo mật / Nghiệp vụ Backend**.
   - **Đặt ở CUỐI**: Các hạng mục **Giao diện Frontend (Component UI, Tab)**, **Đa ngôn ngữ (i18n)** và các công việc **Support / Verify**.
   - **Chia đều theo mỗi ngày trong tuần (Daily Breakdown)**: Khi task kéo dài 1 tuần, **BẮT BUỘC chia đều các đầu mục theo từng ngày làm việc trong tuần** (được phép tách nhỏ tính năng lớn thành các bước theo ngày để ngày nào cũng có đầu mục kết quả rõ ràng).
3. **Định dạng & Văn phong ngắn gọn**:
   - Tách bạch rõ **ngày/tháng/năm hoàn thành (`dd/MM/yyyy`)** đặt trên 1 dòng riêng ở đầu mỗi ngày.
   - Giữa các mốc ngày khác nhau ngăn cách bằng dòng `==============================`.
   - Dưới mỗi mốc ngày:
     - Đánh số thứ tự đầu mục tính năng **liên tiếp xuyên suốt các ngày** (`1.`, `2.`, `3.`, `4.`...) kèm phạm vi (`Service / BE`, `BE`, `FE`...).
     - **Ngắn gọn, súc tích**: Tiêu đề ngắn (4–8 từ); mỗi gạch đầu dòng `- ` chỉ viết **1 câu ngắn** mô tả kết quả chính, **KHÔNG** liệt kê tên file (`.vue`, `.cs`), tên bảng/cột hay biến code dài dòng.
     - **Viết sát dòng (KHÔNG chèn dòng trống giữa các dòng)**: Toàn bộ các dòng (ngày tháng, tiêu đề, gạch đầu dòng `- `, vạch `==============================`) **BẮT BUỘC viết sát liền nhau (không có dòng trống ở giữa)** để khi copy-paste vào trình soạn thảo Task Portal không bị sinh ra 2 lần ngắt dòng.
   - ⛔ **CẤM** ghi mã ký hiệu nội bộ (`R1`, `R2`, `R5`, `R7`, `BE-7`, `G.1`...) và **CẤM** liệt kê các việc dọn dẹp code.
4. **Mẫu chuẩn Kết quả công việc**:
   ```text
   29/09/2026
   1. Service giao tiếp thiết bị (Worker VideoWall)
   - Xây dựng Worker Service độc lập kết nối điều khiển phần cứng qua chuẩn ISAPI của Hikvision với cơ chế xác thực Digest Auth.
   - Tích hợp hàng đợi bản tin NATS trung gian giữa WebAPI và Worker để tiếp nhận và thực thi các lệnh điều khiển bất đồng bộ, chống gây nghẽn WebAPI.
   2. Phân quyền VideoWall (BE / FE)
   - Xây dựng API và cơ chế phân quyền truy cập tường màn hình chi tiết theo Người dùng và Tổ chức (User/Org).
   - Bổ sung kiểm soát quyền theo từng ô màn hình (Grid Cell) và giới hạn quyền thao tác chuyển đổi kịch bản.
   ==============================
   01/10/2026
   3. Công cụ WPF kiểm thử thiết bị (ITS.VideoWall.WPF)
   - Xây dựng ứng dụng WPF chuyên dụng kiểm thử trực tiếp kết nối và cấu hình với thiết bị điều khiển phần cứng.
   - Thiết kế giao diện Canvas trực quan hiển thị ma trận tường màn hình, hỗ trợ kiểm tra chia cửa sổ và gán nguồn tín hiệu.
   - Bổ sung bộ giả lập gửi và phân tích các gói tin điều khiển XML/JSON theo chuẩn ISAPI.
   ==============================
   06/10/2026
   - Support làm phần nhận sự kiện sự cố, bắn tín hiệu qua VideoWall.
   ==============================
   08/10/2026
   - Verify lại tính năng lập lịch và nhận tín hiệu khi cấu hình nhận sự kiện.
   ```


