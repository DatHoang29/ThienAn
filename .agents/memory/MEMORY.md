# Memory Index

> 🔴 **Mọi file memory session GHI VÀO thư mục NÀY** (`<repo>/.agents/memory/`) — **KHÔNG BAO GIỜ**
> ghi vào `~/.claude/projects/*/memory/`. Đây là **nguồn duy nhất**; path global chỉ là **junction
> trỏ về đây**. Nếu path global là thư mục thật (không phải junction) trên máy hiện tại ⇒ junction
> chưa/hỏng: cứ ghi local, rồi tạo lại junction (snippet cuối file).
>
> ⚠️ **LƯU Ý QUAN TRỌNG**: Toàn bộ quy tắc, quy ước dự án và hướng dẫn AI tập trung DUY NHẤT tại
> [`../rules/thienan_rules.md`](../rules/thienan_rules.md). **TUYỆT ĐỐI KHÔNG TẠO FILE FEEDBACK-*.MD RẢI RÁC.**

## User & Quy định
- [User profile](user-profile.md) — backend dev trên TA-ITS015 (ITS/C2C ISO 14827); viết & trả lời tiếng Việt
- [Quy định chung & Kiến trúc Thiên An](../rules/thienan_rules.md) — **Nguồn sự thật duy nhất (SSOT)** cho toàn bộ quy định dự án (BẮT BUỘC AI ĐỌC TRỰC TIẾP TỪ FILE NÀY: cấm sửa code không liên quan trực tiếp để tránh biến động code quá nhiều không cần thiết, quy định git, commit feat/fix, ưu tiên ORM SqlSugar cấm raw SQL DML, không tự xoá prompt - giữ để review, no-Async suffix, plan-handoff, no manual DDL, code-first, entity, testing, chuẩn code Frontend Vue 3 / VxeTable / Element Plus...). Mọi rule mới chỉ thêm vào `thienan_rules.md`, không ghi đúp vào file memory này.
- **Gortex-First & Default-as-Fallback Protocol**: Luôn luôn ưu tiên dùng bộ công cụ Gortex MCP (`call_mcp_tool` với `ServerName: "gortex"`) cho mọi thao tác mã nguồn (tìm kiếm symbol, usages, dependencies, call chain, đọc và sửa code); nếu Gortex không thực hiện được (bị lỗi, timeout, chưa index file, hoặc thao tác trên tài liệu Markdown) thì mới chuyển sang (fallback) dùng các công cụ mặc định (`view_file`, `grep_search`, `replace_file_content`).

## Database / MCP
- [MCP DAB database access](mcp-dab-database-access.md) — 3 DAB MCP server; review/plan/prompt dùng `mssql_dev`/`mssql_test` (local), `mssql_staging` chỉ khi người dùng yêu cầu
- [Read-only DB queries: just run them](read-only-db-queries-just-run.md) — chạy read không cần hỏi; vẫn cấm write

