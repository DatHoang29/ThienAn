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

### Phạm Vi Cốt Lõi Của MasterPlan — Chỉ Báo Cáo Luồng Nghiệp Vụ Chính (Rule 19.52)
* MasterPlan là tài liệu kế hoạch & kiến trúc nghiệp vụ cấp cao của phân hệ:
  - **Nội dung ĐƯỢC PHÉP có trong MasterPlan**:
    1. Hiện trạng kiến trúc nghiệp vụ phân hệ, phân tích luồng dữ liệu (Data Flow), hợp đồng dữ liệu, danh mục gói tin / thiết bị / màn hình.
    2. Danh sách tính năng và phân rã công việc nghiệp vụ hệ thống (`R1..Rn`, `BE-*`, `FE-*`, các luồng Outbound/Inbound...).
    3. Trạng thái tiến độ tổng quan: chỉ dùng 2 trạng thái **"Đã làm"** hoặc **"Chưa làm"** kèm tiêu chí nghiệm thu cấp hệ thống.
    4. Các quyết định kiến trúc nghiệp vụ quan trọng và phương án bị bác (ADR - Architectural Decision Record) được chốt qua cuộc họp/spec.
  - ⛔ **TUYỆT ĐỐI CẤM đưa vào MasterPlan**:
    1. **Các quy chuẩn lập trình / coding conventions / linter rules**: Như "Cấm hardcode text mẫu / đưa vào i18n (Rule 20.15)", "Đổi tên component sang camelCase (Rule 20.14)", "Cấm toán tử !! (Rule 20.8)", "Cấm !important (Rule 20.6)"... Mọi quy chuẩn này **BẮT BUỘC nằm tại `.agents/rules/` (SSOT)**.
    2. **Nhật ký sửa code / fix bug chi tiết sau review**: Như nhật ký sửa CSS, đổi class, tăng width modal, bỏ padding footer, căn lề, sửa thanh tab, thêm computed, escape ký tự `@`... Những thay đổi kỹ thuật này được thể hiện và đối chiếu qua **Git Diff** và **bộ test tự động (Playwright/xUnit)**.
    3. **Các phần dạng "Phần VII — Nhật ký xử lý & đồng bộ sau review..."**: MasterPlan không phải là changelog, commit log hay bug tracking sheet. Việc đưa chi tiết fix code lặt vặt vào MasterPlan làm loãng tài liệu kiến trúc nghiệp vụ và vi phạm nguyên tắc tập trung quy tắc tại SSOT.

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
5. **Phạm Vi Đối Chiếu**: Chỉ đối chiếu code working tree, test và DB local; không lấy staging làm phát hiện.

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

1. **Khảo Sát Thực Tế Trước Khi Viết**: Kiểm tra schema DB thật trên môi trường local (hoặc staging khi người dùng yêu cầu) qua DAB MCP và codebase trước khi viết spec.
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
| **Chia mốc ngày (`dd/MM/yyyy` + `===`)** | ❌ Không chia theo kiểu nhật ký Task Portal. | ❌ **KHÔNG** ghi ngày tháng `dd/MM/yyyy`, **KHÔNG** dùng dòng `==============================`. | ✅ **BẮT BUỘC** ngăn cách giữa các ngày bằng dòng `==============================` (dòng đầu tiên KHÔNG cần `==============================`). |
| **Cách trình bày** | Khung 4 trục: Hiện trạng → Đã làm → Chưa làm → Rủi ro & Đề xuất (Bảng Markdown). | Đánh số đầu mục (`1.`, `2.`, `3.`...) kèm phạm vi (`FE`, `BE`, `Service`) và gạch đầu dòng `- ` tóm tắt yêu cầu. | Cú pháp mỗi ngày: Dòng 1 `[Tên phạm vi]: dd/MM/yyyy` (ví dụ `Backend: 13/10/2026` — ⛔ **CẤM** thêm chữ "Phạm vi: " hay "Scope: "), Dòng 2 `[Tên công việc]` (bỏ ngày tháng ở dòng dưới), viết sát liền dòng (chỉ ngắt dòng đơn `\n`, KHÔNG để dòng trống ở giữa) với các gạch đầu dòng `- ` tóm tắt kết quả ngắn gọn. |
| **Thứ tự sắp xếp ưu tiên** | Theo luồng kiến trúc hoặc mức độ rủi ro. | ✅ **Việc quan trọng để ở ĐẦU** (`Service / Worker`, Kiến trúc kết nối, Nghiệp vụ/Bảo mật `BE`); **FE giao diện & Đa ngôn ngữ để ở CUỐI**. | ✅ **Việc quan trọng để ở ĐẦU** (`Service / Worker`, Kiến trúc kết nối, Nghiệp vụ/Bảo mật `BE`); **FE giao diện, Đa ngôn ngữ & Support/Verify để ở CUỐI**. |
| **Mã nội bộ, Tên class chi tiết & Việc dọn code** | Được phép dẫn chiếu mã task của MasterPlan khi phân tích kỹ thuật. | ⛔ **CẤM** ghi mã nội bộ (`R1`, `R2`...), **CẤM** khai báo chi tiết tên class/bảng (chỉ cần ghi khái quát `Service / BE`), và **CẤM** liệt kê việc dọn dẹp code. | ⛔ **CẤM** ghi mã nội bộ (`R1`, `R2`...), **CẤM** khai báo chi tiết tên class/bảng (chỉ cần ghi khái quát `Service / BE`), và **CẤM** liệt kê việc dọn dẹp code. |

