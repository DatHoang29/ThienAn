---
name: code-review
description: Review thay đổi so với một mốc theo hai trục, đúng chuẩn trong .agents/rules và đúng yêu cầu nghiệp vụ trong DocBusinessThienAn (transcript trước MasterPlan). Dùng khi review nhánh, PR hoặc mã đang sửa dở.
---

# Review hai trục

- **Chuẩn**: mã có theo `.agents/rules/` không.
- **Yêu cầu**: mã có làm đúng điều tài liệu nghiệp vụ yêu cầu không.

Báo riêng hai trục, không gộp hay xếp hạng chung: mã đúng chuẩn vẫn có thể làm sai việc, và ngược lại.

## 1. Chốt mốc, lấy diff từng repo
- ThienAn có 3 repo riêng: repo cha `ThienAn` (`.agents/`, `tests/`, `DocBusinessThienAn/`), `TA-ITS015-WEBAPI-V1.0`, `TA-ITS015-WEBVUE-V1.0`. Lấy diff trong từng repo có thay đổi: `git -C <repo> diff <mốc>...HEAD`; mã đang sửa dở thì `git -C <repo> diff` và `git -C <repo> status`.
- Người dùng chưa nêu mốc thì hỏi. Kiểm mốc có thật (`git -C <repo> rev-parse <mốc>`) và diff không rỗng trước khi review.
- Chỉ đọc git; không add, commit, checkout, restore (Rule 19.58).
- Đọc mã bằng Gortex (`read`, `relations`, `change(operation:"impact")`). Đối chiếu dữ liệu trên DB local (`mssql_dev`, `mssql_test`); không lấy staging làm phát hiện.

## 2. Nguồn của từng trục
- **Chuẩn**: bảng P0 trong `thienan_rules.md`, rule miền của tệp bị sửa (bảng định tuyến trong `request-routing.md`) và skill `clean-code`.
- **Yêu cầu**: tài liệu trong `DocBusinessThienAn/<DựÁn>/<PhânHệ>/`. Transcript (`doc/transcript/`) trước, MasterPlan sau, cùng prompt đang thực thi. Transcript không nói tới thì ghi "transcript không đề cập", không lấy MasterPlan làm bằng chứng duy nhất. Không thấy tài liệu thì hỏi; người dùng nói không có thì bỏ trục này và ghi rõ.

## 3. Chạy hai trục
Hai trục chạy song song bằng hai subagent được (Rule 19.70). Mỗi subagent nhận lệnh diff, danh sách nguồn của trục và giới hạn 400 từ.
- **Chuẩn**: theo từng tệp hoặc đoạn, chỉ chỗ vi phạm và trích rule (tệp, số rule). Tách vi phạm P0 khỏi góp ý nên sửa. Bỏ qua điều công cụ đã tự kiểm.
- **Yêu cầu**: (a) yêu cầu còn thiếu hoặc làm dở; (b) hành vi không ai yêu cầu; (c) đã làm nhưng có vẻ sai. Mỗi ý trích câu tài liệu (tệp, mốc giờ nếu là transcript).
- Cả hai trục kiểm thêm: mã mới sửa đã có test trong `tests/BE/`, `tests/FE/` chưa (Rule 19.109).

## 4. Báo cáo, prompt sửa
- Báo cáo theo [documentation-standards_thienan.md](../../rules/documentation-standards_thienan.md) §2: mở đầu là kết luận và việc cần làm; khung Hiện trạng, Đã làm, Chưa làm, Rủi ro và Đề xuất; chỉ hai trạng thái Đã làm, Chưa làm; không phụ lục.
- Phát hiện nằm trong hai mục `## Chuẩn` và `## Yêu cầu`, giữ như subagent báo. Cuối báo cáo một dòng: số phát hiện mỗi trục và lỗi nặng nhất của từng trục.
- Chỗ cần sửa nêu trong báo cáo; người dùng bảo sửa thì sửa thẳng, không viết prompt (thienan_rules STT 16, bỏ prompt-first 13/11/2026).
