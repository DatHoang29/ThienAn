---
tier: A
read: full
source: 
  - Sharedata-videowall.m4a
  - Sharedata-videowall-2.m4a
date: 2026-09-11
model: Gemini Multimodal Native Audio Transcribe
status: verified
topic: ShareData (Chia sẻ Dữ liệu - ESHARE)
participants:
  - Anh Sơn (Tech Lead / Kiến trúc hệ thống)
  - Hiếu (Dev Backend / Service Worker / Mapping)
  - Đạt (Dev Backend / Web API / Database)
---

# Kịch bản & Biên bản Cuộc họp: Phân hệ Chia sẻ Dữ liệu (ShareData)

> **Ghi chú:** Tài liệu này được tổng hợp và hợp nhất toàn diện từ 2 tệp ghi âm cuộc họp ngày 11/09/2026 (`Sharedata-videowall.m4a` và `Sharedata-videowall-2.m4a`), lọc riêng toàn bộ nội dung thảo luận, quyết định kiến trúc, cơ sở dữ liệu và kịch bản đối thoại thuộc phân hệ **ShareData**.

---

## 1. Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary)

### 1.1. Bối cảnh & Yêu cầu thay đổi
- **Phương án cũ:** Hệ thống đang dùng câu truy vấn SqlSugar nối bảng tĩnh (JOIN/WHERE hardcode) để trích xuất dữ liệu và xuất file.
- **Bất cập:** Khi đối tác thay đổi cấu trúc dữ liệu hoặc cần bổ sung các gói tin mới (như gói 101), lập trình viên phải sửa code và build lại toàn bộ; không đáp ứng được yêu cầu tích hợp linh hoạt với nhiều đối tác khác nhau.
- **Quyết định nền tảng:**
  - **Bảo toàn 9 gói tin cũ:** Giữ nguyên cấu trúc CSDL và code hiện có của 9 gói tin đã demo thành công; tuyệt đối không đập đi làm lại để bảo đảm an toàn hệ thống và tiến độ.
  - **Cấu hình động cho luồng mới:** Xây dựng cơ chế cấu hình trường (Field Metadata) và bảng ánh xạ (Mapping Rules) động trong CSDL cho các gói tin mới (bắt đầu từ gói 101).

---

### 1.2. Thiết kế Cơ sở dữ liệu: Bảng cấu hình trường (Field Metadata Schema)
Hệ thống quản lý danh mục trường dữ liệu theo gói tin thông qua bảng cấu hình metadata:
- `PacketCode`: Mã gói tin (ví dụ: `101`).
- `FieldCode`: Mã định danh trường kỹ thuật (ví dụ: `TocDo`, `LuuLuong`, `BienSo`).
- `FieldName`: Tên hiển thị của trường.
- `DataType`: Kiểu dữ liệu, chuẩn hóa gồm các nhóm:
  - `String`: Chuỗi ký tự (hỗ trợ kiểm tra độ dài tối đa qua `Length` / `MaxLen`).
  - `Number`: Phân định rõ số nguyên 4-byte (`int`), số nguyên 8-byte (`long`), và số thực có dấu thập phân (`float`/`double`).
  - `DateTime`: Thời gian, có định dạng format đi kèm (`FormatString`).
  - `Boolean`: Đúng / Sai (`true`/`false`).
- `Description`: Mô tả ý nghĩa nghiệp vụ của trường.

---

### 1.3. Cơ chế Ánh xạ (Mapping Engine) & Quy tắc biến đổi dữ liệu
Để kết nối linh hoạt với nhiều đối tác mà không cần sửa code, hệ thống xây dựng cơ chế Mapping gồm 3 thành phần:
1. **Bộ mã quy đổi (CodeSet / Value Mapping):**
   - Dùng cho các trường trạng thái, danh mục enum.
   - Ánh xạ cặp giá trị: `SourceValue` $\leftrightarrow$ `TargetValue` (ví dụ: nội bộ lưu `1`/`0`, xuất đối tác là `"Bật"`/`"Tắt"` hoặc `"Active"`/`"Inactive"`).
   - Nếu dữ liệu không nằm trong CodeSet: hỗ trợ giá trị mặc định (`DefaultValue`) hoặc giữ nguyên giá trị gốc.
