# Test E2E Frontend

⚠️ E2E ≠ `dotnet test`. Nó điều khiển trình duyệt thật nên BẮT BUỘC có sẵn:

1. **WebAPI đang chạy** (`TAC_WebAPI`) và trỏ vào CSDL có dữ liệu.
2. **Dev server FE đang chạy**: `cd TA-ITS015-WEBVUE-V1.0/src && pnpm dev`.
3. **Đã đăng nhập được** — các bài test cần tài khoản; ⛔ không hardcode mật khẩu vào tệp test, dùng biến môi trường.

## Chạy

```bash
cd tests/FE
pnpm install
pnpm playwright:install     # 1 lần cho mỗi máy
pnpm test:e2e
```

⛔ KHÔNG gộp vào `dotnet test` — hai toolchain khác nhau, ⛔ không dùng chung runner hay bước CI nào.

## Phân hệ nào được chạy — và vì sao có bài bị "skipped"

Có **2 tầng loại trừ**, tương đương cơ chế `CanCompileShareDataTests` của `tests/BE/test.csproj`:

| Tầng | Điều kiện | Biểu hiện |
|---|---|---|
| 1 — `testIgnore` | Thư mục `TA-ITS015-WEBVUE-V1.0/src/src/views/<phân-hệ>/` **không có** trong nhánh | Lúc khởi động in `[E2E MODULES] Loại khỏi chạy: ...`; bài test ⛔ **không xuất hiện** trong báo cáo |
| 2 — `test.skip` | Có mã nguồn, nhưng WebAPI trả **404** (chưa triển khai) hoặc **403** (tài khoản chưa được cấp quyền) | Báo **skipped** kèm lý do |

🔴 **"skipped" ⛔ KHÔNG phải "passed".** Thấy nhiều bài skipped thì đọc lý do: thường là tài khoản test chưa được cấp menu phân hệ đó, ⛔ không phải tính năng hỏng. Menu/route lấy từ CSDL (`SysMenu`) nên quyền do dữ liệu quyết định.

⚠️ Tag `@sharedata` / `@videowall` chỉ để chạy tay chọn lọc bằng `--grep`, ⛔ **không** phải cơ chế loại trừ.
