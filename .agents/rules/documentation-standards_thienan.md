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
| **Chia mốc ngày (`dd/MM/yyyy` + `===`)** | ❌ Không chia theo kiểu nhật ký Task Portal. | ❌ **KHÔNG** ghi ngày tháng `dd/MM/yyyy`, **KHÔNG** dùng dòng `==============================`. | ✅ **BẮT BUỘC** ngăn cách giữa các ngày bằng dòng `==============================` (dòng đầu tiên KHÔNG cần `==============================`). |
| **Cách trình bày** | Khung 4 trục: Hiện trạng → Đã làm → Chưa làm → Rủi ro & Đề xuất (Bảng Markdown). | Đánh số đầu mục (`1.`, `2.`, `3.`...) kèm phạm vi (`FE`, `BE`, `Service`) và gạch đầu dòng `- ` tóm tắt yêu cầu. | Cú pháp tiêu đề mỗi ngày: Dòng 1 `Phạm vi: [Tên phạm vi]`, Dòng 2 `dd/MM/yyyy: [Tên công việc]`, viết sát liền dòng (chỉ ngắt dòng đơn `\n`, KHÔNG để dòng trống ở giữa) với các gạch đầu dòng `- ` chi tiết kết quả. |
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
   - Mỗi gạch đầu dòng `- ` chỉ **1 câu ngắn gọn** (15–25 từ), **KHÔNG** liệt kê tên class, tên bảng, tên file, tên biến code hoặc danh sách tham số dài dòng trong ngoặc; chỉ cần nêu khái quát nghiệp vụ và phạm vi `(Service / BE)`.
   - **KHÔNG** chia mốc ngày `dd/MM/yyyy`, **KHÔNG** có vạch `==============================`.
   - ⛔ **CẤM** ghi mã ký hiệu nội bộ (`R1`, `R2`, `R5`...), **CẤM** ghi tên class cụ thể và **CẤM** đưa việc dọn dẹp code.
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
   - **Chia đều theo mỗi ngày trong tuần (Daily Breakdown)**: Khi task kéo dài 1 tuần, **BẮT BUỘC chia đều các đầu mục theo từng ngày làm việc trong tuần** (được phép tách nhỏ tính năng lớn thành các bước theo ngày để ngày nào cũng có đầu mục kết quả rõ ràng).
3. **Định dạng & Bố cục chuẩn (Viết sát dòng, chỉ ngắt dòng đơn)**:
   - **Vạch ngăn cách**: Phân tách giữa các ngày bằng dòng `==============================` (**dòng đầu tiên của báo cáo TUYỆT ĐỐI CẤM có `==============================`**).
   - **Dòng Phạm vi riêng biệt**: Dòng đầu tiên của mỗi ngày là `Phạm vi: [Tên phạm vi]` (ví dụ: `Phạm vi: Backend`, `Phạm vi: Frontend`, `Phạm vi: Service / Backend`, `Phạm vi: Backend / Frontend`, `Phạm vi: Support / Verify`).
   - **Dòng tiêu đề ngày**: Cú pháp `dd/MM/yyyy: [Tên công việc]` (⛔ **TUYỆT ĐỐI CẤM** lặp lại `(BE)`, `(FE)` trong ngoặc ở cuối dòng này vì đã khai báo `Phạm vi` ở dòng trên).
   - **Quy chuẩn nội dung theo Phạm vi**: Khi `Phạm vi: Frontend`, toàn bộ các gạch đầu dòng bên dưới **BẮT BUỘC là công việc Frontend**, ⛔ **CẤM** đưa các việc trích xuất backend/CSDL vào scope Frontend.
   - **Viết sát dòng (KHÔNG để dòng trống ở giữa)**: Toàn bộ các dòng (`Phạm vi: ...`, dòng tiêu đề ngày, các gạch đầu dòng `- `, vạch ngăn cách) **BẮT BUỘC viết sát liền nhau (chỉ dùng ngắt dòng đơn `\n`, không chèn dòng trống)** để khi copy-paste vào trình soạn thảo Task Portal không bị sinh khoảng cách thừa làm bể layout.
   - **Nội dung gạch đầu dòng**: Mỗi gạch đầu dòng `- ` viết 1 câu ngắn gọn, súc tích mô tả một kết quả nghiệp vụ cụ thể.
   - ⛔ **CẤM** ghi mã ký hiệu nội bộ (`R1`, `R2`, `R5`, `R7`, `BE-7`, `G.1`...).
   - ⛔ **CẤM khai báo chi tiết tên class, tên bảng** (như `ShareDataWorker`, `IntgVmsManager`, `ShareDataPartner`...): **chỉ cần ghi khái quát nghiệp vụ**.
   - ⛔ **CẤM** liệt kê các việc dọn dẹp code.
4. **Mẫu chuẩn Kết quả công việc**:
   ```text
   Phạm vi: Backend / Frontend
   14/10/2026: Cấu hình kênh và thông số kết nối đối tác
   - Mở rộng lưu trữ các giao thức kết nối (API, Kafka, FTP) cùng tham số kỹ thuật linh hoạt theo từng đối tác.
   - Cập nhật giao diện khai báo đối tác tự động hiển thị các trường cấu hình theo loại giao thức được chọn.
   ==============================
   Phạm vi: Service / Backend
   15/10/2026: Xây dựng tầng điều phối kết nối dịch vụ
   - Xây dựng tầng điều phối kết nối độc lập nhận dữ liệu JSON sau ánh xạ, tách rời logic vận chuyển khỏi quy trình trích xuất cốt lõi.
   - Tự động chuyển đổi định dạng và gửi dữ liệu theo từng loại kênh đã cấu hình như HTTP REST cho API, mảng byte cho Kafka hoặc FTP.
   ==============================
   Phạm vi: Support / Verify
   16/10/2026: Hỗ trợ phối hợp sự cố và xác minh hệ thống
   - Phối hợp hỗ trợ kiểm thử tính năng nhận sự kiện sự cố và bắn tín hiệu sang hệ thống liên kết.
   - Xác minh toàn trình luồng gửi nhận và hoàn thiện tài liệu kiểm thử.
   ```

---

## 🌐 7. Phạm Vi Áp Dụng Chú Thích Tiếng Anh Trong Ngoặc (Chỉ Áp Dụng Cho Tài Liệu Markdown `.md` — Rule 20.13)

* **Quy ước song ngữ `[Tiếng Việt] ([English])`**:
  - Khi viết tài liệu phân tích kỹ thuật, kiến trúc, đặc tả nghiệp vụ, biên bản họp hoặc MasterPlan bằng tiếng Việt, việc mở ngoặc ghi kèm thuật ngữ tiếng Anh chuẩn (như `Đăng ký (Subscription)`, `Gói tin (Packet)`, `Ánh xạ (Mapping)`, `Trực quan (Builder)`, `Mã nguồn (Raw)`) nhằm giúp lập trình viên và Tech Lead đối chiếu chính xác giữa khái niệm nghiệp vụ và tên class/bảng/API trong mã nguồn.
  - ⛔ **CẤM TUYỆT ĐỐI áp dụng quy ước này lên giao diện người dùng (HTML/Vue/i18n)**: Trên màn hình HTML/UI (template, component, dialog, tab, button, placeholder, message, và các tệp `vi-vn.json`/`en-us.json`), **ngôn ngữ nào ra ngôn ngữ đó qua hệ thống i18n**. Giao diện tiếng Việt phải dùng tiếng Việt thuần túy, không chèn tiếng Anh trong ngoặc (xem Rule 20.13 trong `frontend-vue_thienan.md`).


