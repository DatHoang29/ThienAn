# ✅ Kịch Bản Nghiệm Thu Thật — Phân Hệ ShareData

> 📅 **Soạn & Cập nhật:** 18/10/2026 · **Người chạy:** người dùng tự khởi động dự án và kiểm tay · **Môi trường:** DB **local/test** (⛔ không chạy trên staging hay môi trường thật).  
> 📌 **Căn cứ:** [`Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) PHẦN VII, [`02-mapping-goi-tin-101-111.md`](02-mapping-goi-tin-101-111.md), code Worker/WebAPI/FE tại thời điểm soạn.  
> 📌 **Phạm vi:** Nghiệm thu luồng nghiệp vụ với **API và DB thật**. Phần Playwright với API giả lập đã chấp nhận (R16 bỏ); kịch bản này thay cho nó.  
> 📌 **Nguyên tắc ghi nhận:** Mỗi mục có: **Bước** → **Kết quả mong đợi** → **☐ Đạt / ☐ Không đạt**. Ghi lỗi vào bảng cuối tài liệu.

---

## 🗺️ Lộ Trình Nhanh (8 Bước Nghiệm Thu)

1. [Kiểm tra dữ liệu cũ và môi trường](#00-kiểm-dữ-liệu-cũ-trước-khi-bật-worker) (Mục 0.0 & 0.1)
2. [Chuẩn bị cấu trúc bảng và dữ liệu nền](#02-chuẩn-bị-dữ-liệu-nền-chọn-một-trong-hai-cách) (Mục 0.2: CÁCH 1 dựng A→Z trên giao diện từ Đối tác hoặc CÁCH 2 chạy Script SQL tùy chọn)
3. [Khởi động các thành phần hệ thống theo thứ tự: WebAPI → Worker → Frontend](#03-thành-phần-cần-chạy) (Mục 0.3)
4. [Bật receiver giả lập đối tác nhận](#04-receiver-giả-đối-tác-nhận) (Mục 0.4)
5. [Đồng bộ từ điển đa ngôn ngữ trên giao diện](#đồng-bộ-từ-điển-đa-ngôn-ngữ-trên-giao-diện) (🔄)
6. [Chuẩn bị dữ liệu ánh xạ cho chiều gửi D0 và bảng đích chiều nhận E0](#06-chuẩn-bị-riêng-cho-chiều-gửi-d0-và-chiều-nhận-e0) (Mục 0.6)
7. [Thực hiện nghiệm thu chi tiết từng phân hệ từ A đến I](#🅰️-a-chế-độ-và-trạng-thái-gói-tin-màn-hình-cấu-hình-gói-tin--sql-center-thư-mục-datasource) (Mục A–I)
8. [Dọn dẹp dữ liệu thử nghiệm sau kiểm thử](#🧹-dọn-sau-khi-test) (Mục Dọn sau khi test)

---

## 🧭 0. Điều kiện trước khi chạy

### 0.0. Kiểm dữ liệu cũ trước khi bật Worker

Trước khi khởi động Worker trên CSDL có sẵn dữ liệu cũ (hoặc sau khi khôi phục CSDL), cần kiểm tra các đăng ký chia sẻ (`ShareDataSubscription`) đang ở trạng thái `State = 1` (Active). Nếu các đăng ký này trỏ tới đối tác có địa chỉ thật bên ngoài (`Address` không phải `localhost` hoặc `127.0.0.1`), Worker khi bật lên sẽ lập tức quét và gửi gói tin thử nghiệm ra bên ngoài.

```sql
-- Liệt kê đăng ký State = 1 kèm Address/Port của đối tác:
SELECT s.ID, s.PacketCode, s.PartnerID, p.Name AS PartnerName, p.ConnectionType, p.Address, p.Port, s.State
FROM dbo.ShareDataSubscription s
JOIN dbo.ShareDataPartner p ON s.PartnerID = p.ID
WHERE s.IsDelete IS NULL AND s.State = 1;

-- Đưa State về 2 (Paused) cho đăng ký không trỏ localhost:
UPDATE s
SET s.State = 2, s.UpdateTime = GETDATE()
FROM dbo.ShareDataSubscription s
JOIN dbo.ShareDataPartner p ON s.PartnerID = p.ID
WHERE s.IsDelete IS NULL AND s.State = 1
  AND (p.Address IS NULL OR (p.Address NOT LIKE '%localhost%' AND p.Address NOT LIKE '%127.0.0.1%'));
```

> 💡 **Lưu ý dữ liệu cũ:**
> - Trên CSDL đã vận hành trước đó, các gói tin có thể mang ID kiểu GUID (các ID cố định trong Phụ lục C chỉ áp dụng khi nạp mới từ đầu).
> - Nếu phát hiện gói tin dư thừa không nằm trong danh mục chuẩn (ví dụ gói thử nghiệm cũ `101_commonData1`), cần đánh dấu xóa mềm (`UPDATE dbo.ShareDataPacket SET IsDelete = GETDATE() WHERE Code = '101_commonData1';`) hoặc tính đến các gói này khi đối chiếu số lượng trong các câu truy vấn đếm.

### 0.1. Trỏ đúng DB (quan trọng) — chạy bằng VS Code debug

- `ShareDataWorker/appsettings.json` **và** `appsettings.Development.json` đều **mặc định trỏ DB staging `10.10.8.30`**. Chạy mà không có biến môi trường ghi đè là **ghi lên staging**.
- [`.vscode/launch.json`](../../../../.vscode/launch.json) đã có sẵn cấu hình ép về local bằng biến môi trường (biến môi trường thắng file `appsettings` và `Database.json`).
- Cấu hình **`🌐 TAC_WebAPI (Local Docker)`** ghim cứng các cờ `DbSettings.EnableInitDb`, `TableSettings.EnableInitTable`, `TableSettings.EnableIncreTable = "false"` cho `ConfigId 0` (`DEV_ITS10`) bằng biến môi trường. `ConfigId 3` (`ShareDataDB`, `DEV_ITS10_Inbound`) tuân theo `Database.json`. Worker không chạy cơ chế CodeFirst.
- **Chỉ dùng các cấu hình debug sau, tuyệt đối không dùng cấu hình `Server 10.10.8.30`:**

| Mục đích | Cấu hình debug nên chọn | Ghi chú cổng & môi trường |
|---|---|---|
| WebAPI + Worker cùng lúc | Compound **`🚀 ShareData (WebAPI + Worker Local)`** | Chạy đồng thời cả 2 dịch vụ với biến môi trường local |
| WebAPI riêng | `🌐 TAC_WebAPI (Local Docker)` | Cổng `http://localhost:5005` |
| Worker riêng | `📦 ShareDataWorker (Local Docker)` | `DOTNET_ENVIRONMENT=Development` |
| Frontend | `🎨 WebVue (Dev Server)` | Mở thêm riêng (compound ShareData không gồm FE) |

- Hai cấu hình Local trỏ `Server=localhost`, CSDL `DEV_ITS10`, `DEV_ITS10_Log` (WebAPI) và `DEV_ITS10_Inbound` (Worker), **cổng mặc định 1433**. Nếu SQL Server local của bạn ở cổng khác (ví dụ Docker `14333`), sửa `Server=localhost,<cổng>` trong **chính cấu hình Local của `launch.json`**, tuyệt đối không sửa file `appsettings`.
- Tự kiểm sau khi lên: log khởi động của Worker và truy vấn `SELECT @@SERVERNAME, DB_NAME()` trên DB mà các script ở 0.2 đã chạy phải **khớp** với DB mà WebAPI đang dùng.
- ⛔ Đừng bấm F5 bằng cấu hình `Attach` hay `dotnet run` trần cho Worker: khi đó không có biến môi trường và Worker kết nối staging.

---

### 0.2. Chuẩn bị dữ liệu nền: chọn một trong hai cách

Dữ liệu nền cho phân hệ ShareData bao gồm: 11 gói tin chia sẻ (`ShareDataPacket`), danh mục 25 mã cảnh báo lỗi (`SysConfigData` loại `sharedata_error_code`), 15 bảng nguồn (`ShareDataSourceTable`), 46 tham số vận hành (`SysOpConfig` nhóm `sharedata_config`), và các khóa thuật ngữ đa ngôn ngữ (`SysTerminology`).

#### 0.2.1. Bảng lựa chọn cách chuẩn bị dữ liệu

> ⚠️ **ĐIỂM GIAO THOA QUAN TRỌNG GIỮA HAI CÁCH:**  
> Toàn bộ các script SQL từ `00` đến `06` **hoàn toàn KHÔNG khởi tạo** dữ liệu cho 3 bảng: Đối tác (`ShareDataPartner`), Ánh xạ (`ShareDataMapping`), và Đăng ký chia sẻ (`ShareDataSubscription`).  
> Do đó, dù chọn **Cách 1** (dựng trên giao diện) hay **Cách 2** (chạy script SQL), người dùng **BẮT BUỘC PHẢI THỰC HIỆN Bước 1 (Đối tác), Bước 3 (Ánh xạ), và Bước 4 (Đăng ký chia sẻ) của mục 0.2.3** trên giao diện WebVue để chuẩn bị đối tác `P-REST`, cấu hình ánh xạ và đăng ký gửi/nhận cho các kịch bản D0, E0, C, D, E.

| Tình huống CSDL | Cách lựa chọn | Lý do & Lưu ý |
|---|---|---|
| **Dựng dữ liệu nền trên giao diện (Ưu tiên)** | **[Cách 1](#023-cách-1--lộ-trình-az-dựng-dữ-liệu-nền-trên-giao-diện-tài-khoản-superadmin)** (Lộ trình A→Z trên giao diện) | **Khuyên dùng theo chủ trương dự án.** Đi tuần tự theo chuỗi phụ thuộc chuẩn: **Đối tác → Gói tin → Ánh xạ → Đăng ký chia sẻ → (Bảng nguồn, Mã ESH, Thuật ngữ, Tham số sharedata_config)**. Trực quan, kiểm soát 100% từng cấu hình trực tiếp trên WebVue. |
| **CSDL mới hoàn toàn** (local / dev / test) | **[Cách 2](#024-cách-2--nạp-nhanh-bằng-script-sql-tùy-chọn)** (Tùy chọn nạp nhanh: Script SQL `02` → `03` → `04` → `05`) | Đường tắt nạp nhanh toàn bộ 11 gói tin, 25 mã cảnh báo lỗi, 15 bảng nguồn và 46 tham số vận hành chỉ trong một lần chạy. Sau khi chạy script, vẫn tiếp tục làm **Bước 1, 3, 4 của mục 0.2.3** trên UI để tạo Đối tác, Ánh xạ và Đăng ký. |
| **CSDL đã có dữ liệu cũ** (đang chạy / nâng cấp) | **[Cách 2](#024-cách-2--nạp-nhanh-bằng-script-sql-tùy-chọn)** (Script di trú: `06` → `00` → `01` → `02` → `03` → `04`) | Chạy script di trú để chuẩn hóa cấu trúc, đổi tên cột đồng bộ (`06`), di trú sang `ShareDataSourceTable` (`00`) và bảo toàn dữ liệu hiện có. Sau đó thực hiện **Bước 1, 3, 4 của mục 0.2.3** trên UI. |
| **Nạp nhanh mã lỗi và bản dịch thuật ngữ** | **[Cách 2](#024-cách-2--nạp-nhanh-bằng-script-sql-tùy-chọn)** (Đường tắt nạp nhanh: Script SQL `03` và `04`) | Số lượng khóa bản dịch (49 khóa hai ngôn ngữ vi/en = 98 bản ghi) và mã lỗi (25 mã) rất lớn. Nhập tay trên giao diện tốn thời gian; script `03` và `04` chạy theo cơ chế idempotent, an toàn tuyệt đối. |

---

#### 0.2.2. Khởi tạo cấu trúc: CodeFirst hoặc Script di trú

Trước khi nạp dữ liệu (theo Cách 1 hay Cách 2), CSDL cần có sẵn cấu trúc các bảng và cột của ShareData.

##### Lựa chọn A: Khởi tạo tự động bằng CodeFirst qua cấu hình tạm trong `launch.json`
1. Mở file [`.vscode/launch.json`](../../../../.vscode/launch.json).
2. Sao chép cấu hình `🌐 TAC_WebAPI (Local Docker)` thành một bản ghi tạm (ví dụ `🌐 TAC_WebAPI (Init DB Schema Temporary)`).
3. Trong khối `env` của bản ghi tạm, ghi đè các cờ khởi tạo CSDL:
   ```json
   "DbConnection__ConnectionConfigs__0__DbSettings__EnableInitDb": "true",
   "DbConnection__ConnectionConfigs__0__TableSettings__EnableInitTable": "true",
   "DbConnection__ConnectionConfigs__0__TableSettings__EnableIncreTable": "false"
   ```
   - **CHỈ ÁP DỤNG CHO CSDL MỚI HOÀN TOÀN:** Nếu CSDL hoàn toàn trống chưa có dữ liệu nào, có thể bổ sung `"DbConnection__ConnectionConfigs__0__SeedSettings__EnableInitSeed": "true"` để nạp hạt giống ban đầu.
   - ⛔ **CẢNH BÁO QUAN TRỌNG:** Tuyệt đối **CẤM** bật `EnableInitSeed` trên CSDL đã có dữ liệu cũ (sẽ gây xung đột hoặc trùng lặp dữ liệu ID cố định).
   - Nếu CSDL chưa có CSDL `DEV_ITS10_Inbound`, bổ sung thêm:
     ```json
     "DbConnection__ConnectionConfigs__3__DbSettings__EnableInitDb": "true"
     ```
4. Khởi động WebAPI với cấu hình tạm này cho tới khi console in thông báo `"Now listening on: http://localhost:5005"`.
5. Bấm dừng WebAPI.
6. Xóa bỏ bản ghi cấu hình tạm trong `launch.json`. ⛔ **TUYỆT ĐỐI KHÔNG sửa trực tiếp và KHÔNG commit file `Database.json` hay `Database.Development.json`**.

##### Lựa chọn B: Nâng cấp CSDL cũ bằng Script di trú
- Nếu CSDL đã có sẵn dữ liệu và cột cũ, chạy lần lượt theo thứ tự: `06-sharedata-doi-ten-cot-dong-bo.sql` → `00-sharedata-migration-mot-lan.sql` → `01-sharedata-cau-truc.sql`.
- ⚠️ **Cảnh báo bẫy thứ tự khi nâng cấp CSDL cũ (Script 06):**
  + Nếu CSDL cũ còn mang tên cột cũ (`SyncKind`, `TimeColumn` / `CursorColumn`, `LookbackDays`), **BẮT BUỘC CHẠY SCRIPT `06` TRƯỚC KHI BẬT CODEFIRST** hoặc trước khi chạy các script khác.
  + *Nguyên nhân:* Nếu bật CodeFirst trước, SqlSugar sẽ tự tạo cột mới (`SyncMode`, `CursorTimeColumn`, `MaxLookBackDays`) với giá trị NULL. Khi đó script `06` kiểm tra thấy cột mới đã tồn tại sẽ in *"đã tồn tại, bỏ qua"* và dữ liệu cấu hình cũ sẽ bị kẹt lại ở cột cũ, không được dời sang cột mới.

##### Truy vấn kiểm tra cấu trúc cột trên `ShareDataPacket` (chỉ đọc):
```sql
SELECT name, system_type_id, max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID('dbo.ShareDataPacket')
  AND name IN ('SyncKind','SyncMode','TimeColumn','CursorColumn','CursorTimeColumn','LookbackDays','MaxLookBackDays');
```
*Kết quả mong đợi:* Phải thấy các cột mới `SyncMode` (int), `CursorTimeColumn` (nvarchar), `MaxLookBackDays` (int).

---

#### 0.2.3. CÁCH 1 — Lộ trình A→Z dựng dữ liệu nền trên giao diện (Tài khoản SuperAdmin)

Theo chủ trương của chủ dự án, **ưu tiên thao tác trực tiếp trên giao diện WebVue**. Người dùng có thể tự cấu hình toàn diện dữ liệu nền trực tiếp trên giao diện bằng tài khoản **SuperAdmin** mà không cần chạy các script SQL danh mục.

##### Điều kiện tiên quyết & Chuỗi phụ thuộc nghiệp vụ bắt buộc:
- **Điều kiện:** Đã hoàn tất khởi tạo cấu trúc bảng ở mục 0.2.2; WebAPI và Frontend WebVue đang chạy bình thường (xem mục 0.3).
- **Tài khoản:** Đăng nhập bằng tài khoản **SuperAdmin** để có đầy đủ quyền thao tác lưu SQL trích xuất và các cấu hình hệ thống.
- **Sơ đồ phụ thuộc nghiệp vụ một dòng (A → Z):**
  $$\text{Đối tác} \longrightarrow \text{Gói tin} \longrightarrow \text{Ánh xạ} \longrightarrow \text{Đăng ký chia sẻ} \quad \Big( \text{Bảng theo dõi, Mã ESH, Thuật ngữ độc lập} \Big)$$
- **Tại sao bắt buộc Ánh xạ phải làm trước Đăng ký chia sẻ?**
  1. **Ràng buộc khóa công tắc kích hoạt trên giao diện:** Trong component `subscriptionTable.vue`, hàm `canToggle` kiểm tra `isMappingReady`: nếu chưa có hồ sơ ánh xạ hợp lệ đang bật (`IsActive = true`), công tắc kích hoạt đăng ký bị khóa cứng để tránh người dùng bật vào trạng thái chắc chắn lỗi.
  2. **Quy tắc gán trạng thái tự động của Backend (`SubscriptionCommandHandler.cs`):** Khi tạo mới Đăng ký chia sẻ, Backend kiểm tra `mappingReady`. Nếu chưa có ánh xạ đang bật, Backend tự động gán trạng thái của Đăng ký là `Paused` (Tạm dừng) và buộc người dùng phải sang màn Ánh xạ tạo rồi quay lại bật. Ngược lại, nếu đã có Ánh xạ đang bật và đối tác đã kết nối, Backend gán luôn trạng thái `Active` (Hoạt động) ngay khi tạo!
  3. **Quy tắc vận hành của Worker nền:** Dịch vụ nền Worker chỉ quét các đăng ký có trạng thái `Active` và có Ánh xạ kích hoạt (`IsActive = 1`). Nếu thiếu ánh xạ, Worker sẽ ghi nhận cảnh báo mã `ESH-1303` (*"Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất"*) và giữ nguyên mốc dữ liệu. Do đó, tạo Ánh xạ trước Đăng ký đảm bảo chu kỳ đầu tiên của Worker chạy trơn tru, không phát sinh lỗi cảnh báo.

---

##### Bước 1 — Khởi tạo đối tác chia sẻ (`ShareDataPartner`)
1. Mở màn hình: **Chia sẻ dữ liệu** → **Đối tác & Đăng ký chia sẻ** (`views/sharedata/sharing/index.vue`).
2. Tại danh sách Đối tác (cột bên trái), bấm nút **Thêm mới**:
   - **Mã đối tác** (`code`): Nhập `P-REST`. *(Bắt buộc; duy nhất trong toàn hệ thống; chữ cái/số/gạch dưới; **KHÔNG sửa được sau khi lưu** - ô nhập bị vô hiệu hóa).*
   - **Tên đối tác** (`name`): Nhập `Đối tác thử nghiệm REST API`. *(Bắt buộc).*
   - **Loại kết nối** (`connectionType`): Chọn `API` (WebAPI / REST). *(Giao diện hỗ trợ đầy đủ 4 loại kết nối: `API`, `File`, `Kafka`, `FTP`).*
   - **Địa chỉ** (`address`): Nhập `http://localhost`. *(Bắt buộc).*
   - **Cổng** (`port`): Nhập `9090`. *(Bắt buộc; số nguyên từ 1 đến 65535; **KHÔNG sửa được sau khi lưu** - ô nhập bị vô hiệu hóa).*
   - **Đường dẫn API** (`endPointApiUrl`): Nhập `/api/receive`. *(Tùy chọn; hệ thống tự động chuẩn hóa dấu gạch chéo `/` ở đầu nếu người dùng quên nhập).*
   - **Địa chỉ đầy đủ** (`fullAddress`): Hiển thị `http://localhost:9090/api/receive` *(Chỉ đọc).*
   - **Cấu hình kết nối** (`connectionConfigForm`): Giao diện cung cấp form nhập nhóm trực quan gồm các khối: *Xác thực* (`sectionAuth`), *Dữ liệu* (`sectionData`), *Nâng cao* (`collapseAdvanced`), *Mã hóa* (`collapseEncryption`), và *Tham số mở rộng* (`collapseExtra`). Người dùng có thể điền trực tiếp trên các trường của form (ví dụ định dạng JSON, thuật toán mã hóa) hoặc bấm nút **"Sửa JSON thô"** (`switchToRaw`) để nhập chuỗi JSON trực tiếp: `{ "Format": "Json", "Encryptor": "None", "Enabled": "true" }` (hoặc để trống, hệ thống sẽ tự áp dụng giá trị mặc định chuẩn).
   - **Ghi chú** (`remark`): Nhập ghi chú tùy ý (tối đa 256 ký tự).
   - Bấm nút **Xác nhận** (hoặc Lưu).
3. ⚠️ **Ràng buộc vận hành & Thao tác bấm "Kết nối" trên giao diện:**
   - **Khóa sửa đối tác:** Nút sửa cấu hình đối tác bị KHÓA khi đối tác đang ở trạng thái "Kết nối" hoặc còn bất kỳ đăng ký nào đang ở trạng thái "Hoạt động".
   - **Nút "Kết nối" trên thẻ đối tác:** Sau khi lưu thẻ đối tác, trên card đối tác `P-REST`, bấm nút **"Kết nối"** (nút chuyển sang trạng thái xanh / đã kết nối).  
     *Ý nghĩa:* Trên giao diện FE, hàm `canToggle` (`subscriptionTable.vue`) yêu cầu `partnerConnected = true` để cho phép gạt công tắc kích hoạt Đăng ký từ trạng thái `Pending` sang `Active`. *(Ở tầng Worker nền, Worker chỉ lọc `State == Active` và `IsActive = 1` của Ánh xạ, không phụ thuộc vào trạng thái phiên kết nối).*
   - *(Tùy chọn)* Đối tác kiểu FTPS/DISK/NAS: Khi cần chạy kịch bản C5, các kênh này được định nghĩa ở tầng backend (`ConnectTypes.cs`) hoặc cấu hình bên trong `ConnectionConfig`.

---

##### Bước 2 — Khởi tạo và cấu hình gói tin chia sẻ (`ShareDataPacket`)
1. Mở màn hình: **Chia sẻ dữ liệu** → **Cấu hình gói tin** (`views/sharedata/dataSource/index.vue`).
2. Trên thanh công cụ danh sách gói tin (cột bên trái), bấm nút **Thêm mới**:
   - Nhập **Mã** (`code`): Bắt buộc dùng đúng mã chuẩn tại [Phụ lục C](#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata) (ví dụ: `101_commonData`, `103_vdsData`...).
   - Nhập **Tên** (`name`), **Số thứ tự** (`orderNo` = 101..111).
   - Chọn **Trạng thái** (`status`): Chọn *Bật* (Status = 1) cho các gói từ 101 đến 110; riêng gói 111 chọn *Tắt* (Status = 0).
   - Nhập **Ghi chú** nếu cần → Bấm nút **Lưu**.
3. **Cơ chế tự động điền cấu hình mẫu chuẩn:** Khi nhập mã gói chuẩn (dạng 101..110 hoặc `101_xxx`), hệ thống tự động điền câu truy vấn SQL mẫu chuẩn, chế độ đồng bộ (`SyncMode`: Snapshot / Incremental) và số ngày quét lùi (`MaxLookBackDays`) nhờ `PacketCommandHandler` và `PacketDefaultTemplate.TryGet` mà không cần người dùng phải gõ tay hay dán SQL. *(Giao diện đã bỏ hoàn toàn khái niệm "Phạm vi" hay `partnerScope`).*
4. Cấu hình SQL trích xuất và tham số đồng bộ:
   - Nhấp chọn gói tin vừa tạo ở danh sách bên trái.
   - Ở khung bên phải, chuyển sang tab **Cấu hình SQL** (`tabSqlCenter`).
   - Khung soạn thảo SQL đã hiển thị câu truy vấn trích xuất chuẩn. Nếu muốn khôi phục lại câu SQL chuẩn từ hệ thống, bấm nút **Mẫu mặc định** (`vqbDefaultTemplate`). *(Lưu ý: Bấm nút này chỉ nạp nội dung lên khung soạn thảo, chưa ghi đè xuống CSDL cho tới khi bấm nút Lưu).*
   - Kiểm tra **Chế độ đồng bộ** (`syncMode`): Chọn *Toàn bộ* (Snapshot) cho các gói 101/102/105/108/110; hoặc *Nối đuôi* (Incremental) cho các gói 103/104/106/107/109 theo đúng [Phụ lục C](#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata).
   - Nếu là gói *Nối đuôi*: Nhập **Quét lùi tối đa (ngày)** (`maxLookBackDays` = 1).
   - Bấm nút **Lưu** (`save`) để lưu cấu hình SQL và các tham số đồng bộ xuống CSDL.
   - *(Tùy chọn)* Bấm nút **Đồng bộ vào Gói tin** (`syncToFields`) để hệ thống phân tích các cột trả về từ câu SQL và sinh danh mục trường dữ liệu nguồn.
5. 💡 **Khuyến nghị gói tin tối thiểu để nghiệm thu:** Để thực hiện nhanh các kịch bản kiểm thử, người dùng tối thiểu cần khởi tạo 2 gói tin:
   - Gói **`101_commonData`** (Snapshot, phục vụ kịch bản D1, D5, E0, E1).
   - Gói **`103_vdsData`** (Incremental, phục vụ kịch bản D3 nối đuôi).  
   Khuyến nghị khởi tạo đầy đủ cả 11 gói tin theo [Phụ lục C](#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata) (10 gói Bật, gói 111 Tắt).

---

##### Bước 3 — Khởi tạo cấu hình ánh xạ (`ShareDataMapping`)
1. Mở màn hình: **Chia sẻ dữ liệu** → **Cấu hình ánh xạ** (`views/sharedata/mapping/index.vue`).
2. Bấm nút **Thêm mới** trên thanh công cụ:
   - **Tab Thông tin chung (`info`):**
     + **Chiều:** Chọn **Gửi đi (Outbound)** (`Direction = 0`) hoặc **Nhận về (Inbound)** (`Direction = 1`). *(Lưu ý: `Direction = 2` là Hai chiều - Both, chỉ dùng cho Mapping khi một cấu hình dùng chung cả gửi và nhận).*
     + **Đối tác:** Chọn đối tác `P-REST` vừa tạo ở Bước 1.
     + **Gói tin:** Chọn gói tin tương ứng (`101_commonData` hoặc `103_vdsData`) vừa tạo ở Bước 2.
     + **Tên ánh xạ:** Nhập tên mô tả (ví dụ: `Ánh xạ gửi gói 101 chuẩn` hoặc `Ánh xạ nhận gói 101`).
     + **Sử dụng:** Bật công tắc kích hoạt (`isActive = true`).
   - **Tab Cấu hình ánh xạ (`map`):**
     + Dán khuôn JSON mẫu của đối tác vào khung bên trái và bấm **Phân tích** (biểu tượng cây đũa thần).
     + 📌 *Chi tiết khuôn JSON mẫu và cây ánh xạ:* Vui lòng xem chi tiết tại mục **[0.6. Chuẩn bị riêng cho chiều gửi D0 và chiều nhận E0](#06-chuẩn-bị-riêng-cho-chiều-gửi-d0-và-chiều-nhận-e0)** (Mục **D0** cho Ánh xạ chiều gửi Outbound; Mục **E0** cho Ánh xạ chiều nhận Inbound).
     + ⚠️ **Hai nguyên tắc bắt buộc khi cấu hình ánh xạ:**
       1. **Chiều gửi (Outbound):** Bắt buộc phải khai đủ **4 trường siêu dữ liệu hệ thống `$meta`**: `PartnerCode`, `PacketCode`, `Now`, và `Serial` (ràng buộc kiểm tra của `MappingCommandHandler.EnsureMetaFields`).
       2. **Chiều nhận (Inbound):** **Không yêu cầu các trường `$meta`**, chỉ cần định nghĩa mảng `data` để đưa các trường dữ liệu vào `@records` cho câu lệnh `OPENJSON(@records)` bóc tách trên CSDL Inbound.
   - Bấm nút **Lưu**.

---

##### Bước 4 — Khởi tạo đăng ký chia sẻ (`ShareDataSubscription`)
1. Mở màn hình: **Chia sẻ dữ liệu** → **Đối tác & Đăng ký chia sẻ** (`views/sharedata/sharing/index.vue`).
2. Nhấp chọn đối tác `P-REST` ở danh sách bên trái. Ở khung bên phải, thực hiện tạo đăng ký cho từng chiều:
   - **Chiều gửi (Tab "Gửi đi" - Outbound, `Direction = 0`):**
     1. Bấm nút **Thêm mới** đăng ký gửi.
     2. **Gói tin:** Chọn gói tin `101_commonData` (hoặc `103_vdsData`).
     3. **Lịch gửi (`scheduler`):** Chọn loại *Định kỳ (Continuous)*, đặt chu kỳ gửi thử nghiệm ngắn (ví dụ: `30` giây; giới hạn từ 5s đến 86400s theo `feConfig.subscriptionInterval`). Chế độ Theo sự kiện đã được ẩn trên giao diện.
     4. **Cờ `sendOnNewData` (*Gửi khi có dữ liệu mới*):** Bật để Worker lắng nghe sự kiện NATS từ Change Tracking và kích hoạt gửi ngay lập tức khi bảng nguồn có thay đổi dữ liệu mà không cần chờ hết chu kỳ 30 giây (kịch bản D9 yêu cầu NATS local cổng 4222).
     5. **Trạng thái:** Vì đã tạo Ánh xạ ở Bước 3 và đã bấm "Kết nối" ở Bước 1, đăng ký sẽ mang trạng thái **`Active` (Hoạt động)** ngay sau khi lưu.
     6. Bấm nút **Lưu**.
   - **Chiều nhận (Tab "Nhận về" - Inbound, `Direction = 1`):**
     1. Chuyển sang tab **Nhận về**. Bấm nút **Thêm mới** đăng ký nhận.
     2. **Gói tin:** Chọn gói tin `101_commonData`.
     3. **Trạng thái:** Chọn *Hoạt động (Active)* → Bấm nút **Lưu**.
     4. 💡 **Đặc thù chiều nhận:** Chiều nhận là *bị động* theo request từ đối tác gọi vào WebAPI (`POST /api/sharedata/shareDataInbound/receive`); giao diện sẽ tự động ẩn hoàn toàn khối cấu hình Lịch gửi (`scheduler`) và cờ `sendOnNewData`, chỉ cần chọn Đối tác và Gói tin.
3. Bảng đăng ký hiển thị danh sách đăng ký đã tạo kèm công tắc chuyển trạng thái Bật/Tắt (`canToggle`) và các nút thao tác.
4. Cặp đăng ký tối thiểu để nghiệm thu: Chiều Gửi {`101_commonData`, `103_vdsData`}, Chiều Nhận {`101_commonData`}.

---

##### Bước 5 — Khởi tạo danh mục 15 bảng nguồn (`ShareDataSourceTable`)
- Danh mục bảng nguồn (`ShareDataSourceTable`) là quy tắc nghiệp vụ riêng của phân hệ ShareData kết hợp lưu trực tiếp trạng thái mốc phiên bản (`LastVersion`, `LastChangeTime`, `LastIssue`), hai công tắc độc lập `IsTracked` và `IsQueryable`, cùng `Category` và `DefaultAlias`, nên không bố trí màn hình cấu hình riêng trên giao diện (ngoại lệ chủ đích theo quy chuẩn kiến trúc dữ liệu nền).
- Thao tác khởi tạo và kiểm tra danh mục 15 bảng nguồn ([Phụ lục B](#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable)) được thực hiện tại **[Khung ngoại lệ duy nhất cuối Cách 1](#khung-ngoai-le-duy-nhat)** bên dưới.

---

##### Bước 6 — Khởi tạo danh mục 25 mã cảnh báo lỗi ESH
1. Mở màn hình: **Hệ thống** → **Loại cấu hình** (`views/cfgSystem/sysConfigType/index.vue`).
2. Ở cột bên trái (*Loại cấu hình*): Tìm loại có Mã `sharedata_error_code`. Nếu chưa có, bấm **Thêm mới**: Tên: *Mã cảnh báo lỗi ShareData*, Mã: `sharedata_error_code`, Thứ tự: `1900`, Trạng thái: *Bật* → Bấm **Lưu**.
3. Nhấp chọn dòng `sharedata_error_code`: Bảng bên phải (*Dữ liệu cấu hình*) sẽ tải danh sách mã lỗi của loại này.
4. Bấm nút **Thêm mới** để nạp từng mã cảnh báo lỗi theo danh sách tại [Phụ lục A](#phụ-lục-a-danh-mục-25-mã-cảnh-báo-lỗi-esh-xxxx):
   - ⚠️ **LƯU Ý QUAN TRỌNG VỀ GIỚI HẠN ĐỘ DÀI:** Ô **Tên** (`name`) trên giao diện bị giới hạn tối đa **64 ký tự**. Trong 25 mã ESH, có **9 mã có tên gốc dài hơn 64 ký tự**; khi nhập tay trên giao diện, **BẮT BUỘC sử dụng tên rút gọn (≤ 64 ký tự)** được cung cấp riêng tại [Phụ lục A](#phụ-lục-a-danh-mục-25-mã-cảnh-báo-lỗi-esh-xxxx).
   - Nhập **Mã** (`code`): Mã lỗi chuẩn ESH (ví dụ: `ESH-1201`).
   - Nhập **Giá trị** (`value`): Phân loại (`outbound`, `inbound`, hoặc `tracking`).
   - Chọn **Loại thẻ** (`tagType`): `warning` hoặc `danger`.
   - Nhập **Thứ tự** (`orderNo`): Số thứ tự mã lỗi (ví dụ: 1201, 1301...).

---

##### Bước 7 — Khởi tạo bản dịch giao diện & thuật ngữ + Đồng bộ 🔄
1. Mở màn hình: **Hệ thống** → **Thuật ngữ** (`views/cfgSystem/sysTerminology/index.vue`).
2. Bấm nút **Thêm mới** để nạp từng bản dịch theo danh sách tại [Phụ lục E: Danh mục thuật ngữ ShareData (SysTerminology)](#phụ-lục-e-danh-mục-thuật-ngữ-sharedata-systerminology):
   - **Tên tiền tố** (`name`): Ví dụ `lz.validation` hoặc `lz.exception`.
   - **Mã** (`code`): Ví dụ `sharedata.lookbackDaysRange` hoặc `sharedata.connectConfigSyntaxError`.
   - **Giá trị** (`value`): Nội dung chuỗi dịch tiếng Việt hoặc tiếng Anh.
   - **Ngôn ngữ** (`lang`): Chọn `vi-VN` hoặc `en-US`.
   - *Quy tắc:* Mỗi bản ghi trên giao diện đại diện cho một cặp khóa - ngôn ngữ (nhập bản dịch `vi-VN` trước, sau đó nhập bản dịch `en-US`). Giao diện không hỗ trợ nhập hàng loạt.
3. **Đồng bộ bộ nhớ cache thuật ngữ lên giao diện:** Sau khi nạp xong, người dùng thực hiện một trong hai cách:
   - *Cách 1 (UI):* Bấm vào ảnh đại diện (avatar) ở góc trên bên phải màn hình → chọn mục **"Ngôn ngữ"** → bấm nút biểu tượng **🔄 (Làm mới từ điển)**; hoặc bấm nút 🔄 trên thanh công cụ màn hình Thuật ngữ.
   - *Cách 2 (API):* Gọi trực tiếp HTTP `GET http://localhost:5005/api/system/sysconfig/loadserverterm?language=vi-VN` (và `language=en-US`).
4. 💡 **Đường tắt thực tế:** Do số lượng bản dịch gồm 49 khóa $\times$ 2 ngôn ngữ = 98 bản ghi, việc nhập tay từng dòng trên giao diện rất mất thời gian. Khuyến nghị người dùng nên sử dụng **CÁCH 2 (đường tắt)** chạy script `04-sharedata-dich-thuat.sql` để nạp tự động an toàn trong vài giây.

---

##### Bước 8 (Tùy chọn) — Cấu hình tham số vận hành ShareData (`SysOpConfig` nhóm `sharedata_config`)
1. Mở màn hình: **Hệ thống** → **Cấu hình hoạt động** (`views/cfgSystem/sysOpConfig/index.vue`).
2. Tại ô lọc *Nhóm cấu hình*: Chọn nhóm `sharedata_config` (Cấu hình vận hành & giới hạn Chia sẻ dữ liệu).
3. Bảng bên phải hiển thị danh sách 46 tham số vận hành của phân hệ ShareData (gồm 17 tham số Worker, 23 tham số FE và 6 tham số WebAPI; chi tiết xem [Phụ lục F](#phụ-lục-f-danh-mục-46-tham-số-vận-hành-sharedata-sysopconfig-nhóm-sharedata_config)).
4. Người dùng có thể chỉnh sửa trực tiếp giá trị tham số trên giao diện mà không cần build lại mã nguồn hay khởi động lại ứng dụng. Phía WebAPI (`ShareDataConfigService` đăng ký `IScoped`) truy vấn trực tiếp `SysOpConfig` theo từng HTTP request nên nhận giá trị mới ngay lập tức không có độ trễ; phía Worker quét nạp lại định kỳ sau mỗi chu kỳ `ConfigReloadSeconds` (30 giây). Nếu không cấu hình hoặc để trống, hệ thống luôn tự động dùng giá trị dự phòng an toàn trong mã nguồn.

---

<a id="khung-ngoai-le-duy-nhat"></a>
##### ⚠️ KHUNG NGOẠI LỆ DUY NHẤT KHÔNG CÓ GIAO DIỆN / API (CHUẨN BỊ CHIỀU NHẬN E0 & DANH MỤC BẢNG NGUỒN `ShareDataSourceTable`):
> 🔴 **CẢNH BÁO QUAN TRỌNG:**  
> Toàn bộ quy trình dựng dữ liệu nền đều thực hiện trên giao diện WebVue, **NGOẠI TRỪ 2 HẠNG MỤC HẠ TẦNG / TRẠNG THÁI CSDL DƯỚI ĐÂY**:
>
> **1. Hạ tầng tiếp nhận cho kịch bản chiều nhận E0 (dùng gói kiểm thử riêng `E2E_INBOUND_TEST`):**
> - **Lý do dùng gói riêng:** Gói 101 là gói chuẩn chiều GỬI (Outbound). Sử dụng gói kiểm thử riêng `E2E_INBOUND_TEST` giúp cô lập hoàn toàn môi trường kiểm thử E2E hai chiều, tránh xung đột cấu hình ánh xạ giữa luồng gửi D và luồng nhận E, đồng thời dễ dàng dọn dẹp sạch sẽ sau kiểm thử mà không đụng chạm dữ liệu sản xuất.
> - **Bảng đích `InboundTrafficFlow` trên CSDL `[DEV_ITS10_Inbound]`**: CSDL tiếp nhận gói tin độc lập với CSDL chính, hệ thống không có màn hình tạo bảng CSDL.
> - **Cấu hình câu ghi MERGE trong bảng `ShareDataPacketSql` trên CSDL `[DEV_ITS10]`**: Lưu câu lệnh MERGE cho gói `E2E_INBOUND_TEST` (ID `1500000000903`). Hệ thống chưa cung cấp màn hình quản trị câu lệnh ghi CSDL chiều nhận.  
> 👉 *Cách thực hiện:* Chạy Khối 4 của script [`05-sharedata-du-lieu-gia-lap.sql`](../sql/05-sharedata-du-lieu-gia-lap.sql) hoặc chạy các câu lệnh SQL mẫu tại [Phụ lục D](#phụ-lục-d-mẫu-cấu-hình-chiều-nhận-e0-bảng-đích-câu-merge-và-targetshapejson).
>
> **2. Danh mục 15 bảng nguồn (`ShareDataSourceTable` trên CSDL `[DEV_ITS10]` — Bước 5):**
> - **Lý do ngoại lệ chủ đích:** Bảng `ShareDataSourceTable` lưu đồng thời danh sách bảng nguồn của phân hệ ShareData và trạng thái con trỏ phiên bản (`LastVersion`, `LastChangeTime`, `LastIssue`) do Worker cập nhật nguyên tử theo từng chu kỳ quét, cùng 2 công tắc độc lập `IsTracked` và `IsQueryable`, nên không đặt màn hình cấu hình trên giao diện.
> 👉 *Cách thực hiện:*
> - **Cách thuận tiện:** Chạy Mục 3 của script [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) (idempotent — tự động chèn/cập nhật đủ 15 bảng chuẩn theo [Phụ lục B](#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable), hoặc tự nạp qua `ShareDataSourceTableSeedData` nếu CSDL mới bật `EnableInitSeed`).
> - **Cách thủ công (chèn/cập nhật từng dòng):**
>   ```sql
>   USE [DEV_ITS10];
>   -- Ví dụ bật/tắt trạng thái theo dõi Change Tracking của 1 bảng:
>   UPDATE dbo.ShareDataSourceTable
>   SET IsTracked = 0, UpdateTime = GETDATE()
>   WHERE TableName = N'TmsEquipment' AND IsDelete IS NULL;
>
>   -- Kiểm tra danh mục 15 bảng nguồn hiện có:
>   SELECT TableName, Name, Category, DefaultAlias, IsTracked, IsQueryable, LastVersion, LastChangeTime, LastIssue
>   FROM dbo.ShareDataSourceTable
>   WHERE IsDelete IS NULL
>   ORDER BY OrderNo;
>   ```

---

##### Bảng checklist nghiệm thu dữ liệu nền bằng giao diện (Cách 1):

| Bước | Thao tác kiểm tra | Kết quả mong đợi | Kết quả |
|:---:|---|---|---|
| **B1** | Tạo đối tác `P-REST` (màn *Đối tác & Đăng ký chia sẻ*) | Lưu thành công; các trường `code` và `port` hiển thị vô hiệu hóa khi xem lại; bấm nút "Kết nối" chuyển sang màu xanh | ☐ Đạt ☐ Không đạt |
| **B2** | Tạo gói tin chuẩn (101, 103... màn *Cấu hình gói tin*) | Tự động điền câu SQL, `SyncMode`, `MaxLookBackDays` từ Catalog; lưu thành công | ☐ ☐ |
| **B3** | Tạo Ánh xạ gửi & nhận (màn *Cấu hình ánh xạ*) | Chiều gửi đủ 4 khóa `$meta`; chiều nhận đủ mảng `data`; lưu thành công với `isActive = true` | ☐ ☐ |
| **B4** | Tạo Đăng ký gửi & nhận (màn *Đối tác & Đăng ký chia sẻ*) | Đăng ký hiển thị ở trạng thái `Active`; chiều nhận ẩn lịch gửi; công tắc bật/tắt hoạt động đúng | ☐ ☐ |
| **B5** | Kiểm tra 15 bảng nguồn `ShareDataSourceTable` (truy vấn SQL tại khung ngoại lệ) | Đủ 15 bảng (`IsDelete IS NULL`), 14 bảng `IsTracked = 1`, `TmsEquipment` có `IsTracked = 0`, cả 15 bảng `IsQueryable = 1` | ☐ ☐ |
| **B6** | Khởi tạo 25 mã cảnh báo lỗi ESH | Đủ 25 mã ESH trong loại `sharedata_error_code`, không vượt quá giới hạn 64 ký tự | ☐ ☐ |
| **B7** | Khởi tạo thuật ngữ & bấm 🔄 làm mới từ điển | Các thông báo lỗi và nhãn giao diện hiển thị đúng ngôn ngữ, không còn mã thô `lz.*` | ☐ ☐ |
| **B8** | Kiểm tra 46 tham số vận hành nhóm `sharedata_config` (màn *Cấu hình hoạt động*) | Đủ 46 tham số hiển thị, có thể chỉnh sửa giá trị và lọc theo nhóm `sharedata_config` bình thường | ☐ ☐ |

---

#### 0.2.4. CÁCH 2 — Nạp nhanh bằng Script SQL (Tùy chọn)

Nếu muốn tiết kiệm thời gian nhập liệu thủ công trên giao diện, người dùng có thể sử dụng các tệp script SQL có sẵn trong thư mục [`DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/sql/`](../sql/) trên CSDL local (mỗi script đã có sẵn chỉ thị `USE [DEV_ITS10]`):

> ⚠️ **LƯU Ý BẮT BUỘC SAU KHI CHẠY SCRIPT SQL (CÁCH 2):**  
> Các script SQL từ `00` đến `06` chỉ nạp các danh mục tĩnh (Gói tin, Mã lỗi, Bảng theo dõi Tracker, Bản dịch Thuật ngữ, Dữ liệu giả lập). Các script này **hoàn toàn KHÔNG tạo bản ghi nào trong `ShareDataPartner`, `ShareDataMapping`, hay `ShareDataSubscription`**.  
> Vì vậy, sau khi hoàn tất chạy script, người dùng **BẮT BUỘC thực hiện tiếp Bước 1 (Đối tác), Bước 3 (Ánh xạ), và Bước 4 (Đăng ký chia sẻ) của mục 0.2.3** trên giao diện WebVue để tạo đối tác `P-REST`, cấu hình ánh xạ và đăng ký gửi/nhận trước khi tiến hành các kịch bản kiểm thử nhóm C, D, E.

| # | Script | CSDL | Mục đích thực tế chính xác | Áp dụng cho DB mới | Áp dụng cho DB cũ |
|---|---|---|---|:---:|:---:|
| **00** | [`00-sharedata-migration-mot-lan.sql`](../sql/00-sharedata-migration-mot-lan.sql) | `DEV_ITS10` | Bổ sung 4 cột mới vào `ShareDataPacket`, tạo/nâng cấp bảng danh mục nguồn `ShareDataSourceTable` (có đủ cột `IsTracked`, `IsQueryable`, `Category`, `DefaultAlias` kèm unique index, chép `Status` → `IsTracked`), di trú dữ liệu bảng theo dõi và mốc phiên bản từ `SysOpConfig` (nhóm `sharedata_tracker` cũ) + `ShareDataTrackVersion` sang `ShareDataSourceTable`, xóa mềm các dòng cấu hình cũ và DROP bảng mồ côi (`ShareDataTable`, `ShareDataTrackVersion`). Idempotent, PRINT từng nhánh. | Bỏ qua | **Bắt buộc** |
| **01** | [`01-sharedata-cau-truc.sql`](../sql/01-sharedata-cau-truc.sql) | `DEV_ITS10` | Dời các trường kết nối (`Address`, `Port`, `EndPointApiUrl`) từ JSON `ConnectionConfig` sang cột trực tiếp trên bảng `ShareDataPartner`. | Bỏ qua (nếu CodeFirst đã tạo) | **Bắt buộc** |
| **02** | [`02-sharedata-seed-goi-tin.sql`](../sql/02-sharedata-seed-goi-tin.sql) | `DEV_ITS10` | Tạo mới các gói tin còn thiếu trong 11 gói chuẩn (101–111) với ID cố định; nạp câu truy vấn SQL mẫu, gán `SyncMode` mặc định (Snapshot 101/102/105/108/110, Incremental 103/104/106/107/109); bật gói 110, tắt gói 111. Cờ `@GhiDeMauGoiTin = 0` giữ nguyên các câu SQL tùy biến đã có. | **Bắt buộc** | **Bắt buộc** |
| **03** | [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) | `DEV_ITS10` | Khởi tạo `SysConfigType` (`sharedata_error_code`), nạp **25** mã cảnh báo lỗi ESH-xxxx vào `SysConfigData`; nạp 15 bảng nguồn vào bảng danh mục riêng `ShareDataSourceTable` (14 bảng theo dõi `IsTracked = 1`, `TmsEquipment` mặc định `IsTracked = 0`, cả 15 bảng `IsQueryable = 1`); đăng ký nhóm `sharedata_config` vào `SysConfigData` (`operation_config_type`) và nạp **46** tham số vận hành vào `SysOpConfig`. | **Bắt buộc** | **Bắt buộc** |
| **04** | [`04-sharedata-dich-thuat.sql`](../sql/04-sharedata-dich-thuat.sql) | `DEV_ITS10` | Nạp $\ge 49$ khóa bản dịch vi/en (98 bản ghi song ngữ) vào `SysTerminology` cho các nhãn giao diện, bộ lọc, kiểm tra an toàn SQL (bao gồm khóa `SqlTableNotAllowed`), và 12 khóa lỗi kết nối đối tác (`sharedata.connectConfig*`). **Bắt buộc chạy trước mục C**. | **Bắt buộc** | **Bắt buộc** |
| **05** | [`05-sharedata-du-lieu-gia-lap.sql`](../sql/05-sharedata-du-lieu-gia-lap.sql) | `DEV_ITS10`, `DEV_ITS10_Inbound` | Nạp dữ liệu giả lập cho gói 104 (Thời tiết), 106 (WIM), 110 (Sự cố & VMS) và Khối 4 chuẩn bị cho kịch bản E0 (tạo gói kiểm thử riêng `E2E_INBOUND_TEST` ID `1500000000900`, bảng đích `InboundTrafficFlow` trên `DEV_ITS10_Inbound` và cấu hình câu ghi MERGE trong `ShareDataPacketSql` ID `1500000000903`). | *Tùy chọn* (cần cho E, G) | *Tùy chọn* (cần cho E, G) |
| **06** | [`06-sharedata-doi-ten-cot-dong-bo.sql`](../sql/06-sharedata-doi-ten-cot-dong-bo.sql) | `DEV_ITS10` | Đổi tên các cột đồng bộ trên `ShareDataPacket` và `ShareDataMapping` (`SyncKind` → `SyncMode`, `TimeColumn`/`CursorColumn` → `CursorTimeColumn`, `LookbackDays` → `MaxLookBackDays`). | Bỏ qua | **Bắt buộc (chạy đầu tiên)** |

##### Đồng bộ từ điển đa ngôn ngữ trên giao diện:
Sau khi thực thi script `04-sharedata-dich-thuat.sql`, người dùng **BẮT BUỘC** đồng bộ bộ nhớ cache thuật ngữ lên giao diện:
- **Cách 1 (UI):** Bấm vào ảnh đại diện (avatar) ở góc trên bên phải màn hình → chọn mục **"Ngôn ngữ"** → bấm nút biểu tượng **🔄 (Làm mới từ điển)**.
- **Cách 2 (API):** Gọi trực tiếp HTTP `GET http://localhost:5005/api/system/sysconfig/loadserverterm?language=vi-VN` (hoặc `language=en-US`).


---

#### 0.2.5. Kiểm tra đối soát sau khi dựng dữ liệu nền

Chạy truy vấn SQL chỉ đọc sau để đối soát số lượng trên CSDL `[DEV_ITS10]`:
```sql
-- 1. Kiểm tra 11 gói tin chuẩn (10 bật, 1 tắt - gói 111):
SELECT COUNT(1) AS TotalPackets,
       SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS EnabledPackets,
       SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS DisabledPackets
FROM dbo.ShareDataPacket WHERE IsDelete IS NULL;
-- Mong đợi: TotalPackets = 11, EnabledPackets = 10, DisabledPackets = 1

-- 2. Kiểm tra 25 mã cảnh báo lỗi ESH-xxxx:
SELECT COUNT(1) AS TotalErrors
FROM dbo.SysConfigData d
JOIN dbo.SysConfigType t ON d.ConfigTypeId = t.ID
WHERE t.Code = 'sharedata_error_code' AND d.IsDelete IS NULL;
-- Mong đợi: TotalErrors = 25

-- 3. Kiểm tra 15 bảng nguồn ShareDataSourceTable (14 theo dõi IsTracked = 1, 1 tắt - TmsEquipment; cả 15 cho phép truy vấn IsQueryable = 1):
SELECT COUNT(1) AS TotalSources,
       SUM(CASE WHEN IsTracked = 1 THEN 1 ELSE 0 END) AS TrackedSources,
       SUM(CASE WHEN IsTracked = 0 THEN 1 ELSE 0 END) AS UntrackedSources,
       SUM(CASE WHEN IsQueryable = 1 THEN 1 ELSE 0 END) AS QueryableSources
FROM dbo.ShareDataSourceTable
WHERE IsDelete IS NULL;
-- Mong đợi: TotalSources = 15, TrackedSources = 14, UntrackedSources = 1, QueryableSources = 15

-- 4. Kiểm tra các khóa dịch thuật ShareData (bao gồm khóa mới SqlTableNotAllowed):
SELECT Code, Lang AS [Language], Value AS [Terminology]
FROM dbo.SysTerminology
WHERE IsDelete IS NULL
  AND Code IN ('sharedata.sqlTableNotAllowed', 'sharedata.connectConfigFileNamePatternNoToken', 'sharedata.lookbackDaysRange')
ORDER BY Code, Lang;

-- Đếm tổng số bản ghi thuật ngữ ShareData theo danh mục script 04:
SELECT COUNT(1) AS TotalTerms,
       COUNT(DISTINCT Code) AS UniqueKeys
FROM dbo.SysTerminology
WHERE Name IN ('lz.validation', 'lz.exception')
  AND Code LIKE 'sharedata.%'
  AND IsDelete IS NULL;
-- Mong đợi: TotalTerms = 98 (49 khóa song ngữ vi-VN và en-US), UniqueKeys = 49

-- 5. Kiểm tra 46 tham số vận hành SysOpConfig nhóm sharedata_config:
SELECT COUNT(1) AS TotalConfigs
FROM dbo.SysOpConfig
WHERE GroupCode = 'sharedata_config' AND IsDelete IS NULL;
-- Mong đợi: TotalConfigs = 46 (17 Worker + 23 FE + 6 BE)

-- 6. Kiểm tra Đối tác, Ánh xạ và Đăng ký chia sẻ:
SELECT COUNT(1) AS TotalPartners,
       SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS EnabledPartners
FROM dbo.ShareDataPartner WHERE IsDelete IS NULL;
-- Mong đợi: TotalPartners >= 1 (có P-REST)

SELECT COUNT(1) AS TotalMappings,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveMappings,
       SUM(CASE WHEN Direction = 0 THEN 1 ELSE 0 END) AS OutboundMappings,
       SUM(CASE WHEN Direction = 1 THEN 1 ELSE 0 END) AS InboundMappings,
       SUM(CASE WHEN Direction = 2 THEN 1 ELSE 0 END) AS BothMappings
FROM dbo.ShareDataMapping WHERE IsDelete IS NULL;
-- Mong đợi: TotalMappings >= 2 (có ánh xạ gửi 101 và nhận 101)

SELECT COUNT(1) AS TotalSubscriptions,
       SUM(CASE WHEN State = 1 THEN 1 ELSE 0 END) AS ActiveSubscriptions,
       SUM(CASE WHEN Direction = 0 THEN 1 ELSE 0 END) AS OutboundSubscriptions,
       SUM(CASE WHEN Direction = 1 THEN 1 ELSE 0 END) AS InboundSubscriptions
FROM dbo.ShareDataSubscription WHERE IsDelete IS NULL;
-- Mong đợi: TotalSubscriptions >= 2 (có đăng ký gửi 101 và nhận 101 đang Active)
```

---

### 0.3. Thành phần cần chạy

Khởi động các thành phần theo đúng thứ tự phụ thuộc: **WebAPI → ShareDataWorker → Frontend WebVue**:

| Thứ tự | Thành phần | Cách khởi chạy & Ghi chú |
|:---:|---|---|
| **1** | **WebAPI** (`TAC_WebAPI`) | Khởi chạy qua cấu hình debug `🌐 TAC_WebAPI (Local Docker)` (cổng `5005`). Đăng nhập tài khoản **SuperAdmin** trên giao diện để có đủ quyền lưu SQL và cấu hình. |
| **2** | **ShareDataWorker** | Khởi chạy qua cấu hình debug `📦 ShareDataWorker (Local Docker)`. Cấu hình này **ĐÃ CÓ SẴN `"Nats__Enabled": "true"`** trong `.vscode/launch.json`. Do đó, chỉ cần bật một NATS server local (cổng `4222`) là có thể thử nghiệm kịch bản D9 (gửi ngay khi có dữ liệu mới). Nếu không chạy NATS, luồng quét định kỳ vẫn vận hành bình thường. |
| **3** | **Frontend** (`WebVue`) | Khởi chạy qua cấu hình `🎨 WebVue (Dev Server)` hoặc terminal `cd TA-ITS015-WEBVUE-V1.0/src && pnpm dev`. |
| **4** | **Receiver giả** (đối tác nhận) | Bật script node giả lập máy chủ đối tác tiếp nhận dữ liệu (xem mục 0.4). |

---

### 0.4. Receiver giả (đối tác nhận)

Tạo file `receiver.mjs` tại thư mục làm việc bất kỳ, chạy lệnh `node receiver.mjs 9090`. Mỗi request nhận được ghi ra file `received-<n>.json` (method, url, header, body). Tạo file rỗng tên `FAIL` cùng thư mục để receiver trả HTTP **500** (giả lập lỗi), xóa file để trả lại **200**.

```js
import http from 'node:http';
import fs from 'node:fs';

const port = Number(process.argv[2] ?? 9090);
let n = 0;

http.createServer((req, res) => {
  let body = '';
  req.on('data', (c) => (body += c));
  req.on('end', () => {
    n++;
    const fail = fs.existsSync('FAIL');
    fs.writeFileSync(
      `received-${n}.json`,
      JSON.stringify({ method: req.method, url: req.url, headers: req.headers, body }, null, 2),
    );
    console.log(`#${n} ${req.method} ${req.url} ${body.length}B -> ${fail ? 500 : 200}`);
    res.writeHead(fail ? 500 : 200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ success: !fail }));
  });
}).listen(port, () => console.log(`Receiver http://localhost:${port}`));
```

> Nếu đối tác cấu hình `SuccessField`/`ResponseContract`, trả đúng trường đó thay vì `success`.

---

### 0.5. Truy vấn kiểm tra (chỉ đọc, chạy trên DB local)

```sql
SELECT Code, SyncMode, Status, CursorTimeColumn, MaxLookBackDays FROM dbo.ShareDataPacket ORDER BY Code;
SELECT TOP 20 * FROM dbo.ShareDataLastSend;                                   -- mốc gửi nối đuôi
SELECT TOP 30 * FROM dbo.ShareDataActivityLog ORDER BY CreateTime DESC;       -- nhật ký cha–con
SELECT TOP 30 * FROM dbo.ShareDataAlertLog    ORDER BY CreateTime DESC;       -- cảnh báo ESH-xxxx
-- Trên CSDL Inbound:
SELECT TOP 20 * FROM dbo.ShareDataInboundPacket ORDER BY CreateTime DESC;     -- gói nhận
```

---

### 0.6. Chuẩn bị riêng cho chiều gửi D0 và chiều nhận E0

#### D0: Chuẩn bị chiều gửi (Ánh xạ chiều gửi cho gói 101 và 103)
Trước khi thực hiện các bài test nhóm D, cần đảm bảo đối tác `P-REST`, đăng ký gửi và ánh xạ chiều gửi đã được chuẩn bị đầy đủ (nếu đã tạo theo Bước 1, 3, 4 tại [mục 0.2.3](#023-cách-1--lộ-trình-az-dựng-dữ-liệu-nền-trên-giao-diện-tài-khoản-superadmin) thì chỉ cần đối soát lại các tham số dưới đây):
1. **Tạo đăng ký gửi (Outbound):** Vui lòng xem và thực hiện theo **Bước 4 mục 0.2.3** (tạo đăng ký chiều Outbound cho đối tác `P-REST` với gói `101_commonData` và gói `103_vdsData`, chu kỳ gửi 30 giây).
2. **Tạo ánh xạ (Phễu lọc) chiều gửi:** *(Nếu chưa tạo ở Bước 3 mục 0.2.3)* Vào màn hình *Cấu hình ánh xạ* → Bấm nút **Thêm mới**:
   - Tab **Thông tin chung** (`info`):
     + Chọn tab chiều: **Gửi đi (Outbound)** (giá trị `Direction = 0`).
     + Chọn **Đối tác**: Chọn đối tác `P-REST`.
     + Chọn **Gói tin**: Chọn `101_commonData` (hoặc `103_vdsData`).
     + Nhập **Tên ánh xạ**: `Ánh xạ gửi gói 101 chuẩn`.
     + Bật công tắc **Sử dụng** (`isActive = true`).
   - Tab **Cấu hình ánh xạ** (`map`):
     + Dán khuôn JSON mẫu của đối tác vào ô bên trái:
       ```json
       {
         "partnerCode": "P-REST",
         "packetCode": "101",
         "sendTime": "2026-10-16 12:00:00",
         "serialNbr": 1,
         "data": [
           {
             "id": "1",
             "code": "ACTIVE",
             "time": "2026-10-16 12:00:00",
             "val": 100
           }
         ]
       }
       ```
     + Bấm **Phân tích** (biểu tượng cây đũa thần).
     + ⚠️ **RÀNG BUỘC BẮT BUỘC (Quy tắc `MappingCommandHandler.EnsureMetaFields`):** Chiều gửi bắt buộc phải khai đủ 4 khóa giá trị hệ thống `$meta`: `Now`, `Serial`, `PacketCode`, `PartnerCode`.
     + Cây ánh xạ bên phải:
       - Ánh xạ `partnerCode` → Nhóm hệ thống: `PartnerCode`.
       - Ánh xạ `packetCode` → Nhóm hệ thống: `PacketCode`.
       - Ánh xạ `sendTime` → Nhóm hệ thống: `Now` (định dạng ngày: `yyyy-MM-dd HH:mm:ss`).
       - Ánh xạ `serialNbr` → Nhóm hệ thống: `Serial`.
       - Trong mảng `data`: Ánh xạ `id` → trường nguồn `ID`, `val` → trường nguồn `Status`.
     + Bộ khung đầu ra sinh ra trong `TargetShapeJson`:
       ```json
       {
         "partnerCode": { "$meta": "PartnerCode" },
         "packetCode": { "$meta": "PacketCode" },
         "sendTime": { "$meta": "Now", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
         "serialNbr": { "$meta": "Serial" },
         "data": [
           {
             "id": { "$field": "ID" },
             "code": { "$field": "Code" },
             "time": { "$field": "UpdateTime", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
             "val": { "$field": "Status" }
           }
         ]
       }
       ```
     + Bấm nút **Lưu**.
3. **Kiểm tra CSDL:**
   ```sql
   SELECT ID, PartnerId, DatatypeId, Direction, IsActive, TargetShapeJson
   FROM dbo.ShareDataMapping
   WHERE Direction IN (0, 2) AND IsActive = 1 AND IsDelete IS NULL;
   ```

#### E0: Chuẩn bị chiều nhận (Bảng đích, câu ghi MERGE và Ánh xạ chiều nhận cho gói `E2E_INBOUND_TEST`)
Trước khi thực hiện các bài test nhóm E, cần chuẩn bị hạ tầng tiếp nhận dữ liệu cho gói kiểm thử riêng `E2E_INBOUND_TEST`:
> 📌 **Lý do sử dụng gói kiểm thử riêng `E2E_INBOUND_TEST` (thay vì gói `101_commonData`):**  
> Gói `101_commonData` là gói tin chuẩn của chiều GỬI (Outbound) dữ liệu dùng chung. Việc sử dụng gói kiểm thử riêng `E2E_INBOUND_TEST` giúp cô lập hoàn toàn môi trường kiểm thử E2E hai chiều, tránh xung đột cấu hình ánh xạ và đăng ký giữa luồng gửi D và luồng nhận E, đồng thời dễ dàng dọn dẹp sạch sẽ sau kiểm thử mà không đụng chạm tới cấu hình gói tin chuẩn.

1. **Bảng đích trên `DEV_ITS10_Inbound` và Cấu hình câu ghi `ShareDataPacketSql`:**
   - Đã được nạp sẵn tự động khi chạy **Khối 4 của script `05-sharedata-du-lieu-gia-lap.sql`** (tạo gói `E2E_INBOUND_TEST` ID `1500000000900`, tạo bảng `InboundTrafficFlow` trên `DEV_ITS10_Inbound` và chèn câu MERGE vào `ShareDataPacketSql` ID `1500000000903` cho gói `E2E_INBOUND_TEST`).
   - Nếu chưa chạy script 05, có thể thực thi câu lệnh SQL tại [Phụ lục D](#phụ-lục-d-mẫu-cấu-hình-chiều-nhận-e0-bảng-đích-câu-merge-và-targetshapejson).
2. **Tạo đăng ký nhận (Inbound):** Vào màn hình *Đối tác & Đăng ký chia sẻ* → Tab *Đăng ký chia sẻ* → Thêm mới:
   - Chọn tab chiều: **Nhận về (Inbound)** (`Direction = 1`).
   - Chọn **Đối tác**: `P-REST`.
   - Chọn **Gói tin**: `E2E_INBOUND_TEST`.
   - Đặt trạng thái: **Hoạt động** (`State = 1`).
   - Nhấn **Xác nhận**.
3. **Tạo ánh xạ chiều Nhận:** Vào màn hình *Cấu hình ánh xạ* → Bấm nút **Thêm mới**:
   - Tab **Thông tin chung**: Chọn tab **Nhận về (Inbound)** (`Direction = 1`), Đối tác: `P-REST`, Gói tin: `E2E_INBOUND_TEST`, Tên: `Ánh xạ nhận gói E2E_INBOUND_TEST`, Bật: `isActive = true`.
   - Tab **Cấu hình ánh xạ**: Khác với chiều gửi, chiều nhận **không yêu cầu các trường `$meta`**. `TargetShapeJson` cho chiều nhận chỉ cần chỉ định các trường dữ liệu đưa vào `@records` để câu lệnh `OPENJSON(@records)` bóc tách:
     ```json
     {
       "data": [
         {
           "id": { "$field": "id" },
           "code": { "$field": "code" },
           "time": { "$field": "time", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
           "val": { "$field": "val" }
         }
       ]
     }
     ```
   - Bấm **Lưu**.
4. **Dữ liệu JSON mẫu và lệnh gọi HTTP POST thử nghiệm tiếp nhận:**
   - Dữ liệu gửi mẫu (`body` JSON) tuân thủ cấu trúc của `TargetShapeJson` (mảng `data` chứa các trường `id`, `code`, `time`, `val`):
     ```json
     {
       "data": [
         {
           "id": "1",
           "code": "FLOW_01",
           "time": "2026-10-18 08:00:00",
           "val": 120
         }
       ]
     }
     ```
   - Câu lệnh `curl` gọi trực tiếp vào WebAPI local để đẩy gói tin (phục vụ nghiệm thu E1):
     ```bash
     curl -X POST http://localhost:5005/api/sharedata/shareDataInbound/receive \
       -H "Content-Type: application/json" \
       -H "PartnerCode: P-REST" \
       -H "PacketCode: E2E_INBOUND_TEST" \
       -H "SerialNbr: 1" \
       -d '{
         "data": [
           {
             "id": "1",
             "code": "FLOW_01",
             "time": "2026-10-18 08:00:00",
             "val": 120
           }
         ]
       }'
     ```

---

## 🅰️ A. Chế độ và trạng thái gói tin (màn hình *Cấu hình gói tin / SQL Center*, thư mục `dataSource`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| A1 | Mở danh sách gói tin | Tag chế độ mỗi gói **khớp cột `SyncMode` trong DB** (0.5): 101, 102, 105, 108, 110 là *gửi toàn bộ* (Snapshot); 103, 104, 106, 107, 109 là *nối đuôi* (Incremental) | ☐ Đạt ☐ Không đạt |
| A2 | Mở gói **105** (chưa chỉnh gì) | Chế độ mặc định **Snapshot / gửi toàn bộ** (không phải nối đuôi) | ☐ ☐ |
| A3 | Thử đổi gói 105 sang *nối đuôi* (Incremental / 2) trong khi vẫn giữ nguyên câu SQL dạng Snapshot (chưa có `TOP`, `ORDER BY`, cột con trỏ) → bấm Lưu. Sau đó cập nhật câu SQL đầy đủ cấu trúc nối đuôi (có `TOP (@pageSize)`, `__lastTime`, `__rowid`, và `ORDER BY __lastTime ASC, __rowid ASC`), đặt `CursorTimeColumn = 'UpdateTime'`, `MaxLookBackDays = 7` → bấm Lưu → tải lại trang và mở lại gói 105 | Khi câu SQL chưa đúng cấu trúc nối đuôi: **Bị từ chối lưu**, hệ thống hiển thị thông báo lỗi kiểm tra cấu trúc SQL nối đuôi (thiếu `TOP (@pageSize)`, thiếu `ORDER BY ... ASC`, hoặc thiếu `__lastTime`/`__rowid`). Sau khi sửa SQL chuẩn: **Lưu thành công**, mở lại giữ nguyên chế độ nối đuôi, cột mốc `UpdateTime`, quét lùi 7 ngày; tag trên danh sách đổi sang Nối đuôi | ☐ ☐ |
| A4 | Xóa trắng ô `MaxLookBackDays` → bấm Lưu → tải lại trang và mở lại gói 105 | Ô `MaxLookBackDays` hiển thị **rỗng** (giá trị `null` trong CSDL, không tự động quay về 7) | ☐ ☐ |
| A5 | Đổi gói 105 về lại chế độ *gửi toàn bộ* (Snapshot), khôi phục câu SQL mẫu chuẩn qua nút "Mẫu mặc định" → bấm Lưu | **Lưu thành công**; hệ thống không kiểm tra cấu trúc nối đuôi đối với gói Snapshot | ☐ ☐ |
| A6 | Xem gói **111** (và 110 nếu chưa bật) | Hiện **"Đã tắt"**; không có nút chạy/gửi | ☐ ☐ |
| A7 | Tạo gói mới mã chuẩn (ví dụ `105_rfidData` hoặc `101_commonData`) | Mở tab *Cấu hình SQL* thấy hiển thị SQL mẫu Catalog, chế độ đồng bộ và số ngày quét lùi khớp hoàn toàn theo [Phụ lục C](#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata); gửi dữ liệu bình thường mà không cần dán đè câu SQL | ☐ ☐ |

---

## 🅱️ B. SQL trích xuất và an toàn

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| B1 | Gói 101 → *Xem trước (preview)* | Có cột và dữ liệu thật, trả trong vài giây, không treo | ☐ Đạt ☐ Không đạt |
| B2 | SQL `SELECT … ORDER BY …` **không có TOP** → preview | Chạy được, không lỗi cột trùng tên | ☐ ☐ |
| B3 | Gói nối đuôi (103): bỏ `__lastTime` hoặc `__rowid` khỏi SELECT → Lưu | **Bị từ chối**, thông báo nêu rõ thiếu cột con trỏ bắt buộc | ☐ ☐ |
| B4 | Gói nối đuôi: đổi `ORDER BY … DESC` → Lưu | **Bị từ chối** (sắp giảm dần không hợp lệ cho con trỏ đồng bộ) | ☐ ☐ |
| B5 | Lưu lần lượt: `DELETE FROM TmsZone`; `SELECT 1; SELECT 2`; `EXEC sp_who`; `SELECT * FROM SysUser`; `SELECT * FROM ShareDataPartner`; `SELECT * FROM OtherDb.dbo.TmsZone` | **Mỗi câu đều bị từ chối** với thông báo bảo mật đúng ngôn ngữ đang chọn | ☐ ☐ |
| B6 | Nút **"Mẫu mặc định"** (`vqbDefaultTemplate`) | Khôi phục lại câu truy vấn SQL mẫu chuẩn theo mã gói từ hệ thống (Catalog/Seed), chưa ghi đè xuống CSDL cho tới khi người dùng bấm nút Lưu | ☐ ☐ |
| B7 | *Đã bỏ (SQL một tầng trên ShareDataPacket, không còn cấu hình SQL riêng theo đối tác)* | Đã chuẩn hóa kiến trúc: câu SQL đặt duy nhất tại `ShareDataPacket.ExtractSql`, không còn phân tầng SQL riêng cho đối tác | Bỏ qua (đã chuẩn hóa) |
| B8 | (Nếu có tài khoản không phải SuperAdmin) thử lưu SQL | **Bị chặn quyền** (chức năng lưu SQL yêu cầu quyền SuperAdmin) | ☐ ☐ |
| B9 | Nhập câu SQL đọc bảng ngoài danh mục `ShareDataSourceTable` (ví dụ `SELECT TOP 10 * FROM dbo.BangLaNgoaiDanhMuc;`) → bấm Dò cột, Xem mẫu hoặc Lưu | **Bị từ chối ngay lập tức**, thông báo hiển thị đúng ngôn ngữ theo khóa `SqlTableNotAllowed`: *"Bảng BangLaNgoaiDanhMuc chưa có trong danh mục bảng nguồn được phép chia sẻ. Thêm bảng vào ShareDataSourceTable (IsQueryable = 1) rồi thử lại."* (Yêu cầu đã chạy script 04 và bấm 🔄 làm mới từ điển) | ☐ Đạt ☐ Không đạt |
| B10 | Đặt `IsQueryable = 0` cho bảng `TmsWeather` trong CSDL: `UPDATE dbo.ShareDataSourceTable SET IsQueryable = 0, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;` → mở màn hình *Cấu hình gói tin* tab *Cấu hình SQL* | Bảng `TmsWeather` **biến mất** khỏi danh sách dropdown chọn Bảng nguồn của bộ dựng Visual Query Builder; nếu dán câu SQL chứa bảng `TmsWeather` thì bị từ chối khi Lưu/Dò cột (WebAPI chặn); tuy nhiên luồng Worker gửi gói 104 đang chạy nền **vẫn gửi dữ liệu bình thường** (vì Worker chỉ kiểm tra `IsTracked = 1`, hoàn toàn không kiểm tra `IsQueryable`) | ☐ ☐ |
| B11 | Bật lại `IsQueryable = 1` cho `TmsWeather`: `UPDATE dbo.ShareDataSourceTable SET IsQueryable = 1, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;` → tải lại trang *Cấu hình gói tin* | Bảng `TmsWeather` **xuất hiện trở lại ngay lập tức** trong dropdown chọn Bảng nguồn; các thao tác dò cột, xem trước và lưu câu SQL hoạt động bình thường | ☐ ☐ |

---

## 🅲 C. Đối tác và kết nối (màn *Đối tác & Đăng ký chia sẻ*, thư mục `sharing`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| C1 | Mở đối tác **P-REST** (đã tạo ở Bước 1 mục 0.2.3, hoặc tạo mới nếu chưa có): Address `http://localhost`, Port `9090`, EndPoint `/api/receive`, kiểu `API`. Chỉnh sửa tham số (ví dụ đổi Tên hoặc EndPoint) → Lưu / Xác nhận | Lưu thành công, mở lại giữ nguyên các giá trị đã cập nhật. Cổng (Port) và Mã (Code) hiển thị vô hiệu hóa (disabled), không cho phép sửa đổi | ☐ Đạt ☐ Không đạt |
| C2 | Trong cấu hình kết nối (nhập trên form nhóm hoặc bấm "Sửa JSON thô") thêm `Format=Json`, `Encryptor=None`, `Enabled=true` và 1 key tùy ý → Xác nhận → mở lại | Mọi key **giữ nguyên**; giá trị bí mật (Token/ApiKey/Password) **bị che** khi hiển thị | ☐ ☐ |
| C3 | Thử thêm key trùng tên cột (`Address`, `Port`, `EndPointApiUrl`), key trùng nhau, key rỗng (nhập qua chế độ "Sửa JSON thô"; yêu cầu đã chạy script 04 và bấm 🔄 đồng bộ từ điển) | **Từng trường hợp bị từ chối**, thông báo hiển thị theo ngôn ngữ đang chọn (nhóm lỗi: khóa trùng cột, khóa trùng nhau, khóa rỗng), không hiện mã lỗi thô chưa dịch | ☐ ☐ |
| C4 | Mở đối tác khác rồi quay lại P-REST | Cấu hình **không lẫn** sang nhau (không mang token qua đối tác khác) | ☐ ☐ |
| C5 | Tạo đối tác kiểu FTPS (rồi DISK, NAS) → Lưu được; mở lại giữ nguyên kiểu đã nhập | Được chấp nhận và gửi qua đúng kênh (FTPS bật `UseSsl`, DISK/NAS đi kênh File). Một giá trị lạ (ví dụ "ABC") vẫn bị từ chối | ☐ ☐ |
| C6 | Kiểm tra 12 trường hợp cấu hình kết nối đối tác sai (các trường hợp form chặn không cho nhập sai như JSON không phải object, cú pháp lỗi, cấu trúc lồng nhau, boolean sai định dạng, AuthType/Encryptor không hỗ trợ đã được kiểm chứng tự động 100% qua bộ test `ConnectConfigValidatorTests`; với các trường hợp nhập được qua "Sửa JSON thô" như key rỗng, key trùng tên cột, key bãi bỏ OutputPath, trùng lặp key, Timeout không nguyên dương, FileNamePattern thiếu token thì kiểm tra trực tiếp trên UI). Thử đặt khóa cũ `ArchiveEnabled=false` | Từng trường hợp sai bị từ chối với thông điệp đa ngôn ngữ chuẩn từ `BaseMsg.ShareData.Validation` (mã `ConnectConfigIssue` tương ứng). Riêng khóa cũ `ArchiveEnabled` **bị bỏ qua mà không báo lỗi**, cấu hình lưu bình thường | ☐ ☐ |

---

## 🅳 D. Gửi (chiều ra)

**Điều kiện:** Đã chuẩn bị ánh xạ chiều gửi theo **D0**; receiver chạy ở 0.4; Worker đang chạy; đối tác P-REST đã tạo. Tạo **đăng ký chiều gửi** (Outbound) cho gói cần thử với chu kỳ ngắn (~30 giây).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| D1 | Đăng ký gói **101** (Snapshot) | Receiver nhận request; body JSON đủ trường theo mapping; **số phần tử = số dòng nguồn** (không bị cắt cứng 100); header có `PartnerCode`, `PacketCode`, `SerialNbr` | ☐ Đạt ☐ Không đạt |
| D2 | Màn *Lịch sử chia sẻ* (`history`) | Có bản ghi **cha** và các bản ghi **con** theo bước/trang; mô tả **không nối lặp** | ☐ ☐ |
| D3 | Đăng ký gói **103** (nối đuôi), chờ 1 chu kỳ; sau đó chạy `UPDATE dbo.TmsTrafficData SET UpdateTime = GETDATE() WHERE ID IN (<3 ID bất kỳ>)` trên DB local; chờ chu kỳ kế | Chu kỳ sau gửi **đúng 3 dòng** vừa đổi; chu kỳ tiếp theo không có dòng mới → ghi *không có dữ liệu mới*, **không gửi lặp**; `ShareDataLastSend` tiến, không lùi | ☐ ☐ |
| D4 | Tạo file `FAIL` cạnh receiver → chờ chu kỳ gửi → xóa file `FAIL` → chờ chu kỳ | Khi lỗi: cảnh báo ở màn *Nhật ký lỗi* (`errorLog`), `ShareDataLastSend` **không tiến**; khi hết lỗi: gửi bù đủ dữ liệu bị lỡ, không sót | ☐ ☐ |
| D5 | Tìm tệp bằng chứng trong `NasStorage:BasePath` (mặc định `sharedata/send`, tương đối với thư mục chạy Worker) theo `Out/<Đối tác>/yyyy/MM/dd/<Gói>/…`; so sánh với `body` ở `received-<n>.json` | Tệp bằng chứng **LUÔN LUÔN được lưu** trong `sharedata/send` và **đúng từng byte** với bản đã gửi sang receiver | ☐ ☐ |
| D6 | Đặt khóa cũ `ArchiveEnabled=false` trong cấu hình kết nối đối tác, chờ 1 lần gửi | Khóa cũ bị bỏ qua, hệ thống **vẫn sinh tệp bằng chứng bình thường**; log cha không ghi chú không lưu | ☐ ☐ |
| D7 | Đối tác kiểu **File**: `DirectoryPath` nằm trong thư mục gốc `BasePath`, `FileNamePattern` có `{serial}`/`{timestamp}` | Sinh tệp đúng tên, mỗi trang một tệp, không ghi đè | ☐ ☐ |
| D8 | `DirectoryPath` ngoài thư mục cho phép; `FileNamePattern` có `../` hoặc `/` | **Bị từ chối**, không ghi tệp nào ngoài thư mục cho phép | ☐ ☐ |
| D9 | *(Cần NATS local cổng 4222)* Đăng ký gói **101** với *gửi ngay khi có dữ liệu mới* bật, `DebounceSec = 10`; chạy `UPDATE dbo.TmsZoneStatus SET UpdateTime = GETDATE() WHERE ID = '<ID>'` | Worker local (đã có sẵn `Nats__Enabled = true`) nhận sự kiện và gửi trong khoảng `DebounceSec`, **không đợi hết chu kỳ quét định kỳ** | ☐ ☐ |
| D10 | Đăng ký chế độ *Single* (một lần): gửi thành công; làm lại với receiver lỗi (`FAIL`) | Thành công → chuyển trạng thái *hết hạn* (Expired); lỗi → **giữ hoạt động** (Active) và thử lại ở chu kỳ sau | ☐ ☐ |
| D11 | *(Mã hóa chỉ viết sẵn `EncryptManager`, chưa nối luồng gửi/nhận theo quyết định 16/10/2026)* | Chưa nối vào luồng nghiệp vụ: **BỎ QUA**, chỉ nghiệm thu khi áp dụng mã hóa | Bỏ qua (chưa nối) |
| D12 | *(Tùy chọn — cần hạ tầng Kafka broker local)* Đối tác kiểu **Kafka**, `BootstrapServers` + `Topic` trong cấu hình | Message là byte[] UTF-8 JSON, có 4 header định danh; gửi lỗi (tắt broker) thì cảnh báo và mốc không tiến | ☐ Tùy chọn |
| D13 | *(Tùy chọn — cần hạ tầng FTP server local)* Đối tác kiểu **Ftp** (hoặc **FTPS**), `RemotePath` trong cấu hình | Tệp tải lên đúng nội dung, tên theo mẫu; sai mật khẩu thì lỗi rõ ràng, mốc không tiến | ☐ Tùy chọn |
| D14 | *(Tùy chọn — cần hạ tầng)* Cấu hình `Format=Csv` cho kênh **File** hoặc **Ftp** | Tệp CSV phân cách dấu phẩy, đúng thứ tự cột; bằng chứng lưu đúng byte như tệp đã gửi | ☐ Tùy chọn |

---

## 🅴 E. Nhận (chiều vào)

**Điều kiện:** Đã chuẩn bị hạ tầng tiếp nhận theo **E0** (gói kiểm thử riêng `E2E_INBOUND_TEST` ID `1500000000900`, bảng đích `InboundTrafficFlow` trên `DEV_ITS10_Inbound`, câu MERGE trong `ShareDataPacketSql` ID `1500000000903`, đối tác `P-REST` có đăng ký Inbound và ánh xạ chiều nhận cho gói `E2E_INBOUND_TEST`).

Dùng nội dung `body` đã chuẩn bị ở E0 làm dữ liệu thử nghiệm.

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| E1 | `POST /api/sharedata/shareDataInbound/receive` với header `PartnerCode: P-REST`, `PacketCode: E2E_INBOUND_TEST`, `SerialNbr: 1`; body = payload JSON mẫu ở E0 | HTTP **200**; `ShareDataInboundPacket` (CSDL Inbound) có 1 dòng mới; Worker xử lý xong, `ProcessState` chuyển sang hoàn tất (`Done`, giá trị số 2); dữ liệu được MERGE thành công vào bảng đích `InboundTrafficFlow` | ☐ Đạt ☐ Không đạt |
| E2 | Gửi lại **đúng gói cũ** với cùng `SerialNbr` | Câu lệnh MERGE thực thi cập nhật, **không nhân đôi số dòng** ở bảng đích (đảm bảo tính Idempotent) | ☐ ☐ |
| E3 | Gửi gói tin thiếu header `PartnerCode` hoặc đối tác không tồn tại trong CSDL | Bị từ chối ngay từ cổng WebAPI hoặc Worker ghi nhận lỗi `ESH-1504`, không sinh dòng dữ liệu mới trong bảng đích | ☐ ☐ |
| E4 | Body không đọc được (chuỗi rác sai định dạng JSON) | Gói ghi nhận trạng thái **lỗi** (`Failed`) với mã cảnh báo `ESH-1501` (lỗi đọc gói); không làm ảnh hưởng hoặc làm treo các gói khác | ☐ ☐ |
| E5 | *(Mã hóa chỉ viết sẵn `EncryptManager`, chưa nối luồng gửi/nhận theo quyết định 16/10/2026)* | Chưa nối vào luồng nghiệp vụ: **BỎ QUA**, chỉ nghiệm thu khi áp dụng mã hóa | Bỏ qua (chưa nối) |

---

## 🅵 F. Danh mục bảng nguồn và giám sát Change Tracking (bảng `ShareDataSourceTable`)

> ℹ️ **Chu kỳ nạp lại danh sách bảng (`ShareDataTracker:ConfigReloadSeconds`):** Trong `ShareDataWorker/appsettings.json` đang đặt **30 giây** (giá trị dự phòng trong code C# khi thiếu cấu hình là **60 giây**). Mọi thao tác kiểm tra, bật/tắt, thêm mới và xóa mềm dưới đây thực hiện trực tiếp bằng SQL trên bảng `dbo.ShareDataSourceTable` (CSDL `[DEV_ITS10]`).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| F1 | Chạy `SELECT TableName, Name, PacketCodes, OrderNo, IsTracked, IsQueryable, LastVersion, LastChangeTime, LastIssue FROM dbo.ShareDataSourceTable WHERE IsDelete IS NULL ORDER BY OrderNo;` | Thấy đúng 15 dòng danh mục bảng nguồn; 14 bảng theo dõi `IsTracked = 1` (Bật), riêng bảng `TmsEquipment` ở trạng thái **tắt theo dõi** (`IsTracked = 0`); cả 15 bảng đều có `IsQueryable = 1` (cho phép dùng trong bộ dựng SQL); các bảng đã bật Change Tracking có `LastVersion >= 0` và `LastIssue IS NULL` | ☐ Đạt ☐ Không đạt |
| F2 | Sửa 1 thiết bị trong bảng `TmsEquipment` (khi `IsTracked = 0`) | **Không** kích hoạt gửi các gói 102/103/108 (tránh trigger quét thừa khi chỉ bảo trì thiết bị) | ☐ ☐ |
| F3 | Bật theo dõi `TmsEquipment` (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 1, UpdateTime = GETDATE() WHERE TableName = 'TmsEquipment' AND IsDelete IS NULL;`), chờ ≤ `ConfigReloadSeconds` (30 giây theo `appsettings.json`, dự phòng 60 giây), sửa thiết bị, rồi **tắt lại** (`IsTracked = 0`) | Log Worker ghi *đã cập nhật cấu hình bảng giám sát (Thêm mới: 1)*; sửa thiết bị lúc bật thì **có** kích hoạt và tiến `LastVersion`, `LastChangeTime`; sau khi tắt lại thì dừng trigger | ☐ ☐ |
| F4 | Tắt theo dõi 1 bảng (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 0, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;`) | Bảng `TmsWeather` **ngừng** được theo dõi sau ≤ 30–60 giây (sau khi kiểm xong, khôi phục `IsTracked = 1`) | ☐ ☐ |
| F5 | Thêm 1 dòng với **tên bảng không tồn tại trong CSDL** (ví dụ `TableName = 'BangKhongTonTai_Test'`, `PacketCodes = '101_commonData'`, `IsTracked = 1`, `IsQueryable = 1`) | Worker ghi nhận cảnh báo `ESH-1603` và cập nhật lý do vào cột `LastIssue` của dòng đó; các bảng hợp lệ khác **vẫn tiếp tục được theo dõi bình thường** (xóa dòng test sau khi kiểm) | ☐ ☐ |
| F6 | **Tắt theo dõi tất cả** 15 dòng (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 0, UpdateTime = GETDATE() WHERE IsDelete IS NULL;`) | Worker ghi cảnh báo `ESH-1603` **đúng 1 lần**, không còn kích hoạt gửi tức thì; luồng quét định kỳ (D1/D3) **vẫn gửi bình thường** | ☐ ☐ |
| F7 | Bật lại theo dõi 14 dòng chuẩn (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 1, UpdateTime = GETDATE() WHERE IsDelete IS NULL AND TableName <> 'TmsEquipment';`) | Hệ thống tự động theo dõi trở lại sau ≤ 30–60 giây, giữ nguyên `LastVersion` của từng dòng, không quét lại từ đầu, không làm mất các thay đổi | ☐ ☐ |
| F8 | Thêm mới hoặc bật thêm 1 dòng bảng hợp lệ (`LastVersion IS NULL`) trong khi các bảng khác đang chạy ổn định (`LastVersion > 0`) | Chỉ riêng bảng mới được khởi tạo `LastVersion = CHANGE_TRACKING_CURRENT_VERSION()`; **giá trị `LastVersion` của 14 bảng đang chạy hoàn toàn không bị đặt lại (không reset)** | ☐ ☐ |
| F9 | Bảng không hợp lệ / chưa bật Change Tracking cùng tồn tại với các bảng hợp lệ đang có thay đổi dữ liệu | Dòng bảng lỗi được ghi chú vào cột `LastIssue`; các bảng hợp lệ còn lại vẫn claim `LastVersion` nguyên tử (`UPDATE ... OUTPUT`), cập nhật `LastChangeTime` và bắn sự kiện NATS bình thường | ☐ ☐ |

---

## 🅶 G. Gói 110 (Gói 110 chạy trên nguồn có sẵn — phương án A, 16/10/2026, Master Plan R13)

**Điều kiện:** Đã chạy script `05-sharedata-du-lieu-gia-lap.sql` để có mock dữ liệu `TmsWeather`, `TmsIncident`, `VmsCurrent`; Worker đang chạy.

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| G1 | Gói 110 đang bật; đăng ký REST tới receiver; đã chạy script 05 để có mock dữ liệu | Receiver nhận payload JSON có đủ các trường: `incidentMessage`, `weatherMessage`, `guidanceContent`, `locationKm`, `locationMet`, `publishedTime`; các trường `channel`, `deliveryState` để rỗng (chưa có nguồn) | ☐ Đạt ☐ Không đạt |
| G2 | Chờ chu kỳ kế tiếp | Gửi **toàn bộ** lại (Snapshot), không lưu con trỏ mốc thời gian | ☐ ☐ |
| G3 | Đặt gói 110 `Status = 0` (Tắt) | Dừng gửi, không sinh thêm cảnh báo mới | ☐ ☐ |
| G4 | Xem gói 111 | Gói 111 ở trạng thái tắt, không chạy, không phát sinh cảnh báo | ☐ ☐ |

---

## 🅷 H. Chung

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| H1 | Đổi ngôn ngữ giao diện vi ↔ en, lặp lại các bài kiểm tra B5, C3 và C6 | Toàn bộ thông báo lỗi và xác thực đổi ngôn ngữ đồng bộ, không còn chuỗi cứng tiếng Việt lẫn lộn khi đang chọn tiếng Anh | ☐ Đạt ☐ Không đạt |
| H2 | Dọn dẹp định kỳ (Cleanup) | Service dọn dẹp `DataCleanupService` được triển khai dưới dạng stub, chưa đăng ký vào DI và chưa nối vào luồng chạy chính thức theo thống nhất kỹ thuật 16/10/2026 (logic chi tiết lưu trong chú thích mã nguồn, Master Plan R15/DQ-SV-9). Cơ chế dọn nhật ký Change Tracking ESH-16 trong `DataChangeTrackingService` (SV-13) vẫn hoạt động độc lập bình thường. | Bỏ qua (chưa kích hoạt) |

---

## 🅸 I. Nghiệm thu toàn trình tự gửi — tự nhận nội bộ (In-Process Loopback)

Kịch bản chứng minh luồng toàn trình gửi → nhận hoạt động trọn vẹn khi chưa có đối tác bên ngoài hoặc không cần phần mềm bên ngoài hỗ trợ. Kịch bản này đã được khóa tự động bằng bài test `ShareDataSendReceiveTests` (chạy thật trên CSDL test local, không dùng mock).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| I1 | Cấu hình đối tác 'Nội bộ' trỏ endpoint về WebAPI cục bộ (`/api/sharedata/shareDataInbound/receive`), tạo đăng ký Outbound & Inbound cho gói Snapshot (105 hoặc 101) | Worker gửi dữ liệu thành công qua HTTP; Inbound tiếp nhận, lưu bằng chứng đĩa và MERGE vào bảng đích đầy đủ 100% cột | ☐ Đạt ☐ Không đạt |
| I2 | Gửi lặp lại cùng gói Snapshot (kiểm tra Idempotency) | MERGE không nhân đôi số dòng trong bảng đích | ☐ ☐ |
| I3 | Thử nghiệm gói Incremental (104) qua 2 chu kỳ: đợt 1 (3 dòng ban đầu), đợt 2 (1 dòng mới + 1 dòng cập nhật) | Đợt 1 nạp 3 dòng và tiến mốc; đợt 2 chỉ gửi 2 dòng thay đổi, bảng đích cập nhật đúng giá trị mới và tổng 4 dòng | ☐ ☐ |
| I4 | Thử nghiệm đối tác trỏ endpoint lỗi (404 Not Found) | Gửi thất bại sạch sẽ, mốc không tiến, tệp bằng chứng vẫn lưu trên đĩa, ghi cảnh báo vào `ShareDataAlertLog` | ☐ ☐ |

---

## 🅹 J. Tham số vận hành ShareData (SysOpConfig nhóm sharedata_config)

> ℹ️ **Cơ chế nạp và đồng bộ tham số:** Các tham số vận hành của phân hệ ShareData được lưu tập trung trong bảng `dbo.SysOpConfig` với `GroupCode = 'sharedata_config'` (gồm 46 tham số: 17 tham số Worker, 23 tham số FE và 6 tham số WebAPI). Người dùng có thể chỉnh sửa trực tiếp trên giao diện màn hình *Cấu hình hoạt động* (lọc theo loại cấu hình `sharedata_config`) hoặc sửa bằng câu lệnh SQL trên CSDL. Worker tự động nạp lại định kỳ sau mỗi chu kỳ `ConfigReloadSeconds` (mặc định 30 giây trong `appsettings.json`, dự phòng 60 giây).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| J1 | Khởi động Worker / WebAPI / FE khi trong CSDL chưa có hoặc thiếu dòng cấu hình (hoặc bị xóa mềm) | Hệ thống **tự động kích hoạt cơ chế dự phòng (fallback)**: lấy giá trị mặc định được định nghĩa sẵn trong mã nguồn (`DataWorkerOpConfigReader` cho Worker, `ShareDataConfigService` cho WebAPI, `SHAREDATA_FE_CONFIG_FALLBACK` cho FE); không phát sinh exception, không làm sập ứng dụng | ☐ Đạt ☐ Không đạt |
| J2 | Trên màn hình *Cấu hình hoạt động*, lọc nhóm `sharedata_config`, chỉnh sửa giá trị 1 tham số của Worker (ví dụ tăng `outbound_check_interval_sec` từ `5` lên `10`) → bấm Lưu | Sau thời gian ≤ `ConfigReloadSeconds` (30 giây), log console của Worker ghi nhận dòng thông báo phát hiện cấu hình thay đổi: ghi rõ giá trị cũ (`5`) → giá trị mới (`10`) và chu kỳ quét mới lập tức có hiệu lực | ☐ ☐ |
| J3 | Sửa giá trị tham số trong `SysOpConfig` thành chuỗi sai định dạng (ví dụ đặt `outbound_check_interval_sec = 'abc'`) hoặc đặt giá trị vượt quá khoảng cho phép (ví dụ `outbound_check_interval_sec = 9999`) | Worker nhận biết giá trị không hợp lệ, **tự động giữ/quay về giá trị dự phòng mặc định (5 giây)** và ghi **đúng 1 cảnh báo** ra log/alert (`ESH-1403` hoặc log cảnh báo cấu hình), không spam log liên tục | ☐ ☐ |
| J4 | Tắt dòng tham số bằng cách đổi trạng thái `Status = 0` (hoặc xóa mềm `IsDelete = GETDATE()`) trong `SysOpConfig` | Hệ thống coi như tham số không tồn tại trong DB và **tự động chuyển sang sử dụng giá trị mặc định trong mã**, đảm bảo luồng vận hành không bị gián đoạn | ☐ ☐ |
| J5 | Đặt `packet_lookback_days_max = 730` trong `SysOpConfig` → vào màn hình *Cấu hình gói tin* (SQL Center), chọn gói Incremental (103), nhập `MaxLookBackDays = 700` → Lưu | Giao diện WebVue đọc cấu hình từ API cho phép nhập đến 730 ngày (không bị chặn ở 365); WebAPI kiểm tra hợp lệ với cận trên 730 và **cho phép lưu thành công** xuống CSDL | ☐ ☐ |
| J6 | Kiểm tra tính đồng bộ ngưỡng tối thiểu chu kỳ gửi `subscription_interval_sec_min = 5`: thử nhập chu kỳ `3` giây trên form Đăng ký gói tin, hoặc gọi trực tiếp API tạo đăng ký với `IntervalSeconds = 3` | Cả giao diện Vue (`editSubscription.vue`) và backend `SubscriptionValidator` đều **đồng bộ chặn cứng** giá trị nhỏ hơn 5 giây với thông báo lỗi rõ ràng, không có sự sai lệch ngưỡng giữa FE và BE | ☐ ☐ |

---

---

## 📚 Phụ Lục

### Phụ lục A: Danh mục 25 mã cảnh báo lỗi ESH-xxxx

> 📌 *Nguồn trích xuất chuẩn:* [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) (`SysConfigType` = `'sharedata_error_code'`) và [`ShareDataAlertCode.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Services/ShareData/ShareDataWorker/Infrastructure/Logging/ShareDataAlertCode.cs).  
> ⚠️ **Chú ý giới hạn độ dài:** Ô *Tên* trên màn hình `sysConfigType` giới hạn tối đa 64 ký tự. Với **9 mã có tên gốc dài hơn 64 ký tự**, bảng dưới cung cấp sẵn **Tên rút gọn (≤ 64 ký tự)** để sử dụng khi nhập bằng tay trên giao diện.

| Mã lỗi | Phân loại | Mức độ | Tên gốc (khi chạy Script 03) | Tên rút gọn (dùng khi nhập tay trên giao diện, ≤ 64 ký tự) | Ghi chú & Hướng xử lý |
|---|---|:---:|---|---|---|
| **ESH-1201** | Outbound | ⚠️ warning | Bộ mã khai trong metadata không tồn tại trong CSDL | *(Giữ nguyên tên gốc — 52 ký tự)* | Kiểm tra lại cấu hình CodeSet trong metadata. |
| **ESH-1202** | Outbound | 🔴 danger | Trường bắt buộc bị thiếu hoặc null — huỷ kết xuất | *(Giữ nguyên tên gốc — 51 ký tự)* | Kiểm tra dữ liệu nguồn hoặc cấu hình trường bắt buộc. |
| **ESH-1203** | Outbound | ⚠️ warning | Biểu thức không qua kiểm tra an toàn tĩnh hoặc lỗi khi tính — giữ giá trị nguồn *(79 ký tự)* | **Lỗi biểu thức biến đổi an toàn tĩnh — giữ nguyên giá trị nguồn** *(62 ký tự)* | Kiểm tra biểu thức biến đổi trong mapping. |
| **ESH-1204** | Outbound | 🔴 danger | TargetShapeJson rỗng hoặc sai cú pháp — huỷ kết xuất | *(Giữ nguyên tên gốc — 54 ký tự)* | Kiểm tra định dạng TargetShapeJson trong mapping. |
| **ESH-1301** | Outbound | 🔴 danger | Không tìm thấy cấu hình gói tin cho DatatypeId | *(Giữ nguyên tên gốc — 48 ký tự)* | Kiểm tra danh mục gói tin ShareDataPacket. |
| **ESH-1302** | Outbound | 🔴 danger | Lỗi truy vấn dữ liệu nguồn của gói tin | *(Giữ nguyên tên gốc — 39 ký tự)* | Kiểm tra câu lệnh SQL trích xuất và kết nối CSDL. |
| **ESH-1303** | Outbound | 🔴 danger | Không tìm thấy phễu lọc (ShareDataMapping) chiều gửi đang bật — huỷ kết xuất *(76 ký tự)* | **Không có phễu lọc ShareDataMapping chiều gửi bật — hủy kết xuất** *(62 ký tự)* | Kiểm tra cấu hình mapping giữa đối tác và gói tin. |
| **ESH-1304** | Outbound | 🔴 danger | Khoá bản ghi cuối trang (__rowid) dài hơn cột LastKey cho phép — huỷ kết xuất trước khi gửi *(91 ký tự)* | **Khóa cuối trang __rowid dài hơn LastKey — hủy kết xuất gửi** *(58 ký tự)* | Kiểm tra kiểu dữ liệu hoặc giá trị khóa __rowid. |
| **ESH-1305** | Outbound | 🔴 danger | Không tìm thấy câu lệnh SQL trích xuất dữ liệu ở bất kỳ tầng nào *(67 ký tự)* | **Không tìm thấy câu lệnh SQL trích xuất dữ liệu của gói tin** *(58 ký tự)* | Kiểm tra câu truy vấn SQL trong ShareDataPacket. |
| **ESH-1306** | Outbound | 🔴 danger | Thứ tự con trỏ dữ liệu không tăng dần hoặc thiếu khóa __rowid | *(Giữ nguyên tên gốc — 63 ký tự)* | Kiểm tra mệnh đề ORDER BY trong câu lệnh trích xuất dữ liệu. |
| **ESH-1307** | Outbound | ⚠️ warning | Mốc con trỏ gửi mới lùi lại so với mốc hiện tại trong CSDL | *(Giữ nguyên tên gốc — 60 ký tự)* | Kiểm tra mốc đồng bộ con trỏ và dữ liệu thời gian. |
| **ESH-1401** | Outbound | ⚠️ warning | Gửi HTTP REST trực tiếp tới đối tác thất bại — file đã ghi vẫn coi là thành công *(80 ký tự)* | **Gửi HTTP REST thất bại — file đã ghi vẫn tính thành công** *(56 ký tự)* | Kiểm tra kết nối mạng và endpoint của đối tác. |
| **ESH-1402** | Outbound | 🔴 danger | Lưu đệm payload/bằng chứng thất bại, hủy gửi và giữ nguyên mốc | *(Giữ nguyên tên gốc — 64 ký tự)* | Kiểm tra quyền ghi thư mục NasStorage:BasePath hoặc dung lượng ổ đĩa. |
| **ESH-1403** | Outbound | ⚠️ warning | Giá trị cấu hình kết nối không hợp lệ bị bỏ qua hoặc thay bằng mặc định *(71 ký tự)* | **Cấu hình kết nối không hợp lệ — đã dùng giá trị mặc định** *(56 ký tự)* | Kiểm tra các khóa cấu hình trong ConnectionConfig của đối tác. |
| **ESH-1501** | Inbound | 🔴 danger | Không đọc được nội dung gói tin nhận về | *(Giữ nguyên tên gốc — 40 ký tự)* | Kiểm tra định dạng JSON của gói tin nhận. |
| **ESH-1502** | Inbound | 🔴 danger | Lỗi ghi dữ liệu xuống CSDL nhận | *(Giữ nguyên tên gốc — 33 ký tự)* | Kiểm tra kết nối và quyền ghi CSDL Inbound. |
| **ESH-1503** | Inbound | 🔴 danger | Không tìm thấy cấu hình gói tin cho DatatypeId nhận về | *(Giữ nguyên tên gốc — 55 ký tự)* | Kiểm tra danh mục gói tin chiều nhận trong ShareDataPacket. |
| **ESH-1504** | Inbound | 🔴 danger | Đối tác của gói tin không tồn tại hoặc đang tắt | *(Giữ nguyên tên gốc — 48 ký tự)* | Kiểm tra danh mục đối tác ShareDataPartner. |
| **ESH-1505** | Inbound | 🔴 danger | Gói tin chưa có đăng ký nhận hợp lệ | *(Giữ nguyên tên gốc — 36 ký tự)* | Kiểm tra cấu hình mapping chiều nhận ShareDataMapping. |
| **ESH-1506** | Inbound | 🔴 danger | Gói tin chưa khai câu ghi chiều nhận (ShareDataPacketWrite) | *(Giữ nguyên tên gốc — 60 ký tự)* | Kiểm tra cấu hình câu ghi CSDL trong ShareDataPacketSql. |
| **ESH-1507** | Inbound | 🔴 danger | Giải mã gói tin nhận về thất bại (sai khóa, sai envelope hoặc sai tag) *(70 ký tự)* | **Giải mã gói tin nhận thất bại (sai khóa, envelope hoặc tag)** *(58 ký tự)* | Kiểm tra thuật toán mã hóa hoặc khóa bí mật khi áp dụng. |
| **ESH-1601** | Tracking | ⚠️ warning | Mốc Change Tracking không hợp lệ — đã tự nhảy cóc để hồi phục | *(Giữ nguyên tên gốc — 63 ký tự)* | Hệ thống tự động đồng bộ lại mốc Change Tracking khi vượt cleanup. |
| **ESH-1602** | Tracking | 🔴 danger | Truy vấn CHANGETABLE lỗi vì lý do KHÁC version — đã bỏ cache câu SQL và hẹn quét lại *(84 ký tự)* | **Lỗi CHANGETABLE khác version — bỏ cache SQL và hẹn quét lại** *(58 ký tự)* | Kiểm tra cú pháp hoặc quyền truy cập CHANGETABLE trên bảng nguồn. |
| **ESH-1603** | Tracking | ⚠️ warning | CSDL hoặc bảng nguồn chưa bật Change Tracking — polling tạm dừng | *(Giữ nguyên tên gốc — 64 ký tự)* | Kiểm tra cấu hình ALTER DATABASE / TABLE ENABLE CHANGE_TRACKING. |
| **ESH-1604** | Tracking | ⚠️ warning | Mốc đang giữ không còn tương thích CSDL — đã hạ mốc về -1 để đi lại đường khởi tạo *(82 ký tự)* | **Mốc không tương thích CSDL — hạ mốc về -1 để khởi tạo lại** *(57 ký tự)* | Tracking bị tắt/bật lại hoặc CSDL restore từ bản cũ hơn. |

---

### Phụ lục B: Danh mục 15 bảng nguồn ShareData (`ShareDataSourceTable`)

> 📌 *Nguồn trích xuất chuẩn:* [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) (Mục 3) và seed class [`ShareDataSourceTableSeedData.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData/Infrastructure/Persistence/SeedData/ShareDataSourceTableSeedData.cs).

| STT | ID cố định (`ID`) | Tên bảng CSDL (`TableName`) | Tên hiển thị (`Name`) | Gói tin kích hoạt (`PacketCodes`) | Thứ tự (`OrderNo`) | Theo dõi CT (`IsTracked`) | Bộ dựng SQL (`IsQueryable`) | Phân loại (`Category`) | Bí danh (`DefaultAlias`) | Ghi chú vai trò (`Remark`) |
|:---:|:---:|---|---|---|:---:|:---:|:---:|---|:---:|---|
| 1 | `1500000000201` | `TmsZoneStatus` | Trạng thái khu vực giao thông | `101,101_commonData` | 10 | Bật (1) | Bật (1) | Giao thông & Thiết bị | `zs` | Giám sát thay đổi trạng thái zone để kích hoạt gói dữ liệu dùng chung (101). |
| 2 | `1500000000202` | `TmsZone` | Danh mục khu vực giao thông | `101,101_commonData` | 20 | Bật (1) | Bật (1) | Giao thông & Thiết bị | `z` | Giám sát thay đổi danh mục zone để kích hoạt gói dữ liệu dùng chung (101). |
| 3 | `1500000000203` | `TmsTrafficStatistic` | Thống kê lưu lượng giao thông | `101,101_commonData` | 30 | Bật (1) | Bật (1) | Giao thông & Thiết bị | `ts` | Giám sát dữ liệu thống kê giao thông để kích hoạt gói dữ liệu dùng chung (101). |
| 4 | `1500000000204` | `CctvDevice` | Thiết bị camera giám sát | `102,102_cctvData` | 40 | Bật (1) | Bật (1) | Giao thông & Thiết bị | `cd` | Giám sát dữ liệu camera để kích hoạt gói dữ liệu CCTV (102). |
| 5 | `1500000000205` | `TmsTrafficData` | Dữ liệu lưu lượng VDS và WIM | `103,103_vdsData,106,106_wimData` | 50 | Bật (1) | Bật (1) | Giao thông & Thiết bị | `td` | Giám sát dữ liệu đo đếm giao thông kích hoạt gói VDS (103) và WIM cân tải trọng (106). |
| 6 | `1500000000206` | `TmsWeather` | Dữ liệu khí tượng thời tiết | `104,104_weatherData` | 60 | Bật (1) | Bật (1) | Khí tượng & Cảnh báo | `w` | Giám sát dữ liệu trạm khí tượng để kích hoạt gói thời tiết (104). |
| 7 | `1500000000207` | `TollTransactionIn` | Giao dịch thu phí đầu vào | `105,105_rfidData,109,109_etcData` | 70 | Bật (1) | Bật (1) | Thu phí & Phương tiện | `ti` | Giám sát giao dịch vào trạm kích hoạt gói RFID (105) và gói ETC thu phí (109). |
| 8 | `1500000000208` | `TollTransactionOut` | Giao dịch thu phí đầu ra | `105,105_rfidData,109,109_etcData` | 80 | Bật (1) | Bật (1) | Thu phí & Phương tiện | `to` | Giám sát giao dịch ra trạm kích hoạt gói RFID (105) và gói ETC thu phí (109). |
| 9 | `1500000000209` | `TmsVehicleRegistration` | Đăng ký phương tiện | `105,105_rfidData` | 90 | Bật (1) | Bật (1) | Thu phí & Phương tiện | `vr` | Giám sát dữ liệu phương tiện đăng ký kích hoạt gói RFID (105). |
| 10 | `1500000000210` | `TmsIncident` | Sự cố giao thông | `107,107_incidentData,110,110_wpData,111` | 100 | Bật (1) | Bật (1) | Khí tượng & Cảnh báo | `inc` | Giám sát sự cố giao thông kích hoạt gói sự cố (107) và các gói tích hợp phụ trợ (110, 111). |
| 11 | `1500000000211` | `TmsEventType` | Loại sự kiện giao thông | `107,107_incidentData,110,110_wpData,111` | 110 | Bật (1) | Bật (1) | Khí tượng & Cảnh báo | `et` | Giám sát danh mục loại sự kiện giao thông kích hoạt gói sự cố (107) và phụ trợ (110, 111). |
| 12 | `1500000000212` | `VmsCurrent` | Thông tin bản tin biển báo VMS | `108,108_vmsInfo` | 120 | Bật (1) | Bật (1) | Biển báo & Thông tin | `vc` | Giám sát trạng thái hiển thị biển báo VMS kích hoạt gói tin VMS (108). |
| 13 | `1500000000213` | `TmsEquipment` | Danh mục thiết bị phần cứng | `102,102_cctvData,103,103_vdsData,108,108_vmsInfo` | 130 | **Tắt (0)** | Bật (1) | Giao thông & Thiết bị | `eq` | Bảng tra cứu thiết bị, mặc định tắt theo dõi để tránh trigger quét thừa cho 102/103/108 khi chỉ sửa thông tin thiết bị; vẫn cho phép truy vấn (IsQueryable = 1). |
| 14 | `1500000000214` | `TollLane` | Danh mục làn thu phí | `109,109_etcData` | 140 | Bật (1) | Bật (1) | Thu phí & Phương tiện | `tl` | Giám sát danh mục làn thu phí trạm kích hoạt gói dữ liệu ETC (109). |
| 15 | `1500000000215` | `TollStation` | Danh mục trạm thu phí | `109,109_etcData` | 150 | Bật (1) | Bật (1) | Thu phí & Phương tiện | `ts` | Giám sát danh mục trạm thu phí kích hoạt gói dữ liệu ETC (109). |

---

### Phụ lục C: Danh mục chuẩn 11 gói tin ShareData

> 📌 *Nguồn trích xuất chuẩn:* [`02-sharedata-seed-goi-tin.sql`](../sql/02-sharedata-seed-goi-tin.sql) và seed class `ShareDataPacketSeedData.cs` (`Module.ShareData.Infrastructure.Persistence.SeedData`).

| Gói | Số thứ tự (`OrderNo`) | Mã gói chuẩn (`Code`) | ID cố định | Tên gói tin (`Name`) | Chế độ (`SyncMode`) | Cột mốc (`CursorTimeColumn`) | Quét lùi (`MaxLookBackDays`) | Trạng thái (`Status`) |
|:---:|:---:|---|:---:|---|:---:|:---:|:---:|:---:|
| **101** | 101 | `101_commonData` | `1500000000101` | Dữ liệu thông tin chung / luồng giao thông | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **102** | 102 | `102_cctvData` | `1500000000102` | Dữ liệu hình ảnh giao thông CCTV | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **103** | 103 | `103_vdsData` | `1500000000103` | Dữ liệu đếm xe VDS | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **104** | 104 | `104_weatherData` | `1500000000104` | Dữ liệu khí tượng / thời tiết | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **105** | 105 | `105_rfidData` | `1500000000105` | Dữ liệu định danh phương tiện AVI / RFID | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **106** | 106 | `106_wimData` | `1500000000106` | Dữ liệu cân tải trọng xe tự động WIM | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **107** | 107 | `107_incidentData` | `1500000000107` | Dữ liệu thông tin sự cố / sự kiện giao thông | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **108** | 108 | `108_vmsInfo` | `1500000000108` | Dữ liệu thông tin biển báo điện tử VMS | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **109** | 109 | `109_etcData` | `1500000000109` | Dữ liệu giao dịch thu phí ETC | Incremental (Nối đuôi) | `UpdateTime` | 1 | Bật (1) |
| **110** | 110 | `110_wpData` | `1500000000110` | Dữ liệu trao đổi thông tin với người tham gia giao thông | Snapshot (Toàn bộ) | *NULL* | *NULL* | Bật (1) |
| **111** | 111 | `111` | `1500000000111` | Dữ liệu trao đổi thông tin giữa các TT QLĐHGT tuyến | *NULL* | *NULL* | *NULL* | **Tắt (0)** |

---

### Phụ lục D: Mẫu cấu hình chiều nhận E0 (Bảng đích, câu MERGE và TargetShapeJson)

> 📌 *Nguồn trích xuất chuẩn:* [`ShareDataSendReceiveTests.cs`](../../../../tests/BE/ITS/ShareData/Services/ShareDataSendReceiveTests.cs) và [`05-sharedata-du-lieu-gia-lap.sql`](../sql/05-sharedata-du-lieu-gia-lap.sql) Khối 4.  
> ℹ️ **Lý do dùng gói kiểm thử riêng `E2E_INBOUND_TEST`:** Gói `101_commonData` là gói tin chuẩn của chiều GỬI (Outbound). Sử dụng gói kiểm thử riêng `E2E_INBOUND_TEST` (ID `1500000000900`) giúp cô lập hoàn toàn môi trường kiểm thử E2E hai chiều, tránh xung đột cấu hình ánh xạ giữa luồng gửi D và luồng nhận E, đồng thời dễ dàng dọn dẹp sạch sẽ sau kiểm thử mà không đụng chạm dữ liệu sản xuất.

#### 1. Tạo gói kiểm thử riêng `E2E_INBOUND_TEST` trên CSDL `[DEV_ITS10]`
```sql
USE [DEV_ITS10];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ShareDataPacket WHERE Code = 'E2E_INBOUND_TEST' AND IsDelete IS NULL)
BEGIN
    INSERT INTO dbo.ShareDataPacket (
        [ID], [TenantId], [Code], [Name], [Status], [OrderNo], [SyncMode], [Remark], [CreateTime], [IsDelete]
    ) VALUES (
        '1500000000900', 1, 'E2E_INBOUND_TEST', N'Gói tin kiểm thử E2E chiều nhận', 1, 900, 1, N'Gói tin dùng cho kịch bản nghiệm thu chiều nhận E0', GETDATE(), NULL
    );
END
GO
```

#### 2. Tạo bảng đích trên CSDL `[DEV_ITS10_Inbound]`
```sql
USE [DEV_ITS10_Inbound];
GO

IF OBJECT_ID(N'[dbo].[InboundTrafficFlow]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[InboundTrafficFlow] (
        [id] NVARCHAR(64) NOT NULL PRIMARY KEY,
        [code] NVARCHAR(64) NULL,
        [recorded_time] DATETIME2 NULL,
        [val] INT NULL
    );
END
GO
```

#### 3. Cấu hình câu ghi MERGE vào bảng `[dbo].[ShareDataPacketSql]` trên CSDL `[DEV_ITS10]`
```sql
USE [DEV_ITS10];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ShareDataPacketSql WHERE PacketCode = 'E2E_INBOUND_TEST' AND TableName = 'dbo.InboundTrafficFlow' AND IsDelete IS NULL)
BEGIN
    INSERT INTO dbo.ShareDataPacketSql (
        [ID], [TenantId], [PacketCode], [OrderNo], [TableName], [WriteSql], [Status], [Remark], [CreateTime], [IsDelete]
    ) VALUES (
        '1500000000903', 1, 'E2E_INBOUND_TEST', 1, 'dbo.InboundTrafficFlow',
        N'MERGE dbo.InboundTrafficFlow AS target
USING (
    SELECT id, code, recorded_time, val
    FROM OPENJSON(@records)
    WITH (
        id NVARCHAR(64) ''$.id'',
        code NVARCHAR(64) ''$.code'',
        recorded_time DATETIME2 ''$.time'',
        val INT ''$.val''
    )
) AS src
ON target.id = src.id
WHEN MATCHED THEN
    UPDATE SET target.code = src.code, target.recorded_time = src.recorded_time, target.val = src.val
WHEN NOT MATCHED THEN
    INSERT (id, code, recorded_time, val) VALUES (src.id, src.code, src.recorded_time, src.val)
OUTPUT $action;',
        1, N'Cấu hình câu ghi MERGE chiều nhận mẫu cho gói kiểm thử E2E_INBOUND_TEST (Kịch bản E0)', GETDATE(), NULL
    );
END
GO
```

#### 4. Cấu hình `TargetShapeJson` cho Ánh xạ chiều Nhận (Inbound Mapping)
```json
{
  "data": [
    {
      "id": { "$field": "id" },
      "code": { "$field": "code" },
      "time": { "$field": "time", "$extend": { "dateFormat": "yyyy-MM-dd HH:mm:ss" } },
      "val": { "$field": "val" }
    }
  ]
}
```

---

### Phụ lục E: Danh mục thuật ngữ ShareData (SysTerminology)

> 📌 *Nguồn trích xuất chuẩn:* [`04-sharedata-dich-thuat.sql`](../sql/04-sharedata-dich-thuat.sql) và lớp hằng số [`BaseMsg.cs`](../../../../TA-ITS015-WEBAPI-V1.0/src/Modules/ShareData/Module.ShareData.Core/Exceptions/BaseMsg.cs).  
> ℹ️ Toàn bộ phân hệ ShareData bao gồm chính xác **49 mã khóa thuật ngữ**, tương ứng với **98 bản ghi song ngữ** (`vi-VN` và `en-US`) trong bảng `SysTerminology` (đã bổ sung khóa `sharedata.sqlTableNotAllowed` chặn truy vấn bảng ngoài danh mục `ShareDataSourceTable`). Khi nhập bằng tay trên giao diện màn hình *Hệ thống* → *Thuật ngữ*, người dùng tạo từng dòng với Tiền tố (`Name`), Mã (`Code`), và Giá trị (`Value`) cho từng ngôn ngữ.

| STT | Tiền tố (`Name`) | Mã khóa (`Code`) | Bản dịch tiếng Việt (`vi-VN`) | Bản dịch tiếng Anh (`en-US`) |
|:---:|---|---|---|---|
| 1 | `lz.validation` | `sharedata.lookbackDaysRange` | Số ngày quét lùi tối đa phải nằm trong khoảng từ {0} đến {1} ngày. | Maximum lookback days must be between {0} and {1} days. |
| 2 | `lz.exception` | `sharedata.superAdminRequiredForSqlEdit` | Chỉ tài khoản SuperAdmin mới có quyền chỉnh sửa câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to modify data source queries. |
| 3 | `lz.exception` | `sharedata.superAdminRequiredForSqlView` | Chỉ tài khoản SuperAdmin mới có quyền xem cấu hình câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to view data source query configurations. |
| 4 | `lz.exception` | `sharedata.superAdminRequiredForSqlValidate` | Chỉ tài khoản SuperAdmin mới có quyền kiểm tra câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to validate data source queries. |
| 5 | `lz.exception` | `sharedata.superAdminRequiredForSqlPreview` | Chỉ tài khoản SuperAdmin mới có quyền chạy thử câu truy vấn nguồn dữ liệu. | Only SuperAdmin accounts are authorized to preview data source query results. |
| 6 | `lz.exception` | `sharedata.superAdminRequiredForColumnSync` | Chỉ tài khoản SuperAdmin mới có quyền đồng bộ trường dữ liệu của gói tin. | Only SuperAdmin accounts are authorized to sync packet field definitions. |
| 7 | `lz.exception` | `sharedata.packetCodeNotFound` | Không tìm thấy gói tin có mã [{0}]. | Packet with code [{0}] was not found. |
| 8 | `lz.exception` | `sharedata.partnerCodeNotFound` | Không tìm thấy đối tác có mã [{0}]. | Partner with code [{0}] was not found. |
| 9 | `lz.exception` | `sharedata.partnerMappingNotFound` | Đối tác [{0}] chưa có hồ sơ ánh xạ chiều gửi đang bật cho gói tin [{1}]. | Partner [{0}] has no active outbound mapping profile for packet [{1}]. |
| 10 | `lz.validation` | `sharedata.packetCodeRequired` | Mã gói tin không được để trống. | Packet code is required. |
| 11 | `lz.validation` | `sharedata.extractSqlRequired` | Câu lệnh truy vấn SQL không được để trống. | SQL extract query is required. |
| 12 | `lz.exception` | `sharedata.superAdminRequiredForMetadata` | Chỉ tài khoản SuperAdmin mới có quyền truy cập siêu dữ liệu CSDL. | Only SuperAdmin accounts are authorized to access database metadata. |
| 13 | `lz.validation` | `sharedata.tableNameRequired` | Tên bảng nguồn không được để trống. | Source table name is required. |
| 14 | `lz.validation` | `sharedata.tableNameInvalid` | Tên bảng '{0}' không hợp lệ hoặc chứa ký tự bị cấm. | Table name '{0}' is invalid or contains forbidden characters. |
| 15 | `lz.validation` | `sharedata.tableNotAllowed` | Bảng '{0}' không thuộc danh mục được phép chia sẻ dữ liệu. | Table '{0}' is not in the allowed sharing catalog. |
| 16 | `lz.validation` | `sharedata.outputPathDeprecated` | Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]. | Attribute [OutputPath] is deprecated. Please use [DirectoryPath] and [FileNamePattern]. |
| 17 | `lz.validation` | `sharedata.sqlScriptEmpty` | Câu lệnh truy vấn không được để trống. | Query script is required. |
| 18 | `lz.validation` | `sharedata.sqlScriptSyntaxError` | Cú pháp SQL không hợp lệ (Dòng {0}, Cột {1}): {2} | Invalid SQL syntax (Line {0}, Column {1}): {2} |
| 19 | `lz.validation` | `sharedata.sqlScriptInvalid` | Cú pháp SQL không hợp lệ hoặc không chứa script thực thi. | SQL script is invalid or contains no executable statements. |
| 20 | `lz.validation` | `sharedata.sqlMultipleBatchesNotAllowed` | Chỉ chấp nhận duy nhất một lô lệnh (batch). Không được sử dụng lệnh phân đoạn 'GO'. | Only a single batch is allowed. 'GO' delimiter is not permitted. |
| 21 | `lz.validation` | `sharedata.sqlMultipleStatementsNotAllowed` | Chỉ chấp nhận duy nhất một câu lệnh SELECT (không cho phép đa câu lệnh). | Only a single SELECT statement is allowed (multiple statements are prohibited). |
| 22 | `lz.validation` | `sharedata.sqlOnlySelectAllowed` | Chỉ chấp nhận câu lệnh đọc dữ liệu bắt đầu bằng SELECT. | Only read statements starting with SELECT are allowed. |
| 23 | `lz.validation` | `sharedata.sqlSelectIntoNotAllowed` | Không cho phép sử dụng cú pháp SELECT ... INTO tạo bảng mới. | SELECT ... INTO syntax is not permitted. |
| 24 | `lz.validation` | `sharedata.sqlFunctionCallBlocked` | Không cho phép gọi hàm hoặc thủ tục: [{0}]. | Calling function or procedure [{0}] is not allowed. |
| 25 | `lz.validation` | `sharedata.sqlExternalDataSourcesBlocked` | Không cho phép sử dụng các nguồn dữ liệu ngoài (OPENROWSET/OPENQUERY/...). | External data sources (OPENROWSET/OPENQUERY/...) are not allowed. |
| 26 | `lz.validation` | `sharedata.sqlExecuteStatementBlocked` | Không cho phép thực thi câu lệnh EXEC/EXECUTE. | Executing EXEC/EXECUTE statements is not allowed. |
| 27 | `lz.validation` | `sharedata.sqlCrossDatabaseQueryBlocked` | Không được phép truy vấn bảng vượt ra ngoài CSDL hiện tại (tên bảng 3-4 phần: [{0}]). | Cross-database queries are not allowed (3-4 part table name: [{0}]). |
| 28 | `lz.validation` | `sharedata.sqlSystemSchemaBlocked` | Không được phép truy vấn schema hệ thống: [{0}]. | Querying system schema [{0}] is not allowed. |
| 29 | `lz.validation` | `sharedata.sqlSensitiveTableBlocked` | Không được phép truy vấn bảng hệ thống nhạy cảm: [{0}]. | Querying sensitive system table [{0}] is not allowed. |
| 30 | `lz.validation` | `sharedata.sqlUnsupportedParameter` | Tham số [@{0}] không được hỗ trợ. Chỉ hỗ trợ: @lastTime, @lastKey, @pageSize, @fromDate, @toDate. | Parameter [@{0}] is not supported. Supported parameters: @lastTime, @lastKey, @pageSize, @fromDate, @toDate. |
| 31 | `lz.validation` | `sharedata.sqlDatabaseExecutionError` | Cú pháp SQL không hợp lệ trên cơ sở dữ liệu: {0} | SQL syntax error on database: {0} |
| 32 | `lz.validation` | `sharedata.sqlIncrementalMissingTop` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề phân trang TOP (@pageSize). | Incremental queries must include a TOP (@pageSize) pagination clause. |
| 33 | `lz.validation` | `sharedata.sqlIncrementalMissingOrderBy` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có mệnh đề ORDER BY ở cấp ngoài cùng. | Incremental queries must include an outer ORDER BY clause. |
| 34 | `lz.validation` | `sharedata.sqlIncrementalOrderByDescNotAllowed` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc sắp xếp tăng dần (ASC); không cho phép sắp xếp giảm dần (DESC) ở mệnh đề ORDER BY ngoài cùng. | Incremental queries must sort ascending (ASC); descending (DESC) sorting is not allowed in the outer ORDER BY clause. |
| 35 | `lz.validation` | `sharedata.sqlIncrementalMissingLastTime` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ thời gian [__lastTime]. | Incremental queries must include the time cursor column [__lastTime]. |
| 36 | `lz.validation` | `sharedata.sqlIncrementalMissingRowid` | Truy vấn đồng bộ nối đuôi (Incremental) bắt buộc có cột con trỏ khóa [__rowid]. | Incremental queries must include the key cursor column [__rowid]. |
| 37 | `lz.validation` | `sharedata.connectConfigNotJsonObject` | Cấu hình kết nối (ConnectionConfig) phải là đối tượng JSON phẳng dạng key-value. | ConnectionConfig must be a flat key-value JSON object. |
| 38 | `lz.validation` | `sharedata.connectConfigSyntaxError` | Cấu hình kết nối (ConnectionConfig) sai cú pháp JSON. | ConnectionConfig contains invalid JSON syntax. |
| 39 | `lz.validation` | `sharedata.connectConfigEmptyKey` | Cấu hình kết nối (ConnectionConfig) không được chứa khóa (Key) rỗng. | ConnectionConfig cannot contain empty keys. |
| 40 | `lz.validation` | `sharedata.connectConfigReservedKey` | Khóa cấu hình bị cấm sử dụng vì là khóa dành riêng của hệ thống. | Configuration key is reserved by the system and cannot be used. |
| 41 | `lz.validation` | `sharedata.connectConfigObsoleteKey` | Thuộc tính [OutputPath] đã bị bãi bỏ. Vui lòng sử dụng [DirectoryPath] và [FileNamePattern]. | Attribute [OutputPath] is obsolete. Please use [DirectoryPath] and [FileNamePattern]. |
| 42 | `lz.validation` | `sharedata.connectConfigDuplicateKey` | Cấu hình kết nối (ConnectionConfig) chứa khóa bị trùng lặp (không phân biệt hoa thường). | ConnectionConfig contains duplicate keys (case-insensitive). |
| 43 | `lz.validation` | `sharedata.connectConfigNestedValue` | Cấu hình kết nối (ConnectionConfig) chỉ cho phép giá trị phẳng (chuỗi/số/boolean), không cho phép lồng mảng hoặc object con. | ConnectionConfig only allows flat primitive values (string/number/boolean); nested arrays or objects are not allowed. |
| 44 | `lz.validation` | `sharedata.connectConfigInvalidTimeout` | Giá trị Timeout phải là số nguyên dương (giây). | Timeout value must be a positive integer (seconds). |
| 45 | `lz.validation` | `sharedata.connectConfigInvalidBoolean` | Giá trị cấu hình boolean chỉ chấp nhận 'true' hoặc 'false'. | Boolean configuration value only accepts 'true' or 'false'. |
| 46 | `lz.validation` | `sharedata.connectConfigUnsupportedAuthType` | Giá trị AuthType chỉ hỗ trợ: None, Bearer, ApiKey, Basic. | AuthType only supports: None, Bearer, ApiKey, Basic. |
| 47 | `lz.validation` | `sharedata.connectConfigUnsupportedEncryptor` | Giá trị mã hóa (Encryptor / InboundEncryptor) chỉ hỗ trợ: None, AesGcm. | Encryptor / InboundEncryptor only supports: None, AesGcm. |
| 48 | `lz.validation` | `sharedata.connectConfigFileNamePatternNoToken` | Thuộc tính [FileNamePattern] bắt buộc chứa ít nhất một biến động ({serial}, {timestamp}, {packet} hoặc {partner}) để tránh ghi đè tệp. | Attribute [FileNamePattern] must contain at least one dynamic token ({serial}, {timestamp}, {packet}, or {partner}) to prevent file overwriting. |
| 49 | `lz.validation` | `sharedata.sqlTableNotAllowed` | Bảng {0} chưa có trong danh mục bảng nguồn được phép chia sẻ.<br>Thêm bảng vào ShareDataSourceTable (IsQueryable = 1) rồi thử lại. | Table {0} is not in the allowed sharing source tables.<br>Add the table to ShareDataSourceTable (IsQueryable = 1) and try again. |

---

### Phụ lục F: Danh mục 46 tham số vận hành ShareData (SysOpConfig nhóm sharedata_config)

> 📌 *Nguồn trích xuất chuẩn:* [`03-sharedata-seed-cau-hinh.sql`](../sql/03-sharedata-seed-cau-hinh.sql) (Mục 4), lớp cấu hình Worker `DataWorkerOpConfigReader.cs` (`ShareDataWorker.Core.Configuration`), dịch vụ WebAPI `ShareDataConfigService.cs` (`Module.ShareData/Services`), và hằng số dự phòng giao diện `SHAREDATA_FE_CONFIG_FALLBACK` (`TA-ITS015-WEBVUE-V1.0/src/src/types/constants/sharedataFeConfig.ts`).  
> ℹ️ Toàn bộ 46 tham số được phân nhóm theo tiền tố và thành phần sử dụng: 17 tham số Worker (nhóm A), 23 tham số FE/WebAPI dùng chung (nhóm B), và 6 tham số WebAPI/SQL Center (nhóm C). Người dùng có thể tra cứu và điều chỉnh trên màn hình *Cấu hình hoạt động* (lọc theo nhóm `sharedata_config`).

| STT | Mã tham số (`Code`) | Tên hiển thị (`Name`) | Mặc định (`Value`) | Thứ tự (`OrderNo`) | Khoảng hợp lệ & Đơn vị | Thành phần sử dụng | Ý nghĩa vận hành chi tiết |
|:---:|---|---|:---:|:---:|---|:---:|---|
| **A. Tham số Worker (17 mã)** |
| 1 | `outbound_check_interval_sec` | Chu kỳ quét gửi gói tin (giây) | `5` | 10 | 1–300 giây | Worker | Chu kỳ vòng lặp quét gửi gói tin của Outbound Worker. |
| 2 | `inbound_check_interval_sec` | Chu kỳ quét nhận gói tin (giây) | `5` | 11 | 1–300 giây | Worker | Chu kỳ vòng lặp quét nhận gói tin của Inbound Worker. |
| 3 | `tracking_poll_interval_sec` | Chu kỳ quét Change Tracking (giây) | `1` | 12 | 1–60 giây | Worker | Chu kỳ vòng lặp quét SQL Server Change Tracking của Tracking Worker. |
| 4 | `outbound_max_pages_per_run` | Số trang trích xuất tối đa mỗi lượt gửi | `20` | 20 | 1–1000 trang | Worker | Số trang trích xuất tối đa mỗi lượt chạy của một đăng ký gửi. |
| 5 | `outbound_lock_budget_percent` | Ngân sách thời gian giữ khóa gửi (%) | `50` | 21 | 10–90 % | Worker | Tỷ lệ ngân sách thời gian giữ khóa phân tán mỗi lượt gửi. |
| 6 | `outbound_lock_min_sec` | Thời gian giữ khóa gửi tối thiểu (giây) | `300` | 22 | 30–3600 giây | Worker | Thời gian giữ khóa phân tán tối thiểu của đăng ký gửi. |
| 7 | `outbound_lock_interval_multiplier` | Hệ số nhân chu kỳ gửi tính thời gian giữ khóa | `3` | 23 | 1–10 lần | Worker | Hệ số nhân với chu kỳ gửi để tính thời gian giữ khóa phân tán. |
| 8 | `outbound_page_size` | Số dòng mỗi trang trích xuất gửi | `100` | 24 | 1–10000 dòng | Worker | Số dòng dữ liệu mặc định mỗi trang trích xuất gửi đi. |
| 9 | `outbound_default_interval_sec` | Chu kỳ gửi mặc định của Worker (giây) | `30` | 25 | 1–86400 giây | Worker | Chu kỳ gửi mặc định khi đăng ký không khai báo IntervalSeconds. |
| 10 | `inbound_max_packets_per_run` | Số gói tin nhận xử lý tối đa mỗi lượt | `50` | 30 | 1–1000 gói | Worker | Số gói tin Pending xử lý tối đa mỗi lượt của Inbound Worker. |
| 11 | `inbound_stuck_timeout_min` | Ngưỡng thu hồi gói nhận bị kẹt (phút) | `10` | 31 | 1–1440 phút | Worker | Ngưỡng thời gian thu hồi gói tin Inbound bị kẹt ở trạng thái Processing. |
| 12 | `inbound_warn_interval_min` | Chu kỳ tối thiểu ghi cảnh báo lặp Inbound (phút) | `5` | 32 | 1–1440 phút | Worker | Chu kỳ tối thiểu giữa hai lần ghi cảnh báo lặp lại ở Inbound Worker. |
| 13 | `inbound_write_timeout_sec` | Thời gian chờ ghi SQL chiều nhận (giây) | `120` | 33 | 10–3600 giây | Worker | Thời gian chờ tối đa khi thực thi câu lệnh SQL ghi dữ liệu nhận về. |
| 14 | `tracking_table_retry_min` | Chu kỳ thử lại bảng nguồn thiếu Change Tracking (phút) | `5` | 40 | 1–1440 phút | Worker | Chu kỳ thử khôi phục bảng nguồn bị cô lập do thiếu Change Tracking. |
| 15 | `tracking_log_retention_days` | Số ngày lưu giữ nhật ký hạ tầng Change Tracking | `7` | 41 | 1–365 ngày | Worker | Số ngày lưu giữ nhật ký hạ tầng Change Tracking mã ESH-16xx. |
| 16 | `tracking_log_purge_batch` | Số dòng xóa mỗi lô nhật ký Change Tracking | `1000` | 42 | 100–50000 dòng | Worker | Số dòng xóa tối đa mỗi lô khi dọn nhật ký hạ tầng Change Tracking. |
| 17 | `tracking_change_retention_days` | Số ngày giữ lịch sử CHANGE_RETENTION khi bật CT | `2` | 43 | 1–30 ngày | Worker | Số ngày giữ lịch sử thay đổi khi tự bật Change Tracking cho CSDL. |
| **B. Tham số Giới hạn Giao diện FE & Dùng chung WebAPI (23 mã)** |
| 18 | `subscription_interval_sec_min` | Chu kỳ gửi - tối thiểu | `5` | 101 | 1–86400 giây | FE / BE | Giới hạn dưới chu kỳ gửi khi đăng ký gói tin (chặn cùng ngưỡng ở FE và BE). |
| 19 | `subscription_interval_sec_max` | Chu kỳ gửi - tối đa | `86400` | 102 | 1–86400 giây | FE / BE | Giới hạn trên chu kỳ gửi khi đăng ký gói tin (tối đa 24 giờ). |
| 20 | `subscription_interval_sec_default` | Chu kỳ gửi - mặc định | `30` | 103 | Giây | FE | Giá trị chu kỳ gửi mặc định hiển thị trên form Đăng ký gói tin. |
| 21 | `subscription_debounce_sec_min` | Thời gian gom sự kiện - tối thiểu | `0` | 111 | 0–3600 giây | FE / BE | Giới hạn dưới thời gian gom sự kiện NATS khi đăng ký gói tin. |
| 22 | `subscription_debounce_sec_max` | Thời gian gom sự kiện - tối đa | `3600` | 112 | 0–3600 giây | FE / BE | Giới hạn trên thời gian gom sự kiện NATS khi đăng ký gói tin. |
| 23 | `subscription_debounce_sec_default` | Thời gian gom sự kiện - mặc định | `30` | 113 | Giây | FE | Giá trị thời gian gom sự kiện mặc định hiển thị trên form Đăng ký gói tin. |
| 24 | `subscription_priority_min` | Độ ưu tiên - tối thiểu | `0` | 121 | 0–10 | FE / BE | Giới hạn dưới mức độ ưu tiên hàng đợi của đăng ký gửi. |
| 25 | `subscription_priority_max` | Độ ưu tiên - tối đa | `10` | 122 | 0–10 | FE / BE | Giới hạn trên mức độ ưu tiên hàng đợi của đăng ký gửi. |
| 26 | `subscription_priority_default` | Độ ưu tiên - mặc định | `5` | 123 | 0–10 | FE | Mức độ ưu tiên mặc định khi tạo mới đăng ký gửi gói tin. |
| 27 | `partner_heartbeat_sec_min` | Heartbeat - tối thiểu | `5` | 201 | 1–300 giây | FE | Giới hạn dưới chu kỳ gửi heartbeat kiểm tra kết nối đối tác. |
| 28 | `partner_heartbeat_sec_max` | Heartbeat - tối đa | `300` | 202 | 1–300 giây | FE | Giới hạn trên chu kỳ gửi heartbeat kiểm tra kết nối đối tác. |
| 29 | `partner_heartbeat_sec_default` | Heartbeat - mặc định | `30` | 203 | Giây | FE | Chu kỳ gửi heartbeat mặc định khi tạo mới đối tác. |
| 30 | `partner_response_timeout_sec_min` | Thời gian chờ phản hồi - tối thiểu | `3` | 211 | 1–60 giây | FE | Giới hạn dưới thời gian chờ đối tác phản hồi bắt tay kết nối. |
| 31 | `partner_response_timeout_sec_max` | Thời gian chờ phản hồi - tối đa | `60` | 212 | 1–60 giây | FE | Giới hạn trên thời gian chờ đối tác phản hồi bắt tay kết nối. |
| 32 | `partner_response_timeout_sec_default` | Thời gian chờ phản hồi - mặc định | `10` | 213 | Giây | FE | Thời gian chờ phản hồi mặc định khi tạo mới đối tác. |
| 33 | `partner_datagram_size_min` | Kích thước datagram - tối thiểu | `512` | 221 | 512–65507 Byte | FE | Giới hạn dưới kích thước gói tin datagram của kênh UDP. |
| 34 | `partner_datagram_size_max` | Kích thước datagram - tối đa | `65507` | 222 | 512–65507 Byte | FE | Giới hạn trên kích thước datagram (giới hạn phần cứng UDP 65507 Byte). |
| 35 | `partner_datagram_size_default` | Kích thước datagram - mặc định | `1024` | 223 | Byte | FE | Kích thước gói tin datagram mặc định khi tạo mới đối tác UDP. |
| 36 | `mapping_sample_top_min` | Số bản ghi mẫu - tối thiểu | `1` | 301 | 1–50 bản ghi | FE / BE | Giới hạn dưới số lượng bản ghi mẫu trích xuất khi cấu hình ánh xạ. |
| 37 | `mapping_sample_top_max` | Số bản ghi mẫu - tối đa | `50` | 302 | 1–50 bản ghi | FE / BE | Giới hạn trên số lượng bản ghi mẫu trích xuất khi cấu hình ánh xạ. |
| 38 | `mapping_sample_top_default` | Số bản ghi mẫu - mặc định | `5` | 303 | Bản ghi | FE / BE | Số lượng bản ghi mẫu mặc định khi bấm xem mẫu tại màn hình Ánh xạ. |
| 39 | `history_range_default_days` | Khoảng tra cứu - mặc định | `7` | 401 | 1–31 ngày | FE | Khoảng thời gian mặc định khi mở màn hình Lịch sử chia sẻ và Nhật ký lỗi. |
| 40 | `history_range_max_days` | Khoảng tra cứu - tối đa | `31` | 402 | 1–31 ngày | FE | Khoảng thời gian tra cứu tối đa cho phép chọn trên bộ lọc ngày. |
| **C. Tham số WebAPI Backend & SQL Center (6 mã)** |
| 41 | `packet_lookback_days_min` | Số ngày quét lùi tối đa - tối thiểu | `1` | 501 | ≥ 1 ngày | BE / FE | Giới hạn dưới của MaxLookBackDays khi cấu hình SQL trích xuất gói tin. |
| 42 | `packet_lookback_days_max` | Số ngày quét lùi tối đa - tối đa | `365` | 502 | 1–730 ngày | BE / FE | Giới hạn trên của MaxLookBackDays (mặc định 365, có thể tăng lên 730). |
| 43 | `sql_sample_top_min` | Số dòng xem mẫu SQL Center - tối thiểu | `1` | 511 | 1–50 bản ghi | BE / FE | Giới hạn dưới khi chạy thử SQL trích xuất tại SQL Center. |
| 44 | `sql_sample_top_max` | Số dòng xem mẫu SQL Center - tối đa | `50` | 512 | 1–50 bản ghi | BE / FE | Giới hạn trên khi chạy thử SQL trích xuất tại SQL Center. |
| 45 | `sql_sample_top_default` | Số dòng xem mẫu SQL Center - mặc định | `10` | 513 | Bản ghi | BE / FE | Số dòng xem mẫu mặc định khi chạy thử câu SQL tại SQL Center. |
| 46 | `log_list_default_top` | Số dòng nhật ký trả về mặc định | `1000` | 521 | 1–10000 bản ghi | BE | Số dòng mặc định trả về khi truy vấn danh sách ActivityLog / AlertLog. |

---

## 🧹 Dọn sau khi test

1. **Khôi phục cấu hình hệ thống & môi trường debug:**
   - Trả gói tin 105 về chế độ gửi toàn bộ ban đầu (Snapshot - theo bước A5).
   - Xóa bỏ bản sao cấu hình debug tạm trong file [`.vscode/launch.json`](../../../../.vscode/launch.json) (nếu đã tạo ở mục 0.2.2).
   - Đảm bảo các cờ CodeFirst/Seed trong file cấu hình dự án giữ nguyên giá trị `false`.
   - Nếu đã thêm biến môi trường `"Nats__Enabled": "false"` hoặc tùy chỉnh cổng NATS, trả lại như ban đầu.
2. **Khôi phục danh mục 15 bảng nguồn (`ShareDataSourceTable`):**
   - Trả lại trạng thái ban đầu của 15 dòng trong bảng `dbo.ShareDataSourceTable` theo đúng [Phụ lục B](#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable) (xóa các dòng bảng thử nghiệm như `BangKhongTonTai_Test`, khôi phục `IsDelete = NULL`, đưa `TmsEquipment` về trạng thái tắt theo dõi `IsTracked = 0`, 14 bảng còn lại `IsTracked = 1`, và cả 15 bảng `IsQueryable = 1`):
     ```sql
     USE [DEV_ITS10];
     DELETE FROM dbo.ShareDataSourceTable
     WHERE ID NOT BETWEEN '1500000000201' AND '1500000000215';

     UPDATE dbo.ShareDataSourceTable
     SET IsDelete = NULL,
         IsTracked = CASE WHEN TableName = N'TmsEquipment' THEN 0 ELSE 1 END,
         IsQueryable = 1,
         UpdateTime = GETDATE()
     WHERE ID BETWEEN '1500000000201' AND '1500000000215';
     ```
3. **Dọn dẹp dữ liệu thử nghiệm nghiệp vụ:**
   - Xóa các bản ghi đăng ký chia sẻ (Subscription) và phễu lọc (Mapping) đã tạo thử nghiệm cho kịch bản D0 và E0.
   - Xóa đối tác thử nghiệm `P-REST` (trên giao diện hoặc đặt `IsDelete = GETDATE()`).
   - Xóa dữ liệu thử nghiệm trong bảng đích chiều nhận:
     ```sql
     USE [DEV_ITS10_Inbound];
     TRUNCATE TABLE [dbo].[InboundTrafficFlow];
     ```
   - Xóa cấu hình gói kiểm thử riêng `E2E_INBOUND_TEST` khỏi `ShareDataPacketSql` và `ShareDataPacket`:
     ```sql
     USE [DEV_ITS10];
     DELETE FROM dbo.ShareDataPacketSql WHERE ID = '1500000000903' OR PacketCode = 'E2E_INBOUND_TEST';
     DELETE FROM dbo.ShareDataPacket WHERE ID = '1500000000900' OR Code = 'E2E_INBOUND_TEST';
     ```
4. **Dọn dẹp tệp tin trên ổ đĩa:**
   - Xóa các tệp `received-*.json` và file `FAIL` do receiver sinh ra trong quá trình kiểm thử.
   - Xóa các thư mục/tệp bằng chứng sinh ra trong thư mục `sharedata/send` (đường dẫn tương đối của Worker).
   - Dọn dẹp topic/tệp thử cho D12–D14 nếu có chạy thử nghiệm: xóa topic/message thử trên Kafka broker, xóa tệp đã tải lên trên FTP server local và thư mục File local.

---

## 📋 Danh sách kịch bản bỏ qua và lý do

| Mã kịch bản | Phân hệ | Trạng thái | Lý do chi tiết |
|:---:|---|:---:|---|
| **D11** | Chiều gửi | **Bỏ qua** | Cơ chế mã hóa đối tác hiện tại chỉ mới viết sẵn thư viện lõi `EncryptManager` và các bộ mã hóa (AesGcm, Rsa...), chưa được đấu nối vào luồng xử lý nghiệp vụ gửi/nhận thực tế theo quyết định kỹ thuật ngày 16/10/2026. |
| **E5** | Chiều nhận | **Bỏ qua** | Tương tự D11, tầng giải mã chiều nhận chưa được kích hoạt trong luồng Worker Inbound. |
| **D9** | Chiều gửi | **Tùy chọn** | Kịch bản gửi ngay khi có dữ liệu mới qua NATS. Cấu hình debug Worker local đã bật sẵn `Nats__Enabled = true`, nhưng chỉ thực hiện được khi máy trạm của lập trình viên có chạy NATS Server local (cổng 4222). Nếu không có NATS, hệ thống vẫn hoạt động bình thường theo cơ chế quét định kỳ. |
| **D12** | Chiều gửi | **Tùy chọn** | Kênh truyền Kafka yêu cầu máy chủ Kafka broker local đang hoạt động. |
| **D13** | Chiều gửi | **Tùy chọn** | Kênh truyền Ftp/FTPS yêu cầu máy chủ FTP server local đang hoạt động. |
| **D14** | Chiều gửi | **Tùy chọn** | Định dạng CSV cho kênh File/Ftp yêu cầu cấu hình đối tác và môi trường tương ứng. |
| **H2** | Hệ thống chung | **Bỏ qua** | Service dọn dẹp `DataCleanupService` được triển khai dưới dạng stub, chưa đăng ký vào DI và chưa nối vào luồng chạy chính thức theo thống nhất kỹ thuật 16/10/2026 (logic chi tiết lưu trong chú thích mã nguồn, Master Plan R15/DQ-SV-9). |

---

## 🎯 Tiêu chí đạt

- **Nhóm A–F (Cấu hình gói tin, SQL an toàn, Đối tác & Kết nối, Chiều gửi, Chiều nhận, Bảng theo dõi):** Đạt 100% các tiêu chí kiểm thử bắt buộc.
- **Nhóm G (Gói 110):** Đạt luồng gửi dữ liệu giả lập từ nguồn có sẵn theo phương án A.
- **Nhóm H (Đa ngôn ngữ):** Đạt 100%, không phát sinh chuỗi cứng hoặc lỗi hiển thị bản dịch khi chuyển đổi vi ↔ en.
- **Nhóm I (Nghiệm thu toàn trình In-Process Loopback):** Đạt toàn bộ luồng tự gửi – tự nhận nội bộ (đã được xác nhận tự động qua bộ bài test tích hợp `ShareDataSendReceiveTests`).
- Kết quả tổng hợp sau khi hoàn tất được ghi nhận vào mục **`DQ-TEST-3`** của `Sharedata_MasterPlan.md`.
