# 📌 Quy Định Chung Phòng Phần Mềm: Nhánh, Commit, Kiến Trúc & Vận Hành AI

Tài liệu này định nghĩa quy trình đặt tên nhánh (branching workflow), luồng phát triển, định dạng thông
điệp commit (commit messages), kiến trúc module, quy tắc viết test và các rule mandatory vận hành AI
— áp dụng cho toàn bộ thành viên trong dự án và cho AI khi làm việc trong repo.

> ⚖️ **Mức độ ưu tiên**: Toàn bộ rule trong file này có độ ưu tiên **CAO HƠN** `.agents/rules/universal-rules.md`
> và mọi rule generic khác từ AG-Kit upstream khi có xung đột nội dung. Nhờ vậy, `universal-rules.md`
> (và `code-rules.md`) có thể được **thay thế trực tiếp bằng bản mới nhất** mỗi khi AG-Kit có update,
> mà không cần lo mất customization của dự án — vì mọi phần riêng của ThienAn đã nằm hết trong file này.

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
`[Keyword]: [TaskCode (nếu có)] - [noi-dung-cong-viec-dung-cau-hanh-dong]`
*(Ví dụ: `feat: XD1.2.2.5 - add map location` hoặc `fix: fix map location`)*

> [!TIP]
> Nên thống nhất một ngôn ngữ chung (tiếng Việt hoặc tiếng Anh) xuyên suốt dự án. Tên công việc viết thường, có thể dùng dấu gạch ngang hoặc tiếng Việt có dấu tùy quy định nhóm.

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
fix!: XD1.2.2.7 - thay đổi luồng gửi mail chức năng A module A


- Thay đổi luồng thứ tự nhân sự duyệt cho phép gửi mail
- Bổ sung cấu hình thiết lập thời gian timeout
- Bỏ bớt code dư thừa

Reviewer: SonTH
CR: CR0001-thay-doi-luong-gui-mail
```

---

### 🏭 Ngoại Lệ Định Dạng Commit Cho Dự Án ThienAn (Nhánh WebAPI / WebVue)

Riêng khi commit trên nhánh thuộc 2 repo `TA-ITS015-WEBAPI-V1.0` và `TA-ITS015-WEBVUE-V1.0`, dòng Summary
**KHÔNG** dùng cú pháp `[Keyword]: [TaskCode] - [noi-dung]` ở mục 3 trên, mà dùng LẠI NGUYÊN VĂN tên nhánh
(đúng Cú Pháp 2 ở mục 1) làm dòng Summary — tức `[BranchKey]/[yyyyMMdd]-[TaskCode]-[ten-cong-viec-viet-thuong-khong-dau]`.
Phần Description chi tiết vẫn liệt kê từng dòng công việc đã thực hiện trong lần commit, **KHÔNG** cần dấu
gạch đầu dòng `-` như cấu trúc chung.

*Ví dụ:*
```text
feat/20260826-XD001.5.5-Service-tich-hop-du-lieu

