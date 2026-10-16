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
   - Báo cáo KPI được lập và đánh giá theo **chu kỳ 1 tháng** (định dạng `YYYY-MM`, ví dụ: `2026-10`).
   - **Ngày báo cáo** (ô `M7` / `K7` trong file Excel KPI): **BẮT BUỘC phải nằm trong tháng đánh giá đó** (ví dụ: kỳ `2026-10` thì ngày báo cáo phải là một ngày trong tháng 10/2026, như `17/10/2026`).

2. **Định Mức Tính Ngày Công (Man-Days) — Chuẩn 5.5 Ngày / Tuần**:
   - Quy chuẩn tính ngày công trong công ty: **1 tuần làm việc = 5.5 ngày công** (từ thứ 2 đến sáng thứ 7).
   - Tổng số ngày công trong kỳ báo cáo 1 tháng được phân bổ theo định mức thực tế của tháng ($\approx 20.5 - 22$ ngày công/tháng, ví dụ: 2 task chia 10.5 ngày và 10 ngày = 20.5 ngày công).
   - ⛔ **TUYỆT ĐỐI CẤM** tự ý thay đổi, hạ thấp, ghi đè hoặc đặt lại (revert) các giá trị ngày công mà người dùng đã khai báo hoặc điều chỉnh trong file KPI (tuân thủ nghiêm ngặt P0 Rule 34).






