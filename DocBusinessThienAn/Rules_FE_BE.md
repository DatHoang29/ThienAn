# 📌 BỘ QUY CHUẨN KỸ THUẬT BACKEND VÀ FRONTEND (THIÊN ÂN)
> **Nguồn gốc:** Tổng hợp từ thảo luận kiến trúc thực tế (Transcript `Street No. 8.m4a`), bộ quy chuẩn Kiro (`TA-ITS015-WEBAPI-V1.0/.kiro/steering`, `TA-ITS015-WEBVUE-V1.0/.kiro/steering`), và hệ thống SSOT `.agents/rules/`. Từ 20/10/2026 nội dung Kiro đã tích hợp đủ vào `.agents/rules/backend-dotnet_thienan.md` và `frontend-vue_thienan.md` (bản sao `DocBusinessThienAn/KiroR/` đã xóa); khi lệch, theo `.agents/rules/`.

---

# 🏛️ PHẦN 1: BỘ QUY CHUẨN BACKEND (.NET 10 / C#)

## 1. Kiến trúc Tổng thể & Nguyên tắc Tinh gọn (Lean Architecture)
- **Framework nền tảng:** .NET 10, ASP.NET Core, Furion 4.9, Lamar IoC, Wolverine 5.x (CQRS), SqlSugar ORM, FusionCache + Redis, FluentValidation 12.x, Mapster.
- **Tư tưởng cốt lõi (Từ Transcript):**
  - **Tránh over-engineering:** Không phân mảnh mã nguồn, không tự ý tạo các lớp Base riêng rẽ cho từng tính năng.
  - Giữ luồng gọi phẳng và tập trung để giảm thiểu tối đa chi phí **Refactor và Debug**.

---

## 2. Quy tắc Vàng: Triệt Tiêu Interface 1:1 (Furion DI)
*(Quyết định kiến trúc trọng tâm từ trao đổi của Lead trong Transcript)*

- ⛔ **CẤM tạo Interface 1:1 cho từng Service nghiệp vụ:** Không tạo kiểu `public interface ITmsEquipmentService` chỉ để phục vụ duy nhất một class `public class TmsEquipmentService`.
  - *Lý do:* Debug rất rối (bấm F12 trỏ vào interface rỗng rồi phải tìm implementation), sửa đổi một phương thức phải cập nhật cả 2 nơi, phải tốn công map đăng ký DI thủ công và gây khó khăn khi viết test/refactor.
- ✅ **Cơ chế thay thế:** Tận dụng 100% cơ chế quét tự động của **Furion Dependency Injection**:
  ```csharp
  // Kế thừa trực tiếp IScoped, ITransient hoặc ISingleton của Furion
  namespace Modules.TMS.Infrastructure.Services.Equipment
  {
      public class TmsEquipmentService : IScoped
      {
          private readonly BaseRepository<TmsEquipment> _repo;
          public TmsEquipmentService(BaseRepository<TmsEquipment> repo)
          {
              _repo = repo;
          }

          public async Task ProcessBusinessLogicAsync(long id) { ... }
      }
  }
  ```
- 🎯 **Ngoại lệ duy nhất được dùng Interface:**
  1. Áp dụng các Design Pattern thực thụ: **Factory Pattern**, **Strategy Pattern**.
  2. Một Interface có **từ 2 implementations trở lên**.
  3. External API clients (Refit: `IBaseApi`, `IEquipmentApi`) hoặc Contracts dùng cho cross-module communication.

---

## 3. Cấu trúc Phân cấp Base, Exception & Error Handling
*(Tránh tình trạng "mỗi người tạo một Base riêng làm mất đồng nhất")*

### 3.1. Phân cấp Base kế thừa chuẩn
```
Shared (Toàn hệ thống)
  └── Modules.{Name} (Phân hệ)
        └── Feature (Chức năng cụ thể)
```
- **Base toàn hệ thống:** `CommonBaseController`, `SqlSugarRepository<T>`, `BaseLocaleManager`.
- **Base cấp Module:**
  - `BaseController.cs`: Định nghĩa `BasePath = "api/{moduleLower}"` và `GroupName = "{ModuleName}"`.
  - `BaseRepository<T> : SqlSugarRepository<T> where T : class, new()`.
- ⛔ **CẤM** tạo các file Base ad-hoc rải rác bên trong từng thư mục Feature.