---

### 6.1. Quy Chuẩn Chi Tiết Cho Loại 2: Mô Tả Công Việc (Ô "MÔ TẢ CÔNG VIỆC")
1. **Bản chất**: Tóm tắt phạm vi và danh sách các đầu mục tính năng sẽ triển khai trong Task/Tuần.
2. **Thứ tự sắp xếp bắt buộc (Quan trọng lên đầu — Giao diện/Ngôn ngữ xuống cuối)**:
   - **Đặt ở ĐẦU**: Các tính năng cốt lõi về **Service / Worker**, **Tầng tích hợp kết nối (API / Kafka / FTP)**, **Phân quyền / Bảo mật / Nghiệp vụ Backend trọng tâm**.
   - **Đặt ở CUỐI**: Các hạng mục **Giao diện Frontend (UI / Component / Tab)** và **Đa ngôn ngữ (i18n / thuật ngữ)**.
3. **Định dạng & Văn phong ngắn gọn**:
   - Tiêu đề đầu mục ngắn gọn (`1.`, `2.`, `3.`...) kèm phạm vi (`(Service / BE)`, `(BE)`, `(FE)`).
   - Mỗi gạch đầu dòng `- ` chỉ **1 câu ngắn gọn** (15–25 từ), không liệt kê tên class, tên bảng, tên file hay tham số chi tiết.
   - **KHÔNG** chia mốc ngày `dd/MM/yyyy`, **KHÔNG** có vạch `==============================`.