Hoàn thiện api share data
Cấu hình lưu chuẩn json db
```

---

## 🚫 4. Quy Định Tự Động Hóa Đối Với Trợ Lý AI (AI Execution Rules)

Đối với Trợ lý AI, tuyệt đối tuân thủ các nguyên tắc sau khi làm việc trong dự án:

1.  **KHÔNG TỰ ĐỘNG CHẠY LỆNH BUILD (`dotnet build`)**: AI không được tự động chạy lệnh `dotnet build` hoặc bất kỳ lệnh biên dịch nào sau khi chỉnh sửa code, trừ khi người dùng yêu cầu trực tiếp.
2.  **KHÔNG TỰ ĐỘNG COMMIT VÀ PUSH GIT (`git commit` / `git push`)**: AI không được tự động chạy `git add`, `git commit`, hay `git push` code lên repository dưới bất kỳ hình thức nào. Quyền commit và push code hoàn toàn thuộc về lập trình viên.
3.  **TỐI THIỂU HÓA THAY ĐỔI (MINIMAL DIFF PRINCIPLE)**: AI CHỈ ĐƯỢC PHÉP chỉnh sửa/thêm code đối với các file và nội dung thực sự phục vụ trực tiếp cho tính năng mới hoặc bug được yêu cầu. TUYỆT ĐỐI KHÔNG tự động upgrade phiên bản thư viện (`PackageReference` trong `.csproj`), không format/touch vào các file không liên quan, không làm thay đổi các file dùng chung (`Shared.Reference`, `appsettings.json`,...) trừ khi có chỉ định rõ ràng từ người dùng.
4.  **PHÂN BIỆT THAM KHẢO VÀ HÀNH ĐỘNG (DISTINGUISH REFERENCE FROM ACTION)**: Khi người dùng yêu cầu "tham khảo", "xem thử", "giải thích" hoặc hỏi ý kiến, AI BẮT BUỘC phải phân tích và trả lời thảo luận trước, KHÔNG ĐƯỢC tự ý nhảy vào áp dụng hoặc thêm/sửa code khi chưa có xác nhận từ người dùng.
5.  **TÔN TRỌNG CODE SỬA TAY & Ý ĐỊNH NGƯỜI DÙNG (PRESERVE USER MANUAL EDITS & PREFERENCES)**: Khi người dùng đã chỉ định cách viết (VD: dùng `while (reader.Read())` đồng bộ) hoặc tự sửa tay/bỏ bớt điều kiện, AI KHÔNG ĐƯỢC TỰ Ý hoàn tác (revert) hoặc sửa ngược lại về cách viết cũ trong các lần refactor tiếp theo.
6.  **GIỮ NGUYÊN THUẬT NGỮ TIẾNG ANH CHUYÊN NGÀNH (KEEP TECHNICAL ENGLISH KEYWORDS AS-IS)**: Các từ tiếng Anh mang tính chất thuật ngữ kỹ thuật, tên thuộc tính, tên tham số giao thức (protocol/API), tên tính năng, hoặc keyword nghiệp vụ (như `Probe`, `Ping`, `WallNo`, `Video Wall`, `Outputs`, `Inputs`, `SubWindow`, `Scene`, `Preset`, `Payload`, `Endpoint`, `Path Parameters`, `Body Parameters`, `Advanced Parameters`, `Digest Auth`, `Circuit Breaker`...) BẮT BUỘC giữ nguyên tiếng Anh gốc, TUYỆT ĐỐI KHÔNG dịch gượng ép sang tiếng Việt (như dịch `Probe` thành "khảo sát", `WallNo` thành "tường số", `Video Wall` thành "tường ghép", `SubWindow` thành "cửa sổ con"...) gây tối nghĩa, nhập nhằng và khó đối chiếu với tài liệu/spec chuẩn.

> [!NOTE]
> - Tự động dọn dẹp file kế hoạch tạm: xem mục 13 "Prompt & Plan File Location Rule" bên dưới (bullet Auto-cleanup).
> - Các quy chuẩn code/hạ tầng chung của dự án (Docker, Entity, Swagger, header comment...) áp dụng cho **cả người lẫn AI** — xem tại mục 5 bên dưới, không lặp lại ở đây để tránh trùng lặp nội dung.
> - **Plan Mode (4-Phase) của `code-rules.md`**: bước 4 IMPLEMENTATION của dự án ThienAn có thêm yêu cầu so với bản gốc AG-Kit — **BẮT BUỘC chạy lại toàn bộ test liên quan + bổ sung test case mới** cho code/UI/logic mới tạo (không chỉ "Code + tests" chung chung).

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
   * **Bản thiết kế Entity gốc là nguồn sự thật (Source of Truth)**: Khi có thư mục `EntityUpdate` hoặc bất kỳ bộ Entity gốc nào được đưa vào từ team thiết kế, đó là bản thiết kế chính thức. AI BẮT BUỘC phải đồng bộ entity trong code hiện tại theo ĐÚNG cấu trúc, kiểu dữ liệu, tên property, và attribute của bản thiết kế gốc.

7. **Quy định Header Comment của Class & XML Doc**:
   * Mỗi Class khi tạo mới hoặc cập nhật BẮT BUỘC phải có khối XML summary comment ở đầu Class theo mẫu (chỉ dùng `Created date:`, KHÔNG dùng `Author:` — quyết định 05/09/2026, không hồi tố class đã có sẵn `Author: Đạt` — và KHÔNG dùng `Updated date:`):
     ```csharp
     /// <summary>
     /// [Mô tả chức năng / Tên bảng]
     /// Created date: [dd/MM/yyyy]
     /// </summary>
     ```
   * **Cấm lặp khối XML summary**: TUYỆT ĐỐI KHÔNG tự ý chèn chồng hoặc nhân bản các khối `/// <summary>` rườm rà trên cùng một class/hàm/property. Mỗi đối tượng code CHỈ ĐƯỢC CÓ DUY NHẤT 1 khối `/// <summary>`. Khi cập nhật nội dung comment, BẮT BUỘC sửa trực tiếp vào khối comment cũ thay vì thêm khối `/// <summary>` thứ 2.
   * **API Controller Action Summary**: Trên mỗi phương thức Action trong Controller, comment XML Doc `/// <summary>` BẮT BUỘC mô tả rõ ràng, tự nhiên ý nghĩa và chức năng thực tế của hàm (VD: `/// <summary>\n/// Lấy danh sách cảnh báo & lỗi (phân trang)\n/// </summary>`). Tuyệt đối KHÔNG chèn mã prefix/số thứ tự rườm rà (như L1., E2., DS3...). Thẻ `[DisplayName("...")]` giữ nguyên tên hiển thị chuẩn.