### 3.2. Ngoại lệ & Thông báo lỗi
- Dùng **Furion FriendlyException** (`Oops.Oh`).
- ⛔ **TUYỆT ĐỐI CẤM hardcode chuỗi string** trực tiếp trong `throw Oops.Oh("...")`.
- BẮT BUỘC dùng định danh qua `BaseMsg` kế thừa từ `BaseLocaleManager`:
  ```csharp
  // Kiểm tra trùng lặp
  throw Oops.Oh(BaseMsg.BaseException.Exist, BaseMsg.BaseEntity.Code);

  // Không tìm thấy dữ liệu
  throw Oops.Oh(BaseMsg.BaseException.DataNotExist, BaseMsg.BaseEntity.ID);
  ```

---

## 4. Quy chuẩn Entity & Chia sẻ Dữ liệu (Shared Core)
- **Tiền tố Module bắt buộc (Prefix Convention):**
  | Module | Prefix | Table/Class Name | Route Base |
  | :--- | :---: | :--- | :--- |
  | Traffic Management (TMS) | `Tms` | `TmsEquipment`, `TmsIncident` | `api/tms` |
  | Variable Message Sign (VMS) | `Vms` | `VmsImage`, `VmsTemplate` | `api/vms` |
  | Fleet Management (FMS) | `Fms` | `FmsVehicle`, `FmsFuel` | `api/fms` |
  | System Configuration | `Cfg` | `CfgParameter`, `CfgDictionary` | `api/cfgSystem` |
- **Quy tắc đặt tên:** Bắt đầu bằng tiền tố module (`Tms...`), **KHÔNG cần thêm hậu tố `Entity`** đằng sau (vì tiền tố đã đảm bảo tính duy nhất và dễ search).
- **Dữ liệu dùng chung:** Nếu một Entity hoặc cấu trúc được từ 2 module trở lên sử dụng, **bắt buộc xả xuống `Shared.DTO` hoặc `Shared/Core.Entities`**, không để cục bộ tại một module con.
- **Base Entity:** Kế thừa `: EntityTenant` (tích hợp sẵn Snowflake ID, Code, TenantId, CreateTime, UpdateTime, CreateUId...).
  > ⚠️ **Lưu ý:** Với WebAPI, ID được AOP tự động sinh Snowflake; không gán tay `entity.ID = ...` trừ khi chạy trong Background Worker.

---

## 5. Base Helper Trung Gian (Tiện ích dùng chung)
*(Người lập trình không cần nhớ cụ thể từng phương thức nội bộ, chỉ cần gọi qua các Helper trung gian)*

Mọi thao tác dùng chung đều được đóng gói tại `Shared.Utility.Helpers.BaseHelper`:
- `BaseHelper.FileHp`: Đọc/ghi/xóa file có cơ chế retry (`OpenReadWithRetry`, `DeleteWithRetry`), format storage path/web path, MIME type.
- `BaseHelper.NetworkHp`: Kiểm tra kết nối IP/Port (`CheckConnect("192.168.1.1", 200)`).
- `BaseHelper.ImageHp`: Chuyển đổi URL sang Base64 (`ToBase64FromUrl`).
- `BaseHelper.ExcelHp`: Xuất danh sách Excel theo template (`ExportByTemplate`, `ExportList`).
- `BaseHelper.RetryHp`: Thực thi delegate với cơ chế exponential backoff (`RetryOnException`).
- `BaseHelper.EChartHp`: Sinh danh sách ngày/giờ đồng bộ cho biểu đồ (`GetListDay`, `GetListHour`).

---

## 6. Xử lý CQRS (Wolverine) & Timezone
- **Controller:** Đóng vai trò Dispatcher mỏng, đẩy message qua `MessBus.InvokeAsync`:
  ```csharp
  [HttpPost("page")]
  public async Task<SqlSugarPagedList<PageEquipmentOutput>> Page([FromBody] PageEquipmentInput input)
  {
      return await MessBus.InvokeAsync<SqlSugarPagedList<PageEquipmentOutput>>(input);
  }
  ```
- **Phân tách Handler vs Service:**
  - *CRUD cơ bản (Insert, Update, Delete, Select):* Xử lý trực tiếp trong `CommandHandler` / `QueryHandler`.
  - *Nghiệp vụ phức tạp / Logic dùng lại:* Bóc tách ra Service (`IScoped`).
- **Thời gian & Múi giờ (Bắt buộc):**
  - Frontend gửi thời gian dạng UTC+0. Khi nhận `DateTime` từ input, **bắt buộc convert qua server time**:
    ```csharp
    DateTime? localTime = BaseUtil.TimeServer.GetTimeServer(command.StartTime);
    ```
  - CẤM dùng `DateTime.Now` trực tiếp $\rightarrow$ Bắt buộc dùng `BaseUtil.TimeServer.GetTimeServer()`.

