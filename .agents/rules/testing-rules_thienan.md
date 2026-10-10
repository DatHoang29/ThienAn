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
3. **Bộ Kiểm Thử Hợp Nhất (Unified Test Suite - Rule 19.63 - P0)**:
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

---

## 🎭 3. Quy Chuẩn Kiểm Thử Frontend (Playwright E2E)

* **Vị trí tệp test**: Đặt tại `tests/FE/views/<module>/<feature>.test.ts`.
* **Timeout kịch bản (Rule 19.61)**: Mặc định tối đa 5s cho mỗi hành động (`waitForSelector`, `expect`), trường hợp mạng chậm chỉ tăng gia số +1 đến 2 giây.
* **Quy chuẩn bộ chọn (Selectors)**: Ưu tiên chọn theo role, aria-label, class định danh ngữ cảnh cụ thể, tránh dùng selector xpath cứng dễ vỡ.
* **Lệnh thực thi**:
  ```bash
  # Chạy toàn bộ E2E test
  pnpm test:e2e

  # Chạy test theo kịch bản cụ thể
  pnpm test:e2e -g "EC10|EC11"
  ```

---

## 🧹 4. Tự Động Dọn Dẹp Test Artifacts Sau Khi Test Pass (Rule 19.64 - P0)

* **Tự động xóa artifacts khi Pass 100%**: Sau khi toàn bộ các bài test chạy thành công (Pass/Green), AI **BẮT BUỘC tự động dọn dẹp và xóa sạch các thư mục/tệp tạm sinh ra do test runner** (như `test-results/`, `playwright-report/`, traces, screenshots, video, log tạm...) ở cả thư mục gốc và thư mục con `tests/FE/`.
* **Giữ sạch Working Tree**: Đảm bảo trạng thái Working Tree của Git luôn sạch sẽ, không để tồn đọng các tệp rác phát sinh trong quá trình chạy test làm ô nhiễm `git status`.
* **Giữ lại artifacts khi Test Thất Bại (Fail)**: Trong trường hợp test bị fail, ĐƯỢC PHÉP giữ lại các artifacts trong `test-results/` để lập trình viên và AI phân tích nguyên nhân lỗi (traces, screenshots). Tuy nhiên, ngay sau khi sửa xong mã nguồn và chạy lại test thành công, AI **BẮT BUỘC thực thi lệnh dọn dẹp xóa bỏ thư mục `test-results/`**.