2. **Hàm tính toán & Tổng hợp (Aggregation Functions):**
   - Hỗ trợ các hàm: `SUM` (tính tổng), `AVG` (tính trung bình), `MIN`, `MAX`, `COUNT` (đếm bản ghi).
   - Áp dụng khi đối tác yêu cầu dữ liệu thống kê từ tập hợp nhiều bản ghi con.
3. **Định dạng dữ liệu (Formatting):**
   - Format ngày giờ theo mẫu đối tác (`yyyy-MM-dd HH:mm:ss`, `dd/MM/yyyy`, ISO-8601...).
   - Format chữ số thập phân (số chữ số sau dấu phẩy).

---

### 1.4. Kiến trúc luồng xử lý hai chiều (Inbound & Outbound)
1. **Luồng gửi đi (Outbound - Xuất dữ liệu cho đối tác):**
   - Service đọc dữ liệu nội bộ từ CSDL.
   - Nạp cấu hình mapping của gói tin tương ứng.
   - Thực hiện biến đổi (đổi tên trường, áp dụng CodeSet, format ngày/số, chạy hàm tính toán).
   - Sinh payload JSON/XML và gửi qua REST API đối tác hoặc ghi file xuất bản.
2. **Luồng nhận vào (Inbound - Tiếp nhận dữ liệu từ đối tác):**
   - Web API tiếp nhận payload từ đối tác -> Ghi nhận dữ liệu thô vào bảng đệm (hoặc đẩy qua NATS).
   - Background Service lắng nghe -> Đọc quy tắc mapping ngược.
   - Chuyển đổi dữ liệu về chuẩn nội bộ -> Lưu vào các bảng dữ liệu nghiệp vụ chính thức.
   - Tách biệt rõ ràng: Web API chỉ nhận request HTTP, còn logic mapping nặng và ghi CSDL do Background Service đảm trách.

---

### 1.5. Thiết kế Giao diện (UI/UX) cấu hình tinh gọn
- **Vấn đề:** Giao diện cũ hiển thị tất cả các trường trên cùng một bảng làm tràn màn hình, rối mắt và khó quản lý.
- **Thống nhất mới:**
  - Bảng chính chỉ hiển thị thông tin vĩ mô: Mã gói tin, Tên gói tin, Người cập nhật, Thời gian cập nhật cuối, Trạng thái.
  - Bấm vào từng gói tin sẽ mở **Modal/Sub-panel** riêng để cấu hình danh sách trường và quy tắc mapping.

---

## 2. Ma trận phân công công việc & Kế hoạch hành động

| Thành viên | Trách nhiệm | Công việc chi tiết | Deadline |
|---|---|---|---|
| **Đạt** | Backend Web API & CSDL | - Thiết kế các bảng CSDL cho Metadata trường và Mapping.<br>- Viết các endpoint Web API phục vụ luồng nhận (Inbound) và gửi (Outbound).<br>- Chuẩn hóa DTO và cấu trúc payload JSON. | Hết tuần sau |
| **Hiếu** | Service Worker & Mapping Engine | - Lập trình Core Service Worker đọc cấu hình và thực thi mapping động.<br>- Hiện thực hóa bộ mã quy đổi CodeSet và các hàm tính toán (`SUM`, `AVG`,...).<br>- Xử lý luồng ghi dữ liệu vào bảng đệm và CSDL chính. | Hết tuần sau |
| **Kiên** | Frontend UI | - Cải tiến giao diện cấu hình gói tin: thu gọn bảng chính, đưa chi tiết trường vào modal/tab.<br>- Tích hợp các API cấu hình từ Đạt và Hiếu. | Hết tuần sau |

