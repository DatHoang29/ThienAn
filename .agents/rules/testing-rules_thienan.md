---
name: testing-rules
version: 1.0.0
priority: P1
trigger: model_decision
description: Unified Testing Suite protocol, Playwright FE E2E, xUnit BE on local DB, and strict testing philosophy.
---

# 🧪 Quy Chuẩn Kiểm Thử (Testing Suite Protocol) — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định triết lý viết test, bộ kiểm thử hợp nhất (Unified Test Suite), chuẩn kiểm thử Backend xUnit và Frontend Playwright.

---

## 🎯 1. Triết Lý Kiểm Thử (Core Testing Philosophy)

1. **Kiểm Thử Toàn Trình Nghiệp Vụ (Full Business Flow - Rule 19.10)**:
   - **BẮT BUỘC** viết các bài kiểm thử tích hợp luồng nghiệp vụ hoàn chỉnh (Full Flow Integration Test) chạy trên CSDL Test Local thật.
   - ⛔ **TUYỆT ĐỐI CẤM** viết các micro unit test vụn vặt chỉ để kiểm tra từng hàm helper static, rẽ nhánh if/else nhỏ lẻ với các object giả lập in-memory rời rạc.
2. **Tái Hiện Bug Trước Khi Fix (TDD Red-Green - Rule 19.59)**:
   - Khi nhận task sửa lỗi (fix bug), BẮT BUỘC viết bài test tái hiện lỗi thất bại (Fail/Red) trước.
   - Sau đó mới sửa code để test chuyển sang thành công (Pass/Green).
3. **Bộ Kiểm Thử Hợp Nhất (Unified Test Suite - Rule 19.63)**:
   - **Frontend Test**: BẮT BUỘC viết bằng Playwright tại `tests/FE/` (`pnpm test:e2e`).
   - **Backend Test**: BẮT BUỘC viết bằng xUnit tại `tests/BE/` (`dotnet test tests/BE/test.csproj`).
   - ⛔ **CẤM TUYỆT ĐỐI** tạo file script test ad-hoc rải rác (`test.js`, `test.ps1`, `verify.sh`...) ở ngoài thư mục quy định.

---

## ⚙️ 2. Quy Chuẩn Kiểm Thử Backend (xUnit & Local Database)

### An Toàn CSDL Test Local (Section 11)
* Mọi bài test backend BẮT BUỘC chạy trên CSDL SQL Server **Local** (`localhost` / `127.0.0.1` / `Host.GuardAllConnectionsLocal`).
* ⛔ **CẤM TUYỆT ĐỐI** trỏ test vào DB staging hoặc production.
* ⛔ **CẤM cờ `--no-build`**: Khi chạy test qua CLI, luôn dùng `dotnet test tests/BE/test.csproj` (không thêm `--no-build`).

### Cấm Thư Viện Mock Ngoài (No-Moq Rule - Section 15 & Rule 19.18)
* ⛔ **CẤM DÙNG Moq, NSubstitute, FakeItEasy** hoặc các thư viện mock bên ngoài.
* ⛔ **CẤM mock Service nội bộ & NATS**: Toàn bộ service nghiệp vụ (`IDataOutboundService`, `IDataInboundService`...) và bus tin nhắn nội bộ bắt buộc lấy bản thật từ `host.Services`.
* **Ngoại lệ duy nhất được giả lập**: Máy chủ HTTP của đối tác bên thứ ba bên ngoài, và BẮT BUỘC dùng mock server thật bằng `HttpListener` trên `127.0.0.1` (dùng `MockHttpClientFactoryTest` trong `tests/Mock/`).

