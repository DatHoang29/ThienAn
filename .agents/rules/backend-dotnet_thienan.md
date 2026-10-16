---
name: backend-dotnet
version: 1.0.0
priority: P1
trigger: model_decision
description: Backend .NET standards, Wolverine bus, SqlSugar ORM, Clean Architecture, and C# coding conventions.
---

# 🏗️ Quy Chuẩn Backend .NET & Clean Architecture — Thiên Ân

> **Thuộc hệ thống SSOT `.agents/rules/`**. Quy định kiến trúc module, Wolverine bus, SqlSugar ORM, FluentValidation và quy chuẩn C# .NET.

---

## 🏛️ 1. Cấu Trúc Module Chuẩn (Layer Separation)

> Nguồn đối chiếu của team: `TA-ITS015-WEBAPI-V1.0/.kiro/steering/structure.md`, `.kiro/steering/coding-rules.md` (Module) và `TA-ITS015-WEBAPI-V1.0/docs/PROJECT_STRUCTURE_WPF_WORKER_V3.md` (Service).

### 1.1. Thuật ngữ "phân hệ" (Module / Service)
* **Phân hệ** là một mảng nghiệp vụ: ShareData, VMS, VideoWall, TMS, VDS, Toll, CCTV, FMS, WP, Barrier, TrafficLight…
* Mỗi phân hệ có 1 hoặc cả 2 dạng triển khai:
  - **Module**: chạy trong host WebAPI `TAC_WebAPI`, đặt tại `src/Modules/<PhânHệ>/` (`Modules.<X>.Core` + `Modules.<X>`, có thể thêm `Modules.<X>.Report`).
  - **Service**: tiến trình chạy độc lập (Worker / Windows Service, WPF), đặt tại `src/Services/<PhânHệ>/`.
* Nói "phân hệ X" là gồm cả Module lẫn Service của X. Ví dụ phân hệ ShareData gồm Module `Module.ShareData*` và Service `ShareDataWorker*`.
* Không phải phân hệ: module hạ tầng dùng chung (`CfgSystem`, `DataTransporter`, `Mail`, `Media`, `Notification`, `Report`), mẫu (`Samplev2`, `Services/Sample`), thư viện chung (`src/Shared`, `src/Services/Shared`).

### 1.2. Cấu trúc Module
1. **`Modules.<X>.Core`**: chỉ chứa hợp đồng — Entity, DTO, Interface, Enum, Constant, Exception/`BaseMsg`, Options, đăng ký DI (`Extensions`). ⛔ **CẤM** đặt logic xử lý nghiệp vụ trong Core của module.
2. **`Modules.<X>`**: Controllers, Commands/Queries (Wolverine), FluentValidation, DI Extensions và **logic xử lý tại `Infrastructure/Services/<ChứcNăng>/`**. ⛔ Không tạo thư mục `Services/` ngang hàng `Infrastructure/`.
3. **Driver/giao thức với hệ thống hoặc thiết bị bên ngoài theo hãng** đặt tại `src/Modules/Integration/<PhânHệ>/` (`.Base` chứa interface, project theo hãng chứa phần cài đặt, project gốc làm manager), dùng bởi Module và/hoặc Service.

### 1.3. Cấu trúc Service
* Theo mẫu `src/Services/Sample` (V3): `Services.<X>` là thư viện lõi chứa logic dùng chung; `Services.<X>.Worker` / `Services.<X>.Wpf` là host mỏng, chỉ tham chiếu thư viện lõi; dùng `Services.Shared` / `Services.Shared.Runtime` (cấu hình nạp lại qua `IConfigLoader`, log, NATS).
* Thư viện lõi của **service** (`Services.<X>`, `ITS.<X>.Core`) **được** chứa logic — khác với Core của module ở mục 1.2.
* Service ưu tiên tham chiếu `Modules.<X>.Core` hoặc project Integration. Thư viện thuần mã (codec, formatter, connection keys/options, adapters...) dùng chung giữa module và service đặt thành project riêng `Modules.Integration.<PhânHệ>` (hoặc theo cấu trúc Integration), thuần BCL và không tham chiếu project nào có Furion/Web/SqlSugar; service **không** tham chiếu project module (chứa controller/handler). Việc service tham chiếu project module (hiện chỉ còn CCTV) là **độ lệch tạm thời**, không phải tiền lệ; phân hệ ShareData đã chuẩn hóa thành công sang `Modules.Integration.ShareData` (cho phép dùng các gói kênh truyền vận độc lập `Confluent.Kafka`, `Microsoft.Extensions.Http`, `Microsoft.Extensions.Logging.Abstractions`, vẫn cấm Furion, SqlSugar, Web, Shared.*).

