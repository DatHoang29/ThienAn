# 📌 Quy Định Chung Phòng Phần Mềm: Nhánh & Commit Messages

Tài liệu này định nghĩa quy trình đặt tên nhánh (branching workflow), luồng phát triển và định dạng thông điệp commit (commit messages) áp dụng cho toàn bộ thành viên trong dự án.

---

## 🌿 1. Quy Định Đặt Tên Nhánh (Branch Naming)

### 🏷️ Phân Loại Nhánh (Branch Types)
*   **`release`**: Nhánh chứa sản phẩm đóng gói bàn giao tới khách hàng. Khi cập nhật nhánh này, bắt buộc tạo tag đánh dấu phiên bản (version tag).
*   **`staging`**: Nhánh dùng để chạy thử nghiệm trên môi trường dàn dựng / UAT (User Acceptance Testing).
*   **`hotfix`**: Nhánh sửa lỗi nhanh/khẩn cấp phát hiện trực tiếp trên môi trường release của khách hàng.
*   **`dev`**: Nhánh phát triển chính (tích hợp các tính năng).
*   **`feat`**: Nhánh phát triển một tính năng/chức năng mới (feature).
*   **`fix`**: Nhánh sửa các lỗi phát sinh trong quá trình phát triển hoặc review code (fixbug).
*   **`merge`**: Nhánh trung gian tạo ra để giải quyết xung đột hoặc merge code giữa các nhánh liên quan.
*   **`preview`**: Nhánh dùng để xem trước code (preview) phục vụ việc tạo Pull Request và quản lý issue.
*   **`exp`**: Nhánh thử nghiệm các công nghệ hoặc tính năng mới (experimental).

### ✍️ Cú Pháp Đặt Tên (Naming Syntax)
Tên nhánh được đặt theo một trong hai cú pháp sau:
*   **Cú pháp 1:** `[BranchKey]/[TaskCode]_[ten-cong-viec-viet-thuong-khong-dau-cach-nhau-gach-ngang]`
*   **Cú pháp 2:** `[BranchKey]/[yyyyMMdd]-[ten-cong-viec-cach-nhau-gach-ngang]`

> [!TIP]
> *   Thời gian định dạng theo: `yyyyMMdd` hoặc `yyyyMM`.
> *   Nên đưa thêm **tên viết tắt của nhân sự** thực hiện vào tên nhánh để dễ dàng quản lý.
> *   Tên nhánh viết bằng tiếng Việt không dấu hoặc tiếng Anh.

#### 💡 Ví dụ:
*   `feat/20250101-XD1.2.2.5_map-location`
*   `fix/20250102-XD1.2.2.5_fix-map-location`
*   `merge/20250105-XD1.2.2.5_merge-code-dev-a-b`
*   `release/20250110-v1.0.1`

---

## 🔄 2. Luồng Vận Hành Nhánh (Branching Workflow)

Thứ tự thực hiện quy trình tạo và quản trị nhánh trong dự án như sau:

1.  **Khởi tạo**: Khi bắt đầu dự án, khởi tạo nhánh branch đầu tiên là `release` và commit code đầu tiên với cấu trúc thư mục quy định.
2.  **Nhánh dev**: Tạo nhánh `dev` tương ứng từ nhánh `release`.
3.  **Làm task**: Khi nhận chức năng mới, tạo branch `feat` tương ứng từ nhánh `dev`.
4.  **Giải quyết xung đột**: Nếu có phát sinh cần merge lại code giữa các nhân sự, tạo nhánh `merge` để thực hiện merge code liên quan.
5.  **Chuẩn bị PR**: Khi hoàn tất chức năng, để preview chức năng, tạo nhánh `preview` nếu cần thiết (đặc biệt đối với chức năng phức tạp, có merge code).
6.  **Tạo Pull Request (PR)**: Nhân sự tạo pull request yêu cầu merge vào nhánh `dev` và tiến hành preview trên yêu cầu này.
7.  **Duyệt code**: Sau khi preview hoàn tất, merge từ các nhánh `feat` / `preview` vào `dev` (chấp thuận pull request).
8.  **Fixbug**: Sau khi test ở nhánh `dev/test/staging`, nếu phát sinh lỗi, tạo nhánh `fix` để sửa lỗi tương ứng.
9.  **Staging**: Khi đã test hoàn tất, tạo nhánh `staging` để thiết lập môi trường dàn dựng/uat cho khách hàng test hoặc chạy thử nghiệm thực tế.
10. **Hotfix**: Khi có lỗi phát sinh ở nhánh `release`, tạo nhánh `hotfix` để sửa lỗi gấp/nhỏ. Nếu thay đổi lớn hoặc không gấp, tiến hành xử lý theo quy trình thông thường qua `feat` -> `dev`.
11. **Đóng gói**: Nhánh `release` đại diện cho sản phẩm hoàn thiện tới khách hàng, thực hiện tạo tag Git để đóng gói version.