---

## 3. Kịch bản đối thoại chi tiết (Chronological Transcript)

### [Phần 1: Khởi động & Rà soát vấn đề cốt lõi]
- **Anh Sơn:** Bắt đầu xem lại luồng dữ liệu của ShareData hôm bữa. Luồng hôm bữa là từ dữ liệu trong CSDL, chạy câu truy vấn SqlSugar xong rồi xuất ra file đúng không?
- **Hiếu / Đạt:** Dạ đúng rồi anh, từ CSDL lấy ra file.
- **Anh Sơn:** Bây giờ mình phải hình dung cái output trả về. Hôm bữa em quan tâm là output ra cái gì, rồi trong output đó có những trường nào. Nhưng mà hiện tại mình không thể nào cứ mỗi lần đối tác yêu cầu một kiểu là mình lại ngồi viết code câu SQL cứng được. Cần phải có một bảng cấu hình.
- **Đạt:** Ý anh là thay vì viết câu truy vấn cố định để xuất file thì mình sẽ có một bảng để lưu cấu hình các trường mà gói tin cần xuất ra?
- **Anh Sơn:** Đúng rồi. Mình phải lưu tên cột, mã cột của từng gói tin. Ví dụ gói tin 101 nó có 10 trường hay 12 trường thì mình lưu cấu hình 12 trường đó lại.

---

### [Phần 2: Cấu trúc trường Metadata của Gói tin 101]
- **Anh Sơn:** Trong gói 101, mỗi trường nó sẽ có:
  1. Tên trường (Field Name / Code)
  2. Kiểu dữ liệu của trường đó (Data Type): chuỗi, số, ngày tháng...
  3. Mô tả của trường đó.
  Lúc này mình chưa cần quan tâm dữ liệu lấy từ bảng nào dưới CSDL, mình chỉ cần biết gói tin 101 nó gồm những trường nào đã.
- **Hiếu:** Vậy là mình lưu danh sách các trường của gói tin đó lại vào DB?
- **Anh Sơn:** Đúng. Một gói tin 101 trả về sẽ có cấu trúc trường. Mình lưu tên trường và kiểu dữ liệu. Sau đó mới đến bước mapping: trường này của mình sẽ tương ứng với trường nào của đối tác, hoặc trường này lấy giá trị từ đâu.
- **Đạt:** Vậy kiểu dữ liệu mình phân loại thế nào anh?
- **Anh Sơn:** Kiểu dữ liệu cơ bản thì có:
  - `String`: Chuỗi ký tự.
  - `Number`: Cần chú ý phân biệt số nguyên 4 byte (`int`), 8 byte (`long`), và số thực có phần thập phân (`float`/`double`).
  - `DateTime`: Ngày giờ, cần lưu ý format ngày tháng năm giờ phút giây.
  - `Boolean`: Đúng/sai (`true`/`false`).

---

### [Phần 3: Quy tắc Mapping, Bộ mã quy đổi CodeSet & Hàm tính toán]
- **Hiếu:** Dạ vậy còn cái đoạn mapping giữa bên mình với bên đối tác thì xử lý sao anh?
- **Anh Sơn:** Sẽ có một giao diện hoặc bảng mapping. Trong đó mình chọn:
  - Gói tin cần cấu hình (ví dụ 101).
  - Chọn đối tác A, đối tác B.
  - Đối tác A yêu cầu trường tên là `VehicleSpeed`, bên mình tên là `TocDo`. Thì mình map `TocDo` sang `VehicleSpeed`.
  - Nếu đối tác yêu cầu tính toán, ví dụ tính trung bình tốc độ hay tổng số xe, thì mình áp dụng hàm (`SUM`, `AVG`).
  - Nếu trường đó là mã trạng thái, ví dụ bên mình lưu `0` là bình thường, `1` là sự cố, nhưng đối tác muốn hiển thị chữ `"Normal"` và `"Warning"`, thì mình phải có **bộ mã quy đổi** (CodeSet).
