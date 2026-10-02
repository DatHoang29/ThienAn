# 📌 Quy Định Chung Phòng Phần Mềm: Nhánh, Commit, Kiến Trúc & Vận Hành AI

Tài liệu này định nghĩa quy trình đặt tên nhánh (branching workflow), luồng phát triển, định dạng thông
điệp commit (commit messages), kiến trúc module, quy tắc viết test và các rule mandatory vận hành AI
— áp dụng cho toàn bộ thành viên trong dự án và cho AI khi làm việc trong repo.

> ⚖️ **Mức độ ưu tiên**: Toàn bộ rule trong file này có độ ưu tiên **CAO HƠN** `.agents/rules/universal-rules.md`
> và mọi rule generic khác từ AG-Kit upstream khi có xung đột nội dung. Nhờ vậy, `universal-rules.md`
> (và `code-rules.md`) có thể được **thay thế trực tiếp bằng bản mới nhất** mỗi khi AG-Kit có update,
> mà không cần lo mất customization của dự án — vì mọi phần riêng của ThienAn đã nằm hết trong file này.

---

## 👤 0. Quy Định Tài Khoản Git (Git Account Configuration)

> 📖 **Căn cứ biểu mẫu:** Quy định theo tài liệu chuẩn của công ty [`F10.TAC_CN01 - Hướng Dẫn Sử Dụng Git Flow và Các Lưu Ý Khi Commit Code`](../../DocBusinessThienAn/QuyDinhChung/doc/F10.TAC-CN01-git-rules.md).

Mọi nhân sự và lập trình viên khi tham gia dự án bắt buộc thiết lập thông tin tài khoản Git như sau:
*   **Tên tài khoản (username):** Sử dụng phần tên đứng trước ký tự `@tacorp.vn` trong địa chỉ email công ty (Ví dụ: email `sonth@tacorp.vn` thì username là `sonth`).
*   **Hình đại diện (avatar):** Chọn hình ảnh chỉn chu, rõ mặt, phù hợp với môi trường công sở (sử dụng ảnh thẻ hoặc tương đương).
*   **Bổ sung họ tên thật:** Cập nhật đầy đủ họ và tên tiếng Việt có dấu vào thông tin profile cá nhân.
*   **Email tài khoản:** Bắt buộc sử dụng địa chỉ email công ty cấp (`...@tacorp.vn`).

---

## 🌿 1. Quy Định Đặt Tên Nhánh & Lệnh Tạo Nhánh (Branch Naming & Creation)

### 🏷️ Phân Loại Nhánh & Phân Quyền (Branch Types & Ownership)

Dự án áp dụng 9 loại nhánh theo mô hình Git Flow chuẩn của Thiên Ân:

| Nhánh (Branch) | Nhân sự phụ trách | Nhiệm vụ & Mục đích |
|---|---|---|
| **`release`** | Leader | Sản phẩm đóng gói bàn giao tới khách hàng. Quản lý và **bắt buộc tạo tag version**. |
| **`staging`** | Leader | Quản lý môi trường dàn dựng / UAT (User Acceptance Testing) để khách hàng chạy thử nghiệm thực tế. |
| **`dev`** | Leader | Nhánh phát triển chính (tích hợp các tính năng hoàn thiện). |
| **`feat`** | Nhân sự thực hiện | Tạo mới, cập nhật, xóa tính năng/chức năng (feature). |
| **`fix`** | Nhân sự thực hiện | Sửa các lỗi chức năng phát sinh trong quá trình dev/review code (fixbug). |
| **`merge`** | Nhân sự thực hiện | Nhánh trung gian tạo ra để merge code và giải quyết xung đột giữa các thành viên. |
| **`preview`** | Nhân sự thực hiện | Nhánh dùng để xem trước code phục vụ tạo Pull Request và quản lý issue. |
| **`hotfix`** | Nhân sự thực hiện | Sửa lỗi nhanh/khẩn cấp phát hiện trực tiếp trên môi trường `release` của khách hàng. |
| **`exp`** | Nhân sự thực hiện | Thử nghiệm các tính năng hoặc giải pháp công nghệ mới (experimental). |

---

### ✍️ Cú Pháp Đặt Tên Nhánh (Naming Syntax)

Tên nhánh được đặt theo một trong các cú pháp chuẩn sau:
*   **Cú pháp 1:** `[BranchKey]/[TaskCode]_[ten-cong-viec-viet-thuong-cach-nhau-boi-dau-gach-ngang]`
*   **Cú pháp 2:** `[BranchKey]/[yyyyMMdd]-[TaskCode]_[ten-cong-viec-cach-nhau-gach-ngang]`
*   **Cú pháp rút gọn (khi không có TaskCode):** `[BranchKey]/[yyyyMMdd]-[ten-cong-viec-cach-nhau-gach-ngang]`

> [!TIP]
> *   **Định dạng thời gian:** Sử dụng `yyyyMMdd` hoặc `yyyyMM`.
> *   **Hậu tố nhân sự (Khuyến nghị):** Nên đưa thêm tên viết tắt của nhân sự thực hiện vào cuối tên nhánh (ví dụ: `-sonth`, `-dathp`, `-hieunv`) để dễ dàng quản lý và phân biệt.
> *   **Ngôn ngữ:** Tên nhánh viết bằng **tiếng Việt không dấu** hoặc **tiếng Anh**, từ cách nhau bằng dấu gạch ngang `-`.

#### 💡 Ví dụ Đặt Tên Nhánh Chuẩn:
*   `feat/20250101-XD1.2.2.5_map-location`
*   `feat/20260922-XD1.2.2.5_map-location-dathp`
*   `fix/20250102-XD1.2.2.5_fix-map-location`
*   `merge/20250105-XD1.2.2.5_merge-code-dev-a-b`
*   `release/20250110-v1.0.1`

---

### 💻 Lệnh CLI Tạo Nhánh (`git checkout -b`)

Khi bắt đầu làm tính năng hoặc sửa lỗi, lập trình viên thực hiện câu lệnh sau từ terminal:

```bash
# 1. Cập nhật nhánh dev mới nhất từ remote
git checkout dev
git pull origin dev

# 2. Tạo nhánh feat mới để làm chức năng
git checkout -b feat/20260922-XD1.2.2.5_map-location-dathp

# Hoặc tạo nhánh fix để sửa lỗi
git checkout -b fix/20260922-XD1.2.2.5_fix-map-location-dathp
```

---

## 🔄 2. Luồng Vận Hành Nhánh & Xử Lý Xung Đột (Branching Workflow & Conflict Resolution)

### 📌 Trình Tự Vận Hành Git Flow (11 Bước)

1.  **Khởi tạo dự án:** Khởi tạo nhánh `release` đầu tiên và commit source code gốc của dự án với cấu trúc thư mục quy định.
2.  **Tạo nhánh dev:** Tạo nhánh `dev` tương ứng từ nhánh `release`.
3.  **Nhận chức năng mới:** Khi nhận chức năng mới, tạo branch `feat` tương ứng từ nhánh `dev`.
4.  **Phát sinh merge chéo:** Nếu có phát sinh cần merge lại code giữa các nhân sự, tạo nhánh `merge` để thực hiện merge code liên quan mà không ảnh hưởng nhánh `dev`.
5.  **Chuẩn bị Preview:** Khi hoàn tất chức năng, để preview chức năng, tạo nhánh `preview` nếu cần thiết (đặc biệt đối với chức năng phức tạp, có merge code từ nhiều nhánh).
6.  **Tạo Pull Request (PR):** Nhân sự tạo Pull Request yêu cầu merge nhánh `feat` / `preview` vào nhánh `dev` và tiến hành preview trên yêu cầu này.
7.  **Chấp thuận PR:** Sau khi preview và review hoàn tất, Leader phê duyệt merge từ các nhánh `feat` / `preview` vào `dev`.
8.  **Sửa lỗi (Fixbug):** Sau khi test ở nhánh `dev` / `test` / `staging`, nếu phát sinh lỗi, tạo nhánh `fix` để sửa lỗi tương ứng rồi merge lại vào `dev`.
9.  **Dàn dựng (Staging):** Khi đã test hoàn tất trên `dev`, Leader tạo nhánh `staging` để thiết lập môi trường dàn dựng / UAT cho khách hàng test hoặc chạy thử nghiệm thực tế.
10. **Sửa lỗi khẩn cấp (Hotfix):** Khi có lỗi phát sinh trên `release`, tạo nhánh `hotfix` để sửa lỗi gấp / nhỏ. Nếu thay đổi lớn hoặc không gấp, xử lý theo quy trình thông thường qua `feat` -> `dev`.
11. **Đóng gói phiên bản:** Nhánh `release` đại diện cho sản phẩm hoàn thiện tới khách hàng, Leader tiến hành tạo tag Git để đóng gói version (ví dụ: `v1.0.1`).

---

### 🔀 Quy Trình Xử Lý Xung Đột Khi Pull Request (PR Conflict Resolution)

Trường hợp Pull Request bị báo lỗi xung đột (Conflict) — ví dụ nhánh `staging` PR lên `release`, hoặc nhánh `feat` PR lên `dev`:

1.  **Kéo nhánh làm việc về local:** Kéo nhánh đang có PR về máy local (ví dụ: `git checkout staging && git pull origin staging`).
2.  **Merge nhánh đích vào nhánh của mình:** Thực hiện merge nhánh đích vào nhánh local:
    ```bash
    git merge release    # (Nếu đang xử lý PR staging -> release)
    # hoặc
    git merge dev        # (Nếu đang xử lý PR feat -> dev)
    ```
3.  **Giải quyết xung đột:** Mở IDE (VS Code / Visual Studio) để đối chiếu, resolve conflict, chạy test kiểm tra để đảm bảo hệ thống build và pass kiểm thử.
4.  **Commit code giải quyết xung đột:** Commit các file đã resolve conflict lên remote branch.
5.  **Chấp thuận PR:** Refresh lại trang Pull Request trên GitLab/GitHub, kiểm tra trạng thái xanh và chấp thuận (approve & merge) Pull Request.

---

## 📝 3. Quy Định Nội Dung Commit & Lệnh `git commit -m` (Commit Rules & CLI Commands)

### 🏷️ Các Từ Khóa Summary Commit
Khi thực hiện commit code, phần tiêu đề (Summary) của commit bắt buộc bổ sung các từ khóa phân loại sau:
*   **`feat`**: Commit thêm, hủy chức năng hoặc cập nhật cấu hình / thực thể / luồng xử lý (update).
*   **`fix`**: Fix lỗi chức năng phát sinh khi review/dev trên `dev`/`staging` hoặc hotfix trên `release`.
*   **`refactor`**: Cấu trúc lại mã nguồn / Tối ưu hóa code (giảm độ phức tạp, không làm thay đổi hành vi hệ thống).
*   **`chore`**: Tất cả mọi thứ khác (cập nhật tài liệu HDSD, test case, bỏ code dư thừa, cấu hình build, ...).

> [!IMPORTANT]
> Thêm ký tự **`!`** ngay sau từ khóa (ví dụ: `fix!`, `feat!`) để nhấn mạnh **Breaking Change** — thay đổi lớn có thể gây ảnh hưởng nghiêm trọng đến hệ thống hoặc làm đứt gãy luồng xử lý cũ.

---

### ✍️ Cú Pháp Thông Điệp Commit Chuẩn (Commit Format)

#### Cú pháp Summary:
- **Cú pháp chuẩn thực tế của team (Khuyến nghị — có scope):**
  `[type]([scope]): [noi-dung-cong-viec]`  
  *(Ví dụ: `feat(vms): thêm mới dịch vụ`, `fix(tms): chỉnh map`, `feat(sharedata): hoàn thiện worker luồng outbound và event`)*
- **Cú pháp kèm TaskCode:**
  `[type]([scope]): [TaskCode] - [noi-dung-cong-viec]`  
- **Cú pháp rút gọn (không có scope):**
  `[type]: [noi-dung-cong-viec]`

> [!NOTE]
> **`[scope]`** là tên module/phân hệ viết thường, ví dụ: `vms`, `tms`, `sharedata`, `videowall`, `toll`, `test`...
> Scope **không bắt buộc** nhưng **khuyến nghị** khi commit có phạm vi rõ ràng trong 1 module.

> [!TIP]
> *   Dùng 1 `-m` khi chỉ cần ghi Summary ngắn gọn: `git commit -m "feat(sharedata): hoàn thiện chức năng worker"`
> *   Dùng nhiều `-m` khi muốn bổ sung danh sách gạch đầu dòng chi tiết (Git sẽ tự chèn dòng trống ngăn cách giữa các đoạn):
>     `git commit -m "[Subject]" -m "[Subject]" -m "- gạch đầu dòng 1`\n`- gạch đầu dòng 2"`
> *   Sử dụng câu hành động cụ thể, tiếng Việt hoặc tiếng Anh thống nhất.

#### Cấu trúc Commit Message Chi Tiết (Chuẩn thực tế của team):
1.  **Dòng 1 (Summary):** `[type]([scope]): [noi-dung-cong-viec]`
2.  **Dòng 2:** Lặp lại nguyên văn nội dung Summary (⛔ **TUYỆT ĐỐI KHÔNG thêm chữ `Description:`**).
3.  **Nội dung chi tiết:** Gạch đầu dòng (`- `) các công việc cụ thể đã thực hiện trong lần commit này.
4.  ⛔ **TUYỆT ĐỐI KHÔNG tự ý chèn thẻ metadata** như `Ref: ...`, `Reviewer: ...`, `CR: ...` ở cuối commit nếu không có yêu cầu trực tiếp từ Leader.

---

### 💡 Ví Dụ Minh Họa Commit Chuẩn

#### Ví dụ 1: Summary chuẩn thực tế của team (có scope — khuyến nghị)
*   `feat(vms): thêm mới dịch vụ`
*   `feat(toll): thêm mới fms`
*   `fix(tms): chỉnh map`
*   `fix(toll): điều chỉnh lại tên xử lý các hàm`
*   `feat(sharedata): hoàn thiện chức năng worker outbound và event`
*   `feat(videowall): tích hợp NATS thật cho VwCommandConsumer`
*   `chore(test): bỏ unnecessary usings IDE0005 trong folder tests`

#### Ví dụ 2: Toàn văn Commit Message đầy đủ Summary + Bullet points (Chuẩn form thực tế)
```text
fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

- Cấu hình đăng ký: giới hạn chu kỳ max 86400s, clamp real-time (Issue 1, 2)
- Đối tác chia sẻ: xóa mã khi copy, khóa ô mã khi sửa kèm tooltip (Issue 3, 4)
- Ánh xạ dữ liệu: cập nhật thông báo dữ liệu nguồn rỗng, ẩn nhóm trường ở header (Issue 20, 23, 24)
- Đa ngôn ngữ: bổ sung bản dịch tiếng Việt và tiếng Anh cho các nhãn, tooltip và thông báo
```

---

### 💻 Hướng Dẫn Dùng Lệnh `git commit -m` Chuẩn Xác

Khi thao tác trên terminal/command line, sử dụng các cú pháp `git commit -m` như sau:

#### Cách 1: Commit nhanh 1 dòng Summary (Single-line Summary)
Phù hợp cho các commit nhỏ, cục bộ trong quá trình dev:
```bash
git commit -m "feat(videowall): XD1.2.2.5 - add map location"
```

#### Cách 2: Commit đầy đủ trên Git Bash / Linux (Dùng nhiều cờ `-m`)
*Mẹo: Trong Git CLI, mỗi cờ `-m` sẽ được tự động ghép lại thành một đoạn văn bản riêng biệt cách nhau 1 dòng trống. ⛔ Không dùng nhãn `Description:` và ⛔ không gắn metadata `Ref:`:*
```bash
git commit -m "fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata" \
           -m "fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata" \
           -m "- Cấu hình đăng ký: giới hạn chu kỳ max 86400s, clamp real-time (Issue 1, 2)
- Đối tác chia sẻ: xóa mã khi copy, khóa ô mã khi sửa kèm tooltip (Issue 3, 4)
- Ánh xạ dữ liệu: cập nhật thông báo dữ liệu nguồn rỗng, ẩn nhóm trường ở header (Issue 20, 23, 24)"
```

#### Cách 3: Commit đầy đủ trên Windows PowerShell (Dùng escape `` `n `` cho dòng mới)
```powershell
git commit -m "fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata" `
           -m "fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata" `
           -m "- Cấu hình đăng ký: giới hạn chu kỳ max 86400s, clamp real-time (Issue 1, 2)`n- Đối tác chia sẻ: xóa mã khi copy, khóa ô mã khi sửa kèm tooltip (Issue 3, 4)`n- Ánh xạ dữ liệu: cập nhật thông báo dữ liệu nguồn rỗng, ẩn nhóm trường ở header (Issue 20, 23, 24)"
```

---

### 🏭 Ghi Chú Tương Thích: Nhánh Sub-repo WebAPI / WebVue
Riêng trường hợp làm việc trên các nhánh cũ thuộc 2 repo con `TA-ITS015-WEBAPI-V1.0` và `TA-ITS015-WEBVUE-V1.0`, nếu dự án đã có tiền lệ dùng lại nguyên văn tên nhánh làm dòng Summary:
*Ví dụ:* `feat/20260826-XD001.5.5-Service-tich-hop-du-lieu` thì lập trình viên có thể áp dụng theo format đó. Tuy nhiên, **khuyến nghị chung của toàn công ty theo biểu mẫu F10** vẫn là áp dụng định dạng `[Keyword]: [TaskCode] - [nội dung công việc]`.

---

## ✅ 3.5. 10 Công Việc Bắt Buộc Kiểm Tra Trước Khi Commit Code (Pre-Commit Checklist)

> ⚠️ **BẮT BUỘC:** Lập trình viên phải hoàn thành kiểm tra 10 đề mục sau trước khi thực hiện commit code lên repository:

1.  **Kiểm tra cú pháp và lỗi biên dịch:** Đảm bảo toàn bộ project build thành công (`dotnet build` / `npm run build`), không có lỗi cú pháp hoặc cảnh báo nghiêm trọng.
2.  **Kiểm tra logic code:** Đảm bảo logic xử lý đúng theo đặc tả nghiệp vụ. Chạy thử trên môi trường phát triển (Dev) để xác nhận kết quả trước khi commit.
3.  **Kiểm tra chuẩn code (Coding Standards):** Đảm bảo tuân thủ tiêu chuẩn lập trình của nhóm (quy tắc đặt tên biến/hàm, PascalCase, camelCase, cấu trúc thư mục, Clean Code).
4.  **Xóa code không cần thiết:** Loại bỏ hoàn toàn các dòng code debug, `console.log`, comment tạm thời không còn sử dụng. Tối ưu hóa code nếu cần.
5.  **Chạy các bài kiểm thử (Test Cases):** Chạy toàn bộ các bài kiểm thử tự động (Unit Test, Integration Test). Đảm bảo **100% test cases pass**. Thêm hoặc cập nhật test case nếu có thay đổi logic nghiệp vụ.
6.  **Kiểm tra xung đột merge (Merge Conflicts):** **Cập nhật branch của bạn với branch chính (`dev`)** bằng cách merge `dev` vào branch của bạn để phát hiện và giải quyết xung đột trước khi commit/tạo PR. Nếu cùng làm chung với thành viên khác, tạo nhánh merge để giải quyết xung đột chéo.
7.  **Kiểm tra tài liệu:** Thêm hoặc cập nhật tài liệu kỹ thuật, hướng dẫn cài đặt, cấu hình liên quan nếu có thay đổi logic hoặc API. Đảm bảo comment mô tả rõ ràng.
8.  **Kiểm tra file & commit message:** Đảm bảo chỉ commit các file liên quan trực tiếp đến thay đổi. Kiểm tra không bỏ sót file cấu hình cần thiết. Viết commit message đúng chuẩn cú pháp quy định. Cập nhật `CHANGELOG.md` nếu cần.
9.  **Kiểm tra quyền truy cập & thông tin nhạy cảm:** Tuyệt đối không commit các thông tin nhạy cảm như API keys, credentials, mật khẩu CSDL hoặc dữ liệu cá nhân. Kiểm tra kỹ file `.gitignore`.
10. **Chạy thử trên môi trường staging:** Nếu có thể, kiểm tra lại các thay đổi trên môi trường staging để đảm bảo hệ thống vận hành ổn định trước khi release.

---

## 🚫 4. Quy Định Tự Động Hóa Đối Với Trợ Lý AI (AI Execution Rules)

Đối với Trợ lý AI, tuyệt đối tuân thủ các nguyên tắc sau khi làm việc trong dự án:

1.  **KHÔNG TỰ ĐỘNG CHẠY LỆNH BUILD & TEST KHI CHỈ ĐỔI TÊN BIẾN (ONLY BUILD/TEST ON LOGIC CHANGES)**: AI không được tự động chạy lệnh `dotnet build`, `dotnet test` hoặc bất kỳ lệnh biên dịch/kiểm thử nào sau khi chỉnh sửa code, trừ khi người dùng yêu cầu trực tiếp. Đặc biệt, đối với các trường hợp chỉ **đổi tên biến, đổi tên tham số**, format mã nguồn hoặc chỉnh sửa comment, TUYỆT ĐỐI KHÔNG chạy build hay chạy test tốn thời gian; CHỈ chạy build và chạy test khi có thay đổi **logic nghiệp vụ**, cấu trúc giải thuật, câu truy vấn CSDL, điều kiện rẽ nhánh, hoặc khi người dùng yêu cầu trực tiếp.
2.  **CẤM TUYỆT ĐỐI TỰ Ý CAN THIỆP STAGING AREA: CẤM TỰ Ý `git add` LẪN `git reset` / UNSTAGE, CẤM COMMIT VÀ PUSH (STRICT NO AUTO-STAGE / NO AUTO-UNSTAGE / NO AUTO-COMMIT) (P0)**:
    - AI **TUYỆT ĐỐI KHÔNG ĐƯỢC CHẠY LỆNH `git add`** hoặc đưa bất kỳ file nào vào Staged Changes khi người dùng KHÔNG yêu cầu trực tiếp và tường minh.
    - AI **TUYỆT ĐỐI KHÔNG ĐƯỢC CHẠY LỆNH `git reset`**, `git restore --staged` hoặc tự ý unstage bất kỳ file nào mà người dùng đã chủ động đưa vào Staged Changes. Mọi file người dùng đã stage phải được giữ nguyên trạng thái Staged.
    - **Mọi lần sửa code BẮT BUỘC để nguyên ở trạng thái Changes (Working Tree / Unstaged)** để người dùng tự review qua giao diện IDE (Source Control / Git Diff).
    - Quyền stage (`git add`), unstage (`git reset`), commit (`git commit`) và push (`git push`) hoàn toàn thuộc về lập trình viên. AI chỉ cung cấp câu lệnh gợi ý (nếu cần), tuyệt đối không tự ý can thiệp.
3.  **TỐI THIỂU HÓA THAY ĐỔI (MINIMAL DIFF PRINCIPLE)**: AI CHỈ ĐƯỢC PHÉP chỉnh sửa/thêm code đối với các file và nội dung thực sự phục vụ trực tiếp cho tính năng mới hoặc bug được yêu cầu. TUYỆT ĐỐI KHÔNG tự động upgrade phiên bản thư viện (`PackageReference` trong `.csproj`), không format/touch vào các file không liên quan, không làm thay đổi các file dùng chung (`Shared.Reference`, `appsettings.json`,...) trừ khi có chỉ định rõ ràng từ người dùng.
4.  **PHÂN BIỆT THAM KHẢO VÀ HÀNH ĐỘNG (DISTINGUISH REFERENCE FROM ACTION)**: Khi người dùng yêu cầu "tham khảo", "xem thử", "giải thích" hoặc hỏi ý kiến, AI BẮT BUỘC phải phân tích và trả lời thảo luận trước, KHÔNG ĐƯỢC tự ý nhảy vào áp dụng hoặc thêm/sửa code khi chưa có xác nhận từ người dùng.
5.  **TÔN TRỌNG CODE SỬA TAY & Ý ĐỊNH NGƯỜI DÙNG (PRESERVE USER MANUAL EDITS & PREFERENCES)**: Khi người dùng đã chỉ định cách viết (VD: dùng `while (reader.Read())` đồng bộ) hoặc tự sửa tay/bỏ bớt điều kiện, AI KHÔNG ĐƯỢC TỰ Ý hoàn tác (revert) hoặc sửa ngược lại về cách viết cũ trong các lần refactor tiếp theo.
6.  **GIỮ NGUYÊN THUẬT NGỮ TIẾNG ANH CHUYÊN NGÀNH (KEEP TECHNICAL ENGLISH KEYWORDS AS-IS)**: Các từ tiếng Anh mang tính chất thuật ngữ kỹ thuật, tên thuộc tính, tên tham số giao thức (protocol/API), tên tính năng, hoặc keyword nghiệp vụ (như `Probe`, `Ping`, `WallNo`, `Video Wall`, `Outputs`, `Inputs`, `SubWindow`, `Scene`, `Preset`, `Payload`, `Endpoint`, `Path Parameters`, `Body Parameters`, `Advanced Parameters`, `Digest Auth`, `Circuit Breaker`...) BẮT BUỘC giữ nguyên tiếng Anh gốc, TUYỆT ĐỐI KHÔNG dịch gượng ép sang tiếng Việt (như dịch `Probe` thành "khảo sát", `WallNo` thành "tường số", `Video Wall` thành "tường ghép", `SubWindow` thành "cửa sổ con"...) gây tối nghĩa, nhập nhằng và khó đối chiếu với tài liệu/spec chuẩn.
7.  **TUYỆT ĐỐI KHÔNG CHẠY THỦ CÔNG CÂU LỆNH `ALTER TABLE` / DDL VÀ CẤM TỰ VIẾT HÀM TẠO BẢNG NHƯ `EnsureTablesCreated()`**: Xem mục 6 "Cấm Gọi InitTables<T>() / Tạo Hàm EnsureTablesCreated() Trong Host/Test" để biết đầy đủ quy tắc và cách xử lý đúng (bật cờ `TableSettings` trong `tests/appsettings.Test.json`) — không lặp lại ở đây (đã gộp 25/09/2026 để tránh trùng ý).
8.  **CẤM TỰ ĐỘNG XÓA FILE PROMPT KHI HOÀN TẤT TASK**: Xem mục 13 "Quy Tắc Vị Trí File Prompt / Plan / Task" (và mục 14 cho ghi chú tạm scratch) — không lặp lại ở đây (đã gộp 25/09/2026 để tránh trùng ý).
9.  **TRIẾT LÝ VIẾT TEST: BẮT BUỘC VIẾT TEST CASE TOÀN TRÌNH NGHIỆP VỤ (FULL BUSINESS FLOW), TUYỆT ĐỐI CẤM VIẾT TEST VỤN VẶT / MICRO UNIT TEST RỜI RẠC**:
    - AI tuyệt đối **KHÔNG viết các unit test vụn vặt, vi mô (micro tests)** chỉ để kiểm tra từng hàm helper phụ trợ nhỏ, từng phép toán static, từng nhánh if/else nhỏ lẻ với object giả lập in-memory rời rạc (ví dụ điển hình bị cấm: các test scheduler tính lịch chạy với stub giả `Daily_Kind_No_DaysOfWeek_Accepts_Any_Day`, `Default_IntervalSeconds_When_Null`...).
    - **BẮT BUỘC** chỉ viết các test case luồng nghiệp vụ hoàn chỉnh (Full Business Flow / Integration Test) chạy trên CSDL Test Local thật (hoặc luồng nghiệp vụ tích hợp đầy đủ các chặng), kiểm chứng toàn diện từ đầu vào đến đầu ra nghiệp vụ (Ví dụ chuẩn: `ProcessScheduledSubscriptions_WhenEnvironmentIsStaging_SkipsFileWrite_ApiSucceeds_ExportsSuccessfullyAndAdvancesWatermark_Test` — kiểm chứng trọn vẹn luồng quét DB, kiểm tra điều kiện môi trường, trích xuất dữ liệu, ánh xạ, vận chuyển API/File, tịnh tiến watermark/checkpoint và ghi nhận nhật ký hệ thống).
