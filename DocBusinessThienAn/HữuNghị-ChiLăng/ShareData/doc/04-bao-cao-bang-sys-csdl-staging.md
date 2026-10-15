# 📊 Báo Cáo Các Bảng `Sys*` Trong CSDL Staging — Vai Trò Từng Bảng

> 📅 **Ngày đọc số liệu:** 15/10/2026 · **Nguồn số liệu:** `mssql_staging` (`10.10.8.30 / DEV_ITS10`), chỉ đọc (DAB `read_records`, `aggregate_records`) — đọc theo yêu cầu trực tiếp của người dùng.
> 📌 **Đối chiếu mã nguồn:** `Modules.CfgSystem.Core/Entities/`, `Shared.DTO/Enums/BaseEnums.cs`, `SqlValidator.cs`, module `Mail` (dùng `SysPlugin`), FE `api-services/system/`.

---

## 🧭 I. Tóm tắt

- Staging có **21 bảng `Sys*`** (khớp danh mục bảng của CSDL; không có bảng `Sys*` nào khác).
- Chia **4 nhóm**:

| Nhóm | Bảng | Việc chính |
|---|---|---|
| **A. Cấu hình hệ thống** | `SysConfig`, `SysConfigType`, `SysConfigData`, `SysOpConfig`, `SysTerminology`, `SysDictType`, `SysDictData` | Giao diện web, danh mục dùng chung, cấu hình vận hành từng phân hệ, bản dịch |
| **B. Tài khoản & phân quyền** | `SysUser`, `SysRole`, `SysUserRole`, `SysMenu`, `SysRoleMenu`, `SysRoleOrg`, `SysAccess` | Ai đăng nhập, có quyền gì, thấy menu nào |
| **C. Cơ cấu tổ chức** | `SysOrg`, `SysPos`, `SysUserExtOrg`, `SysTenant` | Đơn vị, chức vụ, kiêm nhiệm, đa thuê bao |
| **D. Ứng dụng, phân hệ, tệp** | `SysApp`, `SysPlugin`, `SysFile` | Ứng dụng con, nhóm phân hệ, tệp tải lên |

- **Bảng đang trống trên staging (0 dòng):** `SysDictType`, `SysDictData`, `SysRoleOrg`, `SysTenant` — tính năng có trong khung nhưng dự án chưa dùng.
- Các nhật ký thao tác (`SysOpLog`, `SysDiffLog`) **không** nằm trong CSDL này mà ở CSDL log riêng (`LogDefault`, xem [báo cáo 03](03-bao-cao-tong-the-entity-csdl-sharedata.md)).

### Cách đọc cột "Căn cứ"

| Ký hiệu | Nghĩa |
|---|---|
| ✅ Mã | Xác nhận từ entity/mã nguồn trong repo |
| 📌 DB | Xác nhận từ dữ liệu staging đọc được |
| ⚠️ Suy ra | Suy từ tên bảng chuẩn của khung Admin.NET hoặc từ vài dấu hiệu gián tiếp — **chưa có entity trong repo để xác nhận** |

> ⚠️ Các entity `SysUser`, `SysRole`, `SysMenu`, `SysOrg`, `SysPos`, `SysTenant`, `SysApp`, `SysPlugin`, `SysFile`, `SysAccess`… thuộc thư viện khung (không có mã nguồn trong repo), và DAB không trả danh sách cột. Vì vậy báo cáo chỉ nêu **vai trò**, không liệt kê cột của các bảng này.

---

## 🗂️ II. Chi tiết từng bảng

### A. Cấu hình hệ thống

