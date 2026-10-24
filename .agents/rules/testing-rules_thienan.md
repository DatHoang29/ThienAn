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
4. **Chỉ viết test mang giá trị — chống thừa test (bổ sung Rule 19.10, 24/10/2026)**:
   - Mỗi test đi qua **điểm vào của một luồng nghiệp vụ** và kiểm **kết quả nghiệp vụ** (dữ liệu CSDL, gói tin gửi đi, cảnh báo, mốc đã gửi, lỗi trả về người dùng):
     - API: `CallApiAsync` / `IMessageBus.InvokeAsync(input)` như controller (§5.3).
     - Worker: phương thức public Worker gọi cho một lượt nghiệp vụ. Mẫu chuẩn: `DataOutboundService.ProcessSubscriptions` — một lượt gửi trọn vẹn (đọc đăng ký → trích SQL → ánh xạ → gửi → ghi mốc).
     - Tầng kết nối: API công khai của `DeliveryDispatcher` gửi/nhận thật tới máy chủ giả lập.
   - ⛔ **CẤM test dạng unit**: gọi thẳng helper / static / reader / validator / enum / handler (`PacketHelper`, `SqlValidator`, `ConnectConfigReader`, `XxxHandler.HandleAsync`…), kiểm hằng số, kiểm "resolve được từ DI", kiểm lại đúng câu lệnh vừa viết. Hành vi của helper được kiểm gián tiếp qua luồng dùng nó.
   - ⛔ **CẤM test không thêm giá trị**: trùng kịch bản với test khác (cùng nhánh, chỉ khác dữ liệu mà kết quả như nhau), chỉ kiểm hành vi thư viện/framework, chỉ assert "không lỗi / khác null" khi đã có test kiểm mạnh hơn cùng luồng.
   - **Cùng một luồng, khác dữ liệu → MỘT `[Theory]` + nhiều `[InlineData]`**, không viết nhiều hàm `[Fact]` giống nhau. FE (Playwright): một mảng dữ liệu + vòng `for` sinh test.
   - **FE**: test chạy màn hình thật (`page.route` giả lập API → `page.goto` → thao tác → kiểm điều người dùng thấy / payload gửi đi). ⛔ Không viết test chỉ đọc mã nguồn `.vue`/`.ts` rồi so chuỗi; kiểm tĩnh chỉ dùng cho thứ khó thấy khi chạy màn hình (khóa dịch đủ ở vi-vn và en-us, `@` chưa thoát trong giá trị dịch).
   - **Câu tự hỏi trước khi viết / giữ một test**: *"Xóa test này thì lỗi nào của luồng nghiệp vụ lọt qua mà không test nào khác bắt?"* Không trả lời được → không viết.
   - ❌ **Sai:** `PacketHelperTests.TryGetPacketNumber_ValidCodes_...` gọi thẳng hàm tách mã gói; năm hàm `[Fact]` chỉ khác câu SQL đầu vào cho cùng một lỗi validator.
   - ✅ **Đúng:** một ca trong `DataOutboundServiceTests` gọi `ProcessSubscriptions` rồi kiểm gói tin tới máy chủ giả lập và mốc `ShareDataLastSend`; một `[Theory]` lưu SQL qua API với các `[InlineData]` câu SQL không an toàn.

---

## ⚙️ 2. Quy Chuẩn Kiểm Thử Backend (xUnit & Local Database)

### An Toàn CSDL Test Local (Section 11)
* Mọi bài test backend BẮT BUỘC chạy trên CSDL SQL Server **Local** (`localhost` / `127.0.0.1` / `Host.GuardAllConnectionsLocal`).
* ⛔ **CẤM TUYỆT ĐỐI** trỏ test vào DB staging hoặc production.
* ⛔ **CẤM cờ `--no-build`**: Khi chạy test qua CLI, luôn dùng `dotnet test tests/BE/test.csproj` (không thêm `--no-build`).

### Cấm Thư Viện Mock Ngoài (No-Moq Rule - Section 15 & Rule 19.18)
* ⛔ **CẤM DÙNG Moq, NSubstitute, FakeItEasy** hoặc các thư viện mock bên ngoài.
* ⛔ **CẤM mock Service nội bộ & NATS**: Toàn bộ service nghiệp vụ (`DataOutboundService`, `DataInboundService`...) và bus tin nhắn nội bộ bắt buộc lấy bản thật từ `host.Services` bằng tên lớp cụ thể (Rule 19.94); ⛔ không tạo interface chỉ để thay lớp giả trong test.
* **Ngoại lệ duy nhất được giả lập**: Máy chủ HTTP của đối tác bên thứ ba bên ngoài, và BẮT BUỘC dùng mock server thật bằng `HttpListener` trên `127.0.0.1` (dùng `MockHttpClientFactoryTest` trong `tests/Mock/`).

