---
name: git-workflow
version: 1.0.0
priority: P1
trigger: model_decision
description: Git branching workflow, Vietnamese commit conventions, and pre-commit checklist based on F10 standard.
---

# 🌿 Quy Chuẩn Git Workflow & Commit — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định quy trình nhánh, định dạng commit và checklist trước khi commit áp dụng cho toàn bộ thành viên và AI.

---

## 👤 1. Cấu Hình Tài Khoản Git
* **Tên tài khoản (username):** Phần tên trước `@tacorp.vn` trong email công ty (VD: `sonth`).
* **Họ và tên:** Bắt buộc họ tên thật tiếng Việt có dấu.
* **Email:** Bắt buộc dùng email công ty (`...@tacorp.vn`).

---

## 🏷️ 2. Quy Định Đặt Tên Nhánh (Branch Naming)

### Cú Pháp Chuẩn
* **Có TaskCode / Issue:** `[BranchKey]/[yyyyMMdd]-[TaskCode]_[ten-cong-viec-viet-thuong-cach-nhau-boi-dau-gach-ngang]`
  - Dấu gạch dưới `_` **DUY NHẤT** được dùng để ngăn cách giữa mã TaskCode/Issue và tên công việc.
  - *Ví dụ:* `feat/20260922-XD1.2.2.5_map-location-dathp`, `fix/20261021-XD006.7.1_chuan-hoa-giao-dien-sharedata`
* **Rút gọn (không TaskCode / không Issue):** `[BranchKey]/[yyyyMMdd]-[module]-[ten-cong-viec-ngan-gon]`
  - BẮT BUỘC dùng 100% dấu gạch ngang `-`, có tiền tố tên module ngay sau ngày tháng.
  - ⛔ **CẤM dùng dấu gạch dưới `_`** khi nhánh không có TaskCode/Issue.
  - *Ví dụ:* `fix/20261028-sharedata-chuan-hoa-giao-dien`, `fix/20261007-sharedata-issue`, `fix/20261008-sharedata-khung-gio-qua-dem`
  - ❌ *Sai:* `fix/20261028_chuan-hoa-giao-dien-sharedata` (sai vì không có TaskCode mà dùng `_`, thiếu tiền tố module).

### Phân Loại Nhánh
| Nhánh | Phụ trách | Mục đích |
|---|---|---|
| `release` | Leader | Sản phẩm bàn giao, bắt buộc tạo git tag version (VD: `v1.0.1`). |
| `staging` | Leader | Môi trường UAT / nghiệm thu khách hàng. |
| `dev` | Leader | Nhánh tích hợp chính. |
| `feat` | Dev | Tính năng mới. |
| `fix` | Dev | Sửa lỗi trong quá trình dev/review trên `dev`/`staging`. |
| `merge` | Dev | Nhánh trung gian resolve conflict chéo giữa các thành viên. |
| `preview` | Dev | Nhánh xem trước phục vụ Pull Request phức tạp. |
| `hotfix` | Dev | Sửa lỗi gấp trực tiếp trên `release`. |
| `exp` | Dev | Thử nghiệm giải pháp công nghệ mới. |

### ⛔ Quy Tắc Cấm Kỵ Khi Đặt Tên Nhánh (P0)
1. **CẤM lặp từ khóa BranchKey trong tên công việc (Rule 19.57)**:
   - ❌ *CẤM*: `fix/...-fix-...`, `fix/..._fix-...`, `feat/..._feat-...` (VD: `fix/20261007-XD001.5.6_fix-sharedata`).
   - ✅ *ĐÚNG*: `fix/20261007-sharedata-issue`.
2. **CẤM đặt tên vô nghĩa hoặc trơ trọi tên module**:
   - ❌ *CẤM*: `chuc-nang`, `code`, `issue`, `loi`, `sharedata`, `tms`.
   - ✅ *ĐÚNG*: Mô tả nghiệp vụ súc tích (`sharedata-issue`, `sharedata-khung-gio-qua-dem`).