| Bảng | Số dòng | Vai trò | Căn cứ |
|---|---:|---|---|
| `SysConfig` | 6 | Cấu hình **giao diện web**: tiêu đề, phụ đề, mô tả, watermark, bản quyền, logo. Mọi dòng thuộc nhóm `WebConfig` (mã `sys_web_title`, `sys_web_viceTitle`, `sys_web_viceDesc`, `sys_web_watermark`, `sys_web_copyright`, `sys_web_logo`) | 📌 DB |
| `SysConfigType` | 122 | **Loại danh mục** dùng chung (đầu của cặp loại–giá trị): `Code`, `Name`, `OrderNo`, `Remark`, `Status` | ✅ Mã |
| `SysConfigData` | 501 | **Giá trị của danh mục**: trỏ `ConfigTypeId → SysConfigType.ID`, có `Name`, `Value`, `OrderNo`, `Status`; thêm `TagType`, `StyleSetting`, `ClassSetting` để FE hiển thị nhãn có màu | ✅ Mã |
| `SysOpConfig` | 111 | **Cấu hình vận hành theo nhóm** của từng phân hệ (`GroupCode` + `Code` + `Value`, có `IsSysConfig`, `OrderNo`, `Remark`, `Status` 0/1). Service/Worker đọc theo `GroupCode` (mẫu CctvProxy: có nạp lại định kỳ, giữ bản cũ khi DB lỗi). Sửa trên màn hình *Cấu hình hoạt động* | ✅ Mã · 📌 DB |
| `SysTerminology` | 4.695 | **Bảng dịch thuật** đa ngôn ngữ: `Name` = tiền tố nhóm (`lz.label`, `lz.entity`…), `Code` = khóa sau tiền tố, `Value` = nội dung dịch, `Lang` (mặc định `vi-VN`) | ✅ Mã · 📌 DB |
| `SysDictType` | 0 | Loại từ điển của khung Admin.NET. **Trống**; FE có đoạn gọi `SysDictTypeApi` đã bị comment (`stores/userInfo.ts:128`), dự án dùng `SysConfigType/Data` thay thế | 📌 DB · ⚠️ Suy ra |
| `SysDictData` | 0 | Giá trị từ điển — như trên, **trống** | 📌 DB · ⚠️ Suy ra |

#### `SysOpConfig` — các nhóm cấu hình trên staging

| `GroupCode` | Số dòng | Phục vụ |
|---|---:|---|
| `media_config` | 23 | Media |
| `cctv_config` | 18 | CCTV |
| `mail_config` | 18 | Gửi mail |
| `toll_config` | 11 | Thu phí |
| `its_operation_config` | 10 | Vận hành ITS |
| `operation_config` | 9 | Vận hành chung |
| `vds_config` | 6 | VDS |
| `day_operation` | 4 | Vận hành theo ngày |
| `shift_operation` | 3 | Vận hành theo ca |
| `wp_config` | 3 | Cổng thông tin (WP) |
| `vms_config` | 2 | VMS |
| `tms_incident_action_group` | 2 | Nhóm hành động sự cố TMS |
| `tms_traffic_matrix` | 2 | Ma trận lưu lượng TMS |

📌 Nhóm `sharedata_tracker` (danh sách bảng theo dõi Change Tracking của ShareData) **chưa có** trên staging tại thời điểm đọc — khớp với việc script seed chưa chạy.

#### `SysOpConfig` — cách cấu hình: API, giao diện, script

`SysOpConfig` có CRUD API đầy đủ trong code và một màn hình quản trị; script `.sql` chỉ dùng để nạp dữ liệu ban đầu hàng loạt.

| Lớp | Vị trí | Ghi chú |
|---|---|---|
| **API** (module CfgSystem) | [`SysOpConfigController.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/SysOpConfigController.cs) | `POST Page`, `GET List`, `POST Single`, `POST Add`, `POST Update`, `POST Delete`, `POST BatchDelete`, `GET AllDataList` |
| **Kiểm tra dữ liệu** | [`OpConfigValidator.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/OpConfig/Validators/OpConfigValidator.cs) | Bắt buộc khi thêm: `Name` (≤ 64), `GroupCode`, `Code` (≤ 128), `Status`; `Value` ≤ 256; `Remark` ≤ 1024; `OrderNo` ≥ 0 |
| **Giao diện** (WEBVUE) | [`views/cfgSystem/sysOpConfig/`](../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/cfgSystem/sysOpConfig/index.vue) (danh sách + form `editOpConfig.vue`), gọi qua [`sys-op-config-api.ts`](../../../../TA-ITS015-WEBVUE-V1.0/src/src/api-services/cfgSystem/apis/sys-op-config-api.ts) | `App.vue` gọi `apiCfgsystemSysopconfigListGet` lúc khởi động để nạp biến dùng chung |
| **Service/Worker** | Đọc trực tiếp bảng theo `GroupCode` (mẫu `CctvOptionsLoader`), **không** đi qua API | Dòng thêm/sửa bằng màn hình hay bằng script đều có hiệu lực như nhau |
| **Script `.sql`** | Thư mục `sql/` của từng phân hệ | Nạp một lần cấu hình mặc định, chạy được trên nhiều môi trường; không phải kênh cấu hình duy nhất |