---
---

# 🎨 PHẦN 2: BỘ QUY CHUẨN FRONTEND (VUE 3 / TYPESCRIPT / ELEMENT PLUS)

## 1. Cấu trúc Thư mục & Quy tắc Đặt tên Component
- **Cấu trúc thư mục:**
  ```
  views/{module}/{featureName}/           # Tên feature viết theo camelCase
  ├── index.vue                           # Trang chính (Grid / Search form)
  └── component/                          # Thư mục chứa component con
      ├── edit{Feature}.vue               # Dialog / Drawer Thêm mới & Cập nhật (camelCase)
      └── detail{Feature}.vue             # Dialog chi tiết (nếu có)
  ```
- ⛔ **QUY CHUẨN ĐẶT TÊN TỆP COMPONENT:**
  - Tên tất cả tệp `.vue` trong `component/` **BẮT BUỘC viết theo camelCase (chữ cái đầu viết thường)**: ví dụ `editEquipment.vue`, `detailVehicle.vue` (CẤM đặt PascalCase như `EditEquipment.vue`).
  - Thuộc tính `name` trong `<script setup>` cũng đồng bộ camelCase: `<script lang="ts" setup name="tmsEquip">`.

---

## 2. Hệ Thống Màu CSS & Giao Diện Dark/Light
*(Trích từ thảo luận Transcript về chuẩn màu, font và SCSS)*

### 2.1. Chuẩn hóa 5 biến màu ngữ nghĩa của Element Plus
⛔ **TUYỆT ĐỐI CẤM HARDCODE MÃ MÀU HEX/RGB** (`#fff`, `#1578a3`, `#f56c6c`, `red`...). Phải dùng 100% CSS Variables:

| Ngữ nghĩa | CSS Variable | Ứng dụng |
| :--- | :--- | :--- |
| **Primary** | `var(--el-color-primary)` | Nút action chính, link, text highlight, trạng thái active |
| **Success** | `var(--el-color-success)` | Hoạt động bình thường, trực tuyến, thành công |
| **Warning** | `var(--el-color-warning)` | Cảnh báo, chờ duyệt, kết nối chập chờn |
| **Danger** | `var(--el-color-danger)` | Lỗi, báo động, nút xóa, mất kết nối |
| **Info** | `var(--el-color-info)` | Thông tin phụ trợ, trạng thái nhàn rỗi / disable |

- **Biến thể sáng/tối (Tự động thích ứng theme):**
  - Nhạt dần: `var(--el-color-primary-light-3)` đến `var(--el-color-primary-light-9)`
  - Đậm hơn: `var(--el-color-primary-dark-2)`
  - Text & Nền: `var(--el-text-color-primary)`, `var(--el-bg-color)`, `var(--el-border-color)`

### 2.2. Quy tắc SCSS & CSS-First
- ⛔ **CẤM dùng `!important` trong CSS/SCSS.** Nâng cao độ ưu tiên tự nhiên bằng specificity hoặc selector lồng nhau.
- ⛔ **CẤM dùng comment `//` trong file SCSS** (Vite build sẽ báo lỗi). BẮT BUỘC dùng block comment `/* ... */`.
- ⛔ **CẤM dấu chấm phẩy kép (`;;`) trong SCSS.**
- Sử dụng các class tiện ích có sẵn của hệ thống: `w100`, `h100`, `mb5`, `mb10`, `mb20`, `flex-center`...

---

## 3. Quy chuẩn VxeTable Grid Pattern
- Bắt buộc khởi tạo bảng thông qua hook `useVxeTable`:
  ```typescript
  import { VxeGridInstance } from 'vxe-table';
  import { useVxeTable } from '/@/hooks/useVxeTableOptionsHook';

  const xGrid = ref<VxeGridInstance>();

  const options = useVxeTable<PageEquipmentOutput>(
      {
          id: 'tmsEquipGrid',      // Bắt buộc có ID duy nhất để lưu trạng thái ẩn/hiện cột
          name: 'Equipment_Export', // Tên file khi xuất Excel
          columns: [
              { type: 'checkbox', fixed: 'left', width: 45 },
              { field: 'code', title: $t('lz.entity.base.code'), minWidth: 140, sortable: true },
              { field: 'name', title: $t('lz.entity.base.name'), minWidth: 180 },
              { field: 'status', title: $t('lz.entity.base.status'), width: 120, slots: { default: 'row_status' } },
              { title: $t('lz.label.base.action'), fixed: 'right', width: 140, slots: { default: 'row_buttons' } },
          ],
      },
      {
          proxyConfig: { autoLoad: true, ajax: { query: ({ page, sort }) => handleQueryApi(page, sort) } },
          sortConfig: { defaultSort: { field: 'createTime', order: 'desc' } },
          pagerConfig: { pageSize: 50 },
      }
  );
  ```