3. **CẤM viết tiếng Việt có dấu trong tên nhánh**: Tên nhánh viết tiếng Việt **KHÔNG DẤU** hoặc tiếng Anh, ngăn cách bằng dấu gạch ngang `-`.
4. **CẤM dùng dấu gạch dưới `_` khi không có TaskCode / Issue (Rule 19.110)**:
   - Dấu gạch dưới `_` **CHỈ ĐƯỢC PHÉP XUẤT HIỆN** khi nhánh có gắn mã TaskCode hoặc Issue (dùng làm ranh giới phân tách duy nhất giữa mã issue và phần mô tả: `[TaskCode]_[mo-ta]`).
   - Mọi nhánh thông thường không có TaskCode/Issue: **100% dùng dấu gạch ngang `-`** và bắt buộc có tiền tố module: `[BranchKey]/[yyyyMMdd]-[module]-[mo-ta]`.
   - ❌ *CẤM*: `fix/20261028_chuan-hoa-giao-dien-sharedata`, `feat/20261028_them-chuc-nang`
   - ✅ *ĐÚNG*: `fix/20261028-sharedata-chuan-hoa-giao-dien`, `feat/20261028-sharedata-them-chuc-nang`

---

## 📝 3. Quy Định Nội Dung Commit & Format Chuẩn

### Cú Pháp Commit Message (BẮT BUỘC Tiếng Việt Có Dấu - Rule 19.55)
* **Summary (Dòng 1):** `<type>(<scope>): [nội dung công việc tiếng Việt có dấu]`
  - *Ví dụ:* `feat(vms): thêm mới dịch vụ`, `fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata`
  - Scope là tên module viết thường: `sharedata`, `vms`, `tms`, `videowall`, `toll`, `test`...
* **Dòng 2:** Lặp lại nguyên văn Summary (⛔ TUYỆT ĐỐI KHÔNG thêm nhãn `Description:`).
* **Dòng 3 trở đi:** Dòng trống, sau đó là các gạch đầu dòng (`- `) ngắn gọn mô tả **fix cái gì / làm cái gì** tại file nào.
* ⛔ **CẤM tự ý chèn thẻ metadata** `Ref: ...`, `Reviewer: ...`, `CR: ...` khi không có yêu cầu.
* ⛔ **Gạch đầu dòng chỉ nêu việc chính đã làm (Rule 19.102)**: mỗi gạch là một thay đổi chính của tính năng/lỗi, viết ngắn. CẤM ghi các việc phụ trợ: viết test, chạy test, kết quả kiểm thử (VD: `thêm test...`, `test 26/26 đạt`), cập nhật tài liệu đi kèm, dọn mã.
  - ❌ *CẤM*: `- Thêm test kiểm tra URL gửi tableId, chạy 27/27 đạt`
  - ✅ *ĐÚNG*: `- Lấy cột bảng nguồn bằng mã định danh thay vì tên bảng`

### Tính Toàn Vẹn Của Commit (Atomic Commit & Phụ Thuộc Hoàn Chỉnh - Rule 19.93)
* **Gom đủ tệp liên quan vào cùng 1 commit**: Khi `git add` và commit (`commit -m`), BẮT BUỘC gom toàn bộ các tệp cấu thành và phụ thuộc trực tiếp của tính năng vào cùng một commit (Atomic Commit).
* **Đặc biệt khi thêm/tách thư viện, project mới**:
  - Bắt buộc add: Mã nguồn (`.cs`, `.ts`, `.vue`), tệp dự án (`.csproj`, `package.json`).
  - **Bắt buộc add tệp Solution (`.sln` như `src/TAC_WebAPI.sln`)**: Đăng ký project mới vào solution để IDE (VS Code, Visual Studio) nhận diện và biên dịch được giải pháp.
  - Bắt buộc add tệp cấu hình (DI registration, extension, `appsettings.json`...) cần thiết để chạy/build project.
* ⛔ **CẤM commit dở dang / sót tệp cấu trúc**: Tuyệt đối cấm commit mã nguồn hoặc `.csproj` mà quên `.sln`. Việc thiếu `.sln` khiến IDE của thành viên khác khi pull về không thấy project, báo lỗi thiếu reference hoặc lỗi build.
* ⛔ **CẤM tách thành các commit chắp vá**: Cấm commit code trước rồi mới commit `.sln` ở một commit lẻ tẻ sau đó chỉ vì lúc đầu quên add.

### Ví Dụ Commit Message Chuẩn:
```text
fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

- Cấu hình đăng ký: giới hạn chu kỳ max 86400s, clamp real-time (Issue 1, 2)
- Đối tác chia sẻ: xóa mã khi copy, khóa ô mã khi sửa kèm tooltip (Issue 3, 4)
- Ánh xạ dữ liệu: cập nhật thông báo dữ liệu nguồn rỗng, ẩn nhóm trường ở header (Issue 20, 23, 24)
```

