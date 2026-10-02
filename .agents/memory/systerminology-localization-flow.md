# Kiến Trúc & Luồng Dữ Liệu Dịch Thuật SysTerminology (Localization Flow)

> 📌 **Nguồn tài liệu**: Dịch ngược từ `Modules.System.dll` (`TermService`, `SysConfigController`) và phân tích mã nguồn `TA-ITS015-WEBVUE-V1.0` (`termHelper.ts`, `userInfo.vue`, `locale.ts`).

---

## 1. Bản Chất Kiến Trúc

- **Quy tắc cốt lõi (Safeguard 11 & Rule 19.26)**: Backend WebAPI **TUYỆT ĐỐI CẤM** sửa tay vào các file tĩnh `src/TAC_WebAPI/Resources/*.json`.
- **Lý do**: Các file `Resources/vi-VN.json` và `en-US.json` **KHÔNG PHẢI LÀ FILE NGUỒN CỐ ĐỊNH CỦA DEV**, mà là **Artifact được sinh tự động (auto-generated) từ CSDL `SysTerminology`** thông qua API `LoadServerTerm`.
- Nếu dev sửa tay vào file tĩnh `Resources/*.json`, khi bất kỳ ai bấm nút "Làm mới thuật ngữ" trên giao diện hoặc đổi ngôn ngữ, hệ thống sẽ ghi đè toàn bộ file tĩnh bằng dữ liệu từ CSDL `SysTerminology`.

---

## 2. Cấu Trúc Bảng CSDL `SysTerminology`

Trong CSDL (`DEV_ITS10`), bảng `SysTerminology` lưu thuật ngữ theo các cột:

| Cột | Ý nghĩa | Ví dụ |
|---|---|---|
| `Name` | Tiền tố nhóm key | `lz.entity`, `lz.exception`, `lz.validation`, `lz.message` |
| `Code` | Mã định danh nghiệp vụ | `sharedata.aliasFieldKey`, `sharedata.packetCode` |
| `Value` | Chuỗi dịch tiếng Việt/Anh | `Khóa field`, `Mã gói tin` |
| `Lang` | Mã ngôn ngữ | `vi-VN`, `en-US` |
| `Status` | Trạng thái kích hoạt | `1` (bắt buộc `1` để hệ thống nạp; `0` là vô hiệu hóa) |

👉 **Quy tắc ghép key**: Khi nạp vào bộ nhớ hoặc sinh file JSON, hệ thống tự động ghép:
$$\text{FullKey} = \text{Name} + \text{"."} + \text{Code}$$
Ví dụ: `lz.entity` + `.` + `sharedata.aliasFieldKey` $\rightarrow$ `lz.entity.sharedata.aliasFieldKey`.

---

## 3. Luồng Đồng Bộ & Sử Dụng (End-to-End Flow)

```
[CSDL: SysTerminology]
       │
       ├── (1) GET /api/system/sysconfig/loadserverterm?language=vi-VN
       │       └── Backend (TermService.LoadServerTerm):
       │           - Đọc SysTerminology (Status == 1)
       │           - Ghép Name + '.' + Code => Dictionary
       │           - Ghi đè file: App.HostEnvironment/Resources/vi-VN.json
       │           - Furion.Localization.L.Text tra cứu & hiển thị cho Exception / Oops.Oh
       │
       └── (2) GET /api/system/sysconfig/loadclientterm?language=vi-VN
               └── Frontend (termHelper.loadTerminologyClient):
                   - Tải dictionary JSON về trình duyệt
                   - Lưu vào IndexedDB (TA-ConfigDb, store terminology)
                   - locale.ts: getTerminologyList() nạp vào Vue i18n
                   - $t('lz...') trên giao diện hiển thị ngôn ngữ mới
```

---

## 4. Điểm Kích Hoạt Đồng Bộ Trên Giao Diện (Trigger)

Hệ thống **không đọc CSDL `SysTerminology` ở mỗi HTTP request** để tránh suy giảm hiệu năng. Việc đồng bộ được kích hoạt theo 2 đường:

1. **Nút Làm mới trên TopBar**:
   - Vị trí: Góc trên bên phải giao diện $\rightarrow$ Click vào **Avatar / Tên người dùng** $\rightarrow$ Di chuột vào menu **Ngôn ngữ**.
   - Icon: **Nút Làm mới 🔄** (`ele-Refresh`) nằm ngay cạnh chữ *"Ngôn ngữ"*.
   - Mã nguồn: `src/layout/navBars/topBar/userInfo.vue` (và `user.vue`), gọi hàm `refreshTerm()`:
     ```ts
     const refreshTerm = async (lang: string) => {
         await loadTerminology(lang); // Gọi cả loadServerTerm và loadClientTerm
         window.location.reload();    // Reload lại trang
     };
     ```
2. **Khi chuyển đổi ngôn ngữ**:
   - Khi người dùng chuyển đổi qua lại giữa `vi-VN` và `en-US` (`onLanguageChange`), hàm `loadTerminology(lang)` cũng tự động được gọi.
3. **Gọi trực tiếp API (cho Dev / Postman / Swagger)**:
   - `GET /api/system/sysconfig/loadserverterm?language=vi-VN` (cập nhật file cho Backend).
   - `GET /api/system/sysconfig/loadclientterm?language=vi-VN` (cập nhật cho Client).

---

## 5. Quy Trình Thêm Mới Dịch Thuật Cho Lập Trình Viên

Khi phát triển tính năng mới cần thêm thông báo lỗi, tên thực thể hoặc ngoại lệ:
1. **Viết script SQL idempotent** (hoặc gọi API `SysTerminologyController`) để INSERT/UPDATE vào bảng `SysTerminology` trên CSDL.
2. **Kích hoạt đồng bộ**:
   - Bấm nút icon **Làm mới 🔄** bên cạnh chữ Ngôn ngữ trên giao diện Web, HOẶC
   - Gọi API `/api/system/sysconfig/loadserverterm?language=vi-VN`.
3. **TUYỆT ĐỐI KHÔNG** sửa tay vào `src/TAC_WebAPI/Resources/*.json`.