---

## 📝 3. Quy Định Nội Dung Commit (Commit Message Rules)

### 🏷️ Các Từ Khóa Summary Commit
Khi thực hiện commit code, phần tiêu đề (summary) của commit bắt buộc bổ sung các từ khóa phân loại sau:
*   **`feat`**: Commit thêm hoặc hủy chức năng.
*   **`fix`**: Sửa lỗi hoặc thay đổi chức năng.
*   **`refactor`**: Cấu trúc lại mã nguồn / Tối ưu hóa code (không thay đổi hành vi hệ thống).
*   **`chore`**: Các công việc bổ trợ khác không ảnh hưởng trực tiếp đến logic code (cập nhật tài liệu HDSD, viết test case, dọn dẹp code thừa...).

> [!IMPORTANT]
> Thêm ký tự **`!`** ngay sau từ khóa (ví dụ: `fix!`, `feat!`) để nhấn mạnh đây là thay đổi lớn, có thể gây ảnh hưởng nghiêm trọng đến hệ thống hoặc làm đứt gãy luồng xử lý cũ (breaking changes).

### ✍️ Cú Pháp Thông Điệp Commit (Commit Format)

#### Cú pháp Summary:
`[Keyword]: [TaskCode] - [noi-dung-cong-viec-dung-cau-hanh-dong]`

> [!TIP]
> Nên thống nhất một ngôn ngữ chung (tiếng Việt hoặc tiếng Anh) xuyên suốt dự án.

#### Cấu trúc Description chi tiết:
1.  **Dòng đầu tiên:** Ghi lại nội dung Summary.
2.  **Hai dòng tiếp theo:** Bỏ trống.
3.  **Nội dung chi tiết:** Gạch đầu dòng các công việc cụ thể đã thực hiện trong lần commit này.
4.  **Thông tin tham chiếu (Metadata) nằm ở cuối:**
    *   `Reviewer: [Tên người duyệt]` (nếu có review)
    *   `CR: [Mã CR]` (nếu commit thuộc Change Request nào)
    *   `Ref: [Mã tham chiếu]` (nếu sửa đổi từ commit/issue log nào trước đó)

---

### 💡 Ví dụ Minh Họa

#### Ví dụ 1: Summary cơ bản
*   `feat: XD1.2.2.5 - add map location`
*   `feat: XD1.2.2.6 - bo chuc nang khong su dung`
*   `fix: fix map location & fix traffic info`
*   `refactor: format code map location`
*   `fix!: XD1.2.2.7 - thay doi luong gui mail` (Thay đổi lớn)

#### Ví dụ 2: Cấu trúc đầy đủ của một Commit Message
```text
fix: sửa luồng gửi mail chức năng A module A

Sửa luồng gửi mail chức năng A module A


- Thay đổi luồng thứ tự nhân sự duyệt cho phép gửi mail
- Bổ sung cấu hình thiết lập thời gian timeout
- Bỏ bớt code dư thừa

Reviewer: SonTH
CR: CR0001-thay-doi-luong-gui-mail
```

---

## 🚫 4. Quy Định Tự Động Hóa Đối Với Trợ Lý AI (AI Execution Rules)

Đối với Trợ lý AI (Antigravity Agent), tuyệt đối tuân thủ 2 nguyên tắc sau khi làm việc trong dự án:

1.  **KHÔNG TỰ ĐỘNG CHẠY LỆNH BUILD (`dotnet build`)**: AI không được tự động chạy lệnh `dotnet build` hoặc bất kỳ lệnh biên dịch nào sau khi chỉnh sửa code, trừ khi người dùng yêu cầu trực tiếp.
2.  **KHÔNG TỰ ĐỘNG COMMIT VÀ PUSH GIT (`git commit` / `git push`)**: AI không được tự động chạy `git add`, `git commit`, hay `git push` code lên repository dưới bất kỳ hình thức nào. Quyền commit và push code hoàn toàn thuộc về lập trình viên.
3.  **QUY ĐỊNH DOCKER SQL SERVER TRÊN MAC**: Máy tính chạy môi trường macOS (đặc biệt chip Apple Silicon M1/M2/M3/M4) **BẮT BUỘC** dùng Docker image `mcr.microsoft.com/azure-sql-edge:latest`. TUYỆT ĐỐI KHÔNG dùng `mcr.microsoft.com/mssql/server:2022-latest` vì bản x86_64 sẽ bị crash tràn bộ nhớ QEMU (`Invalid mapping of address`).
4.  **QUY ĐỊNH MODULE SHARES CONTROLLER**: BaseController trong `Modules.Shares.Controllers` bắt buộc thiết lập `GroupName = "Share"` và `BasePath = "api/vms"`.
5.  **QUY ĐỊNH ENTITY CLASS**: Tất cả các Entity class trong hệ thống bắt buộc phải kế thừa `EntityTenant` (từ `Shared.Core.Domain`).