### Định Dạng Trả Về Nội Dung Commit Cho AI (Rule 19.104)
Khi lập trình viên yêu cầu: *"cho nội dung commit -m theo rule thienan"* (hoặc tương đương):
* AI **BẮT BUỘC** xuất trực tiếp khối văn bản thuần (`text`) riêng biệt cho từng repo (Backend / Frontend) theo đúng format:
```text
<type>(<scope>): [nội dung công việc tiếng Việt có dấu]
<type>(<scope>): [nội dung công việc tiếng Việt có dấu]
- [tính năng / việc chính 1]
- [tính năng / việc chính 2]
- [tính năng / việc chính 3]
```
* ⛔ **CẤM** chèn mã Rule (như `Rule 20.7`) vào trong nội dung commit message; chỉ tập trung mô tả tính năng/việc chính đã làm.
* ⛔ **CẤM** xuất dạng câu lệnh shell (`git commit -m "..."`) hay giải thích dài dòng trước khối text, đảm bảo người dùng có thể bấm copy và dán thẳng vào ô commit message của IDE (VS Code, Visual Studio, Git GUI).

---

## 🛑 4. Quy Định Vận Hành Git Đối Với Trợ Lý AI

1. **CẤM TUYỆT ĐỐI TỰ Ý `git add` VÀ `git reset` (Strict No Auto-Stage / No Auto-Unstage)**:
   - AI **TUYỆT ĐỐI KHÔNG CHẠY `git add`** hoặc đưa file vào Staged Changes khi người dùng không yêu cầu trực tiếp.
   - AI **TUYỆT ĐỐI KHÔNG CHẠY `git reset`** hoặc unstage file người dùng đã chủ động đưa vào Staging.
   - Mọi thay đổi code BẮT BUỘC để nguyên ở trạng thái **Working Tree (Changes / Unstaged)** để người dùng tự review qua diff.
2. **CẤM TỰ Ý COMMIT VÀ PUSH (Rule 19.58)**:
   - Quyền commit và push hoàn toàn thuộc về lập trình viên.
   - BẮT BUỘC viết test đầy đủ, kiểm chứng pass 100%, người dùng kiểm tra xác nhận thì mới được commit.
3. **Repo Cha `ThienAn` Chỉ Dùng `main`**:
   - ThienAn gồm 3 repository: repo cha `ThienAn` (docs, `.agents`, `.claude`, `tests/`) và 2 repo con `TA-ITS015-WEBAPI-V1.0`, `TA-ITS015-WEBVUE-V1.0`.
   - **Chỉ repo cha**: Mọi fix (kể cả fix chạy từ prompt/agent) làm thẳng trên `main` rồi push `origin main`; ⛔ **CẤM** tạo nhánh `fix/*`, `feature/*`, `agents/*` hoặc git worktree riêng; nhánh/worktree lỡ tạo thì merge vào `main` rồi xoá cả local lẫn remote.
   - **Hai repo con WEBAPI và WEBVUE KHÔNG bị ảnh hưởng**: Vẫn tạo nhánh theo quy chuẩn Git của Thiên Ân (Mục 2).
   - Quy tắc cấm tự ý git vẫn giữ nguyên: AI chỉ `git add`/`commit`/`push` khi người dùng ra lệnh rõ.
