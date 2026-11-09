# Memory Index

> Memory duy nhất của mọi công cụ AI; kho riêng của Claude Code, Codex đã tắt.
> Ghi mới vào tệp chủ đề (hoặc tệp mới), mỗi tệp một dòng mục lục ≤ 150 ký tự; quy tắc P0 ghi ở `../rules/thienan_rules.md`.
> Repo công khai: không ghi mật khẩu, chuỗi kết nối, địa chỉ máy chủ.

## Người dùng & cách làm việc
- [User profile](user-profile.md) — backend dev TA-ITS015 (ITS/C2C ISO 14827); viết, trả lời tiếng Việt
- [Cách làm việc với AI](workflow-ai.md) — prompt-first, commit đơn giản, review local, test đỏ trước, không tạo tệp ở thư mục công cụ…
- [Test theo luồng, có giá trị](feedback_test-flow-value.md) — chỉ test qua điểm vào luồng; cấm unit test helper, FE so chuỗi; [Theory]/[InlineData]

## Database / MCP
- [MCP DAB](mcp-dab-database-access.md) — 3 server DAB chỉ đọc; review dùng dev/test local; staging tắt mặc định, bật khi người dùng yêu cầu
- [Truy vấn đọc chạy luôn](read-only-db-queries-just-run.md) — đọc không cần hỏi; tool ghi CSDL đã tắt ở `../mcp/dab-config.json`
- [SqlSugar](sqlsugar-docs.md) — link no-entity / raw SQL / JSON→SQL / bẫy EnableIncreTable & expression tree

## Tham chiếu
- [Transcript trước MasterPlan](doc-priority-transcript-over-masterplan.md) — đối chiếu nghiệp vụ đọc doc/transcript trước; MasterPlan có thể sai
- [Cấu trúc transcript](transcript-structure-rules.md) — chỉ Mục 1 tóm tắt, Mục 2 lời thoại; audio > 15 phút cắt 10–15 phút/đoạn
- [Git Flow F10.TAC-CN01](../../DocBusinessThienAn/QuyDinhChung/doc/F10.TAC-CN01-git-rules.md) — tạo nhánh, lưu ý khi commit (chuẩn ISO F10)
- [Tài liệu nghiệp vụ HN-CL](../../DocBusinessThienAn/HữuNghị-ChiLăng/INDEX.md) — nguồn duy nhất tài liệu Hữu Nghị - Chi Lăng, mở trước
- [Dịch thuật SysTerminology](systerminology-localization-flow.md) — DB → Resources/*.json → IndexedDB → i18n; cấm sửa tay Resources/
- [Biểu mẫu KPI](kpi-template.md) — tạo kỳ mới từ 2026.09; 2026.08 chỉ đọc để đối chiếu; quy chuẩn định dạng trong tệp

## Dự án
- [ShareData](sharedata.md) — trạng thái, quyết định đã chốt, quy chuẩn giao diện Nguồn dữ liệu (W0.2)
- [Bàn giao 11/2026](handover-2026-11.md) — tài liệu ở `bangiao/`, tách theo phân hệ, bản html và md