4. **Mẫu chuẩn Mô tả công việc**:
   ```text
   1. Tách tầng điều phối kết nối độc lập (Service / BE)
   - Xây dựng tầng điều phối kết nối độc lập hỗ trợ gửi dữ liệu qua API, Kafka hoặc FTP theo cấu hình hệ thống.
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
   - **Chia đều theo mỗi ngày trong tuần (Daily Breakdown)**: Khi task kéo dài 1 tuần, **BẮT BUỘC chia đều các đầu mục theo từng ngày làm việc trong tuần**.
3. **Định dạng & Bố cục chuẩn (Viết sát dòng, chỉ ngắt dòng đơn)**:
   - **Vạch ngăn cách**: Phân tách giữa các ngày bằng dòng `==============================` (**dòng đầu tiên của báo cáo TUYỆT ĐỐI CẤM có `==============================`**).
   - **Dòng 1: [Tên phạm vi]: dd/MM/yyyy (Gộp ngày vào phạm vi, bỏ chữ "Phạm vi:")**: Cú pháp `[Tên phạm vi]: dd/MM/yyyy` (ví dụ: `Backend: 13/10/2026`). ⛔ **TUYỆT ĐỐI CẤM** thêm tiền tố thừa như `Phạm vi: ` hay `Scope: `.
   - **Dòng 2: [Tên công việc] (Bỏ ngày bên dưới)**: Dòng thứ 2 ghi trực tiếp tên công việc (ví dụ: `Cơ chế truy vấn SQL động và kiểm tra an toàn cú pháp`), ⛔ **TUYỆT ĐỐI CẤM** lặp lại ngày tháng `dd/MM/yyyy:` hay mã `(BE)`, `(FE)` ở dòng này.
   - **Quy chuẩn nội dung theo Phạm vi**: Khi phạm vi là `Frontend`, toàn bộ các gạch đầu dòng bên dưới **BẮT BUỘC là công việc Frontend**, ⛔ **CẤM** đưa các việc trích xuất backend/CSDL vào scope Frontend.
   - **Viết sát dòng (KHÔNG để dòng trống ở giữa)**: Toàn bộ các dòng **BẮT BUỘC viết sát liền nhau (chỉ dùng ngắt dòng đơn `\n`, không chèn dòng trống)** để tránh sinh khoảng cách thừa làm bể layout Task Portal.
   - **Nội dung gạch đầu dòng**: Mỗi gạch đầu dòng `- ` viết 1 câu ngắn gọn, súc tích mô tả một kết quả nghiệp vụ cụ thể.
4. **Mẫu chuẩn Kết quả công việc**:
   ```text
   Backend / Frontend: 14/10/2026
   Cấu hình kênh và thông số kết nối đối tác
   - Mở rộng lưu trữ các giao thức kết nối (API, Kafka, FTP) cùng tham số kỹ thuật linh hoạt theo từng đối tác.
   - Cập nhật giao diện khai báo đối tác tự động hiển thị các trường cấu hình theo loại giao thức được chọn.
   ==============================
   Service / Backend: 15/10/2026
   Xây dựng tầng điều phối kết nối dịch vụ
   - Xây dựng tầng điều phối kết nối độc lập nhận dữ liệu JSON sau ánh xạ, tách rời logic vận chuyển khỏi quy trình trích xuất cốt lõi.
   - Hỗ trợ chuyển đổi định dạng và gửi dữ liệu theo từng loại kênh cấu hình: HTTP REST cho API, mảng byte cho Kafka hoặc FTP.
   ==============================
   Support / Verify: 16/10/2026
   Hỗ trợ phối hợp sự cố và xác minh hệ thống
   - Phối hợp hỗ trợ kiểm thử tính năng nhận sự kiện sự cố và bắn tín hiệu sang hệ thống liên kết.
   - Xác minh toàn trình luồng gửi nhận và hoàn thiện tài liệu kiểm thử.
   ```

---

## 🌐 7. Phạm Vi Áp Dụng Chú Thích Tiếng Anh Trong Ngoặc (Rule 20.13)

Quy ước chú giải tiếng Anh trong ngoặc `[Tiếng Việt] ([English])` CHỈ ÁP DỤNG cho tài liệu kỹ thuật Markdown (`.md`). Trên giao diện HTML/UI/i18n, ⛔ **CẤM** viết nhãn song ngữ lai (chi tiết xem [frontend-vue_thienan.md](./frontend-vue_thienan.md) Rule 20.13).

---

## 📑 8. Quy Chuẩn Tạo & Sửa Báo Cáo Tuần / Tài Liệu Excel (Zero-Corruption OpenXML - Rule 19.53, Rule 19.72)

1. **Bảo Toàn 100% Cấu Trúc Bit-Perfect Của Template Gốc**:
   - Khi tạo hoặc cập nhật file Báo cáo tuần Excel (`PCN_WeeklyReport_*.xlsx`) hoặc tài liệu mẫu OpenXML (`.xlsx`, `.docx`): **BẮT BUỘC đối chiếu cấu trúc XML/OpenXML với file gốc làm chuẩn và kiểm tra tính toàn vẹn (Zero Corruption) trước khi hoàn tất**.
   - Bảo toàn nguyên vẹn logo (`xl/drawings/drawing1.xml`, `xl/media/image1.png`), rich text màu (`<si><r><rPr>...`), font Times New Roman, border, mergeCells, hyperlinks.
2. ⛔ **CẤM Dùng `ElementTree.tostring()` Trên `sheet1.xml`**:
   - `xml.etree.ElementTree` trong Python tự động loại bỏ các namespace không gắn trên thẻ (`xmlns:x14ac`, `xmlns:xr2`, `xmlns:xr3`), làm hỏng khai báo `mc:Ignorable="x14ac xr xr2 xr3"`, đổi sai tiền tố quan hệ `r:id` thành `ns3:id` trên `<drawing>` và `<hyperlink>`, làm mất cờ `standalone="yes"`.
   - Hậu quả: Khi mở file trên Microsoft Excel sẽ bị bật pop-up cảnh báo lỗi phục hồi (recovery warning).
3. **Giải Pháp Chuẩn**:
   - Dùng **kỹ thuật Direct OpenXML Patching bằng Regex có chọn lọc** (`patch_sheet1_xml_dates_and_heights`) trên chuỗi XML nguyên gốc để chỉ thay đổi đúng chiều cao dòng `ht` và số ngày serial (`excel_date_serial`), giữ nguyên 100% từng byte namespace, tiền tố `r:id`, `xr:uid` và header.
   - Đối với `xl/sharedStrings.xml`: Cập nhật text trong các thẻ `<si>`, giữ nguyên các `si[0..5]` rich text của template gốc, và luôn xuất header chuẩn `<?xml version="1.0" encoding="UTF-8" standalone="yes"?>`.
4. **Kiểm Tra Tính Toàn Vẹn Bắt Buộc Trước Khi Hoàn Tất (`validate_excel_openxml`)**:
   - BẮT BUỘC chạy hàm kiểm tra `validate_excel_openxml()`:
     1. `z.testzip()` không có lỗi CRC/cấu trúc zip.
     2. Tất cả file `.xml` và `.rels` parse hợp lệ.
     3. `sheet1.xml` KHÔNG chứa các tiền tố namespace tự sinh `ns0:`, `ns1:`, `ns2:`, `ns3:`.
     4. `sheet1.xml` có đầy đủ thuộc tính chuẩn `r:id` cho `drawing` và `hyperlink`.
     5. Mọi tiền tố trong `mc:Ignorable` đều có khai báo `xmlns:<prefix>=`.
     6. Danh sách tệp bên trong ZIP khớp 100% với template chuẩn.
     7. Tải thử bằng `openpyxl` (hoặc Excel) mở thành công mà không phát sinh lỗi hoặc cảnh báo phục hồi.
     8. **Bảo toàn 100% công thức (Zero Formula Loss):** Tự động đối chiếu danh sách các ô có công thức (`<f>...</f>`) với template gốc. ⛔ **TUYỆT ĐỐI CẤM** xóa mất công thức hoặc thay thế bằng giá trị số/chuỗi tĩnh.

---

## 📝 9. Quy Chuẩn Viết Nội Dung Báo Cáo Tiến Độ & Báo Cáo Tuần — Chỉ Báo Cáo Thay Đổi Thực Tế (Rule 19.74)

1. **Nguyên Tắc Cốt Lõi — Chỉ Báo Cáo Những Gì Thực Tế Đã Làm Hoặc Đã Từng Tồn Tại**:
   - Khi viết báo cáo tuần, báo cáo tiến độ, mô tả/kết quả công việc (Task Portal), tài liệu kiến trúc hoặc phản hồi người dùng:
     - **Chỉ ghi nhận**: Những tính năng, thành phần, cấu trúc dữ liệu, API, module thực tế đã hoàn thành, đã xây dựng mới hoặc cải tiến.
     - **Với thao tác xóa / gỡ bỏ**: CHỈ ĐƯỢC GHI "xóa / gỡ bỏ [thành phần X]" nếu thành phần X đó **ĐÃ THỰC SỰ TỒN TẠI TRƯỚC ĐÓ TRONG HỆ THỐNG / CSDL THỰC TẾ** (ví dụ: bảng thừa đã có sẵn trong CSDL từ các phiên bản trước được DROP bỏ).

2. ⛔ **TUYỆT ĐỐI CẤM Ghi "Bỏ / Xóa [Thành Phần X]" Khi X Chưa Từng Tồn Tại Thực Tế**:
   - ⛔ **CẤM** ghi báo cáo theo kiểu *"Bỏ bảng trung gian X, gom vào..."* khi thực chất bảng X đó chỉ là một phương án nháp, ý tưởng thiết kế nội bộ hoặc chưa từng được tạo/triển khai trong CSDL trước đây.
   - Báo cáo chuẩn xác theo hướng **giải pháp thực tế được triển khai**:
     - ❌ **Sai:** `- Bỏ bảng trung gian ShareDataPacketExtractSql, gom trực tiếp câu lệnh SQL vào cấu hình gói tin (ExtractSql).`
     - ✅ **Đúng:** `- Tích hợp trực tiếp câu lệnh trích xuất dữ liệu động vào cấu hình gói tin và ánh xạ đối tác.`

3. **Mức Độ Chi Tiết Trong Báo Cáo Tuần Excel (`@xlsx` / `PCN_WeeklyReport_*.xlsx`) — Ghi Khái Quát Nghiệp Vụ, CẤM Viết Chi Tiết Code**:
   - **Chỉ ghi những gì mình làm / kết quả đạt được từ góc độ nghiệp vụ và tính năng hệ thống**.
   - ⛔ **TUYỆT ĐỐI CẤM** viết chi tiết triển khai code & tên component UI:
     - CẤM đưa tên hàm/phương thức C# (như `EnsureTablesCreated()`), tên class nội bộ, tên biến, cấu trúc bảng CSDL chi tiết (`SysOpConfig (ShareDataTracker)`), hay các công việc dọn dẹp code nội bộ vào báo cáo tuần.
     - CẤM đưa tên component kỹ thuật/giao diện nội bộ (như `Visual Query Builder`, `editPacket.vue` -> BẮT BUỘC ghi theo tính năng hệ thống: *Xây dựng tính năng tạo và tùy biến câu truy vấn trích xuất dữ liệu động*).
   - ⛔ **TUYỆT ĐỐI CẤM** đưa phân hệ không làm trong tuần vào báo cáo (tuần nào chỉ làm ShareData thì CHỈ BÁO CÁO ShareData, bỏ hẳn phân hệ khác).
   - ⛔ **CẤM** liệt kê lan man các việc phụ trợ/dọn dẹp kỹ thuật lặt vặt (như gom script SQL 00-05, chuẩn hóa cơ chế lưu vết cấu hình, thông luồng từng mã gói nhỏ...). Chỉ chắt lọc 2-3 gạch đầu dòng tính năng lớn cốt lõi.
   - **Kế hoạch tuần tiếp theo**: Bám sát nghiệp vụ thực tế: Theo dõi và khắc phục các vấn đề phát sinh theo mã task (ví dụ: `XD006.7.1`), xây dựng kịch bản kiểm thử và nghiệm thu chức năng, kiểm thử tích hợp trên môi trường nội bộ.
   - **Ví dụ đối chiếu**:
     - ❌ **Sai:** `- Xây dựng Visual Query Builder và tùy biến truy vấn SQL trích xuất động cho các gói tin, hỗ trợ đầy đủ các tham số hóa hệ thống.` $\rightarrow$ ✅ **Đúng:** `- Xây dựng tính năng tạo và tùy biến câu truy vấn trích xuất dữ liệu động.`
     - ❌ **Sai:** `- Chuyển đổi cơ chế lưu vết cấu hình động sang chuẩn SysOpConfig (ShareDataTracker), loại bỏ hàm tạo bảng động EnsureTablesCreated.` $\rightarrow$ ✅ **Đúng:** `- Tách độc lập tầng kết nối và điều phối giao thức truyền tải (WebAPI, Kafka, FTP) dùng chung cho toàn hệ thống.`
     - ❌ **Sai:** Để nguyên khối `Phân hệ Tường màn hình (VideoWall)` dù tuần đó không làm việc gì về VideoWall $\rightarrow$ ✅ **Đúng:** Chỉ để `Phân hệ Chia sẻ Dữ liệu (ShareData - ESHARE)`.

---

## ⏱️ 10. Quy Chuẩn Báo Cáo KPI Tháng & Định Mức Ngày Công (Rule 19.77)

1. **Chu Kỳ Báo Cáo & Ngày Báo Cáo**:
   - Báo cáo KPI được lập và đánh giá theo **chu kỳ 1 tháng** (định dạng `YYYY-MM`, ví dụ: `2026-09` hoặc `2026-10`).
   - **Ngày báo cáo** (ô `M7` Sheet 2): **BẮT BUỘC ghi ngày hôm nay** — ngày thực tế thao tác lập/chỉnh sửa báo cáo (sửa ngày nào ghi ngày báo cáo ngày đó, ví dụ sửa ngày 16/10/2026 thì ghi `16/10/2026`).

2. **Định Mức Tính Ngày Công (Man-Days) — Chuẩn 5.5 Ngày / Tuần**:
   - Quy chuẩn tính ngày công trong công ty: **1 tuần làm việc = 5.5 ngày công** (từ thứ 2 đến sáng thứ 7).
   - Tổng số ngày công trong kỳ báo cáo 1 tháng được phân bổ theo định mức thực tế của tháng ($\approx 20.5 - 22$ ngày công/tháng, ví dụ: 2 task chia 10.5 ngày và 10 ngày = 20.5 ngày công).
   - ⛔ **TUYỆT ĐỐI CẤM** tự ý thay đổi, hạ thấp, ghi đè hoặc đặt lại (revert) các giá trị ngày công mà người dùng đã khai báo hoặc điều chỉnh trong file KPI (tuân thủ nghiêm ngặt P0 Rule 34).

3. **Quy Tắc Định Dạng Dòng TỔNG KẾT (Cố Định Row 20) & Ẩn Task Dư (Zero Leftover Columns)**:
   - **Dòng TỔNG KẾT cố định tại Row 20:**
     - Dòng `TỔNG KẾT` trong Sheet `01. Tổng task` **BẮT BUỘC giữ nguyên cố định tại Row 20**, bảo toàn 100% định dạng dòng chuẩn từ file gốc `2026.08_KPI_TenHoTenLot.xlsx`:
       - Độ cao dòng: `ht="22.5" customHeight="1"`.
       - Merge cell: `mergeCell ref="A20:G20"`.
       - Định dạng ô (Styles): `A20` dùng `s="329"` (Segoe UI 11pt Bold, màu nền `fillId=28`, viền dày `borderId=91`), `B20..F20` dùng `s="330"`, `G20` dùng `s="331"`, `H20..I20` dùng `s="146"`, `J20..W20` dùng `s="147"`, `X20..AA20, AF20..AO20` dùng `s="148"`, `AB20..AE20` dùng `s="149"`.
       - Công thức tổng: `SUM(H13:H19)`, `SUM(I13:I19)`, `IFERROR(SUM(...)/SUM(...), 0)`.
       - **Viền trên đậm (Border Top = Medium) của dòng TỔNG KẾT:** Đường phân cách phía trên dòng `TỔNG KẾT` BẮT BUỘC phải là **đường viền đậm (`medium`) xuyên suốt toàn bộ các cột từ A đến AO**:
         - Dòng task cuối cùng trước các dòng ẩn (ví dụ Row 14 khi có 2 task): BẮT BUỘC áp dụng các style có viền đáy đậm `bottom="medium"` (`A14: s="333"`, `B14: s="334"`, `C14: s="335"`, `D14: s="336"`, `E14: s="337"`, `F14: s="338"` giống như Row 16 trong file gốc).
         - Các ô `A20:G20` của dòng TỔNG KẾT (Borders 91, 92, 93) BẮT BUỘC có thuộc tính viền trên `top style="medium"` để bảo đảm đường kẻ luôn đậm và liền mạch 100% với các cột H20..AO20.
     - ⛔ **TUYỆT ĐỐI CẤM** xóa bỏ dòng làm kéo dòng `TỔNG KẾT` lên hàng 14, 15... làm sai lệch vị trí row, sai lệch format dòng và làm vỡ tọa độ neo các nút drawing shapes (`drawing1.xml`).
   - **Xử lý các dòng task chưa sử dụng (dòng 15..19):**
     - Khi số lượng task thực tế ít hơn 7 (ví dụ có 2 task tại dòng 13, 14): Toàn bộ các dòng task mẫu còn lại (dòng 15, 16, 17, 18, 19) **BẮT BUỘC đặt thuộc tính ẩn dòng (`hidden="1"`)**.
     - **Dọn sạch cột dư thừa:** BẮT BUỘC xóa bỏ toàn bộ các giá trị mẫu dư thừa trên các dòng ẩn này (như chữ `DOC` ở `G15`, `DEV` ở `G16`, các giá trị tính toán mẫu thừa ở cột X-AO) để không gây sai lệch dữ liệu hay để lại rác khi unhide.
   - Giữ nguyên khoảng cách chuẩn 5 dòng trắng (dòng 21-25) sau dòng `TỔNG KẾT` trước khi đến bảng `THANG ĐIỂM KPI` (dòng 26-33).

4. **Quy Chuẩn Viết Mô Tả Task & "Phần Kết Quả Nổi Bật" (Cột G12 Sheet `02. Tổng KPI`) — Chuẩn Thô Gọn, Súc Tích & Dễ Hiểu**:
   - **Văn phong thô, ngắn gọn, súc tích:** Làm gì ghi nấy trực diện, gãy gọn, không văn vẻ hoa mỹ hay diễn đạt sáo rỗng.
   - ⛔ **TUYỆT ĐỐI CẤM Báo Cáo Quá Chi Tiết Kỹ Thuật Nội Bộ:**
     - ⛔ **CẤM** ghi chi tiết các bước xử lý code, truy vấn CSDL hay thao tác nội bộ (như *"xử lý luồng: lấy dữ liệu từ CSDL và chuyển đổi..."*).
     - Chỉ ghi ở mức tính năng nghiệp vụ rõ ràng: *"Chuyển đổi cấu trúc dữ liệu theo chuẩn đối tác yêu cầu"*, *"Triển khai gửi dữ liệu nối đuôi và truyền nhận tức thì"*.
   - ⛔ **TUYỆT ĐỐI CẤM Dùng Từ Ngữ Văn Vẻ Sáo Rỗng & Thuật Ngữ Hàn Lâm:**
     - ⛔ **CẤM** dùng các từ ngữ văn vẻ hoa mỹ (như *"làm chủ"*, *"vận hành mượt mà không bị nghẽn tải"*...).
     - ⛔ **CẤM** dùng các thuật ngữ quá hàn lâm, chuyên sâu khó hiểu (như *"ma trận"*, *"polling"*...) làm người ngoài ngành (HR, Quản lý) khó nắm bắt.
   - ⛔ **TUYỆT ĐỐI CẤM Dùng Từ Ngữ Mang Tính Cá Nhân Hóa Cảm Tính:**
     - ⛔ **CẤM** dùng các từ ngữ mang tính cá nhân hóa cảm tính như *"tự làm"*, *"tự xây dựng"*, *"tự mình"*. Báo cáo công việc trong tổ chức cần mang tính khách quan, trung tính: chỉ ghi *"làm"*, *"xây dựng"*, *"triển khai"*, *"phát triển"*.
   - ⛔ **TUYỆT ĐỐI CẤM Ghi Số Ngày Công / Thời Gian Cụ Thể Trong Nội Dung Task & Kết Quả Nổi Bật:**
     - ⛔ **CẤM** đưa các cụm từ chỉ số ngày thực hiện (như *"2 ngày"*, *"3 ngày"*, *"trong x ngày"*) vào nội dung mô tả task hoặc ô G12.
     - Toàn bộ thời lượng/ngày công đã được định lượng và quản lý riêng tại cột I ("Ngày công thực hiện"). Nội dung mô tả chỉ tập trung vào hành động và kết quả đạt được (ví dụ: *"Trực tiếp ra site kiểm thử và nghiệm thu thiết bị"* thay vì *"...(2 ngày)"*).
   - ⛔ **TUYỆT ĐỐI CẤM Ghi Nhận Fix Bug Lặt Vặt:**
     - ⛔ **CẤM** note các việc fix bug con, lỗi lặt vặt (như *"chuẩn hóa log cha - con và theo dõi fix bug kiểm thử F16"*) vào KPI.
     - Thay vào đó, ghi ngắn gọn mục tiêu nghiệm thu: *"Tự động đóng gói và lưu trữ file dữ liệu đối soát"*, *"Chuẩn hóa luồng log phục vụ nghiệm thu hệ thống"*.
   - Chuẩn format plain text, ⛔ **CẤM** để ký tự markdown `**` trong chuỗi text Excel.
   - **Ví dụ chuẩn mực ô G12:**
     - *Phân hệ VideoWall:* Nghiên cứu chuẩn giao tiếp ISAPI Hikvision; xây dựng API phân quyền điều khiển theo người dùng và khu vực màn hình; phát triển Worker Service chạy nền kết nối thiết bị qua NATS; làm tool WPF kiểm thử và trực tiếp ra site kiểm thử thiết bị nghiệm thu thành công.
     - *Phân hệ ShareData:* Xây dựng cơ chế gửi nối đuôi và truyền nhận dữ liệu tức thì, tối ưu tải hệ thống; tự động đóng gói lưu trữ file đối soát và chuẩn hóa log phục vụ nghiệm thu hệ thống.

5. **Tạo Mới Tháng Mới Dựa Trên `2026.09_KPI_DatHoangQuy.xlsx` & Bắt Buộc Đối Chiếu Cả Format Và Công Thức Với `2026.08_KPI_TenHoTenLot.xlsx` (Zero UI & Formula Misalignment)**:
   - **Tạo mới tháng mới:** Khi cần tạo báo cáo KPI cho tháng mới, **BẮT BUỘC dựa trên tệp chuẩn hoàn chỉnh gần nhất là `DocBusinessThienAn/BaoCao/KPI/2026.09_KPI_DatHoangQuy.xlsx` để tạo**. Tệp này đã chuẩn hóa sẵn cấu trúc bảng chuỗi (sharedStrings), tự động co giãn dòng theo nội dung và đồng bộ liên kết.
   - **Tệp baseline đối chiếu:** `DocBusinessThienAn/BaoCao/KPI/2026.08_KPI_TenHoTenLot.xlsx` là baseline chuẩn (Read-Only), **chỉ dùng để đối chiếu format và công thức xem có đúng hay không**.
   - ⛔ **TUYỆT ĐỐI CẤM SỬA ĐỔI, GHI ĐÈ HOẶC XÓA TỆP `DocBusinessThienAn/BaoCao/KPI/2026.08_KPI_TenHoTenLot.xlsx`** dưới mọi hình thức. Tệp này được bảo vệ nghiêm ngặt ở chế độ Chỉ Đọc (Read-Only) để làm thước đo đối chiếu format vĩnh viễn cho mọi kỳ đánh giá KPI tiếp theo.
   - **Bắt buộc đối chiếu format & công thức sau khi hoàn thành:** Sau khi tạo hoặc chỉnh sửa bất kỳ file KPI tháng mới nào (ví dụ `2026.09_KPI_DatHoangQuy.xlsx`), **BẮT BUỘC phải mở và đối chiếu lại toàn diện cấu trúc XML, format và công thức tính toán với file gốc `2026.08_KPI_TenHoTenLot.xlsx`**:
     - **Đối chiếu vị trí & định dạng dòng TỔNG KẾT:** BẮT BUỘC nằm cố định tại **Row 20**, `ht="22.5" customHeight="1"`, `mergeCell ref="A20:G20"`, đúng mã style (`s="329"` cho A20: font Segoe UI 11pt Bold, màu nền `fillId=28`, viền dày `borderId=91`, `s="330"`, `s="331"`, `s="146"`, `s="147"`, `s="148"`, `s="149"`).
     - **Đối chiếu giao diện & đối tượng vẽ (Drawing Shapes):** Kiểm tra các nút bo góc, hình vẽ, icon màu chỉ thị (như nút `DEV`, `DOC`, chấm tròn điểm số) có bị lệch dòng hoặc trôi ra ngoài bảng hay không. Các đối tượng vẽ trong OpenXML (`drawing1.xml`) được neo cố định theo số dòng (`<xdr:row>`), do đó vị trí bảng hướng dẫn (hàng 26 - 33) **BẮT BUỘC giữ nguyên vị trí dòng tuyệt đối theo file gốc**, tuyệt đối không được tự ý dịch chuyển làm lệch hình vẽ.
     - **Đối chiếu định dạng ô (Styles) & Màu chữ (Zero Color Regression):**
       - Font chữ, độ cao dòng (`ht`), màu nền (`fillId`), viền ô (`borderId`) và căn lề (đặc biệt `wrapText="1"` cho các ô nhiều dòng như `G12`, `F13`, `F14`) phải đồng bộ 100% với file gốc.
       - **Bảo toàn màu chữ chuẩn & cấm chữ màu vàng/cam ngoài vùng sao; Bảo toàn viền mỏng FFD5DEE8 khối C6..C8 (Cấm viền đậm FF4E5D78):** Ô Họ tên nhân viên `C6` và ô Kỳ đánh giá `C8` (Sheet 2) BẮT BUỘC dùng Style 112 (màu text chuẩn `#1E293B`, `fillId=0` transparent, `borderId=21` viền mỏng `FFD5DEE8` xung quanh), đối chiếu 1:1 với baseline `2026.08` Style 294 và Style 283; ⛔ **CẤM** gán nhầm style rating sao vàng (như 105 trong 2026.08 hoặc 107 trong 2026.09 có font `FFFFC000`), ⛔ **CẤM** gán nhầm style 283 vào ô C8 làm viền bị đậm màu `FF4E5D78`. Quét cấm chữ vàng/cam `FFFFC000` trên toàn bộ các ô ngoài khu vực đánh giá sao (`L36..L39`) và đối chiếu toàn vẹn 100% đường viền (Zero Border Corruption) của Sheet 2 so với baseline.
       - **Bảo toàn Wrap-Text & Chiều cao hàng khít vừa đủ xem (Row 13, 14):** Hàng mô tả task ở Sheet 1 BẮT BUỘC giữ nguyên `wrapText=1`, có thuộc tính `customHeight="1"` và chiều cao hàng chuẩn khít vừa đủ xem (Row 13 `ht="87"`, Row 14 `ht="101.75"`); ⛔ **TUYỆT ĐỐI CẤM** gán đè style không tồn tại vào các ô `D14..F14` làm Excel mất wrapText khiến chiều cao hàng bị co rúm lại thành 1 dòng.
     - **Đối chiếu bảo toàn 100% công thức (Zero Formula Loss):** Quét và so sánh danh sách các ô có công thức (`<f>...</f>`) trên cả Sheet 1 và Sheet 2 (239/239 công thức) so với baseline. ⛔ **TUYỆT ĐỐI CẤM** làm mất hoặc ghi đè công thức thành giá trị tĩnh.
     - **Đối chiếu cấu trúc XML:** Chạy hàm `validate_excel_openxml(output_path, baseline_compare_path)` đối chiếu với template gốc để đảm bảo không phát sinh bất kỳ lỗi cú pháp hoặc hỏng tệp nào.