10.5. **BẮT BUỘC VIẾT PROMPT CHO MỌI THAY ĐỔI FILE CODE SẢN XUẤT — KHÔNG TỰ Ý SỬA TRỰC TIẾP (ALWAYS WRITE A PROMPT FOR PRODUCTION CODE CHANGES, NEVER INLINE-EDIT) (đã chốt 25/09/2026)**: Khi phát hiện hoặc được yêu cầu sửa/đổi tên/tái cấu trúc/xoá đoạn code trong file mã nguồn sản xuất (`.cs`, `.vue`, `.ts`, `.ps1`...), AI **TUYỆT ĐỐI KHÔNG dùng công cụ sửa file trực tiếp** (kiểu `Edit`/`Write`/`replace_file_content`) lên các file đó — **kể cả thay đổi nhỏ 1 dòng** (đổi tên 1 biến, xoá 1 câu lệnh, thêm 1 dòng comment). BẮT BUỘC viết ra file prompt thực thi (`{task-slug}-prompt.md`, đặt tại đúng thư mục `Prompt/` của phân hệ theo mục 13) mô tả đầy đủ đoạn TRƯỚC/SAU, lý do đổi, phạm vi rà tác động tới test, và bước kiểm chứng — để người dùng tự đọc, tự áp dụng, tự chạy `dotnet build`/`dotnet test`/`git commit`. ⛔ **Ngoại lệ duy nhất**: file tài liệu `.md` (báo cáo review, `MasterPlan`, `README.md` của `Plan/`/`Prompt/`, chính file quy tắc này) vẫn được AI sửa trực tiếp như bình thường, không cần qua prompt — quy định này chỉ áp dụng cho file mã nguồn thực thi được.
11. **ƯU TIÊN TUYỆT ĐỐI GORTEX MCP + DAB MCP STAGING — DUAL SOURCE OF TRUTH (GORTEX-FIRST & DAB-MCP-FIRST, DEFAULT-AS-FALLBACK)**:
    - **Nguyên tắc hành động — Code:** Mọi thao tác tìm kiếm, đọc, phân tích và sửa mã nguồn (C#, Vue, TypeScript, SQL, XAML...) BẮT BUỘC phải gọi công cụ Gortex MCP (`call_mcp_tool` với `ServerName: "gortex"`) trước tiên — để có dữ liệu chính xác nhất từ đồ thị.
    - **Nguyên tắc hành động — DB:** Mọi kiểm tra schema / dữ liệu / cột / bảng BẮT BUỘC đọc trực tiếp từ DB staging `mssql_staging` (`10.10.8.30/DEV_ITS10` — source of truth) qua DAB MCP (`mssql_staging__describe_entities` / `read_records` / `aggregate_records` với `autoentities: dbo-readonly`) hoặc `sqlcmd -C -S 10.10.8.30` read-only. Tuyệt đối không suy đoán cột/bảng từ Entity code hay tài liệu cũ.
    - **Cơ chế Fallback (Khi nào dùng tool mặc định):** CHỈ KHI Gortex/DAB MCP không thực hiện được (báo lỗi, timeout, file/symbol chưa có trong index đồ thị, MCP báo `Connection closed`, hoặc khi thao tác trên file tài liệu Markdown `DocBusinessThienAn/`, `.agents/`, `*.md`) thì AI mới chuyển sang (fallback) dùng các công cụ mặc định của hệ thống (`grep_search`, `view_file`, `find_by_name`, `replace_file_content`, `write_to_file`, `sqlcmd -C`).
    - **Khi MCP báo `Connection closed`:** Tự chẩn đoán `dotnet tool run --allow-roll-forward dab validate --config .agents/dab-config.staging.json` theo `.agents/memory/mcp-dab-database-access.md` — do config drift, không phải lỗi mạng.
    - **Công cụ Gortex tương ứng:**
      - *Tìm kiếm symbol/hàm/class/interface:* `gortex.search_symbols` / `gortex.get_symbol` (thay cho `grep_search`).
      - *Tìm nơi sử dụng / hàm gọi:* `gortex.find_usages` / `gortex.get_callers` / `gortex.get_call_chain`.
      - *Phân tích quan hệ & ảnh hưởng (Blast Radius):* `gortex.explore` / `gortex.get_dependencies` / `gortex.analyze`.
      - *Đọc code & ngữ cảnh thông minh:* `gortex.smart_context` / `gortex.read_file` / `gortex.get_symbol_source`.
      - *Chỉnh sửa code:* `gortex.edit_file` / `gortex.edit_symbol` / `gortex.preview_edit`, `gortex.batch_edit`.
    - **Công cụ DAB MCP tương ứng:**
      - *Liệt kê bảng/cột:* `mssql_staging__describe_entities` / `INFORMATION_SCHEMA.COLUMNS` qua `sqlcmd -C`.
      - *Đọc mẫu/thống kê:* `mssql_staging__read_records` / `aggregate_records` (read-only, `anonymous:read`).
12. **CẤM TỰ Ý THÊM `using` DƯ THỪA / TRÙNG LẶP VỚI `GlobalUsings.cs` (LỖI IDE0005 BỊ LIÊN TỤC — ZERO UNNECESSARY USINGS)**:
    - **Thực trạng & Nguyên nhân**: AI thường có phản xạ tự động chèn một loạt chỉ thị `using` ở đầu file C# (`using SqlSugar;`, `using Microsoft.Extensions.Logging;`, `using Module.ShareData.Core.Entities;`, `using System.Threading.Tasks;`...). Trong khi đó, hầu hết các project trong solution đều đã bật `<ImplicitUsings>enable</ImplicitUsings>` hoặc có file `GlobalUsings.cs` (như `ShareDataWorker/GlobalUsings.cs`, `tests/GlobalUsings.cs`, `Modules.[TênHệ]/GlobalUsings.cs`...) đã khai báo `global using` sẵn các namespace dùng chung này. Việc chèn thêm cục bộ khiến Roslyn Analyzer liên tục cảnh báo `IDE0005: Using directive is unnecessary` (hoặc `CS0105`), gây bẩn code và làm phiền lập trình viên.
    - **3 Nguyên Tắc Bắt Buộc Đối Với AI**:
      1. **Kiểm tra `GlobalUsings.cs` trước khi thêm**: Trước khi chèn bất kỳ `using` nào vào đầu file C#, AI BẮT BUỘC kiểm tra file `GlobalUsings.cs` của chính project đó (và `tests/GlobalUsings.cs` nếu là file test). Nếu namespace đã có trong `GlobalUsings.cs` hoặc implicit usings, **TUYỆT ĐỐI CẤM** thêm vào file riêng lẻ.
      2. **Cấm chèn `using` theo quán tính**: Tuyệt đối không copy-paste cả khối `using` mặc định vào đầu file mới hoặc file chỉnh sửa. Chỉ thêm đúng những namespace đặc thù thực sự cần mà `GlobalUsings.cs` chưa có.
      3. **Tự động dọn dẹp sạch (Clean-up Gate)**: Sau khi tạo mới, chỉnh sửa hoặc refactor code (đặc biệt sau khi xoá/thay thế kiểu dữ liệu), AI BẮT BUỘC rà soát lại toàn bộ khối `using` ở đầu file, xóa bỏ ngay lập tức các dòng `using` không còn sử dụng trong file hoặc trùng lặp với `GlobalUsings.cs` trước khi bàn giao cho người dùng.
13. **CẤM TỰ Ý CHẠY `npm run build` KHI SỬA CODE FRONTEND (CLIENT ĐANG CHẠY CÓ HOT-RELOAD - STRICT NO AUTO-BUILD ON FRONTEND) (P0)**:
    - **Thực trạng**: Phía frontend (`TA-ITS015-WEBVUE-V1.0`), môi trường phát triển (Client Dev Server qua Vite / Webpack / pnpm) thường xuyên được lập trình viên khởi chạy nền (`client start`) và đã tích hợp sẵn cơ chế Hot-Module Replacement (Hot-Reload / HMR).
    - **Quy định bắt buộc**: Sau khi tạo hoặc chỉnh sửa code Frontend (`.vue`, `.ts`, `.js`, `.scss`, `.css`...), AI **TUYỆT ĐỐI KHÔNG tự động chạy lệnh `npm run build`** (hoặc `pnpm run build`), vì lệnh này tốn thời gian, ngốn tài nguyên và không cần thiết khi client dev server đã tự động nạp thay đổi qua hot-reload ngay lập tức trên trình duyệt.
    - AI **CHỈ** được chạy lệnh `npm run build` khi người dùng yêu cầu trực tiếp và tường minh (ví dụ: *"chạy build kiểm tra lỗi"* hoặc khi chuẩn bị đóng gói release).

> [!NOTE]
> - Các quy chuẩn code/hạ tầng chung của dự án (Docker, Entity, Swagger, header comment...) áp dụng cho **cả người lẫn AI** — xem tại mục 5 bên dưới, không lặp lại ở đây để tránh trùng lặp nội dung.
> - **Plan Mode (4-Phase) của `code-rules.md`**: bước 4 IMPLEMENTATION của dự án ThienAn có thêm yêu cầu so với bản gốc AG-Kit — **BẮT BUỘC chạy lại toàn bộ test liên quan + bổ sung test case mới** cho code/UI/logic mới tạo (không chỉ "Code + tests" chung chung), và test case mới phải tuân thủ nghiêm ngặt nguyên tắc Full Business Flow ở trên.

---

## 🏗️ 5. Quy Định Cấu Hình & Thiết Kế Module (Module Architecture & Configuration Rules)

Các hệ thống / Module phát triển mới về sau bắt buộc tuân thủ mô hình thiết kế chuẩn như sau:

1. **Phân tách Project Layer**:
   * **`Modules.[TênHệ].Core`**: Chứa toàn bộ Entity, DTO (Data Transfer Object), Interfaces, Enums và Business Core Logic của Module.
     * *Ví dụ:* `Modules.ShareData.Core` chứa các Entities (`SharesConfig.cs`,...), DTOs.
   * **`Modules.[TênHệ]`**: Chứa Controllers, Application Services, API Endpoints, Dependency Injection Extensions.
     * *Ví dụ:* `Modules.ShareData` chứa `SharesConfigController.cs`, `BaseController.cs`, Services.

2. **Cấu hình BaseController & Swagger Auto-Discovery**:
   * Mỗi Module bắt buộc có file `BaseController.cs` riêng nằm tại `Modules.[TênHệ].Controllers`.
   * Khai báo hằng số `GroupName` (VD: `GroupName = "ShareData"`) và `BasePath` (VD: `BasePath = "api/vms"`), gán attribute `[ApiDescriptionSettings(GroupName)]` để Furion/Swagger tự động quét nhóm API (Auto-Discovery). Tuyệt đối KHÔNG cấu hình khai báo thủ công trong `Swagger.json`.
   ```csharp
   namespace Modules.ShareData.Controllers
   {
       /// <summary>
       /// Base Controller của Module ShareData
       /// Created date: 24/07/2026
       /// </summary>
       [ApiDescriptionSettings(GroupName)]
       [Route(BasePath + "/[controller]")]
       public abstract class BaseController : AppControllerBase
       {
           public const string GroupName = "ShareData";
           public const string BasePath = "api/vms";
       }
   }
   ```

3. **Quy định đặt tên & cấu trúc cho Sub-module / Controller Báo cáo (Report)**:
   * **Trường hợp tách thành Sub-project Báo cáo riêng biệt**:
     * Đặt tên Project là: **`Modules.[TênHệ].Report`** (Nằm trong thư mục `src/Modules/[TênHệ]/Modules.[TênHệ].Report/`).
     * Nếu có Entities/DTOs riêng cho báo cáo: Tạo **`Modules.[TênHệ].Report.Core`**.
     * Namespace Controller: `Modules.[TênHệ].Report.Controllers`.
     * BaseController của Report quy định `GroupName = "[TênHệ]Report"` (ví dụ `GroupName = "ShareReport"`) và `BasePath = "api/vms/[TênHệ]Report"`.
   * **Trường hợp nằm chung trong Project `Modules.[TênHệ]`**:
     * Đặt trong thư mục `Controllers/Report/` (ví dụ `Modules.Shares/Controllers/Report/`).
     * Kế thừa `BaseController` chung của Module hoặc tạo `ReportBaseController` riêng nếu muốn gom thành Group Swagger riêng (`ShareReport`).

4. **Giao tiếp giữa các Module (Inter-Module Communication)**:
   * **Ưu tiên hàng đầu**: Sử dụng Event Bus (`MessBus`) để đảm bảo Loose Coupling (các Module không phụ thuộc trực tiếp code của nhau).
   * **Trường hợp gọi trực tiếp Sync**: Sử dụng Refit API Interface trong `Shared.Utility.Apis.[TênHệ]` hoặc Inject Service Interface.

5. **Cấu Trúc Chi Tiết Thư Mục Module & Xử Lý API (Wolverine & FluentValidation)**:
   * Mỗi thực thể/chức năng chính trong Project `Modules.[TênHệ]` phải được cấu trúc thành một thư mục riêng biệt đặt trong `Controllers/<TênChứcNăng>/` với các thư mục con sau:
     * **`Controllers/<TênChứcNăng>/<TênChứcNăng>Controller.cs`**: Controller siêu mỏng (Thin Controller), **BẮT BUỘC** chỉ dùng `MessBus.InvokeAsync()` để gọi Commands/Queries. Không viết bất kỳ logic nghiệp vụ nào tại đây.
     * **`Commands/`**: Chứa Handler xử lý Ghi (Add/Update/Delete). **BẮT BUỘC** implement `IWolverineHandler` và định nghĩa các hàm `HandleAsync(<InputType> command)`. Dùng `Mapster` để map DTO sang Entity. TUYỆT ĐỐI KHÔNG tự viết các hàm trợ giúp thủ công như `ValidateInput` hoặc `MapToOutput` bên trong CommandHandler; dùng FluentValidation và Mapster.
     * **`Queries/`**: Chứa Handler xử lý Đọc (Page/GetList/GetById). Các truy vấn phân trang phải trả về `SqlSugarPagedList<Output>`, sử dụng `.OrderBuilder()` và `.ToPagedListAsync()`. Khi dùng `.Select(x => new TOutput { ... }, true)` hoặc truy vấn trực tiếp ra `SqlSugarPagedList<TOutput>`, BẮT BUỘC trả thẳng đối tượng phân trang (VD: `return paged;` hoặc `return await query.ToPagedListAsync(...)`), KHÔNG bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()`.
     * **`Dto/`**: Chứa DTO Input và Output:
       * Input: `PageXxxInput` (kế thừa `BasePageInput`), `AddXxxInput` (kế thừa Entity gốc), `UpdateXxxInput` (kế thừa `AddXxxInput`), `DeleteXxxInput` (kế thừa `BaseIdInput`).
       * Output: `XxxOutput` / `PageXxxOutput` (kế thừa Entity gốc). Cấu hình ánh xạ Mapster (`IRegister`) BẮT BUỘC viết trực tiếp bên trong file DTO Output tương ứng (VD: `EshPartnerOutput.cs` chứa `public class EshPartnerMapper : IRegister`), KHÔNG tạo thư mục `Mappings` riêng rẽ.
       * KHÔNG dùng các thuộc tính DataAnnotation validation (`[Required]`, `[Range]`, `[StringLength]`...) trong các class DTO. Tất cả logic kiểm tra dữ liệu và thông báo lỗi đa ngôn ngữ BẮT BUỘC thực hiện 100% qua FluentValidation (`AbstractValidator<T>`) kết hợp `IStringLocalizer lz` và `BaseMsg`.
     * **`Validators/`**: Chứa `AbstractValidator<T>` (FluentValidation) kiểm tra tính hợp lệ dữ liệu đầu vào của Add/Update/Delete. Validator chỉ khai báo duy nhất 1 Constructor nhận `IStringLocalizer lz` (hoặc `localizer`), KHÔNG tự ý chèn các class phụ/mock như `DesignTimeLocalizer` hay constructor không tham số `: this(...)`.
     * **Repository Naming**: Đặt tên biến Repository trong CommandHandler / QueryHandler theo chuẩn prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`). KHÔNG dùng `_repository`, `_repo`, hay `_baseRepository`.
   * **GlobalUsings.cs**: Mỗi module bắt buộc phải có file `GlobalUsings.cs` khai báo tối thiểu:
     ```csharp
     global using Furion.DependencyInjection;
     global using Furion.FriendlyException;
     global using Microsoft.AspNetCore.Mvc;
     global using Shared.Core.Domain;
     global using SqlSugar;
     global using System.ComponentModel;
     global using System.Data;
     global using System.Linq.Dynamic.Core;
     global using Wolverine.Attributes;
     [assembly: WolverineModule]
     ```

6. **Quy định Entity Class**:
   * Tất cả các Entity class trong hệ thống bắt buộc phải kế thừa `EntityTenant` (từ `Shared.Core.Domain`).
   * **Thuộc tính Base Class `EntityTenant`**: Base class dùng `CreateTime` và `UpdateTime` (KHÔNG phải `CreatedTime` hay `UpdatedTime`). Thuộc tính ID viết HOA cả hai ký tự: `ID` (KHÔNG phải `Id`).
   * **Hằng số độ dài `EntityConst`**: BẮT BUỘC dùng các hằng số `EntityConst` (từ namespace `Shared.DTO.Constants.Application`, VD: `EntityConst.Length32`, `EntityConst.Length64`, `EntityConst.Length128`, `EntityConst.Length256`, `EntityConst.Length512`, `EntityConst.KeyFieldLength`) cho tất cả attribute `[SugarColumn(Length = ...)]` và `[MaxLength(...)]` trong Entity và DTO. KHÔNG ĐƯỢC dùng số hardcode trực tiếp (như `Length = 32`).
   * **Cấm gán `Length` cho kiểu không phải chuỗi (Lỗi SQL Server 2716)**: Tham số `Length = EntityConst.Length...` CHỈ ĐƯỢC PHÉP dùng cho các thuộc tính kiểu chuỗi (`string` / `string?`). TUYỆT ĐỐI CẤM gán `Length` cho các kiểu số nguyên (`int`, `long`, `short`, `byte`), số thực (`float`, `double`, `decimal`), ngày tháng (`DateTime`), hoặc boolean (`bool`). Nếu gán `Length` vào kiểu số, khi bật SqlSugar CodeFirst (`EnableInitTable` / `EnableIncreTable`), SqlSugar sẽ sinh ra DDL không hợp lệ dạng `[ColName] INT(64)` khiến SQL Server quăng lỗi `SqlSugarException: Column, parameter, or variable: Cannot specify a column width on data type int (Error 2716)`. Đối với các kiểu số và ngày tháng, chỉ dùng `[SugarColumn(IsNullable = true)]` hoặc `ColumnDescription` (với decimal dùng `DecimalDigits = ...`).
   * **Quy định `IsNullable = true` cho tất cả các cột Entity**: Khi tạo mới hoặc cập nhật Entity trong hệ thống (sử dụng SqlSugar CodeFirst), tất cả các thuộc tính/cột mapping CSDL (ngoại trừ khoá chính `ID` do base class `EntityTenant` quản lý) **BẮT BUỘC** phải khai báo `[SugarColumn(IsNullable = true, ...)]` và sử dụng kiểu nullable (`string?`, `DateTime?`, `int?`, `long?`, `bool?`...). Tuyệt đối **KHÔNG** đặt `IsNullable = false` ở mức schema/bảng CSDL để đảm bảo tính an toàn và khả năng tương thích khi migrate/nâng cấp schema (tránh lỗi xung đột SQL Server `ALTER TABLE` khi bảng đã tồn tại dữ liệu). Mọi nghiệp vụ kiểm tra trường bắt buộc (mandatory/required) và tính toàn vẹn dữ liệu **BẮT BUỘC** phải được kiểm tra ở tầng logic ứng dụng (FluentValidation, Service check hoặc Gate check).
   * **Bản thiết kế Entity gốc là nguồn sự thật (Source of Truth)**: Khi có thư mục `EntityUpdate` hoặc bất kỳ bộ Entity gốc nào được đưa vào từ team thiết kế, đó là bản thiết kế chính thức. AI BẮT BUỘC phải đồng bộ entity trong code hiện tại theo ĐÚNG cấu trúc, kiểu dữ liệu, tên property, và attribute của bản thiết kế gốc.

7. **Quy định Header Comment của Class & XML Doc**:
   * Mỗi Class khi tạo mới hoặc cập nhật BẮT BUỘC phải có khối XML summary comment ở đầu Class theo mẫu (chỉ dùng `Created date:`, KHÔNG dùng `Author:` — quyết định 05/09/2026, không hồi tố class đã có sẵn `Author: Đạt` — và KHÔNG dùng `Updated date:`):
     ```csharp
     /// <summary>
     /// Description: [Mô tả chức năng / Tên bảng / Interface]
     /// Created date: [dd/MM/yyyy]
     /// </summary>
     ```
   * 🔴 **XML Summary PHẢI NGẮN — `Description:` tối đa 2–3 dòng (chốt 28/09/2026)**:
     - Khối `/// <summary>` chỉ trả lời **"cái này là gì / làm gì"**. Quá 3 dòng `Description:` là đang viết nhầm chỗ.
     - ⛔ **CẤM nhồi vào XML doc**: lý do chọn thiết kế, lịch sử quyết định, so sánh với phương án bị bác, biện luận *"vì sao không làm cách kia"*, lời dặn dò người bảo trì tương lai.
     - **Chỗ đúng của phần lý do**: mục nghiệp vụ tương ứng trong `Plan/<Xx>_MasterPlan.md`, hoặc chính tệp prompt sinh ra thay đổi đó. Lý do: XML doc bật lên IntelliSense **mỗi lần gõ tên biến** — nhét cả bản thiết kế vào đó là bắt người đọc nuốt 10 dòng biện luận chỉ để biết một `Dictionary` chứa gì.
     - **Dấu hiệu nhận biết đã viết sai**: khối comment **dài hơn đoạn code nó mô tả**, hoặc chứa các cụm *"có chủ đích"*, *"lý do là"*, *"nên không thuộc diện"*, *"khác với ..."*.
     - **Lỗi thật đã mắc**: `DataChangeTrackingService._missingTables` có XML doc **10 dòng**, trong đó 7 dòng là bản thiết kế chép lại từ `Sharedata_MasterPlan.md` §6b:
       ```csharp
       // ❌ SAI — 10 dòng, 7 dòng cuối là biện luận thiết kế
       /// <summary>
       /// Description: Các bảng nguồn đang bị cô lập do mất Change Tracking, kèm mốc sớm nhất được thử lại.
       ///              Lưu RAM có chủ đích: trạng thái này suy ra được từ sys.change_tracking_tables bất cứ
       ///              lúc nào, nên khởi động lại thì chu kỳ đầu tự phát hiện lại, không có gì cần bảo toàn.
       ///              Mỗi bảng giữ mốc hẹn RIÊNG để một bảng hỏng muộn không đẩy lùi lượt thử lại của
       ///              bảng đã hỏng trước đó.
       ///              Mốc dùng DateTime.UtcNow là đúng ở đây: nó chỉ so với chính nó trong cùng tiến
       ///              trình, không ghi vào cột CSDL và không so với giá trị đọc từ CSDL, nên không thuộc
       ///              diện phải chuyển sang đồng hồ CSDL như các mốc của luồng khoá độc quyền.
       /// Created date: 27/09/2026
       /// </summary>

       // ✅ ĐÚNG — 2 dòng Description, phần lý do để ở MasterPlan §6b
       /// <summary>
       /// Description: Các bảng nguồn đang bị cô lập do mất Change Tracking, kèm mốc hẹn thử lại RIÊNG
       ///              của từng bảng. Giữ ở RAM, không lưu CSDL — xem MasterPlan §6b.
       /// Created date: 27/09/2026
       /// </summary>
       ```
     - ✅ **Ngoại lệ DUY NHẤT**: nhóm hàm ở mục **19.7** (duyệt cây dữ liệu / template / binding như `HasFieldBinding`, `IsRecordTemplateArray`) vẫn BẮT BUỘC viết dài kèm `Guard` và `Cross-pipeline Sync` — vì lệch ngữ nghĩa giữa chiều gửi và chiều nhận là lỗi **không thể phát hiện bằng biên dịch**. ⛔ Ngoài đúng nhóm đó, ⛔ không viện dẫn 19.7 để viết dài.
     - 📌 Quy tắc **19.13** (viết tự chứa, ⛔ không đẩy câu trả lời sang chỗ khác) chỉ áp cho tài liệu trong `DocBusinessThienAn/` và tệp prompt — ⛔ **không** áp cho comment trong mã nguồn. Ở đây dẫn chiếu sang MasterPlan là **đúng**, không phải vi phạm.
   * **Bắt Buộc XML Summary Trên Interface & Mọi Phương Thức Interface (Interface Methods)**:
     - Mọi Interface (`public interface I...`) và TẤT CẢ các phương thức định nghĩa bên trong interface BẮT BUỘC phải có khối XML summary comment chuẩn 2 dòng (`/// <summary>\n/// Description: ...\n/// Created date: ...\n/// </summary>`).
     - Tuyệt đối CẤM để phương thức trong interface trơ trọi không có XML summary, gây khó khăn cho IntelliSense, phân tích kiến trúc và gây thiếu nhất quán giữa interface với class thực thi.
   * **Cấm lặp khối XML summary**: TUYỆT ĐỐI KHÔNG tự ý chèn chồng hoặc nhân bản các khối `/// <summary>` rườm rà trên cùng một class/hàm/property. Mỗi đối tượng code CHỈ ĐƯỢC CÓ DUY NHẤT 1 khối `/// <summary>`. Khi cập nhật nội dung comment, BẮT BUỘC sửa trực tiếp vào khối comment cũ thay vì thêm khối `/// <summary>` thứ 2.
   * **API Controller Action Summary**: Trên mỗi phương thức Action trong Controller, comment XML Doc `/// <summary>` BẮT BUỘC mô tả rõ ràng, tự nhiên ý nghĩa và chức năng thực tế của hàm (VD: `/// <summary>\n/// Lấy danh sách cảnh báo & lỗi (phân trang)\n/// </summary>`). Tuyệt đối KHÔNG chèn mã prefix/số thứ tự rườm rà (như L1., E2., DS3...). Thẻ `[DisplayName("...")]` giữ nguyên tên hiển thị chuẩn.
   * **Chỉ Viết XML Summary Ở Đầu Hàm & Class — CẤM Tự Tiện Comment Bên Trong Thân Hàm**:
     - Chỉ viết comment `/// <summary>` ở đầu Class, Interface, Method, Property để mô tả mục đích/nghiệp vụ.
     - TUYỆT ĐỐI KHÔNG tự tiện viết các dòng comment giải thích dông dài, rườm rà bên trong thân hàm (`// ...`). Code phải tự rõ nghĩa (self-documenting).
     - CHỈ thêm comment giải thích bên trong thân hàm khi người dùng yêu cầu trực tiếp.
   * **Độc Lập Phân Hệ Trong Comment & XML Doc (Module Isolation in Comments)**:
     - **Ngữ cảnh khép kín theo từng phân hệ**: Khi viết XML doc, summary hay bất kỳ comment giải thích nào trong code thuộc một phân hệ cụ thể (VD: `VideoWall`, `ShareData`, `TMS`...), nội dung comment CHỈ ĐƯỢC PHÉP mô tả các khái niệm, quy trình nghiệp vụ, đối tượng thuộc nội bộ chính phân hệ đó hoặc các lớp trừu tượng chung dùng chung ở tầng `Shared` (`EntityTenant`, `BaseRepository`, `MessBus`...).
     - **TUYỆT ĐỐI CẤM dẫn chiếu, so sánh chéo sang phân hệ khác**: Tuyệt đối không nhắc tên class, interface, service cụ thể hay so sánh cách xử lý/nguyên tắc với một phân hệ độc lập khác (Ví dụ: CẤM viết trong `VideoWall` câu kiểu *"— cùng nguyên tắc với ShareDataActivityLogger của phân hệ Chia sẻ dữ liệu"*, hoặc trong `ShareData` lại dẫn chiếu sang cách làm của `VideoWall`/`TMS`). Phân hệ nào độc lập phân hệ đó, việc dẫn chiếu chéo gây rò rỉ ngữ cảnh (leaky context), gây hiểu nhầm về sự phụ thuộc giữa các module và để lại vết copy-paste thiếu chuẩn mực.

8. **Quy định Docker SQL Server trên Mac**: Máy tính chạy môi trường macOS (đặc biệt chip Apple Silicon M1/M2/M3/M4) **BẮT BUỘC** dùng Docker image `mcr.microsoft.com/azure-sql-edge:latest`. TUYỆT ĐỐI KHÔNG dùng `mcr.microsoft.com/mssql/server:2022-latest` vì bản x86_64 sẽ bị crash tràn bộ nhớ QEMU (`Invalid mapping of address`).
9. **Quy định Primary Constructor ([IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290)) (đã chốt 05/09/2026)**: Chỉ áp dụng C# Primary Constructor khi **VIẾT CLASS MỚI** (ví dụ: `public class MyService(ILogger<MyService> Logger, IConfiguration Configuration) : IMyService`). Đối với **CLASS CŨ ĐÃ TỒN TẠI** đang dùng constructor tường minh kèm field private thủ công → KHÔNG sửa, KHÔNG refactor sang primary constructor, giữ nguyên style cũ để tránh diff không cần thiết.
10. **Quy định Structured Logging ([CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873))**: LUÔN dùng structured logging message template (VD: `_logger.LogInformation("Processing {Id} for {Partner}", id, partner)`) thay vì string interpolation (VD: `_logger.LogInformation($"Processing {id} for {partner}")`) hoặc tính toán trước các biểu thức tốn kém (`string.Join(...)`, `.Count()`, LINQ...) ngay trong tham số log. Kiểm tra `_logger.IsEnabled(...)` trước khi chuẩn bị dữ liệu log tốn kém để tránh cấp phát bộ nhớ và tốn CPU không cần thiết khi logging đang tắt.
11. **Quy định Tự Động Xóa Using Thừa ([IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005))**: Sau mỗi lần tạo mới hoặc chỉnh sửa file code C#, **BẮT BUỘC** phải rà soát và xóa bỏ tất cả các chỉ thị `using ...;` không còn sử dụng hoặc bị trùng lặp với `GlobalUsings.cs` (CS0105 / IDE0005) để giữ mã nguồn gọn gàng và không sinh cảnh báo build. Tuyệt đối không tự ý thêm các using đã có trong `GlobalUsings.cs` của project (xem chi tiết quy định tại Mục 4, điều 12).
12. **Quy định vòng đời DI cho `IVwISAPIDeviceService` (đã chốt 05/09/2026)**: Đăng ký theo vòng đời **`IScoped`** (`VwISAPIDeviceService : IVwISAPIDeviceService, IScoped`) — đây là chỉ đạo trực tiếp của chủ dự án, không phải Singleton.
13. **Quy định Đa Ngôn Ngữ & Dịch Thuật Module (`BaseMsg`)**:
    * Tất cả các Module có sử dụng dịch thuật BẮT BUỘC tạo file `Core/Exceptions/BaseMsg.cs` kế thừa `BaseLocaleManager` (từ `Shared.DTO.Constants.Localization`).
    * Trong `BaseMsg`, tạo các class đại diện cho từng Chức năng/Entity (VD: `EshPartner`, `EshDataSource`...). Trong mỗi class chức năng, chia thành các class con chứa hằng số dịch thuật: `Validation`, `Message`, `Exception`, `Entity` (group action).
    * **Vị trí thư mục Resources**: Thư mục `Resources` nằm ngang hàng với `Controllers`, `Core`, `Extensions`, `Infrastructure` trong root project của Module (VD: `Modules.ShareData/Resources/vi-VN.json`). KHÔNG đặt bên trong thư mục `Controllers`. Dịch thuật được cập nhật đồng bộ vào `src/TAC_WebAPI/Resources/` để hệ thống load đầy đủ.
14. **Quy định Quét SqlSugar CodeFirst (`inherit: false`)**:
    * Khi quét entity để tạo bảng qua CodeFirst (`InitTables`), BẮT BUỘC dùng `t.IsDefined(typeof(SugarTable), inherit: false)` để DTO kế thừa Entity (`AddXxxInput : EntityBase`, `PageXxxOutput : EntityBase`) không bị nhận nhầm và tự tạo bảng.