### 1.4. Lệch hiện có
* Các chỗ lệch dưới đây chỉ chuẩn hóa khi có task riêng, không tự sửa kèm task khác:
  - VDS/TMS để quy tắc thuần trong Core (`Modules.VDS.Core/Topology`, `Media`, `Modules.TMS.Core/Validators`).
  - CCTV, FMS, WP dùng một project có thư mục `Core/` bên trong (mẫu cũ của `.kiro/steering`); CfgSystem có cả project `Modules.CfgSystem.Core` lẫn thư mục `Core/` trong `Modules.CfgSystem`.
  - Tên project lẫn `Module.*` / `Modules.*`; service lẫn `ITS.<X>.*` / `Services.<X>.*` / `ShareDataWorker`.

### Cấu Hình `BaseController` & Swagger Auto-Discovery
* Mỗi Module bắt buộc có `BaseController.cs` riêng tại `Modules.[TênHệ].Controllers`.
* Khai báo hằng số `GroupName` và `BasePath`, gắn `[ApiDescriptionSettings(GroupName)]`:
```csharp
using Shared.Infrastructure.Controllers;
using Wolverine;

namespace Module.ShareData.Controllers
{
    [Route(BasePath + "/[controller]")]
    [ApiDescriptionSettings(GroupName)]
    public class BaseController : CommonBaseController, ITransient
    {
        protected internal new const string BasePath = "api/sharedata";
        protected internal new const string GroupName = "ShareData";
    }
}
```

---

## ⚙️ 2. Wolverine Bus, CQRS & Validation

### Cấu Trúc Thư Mục Chức Năng (`Controllers/<ChứcNăng>/`)
* **`<ChứcNăng>Controller.cs`**: Thin Controller, **BẮT BUỘC chỉ gọi `MessBus.InvokeAsync()`**. Tuyệt đối không viết business logic tại controller.
* **`Commands/`**: Handler xử lý Ghi (Add/Update/Delete). Bắt buộc implement `IWolverineHandler` với phương thức `HandleAsync(<InputType> command)`. Dùng Mapster để ánh xạ DTO sang Entity.
* **`Queries/`**: Handler xử lý Đọc (Page/GetList/GetById). Truy vấn phân trang trả về `SqlSugarPagedList<Output>`, dùng `.OrderBuilder()` và `.ToPagedListAsync()`. Khi cần sắp xếp bản ghi mới nhất, bắt buộc dùng `BaseConst.SortFieldConst.UpdateOrCreateTime` (Rule 19.60). Trả thẳng đối tượng phân trang, **CẤM** bọc qua `.Adapt<SqlSugarPagedList<TOutput>>()`.
* **`Dto/`**:
  - Input: `PageXxxInput` (kế thừa `BasePageInput`), `AddXxxInput` (kế thừa Entity), `UpdateXxxInput`, `DeleteXxxInput`.
  - Output: `XxxOutput` / `PageXxxOutput`. Cấu hình ánh xạ Mapster (`IRegister`) viết trực tiếp trong file DTO Output, **CẤM** tạo folder `Mappings/` riêng.
  - ⛔ **CẤM dùng DataAnnotation** (`[Required]`, `[Range]`, `[StringLength]`). Toàn bộ kiểm tra dữ liệu thực hiện 100% qua FluentValidation.
* **`Validators/`**: Chứa `AbstractValidator<T>` nhận `IStringLocalizer lz` qua DI. Không tạo class phụ/mock hay constructor không tham số.
* **Repository Naming**: Đặt tên theo prefix `_rsp{EntityName}` (VD: `SqlSugarRepository<EshPartner> _rspEshPartner;`).

---

## 💾 3. Thực Thể (Entity) & SqlSugar ORM

