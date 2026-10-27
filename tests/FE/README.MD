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

---

## 📖 Cẩm nang Playwright: `page.route` & `page.locator` là gì?

Để viết và đọc hiểu các bài test E2E Frontend nhanh chóng, dưới đây là đặc tả chi tiết 2 khái niệm cốt lõi:

### 1. `page.route`: Bộ đánh chặn và làm giả mạng (Network Interception / Mocking)

#### Vấn đề giải quyết:
Khi test giao diện người dùng (UI), nếu mỗi lần test đều phải gọi vào API thật và CSDL thật:
- Test sẽ chạy chậm (phải chờ mạng, chờ query DB).
- Dữ liệu DB thật bị thay đổi sau mỗi lần test, gây xung đột giữa các bài test.
- Khó giả lập các trường hợp đặc biệt (như mất mạng, API trả lỗi 500, hoặc giả lập đối tác chưa có cấu hình riêng).

#### Cách hoạt động:
`page.route(urlPattern, handler)` giống như một **trạm kiểm soát giao thông**:
- Khi trình duyệt (Vue app) gửi một request HTTP (ví dụ `GET /api/sharedata/sharedatapacket/getextractsql`), Playwright chặn request này lại **ngay trên trình duyệt**, không cho gửi ra ngoài mạng.
- Playwright trả về ngay lập tức dữ liệu JSON giả lập do bài test chỉ định thông qua `route.fulfill(...)`.

#### Ví dụ minh họa:
```typescript
// Giả lập backend trả về gói tin có câu SQL và danh sách trường
await page.route('**/api/sharedata/sharedatapacket/getextractsql*', (route) => {
    route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
            code: 200,
            result: {
                extractSql: 'SELECT td.Speed AS speed FROM TmsTrafficData td',
                fields: [{ key: 'speed', label: 'Tốc độ', type: 'number' }]
            }
        })
    });
});
```
> **Điểm mấu chốt**: Vue app vẫn nghĩ là đang giao tiếp với server thật 100%, render giao diện bình thường, nhưng bài test chạy xong chỉ trong vài mili-giây và độc lập hoàn toàn.

---

### 2. `page.locator`: Con trỏ tìm kiếm phần tử trên giao diện (Element Selector)

#### Vấn đề giải quyết:
Trong test tự động, bạn cần "chỉ tay" cho robot biết: *bấm vào nút nào*, *nhập vào ô nào*, *kiểm tra dòng chữ nào trên bảng*. `page.locator` chính là ngón tay chỉ vào phần tử đó.

#### Đặc tính quan trọng (Auto-waiting):
Playwright `locator` không chỉ tìm phần tử một lần rồi thôi. Nó **tự động chờ (polling)** cho đến khi:
- Phần tử xuất hiện trong DOM.
- Phần tử hiển thị trên màn hình (visible, không bị ẩn bởi `display: none` hay `v-if`).
- Phần tử hết hiệu ứng animation và sẵn sàng nhận tương tác (enabled, không bị disabled).
- Vì vậy, bạn **không bao giờ cần viết `sleep(2000)` hay `waitForTimeout`** thủ công.

#### Các cách dùng phổ biến:

| Mục đích | Cú pháp `page.locator` | Ví dụ thực tế |
| :--- | :--- | :--- |
| **Tìm theo CSS Class** | `page.locator('.ten-class')` | `page.locator('.sql-toolbar-bar')` |
| **Tìm theo chữ hiển thị** | `page.locator('selector', { hasText: 'Chữ' })` | `page.locator('.el-button', { hasText: 'Lưu & phân tích' })` |
| **Lấy phần tử đầu / cuối** | `.first()`, `.last()`, `.nth(i)` | `page.locator('.vxe-body--row').first().click()` |
| **Rê chuột (Hover)** | `.hover()` | `await page.locator('.add-partner-tab-btn').hover()` |
| **Nhập liệu** | `.fill('văn bản')` | `await page.locator('input').first().fill('Tốc độ xe')` |
| **Kiểm tra hiển thị (Assert)** | `expect(locator).toBeVisible()` | `await expect(page.locator('.partner-setup-dialog')).toBeVisible()` |
| **Kiểm tra nội dung chữ** | `expect(locator).toContainText('...')` | `await expect(page.locator('.sql-toolbar-left')).toContainText('VDOT')` |

