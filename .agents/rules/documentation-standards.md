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