### Cấu Trúc Test Chuẩn AAA (Rule 19.43, 19.46)
* **Khai báo đầu hàm**: Service và dependencies khởi tạo ở khối Arrange (đầu hàm).
  ```csharp
  [Fact]
  public async Task ProcessData_FullFlow_Success_Test()
  {
      // 1. Arrange: Khởi tạo service, seed data với Unique ID
      var service = host.Services.GetRequiredService<IDataOutboundService>();
      var uniqueCode = $"PARTNER_{Guid.NewGuid():N}";
      
      // 2. Act: Thực thi luồng nghiệp vụ
      var result = await service.ProcessAsync(uniqueCode);
      
      // 3. Assert: Kiểm tra kết quả
      Assert.True(result.IsSuccess);
  }
  ```
* ⛔ **CẤM bọc `try-catch` hoặc `try-finally`** quanh Act và Assert. Luồng test phải phẳng và tuần tự theo AAA. Nếu Act quăng exception ngoài ý muốn, hãy để test fail tự nhiên.

### Quản Lý Dữ Liệu Test (Rule 19.45)
* ⛔ **CẤM tự ý xóa bảng DB trong từng test method**: Không viết `db.Deleteable<Table>().ExecuteCommand()` rải rác trong từng test.
* Dọn dẹp tập trung tại `Host.ClearAllData()` và mỗi test method tự cô lập dữ liệu bằng **Unique ID / Mã riêng biệt** để đảm bảo test chạy song song không xung đột.
* ⛔ **CẤM gọi `InitTables<T>()` / `EnsureTablesCreated()`**: Kích hoạt tự động tạo bảng thông qua cấu hình `TableSettings` trong `tests/appsettings.Test.json`.

### Cô Lập Kiểm Thử Kết Nối Server Thật (`tests/BE/RealServer/` - Rule 19.66)
* **Thư mục & File riêng biệt**: Mọi bài test kết nối trực tiếp tới **server thật / hạ tầng ngoài thật** (ví dụ: NATS Server TLS thật `tls://115.78.1.139:12211`, SMTP/FTP server thật, thiết bị phần cứng thật...) **BẮT BUỘC tách ra thư mục chuyên biệt `tests/BE/RealServer/`** (ví dụ `ShareDataNatsServerConnectionTests.cs`). ⛔ **CẤM** đặt lẫn lộn trong các file test nghiệp vụ local (`tests/BE/ITS/...`).
* **Collection riêng biệt (`[Collection("real-server")]`) & Không trộn nghiệp vụ**: Các bài test server thật chỉ kiểm chứng tầng kết nối/truyền tải (connect, publish/subscribe round-trip, handshake...), **BẮT BUỘC dùng `[Collection("real-server")]`** (`RealServerTestCollection`, không gắn `ICollectionFixture<Host>`), hoàn toàn độc lập với CSDL local và không kéo theo logic nghiệp vụ (`ShareDataSubscription`, `DataOutboundService`...).
* **Bắt buộc gắn `[Fact(Skip = "...")]`**: Mọi test gọi ra server thật phải có tham số `Skip` giải thích rõ lý do để luồng test local mặc định luôn chạy độc lập, nhanh và pass 100%; chỉ gỡ `Skip` khi cần chủ động kiểm tra kết nối tới server thật.

### 2.6. Quy Chuẩn Kiểm Thử NATS Tích Hợp Thật (Phương Án 3 — Rule 19.81)
Khi kiểm thử tích hợp với NATS broker thật (Docker local 127.0.0.1:4222):
- **Cách ly Subject NATS độc lập cho từng bài test:** Mỗi test method tự sinh `uniqueSubject = $"ta.its.event.sharedata.newdata.{testName}_{testId}"` và inject qua `ConfigurationBuilder` đè cả 2 khóa `Nats:NewDataSubject` và `Nats:Subject`. Tuyệt đối không dùng chung subject mặc định (`DEFAULT_NATS_SUBJECT`).
- **Cơ chế Retry Probe Loop (100ms):** Do `TransportManager.SubscribeAsync` ủy quyền cho client đăng ký `Subscribe` bất đồng bộ trong background `Task.Run`, phát probe 1 lần duy nhất sẽ bị broker drop nếu gói `SUB` chưa tới nơi. BẮT BUỘC phát lặp probe trong vòng lặp `while (DateTime.UtcNow < deadline)` với `await Task.Delay(100, cts.Token)` cho tới khi listener nhận được (`received.Count > 0`), sau đó `received.Clear()`.
- **Làm sạch Subscription:** Gọi `transport.Unsubscribe(uniqueSubject)` ở đầu và cuối bài test để giải phóng handler trên singleton `TransportManager`.
- **Tăng hạn định timeout khi chạy batch lớn:** Đặt timeout tối thiểu 45–60 giây cho các assertion chờ bản tin NATS toàn trình để tránh fail giả do tải cao của luồng CPU/threadpool.