- **Đạt:** Bộ mã quy đổi này là mình định nghĩa trước các cặp key-value phải không anh?
- **Anh Sơn:** Đúng, một bộ mã quy đổi gồm: giá trị nguồn (`SourceValue`) và giá trị đích (`TargetValue`). Khi xử lý gói tin, nếu gặp trường có cấu hình CodeSet thì nó sẽ tự động thay thế giá trị tương ứng.
- **Anh Sơn:** Ngoài bộ mã quy đổi, còn các phép tính tổng hợp dữ liệu: Đếm số lượng bản ghi (`COUNT`), tính tổng (`SUM`), tính trung bình (`AVG`), tìm giá trị lớn nhất/nhỏ nhất (`MAX`/`MIN`).
- **Hiếu:** Nếu dữ liệu trả về dạng mảng danh sách (array) hoặc object lồng nhau (cha - con) thì sao anh?
- **Anh Sơn:** Cấu trúc gói tin sẽ có trường hợp là 1 record đơn (object phẳng) hoặc danh sách nhiều record (array/collection). Cấu hình của mình phải thể hiện được trường nào là mảng, trường nào là phần tử con. Sau đó vòng lặp sẽ duyệt qua các phần tử để map theo đúng cấu trúc JSON mong muốn.

---

### [Phần 4: Luồng xử lý dữ liệu 2 chiều & Tách biệt Service]
- **Đạt:** Vậy luồng gửi dữ liệu đi (Outbound) và luồng nhận về (Inbound) sẽ tổ chức thế nào?
- **Anh Sơn:** 
  - **Luồng gửi (Outbound):** Service đọc dữ liệu từ database nội bộ $
ightarrow$ Đọc cấu hình mapping của gói tin $
ightarrow$ Chuyển đổi dữ liệu $
ightarrow$ Sinh payload JSON/XML $
ightarrow$ Gửi qua API đối tác hoặc xuất file.
  - **Luồng nhận (Inbound):** Web API tiếp nhận request từ đối tác $
ightarrow$ Nhận payload vào, kiểm tra hợp lệ sơ bộ $
ightarrow$ Đẩy dữ liệu vào bảng trung gian/bảng đệm $
ightarrow$ Service đọc cấu hình mapping ngược $
ightarrow$ Ghi dữ liệu chuẩn hóa vào các bảng nghiệp vụ trong CSDL.
- **Hiếu:** Em thấy nếu làm vậy thì giữa Web API và Service phải có cơ chế giao tiếp rõ ràng, không nên để Web API xử lý quá nặng logic mapping và ghi DB.
- **Anh Sơn:** Đúng, Web API chỉ làm nhiệm vụ nhận/gửi HTTP, xác thực quyền. Toàn bộ logic nghiệp vụ nặng như mapping, convert dữ liệu, ghi log, đồng bộ phải đưa vào Background Service.

---