### Quy Chuẩn Entity Class
* Tất cả Entity kế thừa `EntityTenant` (từ `Shared.Core.Domain`).
* Base class sử dụng `CreateTime` và `UpdateTime` (ID viết HOA cả hai ký tự: `ID`).
* **Hằng số độ dài `EntityConst`**: BẮT BUỘC dùng `EntityConst.Length32`, `EntityConst.Length64`, `Length128`, `Length256`, `Length512`, `KeyFieldLength`. CẤM hardcode số (như `Length = 32`).
* ⛔ **CẤM gán `Length` cho kiểu không phải chuỗi (Lỗi SQL Server 2716)**: `Length = EntityConst...` CHỈ dùng cho chuỗi (`string` / `string?`). Tuyệt đối cấm gán vào `int`, `long`, `float`, `DateTime`, `bool`.
* **Quy định `IsNullable = true`**: Tất cả cột Entity (trừ khóa chính `ID`) bắt buộc khai báo `[SugarColumn(IsNullable = true, ...)]` và dùng kiểu nullable (`string?`, `DateTime?`, `int?`...).
* **Thuộc tính Entity không ánh xạ CSDL (Rule 19.68)**: BẮT BUỘC gắn `[SugarColumn(IsIgnore = true)]`. Chi tiết xem mục 4.10.

### Khóa Chính `ID` (SnowFlake ID - Rule 19.44)
* **WebAPI**: Được AOP/Interceptors tự động sinh Snowflake ID khi Insert. **CẤM tự gán tay `entity.ID`** trong WebAPI.
* **Background Worker**: BẮT BUỘC gán thủ công `entity.ID = Yitter.IdGenerator.YitIdHelper.NextId()` trước khi insert.

### Quy Tắc Thao Tác SqlSugar (P0)
1. **Ưu tiên ORM tuyệt đối khi đã có Entity (Rule 19.20)**: TUYỆT ĐỐI CẤM dùng raw SQL DML (`UPDATE`, `INSERT`, `DELETE`) thủ công.
2. **CẤM nối chuỗi SQL (Rule 19.17)**: Khi bắt buộc dùng SQL truy vấn động, BẮT BUỘC dùng tham số hóa qua `SugarParameter`.
3. **SqlSugar Client trong Worker (Rule 19.41)**: Trong Background Worker, Polling Loop hoặc Concurrent Tasks, BẮT BUỘC khởi tạo bản copy độc lập:
   ```csharp
   using var db = baseClient.CopyNew();
   ```
4. **Không tự dựng tính năng có sẵn (Rule 19.33)**: SqlSugar đã có API sẵn (`InsertReturnIdentity`, `UpdateColumns`, `SplitTable`...) thì BẮT BUỘC dùng, cấm tự viết lại.

---

## 🧹 4. Quy Chuẩn Code C# (Clean Code Standards)

1. **Zero Unnecessary Usings (IDE0005)**:
   - Kiểm tra `GlobalUsings.cs` trước khi thêm `using`. Nếu namespace đã có trong `GlobalUsings.cs`, **TUYỆT ĐỐI CẤM** thêm vào file riêng lẻ.
   - Luôn rà soát và dọn dẹp các `using` thừa sau khi sửa code.
2. **Không Dùng Hậu Tố `-Async` (No-Async Suffix)**:
   - Mọi phương thức trả về `Task` hoặc `Task<T>` đặt tên trực diện: `GetData()`, `ExecuteCommand()`, `ProcessQueue()`.
   - ⛔ KHÔNG đặt tên `GetDataAsync()`, `ExecuteCommandAsync()`. (Ngoại lệ: implement interface từ thư viện ngoài như Wolverine `HandleAsync`).
3. **Ưu tiên `var` cho biến cục bộ (IDE0007 / Rule 19.39)**:
   - Dùng `var` khi kiểu dữ liệu hiển nhiên hoặc có thể suy luận rõ ràng: `var entity = new EshPartner();`.
4. **Auto-Property & Cấm Backing Field Thừa (Rule 19.22)**:
   - Dùng auto-property: `public string Name { get; set; }`. Cấm tách `private string _name` nếu không có logic phụ trợ đặc thù.
5. **Xử lý Exception Tập Trung (Rule 19.19, 19.48)**:
   - ⛔ CẤM `try-catch` dồn dập, lồng nhau nhiều lớp.
   - ⛔ CẤM nuốt lỗi ở hàm con / helper (`catch { return null; }`). Để exception ném tự nhiên lên orchestrator / tầng cha điều phối để log và rollback giao dịch.
6. **Thứ Tự Thành Viên Class (Rule 19.47)**:
   - Cấu trúc theo nhóm: Constants/Fields tĩnh -> Fields/Dependencies DI -> Constructor -> Public Methods -> Protected/Internal Methods -> Private Helper Methods.
   - Khi thêm code mới, thêm vào cuối nhóm tương ứng (Append-only).