6. **Đồng Bộ Tuyệt Đối Giữa URL Hyperlink Và Nội Dung Text Hiển Thị (Nội Dung Link Phải Giống Nội Dung Text) (Rule 19.79)**:
   - Trong file Excel KPI (sheet `01. Tổng task`, cột E "Link công việc") cũng như mọi file Excel/tài liệu trong dự án:
     - **Nội dung văn bản hiển thị trong ô (Cell Text / `sharedStrings.xml`) BẮT BUỘC PHẢI TRÙNG KHỚP 100% với địa chỉ liên kết thực tế (`Target` trong file `_rels/sheet*.xml.rels`)**.
     - Ví dụ: Nếu liên kết mở task `https://wework.base.vn/tasks?task=11004385` thì text hiển thị trên bảng tính cũng phải chính xác là `https://wework.base.vn/tasks?task=11004385`.
   - ⛔ **TUYỆT ĐỐI CẤM** để xảy ra tình trạng "bấm link này ra link nọ", tức là nội dung text hiển thị mang mã task cũ hoặc task khác (như `11034326`) trong khi URL hyperlink lại trỏ đến mã task mới (như `11004385`), hoặc ngược lại.
   - **Quy trình cập nhật liên kết bằng script / OpenXML patch:**
     - Mỗi khi cập nhật link task, **BẮT BUỘC thực hiện cập nhật đồng thời ở cả 2 nơi**:
       1. Thay thế chuỗi URL trong `xl/sharedStrings.xml` để text hiển thị trên ô được cập nhật chuẩn xác.
       2. Thay thế thuộc tính `Target` của `<Relationship>` tương ứng trong `xl/worksheets/_rels/sheet*.xml.rels`.
     - Loại bỏ hoàn toàn thuộc tính `quotePrefix="1"` trong `xl/styles.xml` đối với các ô link để tránh dấu phẩy trên `'` cản trở việc click link trên thanh công thức.

