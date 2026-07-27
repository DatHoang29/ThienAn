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
3.  **TỐI THIỂU HÓA THAY ĐỔI (MINIMAL DIFF PRINCIPLE)**: AI CHỈ ĐƯỢC PHÉP chỉnh sửa/thêm code đối với các file và nội dung thực sự phục vụ trực tiếp cho tính năng mới hoặc bug được yêu cầu. TUYỆT ĐỐI KHÔNG tự động upgrade phiên bản thư viện (`PackageReference` trong `.csproj`), không format/touch vào các file không liên quan, không làm thay đổi các file dùng chung (`Shared.Reference`, `appsettings.json`,...) trừ khi có chỉ định rõ ràng từ người dùng.
4.  **QUY ĐỊNH DOCKER SQL SERVER TRÊN MAC**: Máy tính chạy môi trường macOS (đặc biệt chip Apple Silicon M1/M2/M3/M4) **BẮT BUỘC** dùng Docker image `mcr.microsoft.com/azure-sql-edge:latest`. TUYỆT ĐỐI KHÔNG dùng `mcr.microsoft.com/mssql/server:2022-latest` vì bản x86_64 sẽ bị crash tràn bộ nhớ QEMU (`Invalid mapping of address`).
5.  **QUY ĐỊNH SWAGGER GROUPNAME KHI TẠO CONTROLLER**: Mỗi Module/Sub-module khi tạo Controller/BaseController **BẮT BUỘC** phải định nghĩa hằng số `GroupName` (Ví dụ: `GroupName = "ShareData"`) và `BasePath` (Ví dụ: `BasePath = "api/vms"`), gán attribute `[ApiDescriptionSettings(GroupName)]` để Furion/Swagger Auto-Discovery tự động phát hiện và hiển thị trên dropdown UI, tuyệt đối KHÔNG cấu hình khai báo thủ công trong `Swagger.json`.
6.  **QUY ĐỊNH ENTITY CLASS**: Tất cả các Entity class trong hệ thống bắt buộc phải kế thừa `EntityTenant` (từ `Shared.Core.Domain`).
7.  **QUY ĐỊNH HEADER COMMENT CỦA CLASS**: Mỗi Class khi tạo mới hoặc cập nhật BẮT BUỘC phải có khối XML summary comment ở đầu Class theo mẫu (Author luôn là **Đạt**):
```csharp
/// <summary>
/// [Mô tả chức năng / Tên bảng]
/// Author: Đạt
/// Created date: [dd/MM/yyyy]
/// </summary>
```

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
   * Khai báo hằng số `GroupName` và sử dụng attribute `[ApiDescriptionSettings(GroupName)]` để Swagger tự động quét nhóm API (Auto-Discovery), không tự ý chỉnh sửa thủ công file `Swagger.json`.
   ```csharp
   namespace Modules.ShareData.Controllers
   {
       /// <summary>
       /// Base Controller của Module ShareData
       /// Author: Đạt
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
     * **`Commands/`**: Chứa Handler xử lý Ghi (Add/Update/Delete). **BẮT BUỘC** implement `IWolverineHandler` và định nghĩa các hàm `HandleAsync(<InputType> command)`. Dùng `Mapster` để map DTO sang Entity.
     * **`Queries/`**: Chứa Handler xử lý Đọc (Page/GetList/GetById). Các truy vấn phân trang phải trả về `SqlSugarPagedList<Output>`, sử dụng `.OrderBuilder()` và `.ToPagedListAsync()`.
     * **`Dto/`**: Chứa DTO Input và Output:
       * Input: `PageXxxInput` (kế thừa `BasePageInput`), `AddXxxInput` (kế thừa Entity gốc), `UpdateXxxInput` (kế thừa `AddXxxInput`), `DeleteXxxInput` (kế thừa `BaseIdInput`).
       * Output: `XxxOutput` / `PageXxxOutput` (kế thừa Entity gốc).
     * **`Validators/`**: Chứa `AbstractValidator<T>` (FluentValidation) kiểm tra tính hợp lệ dữ liệu đầu vào của Add/Update/Delete.
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

---

## 🧪 6. Quy Định Thiết Kế & Viết Test (Testing Rules)

Tất cả các bài kiểm thử tự động (Integration/Unit Tests) bắt buộc tuân theo cấu trúc gọn nhẹ và quy tắc viết test sau:

### 1. Cấu Trúc Thư Mục
Dự án test nằm trực tiếp trong thư mục `tests/` của repo gốc (không lồng subfolder project):
```
tests/
├── test.csproj                            ← Project file test (Target net8.0)
├── TestHost.cs                            ← WebApplicationFactory chính (override DB, tắt Hangfire)
├── GlobalUsings.cs                        ← Chứa global using chung (Xunit, System.Net...) để tránh IDE0005
└── Modules/
    └── <TênModule>/
        └── <TênModule>Tests.cs            ← File test của module (IClassFixture<TestHost>)
```

### 2. Triết Lý & Phương Pháp Viết Test
* **Tập trung vào Happy Path**: Chỉ tập trung viết test cho các luồng chính (**Happy Path** của Queries & Commands). 
* **Gọi trực tiếp qua Wolverine `IMessageBus` (Bypass Controller/HTTP)**: 
  - **Lợi ích**: Giúp quá trình chạy test cực kỳ nhanh, bỏ qua lớp kiểm tra quyền JWT Authentication/Authorization phiền phức và **100% bắt được breakpoint** khi debug bằng VS Code (do cùng chạy trên 1 luồng xử lý chính).
  - **Cách gọi**: Inject `IMessageBus` từ `TestHost.Services` và gọi trực tiếp:
    - *Queries (Phân trang)*: `var result = await _bus.InvokeAsync<SqlSugarPagedList<OutputDTO>>(new InputDTO { ... });`
    - *Commands (Thêm/Sửa/Xóa)*: `await _bus.InvokeAsync(payload);`
* **Kiểm tra trạng thái DB trực tiếp**: Đối với các Command (Add/Update/Delete), sau khi gọi `_bus.InvokeAsync`, hãy resolve `ISqlSugarClient` từ scope của Host để query và so sánh trực tiếp dữ liệu trong DB (ví dụ: `Assert.NotNull(added)`, `Assert.Null(deleted)`).
* **Cấu trúc SqlSugarPagedList**: Đối tượng phân trang trả về là `SqlSugarPagedList<T>`, truy xuất dữ liệu danh sách qua thuộc tính **`.Records`** (kiểu `IEnumerable<T>`), không phải `.List` hay `.Rows`.
* **Cơ chế Tự Dọn Dẹp Dữ Liệu Cục Bộ (Dispose)**:
  - Lớp Test bắt buộc kế thừa **`IDisposable`**.
  - Triển khai phương thức **`Dispose()`** để tự động chạy sau mỗi hàm test, thực hiện `DELETE` tất cả các bản ghi/ID mock vừa chèn vào DB. Không drop database khi tắt host.

### 3. Quy Tắc Đặt Tên & Định Dạng
* **File test**: `<TênModule>Tests.cs` (không dùng hậu tố `IntegrationTests.cs`).
* **Class test**: `<TênModule>Tests`.
* **Namespace**: `TAC_WebAPI.IntegrationTests.Modules.<TênModule>`.
* **Tên phương thức test**: Định dạng **PascalCase**, hậu tố **Test**, **tuyệt đối không dùng dấu gạch dưới `_`**.
* **Thứ tự**: Sắp xếp các Happy Case của Queries lên trước, sau đó đến các Happy Case của Commands (ví dụ: `QueryPageReturnsSuccessTest`, `CommandAddReturnsSuccessTest`).
