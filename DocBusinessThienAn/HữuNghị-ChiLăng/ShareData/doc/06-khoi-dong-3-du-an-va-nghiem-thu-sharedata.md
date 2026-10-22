# 🚀 Khởi Động Dự Án & Nghiệm Thu Nghiệp Vụ Thực Tế — Phân Hệ ShareData

> 📅 **Soạn & Cập nhật:** 19/10/2026 · **Người chạy:** người dùng tự khởi động dự án và kiểm tay · **Môi trường:** DB **local/test** (mục 1–4) và **staging** (mục 5).  
> 📌 **Điều kiện tiên quyết:** Bắt buộc đã hoàn thành khởi tạo CSDL và chạy xong toàn bộ script SQL tại [05-chay-sql-tu-dau-sharedata.md](05-chay-sql-tu-dau-sharedata.md).  
> 📌 **Căn cứ:** [`Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) PHẦN VII, [`02-mapping-goi-tin-101-111.md`](02-mapping-goi-tin-101-111.md), code Worker/WebAPI/FE tại thời điểm soạn.  
> 📌 **Phạm vi:** Nghiệm thu luồng nghiệp vụ với **API và DB thật**. Phần Playwright với API giả lập đã chấp nhận (R16 bỏ); kịch bản này thay cho nó.  
> 📌 **Nguyên tắc ghi nhận:** Mỗi mục có: **Bước** → **Kết quả mong đợi** → **☐ Đạt / ☐ Không đạt**. Ghi lỗi vào bảng cuối tài liệu.

---

## 🗺️ Lộ Trình Nhanh (6 Bước Nghiệm Thu)

1. [Khởi động 3 dự án theo thứ tự: WebAPI → Worker → Frontend](#1-điều-kiện--khởi-động-3-dự-án-webapi-sharedataworker-webvue) (Mục 1)
2. [Bật receiver giả lập & Làm mới từ điển đa ngôn ngữ](#2-receiver-giả-lập--làm-mới-từ-điển-đa-ngôn-ngữ) (Mục 2)
3. [Dựng dữ liệu nền trên giao diện WebVue & Chuẩn bị D0, E0](#3-chuẩn-bị-dữ-liệu-qua-giao-diện-webvue--chuẩn-bị-d0-e0) (Mục 3)
4. [Thực hiện nghiệm thu chi tiết từng phân hệ từ A đến J](#4-nghiệm-thu-chi-tiết-từng-phân-hệ-từ-a-đến-j) (Mục 4: A–J)
5. [Chạy nghiệm thu trên staging (khi kiểm thử môi trường staging)](#5-chạy-nghiệm-thu-trên-staging) (Mục 5)
6. [Dọn dẹp môi trường và tệp tin sau kiểm thử](#6-dọn-dẹp-sau-khi-test-phần-không-phải-sql) (Mục 6)

---

## 1. Điều kiện & Khởi động 3 dự án (WebAPI, ShareDataWorker, WebVue)

### 1.1. Điều kiện tiên quyết
- Đã hoàn tất khởi tạo CSDL và chạy xong toàn bộ script SQL bắt buộc tại [05-chay-sql-tu-dau-sharedata.md](05-chay-sql-tu-dau-sharedata.md).
- CSDL đã có đầy đủ 15 bảng nguồn `ShareDataSourceTable`, 25 mã cảnh báo lỗi ESH trong `SysConfigData`, 48 tham số vận hành nhóm `sharedata_config` trong `SysOpConfig`, và 93 khóa thuật ngữ (186 bản ghi song ngữ) trong `SysTerminology`.

### 1.2. Trỏ đúng DB (quan trọng) — chạy bằng VS Code debug

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
- **Lưu ý cấu hình đặc biệt:**
  - (a) Compound/cấu hình "Server 10.10.8.30" chỉ ghi đè chuỗi kết nối ConfigId 0/1 và cờ của ConfigId 1/3; cờ CodeFirst của ConfigId 0 và chuỗi ShareDataDB vẫn lấy từ `Database.json` $\rightarrow$ trước khi dùng phải kiểm `EnableInitDb`/`EnableInitTable` của Default = false. ⛔ Không hoàn tác cả `Database.json` bằng git (bản trong git không có khối `ShareDataDB`).
  - (b) Furion nạp mọi `.json` trong `TAC_WebAPI/Configuration` (`ConfigurationScanDirectories`), kể cả `Database _Sharedata.json`. Sau E1, kiểm gói nhận nằm đúng CSDL Inbound mong muốn.


### 1.3. Khởi động các thành phần theo thứ tự phụ thuộc

Khởi động các thành phần theo đúng thứ tự phụ thuộc: **WebAPI → ShareDataWorker → Frontend WebVue**:

| Thứ tự | Thành phần | Cách khởi chạy & Ghi chú |
|:---:|---|---|
| **1** | **WebAPI** (`TAC_WebAPI`) | Khởi chạy qua cấu hình debug `🌐 TAC_WebAPI (Local Docker)` (cổng `5005`). Đăng nhập tài khoản **SuperAdmin** trên giao diện để có đủ quyền lưu SQL và cấu hình. |
| **2** | **ShareDataWorker** | Khởi chạy qua cấu hình debug `📦 ShareDataWorker (Local Docker)`. Cấu hình này **ĐÃ CÓ SẴN `"Nats__Enabled": "true"`** trong `.vscode/launch.json`. Do đó, chỉ cần bật một NATS server local (cổng `4222`) là có thể thử nghiệm kịch bản D9 (gửi ngay khi có dữ liệu mới). Nếu không chạy NATS, luồng quét định kỳ vẫn vận hành bình thường. |
| **3** | **Frontend** (`WebVue`) | Khởi chạy qua cấu hình `🎨 WebVue (Dev Server)` hoặc terminal `cd TA-ITS015-WEBVUE-V1.0/src && pnpm dev`. |
| **4** | **Receiver giả** (đối tác nhận) | Bật script node giả lập máy chủ đối tác tiếp nhận dữ liệu (xem mục 0.4). |



---

## 2. Receiver giả lập & Làm mới từ điển đa ngôn ngữ

### 2.1. Receiver giả (đối tác nhận)

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



### 2.2. Làm mới từ điển đa ngôn ngữ trên giao diện

Sau khi thực thi script `sharedata-dich-thuat.sql` (thứ tự 3, ở tài liệu 05), người dùng **BẮT BUỘC** đồng bộ bộ nhớ cache thuật ngữ lên giao diện:
- **Cách 1 (UI):** Bấm vào ảnh đại diện (avatar) ở góc trên bên phải màn hình → chọn mục **"Ngôn ngữ"** → bấm nút biểu tượng **🔄 (Làm mới từ điển)**.
- **Cách 2 (API):** Gọi trực tiếp HTTP `GET http://localhost:5005/api/system/sysconfig/loadserverterm?language=vi-VN` (hoặc `language=en-US`).

---

## 3. Chuẩn bị dữ liệu qua giao diện WebVue & Chuẩn bị D0, E0

### 3.1. Dựng dữ liệu nền trên giao diện WebVue (Tài khoản SuperAdmin)

Theo chủ trương của chủ dự án, **ưu tiên thao tác trực tiếp trên giao diện WebVue**. Người dùng có thể tự cấu hình dữ liệu nghiệp vụ chính trực tiếp trên giao diện bằng tài khoản **SuperAdmin** (riêng danh mục bảng nguồn `ShareDataSourceTable` vẫn phải chạy script `03` và câu lệnh ghi chiều nhận `ShareDataPacketSql` phải viết script nạp riêng khớp với khung ngoại lệ kỹ thuật đã có).

##### Điều kiện tiên quyết & Chuỗi phụ thuộc nghiệp vụ bắt buộc:
- **Điều kiện:** Đã hoàn tất khởi tạo cấu trúc bảng ở mục 0.2.2; WebAPI và Frontend WebVue đang chạy bình thường (xem mục 1.3).
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
   - **Địa chỉ** (`address`): Nhập `http://localhost`. *(Bắt buộc).*
   - **Cổng** (`port`): Nhập `9090`. *(Bắt buộc; số nguyên từ 1 đến 65535; **KHÔNG sửa được sau khi lưu** - ô nhập bị vô hiệu hóa).*
   - **Đường dẫn API** (`endPointApiUrl`): Chiếm cả hàng, chỉ hiện khi chọn kênh `API`; nhập `/api/receive`. *(Tùy chọn; hệ thống tự động chuẩn hóa dấu gạch chéo `/` ở đầu nếu người dùng quên nhập).*
   - **Địa chỉ đầy đủ** (`fullAddress`): Dòng gợi ý ngay dưới ô Endpoint API hiển thị `http://localhost:9090/api/receive` *(đồng bộ quy tắc ghép URL với Backend `TryBuildEndpointUri`).*
   - **Loại kết nối** (`connectionType`): Chọn `API` (WebAPI / REST). *(Giao diện hiển thị 4 lựa chọn trên dropdown: `API` (1), `File` (2), `FTP` (3 — khi bật công tắc "Mã hóa kết nối FTPS" sẽ lưu `connectionType = 4` FTPS; mở đối tác loại 4 tự động hiển thị FTP + bật công tắc FTPS), `Kafka` (5)).*
   - **Cấu hình kết nối** (`connectionConfigForm`): Giao diện cung cấp form nhập nhóm trực quan gồm các khối: *Xác thực* (`sectionAuth`), *Dữ liệu* (`sectionData`); riêng kênh API có ô chọn *Nhận biết đối tác báo lỗi* (3 chế độ: *Tự động (khuyến nghị)* mặc định `"Auto"` tự phát hiện `succeeded: false`, *Chỉ theo mã HTTP* `"HttpOnly"`, và *Tùy chỉnh* khai `SuccessField`/`ErrorMessageField`/`ErrorCodeField`). Đã ẩn 2 mục *Mã hóa bảo mật* và *Tham số bổ sung*; khi đối tác đã có sẵn khóa mã hóa hoặc khóa cấu hình khác, hệ thống tự động giữ nguyên và hiển thị dòng chú thích số tham số được bảo toàn ("Cấu hình có N tham số khác được giữ nguyên"). Người dùng điền trực tiếp trên các trường của form (phương thức xác thực, định dạng JSON/CSV, thời gian chờ...).
   - **Ghi chú** (`remark`): Nhập ghi chú tùy ý (tối đa 256 ký tự).
   - Bấm nút **Xác nhận** (hoặc Lưu).
3. ⚠️ **Ràng buộc vận hành & Thao tác bấm "Kết nối" trên giao diện:**
   - **Khóa sửa đối tác:** Nút sửa cấu hình đối tác bị KHÓA khi đối tác đang ở trạng thái "Kết nối" hoặc còn bất kỳ đăng ký nào đang ở trạng thái "Hoạt động".
   - **Nút "Kết nối" trên thẻ đối tác:** Sau khi lưu thẻ đối tác, trên card đối tác `P-REST`, bấm nút **"Kết nối"** (nút chuyển sang trạng thái xanh / đã kết nối).  
     *Ý nghĩa:* Trên giao diện FE, hàm `canToggle` (`subscriptionTable.vue`) yêu cầu `partnerConnected = true` để cho phép gạt công tắc kích hoạt Đăng ký từ trạng thái `Pending` sang `Active`. *(Ở tầng Worker nền, Worker chỉ lọc `State == Active` và `IsActive = 1` của Ánh xạ, không phụ thuộc vào trạng thái phiên kết nối).*
   - *(Tùy chọn)* Đối tác các loại File (2), FTP (3), FTPS (4), Kafka (5): Chọn trực tiếp trên ô chọn Loại kết nối của giao diện khi cần kiểm thử các kịch bản C5, D7, D12, D13.

---

##### Bước 2 — Khởi tạo và cấu hình gói tin chia sẻ (`ShareDataPacket`)
1. Mở màn hình: **Chia sẻ dữ liệu** → **Cấu hình gói tin** (`views/sharedata/dataSource/index.vue`).
2. Trên thanh công cụ danh sách gói tin (cột bên trái), bấm nút **Thêm mới**:
   - Nhập **Mã** (`code`): Bắt buộc dùng đúng mã chuẩn tại [Phụ lục C](05-chay-sql-tu-dau-sharedata.md#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata) (ví dụ: `101_commonData`, `103_vdsData`...).
   - Nhập **Tên** (`name`), **Số thứ tự** (`orderNo` = 101..111).
   - Chọn **Trạng thái** (`status`): Chọn *Bật* (Status = 1) cho các gói từ 101 đến 110; riêng gói 111 chọn *Tắt* (Status = 0).
   - Nhập **Ghi chú** nếu cần → Bấm nút **Lưu**.
3. **Cơ chế tự động điền cấu hình mẫu chuẩn:** Khi nhập mã gói chuẩn (dạng 101..110 hoặc `101_xxx`), hệ thống tự động điền câu truy vấn SQL mẫu chuẩn, chế độ đồng bộ (`SyncMode`: Snapshot / Incremental) và số ngày quét lùi (`MaxLookBackDays`) nhờ `PacketCommandHandler` và `PacketDefaultTemplate.TryGet` mà không cần người dùng phải gõ tay hay dán SQL. *(Giao diện đã bỏ hoàn toàn khái niệm "Phạm vi" hay `partnerScope`).*
4. Cấu hình SQL trích xuất và tham số đồng bộ:
   - Nhấp chọn gói tin vừa tạo ở danh sách bên trái.
   - Ở khung bên phải, chuyển sang tab **Cấu hình SQL** (`tabSqlCenter`).
   - Khung soạn thảo SQL (Monaco Editor) đã hiển thị câu truy vấn trích xuất chuẩn. Nếu muốn khôi phục lại câu SQL chuẩn từ hệ thống, bấm nút **Khôi phục SQL mẫu** (`vqbResetToTemplateTitle`). *(Lưu ý: Bấm nút này chỉ nạp nội dung lên khung soạn thảo, chưa ghi đè xuống CSDL cho tới khi bấm nút Lưu).*
   - **Điều kiện lấy dữ liệu (`SyncModePanel`):**
     + Kiểm tra **Chế độ đồng bộ** (`syncMode`): Chọn *Toàn bộ* (Snapshot) cho các gói 101/102/105/108/110; hoặc *Nối đuôi* (Incremental) cho các gói 103/104/106/107/109 theo đúng [Phụ lục C](05-chay-sql-tu-dau-sharedata.md#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata).
     + Nếu là gói *Nối đuôi*: Câu lệnh SQL tự động có điều kiện con trỏ 2 khóa (`@lastTime`, `@lastKey`) và sắp xếp theo mốc thời gian tăng dần (`ASC`). Ô *Quét lùi tối đa* tạm thời được ẩn trên giao diện chờ review nghiệp vụ (Worker vẫn áp dụng nếu CSDL đã lưu giá trị).
   - **Trường trích xuất (sau AS):** Hệ thống tự động bóc tách các bí danh sau `AS` trong câu SELECT:
     + Chip màu xanh (✓): Bí danh đã được khai báo thành trường gói tin.
     + Chip viền nét đứt: Bí danh chưa khai trường.
     + Chip gạch ngang đỏ nhạt: Trường gói tin đã khai nhưng câu SQL không trả về.
     + Dòng trạng thái dưới chip: Tóm tắt số trường đã khai · chưa khai kèm nút `[Khai thêm M trường]` · số trường SQL không trả về.
   - **Biến hệ thống trong câu truy vấn:** Hiển thị trực quan nguồn giá trị của từng biến do Worker tự điền (`@lastTime ← __lastTime = <biểu_thức>`, `@lastKey ← __rowid = <biểu_thức>`, `@pageSize ← cấu hình Worker`, `@fromDate ← hôm nay − N ngày`, `@toDate ← thời điểm chạy`). Muốn đổi nguồn mốc con trỏ chỉ cần sửa biểu thức sau `AS __lastTime` / `AS __rowid` trong câu lệnh SQL; khi chuyển đổi chế độ đồng bộ hệ thống tự gắn/gỡ phần nối đuôi mà không bật thông báo thừa.
   - Bấm nút **Lưu** (`save`) để lưu cấu hình SQL và các tham số đồng bộ xuống CSDL.
5. 💡 **Khuyến nghị gói tin tối thiểu để nghiệm thu:** Để thực hiện nhanh các kịch bản kiểm thử, người dùng tối thiểu cần khởi tạo 2 gói tin:
   - Gói **`101_commonData`** (Snapshot, phục vụ kịch bản D1, D5, E0, E1).
   - Gói **`103_vdsData`** (Incremental, phục vụ kịch bản D3 nối đuôi).  
   Khuyến nghị khởi tạo đầy đủ cả 11 gói tin theo [Phụ lục C](05-chay-sql-tu-dau-sharedata.md#phụ-lục-c-danh-mục-chuẩn-11-gói-tin-sharedata) (10 gói Bật, gói 111 Tắt).

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
     + 📌 *Chi tiết khuôn JSON mẫu và cây ánh xạ:* Vui lòng xem chi tiết tại mục **[mục 3.2. Chuẩn bị riêng cho chiều gửi D0 và chiều nhận E0](#32-chuẩn-bị-riêng-cho-chiều-gửi-d0-và-chiều-nhận-e0)** (Mục **D0** cho Ánh xạ chiều gửi Outbound; Mục **E0** cho Ánh xạ chiều nhận Inbound).
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
- Danh mục bảng nguồn (`ShareDataSourceTable`) là quy tắc nghiệp vụ riêng của phân hệ ShareData kết hợp lưu trực tiếp trạng thái mốc phiên bản (`LastVersion`, `LastChangeTime`, `LastIssue`), hai công tắc độc lập `IsTracked` và `IsQueryable`, cùng `DefaultAlias`, nên không bố trí màn hình cấu hình riêng trên giao diện (ngoại lệ chủ đích theo quy chuẩn kiến trúc dữ liệu nền).
- Thao tác khởi tạo và kiểm tra danh mục 15 bảng nguồn ([Phụ lục B](05-chay-sql-tu-dau-sharedata.md#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable)) được thực hiện tại **[Khung ngoại lệ duy nhất cuối Cách 1](#khung-ngoai-le-duy-nhat)** bên dưới.

---

##### Bước 6 — Khởi tạo danh mục 25 mã cảnh báo lỗi ESH
1. Mở màn hình: **Hệ thống** → **Loại cấu hình** (`views/cfgSystem/sysConfigType/index.vue`).
2. Ở cột bên trái (*Loại cấu hình*): Tìm loại có Mã `sharedata_error_code`. Nếu chưa có, bấm **Thêm mới**: Tên: *Mã cảnh báo lỗi ShareData*, Mã: `sharedata_error_code`, Thứ tự: `1900`, Trạng thái: *Bật* → Bấm **Lưu**.
3. Nhấp chọn dòng `sharedata_error_code`: Bảng bên phải (*Dữ liệu cấu hình*) sẽ tải danh sách mã lỗi của loại này.
4. Bấm nút **Thêm mới** để nạp từng mã cảnh báo lỗi theo danh sách tại [Phụ lục A](05-chay-sql-tu-dau-sharedata.md#phụ-lục-a-danh-mục-25-mã-cảnh-báo-lỗi-esh-xxxx):
   - ⚠️ **LƯU Ý QUAN TRỌNG VỀ GIỚI HẠN ĐỘ DÀI:** Ô **Tên** (`name`) trên giao diện bị giới hạn tối đa **64 ký tự**. Trong 25 mã ESH, có **9 mã có tên gốc dài hơn 64 ký tự**; khi nhập tay trên giao diện, **BẮT BUỘC sử dụng tên rút gọn (≤ 64 ký tự)** được cung cấp riêng tại [Phụ lục A](05-chay-sql-tu-dau-sharedata.md#phụ-lục-a-danh-mục-25-mã-cảnh-báo-lỗi-esh-xxxx).
   - Nhập **Mã** (`code`): Mã lỗi chuẩn ESH (ví dụ: `ESH-1201`).
   - Nhập **Giá trị** (`value`): Phân loại (`outbound`, `inbound`, hoặc `tracking`).
   - Chọn **Loại thẻ** (`tagType`): `warning` hoặc `danger`.
   - Nhập **Thứ tự** (`orderNo`): Số thứ tự mã lỗi (ví dụ: 1201, 1301...).

---

##### Bước 7 — Khởi tạo bản dịch giao diện & thuật ngữ + Đồng bộ 🔄
1. Mở màn hình: **Hệ thống** → **Thuật ngữ** (`views/cfgSystem/sysTerminology/index.vue`).
2. Bấm nút **Thêm mới** để nạp từng bản dịch theo danh sách tại [Phụ lục E: Danh mục thuật ngữ ShareData (SysTerminology)](05-chay-sql-tu-dau-sharedata.md#phụ-lục-e-danh-mục-thuật-ngữ-sharedata-systerminology):
   - **Tên tiền tố** (`name`): Ví dụ `lz.validation` hoặc `lz.exception`.
   - **Mã** (`code`): Ví dụ `sharedata.lookbackDaysRange` hoặc `sharedata.connectConfigSyntaxError`.
   - **Giá trị** (`value`): Nội dung chuỗi dịch tiếng Việt hoặc tiếng Anh.
   - **Ngôn ngữ** (`lang`): Chọn `vi-VN` hoặc `en-US`.
   - *Quy tắc:* Mỗi bản ghi trên giao diện đại diện cho một cặp khóa - ngôn ngữ (nhập bản dịch `vi-VN` trước, sau đó nhập bản dịch `en-US`). Giao diện không hỗ trợ nhập hàng loạt.
3. **Đồng bộ bộ nhớ cache thuật ngữ lên giao diện:** Sau khi nạp xong, người dùng thực hiện một trong hai cách:
   - *Cách 1 (UI):* Bấm vào ảnh đại diện (avatar) ở góc trên bên phải màn hình → chọn mục **"Ngôn ngữ"** → bấm nút biểu tượng **🔄 (Làm mới từ điển)**; hoặc bấm nút 🔄 trên thanh công cụ màn hình Thuật ngữ.
   - *Cách 2 (API):* Gọi trực tiếp HTTP `GET http://localhost:5005/api/system/sysconfig/loadserverterm?language=vi-VN` (và `language=en-US`).
4. 💡 **Đường tắt thực tế:** Do số lượng bản dịch gồm 93 khóa $\times$ 2 ngôn ngữ = 186 bản ghi, việc nhập tay từng dòng trên giao diện rất mất thời gian. Khuyến nghị người dùng nên sử dụng **CÁCH 2 (đường tắt)** chạy script `sharedata-dich-thuat.sql` (thứ tự 3) để nạp tự động an toàn trong vài giây.

---

##### Bước 8 (Tùy chọn) — Cấu hình tham số vận hành ShareData (`SysOpConfig` nhóm `sharedata_config`)
1. Mở màn hình: **Hệ thống** → **Cấu hình hoạt động** (`views/cfgSystem/sysOpConfig/index.vue`).
2. Tại ô lọc *Nhóm cấu hình*: Chọn nhóm `sharedata_config` (Cấu hình vận hành & giới hạn Chia sẻ dữ liệu).
3. Bảng bên phải hiển thị danh sách 48 tham số vận hành của phân hệ ShareData (gồm 19 tham số Worker, 23 tham số FE và 6 tham số WebAPI; chi tiết xem [Phụ lục F](05-chay-sql-tu-dau-sharedata.md#phụ-lục-f-danh-mục-48-tham-số-vận-hành-sharedata-sysopconfig-nhóm-sharedata_config)).
4. Người dùng có thể chỉnh sửa trực tiếp giá trị tham số trên giao diện mà không cần build lại mã nguồn hay khởi động lại ứng dụng. Phía WebAPI (`ShareDataConfigService` đăng ký `IScoped`) truy vấn trực tiếp `SysOpConfig` theo từng HTTP request qua `IMessageBus` nên nhận giá trị mới ngay lập tức không có độ trễ; phía Worker (`DataWorkerConfigService`) quét nạp lại định kỳ theo tham số `reload_interval_sec` trong `SysOpConfig` nhóm `sharedata_config` (mặc định 30 giây, khoảng 5–3600, sửa trên màn Cấu hình hoạt động; không còn chế độ 0). Nếu không cấu hình hoặc để trống, hệ thống luôn tự động dùng giá trị dự phòng an toàn và khoảng an toàn tuyệt đối khai báo tại `ShareDataConst` (`Module.ShareData.Core/Constants`).

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
> 👉 *Cách thực hiện:* Chạy Khối 4 của script [`sharedata-du-lieu-gia-lap.sql`](../sql/sharedata-du-lieu-gia-lap.sql) hoặc chạy các câu lệnh SQL mẫu tại [Phụ lục D](05-chay-sql-tu-dau-sharedata.md#phụ-lục-d-mẫu-cấu-hình-chiều-nhận-e0-bảng-đích-câu-merge-và-targetshapejson).
>
> **2. Danh mục 15 bảng nguồn (`ShareDataSourceTable` trên CSDL `[DEV_ITS10]` — Bước 5):**
> - **Lý do ngoại lệ chủ đích:** Bảng `ShareDataSourceTable` lưu đồng thời danh sách bảng nguồn của phân hệ ShareData và trạng thái con trỏ phiên bản (`LastVersion`, `LastChangeTime`, `LastIssue`) do Worker cập nhật nguyên tử theo từng chu kỳ quét, cùng 2 công tắc độc lập `IsTracked` và `IsQueryable`, nên không đặt màn hình cấu hình trên giao diện.
> 👉 *Cách thực hiện:*
> - **Cách thuận tiện:** Chạy Mục 3.4 của script [`sharedata-seed-cau-hinh.sql`](../sql/sharedata-seed-cau-hinh.sql) (thứ tự 2, idempotent — tự động chèn đủ 15 bảng chuẩn theo [Phụ lục B](05-chay-sql-tu-dau-sharedata.md#phụ-lục-b-danh-mục-15-bảng-nguồn-sharedata-sharedatasourcetable)).
> - **Cách thủ công (chèn/cập nhật từng dòng):**
>   ```sql
>   USE [DEV_ITS10];
>   -- Ví dụ bật/tắt trạng thái theo dõi Change Tracking của 1 bảng:
>   UPDATE dbo.ShareDataSourceTable
>   SET IsTracked = 0, UpdateTime = GETDATE()
>   WHERE TableName = N'TmsEquipment' AND IsDelete IS NULL;
>
>   -- Kiểm tra danh mục 15 bảng nguồn hiện có:
>   SELECT TableName, Name, DefaultAlias, IsTracked, IsQueryable, LastVersion, LastChangeTime, LastIssue
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
| **B8** | Kiểm tra 48 tham số vận hành nhóm `sharedata_config` (màn *Cấu hình hoạt động*) | Đủ 48 tham số hiển thị, có thể chỉnh sửa giá trị và lọc theo nhóm `sharedata_config` bình thường | ☐ ☐ |



### 3.2. Chuẩn bị riêng cho chiều gửi D0 và chiều nhận E0

#### D0: Chuẩn bị chiều gửi (Ánh xạ chiều gửi cho gói 101 và 103)
Trước khi thực hiện các bài test nhóm D, cần đảm bảo đối tác `P-REST`, đăng ký gửi và ánh xạ chiều gửi đã được chuẩn bị đầy đủ (nếu đã tạo theo Bước 1, 3, 4 tại [mục 3.1](#31-dựng-dữ-liệu-nền-trên-giao-diện-webvue-tài-khoản-superadmin) thì chỉ cần đối soát lại các tham số dưới đây):
1. **Tạo đăng ký gửi (Outbound):** Vui lòng xem và thực hiện theo **Bước 4 mục 3.1** (tạo đăng ký chiều Outbound cho đối tác `P-REST` với gói `101_commonData` và gói `103_vdsData`, chu kỳ gửi 30 giây).
2. **Tạo ánh xạ (Phễu lọc) chiều gửi:** *(Nếu chưa tạo ở Bước 3 mục 3.1)* Vào màn hình *Cấu hình ánh xạ* → Bấm nút **Thêm mới**:
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
   - Đã được nạp sẵn tự động khi chạy **Khối 4 của script `sharedata-du-lieu-gia-lap.sql`** (tạo gói `E2E_INBOUND_TEST` ID `1500000000900`, tạo bảng `InboundTrafficFlow` trên `DEV_ITS10_Inbound` và chèn câu MERGE vào `ShareDataPacketSql` ID `1500000000903` cho gói `E2E_INBOUND_TEST`).
   - Nếu chưa chạy script 05, có thể thực thi câu lệnh SQL tại [Phụ lục D](05-chay-sql-tu-dau-sharedata.md#phụ-lục-d-mẫu-cấu-hình-chiều-nhận-e0-bảng-đích-câu-merge-và-targetshapejson).
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

## 4. Nghiệm thu chi tiết từng phân hệ từ A đến J

## 🅰️ A. Chế độ và trạng thái gói tin (màn hình *Cấu hình gói tin / SQL Center*, thư mục `dataSource`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| A1 | Mở danh sách gói tin | Tag chế độ mỗi gói **khớp cột `SyncMode` trong DB** (tài liệu 05 mục 3.1): 101, 102, 105, 108, 110 là *gửi toàn bộ* (Snapshot); 103, 104, 106, 107, 109 là *nối đuôi* (Incremental) | ☐ Đạt ☐ Không đạt |
| A2 | Mở gói **105** (chưa chỉnh gì) | Chế độ mặc định **Snapshot / gửi toàn bộ** (không phải nối đuôi) | ☐ ☐ |
| A3 | Thử đổi gói 105 sang *nối đuôi* (Incremental / 2) trong khi vẫn giữ nguyên câu SQL dạng Snapshot (chưa có cấu trúc nối đuôi) → bấm Lưu (bị từ chối). Tại thẻ *Gửi nối đuôi*, chọn 2 cột (mốc thời gian `td.UpdateTime`, mã dòng `td.ID`), bấm **"Áp vào câu SQL"** → kiểm tra 4 tiêu chí checklist (Có cột mốc, Chỉ lấy dữ liệu mới, Sắp xếp theo thời gian, Giới hạn số dòng mỗi lượt) đều chuyển sang màu xanh ✓ → đặt số ngày quét lùi = 7 → bấm Lưu → tải lại trang và mở lại gói 105 | Khi câu SQL chưa đúng cấu trúc nối đuôi: **Bị từ chối lưu**, hệ thống hiển thị thông báo lỗi kiểm tra cấu trúc SQL nối đuôi (thiếu `TOP (@pageSize)`, thiếu `ORDER BY ... ASC`, hoặc thiếu `__lastTime`/`__rowid`). Sau khi bấm "Áp vào câu SQL" đạt đủ 4 mục và Lưu: **Lưu thành công**, mở lại giữ nguyên chế độ nối đuôi, cột mốc `td.UpdateTime`, quét lùi 7 ngày; tag trên danh sách đổi sang Nối đuôi | ☐ ☐ |
| A4 | Xóa trắng ô `MaxLookBackDays` → bấm Lưu → tải lại trang và mở lại gói 105 | Ô `MaxLookBackDays` hiển thị **rỗng** (giá trị `null` trong CSDL, không tự động quay về 7) | ☐ ☐ |
| A5 | Đổi gói 105 về lại chế độ *gửi toàn bộ* (Snapshot), khôi phục câu SQL mẫu chuẩn qua nút "Mẫu mặc định" → bấm Lưu | **Lưu thành công**; hệ thống không kiểm tra cấu trúc nối đuôi đối với gói Snapshot | ☐ ☐ |
| A6 | Xem gói **111** (và 110 nếu chưa bật) | Hiện **"Đã tắt"**; không có nút chạy/gửi | ☐ ☐ |
| A7 | Tạo gói mới: chọn "Loại gói (thứ tự)" 101–111 (mặc định 101) → Lưu gói | Gói tạo xong có ngay SQL (`TemplateSql` = `ExtractSql` sao chép từ gói nguồn cùng loại trong CSDL); khi bấm Sửa gói thì ô "Loại gói" bị khóa chỉ đọc (kèm gợi ý "Gói mới nhận SQL mẫu theo loại gói; sau khi tạo không đổi loại gói được") | ☐ ☐ |
| A8 | Sửa câu SQL trích xuất của gói rồi bấm [Lưu] → bấm menu "Khôi phục SQL mẫu" | Câu lệnh đang chạy `ExtractSql` bị ghi đè khi Lưu; khi bấm "Khôi phục SQL mẫu" ô soạn thảo khôi phục lại câu SQL mẫu ban đầu lúc tạo gói (đọc từ `TemplateSql`, CSDL không đổi) | ☐ ☐ |
| A9 | Mở chế độ Trực quan (Visual Query Builder) với các gói có JOIN (101, 102, 107...) và gói 110 ("Lấy 1 dòng mới nhất" qua `OUTER APPLY (SELECT TOP 1...)`) | Cả 11 gói (101–111) đều mở được ở chế độ Trực quan; danh sách nối bảng nhận diện chính xác loại nối (`INNER JOIN`, `LEFT JOIN`, `Lấy 1 dòng mới nhất`) và điều kiện `ON` | ☐ ☐ |

---

## 🅱️ B. SQL trích xuất và an toàn

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| B1 | Gói 101 → *Xem trước (preview)* | Có cột và dữ liệu thật, trả trong vài giây, không treo | ☐ Đạt ☐ Không đạt |
| B2 | SQL `SELECT … ORDER BY …` **không có TOP** → preview | Chạy được, không lỗi cột trùng tên | ☐ ☐ |
| B3 | Gói nối đuôi (103): sửa câu SQL bỏ cột `__lastTime` hoặc `__rowid` khỏi SELECT (hoặc thẻ kiểm tra báo đỏ ✗ Có cột mốc) → Lưu | **Bị từ chối**, thông báo nêu rõ thiếu cột con trỏ bắt buộc | ☐ ☐ |
| B4 | Gói nối đuôi: sửa câu SQL đổi `ORDER BY … DESC` (thẻ kiểm tra báo đỏ ✗ Sắp xếp theo thời gian) → Lưu | **Bị từ chối** (sắp giảm dần không hợp lệ cho con trỏ đồng bộ) | ☐ ☐ |
| B5 | Lưu lần lượt: `DELETE FROM TmsZone`; `SELECT 1; SELECT 2`; `EXEC sp_who`; `SELECT * FROM SysUser`; `SELECT * FROM ShareDataPartner`; `SELECT * FROM OtherDb.dbo.TmsZone` | **Mỗi câu đều bị từ chối** với thông báo bảo mật đúng ngôn ngữ đang chọn | ☐ ☐ |
| B6 | Nút **"Mẫu mặc định"** (`vqbDefaultTemplate`) | Khôi phục lại câu truy vấn SQL mẫu chuẩn theo mã gói từ hệ thống (Catalog/Seed), chưa ghi đè xuống CSDL cho tới khi người dùng bấm nút Lưu | ☐ ☐ |
| B7 | *Đã bỏ (SQL một tầng trên ShareDataPacket, không còn cấu hình SQL riêng theo đối tác)* | Đã chuẩn hóa kiến trúc: câu SQL đặt duy nhất tại `ShareDataPacket.ExtractSql`, không còn phân tầng SQL riêng cho đối tác | Bỏ qua (đã chuẩn hóa) |
| B8 | (Nếu có tài khoản không phải SuperAdmin) thử lưu SQL | **Bị chặn quyền** (chức năng lưu SQL yêu cầu quyền SuperAdmin) | ☐ ☐ |
| B9 | Nhập câu SQL đọc bảng ngoài danh mục `ShareDataSourceTable` (ví dụ `SELECT TOP 10 * FROM dbo.BangLaNgoaiDanhMuc;`) → bấm Dò cột, Xem mẫu hoặc Lưu | **Bị từ chối ngay lập tức**, thông báo hiển thị đúng ngôn ngữ theo khóa `SqlTableNotAllowed`: *"Bảng BangLaNgoaiDanhMuc chưa có trong danh mục bảng nguồn được phép chia sẻ. Thêm bảng vào ShareDataSourceTable (IsQueryable = 1) rồi thử lại."* (Yêu cầu đã chạy script 04 và bấm 🔄 làm mới từ điển) | ☐ Đạt ☐ Không đạt |
| B10 | Đặt `IsQueryable = 0` cho bảng `TmsWeather` trong CSDL: `UPDATE dbo.ShareDataSourceTable SET IsQueryable = 0, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;` → mở màn hình *Cấu hình gói tin* tab *Cấu hình SQL* | Bảng `TmsWeather` **biến mất** khỏi danh sách dropdown chọn Bảng nguồn của bộ dựng Visual Query Builder; nếu dán câu SQL chứa bảng `TmsWeather` thì bị từ chối khi Lưu/Dò cột (WebAPI chặn); tuy nhiên luồng Worker gửi gói 104 đang chạy nền **vẫn gửi dữ liệu bình thường** (vì Worker chỉ kiểm tra `IsTracked = 1`, hoàn toàn không kiểm tra `IsQueryable`) | ☐ ☐ |
| B11 | Bật lại `IsQueryable = 1` cho `TmsWeather`: `UPDATE dbo.ShareDataSourceTable SET IsQueryable = 1, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;` → tải lại trang *Cấu hình gói tin* | Bảng `TmsWeather` **xuất hiện trở lại ngay lập tức** trong dropdown chọn Bảng nguồn; các thao tác dò cột, xem trước và lưu câu SQL hoạt động bình thường | ☐ ☐ |
| B12 | Dùng dòng F5 `BangKhongTonTai_Test` với `IsQueryable = 1`, chọn bảng đó trên bộ dựng truy vấn | Danh sách cột rỗng, không hiện cột đoán; log WebAPI có cảnh báo *"Bảng nguồn BangKhongTonTai_Test có trong ShareDataSourceTable nhưng không đọc được cột trên CSDL, kiểm tra lại cấu hình bảng nguồn."* (`PacketSqlService.GetTableFields`) | ☐ ☐ |

---

## 🅲 C. Đối tác và kết nối (màn *Đối tác & Đăng ký chia sẻ*, thư mục `sharing`)

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| C1 | Mở đối tác **P-REST** (đã tạo ở Bước 1 mục 3.1, hoặc tạo mới nếu chưa có): Address `http://localhost`, Port `9090`, EndPoint `/api/receive`, kiểu `API`. Chỉnh sửa tham số (ví dụ đổi Tên hoặc EndPoint) → Lưu / Xác nhận | Lưu thành công, mở lại giữ nguyên các giá trị đã cập nhật. Cổng (Port) và Mã (Code) hiển thị vô hiệu hóa (disabled), không cho phép sửa đổi | ☐ Đạt ☐ Không đạt |
| C2 | Mở đối tác có cấu hình sẵn (hoặc chỉnh sửa trên form các trường `Format=Json`, `TimeoutSeconds=30`...) → Xác nhận → mở lại | Các trường trên form và các tham số khác đã lưu **giữ nguyên giá trị**; hiển thị dòng chú thích số tham số được bảo toàn (nếu có khóa lạ/mã hóa); giá trị bí mật (Token/ApiKey/Password) **bị che** khi hiển thị | ☐ ☐ |
| C3 | Kiểm tra kiểm thực các trường trên form: Timeout ngoài khoảng cho phép (API: 1..300s, FTP: 1..120s, Kafka: 500..60000ms), FileNamePattern của File/FTP thiếu `{serial}` hoặc `{timestamp}`, AuthType yêu cầu Token/ApiKey/Username/Password tương ứng nhưng để trống | **Hiển thị cảnh báo trực quan** dưới từng ô nhập theo ngôn ngữ đang chọn (`vi-vn` / `en-us`), nút chuyển secret che/mở mật khẩu hoạt động trơn tru; không cho phép lưu cấu hình không hợp lệ | ☐ ☐ |
| C4 | Mở đối tác khác rồi quay lại P-REST | Cấu hình **không lẫn** sang nhau (không mang token qua đối tác khác) | ☐ ☐ |
| C5 | Tạo đối tác chọn kênh trong danh mục hợp lệ: File (2), FTP (3 — hoặc bật công tắc "Mã hóa kết nối FTPS" để lưu loại 4 FTPS), Kafka (5) → Lưu được; mở lại giữ nguyên kênh đã chọn (đối tác loại 4 tự động hiển thị FTP + bật công tắc FTPS) | Được chấp nhận và lưu đúng mã số enum (1..5) xuống CSDL, gửi qua đúng kênh tương ứng (FTPS bật UseSsl, File đi kênh ghi đĩa cục bộ/mạng). Form chọn kiểu dạng danh sách thả xuống (dropdown), ngăn chặn hoàn toàn việc nhập giá trị lạ | ☐ ☐ |
| C6 | Kiểm tra các trường hợp cấu hình kết nối đối tác sai: form chặn trực tiếp trên UI (JSON lỗi cú pháp thì hiện cảnh báo `rawInvalidJson`, Timeout không hợp lệ, enum không chuẩn...) hoặc được kiểm chứng tự động 100% qua bộ test backend `ConnectConfigValidatorTests` (kiểm tra qua phương thức `ConnectConfigValidator.Check(config)`). Khi có chuỗi JSON lỗi trước đó, hệ thống giữ nguyên chuỗi khi lưu trừ khi bấm nút "Xóa cấu hình lỗi" | Từng trường hợp sai bị từ chối với thông điệp đa ngôn ngữ chuẩn từ `BaseMsg.ShareData.Validation` hoặc cảnh báo UI tương ứng; cấu hình hợp lệ lưu bình thường | ☐ ☐ |
| C7 | Nghiệm thu kênh **Kafka** (`ConnectionType = Kafka` hoặc 5, hạ tầng Docker local `tac_webapi_kafka`): Tạo đối tác `P-KAFKA`, chọn kiểu `Kafka` (5), cấu hình `BootstrapServers = 127.0.0.1:19092`, `Topic = test.sharedata.traffic` (khóa chuẩn từ `ConnectKeys`). Tạo Đăng ký gửi gói 101 sang P-KAFKA | Lưu đối tác thành công. Khi Worker chạy, bản tin đẩy vào broker; kiểm tra message trong container bằng lệnh `docker exec -it tac_webapi_kafka kafka-console-consumer --bootstrap-server 127.0.0.1:19092 --topic test.sharedata.traffic --from-beginning` thấy bản tin JSON UTF-8 đầy đủ 4 header định danh | ☐ ☐ |
| C8 | Nghiệm thu kênh **FTP** (`ConnectionType = Ftp` hoặc 3, hạ tầng Docker local `tac_webapi_ftp`): Tạo đối tác `P-FTP`, chọn kiểu `Ftp` (3), Address `127.0.0.1`, Port `2121`, cấu hình thêm `Username = tactest`, `Password = Tac@@1234`, `RemotePath = /sharedata-test`, `FileName = packet_{packet}_{timestamp}.json` (hoặc `FileNamePattern`). Tạo Đăng ký gửi gói 101 sang P-FTP | Lưu đối tác thành công. Khi Worker chạy, tệp tải lên FTP server thành công; kiểm tra tệp trong container bằng lệnh `docker exec -it tac_webapi_ftp ls -la /ftp/tactest/sharedata-test` thấy tệp xuất hiện đúng tên, nội dung JSON/CSV khớp chính xác từng byte với tệp bằng chứng tại `sharedata/send` | ☐ ☐ |

---

## 🅳 D. Gửi (chiều ra)

**Điều kiện:** Đã chuẩn bị ánh xạ chiều gửi theo **D0**; receiver chạy ở mục 2.1; Worker đang chạy; đối tác P-REST đã tạo. Tạo **đăng ký chiều gửi** (Outbound) cho gói cần thử với chu kỳ ngắn (~30 giây).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| D1 | Đăng ký gói **101** (Snapshot) | Receiver nhận request; body JSON đủ trường theo mapping; **số phần tử = số dòng nguồn** (không bị cắt cứng 100); header có `PartnerCode`, `PacketCode`, `SerialNbr` | ☐ Đạt ☐ Không đạt |
| D2 | Màn *Lịch sử chia sẻ* (`history`) | Có bản ghi **cha** và các bản ghi **con** theo bước/trang; mô tả **không nối lặp** | ☐ ☐ |
| D3 | Đăng ký gói **103** (nối đuôi), chờ 1 chu kỳ; sau đó chạy `UPDATE dbo.TmsTrafficData SET UpdateTime = GETDATE() WHERE ID IN (<3 ID bất kỳ>)` trên DB local; chờ chu kỳ kế | Chu kỳ sau gửi **đúng 3 dòng** vừa đổi; chu kỳ tiếp theo không có dòng mới → ghi *không có dữ liệu mới*, **không gửi lặp**; `ShareDataLastSend` tiến, không lùi | ☐ ☐ |
| D4 | Tạo file `FAIL` cạnh receiver → chờ chu kỳ gửi → xóa file `FAIL` → chờ chu kỳ | Khi lỗi: cảnh báo ở màn *Nhật ký lỗi* (`errorLog`), `ShareDataLastSend` **không tiến**; khi hết lỗi: gửi bù đủ dữ liệu bị lỡ, không sót | ☐ ☐ |
| D5 | Tìm tệp bằng chứng trong `NasStorage:BasePath` (mặc định `sharedata/send`, tương đối với thư mục chạy Worker) theo `Out/<Đối tác>/yyyy/MM/dd/<Gói>/…`; so sánh với `body` ở `received-<n>.json` | Tệp bằng chứng **LUÔN LUÔN được lưu** trong `sharedata/send` và **đúng từng byte** với bản đã gửi sang receiver | ☐ ☐ |
| D6 | Đặt khóa cũ `ArchiveEnabled=false` trong mục Tham số bổ sung của cấu hình kết nối đối tác, chờ 1 lần gửi | Khóa cũ bị bỏ qua, hệ thống **vẫn sinh tệp bằng chứng bình thường**; log cha không ghi chú không lưu | ☐ ☐ |
| D7 | Đối tác kiểu **File** (`ConnectionType = 2`): `DirectoryPath` nằm trong thư mục gốc `BasePath`, `FileNamePattern` có `{serial}`/`{timestamp}` | Sinh tệp đúng tên, mỗi trang một tệp, không ghi đè | ☐ ☐ |
| D8 | `DirectoryPath` ngoài thư mục cho phép; `FileNamePattern` có `../` hoặc `/` | **Bị từ chối**, không ghi tệp nào ngoài thư mục cho phép | ☐ ☐ |
| D9 | *(Cần NATS local cổng 4222)* Đăng ký gói **101** với *gửi ngay khi có dữ liệu mới* bật, `DebounceSec = 10`; chạy `UPDATE dbo.TmsZoneStatus SET UpdateTime = GETDATE() WHERE ID = '<ID>'` | Worker local (đã có sẵn `Nats__Enabled = true`) nhận sự kiện và gửi trong khoảng `DebounceSec`, **không đợi hết chu kỳ quét định kỳ** | ☐ ☐ |
| D10 | Đăng ký chế độ *Single* (một lần): gửi thành công; làm lại với receiver lỗi (`FAIL`) | Thành công → chuyển trạng thái *hết hạn* (Expired); lỗi → **giữ hoạt động** (Active) và thử lại ở chu kỳ sau | ☐ ☐ |
| D11 | *(Mã hóa chỉ viết sẵn `EncryptManager`, chưa nối luồng gửi/nhận theo quyết định 16/10/2026)* | Chưa nối vào luồng nghiệp vụ: **BỎ QUA**, chỉ nghiệm thu khi áp dụng mã hóa | Bỏ qua (chưa nối) |
| D12 | *(Tùy chọn — cần hạ tầng Kafka broker local)* Đối tác kiểu **Kafka** (`ConnectionType = 5`), `BootstrapServers` + `Topic` trong cấu hình | Message là byte[] UTF-8 JSON, có 4 header định danh; gửi lỗi (tắt broker) thì cảnh báo và mốc không tiến | ☐ Tùy chọn |
| D13 | *(Tùy chọn — cần hạ tầng FTP server local)* Đối tác kiểu **Ftp** (`ConnectionType = 3`) hoặc **FTPS** (`ConnectionType = 4`), `RemotePath` trong cấu hình | Tệp tải lên đúng nội dung, tên theo mẫu; sai mật khẩu thì lỗi rõ ràng, mốc không tiến | ☐ Tùy chọn |
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

> ℹ️ **Chu kỳ nạp lại danh sách bảng:** Nạp định kỳ theo tham số `reload_interval_sec` trong `SysOpConfig` nhóm `sharedata_config` (mặc định 30 giây, khoảng 5–3600, sửa trên màn Cấu hình hoạt động; không còn chế độ 0). Mọi thao tác kiểm tra, bật/tắt, thêm mới và xóa mềm dưới đây thực hiện trực tiếp bằng SQL trên bảng `dbo.ShareDataSourceTable` (CSDL `[DEV_ITS10]`).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| F1 | Chạy `SELECT TableName, Name, PacketCodes, OrderNo, IsTracked, IsQueryable, LastVersion, LastChangeTime, LastIssue FROM dbo.ShareDataSourceTable WHERE IsDelete IS NULL ORDER BY OrderNo;` | Thấy đúng 15 dòng danh mục bảng nguồn; 14 bảng theo dõi `IsTracked = 1` (Bật), riêng bảng `TmsEquipment` ở trạng thái **tắt theo dõi** (`IsTracked = 0`); cả 15 bảng đều có `IsQueryable = 1` (cho phép dùng trong bộ dựng SQL); các bảng đã bật Change Tracking có `LastVersion >= 0` và `LastIssue IS NULL` | ☐ Đạt ☐ Không đạt |
| F2 | Sửa 1 thiết bị trong bảng `TmsEquipment` (khi `IsTracked = 0`) | **Không** kích hoạt gửi các gói 102/103/108 (tránh trigger quét thừa khi chỉ bảo trì thiết bị) | ☐ ☐ |
| F3 | Bật theo dõi `TmsEquipment` (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 1, UpdateTime = GETDATE() WHERE TableName = 'TmsEquipment' AND IsDelete IS NULL;`), chờ ≤ chu kỳ `reload_interval_sec` (mặc định 30 giây), sửa thiết bị, rồi **tắt lại** (`IsTracked = 0`) | Log Worker ghi *đã cập nhật cấu hình bảng giám sát (Thêm mới: 1)*; sửa thiết bị lúc bật thì **có** kích hoạt và tiến `LastVersion`, `LastChangeTime`; sau khi tắt lại thì dừng trigger | ☐ ☐ |
| F4 | Tắt theo dõi 1 bảng (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 0, UpdateTime = GETDATE() WHERE TableName = 'TmsWeather' AND IsDelete IS NULL;`) | Bảng `TmsWeather` **ngừng** được theo dõi sau ≤ chu kỳ `reload_interval_sec` (mặc định 30 giây) (sau khi kiểm xong, khôi phục `IsTracked = 1`) | ☐ ☐ |
| F5 | Thêm 1 dòng với **tên bảng không tồn tại trong CSDL** (ví dụ `TableName = 'BangKhongTonTai_Test'`, `PacketCodes = '101_commonData'`, `IsTracked = 1`, `IsQueryable = 1`) | Worker ghi nhận cảnh báo `ESH-1603` và cập nhật lý do vào cột `LastIssue` của dòng đó; các bảng hợp lệ khác **vẫn tiếp tục được theo dõi bình thường** (xóa dòng test sau khi kiểm) | ☐ ☐ |
| F6 | **Tắt theo dõi tất cả** 15 dòng (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 0, UpdateTime = GETDATE() WHERE IsDelete IS NULL;`) | Worker ghi cảnh báo `ESH-1603` **đúng 1 lần**, không còn kích hoạt gửi tức thì; luồng quét định kỳ (D1/D3) **vẫn gửi bình thường** | ☐ ☐ |
| F7 | Bật lại theo dõi 14 dòng chuẩn (`UPDATE dbo.ShareDataSourceTable SET IsTracked = 1, UpdateTime = GETDATE() WHERE IsDelete IS NULL AND TableName <> 'TmsEquipment';`) | Hệ thống tự động theo dõi trở lại sau ≤ chu kỳ `reload_interval_sec` (mặc định 30 giây), giữ nguyên `LastVersion` của từng dòng, không quét lại từ đầu, không làm mất các thay đổi | ☐ ☐ |
| F8 | Thêm mới hoặc bật thêm 1 dòng bảng hợp lệ (`LastVersion IS NULL`) trong khi các bảng khác đang chạy ổn định (`LastVersion > 0`) | Chỉ riêng bảng mới được khởi tạo `LastVersion = CHANGE_TRACKING_CURRENT_VERSION()`; **giá trị `LastVersion` của 14 bảng đang chạy hoàn toàn không bị đặt lại (không reset)** | ☐ ☐ |
| F9 | Bảng không hợp lệ / chưa bật Change Tracking cùng tồn tại với các bảng hợp lệ đang có thay đổi dữ liệu | Dòng bảng lỗi được ghi chú vào cột `LastIssue`; các bảng hợp lệ còn lại cập nhật từng dòng có điều kiện LastVersion = mốc cũ; instance nào cập nhật được dòng thì mới phát sự kiện, cập nhật `LastChangeTime` và bắn sự kiện NATS bình thường | ☐ ☐ |

---

## 🅶 G. Gói 110 (Gói 110 chạy trên nguồn có sẵn — phương án A, 16/10/2026, Master Plan R13)

**Điều kiện:** Đã chạy script `sharedata-du-lieu-gia-lap.sql` (thứ tự 4) để có mock dữ liệu `TmsWeather`, `TmsIncident`, `VmsCurrent`; Worker đang chạy.

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
| H2 | Dọn dẹp tệp bằng chứng và nhật ký nghiệp vụ (Export files & Business ActivityLog) | Nhánh xóa tệp bằng chứng trên đĩa (`sharedata/send`) và xóa nhật ký nghiệp vụ trong `DataCleanupService` được tạm thời đặt trong chú thích mã nguồn (commented out) theo quyết định kỹ thuật ngày 16/10/2026 (Master Plan R15, DQ-SV-9, RV-8) để tránh nguy cơ mất bằng chứng giao dịch không thể khôi phục. | Bỏ qua (chưa kích hoạt xóa vĩnh viễn) |
| H3 | Dọn dẹp định kỳ nhật ký hạ tầng Change Tracking ESH-16xx (`DataCleanupWorker`) | `DataCleanupWorker` chạy theo chu kỳ `cleanup_interval_min` (mặc định 1440 phút = 24 giờ). Khi điều chỉnh thử nghiệm `cleanup_interval_min = 1` trong `SysOpConfig`, Worker quét và dọn sạch các bản ghi nhật ký ESH-16xx cũ hơn `tracking_log_retention_days` (mặc định 7 ngày) theo từng lô `tracking_log_purge_batch` (1000 dòng); các nhật ký nghiệp vụ và tệp bằng chứng được bảo toàn nguyên vẹn | ☐ Đạt ☐ Không đạt |

---

## 🅸 I. Nghiệm thu toàn trình tự gửi — tự nhận nội bộ (In-Process Loopback)

Kịch bản chứng minh luồng toàn trình gửi → nhận hoạt động trọn vẹn khi chưa có đối tác bên ngoài hoặc không cần phần mềm bên ngoài hỗ trợ. Kịch bản này đã được khóa tự động bằng bài test `ShareDataSendReceiveTests` (chạy thật trên CSDL test local, không dùng mock).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| I1 | Cấu hình đối tác 'Nội bộ' trỏ endpoint về WebAPI cục bộ (`/api/sharedata/shareDataInbound/receive`), tạo đăng ký Outbound & Inbound cho gói Snapshot (105 hoặc 101) | Worker gửi dữ liệu thành công qua HTTP; Inbound tiếp nhận, lưu bằng chứng đĩa và MERGE vào bảng đích đầy đủ 100% cột | ☐ Đạt ☐ Không đạt |
| I2 | Gửi lặp lại cùng gói Snapshot (kiểm tra Idempotency) | MERGE không nhân đôi số dòng trong bảng đích | ☐ ☐ |
| I3 | Thử nghiệm gói Incremental (104) qua 2 chu kỳ: đợt 1 (3 dòng ban đầu), đợt 2 (1 dòng mới + 1 dòng cập nhật) | Đợt 1 nạp 3 dòng và tiến mốc; đợt 2 chỉ gửi 2 dòng thay đổi, bảng đích cập nhật đúng giá trị mới và tổng 4 dòng | ☐ ☐ |
| I4 | Thử nghiệm đối tác trỏ endpoint lỗi (404 Not Found) | Gửi thất bại sạch sẽ, mốc không tiến, tệp bằng chứng vẫn lưu trên đĩa, ghi cảnh báo vào `ShareDataAlertLog` | ☐ ☐ |
| I5 | Gửi tới API nhận của hệ local (`/api/sharedata/shareDataInbound/receive`) với mã đối tác sai (hoặc bỏ trống header làm API trả HTTP 200 kèm `succeeded: false, code: 400`) | Bộ gửi tự động nhận biết lỗi nghiệp vụ (`ResponseContract = "Auto"` mặc định), màn *Nhật ký lỗi* (`errorLog`) hiện cảnh báo `[400] ...` và mốc gửi (`ShareDataLastSend`) **không tiến** | ☐ ☐ |

---

## 🅹 J. Tham số vận hành ShareData (SysOpConfig nhóm sharedata_config)

> ℹ️ **Cơ chế nạp và đồng bộ tham số:** Các tham số vận hành của phân hệ ShareData được lưu tập trung trong bảng `dbo.SysOpConfig` với `GroupCode = 'sharedata_config'` (gồm 48 tham số: 19 tham số Worker, 23 tham số FE và 6 tham số WebAPI). Người dùng có thể chỉnh sửa trực tiếp trên giao diện màn hình *Cấu hình hoạt động* (lọc theo loại cấu hình `sharedata_config`) hoặc sửa bằng câu lệnh SQL trên CSDL. Worker tự động nạp lại định kỳ theo tham số `reload_interval_sec` trong `SysOpConfig` nhóm `sharedata_config` (mặc định 30 giây, khoảng 5–3600, sửa trên màn Cấu hình hoạt động; không còn chế độ 0).

| # | Bước | Kết quả mong đợi | Kết quả |
|---|---|---|---|
| J1 | Khởi động Worker / WebAPI / FE khi trong CSDL chưa có hoặc thiếu dòng cấu hình (hoặc bị xóa mềm) | Hệ thống **tự động kích hoạt cơ chế dự phòng (fallback)**: lấy giá trị mặc định được định nghĩa sẵn trong mã nguồn (`DataWorkerConfigService` cho Worker, `ShareDataConfigService` cho WebAPI, `SHAREDATA_FE_CONFIG_FALLBACK` cho FE, khai tập trung tại `ShareDataConst`); không phát sinh exception, không làm sập ứng dụng | ☐ Đạt ☐ Không đạt |
| J2 | Trên màn hình *Cấu hình hoạt động*, lọc nhóm `sharedata_config`, chỉnh sửa giá trị 1 tham số của Worker (ví dụ tăng `outbound_check_interval_sec` từ `5` lên `10`) → bấm Lưu | Sau thời gian ≤ chu kỳ `reload_interval_sec` (30 giây), log console của Worker ghi nhận dòng thông báo phát hiện cấu hình thay đổi: ghi rõ giá trị cũ (`5`) → giá trị mới (`10`) và chu kỳ quét mới lập tức có hiệu lực | ☐ ☐ |
| J3 | Sửa giá trị tham số trong `SysOpConfig` thành chuỗi sai định dạng (ví dụ đặt `outbound_check_interval_sec = 'abc'`) hoặc đặt giá trị vượt quá khoảng cho phép (ví dụ `outbound_check_interval_sec = 9999`) | Worker nhận biết giá trị không hợp lệ, **tự động giữ/quay về giá trị dự phòng mặc định (5 giây)** và ghi nhận log cảnh báo của `DataWorkerConfigService`: *"Cấu hình SysOpConfig [sharedata_config.<mã>] có giá trị '<giá trị>' không phải số nguyên; dùng giá trị dự phòng <mặc định>."* (hoặc câu *"Cấu hình SysOpConfig [sharedata_config.<mã>] có giá trị <giá trị> ngoài khoảng an toàn [min..max]; dùng giá trị dự phòng <mặc định>."*), mỗi cặp mã–giá trị đúng 1 lần, không sinh cảnh báo ESH | ☐ ☐ |
| J4 | Tắt dòng tham số bằng cách đổi trạng thái `Status = 0` (hoặc xóa mềm `IsDelete = GETDATE()`) trong `SysOpConfig` | Hệ thống coi như tham số không tồn tại trong DB và **tự động chuyển sang sử dụng giá trị mặc định trong mã**, đảm bảo luồng vận hành không bị gián đoạn | ☐ ☐ |
| J5 | Đặt `packet_lookback_days_max = 730` trong `SysOpConfig` → vào màn hình *Cấu hình gói tin* (SQL Center), chọn gói Incremental (103), nhập `MaxLookBackDays = 700` → Lưu | Giao diện WebVue đọc cấu hình từ API cho phép nhập đến 730 ngày (không bị chặn ở 365); WebAPI kiểm tra hợp lệ với cận trên 730 và **cho phép lưu thành công** xuống CSDL | ☐ ☐ |
| J6 | Kiểm tra tính đồng bộ ngưỡng tối thiểu chu kỳ gửi `subscription_interval_sec_min = 5`: thử nhập chu kỳ `3` giây trên form Đăng ký gói tin, hoặc gọi trực tiếp API tạo đăng ký với `IntervalSeconds = 3` | Cả giao diện Vue (`editSubscription.vue`) và backend `SubscriptionValidator` đều **đồng bộ chặn cứng** giá trị nhỏ hơn 5 giây với thông báo lỗi rõ ràng, không có sự sai lệch ngưỡng giữa FE và BE | ☐ ☐ |

---

---

## 5. Chạy nghiệm thu trên staging

Khi thực hiện nghiệm thu trên môi trường staging, người kiểm thử tuân thủ các thông số hạ tầng, trình tự chuẩn bị và các điểm khác biệt dưới đây:

### 5.1. Bảng thông số kết nối staging

| Thành phần | Thông số kết nối trên staging | Ghi chú |
|---|---|---|
| **SQL Server** | `10.10.8.30:1433` | CSDL chính `DEV_ITS10`, CSDL nhận gói `DEV_ITS10_Inbound` (xem chi tiết [tài liệu 05 mục 1.1](05-chay-sql-tu-dau-sharedata.md#11-chọn-csdl-đích-local-dev_its10--staging-1010830)) |
| **WebAPI** | `<URL_WEBAPI_STAGING>` | Tài khoản kiểm thử `<USER_TEST_STAGING>` |
| **Frontend** | `<URL_FE_STAGING>` | Giao diện WebVue trên môi trường staging |
| **Worker** | Máy `<MAY_WORKER_STAGING>`, thư mục chạy `<THU_MUC_WORKER>` | Thư mục bằng chứng file gửi: `<THU_MUC_WORKER>/sharedata/send` |
| **NATS** | `tls://115.78.1.139:12211` | Tài khoản và mật khẩu cấu hình trên Worker & WebAPI staging |

### 5.2. Trình tự chuẩn bị S1–S8

1. **S1:** Dừng `ShareDataWorker` staging.
2. **S2:** Chạy câu `SELECT` ở [tài liệu 05 mục 1.2](05-chay-sql-tu-dau-sharedata.md#12-kiểm-tra-dữ-liệu-cũ-trước-khi-bật-worker), lưu lại danh sách ID; sau đó chạy câu `UPDATE` để tạm thời tắt (`State = 2`) toàn bộ các đăng ký chia sẻ chiều Outbound (`Direction = 0`). Kiểm các đăng ký trỏ https://tacorp.vn:12204 (có thể là chính WebAPI staging); quyết định giữ hay tạm dừng.
3. **S3:** Triển khai bản build mới của WebAPI, Worker và Frontend lên môi trường staging.
4. **S4:** Chỉ chạy CodeFirst khi truy vấn ở tài liệu 05 mục 2.3 thiếu cột/bảng (staging 19/10/2026 đã đủ).
5. **S5:** Chạy lần lượt các script trên CSDL `DEV_ITS10` theo đúng thứ tự chuẩn: (1) `sharedata-seed-goi-tin.sql` $\rightarrow$ (2) `sharedata-seed-cau-hinh.sql` $\rightarrow$ (3) `sharedata-dich-thuat.sql` $\rightarrow$ *(tùy chọn)* (4) `sharedata-du-lieu-gia-lap.sql` (chi tiết xem [tài liệu 05](05-chay-sql-tu-dau-sharedata.md) mục 2.1).
6. **S6:** Làm mới từ điển như [mục 2.2](#22-làm-mới-từ-điển-đa-ngôn-ngữ-trên-giao-diện) (avatar → Ngôn ngữ → 🔄 hoặc GET <URL_WEBAPI_STAGING>/api/system/sysconfig/loadserverterm?language=vi-VN và en-US).
7. **S7:** Khởi động lại `ShareDataWorker` staging.
8. **S8:** Chạy các câu SQL đối soát tại [tài liệu 05 mục 3](05-chay-sql-tu-dau-sharedata.md#3-kiểm-tra-đối-soát-sau-khi-chạy-script-sql); kiểm tra log khởi động của Worker đảm bảo không phát sinh mã cảnh báo `ESH`.

### 5.3. Bảng các điểm khác biệt khi chạy trên staging

| Mục | Trên staging |
|---|---|
| **0.4, C1, D** | receiver chạy trên máy mà Worker staging gọi tới được; Address P-REST = http://<IP_MAY_RECEIVER>; ⛔ không dùng đối tác thật. |
| **D5, D7, D8, D14** | tệp nằm ở <THU_MUC_WORKER>/sharedata/send; không vào được máy Worker → "Chưa kiểm được". |
| **C7, C8, D12, D13** | Kafka/FTP Docker chỉ có ở local → "Bỏ qua (chỉ local)". |
| **D9** | Worker staging bật NATS → bắt buộc làm. |
| **E1–E4** | đổi http://localhost:5005 trong curl thành <URL_WEBAPI_STAGING>. |
| **G** | dùng dữ liệu thật; không có thì "Bỏ qua" (không chạy 05 Khối 1–3). |
| **I** | bài test tự động chạy ở local → "Bỏ qua". |
| **A3–A5, B10–B11, F, J, H3** | báo nhóm trước, khôi phục ngay sau từng mục. |

### 5.4. Dọn sau khi nghiệm thu trên staging

1. Bật lại các đăng ký chia sẻ (Subscription) đã tắt ở bước S2 theo danh sách ID đã lưu (hoặc dùng câu SQL ở [tài liệu 05 mục 4.5](05-chay-sql-tu-dau-sharedata.md#45-bật-lại-các-đăng-ký-chia-sẻ-subscription-đã-tắt-ở-mục-12)).
2. Xóa đối tác kiểm thử (`P-REST` ở kịch bản C1, các đối tác tạo thêm ở C7, C8) trên giao diện hoặc bằng SQL.
3. Phần dọn SQL (`E2E_INBOUND_TEST`, `InboundTrafficFlow`, `SysOpConfig`, `ShareDataSourceTable`): thực hiện đầy đủ theo [tài liệu 05 mục 4](05-chay-sql-tu-dau-sharedata.md#4-khôi-phục-và-dọn-dẹp-bằng-sql).

---

## 6. Dọn dẹp sau khi test (phần không phải SQL)

### 6.1. Khôi phục cấu hình hệ thống & môi trường debug
- Trả gói tin 105 về chế độ gửi toàn bộ ban đầu (Snapshot - theo bước A5).
- Xóa bỏ bản sao cấu hình debug tạm trong file [`.vscode/launch.json`](../../../../.vscode/launch.json) (nếu đã tạo ở tài liệu 05 mục 2.3).
- Đảm bảo các cờ CodeFirst/Seed trong file cấu hình dự án giữ nguyên giá trị `false`.
- Nếu đã thêm biến môi trường `"Nats__Enabled": "false"` hoặc tùy chỉnh cổng NATS, trả lại như ban đầu.

### 6.2. Dọn dẹp tệp tin trên ổ đĩa
- Xóa các tệp `received-*.json` và file `FAIL` do receiver sinh ra trong quá trình kiểm thử.
- Xóa các thư mục/tệp bằng chứng sinh ra trong thư mục `sharedata/send` (đường dẫn tương đối của Worker).
- Dọn dẹp topic/tệp thử cho D12–D14 nếu có chạy thử nghiệm: xóa topic/message thử trên Kafka broker, xóa tệp đã tải lên trên FTP server local và thư mục File local.

---

## 6.3. Danh sách kịch bản bỏ qua và lý do

| Mã kịch bản | Phân hệ | Trạng thái | Lý do chi tiết |
|:---:|---|:---:|---|
| **D11** | Chiều gửi | **Bỏ qua** | Cơ chế mã hóa đối tác hiện tại chỉ mới viết sẵn thư viện lõi `EncryptManager` và các bộ mã hóa (AesGcm, Rsa...), chưa được đấu nối vào luồng xử lý nghiệp vụ gửi/nhận thực tế theo quyết định kỹ thuật ngày 16/10/2026. |
| **E5** | Chiều nhận | **Bỏ qua** | Tương tự D11, tầng giải mã chiều nhận chưa được kích hoạt trong luồng Worker Inbound. |
| **D9** | Chiều gửi | **Tùy chọn** | Kịch bản gửi ngay khi có dữ liệu mới qua NATS. Cấu hình debug Worker local đã bật sẵn `Nats__Enabled = true`, nhưng chỉ thực hiện được khi máy trạm của lập trình viên có chạy NATS Server local (cổng 4222). Nếu không có NATS, hệ thống vẫn hoạt động bình thường theo cơ chế quét định kỳ. |
| **D12** | Chiều gửi | **Tùy chọn** | Kênh truyền Kafka yêu cầu máy chủ Kafka broker local đang hoạt động. |
| **D13** | Chiều gửi | **Tùy chọn** | Kênh truyền Ftp/FTPS yêu cầu máy chủ FTP server local đang hoạt động. |
| **D14** | Chiều gửi | **Tùy chọn** | Định dạng CSV cho kênh File/Ftp yêu cầu cấu hình đối tác và môi trường tương ứng. |
| **H2** | Hệ thống chung | **Bỏ qua** | Nhánh xóa tệp bằng chứng đĩa và nhật ký nghiệp vụ trong `DataCleanupService` tạm đóng trong khối comment để tránh rủi ro mất mát dữ liệu không thể phục hồi (Master Plan R15, DQ-SV-9, RV-8); luồng dọn nhật ký hạ tầng ESH-16xx đã được chuyển sang `DataCleanupWorker` độc lập (H3). |
| **-** | Đăng ký chia sẻ | **Bỏ qua** | Đăng ký chế độ Sự kiện (EventSourceId) — màn Nguồn sự kiện chưa nối API (FE gọi notImplemented, BE chưa có danh sách nguồn sự kiện). |


---

## 6.4. Tiêu chí đạt


- **Nhóm A–F (Cấu hình gói tin, SQL an toàn, Đối tác & Kết nối, Chiều gửi, Chiều nhận, Bảng theo dõi):** Đạt 100% các tiêu chí kiểm thử bắt buộc.
- **Nhóm G (Gói 110):** Đạt luồng gửi dữ liệu giả lập từ nguồn có sẵn theo phương án A.
- **Nhóm H (Đa ngôn ngữ):** Đạt 100%, không phát sinh chuỗi cứng hoặc lỗi hiển thị bản dịch khi chuyển đổi vi ↔ en.
- **Nhóm I (Nghiệm thu toàn trình In-Process Loopback):** Đạt toàn bộ luồng tự gửi – tự nhận nội bộ (đã được xác nhận tự động qua bộ bài test tích hợp `ShareDataSendReceiveTests`).
- Kết quả tổng hợp sau khi hoàn tất được ghi nhận vào mục **`DQ-TEST-3`** của `Sharedata_MasterPlan.md`.