### Cấu Trúc Test Chuẩn AAA (Rule 19.43, 19.46)
* **Khai báo đầu hàm**: Service và dependencies khởi tạo ở khối Arrange (đầu hàm).
  ```csharp
  [Fact]
  public async Task ProcessData_FullFlow_Success_Test()
  {
      // 1. Arrange: Khởi tạo service, seed data với Unique ID
      var service = host.Services.GetRequiredService<DataOutboundService>();
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
* **Ưu tiên tuyệt đối SqlSugar ORM trong bài test & helper test**: Mọi thao tác truy vấn/cập nhật trên bảng Entity CSDL bắt buộc dùng SqlSugar ORM (`Queryable<T>()`, `Insertable<T>()`, `Updateable<T>()`, `Deleteable<T>()` kèm `.ClearFilter()` khi cần đọc/xử lý bản ghi đã xóa mềm); ⛔ **CẤM** viết câu lệnh SQL thuần `db.Ado.ExecuteCommandAsync("DELETE/UPDATE FROM ...")` hoặc `db.Ado.GetIntAsync("SELECT COUNT(1) FROM ...")` trên bảng Entity (ngoại trừ lệnh DDL hệ thống như `ALTER TABLE ... CHANGE_TRACKING` hoặc truy vấn bảng/hàm hệ thống SQL Server như `sys.change_tracking_tables`, `CHANGETABLE(...)`, `SELECT GETDATE()`).

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
* **Ưu tiên sửa code trước — Chỉ viết test khi có yêu cầu (Rule 19.67 - Fix-First & On-Demand Testing)**:
  - **Tập trung sửa dứt điểm code trước (Fix-First)**: Khi người dùng báo lỗi/vấn đề (UI/UX, CSS, font chữ, layout shift, logic, tính năng nhỏ...), AI **BẮT BUỘC tập trung xử lý và sửa mã nguồn dứt điểm trước**, báo cáo rõ ràng kết quả để người dùng review trực quan ngay trên môi trường dev.
  - ⛔ **CẤM** tự ý dừng luồng làm việc để ngồi viết thêm các test case Playwright / unit test mới cho mỗi lần sửa lặt vặt làm chậm tiến độ.
  - **Chỉ viết test khi người dùng yêu cầu rõ ràng (On-Demand Testing)**: CHỈ viết thêm / cập nhật bài test khi người dùng có chỉ thị rõ ràng (ví dụ: *"hãy viết test"*, *"bổ sung test"*, *"viết kịch bản kiểm thử"*), hoặc khi triển khai các luồng nghiệp vụ lớn có yêu cầu test suite đi kèm.
  - **Chạy regression test sẵn có**: Được phép chạy nhanh các bài test đã có sẵn theo phạm vi sửa đổi (filter test theo scope) để đảm bảo không làm gãy logic cũ, nhưng tuyệt đối không tự ý viết thêm test mới khi chưa được yêu cầu.
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

---

## 🗂️ 5. Tệp Test Bám Đúng Luồng Nghiệp Vụ Chính — Không Tự Tạo Tệp Mới (Rule 19.84)

1. **Một tệp test ↔ một lớp/luồng nghiệp vụ có thật trong mã nguồn**:
   - Controller → `tests/BE/ITS/<Module>/Controllers/<TênController>Tests.cs` (ví dụ `ShareDataSubscriptionControllerTests` cho `ShareDataSubscriptionController`).
   - Service / Worker / lớp hạ tầng → `tests/BE/ITS/<Module>/Services/<TênLớp>Tests.cs` (ví dụ `DataOutboundServiceTests` cho `DataOutboundService`).
   - Luồng toàn trình nhiều thành phần → đặt theo tên luồng nghiệp vụ đã có (ví dụ `ShareDataSendReceiveTests`), không mở luồng trùng ý nghĩa.
   - FE → `tests/FE/views/<module>/<màn hoặc khối chức năng>.test.ts`, theo tệp đã có của màn hình/hộp thoại lớn (ví dụ `partner.test.ts` cho hộp thoại đối tác); component con kiểm trong tệp của khối cha (ví dụ form cấu hình kết nối của hộp thoại đối tác → `partner.test.ts`).
2. **Thêm vào tệp có sẵn, không tạo mới**:
   - Trước khi viết test, BẮT BUỘC tìm tệp test sẵn có của lớp/luồng (`search_text` tên lớp, tên controller trong `tests/`) rồi thêm case vào đó.
   - Tệp quá dài thì tách `partial class` theo nhánh nghiệp vụ của CHÍNH lớp đó: `<Tên>Tests.<NhánhNghiệpVụ>.cs` (ví dụ `DataOutboundServiceTests.Cursor.cs`).
   - Chỉ tạo tệp mới khi mã nguồn có lớp/luồng mới chưa có tệp test; báo cáo rõ lớp nguồn tương ứng.
   - ⛔ **CẤM** đặt tên tệp theo tính năng con, đợt sửa, mã prompt hay mã hạng mục (`PacketMetadataTests`, `PacketTemplateTests`, `...FixTests`); ⛔ **CẤM** hai tệp cùng kiểm một lớp nguồn.
3. **Đi qua đúng điểm vào của luồng thật**:
   - API: gọi `IMessageBus.InvokeAsync(input)` đúng như controller (đi qua validator và middleware của pipeline). Chỉ được gọi validator trực tiếp khi riêng nhánh lỗi của validator là thứ cần kiểm.
   - Worker: gọi phương thức public của service đúng như Worker gọi (`ProcessSubscriptions`, `ProcessPendingPackets`, `PollChanges`…).
   - ⛔ **CẤM** gọi thẳng handler hay hàm nội bộ để bỏ qua pipeline; ⛔ **CẤM** dựng lại logic production trong helper test (hàm bọc chuyển tiếp, bản sao SQL/danh sách).
4. **Dữ liệu tiền đề dựng bằng đúng nguồn production dùng**:
   - Dùng seed class, script seed của phân hệ hoặc chính lệnh Add qua bus để tạo đối tác, gói tin, đăng ký, ánh xạ…
   - Kết quả mong đợi so với nguồn thật (seed class, bảng cấu hình, script), ⛔ **CẤM** so với hằng hoặc danh sách tự chép vào test.
5. **Ví dụ đối chiếu**:
   - ❌ **Sai:** tạo `PacketMetadataTests.cs` riêng, lấy `PacketExtractSqlQueryHandler` từ DI rồi gọi `HandleAsync` trực tiếp, so số bảng với `MetadataService.AllowedTables`.
   - ✅ **Đúng:** thêm case vào `ShareDataPacketControllerTests` (hoặc partial `ShareDataPacketControllerTests.Metadata.cs`), gọi `IMessageBus.InvokeAsync<List<MetadataTableDto>>(new GetMetadataTablesInput())` như controller, so với danh mục bảng nguồn thật trên CSDL test.
6. **Lý do**: test là bằng chứng luồng nghiệp vụ chạy đúng; tệp rải theo đợt sửa làm trùng lặp, khó tìm, và test gọi tắt bỏ qua pipeline có thể xanh trong khi luồng thật vẫn lỗi.

---

## ⏱️ 6. Chạy Test Theo Phạm Vi Sửa Để Tiết Kiệm Thời Gian (Rule 19.92, bổ sung 18/10/2026)

Bối cảnh: cả bộ test ShareData (677 test) mất khoảng 13 phút (12 phút 52 giây, đo ngày 20/10/2026 sau khi tối ưu phân trang); chạy cả bộ sau mỗi lần sửa nhỏ làm chậm vòng sửa–kiểm.

1. **Sau khi sửa mã, kiểm chứng theo phạm vi đã sửa trước**, ⛔ không mặc định chạy cả bộ.
2. **Xác định phạm vi** (gộp cả hai nguồn, vì đồ thị Gortex có thể thiếu cạnh từ test — đã gặp: báo 0% phủ cho Delivery trong khi có hơn 100 test):
   - Gortex: `change(operation:"detect")` → dùng symbol ID trả về cho `change(operation:"tests")` (hoặc `get_test_targets`).
   - `search_text` tên lớp/hàm đã sửa trong `tests/` để lấy các lớp test còn thiếu.
3. **Lệnh chạy theo lớp test**:
   ```bash
   dotnet test tests/BE/test.csproj -f net10.0 --filter "FullyQualifiedName~<LớpTest1>|FullyQualifiedName~<LớpTest2>"
   ```
   FE: chạy đúng tệp spec đã liên quan (`npx playwright test tests/FE/views/<module>/<tệp>.test.ts`).
4. **Mở rộng sang cả bộ của phân hệ** (`--filter FullyQualifiedName~<PhânHệ>`) khi sửa phần dùng chung rộng: entity/DTO/enum dùng chung, đăng ký DI, cấu hình (`appsettings*`, `SysOpConfig`), project Core của phân hệ, `Shared.*`, script SQL/seed, hoặc test theo phạm vi lộ lỗi lan sang lớp khác.
5. **Trước commit/push**: chạy cả bộ test của phân hệ **một lần**, phải pass 100% (Pre-Commit Checklist #5 trong git-workflow). Chạy theo phạm vi chỉ dùng trong vòng sửa–kiểm.
6. **Trước khi chạy** (mọi phạm vi): kiểm không có phiên test khác (`pgrep -fl "dotnet test|testhost"`, cổng mock `lsof -nP -iTCP:18090-18093 -sTCP:LISTEN`); có thì chờ, ⛔ không chạy song song, không kill tiến trình của người dùng.
7. **Báo cáo** ghi rõ lệnh `--filter` đã dùng, số test chạy/pass/fail/skip, và nói rõ là chạy **theo phạm vi** hay **cả bộ**.