15. **Quy định Tiền Tố Method Khởi Tạo: Dùng `Init` Thay Vì `Initialize` (đã chốt 23/09/2026)**: Khi đặt tên method private/internal thực hiện công việc khởi tạo một thành phần, đăng ký subscription, thiết lập kết nối ban đầu, v.v., BẮT BUỘC dùng tiền tố ngắn gọn `Init` thay vì `Initialize`. Ví dụ: `InitTriggerSubscription`, `InitNatsConnection`, `InitChangeTracking`. KHÔNG dùng `InitializeTriggerSubscription`, `InitializeNatsConnection`, `InitializeChangeTracking`... Lý do: ngắn gọn hơn, tránh verbose thừa, thống nhất convention toàn dự án.
16. **Quy định Refactor & DRY Thực Dụng: Tránh "DRY Mù Quáng" (Pragmatic DRY vs. Blind/Premature DRY) (đã chốt 23/09/2026)**:
    * **CẤM dùng Flag Argument / Tham số tùy chọn để gộp các Public Method / API có ý định nghiệp vụ khác nhau**: Khi hai phương thức công khai biểu đạt hai luồng nghiệp vụ độc lập (VD: `ProcessScheduledSubscriptions` quét định kỳ theo lịch vs `ProcessTriggeredSubscriptions` phản ứng tức thời khi có dữ liệu mới), TUYỆT ĐỐI KHÔNG gộp thành một phương thức chung nhận flag argument (như `packetCode = null`). Việc gộp như vậy làm rò rỉ rẽ nhánh `if/else`, phá vỡ hợp đồng công khai (interface), làm bẩn call-site và che giấu ngữ cảnh nghiệp vụ khác biệt.
    * **Overload cùng tên cũng KHÔNG phải lối thoát (chốt 23/09/2026, hiệu chỉnh 25/09/2026)**: đừng nghĩ rằng tách thành hai overload cùng tên là né được lệnh cấm flag argument ở trên. Ví dụ minh hoạ rủi ro (không phải sự kiện lịch sử có thật — đã rà `git log -S` toàn bộ lịch sử repo `TA-ITS015-WEBAPI-V1.0` ngày 25/09/2026, xác nhận `ProcessScheduledSubscriptions`/tiền thân của nó chưa từng có overload dạng này; đoạn dưới đây chỉ là kịch bản cảnh báo giả định để giải thích TẠI SAO cấm): nếu tách thành cặp overload cùng tên `Xxx(CancellationToken)` và `Xxx(string, CancellationToken)`, lời gọi `(default)` **không biên dịch được** (CS0121 — `default` khớp cả hai kiểu), còn `(null)` thì **biên dịch ngon lành nhưng có thể chạy thành no-op im lặng** nếu bên trong có guard kiểu `IsNullOrWhiteSpace`, khiến người viết tưởng "quét tất" mà thực tế không làm gì. Hai luồng nghiệp vụ độc lập BẮT BUỘC có **tên khác nhau**, không phải chỉ khác chữ ký.
    * **Chỉ tách đơn vị kỹ thuật/nghiệp vụ mạch lạc (Cohesive Unit)**: Khi khử trùng lặp code, chỉ gom các đoạn logic kỹ thuật/hạ tầng lặp lại nguyên văn (VD: vòng đời claim lease OCC, execute, catch lỗi và release lease trong `ProcessSubscriptionUnderLease`) thành private helper method.
    * **Giữ độc lập các logic trông tương tự nhưng thuộc ngữ cảnh khác nhau**: Các khối lọc truy vấn CSDL (subquery, query builder), điều kiện nghiệp vụ riêng biệt (VD: `DebounceSec` chỉ thuộc về luồng sự kiện), hoặc log thông điệp ngữ cảnh riêng... dù có cấu trúc tương tự cũng KHÔNG ĐƯỢC ép gom chung (Avoid Premature Abstraction), tránh làm sai lệch SQL do ORM dịch ra và giữ cho code dễ đọc, dễ bảo trì độc lập.
17. **Quy định Thứ Tự Tham Số Phương Thức: Tham Số Nullable / Tùy Chọn Luôn Đặt Ở Cuối (đã chốt 26/09/2026)**:
    * **Các tham số bắt buộc, non-nullable** (như `db`, `sub`, `tag`, ID, context, dữ liệu nghiệp vụ chính...) **BẮT BUỘC** đặt ở ĐẦU danh sách tham số.
    * **Các tham số tùy chọn hoặc nullable** (`string?`, `DateTime?`, `int?`, đối tượng nullable...) **BẮT BUỘC** đặt ở CUỐI danh sách tham số (ngay trước `CancellationToken cancelToken` nếu có).
    * **Vị trí của `CancellationToken`**: Nếu phương thức là async có nhận token (`CancellationToken cancelToken` hoặc `cancelToken = default`), token luôn đứng ở vị trí cuối cùng của phương thức theo chuẩn thiết kế API của .NET. Toàn bộ các tham số nullable khác nằm ngay trước `cancelToken`.
    * **Lý do & Lợi ích**:
      - Phù hợp với ngôn ngữ C# và các quy ước chuẩn (call-site tự nhiên, dễ đọc, hỗ trợ optional arguments với giá trị mặc định `= null`).
      - Phân tách rõ ràng giữa "dữ liệu bắt buộc phải có để phương thức hoạt động" và "dữ liệu bổ trợ/tùy chọn", tránh trường hợp truyền nhầm `null` vào giữa các tham số quan trọng.

---

## 🧪 6. Quy Định Thiết Kế & Viết Test (Testing Rules)

Tất cả các bài kiểm thử tự động (Integration/Unit Tests) bắt buộc tuân theo cấu trúc gọn nhẹ và quy tắc viết test sau:

### 1. Cấu Trúc Thư Mục
Dự án test nằm trực tiếp trong thư mục `tests/` của repo gốc (không lồng subfolder project):
```
tests/
├── test.csproj                            ← Project file test (Target net10.0)
├── Host.cs                                ← Host.CreateDefaultBuilder() + Lamar (IAsyncLifetime, override DB, tắt Hangfire)
├── GlobalUsings.cs                        ← Chứa global using chung (Xunit, System.Net...) để tránh IDE0005
└── <TênModule>/
    ├── Host.<TênModule>.cs            ← Partial method cấu hình riêng cho module
    ├── GlobalUsings.<TênModule>.cs    ← Global using riêng cho module
    └── <TênModule>Tests.cs            ← File test của module (xem vòng đời fixture ở mục 2 và 4 bên dưới)
```

### 2. Triết Lý & Phương Pháp Viết Test
* **Vòng đời Fixture & Collection**: Khi có khởi tạo DB / Host nặng, BẮT BUỘC dùng `ICollectionFixture<Host>` và gắn `[Collection("api")]` trên class test (trong đó `Host` triển khai `IAsyncLifetime`, không phải `IDisposable`) để chia sẻ fixture duy nhất cho cả collection, tránh gọi constructor N lần gây đụng độ khi chạy test song song. Việc xóa/dọn dẹp dữ liệu test chỉ thực hiện duy nhất 1 lần ở tầng `Host.cs` (`ClearAllData()`) — xem thêm mục 4 bên dưới.
* **Tập trung vào Happy Path**: Chỉ tập trung viết test cho các luồng chính (**Happy Path** của Queries & Commands).
* **Business Workflows & Mock Integration First (No Pure/Trivial Unit Tests)**: TUYỆT ĐỐI KHÔNG làm các bài unit test thuần túy, vụn vặt (như đếm phần tử static list, assert danh mục enum/preset, test đơn lẻ getter/setter hay in-memory ViewModel helper không có I/O). TẬP TRUNG TOÀN BỘ VÀO: (1) Kiểm thử luồng nghiệp vụ thực tế (business workflows xuyên suốt từ Controller/Command/Handler xuống CSDL/Service), và (2) Các bài test có tương tác với Mock / MockServer (gửi nhận request/response HTTP thật qua mock, digest auth, kiểm tra payload thực tế, kịch bản lỗi khi chạm thiết bị hoặc dịch vụ bên ngoài). Áp dụng mẫu AAA (Arrange - Act - Assert).
* **Gọi trực tiếp qua Wolverine `IMessageBus` (Bypass Controller/HTTP)**:
  - **Lợi ích**: Giúp quá trình chạy test cực kỳ nhanh, bỏ qua lớp kiểm tra quyền JWT Authentication/Authorization phiền phức và **100% bắt được breakpoint** khi debug bằng VS Code (do cùng chạy trên 1 luồng xử lý chính).
  - **Cách gọi**: Inject `IMessageBus` từ `Host.Services` và gọi trực tiếp:
    - *Queries (Phân trang)*: `var result = await _bus.InvokeAsync<SqlSugarPagedList<OutputDTO>>(new InputDTO { ... });`
    - *Commands (Thêm/Sửa/Xóa)*: `await _bus.InvokeAsync(payload);`
* **Kiểm tra Validator Bắt Buộc Trực Tiếp Trong Test Command (Không Tách File Riêng)**: Khi viết test cho bất kỳ Command nào (Add, Update, Delete, BatchDelete, Ping, Probe, Sync, Setup, Reset, Workflow commands...) có validator FluentValidation tương ứng: BẮT BUỘC phải thực hiện kiểm thử validator trực tiếp trong luồng test của class test Controller/Command tương ứng (ví dụ: `var valResult = await new XxxValidator().ValidateAsync(input); Assert.True(valResult.IsValid, ...);`) trước khi gửi qua `_bus.InvokeAsync(payload)`. TUYỆT ĐỐI KHÔNG TÁCH CLASS/FILE TEST VALIDATOR RIÊNG BIỆT (như `*ValidatorTests.cs`). Đối với các case lỗi (negative validation), kiểm tra `Assert.False(invalidResult.IsValid)` trực tiếp trong bài test tương ứng.
* **Kiểm tra trạng thái DB trực tiếp**: Đối với các Command (Add/Update/Delete), sau khi gọi `_bus.InvokeAsync`, hãy resolve `ISqlSugarClient` từ scope của Host để query và so sánh trực tiếp dữ liệu trong DB (ví dụ: `Assert.NotNull(added)`, `Assert.Null(deleted)`).
* **Cấu trúc SqlSugarPagedList**: Đối tượng phân trang trả về là `SqlSugarPagedList<T>`, truy xuất dữ liệu danh sách qua thuộc tính **`.Records`** (kiểu `IEnumerable<T>`), không phải `.List` hay `.Rows`.
* **Dọn Dẹp Dữ Liệu Tập Trung Duy Nhất Ở Tầng Host**: Toàn bộ việc dọn dẹp / xóa dữ liệu test chỉ được thực hiện tập trung duy nhất ở tầng **Host** (thông qua `ClearAllData()` khi khởi tạo `ICollectionFixture<Host>`). TUYỆT ĐỐI KHÔNG viết logic `Dispose()` để `DELETE` hay `TRUNCATE` dữ liệu trong từng `TestClass`.
* **Cô Lập Dữ Liệu Test Bằng GUID / Unique ID**: Mọi bài test BẮT BUỘC tự cô lập dữ liệu bằng cách sinh mã định danh duy nhất (GUID / `Guid.NewGuid():N` / `TestPrefix` ngẫu nhiên) cho các bản ghi tạo mới trong bước Arrange, đảm bảo các bài test chạy song song hoặc tuần tự hoàn toàn độc lập và không bao giờ xung đột dữ liệu với nhau. Bảng dữ liệu nghiệp vụ ngoài (READ-ONLY) tuyệt đối không chạy lệnh xóa/sửa.
* **No Separate Utils Test Folders / Service-Level Testing Focus**: TUYỆT ĐỐI KHÔNG tạo thư mục test `Utils` / `Util` riêng biệt hay viết unit test cô lập cho các class tiện ích (Utils/Helpers). Chỉ cần tập trung viết test ở tầng **Service / Handler / Controller** chính. Nếu logic nghiệp vụ có liên quan đến Util/Helper thì các bài test tại tầng Service bao phủ và kiểm thử các tiện ích đó trong luồng thực thi thực tế là đủ.
* **Cấm Gọi InitTables<T>() / Tạo Hàm EnsureTablesCreated() Trong Host/Test — Chỉ Bật Cờ CodeFirst Trong Setting (LỖI NÀY BỊ HOÀI)**: 
  - TUYỆT ĐỐI KHÔNG tự tiện viết các lệnh gọi `db.CodeFirst.InitTables<T>()` thủ công hay bịa ra các hàm custom như `EnsureTablesCreated()` trong `Host.cs`, `Host.<Module>.cs` hay các file test để sinh/đồng bộ bảng.
  - **Nguyên nhân khi test báo thiếu bảng/cột (`Invalid object name '...'` hoặc `Invalid column name '...'`)**: Cờ CodeFirst trong cấu hình test đang bị tắt (`false`).
  - **Cách xử lý duy nhất**: Mở `tests/appsettings.Test.json` và bật cờ trong `DbConnection:ConnectionConfigs` (hoặc cấu hình test environment):
    ```json
    "DbSettings": {
      "EnableInitDb": true
    },
    "TableSettings": {
      "EnableInitTable": true,
      "EnableIncreTable": false
    }
    ```
  - Khi bật các cờ này (`EnableInitTable: true, EnableIncreTable: false`), hạ tầng `SharedInfrastructure` & SqlSugar khi khởi tạo Host sẽ tự động quét toàn bộ Entity từ code gốc và tự động tạo bảng / bổ sung cột an toàn và chuẩn hóa. Tuyệt đối không can thiệp code C# trong Host/Test.
  - ⚠️ **LƯU Ý CỐT LÕI VỀ `EnableIncreTable` (BẪY TỬ HUYỆT)**: `EnableIncreTable` **BẮT BUỘC PHẢI LÀ `false`** khi muốn CodeFirst tạo bảng mới!
    - **Bản chất hạ tầng (`SqlSugarSetup.cs` thuộc `Shared.Infrastructure.dll`)**:
      Hạ tầng lọc Type quét bảng theo code sau:
      ```csharp
      List<Type> source = (from element in App.EffectiveTypes
          where !element.IsInterface && !element.IsAbstract && element.IsClass && element.IsDefined(typeof(SugarTable), inherit: false)
          where !element.GetCustomAttributes<IgnoreTableAttribute>().Any()
          select element)
          .WhereIF(P_1.TableSettings.EnableIncreTable, (Type type2) => type2.IsDefined(typeof(IncreTableAttribute), inherit: false))
          .ToList();
      ```
      Nếu bật `"EnableIncreTable": true`, SqlSugar sẽ lọc và **CHỈ quét những Entity có gắn attribute `[IncreTableAttribute]`**. Trong toàn bộ repo hiện tại **không có entity nào gắn `[IncreTable]`**, dẫn đến danh sách bảng cần khởi tạo = 0 ⇒ **hoàn toàn không có bảng nào được tạo** dù `EnableInitTable: true`!
    - Do đó, để tạo bảng CodeFirst, chỉ cần:
      `"DbSettings": { "EnableInitDb": true }` và `"TableSettings": { "EnableInitTable": true, "EnableIncreTable": false }`.