---

## 🎭 3. Quy Chuẩn Kiểm Thử Frontend (Playwright E2E)

* **Vị trí tệp test**: Đặt tại `tests/FE/views/<module>/<feature>.test.ts`.
* **Cấu Trúc Luồng Test Tuyến Tính Tuần Tự (Step-by-Step Flow: Setup → Navigate → Action → Assert)**:
  - Mọi bài test tương tác giao diện BẮT BUỘC viết tuần tự từ trên xuống dưới theo 4 bước rõ ràng, dễ đọc, dễ hiểu:
    ```ts
    test('X. Tên bài test rõ ràng, nêu bật tính năng cần kiểm chứng', async ({ page }) => {
        // BƯỚC 1: Setup & Mock API qua page.route()
        // Vai trò của page.route(): Chặn bắt và giả lập dữ liệu trả về từ WebAPI Backend,
        // giúp Frontend test chạy độc lập, siêu tốc và không bị phụ thuộc vào server thật hay rớt mạng.
        let isApiCalled = false;
        await page.route('**/api/sharedata/sharedatapacket/getextractsql*', async (route) => {
            isApiCalled = true;
            await route.fulfill({
                status: 200,
                contentType: 'application/json',
                body: JSON.stringify({ code: 200, result: { extractSql: 'SELECT ...', status: 1 } }),
            });
        });

        // BƯỚC 2: Navigate & Chờ giao diện sẵn sàng
        await page.setViewportSize({ width: 1600, height: 900 });
        await page.goto('/#/sharedata/dataSource');
        const grid = page.locator('.packet-col .vxe-grid');
        await expect(grid).toBeVisible({ timeout: 15_000 });

        // BƯỚC 3: Action / Tương tác người dùng
        const btnAction = page.locator('.btn-target');
        await btnAction.click();
        await page.waitForTimeout(300);

        // BƯỚC 4: Assert / Đối chiếu kết quả kỳ vọng
        expect(isApiCalled).toBe(true);
        await expect(page.locator('.result-panel')).toBeVisible();
    });
    ```
* **Quy Chuẩn Đường Dẫn Mã Nguồn (Path Alias & Helper — CẤM Hardcode Relative Dài)**:
  - ⛔ **CẤM** hardcode đường dẫn tương đối lặp lại kiểu: `../../../../TA-ITS015-WEBVUE-V1.0/src/src/...`.
  - ✅ **BẮT BUỘC**:
    - Sử dụng Path Alias `@webvue/*` (đã khai báo trong `tests/FE/tsconfig.json` trỏ thẳng tới `TA-ITS015-WEBVUE-V1.0/src/src/*`).
    - Hoặc dùng hàm helper `resolveWebvuePath(relativePath)` trong `tests/FE/setup.ts` để đọc/kiểm tra file mã nguồn Frontend.