> 📌 Với nhóm `sharedata_tracker` của ShareData: quản trị thêm hoặc bật/tắt dòng trên màn hình trên, Worker nạp lại sau tối đa `ConfigReloadSeconds` (mặc định 60 giây), không cần sửa code hay chạy `.sql`.

#### `SysTerminology` — phân bố theo nhóm `Name`

| `Name` (tiền tố) | Số dòng | Dùng cho |
|---|---:|---|
| `lz.label` | 2.210 | Nhãn hiển thị |
| `lz.entity` | 986 | Tên thực thể/trường |
| `lz.message` | 701 | Thông báo |
| `lz.exception` | 388 | Thông báo lỗi |
| `lz.validation` | 256 | Thông báo kiểm tra dữ liệu |
| `lz.button` | 118 | Nút bấm |
| `param` | 20 | Tham số |
| `lz._root`, `lz.router`, `lz.param`, `lz.ui` | 8 / 3 / 2 / 1 | Gốc, route, tham số, UI |
| `lz.validation.sharedata.scheduleTimeRangeInvalid` | 2 | ⚠️ Bất thường — xem mục IV |

### B. Tài khoản & phân quyền

| Bảng | Số dòng | Vai trò | Căn cứ |
|---|---:|---|---|
| `SysUser` | 19 | **Tài khoản người dùng** đăng nhập hệ thống (gắn đơn vị, chức vụ) | ⚠️ Suy ra |
| `SysRole` | 14 | **Vai trò** — nhóm quyền gán cho người dùng; có phạm vi dữ liệu | ⚠️ Suy ra |
| `SysUserRole` | 64 | Gán **người dùng ↔ vai trò** (nhiều–nhiều) | ⚠️ Suy ra |
| `SysMenu` | 379 | **Cây menu** và các nút/quyền chức năng của FE | ⚠️ Suy ra |
| `SysRoleMenu` | 1.052 | Gán **vai trò ↔ menu/nút** — quyết định người dùng thấy và thao tác được gì | ⚠️ Suy ra |
| `SysRoleOrg` | 0 | Gán vai trò ↔ đơn vị khi phạm vi dữ liệu là "tùy chỉnh theo đơn vị". **Trống**: chưa dùng | 📌 DB · ⚠️ Suy ra |
| `SysAccess` | 79 | Có hằng `SysAccessTypeEnum.Jwt = "jwt"` (`BaseEnums.cs:590`) — nhiều khả năng lưu **phiên/token truy cập**. Chưa có entity để xác nhận | ⚠️ Suy ra |

### C. Cơ cấu tổ chức

| Bảng | Số dòng | Vai trò | Căn cứ |
|---|---:|---|---|
| `SysOrg` | 28 | **Cây đơn vị/tổ chức** | ⚠️ Suy ra |
| `SysPos` | 33 | **Chức vụ** | ⚠️ Suy ra |
| `SysUserExtOrg` | 4 | Người dùng **kiêm nhiệm** thêm đơn vị/chức vụ ngoài đơn vị chính | ⚠️ Suy ra |
| `SysTenant` | 0 | Danh sách **thuê bao** (đa thuê bao). **Trống**, nhưng entity nghiệp vụ vẫn kế thừa `EntityTenant` (cột `TenantId`) | 📌 DB · ✅ Mã |

### D. Ứng dụng, phân hệ, tệp

| Bảng | Số dòng | Vai trò | Căn cứ |
|---|---:|---|---|
| `SysApp` | 31 | **Ứng dụng con** (nhóm menu/quyền theo ứng dụng). Có API riêng (`sys-app-api.ts`), bộ nhớ đệm `sysApp`. Staging có mã `TMS`, `VMST`… lẫn dữ liệu thử | 📌 DB · ⚠️ Suy ra |
| `SysPlugin` | 17 | **Nhóm phân hệ/plugin** (tên như "Tích hợp VDS", "Tích hợp VMS", "FMS"). `Code` của bảng này được **module Mail dùng để gom báo cáo** (`RptEmailReport.PluginCode = SysPlugin.Code`, tên nhóm lấy từ `SysPlugin.Name`) | ✅ Mã · 📌 DB |
| `SysFile` | 89 | **Metadata tệp tải lên** (logo, ảnh, tài liệu đính kèm) | ⚠️ Suy ra |

