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

## 📋 6. Quy Chuẩn Liệt Kê Mô Tả Công Việc & Kết Quả Công Việc (Task Management Portal)

1. **Chỉ Liệt Kê Tính Năng Sẽ Làm / Đã Làm (Feature-Only)**:
   - Khi lập danh sách đầu mục cho phần **Mô tả công việc** và **Kết quả công việc** (báo cáo tiến độ tuần/sprint trên hệ thống quản lý công việc): **CHỈ liệt kê các tính năng nghiệp vụ và kỹ thuật thực tế sẽ làm hoặc đã hoàn thành** (ví dụ: xây dựng component dùng chung, tách tầng kết nối, cơ chế gửi bù, phân quyền & validate truy vấn động, chuẩn hóa giao diện Tab, đa ngôn ngữ i18n...).
2. ⛔ **CẤM Đưa Các Việc Dọn Dẹp Mã Nguồn (Code Cleanup) Vào Báo Cáo**:
   - **KHÔNG** liệt kê các thao tác dọn dẹp nội bộ như: xóa enum/hằng số chết, xóa file API/model mồ côi, dọn warning compiler, đổi tên biến nội bộ hoặc các việc bảo trì vụn vặt không mang tính chất tính năng.