### [Phần 5: Tinh gọn giao diện & Không đụng vào 9 gói tin cũ]
- **Hiếu:** Mấy chỗ này, mấy cái này có cần nó gọn lại, có cần chi tiết giống vậy không anh?
- **Anh Sơn:** Ý em là muốn gọn cái gì? Nói rõ ra.
- **Hiếu:** Dạ ý em là mấy cái nội dung này, về mặt object về mặt field ở dưới DB á, thì ẩn nó đi chứ hiện lên đây làm cái gì. Chỉ cần ghi là thời gian này ai là người điều chỉnh cho cái cấu hình này thôi.
- **Anh Sơn:** Đúng rồi, còn dữ liệu của trường đó thì có thể lưu lại được. Bê nó qua modal để nhìn cho nó thống nhất.
- **Đạt:** Cái phần cấu hình gói tin á, ví dụ trên UI bên em em sẽ làm gì tới?
- **Hiếu:** Chỗ gói tin này hiện tại không chia theo bảng nữa mà lấy thẳng cái field ra luôn.
- **Đạt:** Chỗ này để em thấy được mã trường với tên trường thì em phải join theo gói tin 101 này nọ đúng không?
- **Anh Sơn:** Về mặt hiển thị thì thật ra không cần cái đó. Nhưng khi mà em lấy lên á, thì em cần câu này để lấy lên. Tên trường sẽ lấy từ thằng này lên.
- **Anh Sơn:** Lưu ý quan trọng: nguyên cái phần phía trước hiện tại là **không có sửa vào SqlSugar hay đụng vào 9 cái gói tin hôm bữa**. Nếu bây giờ ngồi sửa lại là phải sửa lại hết nguyên 9 gói tin hôm bữa đó, rất là mất thời gian và rủi ro.

---

### [Phần 6: Chốt phương án & Cam kết hoàn thành]
- **Hiếu:** Dạ rõ rồi anh. Phần ShareData này em nhúng vào nhiều rồi nên em sẽ tự chỉnh sửa lại phần Service mapping và phối hợp với Kiên ở phần giao diện modal.
- **Đạt:** Em sẽ phụ trách thiết kế các bảng CSDL cho trường metadata, bảng mapping và viết các endpoint API nhận/gửi.
- **Anh Sơn:** Thống nhất vậy. Hết tuần sau phải có bản chạy được trọn vẹn: cấu hình được các trường của gói tin và test thông suốt luồng mapping dữ liệu.\n

===============================================

Note của sếp:
object 101

A     kiểu string/datetime/number/ bool
B
C
D
E
F