---

## 🖱️ 11. Quy Chuẩn Viết Kịch Bản Nghiệm Thu & Hướng Dẫn Thao Tác Thủ Công — Ưu Tiên Giao Diện (UI-First - Rule 19.82)

1. **Phạm vi áp dụng**: mọi tài liệu hướng dẫn người dùng/QA tự làm theo từng bước (kịch bản nghiệm thu, hướng dẫn dựng dữ liệu nền, hướng dẫn kiểm thử thủ công), ví dụ `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/doc/06-khoi-dong-3-du-an-va-nghiem-thu-sharedata.md`. Không áp dụng cho tài liệu kỹ thuật nội bộ mô tả script, không áp dụng cho test tự động (xem [testing-rules_thienan.md](./testing-rules_thienan.md)).

2. **Thứ tự cách làm**:
   - **CÁCH 1 — Giao diện**: lộ trình chính, viết đầy đủ từ đầu đến cuối bằng thao tác trên giao diện (FE) đã có, đặt TRƯỚC.
   - **CÁCH 2 — Script SQL / API / dòng lệnh**: đường phụ (đường tắt khi cần nạp nhanh hàng loạt), đặt SAU CÁCH 1.
   - ⛔ **CẤM** nhắc script `.sql` trong các bước của CÁCH 1, và ⛔ **CẤM** đặt script làm cách mặc định hay "khuyến nghị" thay cho giao diện khi giao diện đã làm được.