---

## 🔗 III. Quan hệ chính

```mermaid
flowchart LR
    User["SysUser"] --- UR["SysUserRole"] --- Role["SysRole"]
    Role --- RM["SysRoleMenu"] --- Menu["SysMenu"]
    Role -. "phạm vi tùy chỉnh (trống)" .- RO["SysRoleOrg"] -.- Org["SysOrg"]
    User --- Org
    User --- Pos["SysPos"]
    User --- UEO["SysUserExtOrg"] --- Org
    Menu --- App["SysApp"]
    CT["SysConfigType"] --- CD["SysConfigData"]
    Plugin["SysPlugin"] -. "Code = PluginCode" .- RptMail["RptEmailReport"]
    Tenant["SysTenant (trống)"] -. "TenantId" .- Entities["Bảng nghiệp vụ (EntityTenant)"]
```

> Quan hệ của nhóm B, C và `SysApp ↔ SysMenu` là suy ra theo mô hình chuẩn của khung; riêng `SysConfigType → SysConfigData` và `SysPlugin → RptEmailReport` đã xác nhận từ mã.

---

## 🛡️ IV. Quan sát cần lưu ý

1. **Danh sách bảng bị chặn trong SQL trích xuất ShareData** (`SqlValidator.RestrictedTables`): `SysUser`, `SysUserRole`, `SysUserExtOrg`, `SysRole`, `SysRoleMenu`, `SysRoleOrg`, `SysTenant`, `SysConfig`, `SysConfigData`, `SysConfigType`, `SysOrg`, `ShareDataPartner`.
   - **Không nằm trong danh sách chặn:** `SysAccess`, `SysApp`, `SysFile`, `SysMenu`, `SysOpConfig`, `SysPlugin`, `SysPos`, `SysTerminology`, `SysDictType`, `SysDictData`.
   - ⚠️ `SysAccess` (có thể chứa token) và `SysOpConfig` (nhóm `mail_config`, 18 dòng, có thể có thông tin kết nối thư) là hai bảng nên được xem xét đưa vào danh sách chặn. **Chưa kiểm** nội dung nhạy cảm của hai bảng này — báo cáo không đọc giá trị `Value`/token.
2. **Dữ liệu thử lẫn trong staging:** `SysApp` có dòng tên/mã vô nghĩa (`t`/`qw`, `twewe`/`qweweew`…), `SysPlugin` có dòng `Plugin A`, `plugin`, một dòng tên trống và hai dòng cùng tên "Tích hợp VDS". Mã `SysPlugin.Code` được module Mail dùng, nên dòng trùng/rác có thể làm lệch cách gom báo cáo email.
3. **`SysTerminology` có 2 dòng `Name` bất thường:** `Name` = `lz.validation.sharedata.scheduleTimeRangeInvalid` (đầy đủ khóa) thay vì tiền tố nhóm (`lz.validation`). Cần đối chiếu cách khóa này được tra cứu.
4. **Bảng trống** (`SysDictType`, `SysDictData`, `SysRoleOrg`, `SysTenant`): giữ nguyên trong CSDL nhưng chưa có dữ liệu — không phải lỗi, chỉ cho biết tính năng chưa dùng.

---

## 🧪 V. Phương pháp và giới hạn

- Số dòng: `count(*)` từng bảng; phân bố nhóm: `groupby GroupCode` (`SysOpConfig`) và `groupby Name` (`SysTerminology`); tên danh mục: `read_records` chỉ các cột `Name`, `Code`, `Remark`.
- ⛔ Không đọc cột nhạy cảm (mật khẩu, token, `Value` của cấu hình kết nối). Chỉ truy vấn đọc, không ghi.
- Số liệu là **ảnh chụp tại thời điểm đọc**; số dòng có thể đổi khi người dùng thao tác.
- Vai trò đánh dấu ⚠️ cần được xác nhận bằng entity của thư viện khung hoặc người nắm hệ thống phân quyền trước khi dùng làm căn cứ thiết kế.