7. **Tôn Trọng Phong Cách Mã Nguồn & Signature (Rule 19.49, 19.50, 19.51)**:
   - Cấm tự ý đổi signature phương thức / service dùng chung.
   - Cấm sinh hàm mới làm hàm cũ mồ côi (dead code).
8. **Vị Trí `using` & Quy Chuẩn `namespace` (Rule 19.65)**:
   - **Vị trí `using` luôn đứng đầu file**: Mọi chỉ thị `using` **BẮT BUỘC đặt ở đầu tệp tin**, phía trước khai báo `namespace`. ⛔ **TUYỆT ĐỐI CẤM** đặt `using` sau khai báo `namespace` (kể cả khi dùng file-scoped namespace).
   - **Đồng nhất kiểu `namespace`**: Tuân thủ phong cách namespace của module / file gốc hiện hữu. Khi tạo file mới hoặc tách `partial class`, **BẮT BUỘC đồng nhất** với các file cùng class/module: nếu các file hiện tại dùng block-scoped namespace (`namespace X { ... }`) thì file mới bắt buộc dùng block-scoped namespace, ⛔ **CẤM** viết lẫn lộn file-scoped namespace (`namespace X;`) trong cùng một partial class.
9. **Can Thiệp Tối Thiểu & Cấm Sửa Code Ngoài Phạm Vi (Minimal Diff - Rule 19.6, 19.35)**:
   - Chi tiết xem [thienan_rules.md](./thienan_rules.md) bảng P0 quy tắc #13.
10. **Xóa Triệt Để Mã Không Dùng & Thuộc Tính Entity Không Ánh Xạ (Rule 19.68)**:
    - Đối với code C# thông thường (service, helper, util, DTO, class, method, handler...): Khi không dùng hoặc đã được thay thế bằng giải pháp mới, **BẮT BUỘC XÓA BỎ HOÀN TOÀN (Clean Delete)** khỏi solution.
    - ⛔ **TUYỆT ĐỐI CẤM** để lại mã rác rồi gắn thuộc tính `[Obsolete]` làm bẩn codebase và sinh warning trình biên dịch (CS0618).
    - Mọi test case kiểm thử mã cũ cũng phải được xóa bỏ hoặc thay thế tương ứng bằng bài test cho giải pháp mới.
    - **Thuộc tính Entity không ánh xạ CSDL**: Nếu Entity class có thuộc tính phụ trợ, tính toán, trạng thái runtime hoặc tạm thời không lưu trữ trong database, **BẮT BUỘC gắn `[SugarColumn(IsIgnore = true)]`** để SqlSugar bỏ qua không sinh/truy vấn cột trên bảng CSDL, tránh lỗi lệch schema (`Invalid column name`).
11. **Chuẩn Hóa Thông Báo Lỗi & Ngoại Lệ — Cấm Hardcode String Trong `Oops.Oh` (Rule 19.69)**:
    - ⛔ **TUYỆT ĐỐI CẤM** hardcode chuỗi string trực tiếp (tiếng Việt hoặc tiếng Anh) trong `Oops.Oh("...")` hay `throw new AppFriendlyException("...")`.
    - **Định danh thông báo qua `BaseMsg`**: Mọi thông báo lỗi, thông điệp kiểm tra dữ liệu (validation), thông báo ngoại lệ nghiệp vụ (exception) **BẮT BUỘC** khai báo tập trung trong `BaseMsg` của module (kế thừa `BaseLocaleManager`) theo tiền tố chuẩn:
      - `lz.validation.<module>.<mãKey>` (hoặc `BaseMsg.BaseValidation.*`): Dùng cho lỗi kiểm tra dữ liệu đầu vào.
      - `lz.exception.<module>.<mãKey>` (hoặc `BaseMsg.BaseException.*`): Dùng cho lỗi logic nghiệp vụ, vi phạm trạng thái.
      - `lz.entity.<module>.<mãKey>` (hoặc `BaseMsg.BaseEntity.*`): Dùng cho tên trường, tên thực thể phục vụ ghép vào placeholder `{0}`, `{1}`.
    - **Tham số hóa thông điệp qua Furion**: Khi thông báo cần chèn tham số động (phạm vi, tên đối tượng, mã lỗi), sử dụng placeholder `{0}`, `{1}` trong CSDL và truyền đối số vào `Oops.Oh`:
      ```csharp
      throw Oops.Oh(BaseMsg.ShareData.Validation.LookbackDaysRange, 1, 365);
      ```
    - **Cấu hình trên CSDL `SysTerminology`**: Mọi key thông báo mới **BẮT BUỘC** tạo script SQL idempotent (`DocBusinessThienAn/**/sql/*.sql`) để seed vào bảng `SysTerminology` trên CSDL với đầy đủ 2 ngôn ngữ (`Lang = 'vi-VN'` và `Lang = 'en-US'`), `Status = 1`:
      - `Name`: Tiền tố nhóm (`lz.validation`, `lz.exception`, `lz.entity`...).
      - `Code`: Mã phân cấp sau tiền tố (ví dụ `sharedata.lookbackDaysRange`, `cctv.deviceNotFound`).
      - `Value`: Nội dung thông điệp (hỗ trợ tham số hóa qua placeholder `{0}`, `{1}`...).
    - **Cấm sửa tay file tĩnh Backend**: ⛔ **TUYỆT ĐỐI CẤM** sửa tay vào `TAC_WebAPI/Resources/*.json` vì các file này được sinh tự động (auto-generated) từ CSDL `SysTerminology` qua endpoint `LoadServerTerm`.