SQLsugar

 -> DL -> file output -> cấu trúc json -> object Json.Serialize (List<object101>
 
 Column                        Collection
 Name Code ....				   Record  query 1
 Name string Tên ....          Record
 Code  string  

 StationName					Info   query 2

{PackageType: 101, Time: now, Records: [ {},{},{},....], Info: {}}


 
 
 mapping dữ liệu
 
 
 gói tin - doi tac - chieu gui (2 chiều)
 
 101    - A   - 2 chiêu
 
 
 
 TRUY VẤN sql SUGAR - >   OBJECT       -> sERIALIZE RA FILE JSON
	
	
DC

 TRUY VẤN sql SUGAR - >   OBJECT   
							-> doc file config mapping (W7)
									-> xử lý map dữ liệu
										- Codeset 
										- Phép tính (avg,count,sum,...)
										- Convert
										
										-> Format đầu ra (kiểu dữ liệu, định dạng hiển thị)
										
										=> object dynamic Hashset/Dictionary/.....
									-> Biến đổi về định dạng gói tin mong muốn (json, xml, byte, 
									
									64byteName32byteCode4byteType8byteTotal
									
									-> Gửi dữ liệu (API/FTP/SSE/....)
										-> Ghi nhan xu ly truyen nhan 
							-> SERIALIZE RA FILE JSON
							-> GHI LOG -> noi dung minh da gui cho doi tac
Nhận

Dữ liệu -> API nhận  -> Xử lý mapping ngược lại -> Lưu trữ

API: Web: noi dung nhan tu doi tac
Xử lý mapping: Service


 

NHan duoc

 
doitac          BE                   Service
Data     ->    API		->			 Code xu ly

						=> Chuyển tiếp dữ liệu xử lý (Nats, CSQL Table Temp, File, MessagePack, ...)
															=> Quét định kỳ ?
															=> Bảng mới trong CSDL2 Nhận dc luu về) => ShareDaa
															doi tac, ma goi tin, thoi gian, du lieu					

Service co DL thô rồi, thì bat dau thuc hien
	 
	- Xac dinh doi tac nao, goi tin nao, huong nhan => doc bang cau hinh
		=> Thiet lap (W7)
		=> Codeset danh sách
		
	- khi có cau hinh thiet lap mapping -> mapping du lieu
		=> Trường dữ liệu -> Cột quản lý trong gói tin (W0)
		=> Codeset nguoc lai ve gia tri CSDL2
		=> Format lai theo cau truc CSDL. dd/MM/yyyy => yyyy-MM-dd HH:mm:ss
		=> COnvert kiểu dữ liệu. (string => int)
	
	Du lieu sau khi mapping
		=> Tạo cau truy van xu ly luu tru du lieu (them moi/chinh sua (check exist)
		=> Truyen gia tri vao cau truy van
		=> Thuc thi truy van
		=> Ghi nhan ket qua
	
	Ghi log xu ly

========================================================
Note của tôi:
1.Từ cơ sở dữ liệu ra file output từ những ouput trong file sẽ lưu tên field trong bảng mới, và kiểu dữ liệu như là string/datetime//number, dùng number cẩn thận liên quan dữ liệu float số thực, bool
2. Trước đó sqlsugar join các kiểu  để ra mong muốn => json object mong muốn => mong muốn lấy từ object mong muốn luu lại tên field và kiểu giá trị
3. bảng mapping => gói tin => đối tác => chiều gửi (2 chiều) 101 => A - => 2 CHIỀU
4. bảng mapping nếu dùng XML vẫn có thể quy về json ví dụ xml <Data> vẫn có thể về => "Data"
5. Ra được cấu trúc mong muốn rồi 10 trường => Gói 101 liên quan giao thông => lúc lấy từ chỉ lấy liên quan giao thông
6. Phải cấu hình Bảng 101 gói tin sẽ có những field gì trong cơ sở dữ liệu bên mình phải cấu hình trước luôn. 
7. Một số hàm có sẵn Sum, Averget (tính trung bình).
8. Lưu xuống dưới DB sau khi xử lý xong 
9. Dựa vô gói 101 có những field gì mình sẽ đưa thông tin cho hiếu lưu DB.
10. Nhãn mô tả , và nếu giá trị đối tác mà không có trong cơ sở dữ liệu.
11. SQL Sugar => object => doc file config mapping  =>  map xử lý dữ liệu => codeset => phép tính(Sum, Averget) => convert => format đầu ra (kiểu dữ liệu, định dạng hiển thị)
12. Luồng gửi Sau khi xử lý xong gửi dữ liệu API/SSE gửi , ghi ra file roi, rồi ghi log. Dữ liệu > API nhận => xử lý mapping => lưu trữ.
13. Luồng nhận đi qua webapi => lưu vô bảng => service  lấy database ra xử lý dữ liệu nhận.
14. Luồng nhận khâu xử lý xác định => đối tác nào => gói tin nào => hướng nhận => đọc bảng cấu hình =>  thiết lập => Codeset danh sách.
15. Khi có cấu hình thiết lập mapping => mapping dữ liệu => trường dữ liệu (tên mình nhận) => cột quản lý trong gói tin mình cấu hình => codes et ngược lại về giá trị CSDL2 => format lại theo cấu trúc CSD dd/mm/yyyy => yyyy-mm-dd hh:mm:ss => convert kiểu dữ liệu (string => int)
16. Dữ liệu sau khi mapping luồng nhận => tạo câu truy vấn xử lý lưu trữ dữ liệu (thêm mới/chỉnh sửa/) check tồn lại => truyền giá trị vào cau truy vấn => thực thi truy vấn => ghi nhận kết quả.
17. SQLSugar native
18. Nhận được ghi log luôn.
19. Trạng thái ghi thêm xử lý thành công hay thất bại.
20. Nội dung log bê qua model cho thống nhất, khỏi sùng sidebar tham khảo nhật ký lỗi
21. Đừng xử lý format byte gửi số bản tin.
22. Luồng nhận xử lý sql truy vấn không hardcode.