---

## 4. i18n & Đa Ngôn Ngữ: 100% Không Hardcode
- ⛔ **CẤM hardcode chuỗi text, label, placeholder, confirm message trong template hoặc script.**
- ⛔ **CẤM nhãn song ngữ lai** (kiểu `Mã thiết bị (Equipment Code)`). Phải phân định rạch ròi qua file i18n: `vi-vn.json` (thuần Việt), `en-us.json` (thuần Anh).
- **Quy tắc phân cấp Namespace Key i18n:**
  1. `lz.*.base.*`: Dùng chung cho toàn hệ sinh thái (code, name, status, confirmDelete, saveSuccess...). **Bắt buộc kiểm tra `base` trước khi tạo mới**.
  2. `lz.*.{module}.*`: Dùng chung cho toàn module (ví dụ `lz.entity.tms.equipmentType`).
  3. `lz.*.{module}{Feature}.*`: Cục bộ cho riêng 1 màn hình cụ thể.

```vue
<!-- Cách dùng chuẩn trong template -->
<el-button type="primary" @click="handleAdd">
    {{ $t('lz.button.base.add') }}
</el-button>

<el-input 
    v-model="state.ruleForm.code" 
    :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" 
/>
```

---

## 5. Dữ liệu Cấu hình (BaseConfig Store) & Enums
- ⛔ **CẤM hardcode chuỗi mã loại cấu hình** (ví dụ `'status_type'`, `'direction_type'`).
- BẮT BUỘC sử dụng enum `BaseConfigTypeEnum`:
  ```typescript
  import { BaseConfigTypeEnum } from '/@/types/enums';
  import { useBaseConfig } from '/@/stores/baseConfig';

  const { getConfigDataByCode, getConfigItemByCode } = useBaseConfig();

  // 1. Lấy danh sách options cho dropdown / radio
  const statusOptions = computed(() => getConfigDataByCode(BaseConfigTypeEnum.SysStatusType));

  // 2. Lấy 1 item để hiển thị tên trên bảng hoặc TagInfo
  const statusItem = getConfigItemByCode(BaseConfigTypeEnum.SysStatusType, row.status);
  ```
- Khi bind vào form có kiểu dữ liệu là số, bắt buộc dùng `parseInt(item.code)`.

---

## 6. Tương tác API Services & Realtime
- ⛔ **TUYỆT ĐỐI CẤM sửa tay hoặc tự viết thủ công các file trong `src/api-services/`:**
  - Thư mục này được sinh tự động 100% từ Swagger thông qua script `pnpm build-api`.
  - Nếu thiếu API hoặc Model, báo backend cập nhật Swagger rồi build lại, không tự chế file client.
- **Cú pháp gọi API:**
  ```typescript
  import { getAPI } from '/@/utils/axios-utils';
  import { TmsEquipmentApi } from '/@/api-services/tms';

  const res = await getAPI(TmsEquipmentApi).apiTmsTmsequipmentPagePost(params);
  ```
- **Realtime Events:** Lắng nghe và hủy đăng ký sự kiện qua `mittBus` và `NatsEvent`:
  ```typescript
  import mittBus from '/@/utils/mitt';
  import { NatsEvent } from '/@/types/constants/natsEvent';

  onMounted(() => mittBus.on(NatsEvent.StatusDevice as any, handleDeviceStatus));
  onUnmounted(() => mittBus.off(NatsEvent.StatusDevice as any, handleDeviceStatus));
  ```

---

## 7. Clean Code TypeScript & Vị trí Code Mới
- ⛔ **CẤM toán tử phủ định kép `!!`:** Dùng `Boolean(val)` hoặc `val != null && val !== ''`.
- ⛔ **CẤM ép kiểu rườm rà** khi khai báo reactive state: Viết trực diện `state = reactive({ status: 1, type: null })`, không viết `null as number | null`.
- ⛔ **CẤM tạo file `types.ts` lẻ** chỉ để chứa `Props` của component con. Khai báo `interface Props` trực tiếp trong component `.vue`.
- 📍 **Vị trí chèn code mới:** Mọi hàm mới, computed mới, CSS block mới, key i18n mới thêm **BẮT BUỘC ĐẶT Ở DÒNG CUỐI CÙNG** của khối tương ứng (không chèn ngang chừng làm xáo trộn git diff).