12. **CẤM Tự Ý Thêm Project & Solution Folder Dư Thừa Vào Solution (`.sln`) (Rule 19.71)**:
    - Solution file chính (`src/TAC_WebAPI.sln`) là tài nguyên dùng chung của toàn hệ thống WebAPI.
    - ⛔ **TUYỆT ĐỐI CẤM** tự ý thêm các project không thuộc phạm vi phân hệ đang làm (như thêm nhầm VideoWall, các project dịch vụ ngoài, hoặc test project `tests/BE/test.csproj`) vào file solution WebAPI.
    - ⛔ **TUYỆT ĐỐI CẤM** tạo các Solution Folder trung gian dư thừa không cần thiết (như tạo thêm Solution Folder lẻ trong khi project có thể lồng trực tiếp vào folder gốc của module, ví dụ lồng thẳng vào `Modules`).
    - Khi bắt buộc phải thêm project của phân hệ vào solution (ví dụ `Modules.Integration.<PhânHệ>`):
      - Chỉ thêm đúng project cần thiết phục vụ build/chạy WebAPI.
      - Phải đăng ký đầy đủ và chính xác trong cả 3 khối: `Project(...)`, `ProjectConfigurationPlatforms`, và `NestedProjects` (trỏ đúng thư mục cha chuẩn, ví dụ `Modules`).
      - Không để sót cấu hình rác hoặc mapping mồ côi.
13. **Quy Chuẩn Đặt Tên Kiểu Mới Trong Phân Hệ — Tránh Lặp Tiền Tố (Rule 19.73)**:
    - Khi kiểu mới đã nằm trong namespace hoặc thư mục của phân hệ (ví dụ `Modules.Integration.ShareData`, `Modules.ShareData` hoặc `ShareData/`): **TUYỆT ĐỐI KHÔNG lặp lại tiền tố của phân hệ ở tên kiểu mới**.
      - Ví dụ chuẩn: Viết `FormatContext` thay vì `ShareDataFormatContext`, `PayloadEncrypt` thay vì `ShareDataPayloadEncrypt`.
    - **Phạm vi áp dụng**: Bao gồm class, interface (`IConnectionHandler`, không phải `IShareDataConnectionHandler`), enum, record, DTO và tên file (tên file = tên kiểu chính).
    - **Tiền tố cần tránh**: Tên phân hệ (`ShareData`, `VMS`, `TMS`...) khi namespace/project đã mang tên phân hệ (`Module.ShareData*`, `Modules.Integration.ShareData`, `ShareDataWorker*`).
    - **Ngoại lệ (giữ nguyên)**:
      - Các kiểu đã có ở `HEAD` (bảo toàn class/kiểu cũ để tránh xáo trộn codebase và giảm thiểu diff).
      - Controller (route, Swagger và client FE sinh theo tên, nên theo các `ShareData*Controller` hiện có).
      - Tên thư mục giữ nguyên theo các thư mục cũ cùng cấp (ví dụ `ShareDataExtraction/`, `ShareDataMetadata/`).
    - **Trùng tên kiểu khác trong solution**: Đặt tên mô tả khác, tuyệt đối không thêm lại tiền tố phân hệ (ví dụ đặt `PayloadDirection` vì `Direction` đã có ở `Shared.DTO/Enums/BaseEnums.cs`).
    - **Kiểm trước khi bàn giao**: Liệt kê khai báo kiểu mới trong `git diff HEAD` có tiền tố phân hệ → danh sách phải rỗng (ngoại trừ controller).