4. **Đồng Bộ `dev` Trước Mọi Lần Push; Commit Một Phần Khi Thư Mục Đang Có Mã Sửa Dở (Rule 19.91, bổ sung 18/10/2026)**:
   - **Nguyên tắc chung (repo con WEBAPI, WEBVUE)**: trước **MỌI** lần push nhánh lên remote, nhánh **BẮT BUỘC** chứa `origin/dev` mới nhất:
     1. `git fetch origin`.
     2. `git rebase origin/dev` (hoặc `git merge origin/dev` khi người dùng không muốn viết lại lịch sử đã push).
     3. Build và chạy lại test liên quan sau khi đồng bộ.
     4. Kiểm `git merge-base --is-ancestor origin/dev HEAD` trả về 0 → mới push.
     ⛔ **CẤM** push nhánh chưa chứa `origin/dev` mới nhất. Repo cha chỉ có `main`: đồng bộ `origin/main` thay cho `dev`.
   - **Trường hợp commit/push một phần** (ví dụ riêng thư mục Delivery) trong lúc thư mục làm việc còn mã sửa dở, tệp đã stage hoặc stash cũ: làm theo các bước dưới.
   - **Kiểm trước, không đoán**: `git fetch origin` rồi đếm:
     - `git rev-list --count HEAD..origin/<nhánh>`: remote có commit mà local chưa có? (đã gặp: nhánh ShareData local ở `56bb4bc3` trong khi remote có thêm 3 commit của chính người dùng).
     - `git rev-list --count HEAD..origin/dev`: dev có commit mới?
     - `git status --porcelain`, `git stash list`: có mã sửa dở, tệp đã stage, stash cũ?
     - Tệp mà các commit remote/dev sửa có trùng tệp đang sửa dở không (`git diff --name-only HEAD origin/<nhánh>` đối chiếu `git status`).
   - ⛔ **CẤM trong thư mục làm việc chính** khi còn mã sửa dở hoặc tệp đã stage: `git stash`, `git rebase` (kể cả `--autostash`), `git pull`, `git reset`, `git checkout -- .`, `git clean`. Các lệnh này làm mất trạng thái stage người dùng đã chọn hoặc gây xung đột với mã dở. ⛔ Không đụng stash cũ của người dùng.
   - **Repo con (WEBAPI, WEBVUE): làm trong worktree tạm**
     1. `git worktree add --detach ../_wt-<việc> origin/<nhánh>` (lấy nhánh từ remote, không từ local, để giữ commit remote mà local chưa có).
     2. `git rebase origin/dev`. Có xung đột → `git rebase --abort`, dừng, báo danh sách tệp; ⛔ không tự giải xung đột.
     3. Chép đúng phần cần commit từ thư mục chính sang (`rsync -a --exclude bin --exclude obj --exclude .DS_Store ...`), build phần đó trong worktree.
     4. Commit theo mục 3, kiểm `git show --stat HEAD` chỉ có tệp của phần việc.
     5. Push. Rebase viết lại commit đã push thì chỉ được `git push --force-with-lease=refs/heads/<nhánh>:<sha origin/<nhánh> đã kiểm ở bước đầu> origin HEAD:refs/heads/<nhánh>`; ⛔ CẤM `--force` trần. Người dùng không muốn viết lại lịch sử thì thay rebase bằng `git merge origin/dev` và push thường.
     6. `git worktree remove ../_wt-<việc>`.
   - **Repo cha (`main`, không dùng worktree — mục 4.3)**: commit bằng pathspec ở cuối lệnh (`git commit -m ... -- <đường dẫn>`) để không cuốn theo tệp khác đang stage; chỉ `git push origin main` khi `HEAD..origin/main` bằng 0, lớn hơn 0 thì dừng, báo.
   - **Báo cáo sau push**: mã commit; `git log --oneline origin/dev..origin/<nhánh>`; trạng thái thư mục chính (đi sau remote bao nhiêu commit, tệp nào sẽ xung đột khi đồng bộ, thư mục chưa track nào phải xóa trước khi pull vì nội dung đã lên remote).
5. **Toàn Vẹn Tệp Phụ Thuộc Khi Commit — Bắt Buộc Kèm `.sln`, `.csproj` Khi Thêm/Tách Project Mới (Atomic Commit & Dependency Completeness) (Rule 19.93, bổ sung 19/10/2026)**:
   - **Nguyên tắc Atomic Commit**: Khi thực hiện `git add` và commit một tính năng, module, hoặc thư viện mới (khi người dùng cho phép): **BẮT BUỘC đưa đầy đủ tất cả các tệp liên quan trực tiếp đến tính năng đó vào cùng một commit**.
   - **Bắt buộc kèm `.sln` và `.csproj`**: Khi thêm, tách hoặc cấu trúc lại project/thư viện (ví dụ tách thư viện `Modules.Integration.Delivery`):
     + Tệp mã nguồn (`.cs`, `.ts`, `.vue`);
     + Tệp dự án (`.csproj`, `package.json`);
     + **Tệp Solution (`.sln` như `src/TAC_WebAPI.sln`)**: BẮT BUỘC `git add` và commit tệp `.sln` chứa định nghĩa project mới;
     + Tệp đăng ký DI, extension, hoặc `appsettings.json` liên quan trực tiếp.
   - ⛔ **TUYỆT ĐỐI CẤM commit dở dang thiếu tệp cấu trúc**: Cấm commit mã nguồn hoặc `.csproj` mà quên `.sln`. Việc thiếu `.sln` trong commit khiến VS Code / Visual Studio của thành viên khác khi pull code về không nhận diện được project mới trong Solution Explorer, Omnisharp/C# Dev Kit báo đỏ và gãy quy trình build giải pháp.
   - ⛔ **CẤM tách thành các commit chắp vá**: Cấm commit mã nguồn trước rồi commit `.sln` sau ở commit khác chỉ vì quên. Trước khi commit, BẮT BUỘC kiểm tra `git status` để rà soát toàn bộ tệp mới (`??`) và tệp cấu trúc đã được đưa vào Staging đầy đủ.