## Reference
- [Ưu tiên transcript hơn MasterPlan](doc-priority-transcript-over-masterplan.md) — đối chiếu nghiệp vụ: đọc doc/transcript trước, MasterPlan có thể sai
- [Quy chuẩn cấu trúc & quy trình nghe tệp transcript](transcript-structure-rules.md) — chỉ giữ Mục 1 (tóm tắt) và Mục 2 (lời thoại chi tiết), cấm bảng Action Items; khi nghe audio $> 15$ phút bắt buộc cắt nhỏ 10–15 phút/đoạn, lọc tạp âm bàn bên và cấm đoán từ theo quán tính tài liệu cũ
- [Biểu mẫu Git Flow F10.TAC-CN01](../../DocBusinessThienAn/QuyDinhChung/doc/F10.TAC-CN01-git-rules.md) — Tài liệu hướng dẫn sử dụng Git Flow, tạo nhánh và các lưu ý khi commit code (chuẩn ISO F10 của Thiên Ân).
- [Tài liệu Nghiệp vụ HN-CL](../../DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md) — Nguồn sự thật duy nhất cho tài liệu nghiệp vụ Hữu Nghị - Chi Lăng (luôn mở file này trước).
- [Kiến trúc & Luồng Dịch thuật SysTerminology](systerminology-localization-flow.md) — Cơ chế đồng bộ DB SysTerminology -> Backend Resources/*.json (auto-generated) -> Frontend IndexedDB (TA-ConfigDb) -> Vue i18n; cấm sửa tay Resources/
- [SqlSugar docs & CodeFirst pitfalls](sqlsugar-docs.md) — link no-entity / raw SQL / JSON→SQL / bẫy EnableIncreTable & expression tree
- [Biểu mẫu KPI chuẩn](../../DocBusinessThienAn/BaoCao/KPI/2026.09_KPI_DatHoangQuy.xlsx) — Tệp chuẩn để tạo cho các kỳ đánh giá KPI tiếp theo (`2026.09_KPI_DatHoangQuy.xlsx`); quy chuẩn: nếu cần tạo mới cứ dựa `DocBusinessThienAn/BaoCao/KPI/2026.09_KPI_DatHoangQuy.xlsx` rồi tạo cho tháng mới; còn `DocBusinessThienAn/BaoCao/KPI/2026.08_KPI_TenHoTenLot.xlsx` là baseline gốc CHỈ ĐỌC (⛔ TUYỆT ĐỐI CẤM SỬA/GHI ĐÈ/XÓA) chỉ dùng để đối chiếu format và công thức coi đúng hay không (bảo toàn 100% công thức tính toán `<f>...</f>` và format hiển thị, cố định dòng TỔNG KẾT tại Row 20 bảo toàn 100% format dòng gốc, viền trên đậm medium, bảo toàn wrapText và chiều cao hàng task khít vừa đủ xem Row 13 ht="87", Row 14 ht="101.75" có customHeight="1" cấm ghi đè style không tồn tại làm mất wrapText, bảo toàn màu chữ chuẩn #1E293B cho ô Họ tên C6 đối chiếu 1:1 với baseline 2026.08 cấm gán nhầm style sao vàng FFFFC000, bảo toàn viền mỏng FFD5DEE8 cho khối C6..C8 cấm gán nhầm style 283 vào ô C8 làm viền bị đậm FF4E5D78, quét cấm chữ vàng/cam ngoài vùng đánh giá sao L36..L39, đối chiếu toàn vẹn 100% đường viền Sheet 2 so với baseline, ẩn task dư 15-19 kèm dọn sạch cột DOC/DEV, bảo toàn tọa độ drawing shapes, ngày báo cáo ghi ngày hôm nay sửa ngày nào ghi ngày đó, văn phong thô gọn súc tích làm gì ghi nấy cấm quá chi tiết kỹ thuật nội bộ, cấm từ ngữ văn vẻ sáo rỗng, cấm từ cá nhân hóa 'tự làm', cấm ghi số ngày công vào mô tả nội dung task, và đồng bộ tuyệt đối 100% giữa URL hyperlink và text hiển thị trong ô).
- [Tech decisions](tech-decisions.md) — bảo trì `.agents/manifest.json` ↔ frontmatter (AG-Kit tooling, không phải app)

## Bài học kinh nghiệm & Quy chuẩn quan trọng
- [Test theo luồng, có giá trị](feedback_test-flow-value.md) — chỉ test qua điểm vào luồng (`ProcessSubscriptions`, API bus); cấm unit test helper, FE so chuỗi; dùng `[Theory]`/`[InlineData]`.
- **Tự động kích hoạt Subagent song song (Rule 19.70)**: Khi prompt chứa danh sách từ 2 task/công việc trở lên (đặc biệt các dạng đánh số `1.`, `2.`, `3.`... hoặc gạch đầu dòng `- `, danh sách file `@prompt-*.md`), hoặc khi các phần việc/nhiệm vụ được ngăn cách bởi dấu phân cách (như `=========`, `---`, `===`...), AI bắt buộc tự động kích hoạt subagents song song để xử lý độc lập từng phần việc mà không cần nhắc.
- **Quy chuẩn Giao diện Nguồn dữ liệu ShareData (Đối chiếu Wireframe W0.2 - Chốt 27/10/2026)**:
  1. Bố cục tổng thể: Giữ nguyên 3 khối dọc phân tách rõ ràng của hệ thống (Khối 1: SQL, Khối 2: Trường dữ liệu, Khối 3: Tham số).
  2. Tab Chiều Gửi / Chiều Nhận: Chuẩn theo kiến trúc phân hệ hiện tại.
  3. Toolbar hành động SQL:
     - Nút Lưu mang nhãn `Lưu & phân tích` (icon FolderChecked).
     - Phía bên trái toolbar: ở tab đối tác hiển thị Mã đối tác in đậm, Tên đối tác, và chip phiên bản mẫu (`Dựa trên mẫu v3 · mẫu hiện tại v5` hoặc `đã đồng bộ`).
     - So sánh code khác biệt (Diff Dialog) giữ nguyên giải pháp Monaco Diff của hệ thống.
  4. Khối 1: Giữ nguyên nút Mở rộng của hệ thống; loại bỏ các dòng chú thích cú pháp rườm rà dưới khung soạn SQL.
  5. Khối 2: Cột trên bảng theo chuẩn wireframe: `Khoá (alias)`, `Tên hiển thị`, `Kiểu`, `Bắt buộc`, `Ghi chú`, `Trạng thái`. Tất cả header cột được căn giữa (`header-align: 'center'`).
  6. Khối 3: Bảng Tham số loại bỏ dòng chú thích dài `Giá trị lần trước:...`; cấu trúc cột tham số giữ nguyên để phân tích sau.
  7. Chiều cao tabs (`.direction-tabs`, `.sql-partner-tabs`): Tối ưu gọn gàng (28px - 32px).
- **Kiểm thử NATS thật (Phương án 3 - Rule 19.81)**: Khi viết hoặc sửa bài kiểm thử xUnit tích hợp NATS broker thật (Docker local), luôn áp dụng Phương án 3 — cách ly Subject độc lập cho từng bài test dựa trên Unique ID (`$"ta.its.event.sharedata.newdata.t_{testId}"`), inject đè cả `Nats:NewDataSubject` và `Nats:Subject` qua `ConfigurationBuilder`. Đồng thời, ở bước kiểm tra listener sẵn sàng, BẮT BUỘC dùng cơ chế **Retry Probe Loop (100ms)** phát lặp định kỳ cho tới khi listener nhận được (`received.Count > 0`) để triệt tiêu race condition đăng ký subscribe nền, và hủy đăng ký (`transport.Unsubscribe`) ở đầu/cuối test; tăng timeout assertion chờ bản tin lên 45–60 giây khi chạy batch test lớn.

---

## Tạo lại junction (chạy 1 lần trên mỗi máy, không cần admin)

Máy repo ở `C:\ThienAn`:
```cmd
rmdir /S /Q "%USERPROFILE%\.claude\projects\c--ThienAn\memory"
mklink /J "%USERPROFILE%\.claude\projects\c--ThienAn\memory" "C:\ThienAn\.agents\memory"
```