14. **Quy Chuẩn Ngắt Dòng Khi Gọi Hàm / Phương Thức Nhiều Tham Số (Rule 19.75)**:
    - Khi gọi hàm/phương thức có từ 3 tham số trở lên, hoặc tham số biểu thức dài/phức tạp, hoặc sử dụng Named Arguments (`paramName: value`):
      - **BẮT BUỘC xuống dòng ngay sau dấu mở ngoặc `(`**.
      - Mỗi đối số nằm riêng trên một dòng, thụt lề 1 cấp (1 tab hoặc 4 spaces).
      - **Dấu đóng ngoặc `);` nằm riêng trên một dòng mới**, thẳng hàng với dòng bắt đầu gọi phương thức.
      - ⛔ **TUYỆT ĐỐI CẤM** viết mở ngoặc `(` rồi dồn ngay tham số đầu tiên trên cùng dòng mà các tham số sau lại ngắt dòng lộn xộn.
      - Ví dụ chuẩn:
        ```csharp
        // CHUẨN:
        var parameters = SqlParameterBuilder.BuildParameters(
            lastTime: isSnapshot ? null : (command.FromTime ?? fromDate),
            lastKey: string.Empty,
            pageSize: safeTop,
            fromDate: fromDate,
            toDate: DateTime.Now
        );

        // ❌ CẤM (dồn dòng sau dấu ngoặc mở):
        var parameters = SqlParameterBuilder.BuildParameters( lastTime: isSnapshot ? null : (command.FromTime ?? fromDate),
            lastKey: string.Empty, pageSize: safeTop, fromDate: fromDate, toDate: DateTime.Now );
        ```
15. **Cấm Bí Danh Thừa & Hàm Bọc Mỏng Chỉ Chuyển Tiếp Sang Lớp Khác (Rule 19.78)**:
    - ⛔ **TUYỆT ĐỐI CẤM** hai dạng mã thừa không thêm ý nghĩa:
      - **Bí danh**: field/property/hằng chỉ gán lại thành viên của lớp khác.
      - **Hàm bọc mỏng**: hàm chỉ có một dòng `return Other.Method(args);` (kể cả đổi tên, đổi thứ tự tham số) mà không thêm logic, kiểm tra hay chuyển đổi.
    - Lý do: thêm một chỗ phải theo dõi, dễ lệch khi nguồn đổi, buộc người đọc đi qua hai lớp mới thấy logic thật, và sinh thêm hàm mồ côi khi đổi nguồn.
    - **Cách đúng**: tham chiếu thẳng nguồn gốc tại nơi cần dùng.
      ```csharp
      // CHUẨN:
      .Must(v => ConnectTypes.IsSupported(v))
      .Must(v => ConnectConfigValidator.Check(v) != ConnectConfigIssue.EmptyKey)

      // ❌ CẤM (bí danh):
      public static readonly HashSet<string> ColumnReservedKeys = ConnectConfigValidator.ColumnReservedKeys;

      // ❌ CẤM (hàm bọc mỏng):
      public static bool IsValidConnectionType(string type)
      {
          return ConnectTypes.IsSupported(type);
      }
      public static ConnectConfigIssue Check(string? configStr)
      {
          return ConnectConfigValidator.Check(configStr);
      }
      ```
    - **Ngoại lệ (phải ghi lý do bằng comment tại chỗ khai báo)**:
      - Đổi kiểu hoặc đóng gói để che chi tiết nội bộ (ví dụ trả `IReadOnlySet<string>` thay cho `HashSet<string>`).
      - Giữ tương thích ngược cho API công khai có nơi dùng bên ngoài, đã kiểm chứng bằng `find_usages`.
      - Biến cục bộ trong hàm để rút gọn biểu thức dài (không phải field/hàm của lớp).
      - Hàm thực hiện một phần việc thật: kiểm tra đầu vào, ghép nhiều lời gọi, xử lý ngoại lệ, đổi kiểu dữ liệu, ghi log.
    - **Kiểm trước khi bàn giao**: với mỗi field/hàm mới trong `git diff HEAD`, dùng `find_usages` và đọc thân hàm; nếu chỉ chuyển tiếp sang lớp khác và không thuộc ngoại lệ thì xóa và sửa nơi gọi dùng thẳng nguồn gốc.
    - Liên quan: Rule 19.68 (xóa mã không dùng), Rule 19.33 (không dựng lại tính năng có sẵn), Rule 19.49-51 (cấm sinh hàm mới làm hàm cũ mồ côi).