8. **Quy định Docker SQL Server trên Mac**: Máy tính chạy môi trường macOS (đặc biệt chip Apple Silicon M1/M2/M3/M4) **BẮT BUỘC** dùng Docker image `mcr.microsoft.com/azure-sql-edge:latest`. TUYỆT ĐỐI KHÔNG dùng `mcr.microsoft.com/mssql/server:2022-latest` vì bản x86_64 sẽ bị crash tràn bộ nhớ QEMU (`Invalid mapping of address`).
9. **Quy định Primary Constructor ([IDE0290](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0290)) (đã chốt 05/09/2026)**: Chỉ áp dụng C# Primary Constructor khi **VIẾT CLASS MỚI** (ví dụ: `public class MyService(ILogger<MyService> Logger, IConfiguration Configuration) : IMyService`). Đối với **CLASS CŨ ĐÃ TỒN TẠI** đang dùng constructor tường minh kèm field private thủ công → KHÔNG sửa, KHÔNG refactor sang primary constructor, giữ nguyên style cũ để tránh diff không cần thiết.
10. **Quy định Structured Logging ([CA1873](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1873))**: LUÔN dùng structured logging message template (VD: `_logger.LogInformation("Processing {Id} for {Partner}", id, partner)`) thay vì string interpolation (VD: `_logger.LogInformation($"Processing {id} for {partner}")`) hoặc tính toán trước các biểu thức tốn kém (`string.Join(...)`, `.Count()`, LINQ...) ngay trong tham số log. Kiểm tra `_logger.IsEnabled(...)` trước khi chuẩn bị dữ liệu log tốn kém để tránh cấp phát bộ nhớ và tốn CPU không cần thiết khi logging đang tắt.
11. **Quy định Tự Động Xóa Using Thừa ([IDE0005](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005))**: Sau mỗi lần tạo mới hoặc chỉnh sửa file code C#, **BẮT BUỘC** phải rà soát và xóa bỏ tất cả các chỉ thị `using ...;` không còn sử dụng hoặc bị trùng lặp với `GlobalUsings.cs` (CS0105 / IDE0005) để giữ mã nguồn gọn gàng và không sinh cảnh báo build.
12. **Quy định vòng đời DI cho `IVwISAPIDeviceService` (đã chốt 05/09/2026)**: Đăng ký theo vòng đời **`IScoped`** (`VwISAPIDeviceService : IVwISAPIDeviceService, IScoped`) — đây là chỉ đạo trực tiếp của chủ dự án, không phải Singleton.
13. **Quy định Đa Ngôn Ngữ & Dịch Thuật Module (`BaseMsg`)**:
    * Tất cả các Module có sử dụng dịch thuật BẮT BUỘC tạo file `Core/Exceptions/BaseMsg.cs` kế thừa `BaseLocaleManager` (từ `Shared.DTO.Constants.Localization`).
    * Trong `BaseMsg`, tạo các class đại diện cho từng Chức năng/Entity (VD: `EshPartner`, `EshDataSource`...). Trong mỗi class chức năng, chia thành các class con chứa hằng số dịch thuật: `Validation`, `Message`, `Exception`, `Entity` (group action).
    * **Vị trí thư mục Resources**: Thư mục `Resources` nằm ngang hàng với `Controllers`, `Core`, `Extensions`, `Infrastructure` trong root project của Module (VD: `Modules.ShareData/Resources/vi-VN.json`). KHÔNG đặt bên trong thư mục `Controllers`. Dịch thuật được cập nhật đồng bộ vào `src/TAC_WebAPI/Resources/` để hệ thống load đầy đủ.