6. **Cấm Commit & Push Các Tệp Cấu Hình Môi Trường Local & Khóa Bí Mật (Rule 19.100)**:
   - Khi người dùng ra lệnh commit (`commit -m`) theo rule rồi push:
     + **Frontend (`TA-ITS015-WEBVUE-V1.0`)**: ⛔ **TUYỆT ĐỐI CẤM** `git add`, commit hoặc push tệp `src/.env.development` (hoặc `.env.local`, `.env.development.local`). Tệp này chứa cấu hình URL API/NATS local riêng của máy lập trình viên.
     + **Backend (`TA-ITS015-WEBAPI-V1.0`)**: ⛔ **TUYỆT ĐỐI CẤM** commit `src/TAC_WebAPI/Configuration/Database.json`, `src/TAC_WebAPI/TAKey.key`, `src/TAC_WebAPI/TAKeyData.key`, `src/DLLs/Shared.Core.Security.dll`.
     + **Quy cách thực hiện an toàn**: Khi `git add`, AI BẮT BUỘC dùng danh sách tệp cụ thể liên quan đến tính năng (hoặc loại trừ các tệp trên), ⛔ **CẤM** dùng `git add .` hoặc `git add -A` bừa bãi làm cuốn nhầm các tệp cấu hình local vào commit.
7. **Cấm Tự Ý Discard / Revert Thay Đổi Trên Working Tree (Rule 19.106)**:
   - ⛔ **TUYỆT ĐỐI CẤM** tự ý chạy `git checkout -- <file>`, `git restore <file>`, `git clean`, `git reset --hard` hoặc bất kỳ thao tác nào nhằm discard/hoàn tác các thay đổi trên Working Tree của lập trình viên (bao gồm các file cấu hình, khóa bảo mật, DLL nội bộ, dữ liệu sửa dở).
   - Khi phát hiện file cấu hình local hoặc bí mật bị sửa đổi: giữ nguyên 100% tại Working Tree; nếu gặp lỗi runtime hoặc license thì báo rõ cho người dùng chứ ⛔ **KHÔNG ĐƯỢC TỰ Ý DISCARD**.

---

## ✅ 5. 10 Điểm Bắt Buộc Kiểm Tra Trước Khi Commit (Pre-Commit Checklist)
1. **Biên dịch & Cú pháp:** Đảm bảo toàn bộ project build thành công, không lỗi cú pháp hoặc warning nghiêm trọng.
2. **Logic nghiệp vụ:** Kiểm tra logic đúng đặc tả, chạy thử trên môi trường dev.
3. **Coding Standards:** Tuân thủ Clean Code, PascalCase, camelCase, cấu trúc thư mục quy định.
4. **Dọn rác code:** Xóa sạch code debug, `console.log`, comment rác, using thừa (IDE0005).
5. **Chạy test tự động:** 100% test cases pass (Full Business Flow, E2E Playwright).
6. **Merge Conflict:** Đồng bộ branch của bạn với `dev` mới nhất **trước mỗi lần push** và trước khi tạo PR (Rule 19.91, mục 4.4).
7. **Tài liệu:** Cập nhật tài liệu kỹ thuật, MasterPlan nếu có thay đổi logic hoặc API.
8. **File & Toàn Vẹn Tệp Phụ Thuộc (Rule 19.93):** Chỉ commit file liên quan trực tiếp, nhưng BẮT BUỘC gom đầy đủ các tệp cấu trúc (`.sln`, `.csproj`, `package.json`...) khi thêm/đổi project; cấm để sót `.sln` làm gãy solution/IDE của đồng đội. Commit message đúng chuẩn tiếng Việt có dấu.
9. **Bảo mật & Cấu hình Local (Rule 19.100):** Tuyệt đối không commit API keys, mật khẩu DB, credentials, dữ liệu cá nhân, và ⛔ **CẤM commit tệp cấu hình môi trường local `.env.development` (Frontend) cũng như `Database.json`, `TAKey*.key`, `Shared.Core.Security.dll` (Backend)**.
10. **Kiểm tra staging:** Xác nhận hệ thống vận hành ổn định trên staging trước khi release.