3. **Thứ tự bước trong CÁCH 1**: đi theo phụ thuộc dữ liệu, bắt đầu từ thực thể gốc mà các thực thể sau phải chọn từ danh sách (ví dụ đối tác và gói tin có trước, rồi ánh xạ, rồi đăng ký). Đầu mục có sơ đồ phụ thuộc một dòng và nêu lý do thứ tự; mỗi bước chỉ dùng dữ liệu đã được tạo ở các bước trước. Một thao tác chỉ mô tả chi tiết ở MỘT nơi, nơi khác dùng liên kết.

4. **Ngoại lệ không có giao diện**: chỉ khi đã kiểm bằng mã FE/BE rằng thao tác THẬT SỰ không có màn hình hoặc API tương ứng mới được dùng SQL trong lộ trình chính. Khi đó gom vào MỘT khung cảnh báo "ngoại lệ", nêu lý do, câu SQL và cách kiểm; không rải rác nhiều chỗ.

5. **Độ chính xác của bước giao diện**: tên màn hình, nút, nhãn, trường bắt buộc, giới hạn nhập (ví dụ độ dài tối đa), ràng buộc khóa sửa phải đối chiếu mã FE và i18n; bước chưa xác minh được thì ghi "(cần kiểm)". Mỗi bước kèm kết quả mong đợi quan sát được trên giao diện.

6. **Kiểm sau khi dựng dữ liệu**: kiểm trên giao diện trước (số bản ghi hiển thị, trạng thái, bộ lọc). Truy vấn SQL chỉ-đọc chỉ là kiểm phụ để đối soát.

7. **Ví dụ đối chiếu**:
   - ❌ **Sai:** Mục dựng dữ liệu nền mở đầu bằng "chạy script 02, 03, 04" rồi mới có phần "tự tạo trên giao diện" ngắn ở sau, và thiếu bước tạo đối tác.
   - ✅ **Đúng:** CÁCH 1 là lộ trình giao diện đủ các bước từ đối tác trở đi; CÁCH 2 (script) nằm phía sau như đường tắt tùy chọn; phần duy nhất cần SQL được đóng khung ngoại lệ.

8. **Lý do**: người nghiệm thu và người dùng thật vận hành hệ thống qua giao diện; giao diện đã xây xong nên đi bằng giao diện vừa dựng được dữ liệu vừa kiểm chứng chính giao diện đó, tránh tình trạng dữ liệu dựng bằng script khác với dữ liệu người dùng tạo ra.
