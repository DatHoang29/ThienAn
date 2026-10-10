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
* **Có TaskCode:** `[BranchKey]/[yyyyMMdd]-[TaskCode]_[ten-cong-viec-viet-thuong-cach-nhau-boi-dau-gach-ngang]`
  - *Ví dụ:* `feat/20260922-XD1.2.2.5_map-location-dathp`, `fix/20260922-XD1.2.2.5_map-location`
* **Rút gọn (không TaskCode):** `[BranchKey]/[yyyyMMdd]-[module]-[ten-cong-viec-ngan-gon]`
  - *Ví dụ:* `fix/20261007-sharedata-issue`, `fix/20261008-sharedata-khung-gio-qua-dem`

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

---

## 📝 3. Quy Định Nội Dung Commit & Format Chuẩn

### Cú Pháp Commit Message (BẮT BUỘC Tiếng Việt Có Dấu - Rule 19.55)
* **Summary (Dòng 1):** `[type]([scope]): [noi-dung-cong-viec-tieng-viet-co-dau]`
  - *Ví dụ:* `feat(vms): thêm mới dịch vụ`, `fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata`
  - Scope là tên module viết thường: `sharedata`, `vms`, `tms`, `videowall`, `toll`, `test`...
* **Dòng 2:** Lặp lại nguyên văn Summary (⛔ TUYỆT ĐỐI KHÔNG thêm nhãn `Description:`).
* **Dòng 3 trở đi:** Dòng trống, sau đó là các gạch đầu dòng (`- `) ngắn gọn mô tả **fix cái gì / làm cái gì** tại file nào.
* ⛔ **CẤM tự ý chèn thẻ metadata** `Ref: ...`, `Reviewer: ...`, `CR: ...` khi không có yêu cầu.

### Ví Dụ Commit Message Chuẩn:
```text
fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

fix(sharedata): xử lý danh sách lỗi kiểm thử F16 phân hệ sharedata

- Cấu hình đăng ký: giới hạn chu kỳ max 86400s, clamp real-time (Issue 1, 2)
- Đối tác chia sẻ: xóa mã khi copy, khóa ô mã khi sửa kèm tooltip (Issue 3, 4)
- Ánh xạ dữ liệu: cập nhật thông báo dữ liệu nguồn rỗng, ẩn nhóm trường ở header (Issue 20, 23, 24)
```

---

## 🛑 4. Quy Định Vận Hành Git Đối Với Trợ Lý AI (P0 Safeguards)

1. **CẤM TUYỆT ĐỐI TỰ Ý `git add` VÀ `git reset` (Strict No Auto-Stage / No Auto-Unstage)**:
   - AI **TUYỆT ĐỐI KHÔNG CHẠY `git add`** hoặc đưa file vào Staged Changes khi người dùng không yêu cầu trực tiếp.
   - AI **TUYỆT ĐỐI KHÔNG CHẠY `git reset`** hoặc unstage file người dùng đã chủ động đưa vào Staging.
   - Mọi thay đổi code BẮT BUỘC để nguyên ở trạng thái **Working Tree (Changes / Unstaged)** để người dùng tự review qua diff.
2. **CẤM TỰ Ý COMMIT VÀ PUSH (Rule 19.58)**:
   - Quyền commit và push hoàn toàn thuộc về lập trình viên.
   - BẮT BUỘC viết test đầy đủ, kiểm chứng pass 100%, người dùng kiểm tra xác nhận thì mới được commit.

---

## ✅ 5. 10 Điểm Bắt Buộc Kiểm Tra Trước Khi Commit (Pre-Commit Checklist)
1. **Biên dịch & Cú pháp:** Đảm bảo toàn bộ project build thành công, không lỗi cú pháp hoặc warning nghiêm trọng.
2. **Logic nghiệp vụ:** Kiểm tra logic đúng đặc tả, chạy thử trên môi trường dev.
3. **Coding Standards:** Tuân thủ Clean Code, PascalCase, camelCase, cấu trúc thư mục quy định.
4. **Dọn rác code:** Xóa sạch code debug, `console.log`, comment rác, using thừa (IDE0005).
5. **Chạy test tự động:** 100% test cases pass (Full Business Flow, E2E Playwright).
6. **Merge Conflict:** Đồng bộ branch của bạn với `dev` mới nhất trước khi tạo PR.
7. **Tài liệu:** Cập nhật tài liệu kỹ thuật, MasterPlan nếu có thay đổi logic hoặc API.
8. **File & Commit Message:** Chỉ commit file liên quan trực tiếp, commit message đúng chuẩn tiếng Việt có dấu.
9. **Bảo mật & Credentials:** Tuyệt đối không commit API keys, mật khẩu DB, credentials, dữ liệu cá nhân.
10. **Kiểm tra staging:** Xác nhận hệ thống vận hành ổn định trên staging trước khi release.