14. **Quy định Quét SqlSugar CodeFirst (`inherit: false`)**:
    * Khi quét entity để tạo bảng qua CodeFirst (`InitTables`), BẮT BUỘC dùng `t.IsDefined(typeof(SugarTable), inherit: false)` để DTO kế thừa Entity (`AddXxxInput : EntityBase`, `PageXxxOutput : EntityBase`) không bị nhận nhầm và tự tạo bảng.

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
* **Cấm Gọi InitTables<T>() Thủ Công Trong Host/Test — Bật Cờ CodeFirst Trong Cấu Hình Test**: TUYỆT ĐỐI KHÔNG tự tiện viết các lệnh gọi `db.CodeFirst.InitTables<T>()` thủ công rải rác trong `Host.cs`, `Host.<Module>.cs` hay các file test để sinh/đồng bộ bảng. Thay vào đó, BẮT BUỘC chỉ cấu hình bật các cờ SqlSugar CodeFirst trong `InMemoryTestConfigurations` (ví dụ: `TableSettings:EnableInitTable = "true"`, `TableSettings:EnableIncreTable = "true"`). Khi bật các cờ này, hệ thống sẽ tự động quét toàn bộ Entity từ code gốc và tự động tạo bảng / bổ sung cột tăng dần (incremental column sync) đúng chuẩn.
* **Tự Động Chạy Lại Test & Bổ Sung Test Case Mới**: Bất cứ khi nào tạo mới hoặc chỉnh sửa code (C#, XAML, ViewModel, Service, Handler, Controller, API...), thêm mới UI, hoặc sửa đổi logic nghiệp vụ/giao diện: AI **BẮT BUỘC** (1) chạy lại toàn bộ bài test liên quan (`dotnet test ...`) để đảm bảo 100% pass, không hồi quy/gãy build; (2) viết bổ sung test case mới nếu tính năng/logic mới chưa có test bao phủ (chuẩn AAA, mock I/O HTTP/thiết bị, đặt tên file/thư mục mirror 1-1). Nhiệm vụ chưa được coi là hoàn thành nếu thiếu 1 trong 2 bước trên.
  - **Dồn test về cuối khi đang trao đổi dồn dập**: Nếu đang trong chuỗi hỏi-đáp/sửa nhanh liên tiếp và test suite chạy chậm (VD ~60s+), KHÔNG chạy lại test sau MỖI lần sửa nhỏ — dồn thay đổi liên quan lại, chỉ chạy 1 lần ở cuối trước khi báo hoàn tất. Vẫn chạy ngay nếu người dùng hỏi trực tiếp kết quả test, thay đổi đủ rủi ro cần xác nhận ngay, hoặc rõ ràng không còn quyết định nào khác đang chờ.

### 3. Quy Tắc Đặt Tên & Định Dạng
* **File test**: `<TênModule>Tests.cs` (không dùng hậu tố `IntegrationTests.cs`).
* **Class test**: `<TênModule>Tests`. Test class, test helper/mock/stub class và test method BẮT BUỘC dùng `Test`/`Tests` làm **hậu tố** (VD: `StringLocalizerTest`, `VwControllerTests`); TUYỆT ĐỐI KHÔNG dùng `Test` làm tiền tố (như `TestStringLocalizer`).
* **Comment XML Summary Bắt Buộc Trên Mọi Phương Thức & Class**: Mọi Class, Constructor, Helper Method và phương thức kiểm thử (`[Fact]` / `[Theory]`) BẮT BUỘC có comment XML `/// <summary>` theo định dạng chuẩn 2 dòng (KHÔNG dùng `Author:` — quyết định 05/09/2026, không hồi tố test đã có sẵn `Author: Đạt`):
  ```csharp
  /// <summary>
  /// Description: [Mô tả chi tiết chức năng / Helper / Test case]
  /// Created date: DD/MM/YYYY
  /// </summary>
  ```
  *(BỎ HẲN và KHÔNG DÙNG field `Updated date:`)*.
* **Namespace**: `Tests` (gốc) và `Tests.Modules.<Module>.<ĐườngDẫnCon>` (ví dụ: `Tests.Modules.VideoWall.MockServer`).
* **Tên phương thức test**: Sử dụng dấu gạch dưới **`_`** để phân tách các phần trong tên phương thức theo định dạng `Feature_Scenario_ExpectedResult` hoặc `Feature_Scenario_ExpectedResult_Test` (ví dụ: `CronJob_SavedQuery_SqlGeneration_Test`, `PartnerQuery_GetById_ReturnsSuccess_Test`).
* **Thứ tự**: Sắp xếp các Happy Case của Queries lên trước, sau đó đến các Happy Case của Commands (ví dụ: `QueryPageReturnsSuccessTest`, `CommandAddReturnsSuccessTest`).

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
3. **Kiểm tra Connection String trước khi `dotnet test`:**
   > Xem mục 11 "Strict Local Database Rule for Testing" bên dưới.

---

## 🧹 7. Clean Code — Bổ Sung (áp dụng ngay cả khi `universal-rules.md` được thay bằng bản AG-Kit mới)

- **No Hardcoded Magic Strings**: Không viết literal chuỗi cứng (mã trạng thái, tên state...) trực tiếp trong query/logic điều kiện nghiệp vụ. LUÔN định nghĩa và dùng Enum hoặc Constant có kiểu rõ ràng (VD: `ShareDataEnum.IncidentState`).
- **Formatting (Single-Statement `if` Without Braces)**: Đối với câu lệnh `if` chỉ chứa 1 dòng thực thi (dù điều kiện `if` nằm trên 1 dòng hay nhiều dòng `&&`/`||`), BẮT BUỘC ngắt dòng và thụt lề cho câu lệnh thực thi. TUYỆT ĐỐI KHÔNG viết inline trên cùng 1 dòng (`if (condition) return;`) và TUYỆT ĐỐI KHÔNG TỰ Ý THÊM cặp dấu ngoặc nhọn `{}` khi code hiện hữu đang viết theo chuẩn single-statement không có ngoặc nhọn.
- **Object Initializer Formatting**: Object initializer nhiều thuộc tính (VD: `new TmsEquipment { ID = eqId, Code = "...", ... }`) BẮT BUỘC ngắt dòng, mỗi thuộc tính 1 dòng thụt lề. TUYỆT ĐỐI KHÔNG viết inline nhiều thuộc tính trên 1 dòng ngang.
- **Multi-Condition LINQ Formatting**: Query LINQ/SqlSugar nhiều điều kiện (VD: `.Where(s => s.IsDelete == null && s.Direction == ... && s.Mode != ...)`) BẮT BUỘC ngắt dòng — hoặc tách thành các `.Where(...)` nối tiếp (mỗi điều kiện 1 dòng), hoặc xuống dòng thụt lề cho từng vế `&&`/`||`. TUYỆT ĐỐI KHÔNG viết chuỗi điều kiện dài inline trên 1 dòng.
- **Async Method Naming**: KHÔNG thêm hậu tố `Async` vào tên phương thức bất đồng bộ (VD: `ProcessBatchSubscriptions`, không phải `ProcessBatchSubscriptionsAsync`) vì kiểu trả về (`Task`/`Task<T>`) đã thể hiện rõ tính bất đồng bộ. Áp dụng NGANG NHAU cho cả private test helper/seed method (VD: `SeedWall` KHÔNG phải `SeedWallAsync`).
- **Dependency Injection Naming & Casing**:
  - Với constructor viết tường minh (không phải primary constructor kiểu property): LUÔN đặt tên dependency injected bằng camelCase (VD: `IFileExportService fileExportService`), gán vào private field `_fileExportService = fileExportService;`.
  - Với Primary Constructor khi viết class mới (khi dependency đóng vai trò public read-only property): BẮT BUỘC viết hoa chữ cái đầu (PascalCase) (VD: `public class MyService(ILogger<MyService> Logger, IOutboundService OutboundService) : IMyService`).
- **Không Sử Dụng `#region`**: KHÔNG tự ý chèn thẻ `#region` hoặc `#endregion` vào code C# trừ khi người dùng yêu cầu. Giữ biến/field nguyên bản và sạch sẽ.
- **Using Directives Thay Vì Inline Namespaces**: BẮT BUỘC dùng `using` directive ở đầu file (VD: `using Modules.ShareData.Core.Entities;`) để gọi tên class ngắn gọn (VD: `EshPartner`) thay vì gõ namespace dài inline trong code (VD: `Core.Entities.EshPartner`).
- **DTO vs Anonymous Objects**: Dữ liệu CÓ xử lý logic nội bộ → tạo DTO. Dữ liệu CHỈ map để gửi đi (bên khác xử lý) → dùng Anonymous Object (hoặc Dictionary).
- **Null Reference (CS8601)**: Luôn gán giá trị dự phòng (`?? string.Empty`) khi gán `string?` cho `string` để dập cảnh báo CS8601.
- **Cấu hình ASP.NET Core**: Ưu tiên `config.GetConnectionString("Default")` thay vì truy vấn key phân cấp thô (`config["DbConnection:ConnectionConfigs:0:ConnectionString"]`).
- **C# / .NET CA2263**: LUÔN ưu tiên `Enum.IsDefined<TEnum>(value)` dạng generic (hoặc `Enum.IsDefined(enumValue)` từ .NET 7+) thay vì bản non-generic `Enum.IsDefined(typeof(TEnum), value)` để tránh boxing và overhead reflection không cần thiết.
- **Class Member & Helper Ordering (Private Helpers at Bottom)**: Trong mọi class/service/handler C#, toàn bộ phương thức `private` (helper, private async method...) và nested helper class/struct BẮT BUỘC đặt ở **CUỐI CÙNG của class/file**, sau toàn bộ phương thức `public`. TUYỆT ĐỐI KHÔNG đặt hàm private xen kẽ ở đầu hoặc giữa các public handler method.
- **Vue SFC Section Ordering**: Trong mọi file `.vue`, thứ tự khối BẮT BUỘC: (1) `<script setup lang="ts">` đầu tiên, (2) `<template>` thứ hai, (3) `<style scoped>` cuối cùng. TUYỆT ĐỐI KHÔNG đặt `<template>` trước `<script>`.
- **Vue `<script setup>` Internal Structure**: Bên trong `<script setup>` sắp xếp theo thứ tự: Imports → Props/Emits/Models → Reactive State & Stores → Computed & Watchers → Lifecycle Hooks → Methods & Event Handlers → Expose.

---

## 🔒 8. SQL & Module Isolation Scope (Mandatory Rule)

- **Strict Module Scope**: Mọi script SQL (DDL & DML) sinh ra hoặc cập nhật cho 1 module CHỈ được tác động lên đúng danh sách bảng thuộc phạm vi sở hữu của module đó (VD module `ShareData`: `EshPartner`, `EshDataSource`, `EshMappingProfile`, `EshFieldMapping`, `EshSubscription`, `EshExportLog`, `EshSystemLog`, `EshEventSource`).
- **Cấm tác động bảng ngoài phạm vi**: TUYỆT ĐỐI KHÔNG `CREATE`, `ALTER`, `DROP`, `INSERT`, `UPDATE`, `DELETE` lên bảng thuộc module khác (VD `TmsTrafficData`, `TmsWeather`, `TmsIncident`, `TollTransactionOut`...).

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
- **Nơi lưu chuẩn (bắt buộc trong repo)**:
  - Prompt/plan theo domain nghiệp vụ → `DocBusinessThienAn/<Dự-án>/<Module>/` (VD VideoWall: `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/`).
  - Prompt/plan hạ tầng/tooling/AG-Kit (không thuộc domain nghiệp vụ) → `.agents/prompts/`.
- **Đặt tên**: `<task-slug>-prompt.md` hoặc `<task-slug>-plan.md` (kebab-case, tiếng Việt không dấu hoặc tiếng Anh).
- **Chế độ Plan (ExitPlanMode)**: Nếu harness ép ghi plan vào `~/.claude/plans/`, ngay sau khi plan được duyệt BẮT BUỘC sao chép vào đúng thư mục repo ở trên và coi bản trong repo là bản chính thức; báo người dùng đường dẫn trong repo, không phải `~/.claude/plans/`.
- **Auto-cleanup**: Xoá file prompt/plan trong repo sau khi task hoàn tất (khớp `*-prompt*.md`, `*-plan.md`, `{task-slug}.md`, và ghi chú tạm `.agents/memory/*-scratch.md` — xem mục 14 bên dưới).

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
- **BẮT BUỘC TỰ ĐỘNG XOÁ CUỐI TASK**: khi task hoàn tất (đã có kết quả cuối và đã báo cáo cho người dùng), AI **tự động xoá toàn bộ** `.agents/memory/*-scratch.md` cùng mọi dòng index trỏ tới chúng — **không hỏi lại**, không giữ "cho lần sau". Việc này nằm cùng nhóm với auto-cleanup file prompt/plan và thư mục artifact tạm (`tests/TestResults/`, `bin`/`obj` tạm do AI sinh ra).
- **Cần lại thì đo lại**: lần sau gặp cùng vấn đề thì chạy lại và viết ghi chú mới, TUYỆT ĐỐI KHÔNG tin số liệu trong bản scratch cũ.
- **Muốn giữ lâu dài thì đặt đúng chỗ, không nhét vào scratch**: nội dung thường trú → memory chuẩn trong `.agents/memory/` (có index trong `MEMORY.md`); hướng dẫn thao tác thuộc một khu vực code cụ thể → README của khu vực đó (ví dụ cách chạy test → `tests/README.MD`).

---

## 🛑 15. Quy Tắc Viết Test, Vị Trí Thư Mục & Cấm Dùng Thư Viện Mock Ngoài (Testing Standards & No-Moq Rule [Mandatory Rule])

- **Toàn bộ test tập trung tại `c:\ThienAn\tests\` (`test.csproj` cấp root)**:
  - Dự án chỉ có DUY NHẤT một project test tập trung là `c:\ThienAn\tests\` (`test.csproj`).
  - Cấu trúc test theo module: `tests\Modules\<ModuleName>\` (ví dụ: `tests\Modules\VideoWall\Controllers\`, `tests\Modules\VideoWall\Infrastructure\Services\`).
- **CẤM TẠO PROJECT HOẶC THƯ MỤC TEST MỚI TRONG CÁC SUB-DIRECTORY**:
  - TUYỆT ĐỐI KHÔNG tạo thư mục test, sub-folder hay file `.csproj` test mới bên trong `TA-ITS015-WEBAPI-V1.0\tests\`, trong `src\Modules\...`, hay bất kỳ vị trí nào khác ngoài `c:\ThienAn\tests\`.
  - Mọi file test mới (unit test, integration test, validator test, fixture test) của bất kỳ phân hệ nào BẮT BUỘC phải viết trực tiếp vào thư mục tương ứng bên trong `c:\ThienAn\tests\Modules\<ModuleName>\`.
- **CẤM DÙNG THƯ VIỆN MOCK BÊN NGOÀI (`Moq`, `NSubstitute`, `FakeItEasy`)**:
  - `test.csproj` **hoàn toàn KHÔNG tham chiếu thư viện Moq**.
  - **CẤM `using Moq;`**, **CẤM `new Mock<T>()`**.
  - **TUYỆT ĐỐI KHÔNG tự tiện suy diễn** các thư viện phổ biến bên ngoài khi chưa mở file `test.csproj` để kiểm tra `PackageReference`.
- **Quy Chuẩn Viết Test Trong Dự Án (xUnit + Host Pattern)**:
  - **Dependency Injection qua Host**: Mọi test class dùng cấu trúc `[Collection("api")] public class ...Tests(Host host)`.
  - **Lấy Localizer**: Dùng trực tiếp `private readonly IStringLocalizer _localizer = host.Localizer;` (KHÔNG mock `IStringLocalizer`).
  - **Lấy Services**: Dùng `host.Services.GetRequiredService<T>()`.
  - **Giả lập thiết bị**: Dùng mock server nội bộ đã được cấu hình trong repo (như `host.MockServer` / `VwISAPIMockServerHikvision`).
  - **Test cô lập không qua Host (POCO/DTO/XML/JSON/Formula)**: Viết test xUnit thuần. Khi cần fake interface, tự viết **class Stub nội bộ** kế thừa interface đó hoặc dùng `NullLogger<T>.Instance`, TUYỆT ĐỐI KHÔNG dùng thư viện mock.
- **Global Usings của Module Test**:
  - Bổ sung namespace/using của module vào `tests\Modules\<ModuleName>\GlobalUsings.<ModuleName>.cs` để tránh xung đột với các module khác và đảm bảo compile condition theo `test.csproj`.

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

- **Bắt buộc cấu trúc 2 phần khi xử lý file ghi âm cuộc họp**:
  1. **Tóm tắt tổng quan & Quyết định kỹ thuật / nghiệp vụ cốt lõi (Executive Summary)**: Nêu bật các kết luận, hành động cần làm, thay đổi kiến trúc hoặc quy ước dữ liệu đã chốt trong cuộc họp.
  2. **Toàn văn nội dung đối thoại (Full Verbatim Transcript)**: BẮT BUỘC trình bày dạng bảng chi tiết gồm đủ 3 cột:
     - **Mốc thời gian (Timestamp)**: Định dạng chuẩn `mm:ss` (hoặc `hh:mm:ss`) bám sát dòng thời gian của file ghi âm.
     - **Người nói (Speaker)**: Phân định rõ ràng từng người tham gia (VD: `Người 1`, `Người 2` kèm vai trò/ngữ cảnh nếu xác định được).
     - **Lời thoại chi tiết**: Ghi lại nguyên văn nội dung trao đổi, không cắt gọt cụt lủn hay tự ý giản lược thoại đối đáp.
- **Quy chuẩn lưu trữ file transcript**: Khi tạo mới hoặc cập nhật tài liệu transcript trong thư mục dự án (như `doc/transcript/*.md`), phần bảng toàn văn đối thoại (kèm mốc thời gian và định danh người nói) BẮT BUỘC phải được đưa vào tài liệu (thường đặt ở mục cuối cùng).

---

## 📎 Ghi chú mở — cần xác minh / còn trùng lặp

- **`GlobalUsings.cs` tối thiểu (mục 5.5)**: liệt kê gồm `Shared.Core.Domain` và `System.Linq.Dynamic.Core`, nhưng `src/Modules/VideoWall/Module.VideoWall/GlobalUsings.cs` **không có** 2 dòng này, lại có `Furion.ConfigurableOptions`, `Furion.DynamicApiController`, `Newtonsoft.Json`, `Microsoft.Extensions.Options`, `System.ComponentModel.DataAnnotations`. Cần rà thêm các module khác (WP, TMS, ShareData) rồi chốt lại danh sách tối thiểu cho đúng.
- **Dependency Injection Naming — casing**: xem ghi chú ngay tại bullet tương ứng ở mục 7 — camelCase (constructor tường minh) vs PascalCase (Primary Constructor khi đóng vai trò public property) áp dụng cho 2 ngữ cảnh khác nhau, dễ nhầm khi đọc lướt.
- **Trùng lặp với file steering AG-Kit — chưa xử lý**: danh mục agent/skill/workflow/script (lặp với `quick-reference.md`, `code-rules.md`, `request-routing.md`); đường dẫn `.agents/...` (lặp với `core-protocol.md` mục *Path Awareness*).