* **Tự Động Chạy Lại Test & Bổ Sung Test Case Mới**: Bất cứ khi nào tạo mới hoặc chỉnh sửa code (C#, XAML, ViewModel, Service, Handler, Controller, API...), thêm mới UI, hoặc sửa đổi logic nghiệp vụ/giao diện: AI **BẮT BUỘC** (1) chạy lại toàn bộ bài test liên quan (`dotnet test ...`) để đảm bảo 100% pass, không hồi quy/gãy build; (2) viết bổ sung test case mới nếu tính năng/logic mới chưa có test bao phủ (chuẩn AAA, mock I/O HTTP/thiết bị, đặt tên file/thư mục mirror 1-1). Nhiệm vụ chưa được coi là hoàn thành nếu thiếu 1 trong 2 bước trên.
  - **Dồn test về cuối khi đang trao đổi dồn dập**: Nếu đang trong chuỗi hỏi-đáp/sửa nhanh liên tiếp và test suite chạy chậm (VD ~60s+), KHÔNG chạy lại test sau MỖI lần sửa nhỏ — dồn thay đổi liên quan lại, chỉ chạy 1 lần ở cuối trước khi báo hoàn tất. Vẫn chạy ngay nếu người dùng hỏi trực tiếp kết quả test, thay đổi đủ rủi ro cần xác nhận ngay, hoặc rõ ràng không còn quyết định nào khác đang chờ.

### 3. Quy Tắc Đặt Tên & Định Dạng
* **File test**: `<TênModule>Tests.cs` (không dùng hậu tố `IntegrationTests.cs`).
* **Class test**: `<TênModule>Tests`. Hậu tố `Test`/`Tests` CHỈ dành cho class chứa `[Fact]`/`[Theory]` và cho test method; TUYỆT ĐỐI KHÔNG dùng `Test` làm tiền tố (như `TestStringLocalizer`).
* **Tên class lớp giả lập — thuật ngữ quốc tế là *test double* (chốt 30/09/2026)**: đặt tên theo **LOẠI của double làm hậu tố**, ⛔ TUYỆT ĐỐI KHÔNG dùng tiền tố `Mock`/`Fake`/`Stub`/`Test`, và ⛔ không nhét loại vào giữa tên (`VwISAPIMockServerHikvision` là SAI):

    | Hậu tố | Nghĩa | Lớp đang có trong repo (soát 02/10/2026) |
    | --- | --- | --- |
    | `Mock` | Có kịch bản, có hành vi, bật/tắt được tình huống | ✅ `VwISAPIServerHikvisionMock`, `ShareDataPartnerServerMock` |
    | `Stub` | Trả giá trị cố định, ⛔ không có logic | ⚠️ **Chưa có lớp nào.** 📌 Trước 02/10/2026 ô này ghi `HostEnvironmentStub` — tên đó **⛔ không tồn tại** ở bất kỳ đâu trong repo, chỉ có trong chính file quy tắc này |
    | `Fake` | Cài đặt thật nhưng đơn giản, chạy trong bộ nhớ | ⚠️ **Chưa có lớp nào.** |
    | `Spy` | Chỉ ghi lại thứ nhận được để bài test soi | ✅ `PublisherSpy` |

    🔴 **Cột thứ ba là số liệu TẠM (quy tắc 14) — soát lại bằng lệnh, ⛔ đừng tin chữ trong bảng:**

    ```bash
    grep -rEn "class \w+(Fake|Stub|Spy|Mock)\b" tests/
    ```

    ⛔ Ô ghi *"chưa có lớp nào"* **KHÔNG** làm hậu tố đó mất hiệu lực — quy ước đặt tên vẫn áp dụng đầy đủ cho lớp mới viết sau này. Nó chỉ nói rằng hiện chưa có ví dụ sẵn để đối chiếu.

    📌 Thư mục chứa lớp giả lập đặt tên **`Mocks/`** (`tests/BE/ITS/VideoWall/Mocks/`, `tests/BE/ITS/ShareData/Mocks/`) — từ quen dùng của nhóm, đọc là hiểu. Thư mục chỉ nói "chỗ chứa đồ giả lập"; việc phân biệt `Mock`/`Stub`/`Fake`/`Spy` do **hậu tố tên class** đảm nhiệm. ⛔ Không đặt tên thư mục theo một loại cụ thể (`Stubs/`, `Fakes/`) vì trong đó có đủ cả 4 loại.
    📌 **Lỗi thật đã mắc**: `MockHttpClientFactoryTest` mang cả tiền tố `Mock` lẫn hậu tố `Test`, còn `VwISAPIMockServerHikvision` nhét `Mock` vào giữa — đọc tên không biết nó là loại double nào.
* **Comment XML Summary Bắt Buộc Trên Mọi Phương Thức & Class**: Mọi Class, Constructor, Helper Method và phương thức kiểm thử (`[Fact]` / `[Theory]`) BẮT BUỘC có comment XML `/// <summary>` theo định dạng chuẩn 2 dòng (KHÔNG dùng `Author:` — quyết định 05/09/2026, không hồi tố test đã có sẵn `Author: Đạt`):
  ```csharp
  /// <summary>
  /// Description: [Mô tả chi tiết chức năng / Helper / Test case]
  /// Created date: DD/MM/YYYY
  /// </summary>
  ```
  *(BỎ HẲN và KHÔNG DÙNG field `Updated date:`)*.
* **Namespace**: `Tests` (gốc) và `Tests.<TênPhânHệ>.<ĐườngDẫnCon>` (ví dụ: `Tests.VideoWall.Mocks`). ⛔ Không chèn tên thư mục nhóm (`ITS`) vào namespace — xem mục 15.
* **Tên phương thức test**: Sử dụng dấu gạch dưới **`_`** để phân tách các phần trong tên phương thức theo định dạng `Feature_Scenario_ExpectedResult` hoặc `Feature_Scenario_ExpectedResult_Test` (ví dụ: `CronJob_SavedQuery_SqlGeneration_Test`, `PartnerQuery_GetById_ReturnsSuccess_Test`).
* **Thứ tự**: Sắp xếp các Happy Case của Queries lên trước, sau đó đến các Happy Case của Commands (ví dụ: `QueryPageReturnsSuccessTest`, `CommandAddReturnsSuccessTest`).
* **Seed & Helper Types — Đặt Cuối Class, Không Truyền Positional Params Dài (Internal Seed Record Pattern)**:
  - Mọi `record`/`class` hỗ trợ bên trong test class (Seed DTO, helper type, kết quả tạm) BẮT BUỘC đặt trong `#region Seed & Helper Types` ở **cuối cùng** của class, TUYỆT ĐỐI KHÔNG rải rác xen kẽ giữa các `[Fact]` hoặc helper method.
  - Khi một hàm seed có **từ 3 tham số tùy chọn trở lên**, BẮT BUỘC đóng gói thành một `private sealed record` nội bộ thay vì truyền positional params. Caller dùng **object initializer** với `required` property để đảm bảo compile-time safety. Ví dụ:
    ```csharp
    // ✅ Đúng — Seed record đặt ở cuối class
    #region Seed & Helper Types
    private sealed record OutboundSubSeed
    {
        public required string PartnerCode                    { get; init; }
        public required string SubCode                        { get; init; }
        public required string DatatypeId                     { get; init; }
        public object?                        MappingShape    { get; init; }
        public Action<ShareDataSubscription>? ConfigureSub    { get; init; }
        public Action<ShareDataPartner>?      ConfigurePartner { get; init; }
    }
    #endregion

    // ✅ Đúng — Call site rõ nghĩa, không cần nhớ thứ tự tham số
    var (partner, sub) = await SeedOutboundSubscription(db, new OutboundSubSeed
    {
        PartnerCode = $"P_HTTP_{unique}",
        SubCode     = $"SUB_HTTP_{unique}",
        DatatypeId  = "101",
        ConfigurePartner = p => { p.Port = 18090; p.EndPointApiUrl = "/api/..."; }
    });

    // ❌ Sai — Positional params dài, dễ nhầm thứ tự
    var (partner, sub) = await SeedOutboundSubscription(db, $"P_HTTP_{u}", $"S_HTTP_{u}", "101", null, p => { ... }, null);
    ```


### 4. .NET & Solution Troubleshooting Protocol

**Khi gặp lỗi thiếu tham chiếu, không nhận diện được Test trong IDE, hoặc lỗi khi debug:**


1. **Kiểm tra đăng ký trong Solution (`.sln`):**
   - Mọi dự án mới tạo (đặc biệt là `*.Tests.csproj`) BẮT BUỘC phải được thêm vào file `.sln` chính của workspace.
   - Nếu IDE/VS Code không quét được test hoặc báo thiếu reference, hãy kiểm tra và chạy:
     `dotnet sln <path-to-sln> add <path-to-csproj>`
   - Lệnh tự động thêm tất cả project: `dotnet sln <sln-file> add $(find . -name "*.csproj")`
2. **Quy tắc thực thi lệnh .NET:**
   - KHÔNG KHUYÊN DÙNG chạy trực tiếp file đơn lẻ dạng `dotnet File.cs` cho project xUnit/C#.
   - LUÔN LUÔN dùng `dotnet test <csproj_or_sln>` hoặc `dotnet build` để nạp đủ các thư viện và dependency.
   - ⛔ **CẤM TUYỆT ĐỐI dùng cờ `--no-build` khi chạy `dotnet test` (P0)**: Khi thực thi kiểm thử qua `dotnet test`, TUYỆT ĐỐI KHÔNG thêm cờ `--no-build` (ví dụ: `dotnet test tests/BE/test.csproj --filter "..." --no-build` là SAI). Việc bỏ qua bước build dẫn đến nguy cơ rất cao là test sẽ chạy trên binary/assembly cũ (stale cache) trong `bin/Debug/`, hoàn toàn bỏ qua các sửa đổi mã nguồn mới vừa lưu trên đĩa, dẫn đến sai lệch nghiêm trọng kết quả kiểm thử (test giả mạo pass/fail, phantom test results). Mặc định `dotnet test` luôn tự động build incremental chỉ cho các project có thay đổi rất nhanh, đảm bảo 100% test chạy trên code thực tế.
3. **Kiểm tra Connection String trước khi `dotnet test`:**
   > Xem mục 11 "Strict Local Database Rule for Testing" bên dưới.

---

## 🧹 7. Clean Code — Bổ Sung (áp dụng ngay cả khi `universal-rules.md` được thay bằng bản AG-Kit mới)

- **No Hardcoded Magic Strings**: Không viết literal chuỗi cứng (mã trạng thái, tên state...) trực tiếp trong query/logic điều kiện nghiệp vụ. LUÔN định nghĩa và dùng Enum hoặc Constant có kiểu rõ ràng (VD: `ShareDataEnum.IncidentState`).
- **Formatting (Single-Statement `if` Without Braces)**: Đối với câu lệnh `if` chỉ chứa 1 dòng lệnh thực thi (ví dụ: các lệnh ghi log ngắn gọn `Logger.Log...`, lệnh `return`, v.v.), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi, ĐỒNG THỜI BỎ cặp dấu ngoặc nhọn `{}`. TUYỆT ĐỐI KHÔNG viết inline trên cùng 1 dòng (`if (condition) return;`) và TUYỆT ĐỐI KHÔNG tự ý thêm `{}` vào các câu lệnh đơn.
- **CẤM dòng trống xen giữa các câu lệnh liên tiếp (chốt 30/09/2026)**: TUYỆT ĐỐI KHÔNG chèn dòng trống sau **mỗi** dòng code. Dòng trống chỉ dùng để tách các khối logic có ý nghĩa (giữa `Arrange` / `Act` / `Assert`, giữa hai phương thức, giữa nhóm khai báo và phần thân).
  - **Dấu hiệu nhận biết tệp đã hỏng**: mọi dòng không rỗng đều được theo sau bởi đúng một dòng rỗng, và tỷ lệ dòng rỗng trên tổng số dòng vượt 45%.
  - **Lỗi thật đã mắc**: 16 tệp test VideoWall từng bị nhân đôi toàn bộ dòng trống — `VwISAPIDeviceServiceTests.cs` phình lên 5.249 dòng thay vì 2.724. Đã dọn ngày 30/09/2026 bằng `.agents/scripts/Fix-DoubledBlankLines.ps1`.
  - 🔴 **Khi dọn, ⛔ TUYỆT ĐỐI KHÔNG thay thế `\r\n\r\n` → `\r\n` toàn cục**: tệp thường có xen vùng code lành, thay mù sẽ xoá luôn dòng trống có chủ đích ở đó. Phải nhận biết theo vùng và kiểm bất biến: **tập hợp các dòng không rỗng phải y nguyên trước/sau khi dọn**.
- **Object Initializer Formatting**: Object initializer nhiều thuộc tính (VD: `new TmsEquipment { ID = eqId, Code = "...", ... }`) BẮT BUỘC ngắt dòng, mỗi thuộc tính 1 dòng thụt lề. TUYỆT ĐỐI KHÔNG viết inline nhiều thuộc tính trên 1 dòng ngang.
- **Inline Temporary Entity Khi Insert (No Redundant Temporary Variable)**: Khi khởi tạo một entity mới chỉ để insert vào CSDL qua `db.Insertable(...)` mà bản ghi đó KHÔNG được dùng lại ở các câu lệnh sau hoặc KHÔNG được `return` ra ngoài, BẮT BUỘC khởi tạo inline trực tiếp trong câu lệnh insert (VD: `await db.Insertable(new ShareDataLastSend { ... }).ExecuteCommandAsync(cancelToken);`), TUYỆT ĐỐI KHÔNG khai báo biến tạm thừa (`var newLastSend = new ...; await db.Insertable(newLastSend)...`). CHỈ khai báo biến khi cần tái sử dụng biến đó hoặc trả về sau khi insert.
- **Ưu Tiên Biến Cục Bộ Thay Vì Field / Property (Prefer Local Variables Over Class Fields/Properties)**: Bất kỳ biến nào chỉ dùng làm dữ liệu tạm thời, phục vụ tính toán trung gian hoặc chỉ dùng trong phạm vi 1 phương thức/truyền qua tham số: BẮT BUỘC dùng biến cục bộ (`var local = ...`). TUYỆT ĐỐI KHÔNG lưu thành field (`private ...`) hoặc property của class nếu không thực sự cần lưu giữ trạng thái sống xuyên suốt vòng đời đối tượng (Stateful Lifecycle).
- **Multi-Condition LINQ & SqlSugar Formatting (Chaining `.Where` — Triệt Tiêu `&&` Nhồi Nhét)**:
  - Khi xây dựng truy vấn ORM (SqlSugar/EF Core), BẮT BUỘC ưu tiên tách các điều kiện logic độc lập thành từng dòng `.Where(...)` nối tiếp nhau (Chaining Where) thay vì dồn tất cả vào một biểu thức Lambda duy nhất với hàng loạt toán tử `&&`.
  - ORM sẽ tự động dịch các lời gọi `.Where(...)` liên tiếp thành các mệnh đề `AND` tương đương 100% trong câu lệnh SQL sinh ra (áp dụng cho cả query chính lẫn `SqlFunc.Subqueryable<T>`).
  - *Mỗi dòng `.Where()` chịu trách nhiệm duy nhất 1 tiêu chí*:
    * Định danh bản ghi (`ID == ...`, `Code == ...`)
    * Trạng thái xóa mềm / kích hoạt (`IsDelete == null`, `Status == ...`, `State == ...`)
    * Khóa tranh chấp đồng thời / OCC (`ProcessingUntil == null || ProcessingUntil <= now`)
    * Lịch trình hoặc thời gian quét (`NextTimeRun == null || NextTimeRun <= now`)
  - *Lợi ích*:
    * Triệt tiêu hoàn toàn các toán tử `&&` rườm rà, mã nguồn trở nên phẳng (flattened), trong sáng và dễ đọc.
    * Các biểu thức có chứa toán tử `||` (như kiểm tra null hoặc mốc thời gian `<= now`) sẽ nằm trọn vẹn và cô lập trong chính dòng `.Where()` đó, không bị kẹp méo mó giữa các dấu `&&`, loại trừ triệt để nguy cơ nhầm lẫn thứ tự ưu tiên toán tử logic (`&&` có độ ưu tiên cao hơn `||`).
    * Dễ dàng bật/tắt, thêm bớt điều kiện hoặc chuyển đổi sang `.WhereIF(...)` mà không phải cắt ghép chuỗi ngoặc nhọn phức tạp.
- **Truy Vấn Phân Tầng: Ưu Tiên Khớp Chính Xác Trước $\rightarrow$ Dự Phòng Sau (Two-Tier Query: Exact Match First, Fallback Later)**:
  - Khi cần tìm kiếm thực thể có thể khớp theo nhiều tiêu chí (vừa khớp chính xác theo ID/Code, vừa hỗ trợ tìm kiếm mờ / tiền tố / số thứ tự OrderNo dự phòng): TUYỆT ĐỐI TRÁNH nhồi nhét tất cả vào một câu truy vấn duy nhất bằng toán tử ba ngôi `? :` lồng ghép hoặc mẹo `OrderBy(p => ... ? 0 : 1)` để ép thứ tự kết quả.
  - BẮT BUỘC tách thành 2 tầng xử lý rõ ràng:
    * **Tầng 1 (Exact Match)**: Tìm kiếm khớp chính xác bằng điều kiện đơn giản trên các cột có Index (VD: `Code == value || ID == value`). Nếu tìm thấy, `return` kết quả ngay lập tức (thường chiếm 90-99% các tình huống chạy thực tế).
    * **Tầng 2 (Fallback Match)**: Chỉ khi tầng 1 không tìm thấy kết quả và có dữ liệu phân giải dự phòng (ví dụ `targetNumber.HasValue`), mới chạy câu truy vấn phụ để quét theo `OrderNo` hoặc tiền tố chuỗi `StartsWith(...)`.
  - *Lợi ích*:
    * **Hiệu năng CSDL vượt trội**: Đa số trường hợp truy vấn sẽ tận dụng Index trực tiếp và kết thúc ngay, CSDL không phải tốn tài nguyên chạy các phép quét chuỗi (`StartsWith`) hoặc tạo bảng tạm sắp xếp trong RAM (`OrderBy`).
    * **Code trong sáng, không lặp lại**: Loại bỏ việc copy-paste điều kiện lặp lại nhiều lần trong toán tử 3 ngôi.
- **Biểu Thức Điều Kiện & Tên Biến Luôn Ở Thể Khẳng Định (Affirmative Logic & Positive Boolean Naming)**:
  - **Tên biến boolean**: BẮT BUỘC đặt tên mô tả trạng thái khẳng định (positive), thể hiện rõ ngữ nghĩa tích cực hoặc luồng thực thi đang diễn ra (VD: `isScheduled`, `isEnabled`, `hasAccess`, `isValid`, `isRecurring`). TUYỆT ĐỐI KHÔNG đặt tên biến mang sẵn ý nghĩa phủ định (VD: `isNotScheduled`, `isDisable`, `isNotTriggered`, `noCache`).
  - **Biểu thức rẽ nhánh & WhereIF / SetColumnsIF**: BẮT BUỘC viết điều kiện ở thể khẳng định, TUYỆT ĐỐI TRÁNH dùng toán tử đảo ngược/phủ định `!` trước biến boolean trong `WhereIF(!isX, ...)` hoặc `if (!isX)` khi điều kiện bên trong đang lọc cho một trạng thái nghiệp vụ cụ thể.
    * *Ví dụ sai*: Khai báo `var isTriggered = packetCode != null;` rồi lọc `.WhereIF(!isTriggered, s => s.NextTimeRun == null || s.NextTimeRun <= now)`.
    * *Ví dụ đúng*: Khai báo trực tiếp thể khẳng định của luồng cần lọc `var isScheduled = packetCode == null;` rồi lọc `.WhereIF(isScheduled, s => s.NextTimeRun == null || s.NextTimeRun <= now)`.
  - *Lý do*: Viết phủ định `!isTriggered` để biểu thị cho luồng quét định kỳ bắt người đọc phải tư duy đảo ngược ("không phải trigger nghĩa là quét định kỳ"), gây quá tải nhận thức (cognitive strain), làm tối nghĩa mã nguồn và dễ dẫn đến sai sót logic khi thêm/sửa điều kiện.
- **Async Method Naming (Áp Dụng Cho Code MỚI)**: Khi viết phương thức bất đồng bộ MỚI (public service, handler, controller, hay private helper, test seed method...), TUYỆT ĐỐI KHÔNG thêm hậu tố `Async` vào tên phương thức (VD: `GetScope`, `ProcessScheduledSubscriptions`, `SeedWall` — không phải `GetScopeAsync`, `ProcessScheduledSubscriptionsAsync`, `SeedWallAsync`) vì kiểu trả về (`Task`/`Task<T>`) đã thể hiện rõ tính bất đồng bộ. Đối với code cũ đã viết trước đó của người khác hoặc API của thư viện bên ngoài: **CỨ KỆ, GIỮ NGUYÊN**, tuyệt đối không tự ý refactor hàng loạt gây diff rác hoặc lỗi tương thích.
- **Dependency Injection Naming & Casing**:
  - Với constructor viết tường minh (không phải primary constructor kiểu property): LUÔN đặt tên dependency injected bằng camelCase (VD: `IFileExportService fileExportService`), gán vào private field `_fileExportService = fileExportService;`.
  - Với Primary Constructor khi viết class mới (khi dependency đóng vai trò public read-only property): BẮT BUỘC viết hoa chữ cái đầu (PascalCase) (VD: `public class MyService(ILogger<MyService> Logger, IOutboundService OutboundService) : IMyService`).


- **Using Directives Thay Vì Inline Namespaces**: BẮT BUỘC dùng `using` directive ở đầu file (VD: `using Modules.ShareData.Core.Entities;`) để gọi tên class ngắn gọn (VD: `EshPartner`) thay vì gõ namespace dài inline trong code (VD: `Core.Entities.EshPartner`).
- **DTO vs Anonymous Objects**: Dữ liệu CÓ xử lý logic nội bộ → tạo DTO. Dữ liệu CHỈ map để gửi đi (bên khác xử lý) → dùng Anonymous Object (hoặc Dictionary).
- **Null Reference (CS8601)**: Luôn gán giá trị dự phòng (`?? string.Empty`) khi gán `string?` cho `string` để dập cảnh báo CS8601.
- **Cấu hình ASP.NET Core**: Ưu tiên `config.GetConnectionString("Default")` thay vì truy vấn key phân cấp thô (`config["DbConnection:ConnectionConfigs:0:ConnectionString"]`).
- **C# / .NET CA2263**: LUÔN ưu tiên `Enum.IsDefined<TEnum>(value)` dạng generic (hoặc `Enum.IsDefined(enumValue)` từ .NET 7+) thay vì bản non-generic `Enum.IsDefined(typeof(TEnum), value)` để tránh boxing và overhead reflection không cần thiết.
- **Quy định phạm vi truy cập & Thứ tự thành viên Class (Minimal Visibility & Class Member Layout)**:
  - **Phạm vi truy cập tối thiểu (Hàm nội bộ BẮT BUỘC dùng `private`)**: Bất kỳ hàm/phương thức, helper, logic con hay query nào chỉ phục vụ nội bộ class mà KHÔNG thuộc hợp đồng API / Interface / đối ngoại thì BẮT BUỘC phải để `private` (hoặc `internal` nếu chỉ dùng nội bộ cùng assembly/module), TUYỆT ĐỐI KHÔNG để `public`.
  - **CẤM nâng `private` lên `public` chỉ để phục vụ viết Unit Test**: TUYỆT ĐỐI CẤM đổi access modifier của hàm nội bộ từ `private` thành `public` chỉ vì muốn project kiểm thử (`tests/`) có thể gọi được trực tiếp. Hành vi này vi phạm nguyên tắc bao đóng (encapsulation), làm lộ chi tiết cài đặt (implementation details) và làm ô nhiễm public interface của class.
  - **Quy chuẩn kiểm thử hàm `private`**:
    * *Ưu tiên số 1*: Kiểm thử gián tiếp thông qua các hành vi / luồng public API của class/worker (Black-box testing).
    * *Khi cần kiểm thử cô lập nhánh lỗi sâu của hàm `private`*: BẮT BUỘC dùng **Reflection** (`BindingFlags.NonPublic | BindingFlags.Static` hoặc `BindingFlags.Instance`) thông qua private helper trong file test (`tests/`). TUYỆT ĐỐI KHÔNG sửa code production sang `public`.
- **Cấm Tạo Hàm Alias Thừa Thãi (No Redundant Alias Methods / Overlapping Wrappers)**: Khi đổi tên hoặc chuẩn hóa một hàm/phương thức, BẮT BUỘC đổi tên trực tiếp và cập nhật call-sites liên quan. TUYỆT ĐỐI CẤM tạo hàng loạt các hàm wrapper/alias 1 dòng (VD: `FooShort() => Foo()`, `FooOld() => Foo()`, `FooVariant() => Foo()`) với lý do "tiện gọi" hoặc "tương thích ngược" trong cùng codebase nội bộ. Điều này gây phình to bề mặt API (bloated API surface), gây rối loạn cho người đọc code và vi phạm triệt để nguyên tắc Clean Code (KISS, YAGNI, Single Source of Truth).
  - **Thứ tự sắp xếp thành viên trong Class (Bắt Buộc Chuẩn Từ Trên Xuống Dưới)**:
    1. **Fields**: Hằng số (`const`), biến tĩnh (`static readonly`), biến thành viên (`private readonly`, instance fields) đặt ở **ĐẦU TIÊN** của class.
    2. **Properties**: Các thuộc tính (`{ get; set; }`).
    3. **Records / Structs / Nested Types**: Các định nghĩa `record`, `record struct`, `struct`, hoặc class lồng nhau (nested types, DTO/Outcome nội bộ).
    4. **Constructors**: Hàm khởi tạo (Constructor tường minh hoặc Primary Constructor).
    5. **Base Class Lifecycle & Overrides (Ưu tiên cao nhất trong khối phương thức)**: Các phương thức `override` kế thừa trực tiếp từ lớp cha (`protected override async Task ExecuteAsync(CancellationToken stoppingToken)`, `protected override void Dispose(bool disposing)`...).
       * *Lý do*: Đây là entry point và xương sống vòng đời chính của class (đặc biệt trong `BackgroundService`, `IHostedService`, `ServiceBase`). Đặt ở đầu khối method giúp người đọc mở file ra là thấy ngay luồng thực thi chủ đạo của class, không bị trôi xuống dưới hàng loạt hàm public hay hàm nội bộ. Độ ưu tiên vị trí cao hơn `public` methods và các hàm nội bộ.
    6. **Public Methods**: Toàn bộ các phương thức `public` của class (API, Interface implementation, nghiệp vụ công khai).
    7. **Protected Methods**: Các phương thức `protected` nội bộ khác (nếu có, không thuộc nhóm lifecycle override kế thừa từ lớp cha).
    8. **Private Methods**: Toàn bộ phương thức `private` (helper, private async method, query con...) BẮT BUỘC đặt ở **CUỐI CÙNG của class/file**, sau toàn bộ các phương thức trên. TUYỆT ĐỐI KHÔNG đặt hàm `private` xen kẽ ở đầu hoặc giữa các method khác.
    9. **Vị trí hàm mới bổ sung (Append-Only / Đặt ở cuối khối hoặc cuối class)**: Khi viết thêm các hàm/phương thức mới vào class/file hiện hữu (ví dụ: `UpdateParentOutcome` trong `ShareDataTransferLog`, hoặc các hàm xử lý mới), BẮT BUỘC đặt ở **CUỐI CÙNG** của khối phương thức tương ứng (hoặc cuối cùng của class), TUYỆT ĐỐI KHÔNG chèn chen ngang vào đầu khối method hoặc nằm giữa các hàm nghiệp vụ chủ đạo cốt lõi đã có từ trước (tránh làm xáo trộn cấu trúc code hiện hữu, giúp người đọc dễ theo dõi và giữ git diff sạch sẽ).
- **Đặt tên biến kết quả ORM SqlSugar / ADO.NET (`ExecuteCommandAsync`)**:
  - `ExecuteCommandAsync` trả về số dòng bị ảnh hưởng (`int`).
  - **BẮT BUỘC** đặt tên thể hiện rõ bản chất số lượng bản ghi: `affected`, `lockedRows`, `updatedRows`, `deletedRows`, `insertedRows`.
  - **TUYỆT ĐỐI CẤM** đặt tên kiểu cờ boolean (như `claimed`, `isSuccess`, `hasLock`), tránh gây nhầm lẫn kiểu dữ liệu khi kiểm tra điều kiện (phải dùng so sánh số lượng `<= 0` hoặc `> 0`).
- **Thống nhất thuật ngữ Concurrency OCC Lock**:
  - Thống nhất tuyệt đối sử dụng thuật ngữ **`lock`** (`LockedSubscription`, `ReleaseLock`, `lockedRows`, `lockDurationSeconds`, `DefaultLockBudgetPercent`) cho cơ chế tranh chấp độc quyền tài nguyên (OCC Guard qua `ProcessingUntil`).
  - **CẤM** dùng các từ ngữ cũ/pha tạp như `lease`, `claim` trong tên biến, tên hàm, tên hằng số, tài liệu và log message.
  - 🔴 **Ngoại lệ có chủ đích (chốt 28/09/2026)**: từ vựng `lock` áp cho **tên biến / tên hàm** của cơ chế (`ReleaseLock`, `lockedRows`, `lockDurationSeconds`, `DefaultLockBudgetPercent`), còn **tên cột CSDL** đặt theo **nghiệp vụ của entity** — cụ thể `ShareDataSubscription.ProcessingUntil`, ⛔ không đặt `LockedUntil`/`LockExpireTime`. Lý do: cột nằm trong bảng nghiệp vụ, người đọc schema cần hiểu nó nói gì về đăng ký chứ ⛔ không cần biết cơ chế đồng thời bên dưới. ⛔ Lượt sau không ai được "đồng bộ lại" hai bên.
- **Phân định ranh giới Cảnh Báo Hệ Thống (`AlertLog`) vs Nhật Ký Ứng Dụng (`ILogger` / `LogWarningMsg`)**:
  - **`AlertLog`** (`ShareDataTransferLog.WriteAlertAsync`): CHỈ dành cho các lỗi/sự cố nghiệp vụ phát sinh **trong quá trình xử lý luồng dữ liệu** mà quản trị viên cần can thiệp (như `PacketNotFound`, `MappingNotFound`, `QueryFailed`, `RequiredFieldMissing`, `HttpSendFailed`...).
  - **`ILogger` / `LogWarningMsg`**: Các tình huống tranh chấp tài nguyên bình thường của hạ tầng OCC (như một worker khác đã nhận lại đăng ký do hết hạn timeout khi nhả lock ở `finally` — `LockLost`): **TUYỆT ĐỐI KHÔNG** ghi vào bảng `AlertLog` làm rác cảnh báo; chỉ ghi log ứng dụng nội bộ qua `LogWarningMsg`.
- **Kiến trúc Logging trong Worker (Single Source of Truth cho Activity / Alert Logging)**:
  - Toàn bộ thao tác ghi nhận nhật ký nghiệp vụ (`ShareDataActivityLog`) và cảnh báo hệ thống (`ShareDataAlertLog`) trong Worker BẮT BUỘC tập trung tại class chuyên trách: `ShareDataTransferLog`.
  - Các Service/Orchestrator không được tự tạo các hàm private helper nội bộ để format và gọi lại CSDL ghi log, mà phải đóng gói thành các public method chuyên trách ngay trong `ShareDataTransferLog` (như `ShareDataTransferLog.LogExportResult(...)`) để tái sử dụng thống nhất.
- **Vue SFC Section Ordering**: Trong mọi file `.vue`, thứ tự khối BẮT BUỘC: (1) `<script setup lang="ts">` đầu tiên, (2) `<template>` thứ hai, (3) `<style scoped>` cuối cùng. TUYỆT ĐỐI KHÔNG đặt `<template>` trước `<script>`.
- **Vue `<script setup>` Internal Structure**: Bên trong `<script setup>` sắp xếp theo thứ tự: Imports → Props/Emits/Models → Reactive State & Stores → Computed & Watchers → Lifecycle Hooks → Methods & Event Handlers → Expose.

---

## 🔒 8. SQL, Code & Module Isolation Scope (Mandatory Rule)

- **Strict Module Scope**: Mọi script SQL (DDL & DML) sinh ra hoặc cập nhật cho 1 module CHỈ được tác động lên đúng danh sách bảng thuộc phạm vi sở hữu của module đó (VD module `ShareData`: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`).
- **Cấm tác động bảng ngoài phạm vi**: TUYỆT ĐỐI KHÔNG `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, `DELETE` lên bảng thuộc module khác (VD `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...).
- **Độc lập Ngữ cảnh, Code & Comment giữa các Phân hệ**: Các phân hệ nghiệp vụ (`VideoWall`, `ShareData`, `TMS`...) hoạt động độc lập (Loose Coupling). TUYỆT ĐỐI CẤM mang tên class, interface, service hoặc so sánh thiết kế của phân hệ này đem vào comment/XML doc của phân hệ khác. Phân hệ nào thì code và comment chỉ phục vụ đúng nghiệp vụ phân hệ đó.

---

## 🛑 9. Strict Manual SQL Execution Rule (Mandatory Rule)

- **Không tự động thực thi mutation**: Khi tạo/cập nhật script SQL (`.SQL`) hoặc cấu hình DB, CHỈ được ghi/sửa file trên đĩa.
- **Cấm tự chạy DDL/DML**: TUYỆT ĐỐI KHÔNG tự động chạy `INSERT`, `UPDATE`, `DELETE`, `ALTER`, `DROP`, `TRUNCATE` lên bất kỳ DB nào (remote hay local, qua script/code/tool) khi chưa có yêu cầu rõ ràng từ người dùng.
- **Người dùng tự duyệt & chạy**: Luôn đưa file SQL đã sinh cho người dùng xem lại và tự chạy tay.

---

## 🔒 10. MCP Database Read-Only Rule (Mandatory Rule)

- **Canonical MCP Config**: `.agents/mcp_config.json` là nguồn duy nhất để đọc; các file cấu hình MCP khác trong công cụ IDE/CLI được đồng bộ qua `.agents/hooks/sync-mcp.mjs`, **không sửa tay**.
- **Ưu tiên 1 — Bắt buộc dùng MCP cho thao tác DB**: Khi cần tra cứu/kiểm tra schema/đọc dữ liệu (Dev, Staging, Test), BẮT BUỘC đọc `@[.agents/mcp_config.json]` và gọi trực tiếp MCP server (`mssql_staging`, `mssql_dev`, `mssql_test`).
- **Fallback khi MCP lỗi**: Nếu MCP không kết nối được/lỗi runtime/thiếu tool, BẮT BUỘC báo lỗi cho người dùng trước. CHỈ khi không còn cách nào khác và được người dùng đồng ý mới dùng script PowerShell/Shell truy vấn ở chế độ strictly READ-ONLY.
- **Strict Read-Only**: Mọi thao tác DB qua MCP CHỈ được đọc (`SELECT`, `list_tables`, `describe_table`, `sample_data`, `get_relationships`...).
- **Cấm mutation tuyệt đối**: TUYỆT ĐỐI KHÔNG `INSERT`/`UPDATE`/`DELETE`/`DROP`/`ALTER`/`TRUNCATE`/`CREATE`/gọi stored procedure làm thay đổi state qua MCP hay script trong mọi trường hợp, trừ khi người dùng yêu cầu trực tiếp.

---

## 🛑 11. Strict Local Database Rule for Testing (`dotnet test` [Mandatory Rule])

- **Bắt buộc local khi test**: Khi chạy `dotnet test` (hoặc bất kỳ kịch bản unit/integration test), TẤT CẢ connection string (RDBMS: SQL Server, PostgreSQL, MySQL...; NoSQL/Cache: Redis...) BẮT BUỘC là local (`localhost`, `127.0.0.1`, `(localdb)`, `.`, container local).
- **Hủy ngay & báo cáo nếu phát hiện remote**: Trước khi chạy `dotnet test`, nếu thấy connection string trong `appsettings*.json`, `Host.cs`, hay cấu hình test trỏ ra remote/IP ngoài (VD `10.10.8.30`, domain staging/prod...), BẮT BUỘC HỦY NGAY việc chạy test và báo lại người dùng.
- **Cấm test trên DB remote**: TUYỆT ĐỐI KHÔNG chạy test khi connection string RDBMS/Redis không phải local.
- **Cấm tuyệt đối cờ `--no-build` khi chạy test**: TUYỆT ĐỐI KHÔNG thêm tham số `--no-build` vào lệnh `dotnet test` (ví dụ: `dotnet test tests/BE/test.csproj --filter "..." --no-build` là SAI). Luôn để `dotnet test` tự động kiểm tra và build incremental để bảo đảm test luôn chạy trên code mới nhất, tránh tình trạng code đã sửa nhưng test lại chạy trên DLL cũ trong cache dẫn đến sai lệch kết quả.
- **Tự động đồng bộ Schema CSDL Local khi Test (`EnableInitTable`, `EnableInitDb` - Bắt buộc)**:
  - Khi chạy `dotnet test` phát sinh lỗi thiếu cột hoặc thiếu bảng (ví dụ `Invalid column name '...'`, `Invalid object name '...'` do rebase/pull code nhánh khác có bổ sung entity):
  - **Bước 1 (Bật cờ đồng bộ):** Tạm thời bật các cờ CodeFirst của SqlSugar trong `tests/BE/appsettings.Test.json`:
    ```json
    "DbSettings": { "EnableInitDb": true },
    "TableSettings": { "EnableInitTable": true, "EnableIncreTable": false }
    ```
    ⚠️ **BẪY TỬ HUYỆT VỚI `EnableIncreTable`**:
    - **`EnableIncreTable` BẮT BUỘC PHẢI LÀ `false`** khi muốn CodeFirst tạo bảng mới!
    - **Nguyên nhân cốt lõi trong hạ tầng (`SqlSugarSetup.cs` thuộc `Shared.Infrastructure.dll`)**:
      Logic hạ tầng lọc danh sách Type quét bảng như sau:
      ```csharp
      List<Type> source = (from element in App.EffectiveTypes
          where !element.IsInterface && !element.IsAbstract && element.IsClass && element.IsDefined(typeof(SugarTable), inherit: false)
          where !element.GetCustomAttributes<IgnoreTableAttribute>().Any()
          select element)
          .WhereIF(P_1.TableSettings.EnableIncreTable, (Type type2) => type2.IsDefined(typeof(IncreTableAttribute), inherit: false))
          .ToList();
      ```
      Nếu bật `"EnableIncreTable": true`, logic SqlSugar sẽ lọc và **CHỈ quét những Entity có gắn attribute `[IncreTableAttribute]`**. Trong toàn bộ repo hiện tại KHÔNG CÓ Entity nào gắn `[IncreTable]`, dẫn tới danh sách bảng cần khởi tạo bị rỗng (0 bảng) ⇒ **KHÔNG CÓ BẢNG NÀO ĐƯỢC TẠO** dù `EnableInitTable: true`!
    - Chỉ cần `"EnableInitTable": true, "EnableIncreTable": false`, SqlSugar sẽ gọi `InitTables(...)` quét toàn bộ entity có `[SugarTable]` và tự động tạo bảng hoặc đồng bộ bổ sung cột mới.
  - **Bước 2 (Tắt lại cờ về `false` sau khi test ổn):** Ngay sau khi test đã chạy qua thành công (CSDL local đã cập nhật schema xong), **BẮT BUỘC SỬA LẠI TOÀN BỘ CỜ THÀNH `false`** (`EnableInitDb: false`, `EnableInitTable: false`, `EnableIncreTable: false`) để tránh lặp lại kiểm tra schema làm chậm tốc độ chạy test ở các lần sau và giữ file cấu hình sạch sẽ.
  - **Bước 3 (Chỉ báo cáo khi bật cờ không được):** Chỉ khi nào đã bật đủ các cờ trên mà test vẫn báo lỗi schema (do constraint phức tạp, kiểu dữ liệu xung đột...) thì mới báo lại cho người dùng kèm câu lệnh SQL để xử lý thủ công; tuyệt đối không tự chế/hack code trong file test.

---

## 🛑 12. Quy Tắc Kill Tiến Trình Khi Rebuild / Chạy WPF (WPF Process Termination Rule [Mandatory Rule])

- **Chỉ kill riêng tiến trình WPF**: Mỗi lần rebuild/re-run/chạy test liên quan module WPF, nếu cần giải phóng DLL/EXE bị lock, CHỈ được tắt đúng tiến trình WPF (`Module.VideoWall.WPF` hoặc PID đang lock DLL đó).
- **Cấm kill diện rộng**: CẤM `Stop-Process -Name "dotnet"`, `Get-Process testhost*,dotnet* | Stop-Process`, `taskkill /f /im dotnet.exe` hay kill hàng loạt `dotnet*`/`testhost*` — dễ tắt nhầm WebAPI/Worker/background service/MockServer khác đang chạy.
- **Lệnh chuẩn (targeted kill only)**:
  ```powershell
  Get-Process -Name "Module.VideoWall.WPF" -ErrorAction SilentlyContinue | Stop-Process -Force
  ```
- **AI không tự chạy WPF**: Người dùng tự chủ động `dotnet run` ứng dụng WPF khi cần; AI không tự ý chạy sau khi sửa code/test.

---

## 🛑 13. Quy Tắc Vị Trí File Prompt / Plan / Task (Prompt & Plan File Location Rule [Mandatory Rule])

- **Không lưu ngoài repo**: Cấm ghi file prompt/plan/task-breakdown vào `~/.claude/plans/`, `%USERPROFILE%\.claude\plans\`, thư mục scratchpad/temp, hay bất kỳ đâu ngoài `c:\ThienAn\`.
- **Nơi lưu chuẩn (bắt buộc trong repo) — mỗi phân hệ có 2 thư mục riêng `Plan/` và `Prompt/` (chốt 2026-09-16)**:
  - `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Plan/` — CHỈ chứa tài liệu SỐNG (plan tổng thể, review tiến
    độ), KHÔNG chứa prompt dùng-1-lần. VD: `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/Plan/`.
  - `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Prompt/` — CHỈ chứa prompt thực thi từng bước. AI **TUYỆT
    ĐỐI KHÔNG tự động xóa** sau khi thực thi xong để người dùng review và đối chiếu sau khi code
    change. VD: `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/Prompt/`. Chưa có prompt nào thì KHÔNG tạo
    thư mục rỗng trước — tạo khi có file prompt đầu tiên. Xem chi tiết ở mục 19.2 bên dưới.
  - `DocBusinessThienAn/HữuNghị-ChiLăng/Plan/` (top-level, KHÔNG có phân hệ con) chỉ còn dùng cho kế
    hoạch/biên bản họp **XUYÊN phân hệ** (toàn tuyến, liên quan ≥ 2 phân hệ cùng lúc) — không đặt
    prompt/plan riêng của 1 phân hệ cụ thể ở đây nữa.
  - Prompt/plan hạ tầng/tooling/AG-Kit (không thuộc domain nghiệp vụ) → `.agents/prompts/`.
- **Đặt tên**: `<task-slug>-prompt.md` (trong `Prompt/`) hoặc `<Xx>_MasterPlan_<ngày>.md` /
  `<Xx>_Review_<...>.md` (trong `Plan/`) — kebab-case cho prompt, tiếng Việt không dấu hoặc tiếng Anh.
- **BẮT BUỘC ghi tên & đường dẫn file ngay đầu nội dung file prompt (tiện 1-click copy)**: Trong mọi file prompt markdown (`*-prompt*.md`), BẮT BUỘC ghi rõ đường dẫn tệp (tương đối từ root repo, ví dụ: `**Tệp prompt:** `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Prompt/<task-slug>-prompt.md``) ngay phần header đầu file dưới dạng inline code để người dùng tiện lợi double-click hoặc copy một chạm khi giao việc hoặc chạy lệnh.
- **Chế độ Plan (ExitPlanMode)**: Nếu harness ép ghi plan vào `~/.claude/plans/`, ngay sau khi plan được duyệt BẮT BUỘC sao chép vào đúng thư mục (`Plan/` hoặc `Prompt/` tuỳ loại nội dung) của phân hệ tương ứng trong repo (xem trên) và coi bản trong repo là bản chính thức; báo người dùng đường dẫn trong repo, không phải `~/.claude/plans/`.
- **CẤM Auto-cleanup file Prompt (Keep for User Review)**: AI **TUYỆT ĐỐI KHÔNG tự động xóa** file trong `Prompt/`
  (khớp `*-prompt*.md`, `{task-slug}.md`) sau khi task hoàn tất. Bắt buộc giữ lại để người dùng review sau code
  change. Chỉ xóa khi người dùng đã nghiệm thu và có chỉ định xóa rõ ràng.
  (Đối với file trong `Plan/`: luôn là tài liệu sống, không bao giờ tự xóa. Đối với ghi chú tạm `.agents/memory/*-scratch.md`:
  xem mục 14 bên dưới).

---

## 🛑 14. Quy Tắc Ghi Chú Tạm Trong `.agents/memory/` (Transient Memory Note Rule [Mandatory Rule])

- **Phân biệt 2 loại nội dung trước khi ghi vào `.agents/memory/`**:
  - **Thường trú (persistent)** — sống lâu, càng đọc lại càng đúng: quy ước dự án, quyết định kiến trúc, sở thích người dùng, ranh giới sở hữu module, nguồn sự thật của tài liệu.
  - **Tạm (transient)** — chỉ đúng tại thời điểm chạy, sẽ sai sau vài commit: mốc số lượng test, danh sách test đang flaky, output của tool, thông báo lỗi môi trường, port/PID/đường dẫn temp cụ thể, số dòng file, phiên bản package đang cài.
- **Nghi ngờ thì coi là TẠM**: nếu không chắc nội dung còn đúng sau 1 tháng thì nó là ghi chú tạm.
- **Quy ước bắt buộc cho ghi chú tạm**:
  - Đặt tên **`<task-slug>-scratch.md`** (hậu tố `-scratch` là dấu hiệu duy nhất để dọn tự động, KHÔNG dựa vào tên riêng của từng task).
  - Frontmatter thêm `metadata.lifetime: transient`.
  - **KHÔNG** thêm vào index `.agents/memory/MEMORY.md`; nếu buộc phải thêm để tra cứu trong phiên thì cuối task **phải xoá kèm dòng index đó**.
- **BẮT BUỘC TỰ ĐỘNG XOÁ CUỐI TASK ĐỐI VỚI SCRATCH TẠM**: khi task hoàn tất (đã có kết quả cuối và đã báo cáo cho người dùng), AI **tự động xoá toàn bộ** `.agents/memory/*-scratch.md` cùng mọi dòng index trỏ tới chúng — **không hỏi lại**, không giữ "cho lần sau". (Lưu ý: File `*-prompt*.md` KHÔNG tự xóa mà giữ lại cho người dùng review theo mục 13; chỉ xóa scratchpad tạm và thư mục artifact tạm `tests/TestResults/`, `bin`/`obj` tạm do AI sinh ra).
- **Cần lại thì đo lại**: lần sau gặp cùng vấn đề thì chạy lại và viết ghi chú mới, TUYỆT ĐỐI KHÔNG tin số liệu trong bản scratch cũ.
- **Muốn giữ lâu dài thì đặt đúng chỗ, không nhét vào scratch**: nội dung thường trú → memory chuẩn trong `.agents/memory/` (có index trong `MEMORY.md`); hướng dẫn thao tác thuộc một khu vực code cụ thể → README của khu vực đó (ví dụ cách chạy test → `tests/README.MD`).

---

## 🛑 15. Quy Tắc Viết Test, Vị Trí Thư Mục & Cấm Dùng Thư Viện Mock Ngoài (Testing Standards & No-Moq Rule [Mandatory Rule])

- **Toàn bộ test tập trung tại `c:\ThienAn\tests\`, chia 2 nhánh theo tầng (chốt 02/10/2026)**:
  - `tests\BE\` — **test .NET** (`test.csproj`). Mọi quy định còn lại của mục 15 áp cho nhánh này.
    Phân hệ nghiệp vụ gom trong `tests\BE\ITS\<TênPhânHệ>\`; phần ⛔ không thuộc phân hệ nào giữ ở `tests\BE\` cấp 1.
  - `tests\FE\` — **test E2E của `TA-ITS015-WEBVUE-V1.0`** (Playwright, toolchain node riêng).
    🔴 E2E ⛔ KHÔNG chạy chung `dotnet test`: nó cần WebAPI + dev server + CSDL đang chạy.
  - 🔴 `BE` và `ITS` đều là **tầng chỉ-trên-đĩa**, ⛔ TUYỆT ĐỐI KHÔNG đưa vào namespace.
    Namespace vẫn là `Tests.<TênPhânHệ>.<ĐườngDẫnCon>` — xem lý do `CS0234` bên dưới.
  - ✅ **Vì sao dời cả project chứ không chỉ dời `ITS/`**: `test.csproj` hard-code 13 đường dẫn `ITS\...`
    trong các khối `Compile Remove`. Dời cả project thì 13 đường dẫn đó giữ nguyên, chỉ `<RepoRoot>` sâu thêm
    1 cấp. ⛔ Dời riêng `ITS/` là phải sửa 13 chỗ, sót 1 chỗ là deadlock ở TFM `net10.0-windows`.
  - **Bên trong mỗi phân hệ, thư mục con PHẢN CHIẾU project nguồn (chốt 30/09/2026)** — nhìn cây thư mục là biết bài test đang kiểm project nào:

    ```
    tests/BE/ITS/VideoWall/
    ├─ WebApi/        <- src/Modules/VideoWall/Module.VideoWall   (Controllers/, Services/)
    ├─ Worker/        <- src/Services/VideoWall/ITS.VideoWall     (ISAPIDevice/, Scene/, Heartbeat/, Messaging/)
    ├─ Wpf/           <- src/Services/VideoWall/ITS.VideoWall.WPF
    ├─ Mocks/         <- lớp giả lập của phân hệ
    └─ VwMockServerRunner/ <- console exe riêng
    ```

    🔴 Dùng **`WebApi/`** cho phần `Module.<Tên>`, ⛔ TUYỆT ĐỐI KHÔNG đặt thư mục tên `Module/`: namespace sẽ thành `Tests.<Tên>.Module` khiến C# phân giải nhầm mọi tên đủ điều kiện `Module.<Tên>.*` ⇒ lỗi `CS0234`.
    🔴 **Đổi tên thư mục test thì BẮT BUỘC sửa `tests/BE/test.csproj` cùng lượt** — tệp đó hard-code tên thư mục trong các khối `Compile Remove` theo `TargetFramework`. Không sửa thì ở TFM `net10.0-windows` test backend ⛔ không bị loại ⇒ chạy trùng và deadlock.
- **CẤM TẠO PROJECT HOẶC THƯ MỤC TEST .NET MỚI TRONG CÁC SUB-DIRECTORY**:
  - TUYỆT ĐỐI KHÔNG tạo thư mục test, sub-folder hay file `.csproj` test mới bên trong `TA-ITS015-WEBAPI-V1.0\tests\`, trong `src\Modules\...`, hay bất kỳ vị trí nào khác ngoài `c:\ThienAn\tests\BE\`. (⛔ Không áp lệnh này cho project E2E của FE ở `tests\FE\`).
  - Mọi file test mới (unit test, integration test, validator test, fixture test) của bất kỳ phân hệ nào BẮT BUỘC phải viết trực tiếp vào thư mục tương ứng bên trong `c:\ThienAn\tests\BE\ITS\<TênPhânHệ>\`.
- **CẤM DÙNG THƯ VIỆN MOCK BÊN NGOÀI (`Moq`, `NSubstitute`, `FakeItEasy`)**:
  - `tests/BE/test.csproj` **hoàn toàn KHÔNG tham chiếu thư viện Moq**.
  - **CẤM `using Moq;`**, **CẤM `new Mock<T>()`**.
  - **TUYỆT ĐỐI KHÔNG tự tiện suy diễn** các thư viện phổ biến bên ngoài khi chưa mở file `tests/BE/test.csproj` để kiểm tra `PackageReference`.
- **Quy Chuẩn Viết Test Trong Dự Án (xUnit + Host Pattern)**:
  - **Dependency Injection qua Host**: Mọi test class dùng cấu trúc `[Collection("api")] public class ...Tests(Host host)`.
  - **Lấy Localizer**: Dùng trực tiếp `private readonly IStringLocalizer _localizer = host.Localizer;` (KHÔNG mock `IStringLocalizer`).
  - **Lấy Services**: Dùng `host.Services.GetRequiredService<T>()`.
  - **Giả lập thiết bị**: Dùng mock server nội bộ đã được cấu hình trong repo (như `host.MockServer` / `VwISAPIMockServerHikvision`).
  - **Test cô lập không qua Host (CHỈ áp dụng cho POCO/DTO/XML/JSON/Formula thuần túy)**: Viết test xUnit thuần không phụ thuộc DB/DI.
  - ⛔ **CẤM TỰ TẠO CLASS STUB/MOCK NỘI BỘ CHO SERVICE NGHIỆP VỤ & NATS**: TUYỆT ĐỐI KHÔNG tự viết các class giả lập (`TestMock...Service`, `Fake...Service`) để thay thế các service lõi (`IDataOutboundService`, `IDataInboundService`...) hoặc NATS pub/sub nội bộ chỉ nhằm đếm số lần gọi hàm. **Ngoại lệ hợp lệ duy nhất được giả lập** là máy chủ HTTP bên ngoài, và phải bằng mock server `HttpListener` thật trên `127.0.0.1` (xem chi tiết mục 19.18). Mọi service nghiệp vụ và NATS BẮT BUỘC phải lấy bản thật từ `host.Services` và test luồng thật qua CSDL local (xem chi tiết mục 19.18).
- **Global Usings của Module Test**:
  - Bổ sung namespace/using của module vào `tests\BE\ITS\<TênPhânHệ>\GlobalUsings.<TênPhânHệ>.cs` để tránh xung đột với các module khác và đảm bảo compile condition theo `tests/BE/test.csproj`.

---

## 🛑 16. Quy Tắc Ưu Tiên Đọc Tài Liệu — `.md` Trước PDF/Ảnh (Doc Read Priority Rule [Mandatory Rule])

- **Phân loại 3 Tier theo chi phí context** (trục phân loại là **token cost**, KHÔNG phải đuôi file — một `.md` 1.6 MB tốn hơn hẳn một PDF 200 KB):
  - **Tier A — AI-first (đọc trực tiếp)**: `.md` < 150 KB, `.json` cấu hình/schema, `.sql`. Đây là **nguồn sự thật** khi làm việc với agent.
  - **Tier B — On-demand (CHỈ `grep` / đọc theo `offset`+`limit`)**: `.md` ≥ 150 KB, bộ API reference dump, JSON log đo thật. VD đã đo: `VideoWall/doc/ISAPI-Videowall-Controller/09-api-reference.md` (1.671 KB), `VideoWall/LogsAPI/session-20260903-real.json` (1.636 KB), `session-20260904-real.json` (2.570 KB) — **TUYỆT ĐỐI KHÔNG đọc nguyên file**, luôn vào qua `00-api-catalog.md` / `LogsAPI/README.md` rồi `grep` theo endpoint hoặc key cụ thể.
  - **Tier C — Human-only (KHÔNG mở)**: `_source/**`, `**/images/**`, và mọi `*.pdf`, `*.xlsx`, `*.xls`, `*.docx`, `*.doc`, `*.pptx`, `*.zip`, `*.png`, `*.jpg`, `*.jpeg`, `*.gif`, `*.webp`.
- **Thứ tự tra cứu bắt buộc khi cần thông tin tài liệu** (dừng ngay khi đủ, không đi tiếp):
  1. `README.md` / `INDEX.md` / `llms.txt` của thư mục tài liệu (Tier Table nếu có) → xác định đúng file cần đọc.
  2. File `.md` / `.json` / `.sql` Tier A trong `doc/`.
  3. Tier B qua `grep` với keyword cụ thể (kèm `00-catalog.md` làm mục lục).
  4. **Chỉ đến bước này mới cân nhắc Tier C — và phải hỏi người dùng trước.**
- **CẤM tự ý mở file Tier C**: TUYỆT ĐỐI KHÔNG đọc/parse/convert PDF, XLSX, DOCX, ảnh, ZIP và KHÔNG `glob`/bulk-read `_source/**` hay `**/images/**`. Ngoại lệ duy nhất: **người dùng chỉ đích danh tên file đó** (VD "đọc `Controller phần cứng.pdf` trang 12", "xem ảnh `fig-05-main-control-board.png`").
- **Thiếu bản `.md` thì BÁO, KHÔNG tự đọc bản gốc**: nếu thông tin cần thiết chỉ tồn tại trong file Tier C mà chưa có bản `.md` tương ứng, BẮT BUỘC báo cáo khoảng trống đó cho người dùng và đề xuất tạo bản `.md` — KHÔNG âm thầm nạp PDF/ảnh vào context để "cho nhanh".
- **Trích dẫn đường dẫn `.md`, không trích PDF**: khi trả lời hoặc viết tài liệu, luôn dẫn tới bản `.md` (kèm số trang bản gốc nếu cần đối chiếu), KHÔNG dẫn thẳng file PDF/XLSX làm nguồn.
- **Mọi file Tier C phải có bản `.md` tương ứng**: khi thêm tài liệu gốc mới vào `_source/`, BẮT BUỘC tạo bản `.md` đặt trong `doc/` cùng module, kèm frontmatter provenance (`tier`, `read`, `source`, `source_pages`, `extracted`) và cập nhật Tier Table trong `README.md` của module.
- **Cấu trúc & validator**: cấu trúc thư mục tài liệu chuẩn, schema Tier Table và validator `check_doc_links.py` đặc tả tại `.agents/prompts/chuan-hoa-cau-truc-tai-lieu-prompt.md`. Nếu đổi ngưỡng 150 KB thì phải sửa đồng thời ở mục này, file prompt đó và `.kiro/steering/main.md` để 3 nơi không lệch nhau.

---

## 🛑 17. Quy Tắc Ưu Tiên MCP Database & Cấm Can Thiệp Docker / Hạ Tầng Tự Ý (Database MCP & Infrastructure Safety [Mandatory Rule])

- **Ưu tiên tuyệt đối dùng MCP Server cho Database**:
  - Mọi thao tác tra cứu, kiểm tra schema, dữ liệu, records của Database (`DEV_ITS10`, `test`, `staging`) BẮT BUỘC ưu tiên gọi qua các MCP server đã cấu hình trong dự án (`mssql_dev`, `mssql_test`, `mssql_staging` qua `dotnet tool run dab`).
  - **NGHIÊM CẤM tự ý chạy `sqlcmd`** hoặc các script shell tự chế để truy vấn DB trong terminal khi chưa có yêu cầu tường minh từ người dùng.
- **NGHIÊM CẤM tự ý can thiệp Docker / Services / Hạ tầng máy chủ**:
  - TUYỆT ĐỐI KHÔNG tự tiện chạy các lệnh thay đổi trạng thái hạ tầng: `docker restart`, `docker stop`, `docker rm`, `docker-compose down/up`, `systemctl restart`, `net stop`, kill service,...
  - Khi phát hiện container hoặc dịch vụ nền gặp sự cố (ví dụ: treo lock, tràn RAM, connection timeout):
    1. BÁO CÁO rõ ràng hiện tượng và nguyên nhân lỗi cho người dùng.
    2. ĐỀ XUẤT giải pháp xử lý (ví dụ: đề xuất restart container cụ thể).
    3. CHỈ THỰC HIỆN sau khi người dùng xác nhận đồng ý ("OK", "Đồng ý", "Chạy đi").
- **Cấu hình MCP cho môi trường AI**:
  - File cấu hình MCP chuẩn của dự án nằm tại `.mcp.json` (dùng `dotnet tool run --allow-roll-forward dab start ...`).
  - Cấu hình này được map tương ứng vào file cấu hình người dùng của Antigravity (`~/.gemini/config/mcp_config.json`).
  - Không over-engineer hay tự ý viết thêm các script phức tạp nếu chỉ cần cấu hình trực tiếp từ dotnet tool có sẵn.

---

## 🛑 18. Quy Định Bóc Tách / Xử Lý File Ghi Âm (Audio Transcription Rule [Mandatory Rule])

- **Phương thức bóc tách mặc định duy nhất (MANDATORY)**:
  - BẮT BUỘC sử dụng **Gemini Multimodal Native Audio Understanding qua `view_file`** (gọi `view_file` với path tệp nhị phân `.m4a`/`.mp3` để nạp thẳng âm thanh vào context).
  - **NGHIÊM CẤM**: Tuyệt đối KHÔNG tự ý chuyển sang dùng Whisper, `faster-whisper`, pipeline Python hay bất kỳ công cụ ASR cục bộ nào. Lý do: chạy CPU ngốn tài nguyên máy trạm, mất hàng giờ, dễ gây ảo giác (hallucination mẫu YouTube) và không bắt được đúng thuật ngữ chuyên ngành ITS/C# như Gemini Multimodal.
- **Bắt buộc cấu trúc 2 phần khi xử lý file ghi âm cuộc họp**:
  1. **Tóm tắt tổng quan & Quyết định kỹ thuật / nghiệp vụ cốt lõi (Executive Summary)**: Nêu bật các kết luận, hành động cần làm, thay đổi kiến trúc hoặc quy ước dữ liệu đã chốt trong cuộc họp.
  2. **Toàn văn nội dung đối thoại (Full Verbatim Transcript)**: BẮT BUỘC trình bày dạng bảng chi tiết gồm đủ 3 cột:
     - **Mốc thời gian (Timestamp)**: Định dạng chuẩn `mm:ss` (hoặc `hh:mm:ss`) bám sát dòng thời gian của file ghi âm.
     - **Người nói (Speaker)**: Phân định rõ ràng từng người tham gia (VD: `Người 1`, `Người 2` kèm vai trò/ngữ cảnh nếu xác định được).
     - **Lời thoại chi tiết**: Ghi lại nguyên văn nội dung trao đổi, không cắt gọt cụt lủn hay tự ý giản lược thoại đối đáp.
- **Quy chuẩn lưu trữ file transcript**: Khi tạo mới hoặc cập nhật tài liệu transcript trong thư mục dự án (như `doc/transcript/*.md`), phần bảng toàn văn đối thoại (kèm mốc thời gian và định danh người nói) BẮT BUỘC phải được đưa vào tài liệu (thường đặt ở mục cuối cùng).
- **Cập nhật mục lục**: Luôn đăng ký file transcript mới vào `doc/transcript/00-catalog.md` và Tier Table của `README.md` cùng phân hệ.

---

## 🛑 19. Quy Định Vận Hành AI & Quy Chuẩn Phát Triển Riêng (AI Operational & Dev Rules [Mandatory])

- **19.1. Nguồn sự thật duy nhất (SSOT) — CẤM tạo file `feedback-*.md` rải rác & CẤM ghi trùng lặp quy tắc vào `MEMORY.md` (chốt 25/09/2026)**:
  - Toàn bộ quy tắc, quy ước và phản hồi của người dùng BẮT BUỘC ghi duy nhất vào file [`thienan_rules.md`](file:///c:/ThienAn/.agents/rules/thienan_rules.md).
  - **Không ghi đúp quy tắc vào `MEMORY.md`**: Khi đã thêm/cập nhật quy tắc vào `thienan_rules.md`, **TUYỆT ĐỐI KHÔNG CẦN và KHÔNG ĐƯỢC thêm vào `MEMORY.md`**. File `MEMORY.md` chỉ đóng vai trò là bảng mục lục trỏ tới và yêu cầu AI đọc `thienan_rules.md`. Việc ghi trùng lặp vào `MEMORY.md` gây phân tán, tốn token và dễ lệch pha nội dung khi cập nhật.
  - TUYỆT ĐỐI KHÔNG tự tiện tạo các file `feedback-*.md` trong thư mục `.agents/memory/`. Chỉ cần sửa file này là toàn bộ hệ thống AI tự động tuân thủ.
  - Khi làm việc trên nhánh `feat` hoặc làm task cập nhật (update) cấu hình/thực thể, commit message bắt buộc dùng tiền tố `feat`, không dùng `fix`.

- **19.2. Quy trình Lên Plan & Bàn giao (Plan Handoff) (chốt 2026-09-16)**: Cấu trúc thư mục `Plan/`/`Prompt/` từng phân hệ và quy tắc không tự xoá Prompt: xem mục 13 (không lặp lại ở đây — đã gộp 25/09/2026 để tránh trùng ý). Riêng 2 điểm chưa nằm ở mục 13:
  - Khi người dùng yêu cầu lên plan: AI **chỉ nghiên cứu + viết plan**, KHÔNG tự ý sửa code kể cả sau khi plan được approve, trừ khi người dùng ra lệnh rõ ràng ("làm đi", "code đi").
  - Khi hoàn tất 1 prompt hoặc đổi trạng thái backlog, BẮT BUỘC cập nhật lại `Vw_MasterPlan_*.md`
    (trong `Plan/`) tương ứng của phân hệ đó (đừng để MasterPlan lạc hậu so với trạng thái file
    prompt thật trong `Prompt/`).
  - 🔴 **Nghĩa vụ cập nhật tài liệu ở gạch đầu dòng trên phải được VIẾT VÀO TRONG chính tệp prompt** — vì theo mục
    10.5 AI chỉ viết prompt và **không tự thực thi** (chỉ thực thi khi chủ dự án yêu cầu trực tiếp), nên bước áp
    prompt có thể do người khác làm hoặc làm ở phiên khác. Cách viết mục đó và 3 thông tin bắt buộc phải nêu: xem
    **mục 19.23**.

- **19.3. Quy tắc đặt tên Method Async**: Xem mục 7 "Async Method Naming" (không lặp lại ở đây — đã gộp 25/09/2026 để tránh trùng ý).

- **19.4. Cấm chạy thủ công DDL / ALTER TABLE & Cấm tự viết hàm tạo bảng (EnsureTablesCreated)**: Xem mục 6 "Cấm Gọi InitTables<T>() / Tạo Hàm EnsureTablesCreated() Trong Host/Test" — không lặp lại ở đây (đã gộp 25/09/2026 để tránh trùng ý).

- **19.5. Đào sâu nguyên nhân gốc rễ (Root Cause) trước khi đề xuất fix**:
  - Khi phát hiện kiến trúc lạ hoặc code có vẻ "sai", BẮT BUỘC đọc hết các file liên quan và docstring/comment gốc để hiểu toàn bộ bối cảnh và quy mô thực tế, tránh đề xuất các bản vá bề mặt.

- **19.6. CẤM TUYỆT ĐỐI tự ý sửa/xóa code ngoài phạm vi yêu cầu (Strict Scope Control & No Unprompted Code Deletion)**:
  - Khi người dùng yêu cầu một công việc cụ thể (ví dụ: sửa launch.json, phân tích lỗi, sửa một method cụ thể), AI CHỈ ĐƯỢC PHÉP thao tác đúng trong phạm vi đó.
  - **TUYỆT ĐỐI KHÔNG tự ý xóa code, dọn dẹp code, xóa hàm/file** (kể cả khi nhận thấy là dead code hoặc không còn được gọi) nếu KHÔNG CÓ YÊU CẦU TRỰC TIẾP VÀ TƯỜNG MINH từ người dùng.
- **19.7. Bắt buộc viết XML Summary chuẩn cho mọi hàm xử lý duyệt cây dữ liệu, template và binding (`HasFieldBinding`, `IsRecordTemplateArray`...)**:
  - Mọi hàm helper xử lý cấu trúc cây (JSON/AST), duyệt phễu Shape, kiểm tra ràng buộc template dữ liệu (đặc biệt là `HasFieldBinding`, `IsRecordTemplateArray`, `TryNavigate`...) BẮT BUỘC phải có comment XML `/// <summary>` đầy đủ, chuẩn mực.
  - **Nội dung comment bắt buộc gồm**:
    1. `Author`: Tác giả viết/duyệt logic (VD: `Author: Đạt`).
    2. `Description`: Giải thích rõ ràng mục đích nghiệp vụ (làm gì, kiểm tra cái gì, nhận diện token nào như `$field`, tại sao cần).
    3. `Guard / Safety limit`: Nêu rõ chốt chặn an toàn (ví dụ: giới hạn độ sâu đệ quy `depth > MaxShapeDepth` để chống tràn stack/StackOverflowException).
    4. `Cross-pipeline Sync`: Nhấn mạnh việc giữ đồng bộ ngữ nghĩa giữa các chiều (ví dụ: `HasFieldBinding` bên Outbound `DataMappingProcess` và Inbound `DataInboundService.Parse` phải hoàn toàn nhất quán, lệch nhau sẽ khiến bên gửi và bên nhận hiểu sai cấu trúc gói tin).
    5. Đầy đủ các thẻ `<param>` và `<returns>`.
  - 🔴 **PHẠM VI HẸP — đây là NGOẠI LỆ của quy tắc "XML Summary phải ngắn" ở mục 5.7 (chốt 28/09/2026)**: mục 19.7 CHỈ áp cho đúng nhóm hàm duyệt cây dữ liệu / template / binding nêu trên, nơi lệch ngữ nghĩa giữa chiều gửi và chiều nhận là lỗi **không thể phát hiện bằng biên dịch** nên phải cảnh báo ngay tại chỗ. ⛔ TUYỆT ĐỐI KHÔNG viện dẫn mục này để viết XML doc dài cho field, property, hàm thường hay class thông thường — mặc định toàn dự án là **`Description:` tối đa 2–3 dòng**, phần lý do thiết kế để ở MasterPlan.

- **19.8. Quy Chuẩn Ngôn Ngữ Báo Cáo & Biên Bản (Ưu tiên tiếng Việt thuần túy, cấm lạm dụng chêm tiếng Anh)**:
  - Khi viết báo cáo, biên bản họp, tóm tắt điều hành và tài liệu kỹ thuật hướng đến nhân sự và quản lý người Việt, **BẮT BUỘC dùng tiếng Việt trong sáng, dễ hiểu**, tránh chêm tiếng Anh chuyên ngành bừa bãi gây rào cản nhận thức.
  - **Bảng đối chiếu thuật ngữ chuẩn**:
    - Thay vì *Watermark*: Dùng **Mốc đánh dấu đã gửi** hoặc **Mốc gửi gần nhất**.
    - Thay vì *Incremental*: Dùng **Gửi nối đuôi** hoặc **Gửi tiếp dữ liệu phát sinh**.
    - Thay vì *Mapping Profile*: Dùng **Hồ sơ ánh xạ**.
    - Thay vì *Snapshot*: Dùng **Bản chụp toàn bộ** hoặc **Dữ liệu hiện trạng**.
    - Thay vì *CDC / Real-time Change Detection*: Dùng **Phát hiện dữ liệu mới tức thì** hoặc **Bắt thay đổi dữ liệu**.
    - Thay vì *Polling*: Dùng **Quét kiểm tra định kỳ**.
    - Thay vì *Payload*: Dùng **Gói dữ liệu truyền** hoặc **Nội dung bản tin**.
  - **Ngoại lệ duy nhất**: Tên biến code, tên method, tên bảng/cột CSDL, giao thức chuẩn quốc tế (như `LastTime`, `LastKey`, `PartnerCode`, `HTTP`, `REST API`, `JSON`, `SqlSugar`) được giữ nguyên và đặt trong dấu backtick (code inline).

- **19.9. Cơ Cấu Nhân Sự Dự Án & Phân Hệ ShareData (Nguồn sự thật về phân công nhân sự)**:
  - **Phân hệ ShareData (TMC-PM-ITS-ESHARE / TA-ShareData)**: DUY NHẤT 2 thành viên kỹ thuật trực tiếp phụ trách là **Đạt** (Backend WebAPI, CSDL, Entity) và **Hiếu** (Ngô Văn Hiếu - Backend Worker, Data Engine, cấu hình & UI logic ShareData). Kiến trúc sư / Tech Lead phụ trách định hướng là **Anh Sơn**.
  - **Kiên**: **KHÔNG PHẢI FRONTEND (FE)** và **KHÔNG THUỘC PHÂN HỆ SHAREDATA**. Tuyệt đối không gán vai trò Frontend (FE) cho Kiên và không đưa Kiên vào danh sách thành viên, bảng phân công hay kịch bản đối thoại của phân hệ ShareData trong bất kỳ tài liệu hay báo cáo nào.

- **19.10. Quy chuẩn Kiểm thử Phần mềm — Ưu tiên Toàn trình Nghiệp vụ (Full Business Flow) thay vì Unit Test Vi mô (Micro Tests)**:
  - **Mục tiêu kiểm thử cốt lõi**: Bài test sinh ra phải bảo vệ giá trị nghiệp vụ thực tế của hệ thống, phát hiện xung đột tích hợp, lỗi truy vấn CSDL, logic phân quyền/phễu lọc và tính toàn vẹn khi lưu trạng thái vào CSDL.
  - **Cấm viết test vi mô / vụn vặt (Micro Tests)**: Không tự ý sinh các test case pure static nhỏ lẻ chỉ để test một hàm tiện ích toán học/chuỗi/ngày giờ (như các test scheduler tính lịch chạy với stub giả `Daily_Kind_No_DaysOfWeek_Accepts_Any_Day`). Các test này làm phình to mã nguồn test hàng nghìn dòng vô ích, tốn token của LLM, tăng thời gian chạy test mà không đem lại sự đảm bảo cho luồng nghiệp vụ thực tế.
  - **Phản mẫu bổ sung — test phản chiếu bảng ánh xạ hardcode (chốt 23/09/2026)**: TUYỆT ĐỐI KHÔNG viết bài test chỉ khẳng định lại nội dung của một `Dictionary` / bảng ánh xạ / danh sách hằng viết cứng trong code (ví dụ điển hình bị cấm: `ResolveTriggerPackets_WhenTmsTrafficDataChanged_ReturnsPackets103And106` — khẳng định `RawTableToPacketMap` trả về đúng các mã gói đã khai cứng ngay trong chính bảng đó). **Dấu hiệu nhận biết:** sửa bảng ánh xạ là **bắt buộc phải sửa test theo ngay** — loại test này không bao giờ phát hiện được lỗi, nó chỉ báo "bạn vừa sửa cái bạn vừa sửa", là thuế bảo trì chứ không phải lưới an toàn. Nguy hiểm hơn: nếu quy ước trong bảng đó **chưa được chốt nghiệp vụ**, bài test sẽ khoá cứng một quyết định chưa chốt và biến nó thành điều tưởng như đã chốt.
  - **Mô hình chuẩn**: Mọi bài test mới phải mô phỏng kịch bản nghiệp vụ thực tế (Business Scenario) chạy qua Host tích hợp hoặc pipeline chính của Service (như `ProcessScheduledSubscriptions`), kiểm tra tương tác giữa các thành phần và xác nhận trạng thái cuối cùng (DB checkpoint, ActivityLog, AlertLog). Mẫu tham chiếu đúng chuẩn: `ProcessTriggeredSubscriptions_WhenSendOnNewDataIsFalse_IgnoresSubscription` (seed CSDL thật → chạy pipeline → kiểm trạng thái cuối).
  - **Được phép**: test chạy qua đúng đường xử lý thật của một thành phần với stub/mock tự viết trong repo (ví dụ nhóm `NatsWorker_HandleTrigger_*` kiểm cơ chế chống dội theo thời gian và khả năng chịu payload rác). Đây KHÔNG phải micro test — xem mục 6 "Business Workflows & Mock Integration First".

- **19.11. Quy Ước Ký Hiệu Trong Tài Liệu Nghiệp Vụ (Document Icon Legend) (chốt 23/09/2026)**:
  - **Phạm vi áp dụng**: mọi file `.md` trong `DocBusinessThienAn/` **có dùng ký hiệu**. File không dùng ký hiệu nào thì **KHÔNG thêm khối `Chú giải`** (thêm vào là rác).
  - **Dùng DUY NHẤT 6 ký hiệu sau, không tự bịa thêm**:

    | Ký hiệu | Ý nghĩa |
    | --- | --- |
    | ✅ | **Đạt** — khớp / đúng / đã hoàn tất, đã kiểm chứng |
    | ⚠️ | **Cần lưu ý** — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
    | ❌ | **Không đạt** — không khớp / không đúng / không áp dụng được |
    | 🔴 | **Rủi ro nghiêm trọng** — phải xử lý **trước khi đi tiếp**, không được bỏ qua |
    | ⛔ | **Cấm tuyệt đối** — làm là sai, không ngoại lệ |
    | 📌 | **Ghi chú bối cảnh** — nguồn dữ liệu, ngày đo, thuật ngữ |

  - 🔴 **QUY TẮC QUAN TRỌNG NHẤT — ký hiệu chỉ nói MỨC ĐỘ, không nói TRỤC ĐÁNH GIÁ**: mỗi bảng BẮT BUỘC có tiêu đề cột tự nói rõ trục của nó (`Khớp?`, `Thay được?`, `Đã kiểm chứng?`). Người đọc thấy ✅ mà không biết "✅ cái gì" thì đó là lỗi của tiêu đề cột, không phải lỗi của ký hiệu. Cùng một ❌ có thể nghĩa là "code không khớp CSDL" ở bảng này và "không thay được bằng ORM" ở bảng khác — chỉ tiêu đề cột phân biệt được.
  - **BỎ HẲN, không dùng nữa**: 🟢 (trùng ✅), 📎 (thay bằng chữ "Xem:"), 🔤 (thay bằng 📌), ⚪ và ⚫ (thuộc bộ cũ).
  - **Khối `Chú giải` dán vào ĐẦU FILE** — nguyên văn, giống hệt nhau ở mọi file, đặt ngay sau tiêu đề `#` và khối blockquote metadata (ngày / nguồn), trước nội dung đầu tiên:

    ```markdown
    ### Chú giải ký hiệu

    | Ký hiệu | Ý nghĩa |
    | --- | --- |
    | ✅ | Đạt — khớp / đúng / đã hoàn tất |
    | ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
    | ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
    | 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
    | ⛔ | Cấm tuyệt đối |
    | 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

    > Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.
    ```

  - **Ngoài phạm vi quy ước này** (không đụng tới): icon giao thức AG-Kit (`📚 Using skill`, `🤖 Applying knowledge`) và icon tiêu đề mục trong chính các file `.agents/rules/*.md` (`## 🛑`, `## 🧪`, `## 🏗️`...) — chúng phục vụ mục đích khác, không phải ký hiệu đánh giá.
  - **Ghi nhận nợ kỹ thuật**: phân hệ `VideoWall` đang dùng bộ cũ 🟢 ⚪ ⚫ (`VideoWall/doc/ISAPI-Videowall-Controller/00-api-catalog.md`). Quy ước này áp dụng **từ 23/09/2026 trở đi**; tài liệu VideoWall cũ **giữ nguyên, không đi sửa hàng loạt** (quy định 19.6 + độc lập phân hệ) — khi nào có việc sửa vào file đó thì chuyển sang bộ chuẩn luôn. Đây là chủ đích, không phải sơ suất.

- **19.12. Thứ Tự Nguồn Dẫn Chứng Trong Tài Liệu Nghiệp Vụ — CẤM Lấy Tên Bài Test Làm Căn Cứ Nghiệp Vụ (chốt 23/09/2026)**:
  - **Phạm vi áp dụng**: mọi báo cáo, biên bản, tài liệu review trong `DocBusinessThienAn/` có đưa ra khẳng định về nghiệp vụ hoặc về hành vi hệ thống.
  - **Ba bậc nguồn dẫn chứng — mỗi loại khẳng định BẮT BUỘC dẫn đúng loại nguồn của nó**:

    | Bậc | Loại khẳng định | Nguồn dẫn chứng hợp lệ |
    | --- | --- | --- |
    | 1 | **Nghiệp vụ** — quy tắc nào bắt buộc, phạm vi áp dụng tới đâu, cái gì bỏ qua | Tài liệu đặc tả gốc (`doc/*.md`), biên bản họp (`doc/transcript/*.md`), dữ liệu thật đọc qua MCP (`mssql_staging`) |
    | 2 | **Hiện thực** — code đang thực sự làm gì | Chính mã nguồn (dẫn tên file + tên symbol, kèm trích đoạn nếu cần) |
    | 3 | **Bài test** | Chỉ để chứng minh một hành vi ở bậc 2 **đã được khoá lại** |

  - 🔴 **CẤM lấy tên bài test (`*_Test`) làm căn cứ cho khẳng định bậc 1.** Bài test được viết dựa theo code, mà code chính là thứ đang bị đem ra soi — lấy test chứng minh code là **lập luận vòng tròn**, nó chỉ nói *"code làm đúng cái code đang làm"*. Ví dụ bị cấm (lỗi thật đã mắc): *"Chỉ áp dụng cho gói `AlwaysIncremental` (103, 104, 106, 107, 109)... (test `ProcessScheduledSubscriptions_Packet108_SnapshotPolicy_DoesNotCreateOrTouchCheckpoint_Test` khẳng định điều này)"* — trong khi căn cứ thật nằm ở tiêu đề mục gói của `02-mapping-goi-tin-101-111.md` (`>= key` vs `lấy all`) và ở biên bản họp 21/09.
  - **Câu hỏi kiểm tra nhanh trước khi dẫn một tên test**: *"nếu code sai ngay từ đầu, bài test này có phát hiện ra không?"* — nếu **không**, nó không phải bằng chứng, chỉ là tấm gương soi lại chính code đó.
  - **Được phép**: dẫn tên test để chứng minh hành vi code đã được khoá lại (VD *"commit theo từng trang, hỏng trang 4 vẫn giữ nguyên 3 trang đã gửi — test `...WhenHttpFailsOnPage4_CommitsFirst3PagesAndHalts_Test` kiểm chứng"*), và bảng liệt kê độ phủ test (vì khi đó chủ đề của bảng chính là bộ test).
  - **Quan hệ với 19.10**: cùng một họ lỗi nhìn từ hai phía — 19.10 cấm **viết** bài test chỉ phản chiếu lại code/bảng hardcode; 19.12 cấm **viện dẫn** bài test như thể nó là nguồn sự thật nghiệp vụ.

- **19.13. Viết Tự Chứa — Dẫn Chiếu Chỉ Được Bổ Sung Chiều Sâu, KHÔNG Được Thay Câu Trả Lời (chốt 23/09/2026)**:
  - **Phạm vi áp dụng**: mọi tài liệu trong `DocBusinessThienAn/` **và mọi file prompt** trong thư mục `Prompt/`.
  - **Nguyên tắc**: nêu kết luận **ngay tại chỗ câu hỏi phát sinh**. Dẫn chiếu (`xem mục X`, `chi tiết ở Y`, `nằm ở mục Z`) chỉ được dùng để mời người đọc tìm hiểu **thêm** chiều sâu, ⛔ TUYỆT ĐỐI KHÔNG được là nơi chứa chính câu trả lời.
  - **Phép thử bắt buộc trước khi viết một dẫn chiếu**: *che dòng dẫn chiếu đi — đoạn văn còn tự trả lời được câu hỏi mà nó vừa đặt ra không?* Nếu **không** thì đang viết sai, phải đưa kết luận vào tại chỗ.
  - 🔴 **Ví dụ bị cấm (lỗi thật đã mắc)**: *"→ **Vậy có nên chuyển sang đọc từ CSDL không?** Khuyến nghị và 3 việc đề xuất nằm ở mục 6.7."* — đặt ra câu hỏi rồi từ chối trả lời, đẩy người đọc cuộn đi hơn 270 dòng mới biết kết luận.
  - ✅ **Ví dụ đúng**: *"Vậy có nên chuyển sang đọc từ CSDL không? **Không.** Chính sách dính chặt với câu truy vấn viết tay của từng gói: `QueryPacket102` bỏ qua hoàn toàn `lastTime`, `lastKey`, `pageSize`... Lập luận đầy đủ cùng 3 việc đề xuất: mục 6.7."* — trả lời trước, dẫn chiếu sau và chỉ để đọc sâu thêm.
  - **Được phép**: ô bảng chật khi kết luận đã nằm ngay trong ô (`❌ Chưa — xem 6.8`, `⚠️ Code đúng, dữ liệu staging sai — xem 1.3`); dòng điều hướng giữa các tài liệu anh em đặt ở đầu file; mục lục; dòng ghi nguồn gốc ở đầu file prompt.
  - **Riêng với file prompt**: phần hướng dẫn thi công BẮT BUỘC tự chứa — trích thẳng đoạn code liên quan và mô tả trọn kịch bản ngay trong chính file prompt. ⛔ KHÔNG viết kiểu *"làm theo mục X của báo cáo Y"*. Người thi công phải làm được việc mà không cần mở thêm tài liệu nào khác.
  - **Lý do**: tài liệu nghiệp vụ thường được đọc một lượt để ra quyết định. Bắt cuộn lên cuộn xuống làm đứt mạch đọc và dễ khiến người đọc bỏ sót đúng cái kết luận quan trọng nhất.

- **19.14. Khung Bắt Buộc Cho Báo Cáo Rà Soát — 4 Trục, Việc Đã Xong Xuống Phụ Lục (chốt 23/09/2026)**:
  - **Phạm vi áp dụng**: mọi báo cáo rà soát / review trong `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Plan/` (`*_Review_*.md`). Không áp cho file prompt (xem 19.13) và MasterPlan.
  - **Người đọc báo cáo rà soát luôn cần đúng 4 điều.** Thân chính BẮT BUỘC có đủ 4 mục này, đúng thứ tự này, không chen mục khác vào giữa:

    | Mục | Trả lời câu hỏi | Nội dung |
    | --- | --- | --- |
    | 1 | **Đặc tả yêu cầu gì?** | Trích **nguyên văn** đặc tả / biên bản họp. Nêu rõ vị thế và ngày trích của tài liệu nguồn |
    | 2 | **Code làm được tới đâu so với đặc tả?** | Bảng đối chiếu, mỗi hạng mục 1 dòng, cột cuối là kết luận khớp / không khớp |
    | 3 | **Chưa làm những gì?** | Bảng: việc · thuộc ai · vì sao chưa · **có chặn luồng đang chạy không**. Không có việc nào thì ghi thẳng "không còn việc nào" |
    | 4 | **Edge case?** | Tình huống bất thường và cách hệ thống xử. Nêu rõ cái giá phải trả, không chỉ nêu "đã xử lý" |

  - **Mở đầu bằng bảng `Tóm tắt` đúng 4 dòng** — mỗi dòng là một câu hỏi trên, cột 2 là **câu trả lời gọn đọc là hiểu**, cột 3 là liên kết tới mục chi tiết.
  - 🔴 **Ô "Trả lời" của bảng `Tóm tắt` phải CHỨA nội dung câu trả lời**, ⛔ không được chỉ ghi số lượng (mục **19.21**) — đủ 4 trục mà ô Tóm tắt rỗng nghĩa thì báo cáo vẫn không đạt. 📌 Thứ tự các khối ⛔ **không còn bị ép cứng** (bãi bỏ 28/09/2026): mở đầu nói ngay kết quả, còn hình thức tự chọn cho hợp nội dung.
  - 🔴 **CẤM VIẾT PHỤ LỤC TRONG BÁO CÁO RÀ SOÁT (chốt 28/09/2026 — bãi bỏ toàn bộ quy định `Phụ lục A/B/C` trước đó).** Báo cáo rà soát **chỉ gồm**: `Chú giải ký hiệu`, phần mở đầu nói ngay kết quả (19.21), bảng `Tóm tắt` 4 trục, rồi 4 mục thân chính. ⛔ Hết. Không `Phụ lục A`, không `Phụ lục B`, không `Phụ lục C`, không mục "nhật ký việc đã xử lý", không "lệnh dùng để đo", không bảng phân bố tệp.
    - **Lý do (nguyên văn chủ dự án 28/09/2026):** *"mấy phụ lục này nhiễu thông tin, tôi cũng không đọc"*. Phụ lục là chỗ AI dồn thứ nó tiếc công viết ra nhưng người đọc không cần — đúng cái mục 19.21 cấm: viết hướng về người viết thay vì hướng về người đọc.
    - 🔴 **Nội dung đắt tiền ⛔ KHÔNG bị mất, nó đổi CHỖ ĐẾN — ghi THẲNG vào MasterPlan ngay lúc chốt**, ⛔ không đi vòng qua phụ lục rồi chờ lượt gộp:

      | Thứ trước đây nhét vào phụ lục | Nay ghi thẳng vào đâu |
      | --- | --- |
      | Quyết định đã chốt + **phương án bị bác và vì sao** *(đắt nhất)* | Mục `Quyết định đã chốt & phương án bị bác` của MasterPlan |
      | Giải thích cơ chế hoạt động (luồng chạy, thuật toán, cấu hình) | Mục nghiệp vụ tương ứng trong MasterPlan |
      | Việc đã đóng | ⛔ Không ghi ở đâu cả — đã xong thì người đọc báo cáo ⛔ không cần. Nếu nó kèm **lý do quyết định** thì phần lý do đó đi vào dòng MasterPlan ở trên |
      | Lệnh dùng để đo, số liệu staging, phân bố tệp | ⛔ Không vào tài liệu sống — là số liệu **tạm** (quy tắc 14). Cần lưu thì để trong prompt hoặc `*-scratch.md` |

    - ⛔ **TUYỆT ĐỐI KHÔNG để mục `✅ [ĐÃ XỬ LÝ]` trong thân chính.** **Dấu hiệu nhận biết đã viết sai:** phần "phát hiện vấn đề" viết ở thì hiện tại, rồi kẹp thêm một khối `✅ Đã chốt` ở cuối — người đọc phải lội hết lịch sử tranh luận của việc đã xong mới thấy việc còn mở.
    - **Dấu hiệu nhận biết đã viết sai (soát trước khi giao):** trong tệp có chuỗi `## Phụ lục`.
  - ⛔ **Mục 3 KHÔNG chứa việc "tuỳ chọn, không ai đang chờ"** (chốt 25/09/2026): nếu một việc không ai yêu cầu, không chặn luồng, và tự mình đã viết ra rằng không ai đang chờ xử lý — thì bỏ hẳn khỏi báo cáo, ⛔ không viết ra rồi tự gắn nhãn "tuỳ chọn" cho nó ở lại. Mục 3 chỉ liệt kê việc khớp đúng 4 cột của bảng ở trên (việc · thuộc ai · vì sao chưa · có chặn luồng không); việc "có cũng được, không có cũng được" không khớp cột nào trong đó, thêm vào chỉ tổ nhiễu, đúng cái người đọc đang cố tránh. **Dấu hiệu nhận biết đã viết sai:** câu mở đầu bằng "Một việc tuỳ chọn, không ai đang chờ:" — tự thân câu đó đã là bằng chứng việc này không thuộc báo cáo. *(Lỗi thật đã mắc: `Sharedata_Review_LuongNoiDuoi_20260923.md` mục 3 từng có dòng "cập nhật tài liệu mapping bản 20/08 cho khỏi lạc hậu" gắn nhãn tuỳ chọn — chủ dự án phản hồi "không cần đưa vô càng nhiều thông tin càng rối và nhiễu".)*
  - **Giải thích cơ chế hoạt động ⛔ KHÔNG thuộc báo cáo** (luồng chạy, thuật toán, cấu hình hạ tầng) — nó thuộc MasterPlan. Đây là tài liệu tham khảo, không phải thứ cần đọc để ra quyết định; để nó chen giữa các mục chính là làm loãng báo cáo.
  - 🔴 **Quyết định kèm lý do thì TUYỆT ĐỐI KHÔNG được mất** — nhưng ghi **thẳng vào MasterPlan ngay lúc chốt**, ⛔ không để trong báo cáo. Đặc biệt là bảng *"phương án bị bác và vì sao"*: đây là thứ đắt nhất, xoá đi là lần sau bàn lại từ đầu. 📌 Ghi thẳng còn **an toàn hơn** để ở phụ lục: báo cáo là tệp sẽ bị xoá (19.24), mà mọi bước gộp đều có thể bỏ sót.
  - ⛔ **Thân chính KHÔNG mang tên bài test, KHÔNG bảng liệt kê độ phủ test, KHÔNG số liệu `N/N PASS 100%`.**
    - Tên bài test dài 60–90 ký tự chen giữa câu làm đứt mạch đọc, và theo 19.12 nó vốn không phải căn cứ cho kết luận nghiệp vụ.
    - Cần nói một hành vi đã được khoá lại thì viết *"đã có test khoá lại"*, KHÔNG nêu tên.
    - Số `N/N PASS` là số liệu **tạm** (rule 14) — sai sau vài commit, ⛔ không đưa vào tài liệu sống.
    - Bảng liệt kê độ phủ test là **bảng chép tay**, lạc hậu ngay khi ai đó đổi tên file test. Độ phủ đọc thẳng từ `tests/`, báo cáo chỉ ghi một dòng trỏ tới thư mục đó. *(Điều này thu hẹp ngoại lệ "bảng độ phủ test" từng nêu ở 19.12: ngoại lệ đó nay chỉ còn hiệu lực cho `tests/README.MD`, không còn cho báo cáo trong `Plan/`.)*
  - **Phép thử bắt buộc trước khi giao báo cáo**: *mở file ra, trong 1 phút có trả lời được đủ 4 câu hỏi ở bảng trên không?* Nếu **không** thì chưa đạt, phải dựng lại — đừng chỉ thêm mục mới vào cuối.
  - **Lý do**: báo cáo rà soát bị phình lên theo từng lượt sửa, vì mỗi lần xong một việc lại kẹp thêm một khối `✅ Đã chốt` mà không dọn phần cũ. Sau vài lượt thì quá nửa tài liệu là lịch sử của việc đã xong, và người đọc không còn tìm ra điều mình cần. *(Lỗi thật đã mắc: `Sharedata_Review_LuongNoiDuoi_20260923.md` phình tới 499 dòng, trong đó riêng mục "rủi ro đang mở" chiếm 264 dòng với 7/8 mục con đã đóng — chủ dự án phản hồi "rất nhiều thông tin đọc rất rối".)*

- **19.15. Trạng Thái Công Việc — CHỈ CÓ "Đã làm" hoặc "Chưa làm", ⛔ KHÔNG CÓ "Làm Sau" (chốt 24/09/2026)**:
  - **Phạm vi áp dụng**: mọi báo cáo, MasterPlan, bảng trạng thái và danh sách việc trong `DocBusinessThienAn/` và `.agents/`.
  - **Đúng hai trạng thái, không có trạng thái thứ ba**:

    | Trạng thái | Nghĩa |
    | --- | --- |
    | ✅ **Đã làm** | Đã hoàn tất và kiểm chứng được |
    | ⚠️ **Chưa làm** | Mọi thứ còn lại — không phân biệt lý do, không phân biệt độ ưu tiên |

  - ⛔ **CẤM mọi nhãn trạng thái kiểu trì hoãn**: *"dự kiến làm sau"*, *"làm sau"*, *"để sau"*, *"tạm hoãn"*, *"sẽ làm"*, *"đang cân nhắc"*, *"phase 2"*, *"nice to have"*, *"đưa vào backlog"*...
  - **Lý do**: "làm sau" là trạng thái **không kiểm chứng được** — không ai biết "sau" là bao giờ, và nó khiến một việc **chưa làm** trông như đã có kế hoạch, nên không ai đi hỏi lại nữa. Khi chỉ còn hai trạng thái thì mỗi dòng đều trả lời dứt khoát: xong hay chưa xong.
  - **Lý do chưa làm thì để ở CỘT RIÊNG, không nhét vào ô trạng thái.** Bảng chuẩn của mục "Chưa làm những gì" (xem 19.14): `Việc · Thuộc ai · Vì sao chưa làm · Có chặn luồng đang chạy không`. Viết *"chưa làm vì đang chờ nguồn dữ liệu WIM"* là đủ và kiểm chứng được; thêm nhãn "làm sau" vào không bổ sung thông tin nào.
  - **Việc đã quyết định KHÔNG làm thì không phải "chưa làm"** — ghi thẳng là **đã quyết định bỏ qua**, kèm căn cứ (VD gói 111: đặc tả ghi `(skip)`). Đó là kết luận đã đóng, ⛔ không được để lẫn vào danh sách việc còn mở.
  - **Trích dẫn lịch sử giữ nguyên văn**: biên bản họp chốt chữ "hoãn" thì vẫn ghi đúng chữ đó, vì là dữ kiện đã xảy ra. ⛔ Nhưng không được lấy chữ đó làm **nhãn trạng thái** cho việc ở hiện tại.
  - **Lỗi thật đã mắc**: tài liệu mapping bản 20/08 có nhóm *"Các gói dự kiến làm sau"* gộp chung 104, 105, 106, 110, 111. Hệ quả: code đã bật 104 và 106 chạy thật thì bị hiểu nhầm là "chạy trước kế hoạch", trong khi 110 chưa chạy được vì **thiếu dữ liệu nguồn** — hai chuyện hoàn toàn khác nhau bị gộp dưới một nhãn duy nhất, và không chuyện nào được xử lý. Chủ dự án bãi bỏ nhóm này ngày 23/09/2026.

- **19.16. Quy Tắc Đặt Tên Biến Ngắn Gọn, Trực Diện (CẤM Đặt Tên Biến Dài Dòng, Thừa Thãi) (chốt 24/09/2026)**:
  - **Nguyên tắc**: Tên biến cục bộ (local variable), tham số hàm (parameter) phải ngắn gọn, tự nhiên, đi thẳng vào bản chất dữ liệu (Concise & Direct Naming). Không đặt tên dài dòng, hoa mỹ hoặc ghép nhiều từ diễn giải thừa thãi làm loãng code và khó đọc.
  - **Cấm đặt tên biến dài dòng, thừa thãi**:
    - ❌ CẤM các tiền tố/hậu tố diễn giải thừa: `effectivePageSize`, `resolvedActualItem`, `calculatedTotalAmount`, `currentProcessingRecord`, `temporaryStorageData`...
    - ✅ Dùng thẳng danh từ cốt lõi: `pageSize`, `item`, `total`, `record`, `data`...
  - **Xử lý giá trị fallback / override**:
    - Khi cần tính toán giá trị mặc định (fallback/coalesce), gán trực tiếp hoặc dùng toán tử gọn:
      ```csharp
      // ❌ SAI: Tạo biến mới dài dòng
      var effectivePageSize = pageSize > 0 ? pageSize : DefaultPageSize;

      // ✅ ĐÚNG: Tái sử dụng hoặc gán trực tiếp biến ngắn gọn
      pageSize = pageSize > 0 ? pageSize : DefaultPageSize;
      ```
  - **Lỗi thật đã mắc**: Trong `DataOutboundExtractionProcess.cs`, việc đặt biến `effectivePageSize` gây dài dòng thừa thãi trong khi chỉ cần dùng thẳng `pageSize`.

- **19.17. CẤM Nối Chuỗi Khi Viết SQL — Bắt Buộc Dùng Tham Số Hóa (SugarParameter / SqlSugar) (chốt 24/09/2026)**:
  - **Nguyên tắc cốt lõi (P0 - Security)**: Tuyệt đối KHÔNG dùng toán tử cộng chuỗi (`+`), `string.Format`, hoặc nội suy chuỗi ($"...") để đưa các biến, tham số hay giá trị lọc vào câu lệnh SQL raw (khi gọi `db.Ado.GetDataTableAsync`, `db.Ado.SqlQueryAsync`, `db.Ado.ExecuteCommandAsync`...).
  - **Bắt buộc dùng tham số hóa (Parameterized Query)**:
    - Mọi giá trị động (ID, mã code, mốc thời gian `DateTime`, cursor `LastKey`, giới hạn bản ghi `top`...) BẮT BUỘC phải truyền qua `SugarParameter` của SqlSugar (hoặc `List<SugarParameter>`).
    - Với mệnh đề `TOP` trong T-SQL: bắt buộc dùng cú pháp có ngoặc đơn `TOP (@top)` và truyền tham số, ❌ CẤM viết `$" TOP {top}"`.
      ```csharp
      // ❌ SAI: Nối chuỗi trực tiếp — nguy cơ SQL Injection nghiêm trọng
      var sql = $"SELECT TOP {top} * FROM TrafficData WHERE CreateTime > '{lastTime:yyyy-MM-dd HH:mm:ss}'";
      var list = await db.Ado.GetDataTableAsync(sql);

      // ✅ ĐÚNG: Dùng câu lệnh chứa placeholder @param và truyền SugarParameter
      var sql = "SELECT TOP (@top) * FROM TrafficData WHERE CreateTime > @lastTime ORDER BY CreateTime ASC";
      var parameters = new List<SugarParameter>
      {
          new SugarParameter("@top", top),
          new SugarParameter("@lastTime", lastTime)
      };
      var list = await db.Ado.GetDataTableAsync(sql, parameters.ToArray());
      ```
  - **Ưu tiên SqlSugar Linq / Expression API**: Khi làm việc với các Entity thông thường, ưu tiên sử dụng `db.Queryable<T>()` cùng các phương thức `.Where(...)`, `.WhereIF(...)`, `.Take(...)` để SqlSugar tự động tham số hóa 100%.
  - **Ngoại lệ duy nhất cho ghép chuỗi**: Chỉ cho phép nội suy tên bảng/tên cột nếu và chỉ nếu chúng xuất phát từ catalog tĩnh hoặc whitelist nội bộ đã được kiểm duyệt nghiêm ngặt (như `TableName`, `Columns`), tuyệt đối không chấp nhận bất kỳ input nào từ bên ngoài đưa vào định danh SQL.
  - **Lỗi thật đã mắc**: Trong `ShareDataPacketSqlCatalogUtil.cs`, câu lệnh từng ghép chuỗi: `.Replace(TopToken, top.HasValue ? $" TOP {top.Value}" : string.Empty)`. Đã được chuẩn hóa lại thành tham số hóa `TOP (@top)` kèm `new SugarParameter($"@{TopParameterName}", safeTop)` theo yêu cầu của chủ dự án.

- **19.18. Quy Tắc Kiểm Thử Thực Chất — CẤM Mock NATS & Luồng Business Nội Bộ, Bắt Buộc Test Full Luồng Thật Qua Host & CSDL Local (chốt 24/09/2026)**:
  - **Bản chất vấn đề (Chống Mock mù / False Confidence)**: Việc tự viết class giả lập hời hợt (`TestMockDataOutboundService`, `FakeService`) chỉ để đếm xem hàm có được gọi hay không (`Assert.Single(HandledPacketCodes)`) là **kiểm thử hình thức, tạo ảo giác an toàn**. Khi chạy thực tế, service thật có thể gặp lỗi kết nối DB, lỗi tranh chấp lock OCC, lỗi mapping schema hoặc ném Exception mà bài test mock hoàn toàn không phát hiện được.
  - **Ranh giới rõ ràng giữa thứ ĐƯỢC PHÉP và BỊ CẤM mock**:
    - ✅ **ĐƯỢC PHÉP giả lập duy nhất: máy chủ HTTP bên ngoài — và BẮT BUỘC bằng mock server `HttpListener` THẬT (chốt 30/09/2026)**:
      - Mở `HttpListener` trên `127.0.0.1` với dải cổng riêng của phân hệ (VideoWall: 18080–18083 · ShareData: 18090–18093), rồi trỏ cấu hình/dữ liệu của đối tác hoặc thiết bị vào đó. Tầng gửi của sản phẩm chạy nguyên vẹn qua dây mạng thật.
      - ⛔ **CẤM chặn ở tầng `HttpMessageHandler` giả** (kiểu `MockHttpClientFactoryTest` cũ, đã xoá 30/09/2026). Handler giả bỏ qua bắt tay TCP, xác thực Digest nhiều chặng, BOM trong thân bản tin, timeout và đứt kết nối — tức bỏ qua đúng những chỗ hay hỏng thật.
      - Mock server BẮT BUỘC có: nhật ký request (method, đường dẫn, header, thân dạng byte), đặt phản hồi **theo lượt gọi** để dựng kịch bản hỏng giữa chừng, cắt kết nối để ép lỗi mạng, và `ResetDefaults()` gom **toàn bộ** cờ kịch bản về mặc định.
      - 🔴 Mock server dùng chung cả Collection nên **mọi bài đổi kịch bản BẮT BUỘC gọi `ResetDefaults()` ở đầu phần Arrange**. Thiếu là bài sau ăn kịch bản của bài trước, lỗi phụ thuộc thứ tự chạy, rất khó lần ra.
      - ⚠️ **Ngoại lệ duy nhất được phép ⛔ không đi qua mock server**: bài test kiểm việc **dựng URL** khi đối tác ⛔ không khai cổng (⇒ cổng 80), vì bind cổng 80 cần quyền quản trị. Trường hợp này gọi hàm dựng URI qua Reflection (`BindingFlags.NonPublic`), ⛔ TUYỆT ĐỐI KHÔNG nâng hàm `private` lên `public` để test gọi được. Ví dụ thật: `RestSender_Send_UrlWithAndWithoutPort_ConstructsCorrectUri_Test`.
      - ⚠️ Khi kiểm lỗi mạng, gửi tới cổng ⛔ không ai lắng nghe và chỉ assert *"thất bại kèm thông điệp"*. ⛔ CẤM khoá cứng chuỗi thông điệp lỗi (`"Connection refused"`) — văn bản đó do .NET và hệ điều hành sinh, khác nhau giữa Windows và Linux.
    - ⛔ **TUYỆT ĐỐI CẤM Mock các thành phần nội bộ**:
      - ❌ CẤM mock NATS / Message Bus nội bộ (`TransportManager`, pub/sub sự kiện trigger giữa các worker trong hệ thống).
      - ❌ CẤM mock toàn bộ luồng Business nội bộ (`IDataOutboundService`, `IDataInboundService`, các Process trích xuất/mapping, Worker).
      - ❌ CẤM mock Database SqlSugar (bắt buộc tương tác CSDL test local `127.0.0.1`).
  - **BẮT BUỘC Test Full Luồng Thật (End-to-End Integration)**:
    - ✅ Lấy Service thật từ DI container: `_host.Services.GetRequiredService<T>()` hoặc `scope.ServiceProvider.GetRequiredService<T>()`.
    - ✅ Truyền Service thật vào Worker: Khởi tạo Worker với Service thật, bắn input (JSON payload, event) và để Worker gọi thẳng vào luồng xử lý lõi của hệ thống.
    - ✅ Kiểm chứng bằng sự biến đổi dữ liệu trên CSDL test local `127.0.0.1`:
      - Subscription: `NextTimeRun`, `LastTimeRun`, `State` có cập nhật đúng không?
      - Activity/Alert Log: Có ghi nhận thành công hoặc ghi đúng mã cảnh báo không?
      - Checkpoint: `LastTime`, `LastKey` có tăng theo chiều monotonic không?
    - Dữ liệu rác sinh ra trong test BẮT BUỘC phải dọn dẹp sạch sẽ trong khối `finally`.
  - **Lỗi thật đã mắc**: Trong `DataNatsWorkerTests.cs`, từng tạo `TestMockDataOutboundService` để kiểm tra `DataNatsWorker.HandleTrigger`. Chủ dự án đã chỉ rõ: test như vậy không kiểm chứng được luồng thật và yêu cầu bãi bỏ hoàn toàn thói quen mock NATS và luồng business nội bộ.

- **19.19. CẤM Viết Try-Catch Dồn Dập / Chồng Chéo Lãng Phí — No Redundant / Nested Try-Catch Stacking (chốt 24/09/2026)**:
  - **Nguyên tắc**: Chỉ dùng `try-catch` tại **ranh giới tác vụ (Unit of Work / Transaction boundary)**: nơi cô lập lỗi, ghi alert, dọn dẹp tài nguyên (rollback / release lock). Mọi lỗi bên trong tác vụ phải **bubble lên** boundary duy nhất đó.
  - ⛔ **CẤM pattern**: `try { someProcess.Do(...); } catch (Exception) { WriteAlertAsync(...); throw; }` khi caller ngoài đã có catch tổng — gây **duplicate alert/log** (alert ghi 2 lần cho 1 lỗi).
  - ✅ **Đúng**: Throw thẳng lên boundary — boundary catch và ghi alert duy nhất 1 lần:
    ```csharp
    catch (ShareDataException ex) { WriteAlertAsync(ex.AlertCode, ex.Severity, ex.AlertSource, ...); }
    catch (Exception ex)          { WriteAlertAsync(QueryFailed, Error, Subscription, ...); }
    ```
  - **Cách kiểm tra nhanh**: Nếu `catch` chỉ ghi log/alert rồi `throw` mà **không có cleanup tài nguyên** nào khác (không rollback transaction, không release lock, không dispose) → đó là try-catch thừa, gỡ đi.
  - **Try-catch HỢP LỆ vẫn giữ**: Transaction boundary (`BeginTran / RollbackTran`), race condition OCC (`catch { reload; throw }`), intentional swallow (`catch { /* optional feature */ }`), log-write defensive (`catch (Exception logEx) { LogWarning }` sau committed data).
- **19.20. BẮT BUỘC ƯU TIÊN ORM (SqlSugar API) Khi Đã Có Entity — TUYỆT ĐỐI CẤM Dùng Raw SQL DML (UPDATE / INSERT / DELETE) Thủ Công (chốt 25/09/2026)**:
  - **Nguyên tắc cốt lõi (P0 - Code Convention & Type Safety)**: Khi thao tác với bất kỳ bảng CSDL nào đã có Entity class tương ứng kế thừa trong codebase (ví dụ: `TmsTrafficData`, `ShareDataSubscription`, `ShareDataPacket`, `ShareDataPartner`, `TmsEquipment`...), BẮT BUỘC sử dụng cú pháp SqlSugar ORM (`db.Updateable<T>()`, `db.Insertable<T>()`, `db.Deleteable<T>()`, `db.Queryable<T>()`) thông qua Expression Tree (`SetColumns(x => ...)`, `Where(x => ...)`).
  - ⛔ **TUYỆT ĐỐI CẤM**:
    - Dùng raw SQL DML `db.Ado.ExecuteCommandAsync("UPDATE [TableName] SET ... WHERE ...")`.
    - Dùng raw SQL DML `db.Ado.ExecuteCommandAsync("INSERT INTO [TableName] ...")`.
    - Dùng raw SQL DML `db.Ado.ExecuteCommandAsync("DELETE FROM [TableName] WHERE ...")`.
    - Tự gõ chuỗi tên bảng hoặc tên cột dạng string trong DML khi đã có class Entity được khai báo.
  - **Mục tiêu & Lợi ích**:
    1. **Compile-time Type Safety**: Phát hiện lỗi ngay khi biên dịch nếu tên cột hoặc kiểu dữ liệu bị thay đổi, tránh lỗi runtime tiềm ẩn.
    2. **Refactoring an toàn**: Tự động đồng bộ khi đổi tên thuộc tính (Rename Symbol) trong toàn bộ solution.
    3. **Chống SQL Injection tự động**: SqlSugar ORM tự động sinh parameterized query, loại trừ hoàn toàn rủi ro bảo mật.
    4. **Đồng nhất kiến trúc**: Dự án vận hành theo chuẩn Code-First / Entity-first của SqlSugar.
  - **Ngoại lệ hợp lệ duy nhất cho `db.Ado`**:
    - Truy vấn các bảng/hàm hệ thống đặc thù của CSDL (như `CHANGETABLE(CHANGES ...)` trong Change Tracking).
    - Các bảng động (Dynamic table) không có Entity tĩnh tại compile-time.
    - Đọc metadata danh mục hệ thống cấp thấp khi không thể ánh xạ POCO.
  - **Lỗi thật đã mắc**: Trong `DataChangeWorkerTests.cs`, từng viết `await db.Ado.ExecuteCommandAsync("UPDATE TmsTrafficData SET CreateTime = @timeA... WHERE ID = @id", new { timeA, id = carA.ID });` trong khi bảng `TmsTrafficData` đã kế thừa Entity chuẩn. Chủ dự án đã chỉ rõ lỗi này và yêu cầu bổ sung rule nghiêm cấm vĩnh viễn.

- **19.21. Báo Cáo Viết HƯỚNG VỀ NGƯỜI ĐỌC, Không Hướng Về Người Viết (chốt 27/09/2026, bỏ khuôn cứng 28/09/2026)**:
  - **Phạm vi áp dụng**: mọi báo cáo, biên bản rà soát, tổng kết trong `DocBusinessThienAn/`, và mọi phần báo cáo kết quả trả lời trực tiếp cho người dùng.
  - 🔴 **PHÉP THỬ DUY NHẤT QUYẾT ĐỊNH BÁO CÁO ĐẠT HAY KHÔNG**: *người đọc mở tệp ra, đọc xong rồi có phải quay lại hỏi AI nữa không?* **Phải quay lại hỏi = báo cáo KHÔNG ĐẠT**, bất kể nó đầy đủ và chính xác tới đâu. Đây là phép thử nghiêm hơn phép thử "1 phút trả lời được 4 câu" của mục 19.14, và **thay thế** nó khi hai phép thử cho kết quả khác nhau.
  - 🔴 **NGUYÊN TẮC DUY NHẤT VỀ HÌNH THỨC — ⛔ KHÔNG CÓ KHUÔN BẮT BUỘC (chốt 28/09/2026, nguyên văn chủ dự án):**

    > **Đưa thông tin sao người đọc hiểu liền là được: ngắn gọn, súc tích, dễ hiểu, đủ ý.**

    - **Mở đầu nói ngay kết quả** — ⛔ không bắt người đọc lội xuống cuối mới biết xong hay chưa xong, phải làm gì.
    - **Hình thức tự chọn cho hợp nội dung**: đoạn văn, bảng, hay gạch đầu dòng đều được.
    - ⛔ **Không** có tiêu đề bắt buộc, ⛔ **không** có số phần bắt buộc, ⛔ **không** đếm dòng, ⛔ **không** bắt buộc cột cố định nào.
    - 📌 **Đã bãi bỏ 28/09/2026** (khuôn cũ quá cứng, áp cho mọi loại báo cáo là gượng): tiêu đề bắt buộc `## Kết luận — đọc 30 giây là đủ` · cấu trúc đúng 3 phần · cột bắt buộc *"Vỡ khi nào"* · giới hạn ~15 dòng · thứ tự khối ép cứng. ⛔ Không khôi phục lại.
  - 🔴 **Ô "Trả lời" của bảng `Tóm tắt` phải CHỨA câu trả lời, không phải mô tả rằng có câu trả lời.**
    - ❌ SAI: *"4 quyết định cốt lõi chốt tại họp 21/09"* — không nói **4 quyết định đó là gì**, đọc xong vẫn phải mở mục chi tiết.
    - ✅ ĐÚNG: *"Bốn việc: chặn gửi khi thiếu hồ sơ ánh xạ · gửi nối đuôi theo mốc `LastTime`/`LastKey` · phát hiện dữ liệu mới tức thì · trường không map để `null`"*.
    - **Dấu hiệu nhận biết viết sai**: ô đó chứa **số lượng** ("4 quyết định", "5 việc", "13 điểm") mà không chứa **nội dung**.
  - ⛔ **CẤM giải thích hệ ký hiệu / cách phân loại của chính mình trong thân báo cáo.** Khối `Chú giải ký hiệu` ở đầu tệp và tiêu đề cột đã làm việc đó (mục 19.11). Viết thêm những đoạn kiểu *"Vì sao mục này là ❌ chứ không phải ⚠️..."*, hay *"Cột X là trục đánh giá của bảng này: ✅ nghĩa là..."* là **tự biện luận cho cách làm của mình** — người đọc không cần, và nó làm loãng đúng chỗ cần gọn.
  - ⛔ **CẤM viết nội dung chỉ có nghĩa với người đi kiểm tra AI.** Cụ thể: biểu thức mã nguồn thô (`NextTimeRun == nextRunDeadline`), chuỗi tên biến nội bộ, đường dẫn tra cứu — những thứ chứng minh "tôi đã rà ở đâu". Người đọc cần biết **xong chưa và phải làm gì**, ⛔ không cần biết AI tra ở đâu. Căn cứ mã nguồn vẫn ghi, nhưng dồn xuống mục chi tiết, ⛔ không để ở phần đầu. *(Trước 28/09/2026 câu này ghi "hoặc phụ lục" — nay phụ lục đã bị cấm, xem 19.14.)*
  - **Dấu hiệu nhận biết đã viết sai (tự soát trước khi giao)**:
    - Tệp phình quá ~10 KB mà phần đầu vẫn chưa trả lời được "phải làm gì".
    - Ô bảng dài 3 câu trở lên với nhiều mệnh đề kẹp bởi dấu gạch ngang.
    - Có đoạn văn nói **về** cách trình bày thay vì nói **về** nội dung nghiệp vụ.
  - **Lỗi thật đã mắc**: `Sharedata_Review_TongThe_20260927.md` bản đầu dài 26 KB, đủ 4 trục theo mục 19.14 và chính xác về nội dung, nhưng chủ dự án đọc xong **vẫn phải quay lại hỏi** vì: ô `Tóm tắt` chỉ ghi số lượng không ghi nội dung; mục 2 đặc biểu thức mã nguồn; có đoạn AI tự biện luận cách phân loại ký hiệu của mình. Chủ dự án phản hồi nguyên văn: *"báo cáo tôi đọc còn phải hỏi lại bạn nghĩa là báo cáo đó tôi không hiểu"*. ⚠️ Lần đầu AI chỉ vá triệu chứng (thêm một cột ký hiệu) — sai, vì nguyên nhân gốc là **viết hướng về mình thay vì hướng về người đọc**.

- **19.22. Quy Chuẩn Khai Báo Property — Dùng Auto-Property { get; } / { get; set; }, CẤM Tách Backing Field Riêng và CẤM Dùng `private readonly` Field Cho State Nội Bộ (chốt 27/09/2026 · mở rộng 02/10/2026)**:
  - **Nguyên tắc**: Khi khai báo property trong class C# (Service, Worker, Controller, Entity, DTO...), BẮT BUỘC sử dụng Auto-Property thuần (`{ get; }`, `{ get; init; }`, hoặc `{ get; set; }`) được khởi tạo giá trị trực tiếp hoặc thông qua Constructor.
  - ⛔ **CẤM tuyệt đối các phản mẫu tách backing field riêng (Backing Field Anti-patterns)**:
    - ❌ **Cấm tách backing field lazy `??=`**: Không viết `public IReadOnlyList<string> TrackedTables => _trackedTables ??= ...; private IReadOnlyList<string>? _trackedTables;`.
    - ❌ **Cấm tách backing field wrapper collection**: Không viết `public IReadOnlyCollection<string> MissingTables => _missingTables; private readonly HashSet<string> _missingTables = new(...);`.
    - Cách viết này làm rác class (sinh thêm nhiều field `private` thừa thãi, rườm rà), khó đọc, khó debug và vi phạm tính đồng nhất của codebase.
  - ⛔ **CẤM dùng `private readonly` field đứng một mình cho state nội bộ — BẮT BUỘC dùng private auto-property (mở rộng 02/10/2026)**:
    - ❌ **Phản mẫu**: `private readonly Dictionary<string, DateTime> _missingTables = new(StringComparer.OrdinalIgnoreCase);`
    - ✅ **Chuẩn**: `private Dictionary<string, DateTime> MissingTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);`
    - **Lý do**: property thống nhất cú pháp với toàn bộ codebase, dễ thêm getter/setter logic sau này mà không phá hợp đồng, và loại bỏ hoàn toàn field `private` đứng lẻ khỏi class.
    - **Ngoại lệ duy nhất** cho `private readonly field`: các **phụ thuộc DI** được inject qua constructor tường minh (`private readonly ILogger _logger;`, `private readonly IServiceScopeFactory _factory;`) — đây là quy ước framework, giữ nguyên. Xem phân biệt casing ở mục 7.
  - ✅ **Cách viết chuẩn**:
    ```csharp
    // ✅ ĐÚNG: Auto-property { get; } — readonly, khởi tạo 1 lần
    public IReadOnlyDictionary<string, IReadOnlyList<string>> TablePacketMap { get; } = LoadTablePacketMap(Configuration);
    public IReadOnlyList<string> TrackedTables { get; } = [.. LoadTablePacketMap(Configuration).Keys];

    // ✅ ĐÚNG: state nội bộ mutable — private auto-property { get; set; }, KHÔNG phải private readonly field
    private Dictionary<string, DateTime> MissingTables { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    private DateTime? NextTableRetryTime { get; set; }
    private string ActiveChangesSql { get; set; } = BuildChangesSql([..]);

    // ✅ ĐÚNG ngoại lệ DI: phụ thuộc inject qua constructor tường minh → giữ private readonly field
    private readonly ILogger<DataChangeTrackingService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    ```


- **19.23. Prompt Sinh Ra Từ Tài Liệu Thì BẮT BUỘC Có Mục Cuối "Cập Nhật Lại Tài Liệu Gốc" (chốt 28/09/2026)**:
  - **Phạm vi áp dụng**: mọi tệp prompt trong `Prompt/` mà nội dung được rút ra từ một tài liệu — báo cáo rà soát (`Plan/*_Review_*.md`), MasterPlan, đặc tả (`doc/*.md`), biên bản họp (`doc/transcript/*.md`), hoặc `README.md` của khu vực code (`tests/README.MD`...).
  - 🔴 **Vì sao cần, và vì sao mục 19.2 KHÔNG bịt được lỗ này**: 19.2 quy định *"khi hoàn tất 1 prompt... BẮT BUỘC cập nhật lại MasterPlan"* — nghĩa vụ đó đặt lên **AI sau khi xong prompt**. Nhưng theo mục 10.5, AI **chỉ viết prompt** và **KHÔNG tự thực thi** — chỉ thực thi khi chủ dự án yêu cầu trực tiếp. Nghĩa là không được giả định AI sẽ là người áp prompt và sẽ nhớ cập nhật tài liệu: prompt có thể do **người khác** áp, hoặc áp ở **phiên khác** khi AI không còn ngữ cảnh của lúc viết. Nghĩa vụ đặt lên một người có thể không tham gia bước đó thì rốt cuộc không ai làm ⇒ tài liệu lạc hậu dần so với code, mất khả năng theo dõi. Vì vậy nghĩa vụ phải được viết **VÀO TRONG chính prompt** như một việc có số, TUYỆT ĐỐI KHÔNG để ngầm.
  - **BẮT BUỘC**: mục **CUỐI CÙNG** của prompt đặt tên thống nhất `## Việc cuối — Cập nhật lại tài liệu gốc` (đặt sau cả mục `## Kiểm chứng`), và phải nêu đủ **3 thứ**:
    1. **Đường dẫn chính xác** của tệp tài liệu cần cập nhật.
    2. **Đúng mục / tiêu đề** nào trong tệp đó cần sửa.
    3. **Nội dung mới cần ghi là gì** — để người thi công dán vào được, KHÔNG phải tự suy diễn lại nghiệp vụ.
  - ✅ **Hai mẫu đều hợp lệ, chọn theo quy mô đợt**:
    1. **Nhúng trong từng prompt** — dùng khi prompt đứng một mình. Mục cuối nằm ngay trong tệp prompt đó.
    2. **Một prompt đồng bộ tài liệu riêng cho cả đợt** — dùng khi một đợt có nhiều prompt cùng sửa vào một vùng
       và cùng ảnh hưởng một bộ tài liệu (ví dụ đợt 28/09/2026: 3 prompt mã nguồn + 1 prompt
       `sharedata-dong-bo-tai-lieu-sau-dot-sua-2809-prompt.md`). Gom lại tránh phải ghi lặp cùng một nội dung
       vào 3 tệp, và tránh việc tài liệu bị sửa dở khi mới áp 1 trong 3 prompt.
    - 🔴 **Điều kiện bắt buộc của mẫu 2**: prompt đồng bộ phải là prompt **áp CUỐI CÙNG** của đợt, phải tự ghi rõ
      *"điều kiện tiên quyết: các prompt kia đã áp xong và test đã xanh"*, và **thứ tự áp của cả đợt BẮT BUỘC được
      ghi ở `Prompt/README.md`**. Thiếu ghi thứ tự ở `README.md` thì mẫu 2 **không hợp lệ** — vì lúc đó ⛔ không có
      chỗ nào cho người thi công biết còn một bước đồng bộ đang chờ.
  - ⛔ **CẤM viết chung chung**: các câu kiểu *"nhớ cập nhật tài liệu liên quan"*, *"cập nhật tài liệu nếu cần"*, *"đồng bộ lại plan"* — không nêu tệp nào, mục nào, ghi gì thì KHÔNG thi hành được, và chắc chắn bị bỏ qua.
  - ⛔ **CẤM coi dòng lịch sử trong `Prompt/README.md` là đủ**: `README.md` ghi *prompt đã chạy và kết quả ra sao*; tài liệu gốc ghi *hệ thống bây giờ là gì*. Hai việc khác nhau, KHÔNG thay nhau được. Vẫn phải ghi cả hai.
  - 🔴 **CHỈ nêu tài liệu THẬT SỰ lạc hậu, CẤM liệt kê cho đủ bộ**: trước khi viết mục này, BẮT BUỘC rà xem tài liệu nào **thật sự** sai sau khi áp prompt. 📌 Ví dụ thật ngày 28/09/2026: prompt tinh gọn `DataTrackerWorker` xoá thuộc tính `ChangesSql` và chuyển sang constructor tường minh, nhưng `Sharedata_MasterPlan.md` §6b tả thiết kế bằng **văn xuôi** (`LastVersion`, nâng mốc nguyên tử, trạng thái cô lập bảng ở RAM) và KHÔNG nêu hai thứ đó ⇒ §6b **không** lạc hậu ⇒ **KHÔNG** đưa MasterPlan vào mục cập nhật. Nêu vào là bịa việc cho người thi công.
  - **Khi prompt thật sự không sinh ra từ tài liệu nào** (ví dụ lỗi phát hiện trực tiếp khi đọc mã nguồn): ghi **một dòng tường minh** ở cuối prompt — *"📌 Prompt này không sinh ra từ tài liệu nào (phát hiện trực tiếp từ đọc mã nguồn) ⇒ ⛔ không có mục cập nhật tài liệu gốc."* Nhờ dòng đó, sự vắng mặt là một **quyết định**, KHÔNG phải sơ suất.
  - **Dấu hiệu nhận biết đã viết sai**: prompt có trích dẫn hoặc dẫn chiếu một tệp `.md` ở phần bối cảnh, mà cuối tệp lại KHÔNG có mục cập nhật đúng tệp đó.
  - **Phép thử bắt buộc trước khi giao prompt**: đọc mục cuối và tự hỏi *"người thi công đọc đến đây có làm được ngay mà không phải hỏi lại không?"* — nếu còn phải hỏi *"sửa mục nào"* hay *"ghi nội dung gì"* thì mục đó **chưa đạt**.
  - 📌 **Quan hệ với 19.2**: mục này **mở rộng** 19.2 chứ không thay. 19.2 nói *cập nhật cái gì khi xong việc*; 19.23 nói *nghĩa vụ đó phải được viết vào đâu để người khác thi hành được*.

- **19.24. Phân Vai MasterPlan vs Báo Cáo Rà Soát — CHỈ MasterPlan Là Sổ Theo Dõi Task (chốt 28/09/2026)**:
  - **Phạm vi áp dụng**: thư mục `Plan/` của mọi phân hệ trong `DocBusinessThienAn/`.
  - **Đúng hai vai, KHÔNG có vai thứ ba**:

    | Tệp | Vai | Dấu hiệu nhận biết |
    | --- | --- | --- |
    | `<Xx>_MasterPlan*.md` | 🔴 **Sổ theo dõi task — nơi DUY NHẤT tra trạng thái** | Tên **không có ngày** ⇒ sống mãi. Bên trong có checklist trạng thái từng mục (`SV-*`, `BE-*`, FE) gắn ✅ / ⚠️ |
    | `<Xx>_Review_*_<ngày>.md` | **Ảnh chụp một lượt rà soát** để ra quyết định tại thời điểm đó | Tên **có ngày** ⇒ tự nó tuyên bố là bản chụp, sẽ hết hạn |

  - 🔴 **BẮT BUỘC: khi một lượt rà soát đóng lại, gộp TRỌN về MasterPlan rồi XOÁ tệp báo cáo.**
    1. **Gộp mọi thứ còn giá trị lâu dài** vào đúng mục sẵn có của MasterPlan, TUYỆT ĐỐI KHÔNG tạo mục mới ở cuối tệp. 📌 Từ 28/09/2026 báo cáo ⛔ **không còn phụ lục** (xem 19.14): quyết định đã chốt + phương án bị bác nay được ghi **thẳng** vào mục `Quyết định đã chốt & phương án bị bác` của MasterPlan ngay lúc chốt, nên bước gộp này nhẹ đi — chủ yếu còn **tình huống biên** và **bẫy vận hành** rút ra được từ lượt rà soát.
    2. **Rà mọi chỗ đang trỏ tới tệp sắp xoá** (trong chính MasterPlan và các tài liệu khác) và sửa lại, kẻo thành liên kết chết.
    3. **Xoá tệp báo cáo.** Để nó lại sau khi đã gộp là **cố tình duy trì hai nguồn nói về cùng một trạng thái** — đúng cái lỗi mục này sinh ra để chặn. 📌 Tệp nằm trong git nên vẫn lấy lại được, rủi ro thấp. Tiền lệ: 25/09/2026 gộp-rồi-xoá 3 báo cáo, 28/09/2026 gộp-rồi-xoá `Sharedata_Review_TongThe_20260927.md`.
    - ⛔ **AI KHÔNG tự xoá** tệp trong `Plan/` (mục 13). Bước 3 chỉ làm khi **chủ dự án chỉ định**; AI làm xong bước 1 và 2 rồi báo lại là đã sẵn sàng xoá.
  - **BẮT BUỘC có dòng phân vai ở ĐẦU tệp**: MasterPlan ghi *"đây là sổ theo dõi duy nhất"*. Báo cáo rà soát **trong lúc còn tồn tại** (chưa gộp xong) ghi *"đây là ảnh chụp, ⛔ không tra trạng thái ở đây, xem MasterPlan"*. Thiếu dòng này thì người đọc không có cách nào biết tệp nào là nguồn.
  - ⛔ **CẤM để trạng thái task chỉ tồn tại trong báo cáo rà soát.** Đây là lỗi nguy hiểm nhất của cặp tài liệu này: báo cáo có ngày **mới hơn** MasterPlan thì người đọc sẽ tin báo cáo, mà báo cáo lại là thứ sắp hết hạn ⇒ vài tuần sau không còn ai biết trạng thái thật ở đâu.
  - 🔴 **Hệ quả nghiêm trọng nhất — task có thể BIẾN MẤT khỏi tầm theo dõi**: một việc được biên bản họp giao, nếu chỉ được ghi vào báo cáo rà soát mà ⛔ không mở thành dòng checklist trong MasterPlan, thì nó **vô hình với mọi người tra checklist**. *(Lỗi thật đã mắc: việc *"API danh mục trả về trạng thái ánh xạ của từng gói tin theo đối tác"* do họp 21/09/2026 giao cho Đạt chỉ nằm ở mục 3 của `Sharedata_Review_TongThe_20260927.md`, KHÔNG có dòng nào trong checklist `BE-*` của MasterPlan. Phát hiện ngày 28/09 khi rà 33 code change, đã mở thành `BE-14`.)*
  - **Phép thử bắt buộc**: mở **một** tệp MasterPlan, có trả lời được *"task nào xong, task nào chưa"* mà KHÔNG phải mở tệp thứ hai không? Phải mở tệp thứ hai ⇒ chưa hợp nhất xong.
  - 📌 **Quan hệ với 19.14 và 19.23**: 19.14 quy định *khung* của báo cáo rà soát; 19.23 quy định nghĩa vụ cập nhật tài liệu phải nằm trong prompt; 19.24 quy định *tài liệu nào mới là nguồn sự thật về trạng thái*. Ba mục bổ sung nhau, KHÔNG xung đột.

- **19.25. Phạm Vi Khai Báo Hằng Số & Biến — Chỉ Khai Báo Cấp Class Khi Dùng Nhiều Chỗ, CÒN LẠI BẮT BUỘC DÙNG BIẾN CỤC BỘ (chốt 28/09/2026)**:
  - **Nguyên tắc cốt lõi (Scope Minimization & Clean Code)**: Biến hoặc hằng số phải được khai báo ở phạm vi hẹp nhất có thể (Narrowest Scope). Một hằng số (`const`) hoặc trường (`readonly field`) **CHỈ ĐƯỢC PHÉP** khai báo ở cấp lớp (class-level `private const` / `private readonly`) khi nó được **dùng chung ở từ 2 phương thức trở lên** trong cùng một class hoặc là cấu hình công khai/phơi ra cho bên ngoài.
  - ⛔ **CẤM tuyệt đối khai báo hằng số cấp lớp nếu chỉ dùng ở đúng 1 hàm duy nhất**:
    - Không khai báo các hằng số như `private const int DefaultPageSize = 100;`, `private const int DefaultMaxPagesPerRun = 20;`, `private const int DefaultLockBudgetPercent = 50;` ở đầu class kèm các khối XML doc summary rườm rà nếu chúng chỉ phục vụ cho đúng một vòng lặp hoặc một phương thức nội bộ duy nhất.
    - Việc đưa hằng số dùng một chỗ lên cấp lớp làm rác đầu file, bắt người đọc phải cuộn lên cuộn xuống để tra cứu giá trị và sinh ra gánh nặng viết XML Doc summary thừa thãi (vi phạm quy tắc 5.7).
  - ✅ **Cách viết chuẩn**:
    - Khai báo hằng số hoặc biến cục bộ (`const int pageSize = 100;`, `const int maxPages = 20;`, `const int lockBudgetPercent = 50;`) **ngay bên trong phương thức sử dụng nó**.
    - Code tự nhiên, tự chứa (self-contained), khép kín phạm vi, dễ đọc hiểu từ trên xuống dưới mà không cần XML doc rườm rà.

- **19.26. Quy Tắc Dịch Thuật (Localization Policy) — BE Tuyệt Đối CẤM Đụng File Resources, Bắt Buộc Lưu DB (Seed SQL/API); FE Dùng i18n JSON Bình Thường (cập nhật 01/10/2026)**:
  - 🛑 **Backend WebAPI (CẤM ĐỤNG FILE RESOURCE - P0)**:
    - **TUYỆT ĐỐI KHÔNG sửa hoặc thêm mới key** vào các file resource của Backend tại `TA-ITS015-WEBAPI-V1.0\src\TAC_WebAPI\Resources` (`vi-VN.json`, `en-US.json`).
    - Khi Backend cần áp dụng dịch thuật (các key nghiệp vụ `lz.exception.*`, `lz.validation.*`, `lz.message.*`, `lz.entity.*`...): **BẮT BUỘC lưu trên Cơ sở dữ liệu (`SysTerminology`)**.
    - **Hai phương thức thực hiện cho Backend**:
      1. 💾 **Cách 1 (Seed / SQL Script - Ưu tiên)**: Xuất file script SQL idempotent (`IF NOT EXISTS ... INSERT ELSE UPDATE`) vào thư mục `sql/` của dự án (ví dụ: `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/`) thao tác trực tiếp trên bảng `SysTerminology` để DBA / Admin chạy cập nhật CSDL.
      2. 🔌 **Cách 2 (Gọi API quản trị thuật ngữ)**: Gọi API thêm / cập nhật thuật ngữ thông qua `SysTerminologyController` (ví dụ `AddSysTerminology`, `UpdateSysTerminology`) để lưu trực tiếp vào CSDL.
  - 🌐 **Frontend (i18n JSON Bình Thường)**:
    - Phía Frontend (`TA-ITS015-WEBVUE-V1.0`) **vẫn sử dụng bình thường hệ thống từ điển JSON của `vue-i18n`**: `src/src/i18n/lang/vi-vn.json` và `en-us.json` cho toàn bộ nhãn giao diện, nút bấm, placeholder, tooltip (`lz.label.*`, `lz.placeholder.*`, `lz.button.*`, `lz.tooltip.*`...) và các thông báo phía client.
  - 📌 **Cơ chế ghi đè**: Hệ thống nạp terminology từ DB (`SysTerminology`) trước, đảm bảo dữ liệu dịch thuật trên DB luôn có hiệu lực cao nhất và đồng bộ động mà không cần rebuild / deploy lại file resource tĩnh của Backend.
  - **Thuật ngữ trong chuỗi hiển thị phải khớp giao diện**: Ví dụ XML doc gọi `ShareDataMapping` là *"phễu lọc"* nhưng giao diện gọi *"Ánh xạ dữ liệu"* / *"hồ sơ ánh xạ"* ⇒ chuỗi dịch dùng **"hồ sơ ánh xạ"**. ⛔ Chỉ đổi chuỗi hiển thị, ⛔ không sửa XML doc, ⛔ không đổi tên biến.

- **19.27. CẤM Tự Ý Sửa/Bỏ Nội Dung & Thuộc Tính UI Khi Fix Giao Diện/Responsive — BẮT BUỘC Chỉ Sửa Bằng CSS, Khó Phải Hỏi (chốt 01/10/2026)**:
  - **Nguyên tắc bất biến**: Khi người dùng yêu cầu sửa lỗi giao diện, căn chỉnh layout, chống tràn viền hoặc tối ưu responsive, AI **TUYỆT ĐỐI KHÔNG tự ý xóa bỏ, rút gọn hoặc thay đổi nội dung, thuộc tính nghiệp vụ, component props của template HTML/Vue** (ví dụ: `show-word-limit`, `:maxlength`, nhãn form `:label`, placeholder, icon, thẻ button, slot...).
  - 🔴 **BẮT BUỘC giải quyết 100% bằng CSS**: Mọi tinh chỉnh về co giãn, ẩn/hiện, padding, margin, wrapping, flexbox, grid, font-size... **BẮT BUỘC PHẢI THỰC HIỆN TRONG KHỐI CSS / SCSS** (`<style lang="scss" scoped>`).
  - 🛑 **Socratic Gate khi gặp ca khó / hạn chế không gian**: Nếu không gian quá chật hẹp hoặc gặp rào cản kỹ thuật không thể xử lý thuần CSS mà bắt buộc phải lược bỏ/rút gọn nội dung UI:
    - **CẤM tự ý xóa code trước rồi mới báo cáo sau.**
    - **BẮT BUỘC DỪNG LẠI VÀ HỎI TRỰC TIẾP NGƯỜI DÙNG**: Nêu rõ lý do kỹ thuật và xin phép rõ ràng: *"Đoạn này không gian quá hẹp, em có được phép lược bỏ thuộc tính/nội dung [X] không?"*. Chỉ được can thiệp template khi người dùng đồng ý bằng văn bản.

- **19.28. Liệt Kê Theo Số Thì BẮT BUỘC Sắp Tăng Dần (chốt 02/10/2026)**:
  - **Phạm vi**: mọi danh sách, bảng, nhóm gạch đầu dòng trong `DocBusinessThienAn/` và `.agents/` mà phần tử được định danh bằng **số** — mã issue, số mục, mã lỗi, số thứ tự bước.
  - 🔴 **BẮT BUỘC sắp từ nhỏ tới lớn.** ⛔ Không xếp theo thứ tự tiện tay của người viết: thứ tự tìm ra, thứ tự quan trọng, hay thứ tự gom theo cùng nguyên nhân.
  - **Lý do**: người đọc tra tài liệu bằng cách **dò số của mình**. Danh sách không tăng dần buộc họ phải quét toàn bộ mới chắc là không bỏ sót — và họ ⛔ không có cách nào đoán được thứ tự kia dựa trên tiêu chí gì.
  - **Lỗi thật đã mắc**: `sharedata-ke-hoach-dot-f16-20261001.md` mục `N5` liệt kê issue `9 · 23 · 24 · 26 · 17` (mục 17 bị đẩy xuống cuối chỉ vì tìm ra sau), và mục nhóm C liệt kê `12 · 3 · 14+16`. Chủ dự án phản hồi: *"nếu báo cáo số thì nên đi từ nhỏ tới lớn"*.
  - **Ngoại lệ duy nhất**: khi thứ tự **chính là nội dung** — các bước phải làm theo trình tự (quy trình thi công, thứ tự áp prompt). Lúc đó đánh số lại từ 1 theo đúng trình tự thực hiện, ⛔ không giữ số gốc rồi xếp lộn xộn.

- **19.29. Tách Biệt Nghiêm Ngặt Giữa Sửa Logic và Sửa UI (chốt 01/10/2026 - P0)**:
  - **Phạm vi**: áp dụng cho mọi tác vụ sửa lỗi logic, validation, chặn giá trị (clamp, max, min), xử lý dữ liệu, API trên toàn bộ dự án.
  - 🔴 **BẮT BUỘC chỉ sửa tầng Logic / Script**: Khi người dùng yêu cầu sửa logic hoặc hành vi nghiệp vụ, AI **CHỈ ĐƯỢC PHÉP can thiệp vào tầng logic / script / event handlers / guards**.
  - ⛔ **CẤM tự ý đổi Component hoặc Layout UI**: TUYỆT ĐỐI KHÔNG tự ý thay đổi component UI, thay đổi layout, đổi sang component khác (ví dụ: đang dùng component có styling/controls chuẩn như `el-input-number` lại tự ý hạ cấp sang `el-input` thường chỉ để né xử lý phức tạp).
  - 🛑 **Chỉ sửa UI khi có yêu cầu trực tiếp**: Mọi can thiệp vào giao diện, component, nút bấm, layout chỉ được thực hiện khi người dùng yêu cầu rõ ràng *"sửa UI"*, *"đổi giao diện"*, *"chỉnh hiển thị"*. Luôn bảo toàn 100% UI/UX gốc hiện hữu khi giải quyết bài toán logic.
  - **Lỗi thật đã mắc**: Sửa chặn max chu kỳ 86400, thay vì xử lý guard/event trên component gốc `el-input-number`, AI đã tự ý đổi sang `el-input` thuần làm mất cụm mũi tên controls và lệch căn chỉnh của người dùng.

- **19.30. Quy Chuẩn Tài Liệu Kiểm Thử F16 & Bảng Phản Hồi Sheet Bug (chốt 02/10/2026 - P0)**:
  - **Phạm vi áp dụng**: Mọi tài liệu chuyển thể kịch bản kiểm thử, nhật ký lỗi F16 (`F16-nhat-ky-loi-issue-*.md`) trong `DocBusinessThienAn/`.
  - 🔴 **Cấu trúc tài liệu chuẩn (BẮT BUỘC tuân thủ)**:
    1. **Khối đầu trang (Tổng quan đưa lên đầu)**:
       - Bảng `Tình trạng xử lý` & `Chú giải ký hiệu`.
       - Khối `Tổng quan issue` (phân bố theo màn hình, nhóm, phân loại, ưu tiên, phụ trách).
       - Khối `Thứ ảnh chụp màn hình nói ra mà phần chữ không nói` (bảng mã lỗi thật đọc từ ảnh chụp, bẫy lỗi 2 toast, lỗi bộ lọc thời gian...).
       - 📌 Toàn bộ khối tổng quan và giải mã ảnh BẮT BUỘC đưa lên ĐẦU, đứng ngay trước bảng chính để người đọc nắm trọn bối cảnh trong 30 giây.
    2. **Bảng phân loại & Phản hồi Sheet Bug (Gom chung toàn bộ chi tiết)**:
       - ⛔ **CẤM tách riêng mục "Chi tiết từng issue" thành các khối văn xuôi dài phía sau** gây trùng lặp và làm loãng tài liệu.
       - Toàn bộ chi tiết kỹ thuật: tóm tắt lỗi kèm link ảnh `[ảnh](...)`, nguyên nhân gốc, giải pháp cụ thể cho cả **FE & BE (code change thật)**, nhân sự phụ trách, **cột Hoàn thành** (`✅ Đã fix (ngày)`, `⚠️ Chờ chạy SQL`, `⚠️ Chờ PO/BA`), và **cột Ghi chú mẫu comment Sheet Bug** (`01102026-TênDev: ...`) BẮT BUỘC gom chung vào từng hàng của bảng.
    3. **Bắt buộc dùng Scoped CSS `table-layout: fixed !important`**:
       - Bảng Markdown nhiều cột (6–7 cột) BẮT BUỘC có khối `<style>` nhắm vào bảng (`table:has(...)`), đặt `table-layout: fixed !important; width: 100% !important;` và định nghĩa tỷ lệ % chuẩn cho từng cột (`th:nth-child(...)`).
       - Ngăn chặn triệt để thuật toán auto-layout của trình duyệt tự động bóp nghẹt cột Ghi chú và phình to cột Nội dung lỗi.
  - **Lỗi thật đã mắc**: Ban đầu tách riêng một mục "Chi tiết từng issue" dài 170 dòng ở cuối gây lặp nội dung, thiếu cột "Hoàn thành" trên bảng chính, và không khóa `table-layout: fixed` khiến cột Ghi chú bị co rúm thành từng từ 1 dòng không thể đọc được.

- **19.31. CẤM Dùng Từ Ngữ Mơ Hồ, Văn Hoa, Tiếp Thị Khi Viết Báo Cáo Nghiệm Thu & Sheet Bug — BẮT BUỘC Ghi Rõ Bị Gì, Fix Như Nào Chuẩn Kỹ Thuật (chốt 02/10/2026 - P0)**:
  - **Phạm vi**: Mọi báo cáo nghiệm thu, nhật ký kiểm thử lỗi F16, tài liệu review, tóm tắt commit và nội dung comment phản hồi Sheet Bug.
  - ⛔ **CẤM TUYỆT ĐỐI các từ ngữ mơ hồ, sáo rỗng, văn hoa, tiếp thị (Fluff / Marketing Words)**:
    - ❌ *thân thiện*, *tự động kẹp số*, *kẹp số an toàn*, *tối ưu hóa*, *trải nghiệm mượt mà*, *xử lý linh hoạt*, *thông minh*, *hoàn hảo*, *ổn định hơn*, *đảm bảo an toàn*...
    - Những từ này không có giá trị kỹ thuật, nói chung chung không rõ hành vi, làm loãng báo cáo và gây khó hiểu cho người nghiệm thu.
  - ✅ **BẮT BUỘC viết ngắn gọn, súc tích, chỉ rõ đúng 2 vế kỹ thuật**:
    1. **Bị gì (Hiện tượng lỗi kỹ thuật thật)**: Ghi chính xác tên lỗi hoặc hiện tượng kỹ thuật (ví dụ: *nhập số vượt int.MaxValue làm văng lỗi deserialization JSON của ASP.NET Core*; *ô el-input-number thiếu :max cho phép nhập số tùy ý*; *catch gọi thêm ElMessage.error làm hiện 2 toast đè nhau*).
    2. **Fix như nào (Hành động sửa kỹ thuật cụ thể)**: Ghi chính xác thay đổi code hoặc cấu hình (ví dụ: *thêm :max="86400" trên el-input-number chặn nhập quá 24h*; *đổi DTO sang long?, thêm rule FluentValidation InclusiveBetween(5, 86400)*; *bỏ lệnh gọi ElMessage.error trong catch*).
  - **Lỗi thật đã mắc**: Viết comment *"Đã xử lý DTO long?, validate dải 5-86400s thân thiện và loại bỏ toast lỗi kép"*, *"tự động kẹp số an toàn"* — dùng từ "thân thiện", "kẹp số" làm báo cáo thiếu chuẩn mực kỹ thuật, bị người dùng nhắc nhở trực tiếp.

- **19.32. CẤM Gộp Ghi Chú Issue — Mỗi Issue Phải Ghi Độc Lập, Rõ Ràng "Bị Gì / Fix Gì" Riêng (chốt 02/10/2026 - P0)**:
  - **Phạm vi**: Mọi hàng trong bảng nhật ký lỗi F16, mọi comment phản hồi Sheet Bug, mọi ghi chú tổng quan issue.
  - ⛔ **CẤM TUYỆT ĐỐI các dạng ghi chú gộp**:
    - ❌ *"Trùng nội dung với Issue X"* — hai issue có thể liên quan đến cùng màn hình nhưng lỗi khác nhau.
    - ❌ *"Xử lý gộp cùng Issue X"* — mỗi issue có file/layer/nguyên nhân sửa riêng.
    - ❌ *"Issue X và Issue Y là cùng một lỗi"* — dù nguyên nhân gần giống, hành vi lỗi và phương án sửa phải được mô tả độc lập.
    - ❌ Tham chiếu chéo issue dạng *"xem Issue X"* làm hàng bảng rỗng nội dung.
  - ✅ **BẮT BUỘC: mỗi hàng issue tự đứng độc lập hoàn toàn**:
    1. **Nội dung lỗi**: Ghi chính xác hiện tượng lỗi thật quan sát được (hiện tượng UI/API/log) — không cần biết issue kia nói gì, người đọc hiểu ngay từ hàng này.
    2. **Nguyên nhân**: Ghi nguyên nhân kỹ thuật gốc rễ của **chính issue này** — không phải nguyên nhân dùng chung với issue khác.
    3. **Phương án**: Ghi hành động sửa cụ thể (file, hàm, field, layer) của **chính issue này**.
    4. **Ghi chú Sheet Bug**: Ghi comment kỹ thuật rõ ràng cho **chính issue này** — không dùng cụm *"xem issue X"* hay *"gộp cùng issue X"*.
  - **Ví dụ đúng (3 issue đều liên quan màn hình Sao chép ánh xạ nhưng ghi độc lập)**:
    - **Issue 13**: DB chưa có bản dịch key `mappingConflictInUse` → fix seed `SysTerminology`.
    - **Issue 14**: Ô Mã `enable` + trống khi sao chép → fix FE thêm `disabled` + tooltip.
    - **Issue 16**: Modal sao chép giữ `isActive=true` + `id` cũ → fix FE xóa `id`, tắt `isActive`, gọi `syncMappingCode()`.
  - **Lỗi thật đã mắc**: Ghi chú tổng quan "14 và 16 là cùng một lỗi", dòng Issue 16 trong bảng viết "Xử lý gộp cùng Issue 14" — người dùng phản hồi trực tiếp: *"đừng báo cáo gộp chung vậy, nào ra đó bị gì, sửa gì độc lập"*.

- **19.33. SqlSugar Có Tính Năng Sẵn Thì BẮT BUỘC Dùng Tính Năng Sẵn — CẤM Tự Dựng Bản Thay Thế (chốt 02/10/2026 - P0)**:
  - **Phạm vi**: mọi thao tác CSDL qua SqlSugar trong toàn bộ dự án.
  - 🔴 **Nguyên tắc**: trước khi tự viết tay một cách làm, BẮT BUỘC tra xem SqlSugar **đã có sẵn** API cho việc đó chưa. Có thì dùng bản có sẵn. Các tính năng sẵn thường bị bỏ qua: `ToTree` / `ToTreeAsync` (dựng cây phân cấp), `ToChildList` / `ToParentList`, `ToPagedListAsync` (phân trang), `OrderBuilder`, `WhereIF`, `SplitTable`, `Insertable` / `Updateable` / `Deleteable` với OCC.
  - **Lý do**: dự án đã chuẩn hoá quanh SqlSugar; mỗi bản tự dựng là một lối riêng phải tự bảo trì, tự kiểm thử, và làm người đọc sau phải học thêm một cách làm nữa cho cùng một việc.
  - ✅ **Cây phân cấp — khuôn chuẩn của dự án** (xem `ZonesQueryHandler.cs:55`, `MenuQueryHandler.cs:51`):
    ```csharp
    // Entity: thuộc tính điều hướng, KHÔNG phải cột CSDL
    [SugarColumn(IsIgnore = true)]
    public List<TEntity>? Children { get; set; }

    // Query: dựng cây rồi .Adapt<>() map sang DTO
    return (await filtered.ToTreeAsync(u => u.Children, u => u.ParentId, rootValue))
        .Adapt<List<TOutput>>();
    ```
    - ⚠️ `ToTreeAsync` dựng cây **trong bộ nhớ** từ tập đã lọc ⇒ BẮT BUỘC `.Where(...)` thu hẹp trước, ⛔ TUYỆT ĐỐI KHÔNG gọi trên cả bảng.
    - ⚠️ ⛔ KHÔNG dùng `.Select(u => new TOutput{}, true)` sau `ToTreeAsync` — `Select` cắt mất nhánh `Children` vừa dựng. Dùng `.Adapt<>()`.
    - 🔴 **Cổng chặn bắt buộc**: ⛔ không thấy nút gốc trong tập kết quả thì `ToTree` trả **rỗng im lặng**. Cả 2 chỗ trong repo đều có cổng chặn trả danh sách phẳng khi đó — khi viết mới BẮT BUỘC có cổng chặn tương đương, kèm **một bài test khẳng định đúng hình dạng lồng** (⛔ không có test thì cổng chặn che mất chính lỗi nó đang chống).
    - 📌 Hai chỗ trong repo dùng **sentinel `"0"`** làm gốc (`u.Pid == "0"`), ⛔ không chỗ nào truyền `null`. Dùng `rootValue: null` là **đường chưa có tiền lệ** ⇒ phải có test chứng minh nó khớp gốc.
  - ⛔ **Ngoại lệ duy nhất được tự viết tay**: SqlSugar thật sự ⛔ không có API cho việc đó (ví dụ `CHANGETABLE(CHANGES ...)` của Change Tracking, bảng động ⛔ không có Entity tĩnh) — xem mục 19.20.
  - 🔴 **LỖI SUY DIỄN THẬT ĐÃ MẮC (02/10/2026) — ⛔ đừng lặp lại**: khi bàn API đọc log cha–con `ShareDataActivityLog`, AI phản đối `ToTreeAsync` với lập luận *"thêm `Children` vào Entity sẽ rò khoá `children` sang DTO vì `ShareDataActivityLogOutput` kế thừa Entity, còn TMS ⛔ không bị vì có `ZoneOutput` DTO riêng"*. Lập luận đó **SAI**: đọc mã nguồn thật thì `ZoneOutput : TmsZone`, `PageZoneOutput : TmsZone`, `WpMenuOutput : WpMenu` — **cả hai tiền lệ đều kế thừa Entity**, tức dự án đã chấp nhận cái giá đó từ trước.
    - **Bài học bắt buộc rút ra**: ⛔ TUYỆT ĐỐI KHÔNG lấy một **suy diễn chưa đọc mã nguồn** làm căn cứ để đi ngược quy ước sẵn có. Muốn bác một tính năng có sẵn thì BẮT BUỘC **mở đúng tệp đọc đúng dòng** trước, và dẫn số dòng cụ thể.
    - 📌 Cùng họ với quy tắc **19.12** (thứ tự nguồn dẫn chứng): căn cứ cho khẳng định *"code đang làm gì"* là **chính mã nguồn**, ⛔ không phải trí nhớ hay phỏng đoán từ tên tệp.

- **19.34. Mọi File Prompt BẮT BUỘC Ghi Tên & Đường Dẫn File Ngay Đầu File Markdown (chốt 02/10/2026 - P0)**:
  - **Phạm vi áp dụng**: Mọi file prompt (`*-prompt.md`, `{task-slug}.md`) được tạo trong thư mục `Prompt/` hoặc bất kỳ đâu trong repo.
  - 🔴 **Yêu cầu bắt buộc**: Ngay dưới tiêu đề chính `# ...`, BẮT BUỘC có dòng khai báo đường dẫn file prompt:
    `**Tệp prompt:** `DocBusinessThienAn/<Dự-án>/<PhânHệ>/Prompt/<task-slug>-prompt.md``
    hoặc dạng inline code để người dùng có thể double-click hoặc copy nhanh một chạm.
  - **Mục đích**: Khi người dùng xem file trên IDE hoặc cần giao việc, bàn giao context sang phiên hội thoại khác hay nhắc lệnh `@path`, người dùng có thể sao chép ngay đường dẫn file prompt mà không phải mất công duyệt cây thư mục hoặc gõ lại tên file.
  - ⛔ **CẤM bỏ sót**: Cấm tạo file prompt chỉ ghi tiêu đề nghiệp vụ mà không có dòng ghi đường dẫn/tên file prompt của chính nó trong nội dung `.md`.

---



Toàn bộ quy tắc dưới đây được đồng bộ từ `.kiro/steering/` của repo Frontend `TA-ITS015-WEBVUE-V1.0`, áp dụng bắt buộc cho toàn bộ mã nguồn Vue 3 / TypeScript:

### 20.1. Cấu trúc Thư mục & Component
- **Cấu trúc thư mục chức năng**:
  ```
  src/src/views/{module}/{featureName}/
  ├── index.vue                    # Trang danh sách chính (list/grid view)
  └── component/
      ├── edit{Feature}.vue        # Modal/Dialog Thêm / Sửa
      ├── detail{Feature}.vue      # Xem chi tiết (nếu có)
      └── {otherComponent}.vue     # Component con khác
  ```
- **Tên thư mục tính năng**: Luôn viết dạng `camelCase` (ví dụ: `dutySchedule`, `equipmentType`, `trafficData`, `shareData`).
- **Tên Component**: Khai báo qua thuộc tính `name` trong `<script lang="ts" setup name="{module}{Feature}">` (ví dụ: `name="tmsEquip"`, `name="shareDataConfig"`).
- **Lazy loading component con**: BẮT BUỘC dùng `defineAsyncComponent(() => import('/@/...'))`.
- **Path Alias**: `/@/` trỏ về thư mục `src/src/` (ví dụ: `import { getAPI } from '/@/utils/axios-utils'`).

### 20.2. Quy tắc API Services (Swagger Auto-Generated — CẤM TỰ TẠO/SỬA)
- **Vị trí**: Toàn bộ API client nằm tại `src/src/api-services/{module}/` được sinh tự động từ Swagger/OpenAPI qua lệnh `pnpm build-api` (hoặc chạy `api_build/build.bat`).
- **NGHIÊM CẤM**:
  - TUYỆT ĐỐI KHÔNG tự ý tạo mới hoặc chỉnh sửa thủ công bất kỳ file nào trong thư mục `api-services/`.
  - Nếu API backend mới chưa có hoặc DTO type chưa được sinh: **BÁO CÁO NGAY CHO NGƯỜI DÙNG** và chờ backend chạy lệnh sinh lại API client, KHÔNG tự chế code gọi API tự do.
- **Cách gọi API chuẩn**:
  ```typescript
  import { getAPI } from '/@/utils/axios-utils';
  import { TmsEquipmentApi, PageEquipmentInput } from '/@/api-services/tms';

  // Lấy dữ liệu phân trang
  const params = { page: 1, pageSize: 50, field: 'createTime', order: 'desc' } as PageEquipmentInput;
  const res = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost(params);
  ```

### 20.3. Chuẩn Trình Bày Modal Thêm / Sửa (BẮT BUỘC — Mọi Modal Phải Đồng Nhất)
> 📌 **Mẫu tham chiếu chuẩn**: `views/tms/workContact/component/editWorkContact.vue`.

Khi tạo mới hoặc sửa modal, BẮT BUỘC tuân thủ đúng bảng đối chiếu sau:

| Hạng mục | Quy chuẩn BẮT BUỘC | KHÔNG ĐƯỢC DÙNG |
|---|---|---|
| **Tiêu đề** | Slot `#header` với icon `<ele-Edit />` + `<span> {{ props.title }} </span>` | Thuộc tính `:title="props.title"` |
| **Thuộc tính dialog** | `draggable` + `:close-on-click-modal="false"` + `width="700px"` | `align-center` |
| **Nhãn form** | `label-width="auto"` + `label-position="left"` | `label-position="right"`, `label-width="120px"` |
| **Khoảng cách hàng** | `<el-row :gutter="10">` | `:gutter="16"` |
| **Cột responsive** | `<el-col :xs="24" :sm="24" :md="12" :lg="12" :xl="12" class="mb20">` (textarea dùng `:span="24"`) | `:span="12"` (thiếu responsive và thiếu class `mb20`) |
| **Validation** | `:rules="[...]"` đặt trực tiếp ngay trên `el-form-item` | Khai báo object `rules` riêng rồi bind trên `el-form` |
| **Giới hạn ký tự** | `EntityFieldLength.Length64/128/256` cho cả `:maxlength` và message | Hardcode số (`maxlength="256"`) |
| **Nút xác nhận** | `:disabled="state.isProcessing"` | `:loading="state.loading"` |
| **Tên State Form** | Luôn đặt tên là `state.editForm` | `state.ruleForm` hoặc tên tự chế |
| **Controls độ rộng** | `el-select`, `el-input-number` phải có `class="w100"` | Để mặc định làm co cụm giao diện |

**Quy tắc logic bắt buộc trong `<script setup>` của Modal**:
1. `openDialog(row)`: BẮT BUỘC gọi `editFormRef.value?.resetFields()` trước rồi mới gán `state.editForm = JSON.parse(JSON.stringify(row))` để tránh lưu lại lỗi validate từ lần mở trước.
2. `props.operateType == 'copy'` hoặc `'add'`: BẮT BUỘC gán `state.editForm.id = ''` để không bị update đè lên bản ghi cũ.
3. `cancel`: Đóng dialog (`state.isShowDialog = false`), KHÔNG emit `handleQuery`.
4. `closeDialog`: Lưu thành công mới emit `handleQuery` để reload lại lưới.
5. `Radio group`: Một `el-radio-group` duy nhất, `v-for` đặt trên từng `el-radio` (CẤM đặt `v-for` trên `el-radio-group`).

### 20.4. VxeTable Grid & Quản lý Danh mục (BaseConfig)
- **VxeTable**:
  - Dùng hook `useVxeTable<OutputType>({ id: 'featureName', name: 'ExportName', columns: [...] }, { proxyConfig, sortConfig, pagerConfig })`.
  - Bắt buộc khai báo `id` độc nhất cho mỗi bảng để hỗ trợ lưu trạng thái ẩn/hiện cột của người dùng.
- **BaseConfig Store**:
  - Luôn sử dụng enum `BaseConfigTypeEnum` trong `/@/types/enums/baseEnum`, TUYỆT ĐỐI KHÔNG hardcode chuỗi (như `'status_type'`).
  - Dùng `getConfigDataByCode(BaseConfigTypeEnum.Xxx)` để lấy danh sách cho `el-select`, `el-radio-group`.
  - Dùng `getConfigItemByCode(BaseConfigTypeEnum.Xxx, code)` để hiển thị trong formatter hoặc thẻ `TagInfo`.
  - BẮT BUỘC dùng `parseInt(item.code)` khi bind value vào model số.

### 20.5. Đa Ngôn Ngữ (i18n Scope Rules)
- **Quy tắc ưu tiên (xem chi tiết mục 19.26)**: Luôn ưu tiên lấy và khai báo bản dịch trên WebAPI Resource (`TAC_WebAPI/Resources/*.json`). Nếu WebAPI không có mới tận dụng dịch thuật tại Frontend i18n (`src/i18n/lang/*.json`) làm fallback.
- Tuân thủ tiền tố `lz.*`:
  - `lz.entity.base.*` / `lz.label.base.*` / `lz.button.base.*` / `lz.message.base.*` / `lz.validation.base.*`: Nội dung dùng chung toàn hệ thống. Nếu key đã có trong `base`, BẮT BUỘC tái sử dụng, KHÔNG tạo lại ở module.
  - `lz.entity.{module}.*` / `lz.label.{module}.*`: Dùng chung cho nhiều tính năng trong cùng phân hệ (`tms`, `vms`, `shareData`...).
  - `lz.entity.{module}{Feature}.*`: Dành riêng cho 1 màn hình đặc thù.
- **Quy tắc chuyển ngữ Menu, Thẻ Tab (TagsView) & Breadcrumb**:
  - Menu thanh bên / thanh ngang (`subItem.vue`, `vertical.vue`) và Thẻ Tab (`other.setTagsViewNameI18n`) được thiết kế mặc định tra cứu theo mẫu: `$t('lz.router.' + route.name, route.meta.title)` (với `route.name` là tên component được khai báo trong `<script lang="ts" setup name="...">`).
  - Khi cần đổi nhãn hiển thị hoặc hỗ trợ song ngữ Việt - Anh cho Menu/Trang mà không muốn tác động CSDL (`SysMenu`/`WpMenu`) và không sửa file `.vue`:
    1. Bổ sung key `lz.router.{routeName}` vào cả 2 file ngôn ngữ:
       - `src/src/i18n/lang/vi-vn.json`: `"lz.router.{routeName}": "Tên tiếng Việt"` (VD: `"lz.router.eshMapping": "Ánh xạ dữ liệu"`)
       - `src/src/i18n/lang/en-us.json`: `"lz.router.{routeName}": "English Name"` (VD: `"lz.router.eshMapping": "Data Mapping"`)
    2. Đồng thời bổ sung key `{meta.title}` gốc vào cả 2 file i18n (VD: `"Mapping": "Ánh xạ dữ liệu"` trong `vi-vn.json` và `"Mapping": "Data Mapping"` trong `en-us.json`) để đồng bộ hoàn toàn với Breadcrumb (`breadcrumb.vue` gọi `$t(v.meta.title)`).
  - TUYỆT ĐỐI KHÔNG hardcode nhãn tiếng Việt đè vào component `.vue` hoặc tự ý chạy DDL/DML sửa DB khi chưa có yêu cầu trực tiếp.

### 20.6. Quy Chuẩn CSS / SCSS & Theme Dark/Light
- **CẤM hardcode mã màu** (`#fff`, `#1578a3`, `#000`, `red`): BẮT BUỘC sử dụng CSS variables của Element Plus để đảm bảo tương thích hoàn hảo giữa Dark Theme và Light Theme:
  - Màu chủ đạo: `var(--el-color-primary)`, `var(--el-color-success)`, `var(--el-color-warning)`, `var(--el-color-danger)`.
  - Màu chữ: `var(--el-text-color-primary)`, `var(--el-text-color-regular)`, `var(--el-text-color-secondary)`.
  - Màu nền & Viền: `var(--el-bg-color)`, `var(--el-fill-color-light)`, `var(--el-border-color)`.
- **SCSS Comments**: BẮT BUỘC dùng dạng block comment `/* */`. TUYỆT ĐỐI KHÔNG dùng comment một dòng `//` trong SCSS (gây vỡ build Vite).
- **Cú pháp SCSS**: Không để thừa hai dấu chấm phẩy (`;;`).

### 20.7. Nguyên Tắc Sửa Giao Diện & Responsive (CSS-First, Không Thay Đổi Template)
- **CSS-First**: Mọi lỗi hiển thị (placeholder bị che, icon che khuất, vỡ dòng, co rúm nút, responsive màn hình nhỏ...) BẮT BUỘC xử lý bằng CSS/SCSS (Flexbox, Grid, Container Queries `@container`, Media Queries `@media`, CSS variables, pseudo-classes...).
- **CẤM xóa props / thuộc tính template**: TUYỆT ĐỐI KHÔNG tự ý gỡ bỏ các thuộc tính chuẩn của Element Plus (`show-word-limit`, `:maxlength`, `clearable`, `filterable`, `:body-style`...) để "né" việc căn chỉnh CSS.
- **Quy trình hỏi ý kiến**: Nếu không gian quá hẹp không thể hiển thị vừa cả nội dung và bộ đếm/nút, BẮT BUỘC hỏi ý kiến người dùng trước khi được phép lược bỏ bất kỳ thành phần nào của UI (tuân thủ mục 19.27).

---

## 🐞 21. Quy Chuẩn Phản Hồi Sheet Bug Kiểm Thử (Sheet Bug Comment Protocol)

> 📌 **Mục đích:** Thống nhất định dạng phản hồi giữa Đội ngũ Phát triển (Dev) và Đội ngũ Kiểm thử (Tester) trên các biểu mẫu / file Excel / Sheet theo dõi lỗi (F16, Issue Tracking Sheet), đảm bảo dễ dàng truy vết lịch sử xử lý, người thực hiện và tiến độ công việc.

### ✍️ Cấu Trúc Bắt Buộc Trong Cột "Ghi chú" (Comment Syntax):
Mọi nội dung phản hồi của Dev tại cột **Ghi chú** bắt buộc tuân theo cú pháp chuẩn:
`[ddMMyyyy]-[TênDev]: [Nội dung phản hồi]`

- **Định dạng thời gian:** `ddMMyyyy` (8 chữ số, ví dụ `01102026` cho ngày 01/10/2026).
- **Tên Dev:** Tên viết tắt chuẩn nội bộ (ví dụ: `DatHQ`, `HieuNV`, `SonTH`...).
- **Dấu phân cách:** Dấu gạch nối `-` giữa ngày và tên; dấu hai chấm kèm khoảng trắng `: ` trước nội dung.
- **Nội dung phản hồi:** Ngắn gọn, nêu rõ hành động kỹ thuật đã làm, phạm vi đã test hoặc trạng thái chờ confirm.

#### 💡 Ví dụ Chuẩn:
- `01102026-DatHQ: Đã điều chỉnh ABC, còn DEF đang đợi confirm.`
- `01102026-HieuNV: Đã giới hạn chu kỳ tối đa 86400s ở cả FE và BE API, tự động kẹp số an toàn.`
- `30092026-DatHQ: Đã dịch hoàn chỉnh thông báo lỗi sang tiếng Việt dễ hiểu trong SysTerminology.`

---

## 📎 Ghi chú mở — cần xác minh / còn trùng lặp

- **`GlobalUsings.cs` tối thiểu (mục 5.5)**: liệt kê gồm `Shared.Core.Domain` và `System.Linq.Dynamic.Core`, nhưng `src/Modules/VideoWall/Module.VideoWall/GlobalUsings.cs` **không có** 2 dòng này, lại có `Furion.ConfigurableOptions`, `Furion.DynamicApiController`, `Newtonsoft.Json`, `Microsoft.Extensions.Options`, `System.ComponentModel.DataAnnotations`. Cần rà thêm các module khác (WP, TMS, ShareData) rồi chốt lại danh sách tối thiểu cho đúng.
- **Dependency Injection Naming — casing**: xem ghi chú ngay tại bullet tương ứng ở mục 7 — camelCase (constructor tường minh) vs PascalCase (Primary Constructor khi đóng vai trò public property) áp dụng cho 2 ngữ cảnh khác nhau, dễ nhầm khi đọc lướt.
- **Trùng lặp với file steering AG-Kit — chưa xử lý**: danh mục agent/skill/workflow/script (lặp với `quick-reference.md`, `code-rules.md`, `request-routing.md`); đường dẫn `.agents/...` (lặp với `core-protocol.md` mục *Path Awareness*).