* **Quy Chuẩn Kiểm Thử Kéo Thả & Resize (Splitter & Column Resizing)**:
  - **Chờ phần tử ổn định tọa độ trước khi kéo**: Luôn dùng assertion chờ tọa độ ổn định:
    ```ts
    await expect(async () => {
        const box = await splitter.boundingBox();
        expect(box).not.toBeNull();
        expect(box!.y).toBeLessThan(200);
    }).toPass({ timeout: 5000 });
    ```
  - **Tọa độ kéo an toàn**: Kéo chuột bằng tọa độ offset an toàn (`startY = box.y + 100`), không kéo vào viền mép trên cùng (`box.y`) hoặc giữa tâm nếu có phần tử lồng bên trong cản trở.
  - **Kiểm thử kéo resize cột table**:
    - Khi kiểm tra tính năng kéo co giãn cột (Column Resizing): Kiểm tra các cột nội dung và cột thao tác không bị đứt gãy bố cục hoặc tràn lệch toạ độ.
    - Với các bảng có cột cố định (`fixed: 'right'`), kiểm tra khối fixed luôn bám sát mép container và không bị trôi lệch sau khi kéo.
* **Bắt buộc viết test cho mọi fix lặt vặt (Rule 19.67 - Zero Regression)**:
  - Mọi yêu cầu sửa/chỉnh lỗi dù là lặt vặt nhất ở Frontend (UI, CSS, font chữ, căn chỉnh khoảng cách, chống giật modal/tab, placeholder, i18n, form validation...) **BẮT BUỘC viết/bổ sung ngay bài test Playwright tương ứng** tại `tests/FE/` (`tests/FE/views/<module>/<feature>.test.ts`) để verify và bảo đảm không bị hồi quy.
  - ⛔ **CẤM** kết thúc lượt làm việc hoặc báo "đã sửa xong" mà không bổ sung bài test xác minh.
  - **Hình thức kiểm thử**:
    - **Quét tĩnh mã nguồn (Static Code / CSS Scan)**: Kiểm tra file `.vue` không có `!important`, có đầy đủ CSS styles/classes yêu cầu, không dùng toán tử `!!`, chuẩn hóa font-family, có key i18n đầy đủ trong cả `vi-vn.json` và `en-us.json`.
    - **Kiểm thử E2E / Render DOM**: Mở component/dialog, đo kích thước phần tử (`getBoundingClientRect`), kiểm tra không bị co giật/layout shift khi chuyển tab hoặc đổi trạng thái.
* **Timeout kịch bản (Rule 19.61)**: Mặc định tối đa 5s cho mỗi hành động (`waitForSelector`, `expect`), trường hợp mạng chậm chỉ tăng gia số +1 đến 2 giây.
* **Quy chuẩn bộ chọn (Selectors)**: Ưu tiên chọn theo role, aria-label, class định danh ngữ cảnh cụ thể, tránh dùng selector xpath cứng dễ vỡ.
* **Lệnh thực thi**:
  ```bash
  # Chạy toàn bộ E2E test
  pnpm test:e2e

  # Chạy test theo file cụ thể
  npx playwright test views/<module>/<feature>.test.ts --project=chromium

  # Chạy test theo kịch bản cụ thể
  pnpm test:e2e -g "EC10|EC11"
  ```

---

## 🧹 4. Tự Động Dọn Dẹp Test Artifacts Sau Khi Test Pass (Rule 19.64)

* **Tự động xóa artifacts khi Pass 100%**: Sau khi toàn bộ các bài test chạy thành công (Pass/Green), AI **BẮT BUỘC tự động dọn dẹp và xóa sạch các thư mục/tệp tạm sinh ra do test runner** (như `test-results/`, `playwright-report/`, traces, screenshots, video, log tạm...) ở cả thư mục gốc và thư mục con `tests/FE/`.
* **Giữ sạch Working Tree**: Đảm bảo trạng thái Working Tree của Git luôn sạch sẽ, không để tồn đọng các tệp rác phát sinh trong quá trình chạy test làm ô nhiễm `git status`.
* **Giữ lại artifacts khi Test Thất Bại (Fail)**: Trong trường hợp test bị fail, ĐƯỢC PHÉP giữ lại các artifacts trong `test-results/` để lập trình viên và AI phân tích nguyên nhân lỗi (traces, screenshots). Tuy nhiên, ngay sau khi sửa xong mã nguồn và chạy lại test thành công, AI **BẮT BUỘC thực thi lệnh dọn dẹp xóa bỏ thư mục `test-results/`**.
