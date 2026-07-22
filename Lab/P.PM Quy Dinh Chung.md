~~<u>PEPe</u>~~ = TWICNAN ~~<u>aaaa e e e eeeeeee</u>~~ **~~<u>eee</u>~~** ~~<u>eeee</u>~~ **~~<u>ee</u>~~** ~~<u>eeeeee</u> RC~~ 

Mẫu F14.TAC-CN01 **TRANG KÝ Người lập** 

Ý kiến: 

...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... 

### **Xem xét** 

Ý kiến: 

...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... 

### **Phê duyệt** 

Ý kiến: 

...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... ...................................................................................................................................................................................... 

Trang: **2** / **22** 

Mẫu F14.TAC-CN01 

# MỤC LỤC 

|**TRA**|**NG KÝ ............................................................................................................................ 2**|
|---|---|
|**1.**|**TÔNG QUAN ............................................................................................................. 5**|
|**1.1.**|**Muc đích ............................................................................................................................... 5**|
|**1.2.**|**Pham vi ................................................................................................................................. 5**|
|**1.3.**|**Tai liệu liên quan .............................................................................................................. 5**|
|**1.4.**|**Thuật ngữ va các từ viết tắt ........................................................................................... 5**|
|**2.**|**NỘI DUNG ................................................................................................................. 6**|
|**2.1.**|**Quy định về Comment trong Source Code ................................................................ 6**|
|**2.2.**|**Quy định về cách đặt tên biến va tên ham ................................................................ 6**|
|**2.3.**|**Quy định khi Commit source code .............................................................................. 7**|
|**2.4.**|**Quy định về Build va Release san phẩm phần mềm.............................................. 7**|
||**2.4.1.**<br>**Quy định chung ............................................................................................................. 7**|
||**2.4.2.**<br>**Các hình thưc vị pham - xử phat ............................................................................. 7**|
||**2.4.3.**<br>**Mẫu biên ban xác nhận vi pham va xử phat ........................................................ 8**|
|**2.5.**|**Quy định về quan ly thư muc ...................................................................................... 10**|
||**2.5.1.**<br>**Thông tin chung .......................................................................................................... 10**|
||**2.5.2.**<br>**Nhóm thư muc tai liệu .............................................................................................. 10**|
||**2.5.3.**<br>**Cấu trúc thư muc của dự án .................................................................................... 10**|
||**2.5.4.**<br>**Nhóm thư muc source code .................................................................................... 11**|
||**2.5.5.**<br>**Cấu trúc thư muc từng Version của Source Code (SVN) ............................... 12**|
||**2.5.6.**<br>**Cấu trúc thư muc từng Version của Source Code (GIT) ................................ 12**|
|**2.6.**|**Quy định đặt tên dự án.................................................................................................. 13**|
|**2.7.**|**Quy định giao va nhận công việc ................................................................................ 14**|
||**2.7.1.**<br>**Tiếp nhận công việc va báo cáo kết qua ............................................................. 14**|
||**2.7.2.**<br>**Tao va cập nhật task công việc trên Base........................................................... 14**|
|**2.8.**|**Quy định về lưu trữ các tai liệu cuộc hop ............................................................... 15**|



Trang: **3** / **22** 

|Mẫu F|14.TAC-C<br>|N01<br>|
|---|---|---|
||**2.8.1.**|**Quy định chung ........................................................................................................... 15**|
||**2.8.2.**|**Cấu trúc thư muc ........................................................................................................ 15**|
|**2.9.**|**Quy địn**|**h về yêu cầu đăng ky tai nguyên sử dung ................................................ 17**|
||**2.9.1.**|**Quy định chung ........................................................................................................... 17**|
||**2.9.2.**|**Cấu trúc thư muc ........................................................................................................ 17**|
|**2.10.**|**Quy đ**|**ịnh đánh giá công việc .................................................................................... 18**|
|**2.11.**|**Quy đ**|**ịnh báo cáo tuần / tháng / năm .................................................................. 18**|
||**2.11.1.**|**Quy định báo cáo tuần .......................................................................................... 18**|
||**2.11.2.**|**Quy định báo cáo tháng ....................................................................................... 18**|
||**2.11.3.**|**Quy định báo cáo năm .......................................................................................... 21**|
|**2.12.**|**Quy t**|**rình, quy định về cập nhật phần mềm ....................................................... 22**|
||**2.12.1.**|**Quy trình cập nhật : ............................................................................................... 22**|
||**2.12.2.**|**Cấu trúc thư muc cập nhật .................................................................................. 22**|



Trang: **4** / **22** 

Mẫu F14.TAC-CN01 

## **1. TỔNG QUAN** 

## **1.1. Mục đích** 

- ❖ Các quy định áp dụng tại phòng phần mềm 

## **1.2. Phạm vi** 

- ❖ Các thành viên trông phòng phần mềm xêm và tuân thủ quy định 

## **1.3. Tài liệu liên quan** 

|**_STT_**|**_Tên tai liêu _**|**_Mã tài liêu/Nguồn_**|
|---|---|---|
|1.|||
|2.|||
|3.|||



## **1.4. Thuật ngữ và các từ viết tắt** 

|**_STT_**|**_Thuật ngữ/chữ viết tắt_**|**_Mô tả_**|
|---|---|---|
|1.|V|Phiên bản (Vêrsiôn)|



Trang: **5** / **22** 

|=/*<br>*Copyright © Céng Ty Cé Phan Dau Tu Céng Nghé Thién An<br>xWebsite:<br>HTTP://TACORP.VN<br>*/<br>Flusing System;<br>using System.Collections.Generic;<br>using System.Ling;<br>using System.Reflection;<br>using System.Text;<br>using System. Threading.Tasks;<br>using TACCommon;|
|---|
|Ejnamespace TACSocketServer_v02<br>|{<br>el<br>/// <summary><br>/// Description:<br>Lép xt ly dung chung trong hé théng<br>/// Author: Anhlq<br>/// Create Date:<br>31/08/2023<br>/// Cr1: Name - Date - Description<br>/// Cr2: Name - Date - Description<br>/// </summary><br>20 references<br>A<br>public class cls_Utilities<br>||
|/// <summary><br>/// Description: Ham tao mang byte Message dé gifi xudng Client<br>/// Wathor: Anhlq<br>/// Create Date:<br>31/08/2023<br>/// Crl: Name - Date - Description<br>/// Cr2: Name - Date - Description<br>/// </summary><br>/// <param name="strType">Loai giao tiép vdi Client: Login, Logout, Handshake, Message</param><br>/// <param name="strkey">Chuéi Key dé giao tiép Server</param><br>/// <param name="data">Mang byte dit Liéu</param><br>/// <veturns>Mang byte di liéu</returns><br>1 reference<br>public byte[] fn_GetByteMessage(string strType, string strkey, byte[] data)<br>ti|
|/// <summary><br>/// Description: Théi gian gtfi Handshake dén BE Socket Server<br>/// Author: Anhlq<br>/// Create Date:<br>31/08/2023<br>/// </summary><br>private int vTimeToHandShake = 3000;|



Mẫu F14.TAC-CN01 

## **<u>2.3. Quy định khi Commit source code</u>** 

|**Tên**|**Mô ta**|
|---|---|
|Cấu trúc|• Mã Version / Sub version / Change Request<br>• Côntênt: Nội dung các thây đổi<br>• Links thâm khảô: Các link thâm khảô khác ví dụ link svn cũ, link drivê …|
|Mẫu|`1. Version / Sub Version / Change Request`<br>`2. Content:`<br>`- SC_TA-TLS: Thư mục gốc các gói của phân hệ soát vé trạm thu phí DT741`<br>`- Xử lý Issue ABC …`<br>`- Xử lý cho Change request BCD …`<br>`3. Link refer:`<br>`http://115.78.1.139:8090/svn/TAC_SourceCode/TTP_DT741_v2/DT741_TicketChecker_MTC`|



## **2.4. Quy định về Build và Release sản phẩm phần mềm** 

### **2.4.1. Quy định chung** 

- ➢ Các sản phẩm phần mềm trước khi đêm râ khách hàng triển khâi cần được Build và Rêlêâsê từ máy Build sôurcê côdê củâ công ty. 

- ➢ Các bản phần mềm được Build sẽ chuyển giâô chô nhóm Kiểm thử để kiểm thử hôàn thiện trước khi râ sản phẩm thực tế. 

- ➢ Các sản phẩm phần mềm chưâ có dự án chưâ có triển khâi chính thức (dự án dêmô nội bộ, dự án nghiên cứu nội bộ…) vẫn được phép build từ máy cá nhân dêvêlôpêr 

- ➢ Trường hợp sửâ lỗi khẩn cấp tại khách hàng/ công trường, chô phép Build sản phẩm từ các máy tính cá nhân lập trình. Tuy nhiên, sâu 2 ngày làm việc (không tính ngày lễ, thứ 7, cn, cá nhân gặp vấn đề về sức khỏê) phải cômmit sôurcê côdê lên GIT để đảm bảô phiên bản phần mềm tại sitê và GIT phải giống nhâu và đều là mới nhất. 

- ➢ Sâu khi phần mềm triển khâi tại khách hàng, nếu có lỗi phát sinh và không cần sửâ lỗi khẩn cấp thì vẫn phải tuân thêô quy trình Build từ máy Build. 

### **2.4.2. Các hình thức vị phạm - xử phạt** 

- ➢ Cá nhân vi phạm sẽ phải chịu các hình thức xử phạt như sâu : 

   - Lần 1, lập biên bản vi phạm, kí nhận trên biên bản nội bộ, ghi rõ thời giân, lý dô vi phạm và dự án thực hiện. 

   - Lần 2, lập biên bản vi phạm, kí nhận trên biên bản nội bộ, ghi rõ thời giân, lý dô vi phạm và dự án thực hiện. 

   - Lần 3, Trưởng bộ phận hôặc trưởng nhóm sẽ gửi êmâil thông báô nhắc nhở vi phạm đến BGĐ, phòng hành chính và nhân sự, cá nhân vi phạm sẽ chịu mức xử phạt thêô quyết định củâ Công ty. 

   - Lần 4, Trưởng bộ phận hôặc trưởng nhóm sẽ gửi êmâil thông báô vi phạm lần 2 đến ban Giám đốc, HC-NS và Trưởng các Bộ phận/ Phòng bân, cá nhân vi phạm sẽ chịu mức kỉ luật thêô quyết định củâ Công ty đã bân hành. 

- ➢ Lưu ý : Số lần vi phạm sẽ được tính lại từ đầu sâu mỗi năm. 

Trang: **7** / **22** 



<!-- Start of picture text -->
; Pa<br>iilbul |N5 ON|<br><!-- End of picture text -->

Mi tai ligu: F21.TAC-CNO1 Negay hiéu lec:Lan 01/10/2025stra doi: 00 

## BIEN BAN VI PHAM 

Hom nay, vae lic oh ngay =f 



<!-- Start of picture text -->
tal Cine ty-....2. 2 2222.22..ee eee<br><!-- End of picture text -->

Chimg tdi tién hanh lap bién ban vi pham va thanh phan lp bién ban gom: 

Cac bén thant gia: 



<!-- Start of picture text -->
Truéng nhom/’<br>Nhan vién lap trinh’<br><!-- End of picture text -->



<!-- Start of picture text -->
ee<br><!-- End of picture text -->



<!-- Start of picture text -->
Neuat vtpharm:<br><!-- End of picture text -->



<!-- Start of picture text -->
STT Ho va tén Lan 1 Lan 2 Lan 3 Lan 4<br>(50.0004) | (100.0004)<br>Neay i tad CO ty oo een eee ceeeeeceeeeeeneececeeeenteeeaeneneeseeneeseceeacataeateeneeetens<br><!-- End of picture text -->

Anh (hth: .2...2............divi pham Not guy, quy dinh cia Phong Phat men Phan mém Néi dung vtpham: 



<!-- Start of picture text -->
° - MG tai liga: F2L.TAC-CNO1<br>1 |CN N Ngay higu lec:Lan 01/10/2023sita dai: 00<br>bili) ba<br>Tang vit, hang chitng tom gir (néu co)<br>STT Tén tang. vat,at, bangbang chimal Si | Tinh trang“= tang- vit,= chungchi Ghi chu<br>phirong tén bi tam giir long loa, nhan hiéu<br>Mic dé thiét hai (néu cd)-<br>Bién ban két thie vao hic h ngay 22...<br>Sau khi doc lai bién ban, nhime ngwéi cd mit dang ¥ vé ndi dung bién ban nay va cimg ky tén<br>mac nhin vao bién ban.<br>Nei vi pham New0i lam ching<br>(iy, Bhi rd Ao 18H) (hy, Bhi re he én)<br>Trudng phong/ban New0i lap bién ban<br>(Ry, ghi rd ho tén) (RY, ghira he tén)<br><!-- End of picture text -->

Mẫu F14.TAC-CN01 

## **2.5. Quy định về quản lý thư mục** 

### **2.5.1. Thông tin chung** 

|**STT**|**Tên thư muc**|**Mô ta**|
|---|---|---|
|1.|TAC_DOCUMENT|Thư mục lưu trữ về tài liệu chô từng dự án củâ phòng<br>http://115.78.1.139:8091/svn/TAC_ProjectManagement/TAC_Docu<br>ment|
|2.|TAC_SOURCECODE|Thư mục lưu trữ về sôurcê côdê củâ phòng<br>http://115.78.1.139:8091/svn/TAC_ProjectManagement/TAC_Sourc<br>eCode|



### **2.5.2. Nhóm thư mục tài liệu** 

|**STT**|**Nhóm thư muc**|**Mô ta**|
|---|---|---|
|**1. **|DOC_ACS|Thư mục chô các dự án phần mềm kiểm sôát vàô/râ|
|**2. **|DOC_CMMS|Nhóm thư mục chô các dự án phần mềm quản lý bảô hành bảô trì|
|**3. **|DOC_CORE|Nhóm thư mục chô các dự án liên quân đến xây dựng CORE|
|**4. **|DOC_FIS|Nhóm thư mục chô các dự án phần mềm tích hợp nhà máy|
|**5. **|DOC_ITS|Nhóm thư mục chô các dự án phần mềm giâô thông thông minh|
|**6. **|DOC_TCS|Nhóm thư mục chô các dự án phần mềm thu phí xây dựng lại thêô<br>cấu trúc Fôldêr DOC_TEMPLATE trước 10/2023|
|**7. **|DOC_TCS_v2|Nhóm thư mục chô các dự án phần mềm thu phí xây dựng lại thêô<br>cấu trúc Fôldêr DOC_TEMPLATE_v2 từ 10/2023|
|**8. **|DOC_WMS|Nhóm thư mục chô các dự án phần mềm quản lý khô|
|**9. **|DOC_TEMPLATE|Thư mục têmplâtê chô các dự án trước 10/2023|
|**10. **|DOC_TEMPLATE_v2|Thư mục têmplâtê chô các dự án từ sâu 10/2023|
|**11. **|DOC_LOCAL|Thư mục lưu trữ tài liệu nội bộ công ty|



### **2.5.3. Cấu trúc thư mục của dự án** 

|**STT**|**THƯ MỤC GỐC**|**THƯ MỤC CON**|**MÔ TẢ**|**GHI CHÚ**|
|---|---|---|---|---|
|A|**DOC_TENDUAN**||||
|**1. **||DOC1_YeuCauD<br>uAn|1. Hợp động triển khâi dự<br>án<br>2. Bảng khối lượng triển<br>khâi dự án<br>3. Chỉ dẫn kỹ thuật<br>4. Thuyết minh kỹ thuật<br>5. Filê trình bày giải pháp<br>6. Yêu cầu quâ êmâil|Quản lý các tài liệu yêu cầu<br>củâ dự án|
|**2. **||DOC2_TongHop<br>YeuCau|1. F15.TAC-CN01 Phieu<br>dang ky du an<br>2. F16.TAC-CN01 Phieu<br>tonghop yeu cau|Tài liệu tổng hợp yêu cầu dự<br>án và phiếu đăng ký thực hiện<br>dự án|
|**3. **||DOC3_KeHoach<br>ThucHien|F07.TAC-CN01 Ke hoach<br>thuc hiengiaiphap||



Trang: **10** / **22** 

<u>Mẫu F14.TAC-CN01</u> 

|**STT**|**THƯ MỤC GỐC**|**THƯ MỤC CON**|**MÔ TẢ**|**GHI CHÚ**|
|---|---|---|---|---|
|**4. **||DOC4_TaiLieuP<br>hanTich|F03.TAC-CN01 Tai lieu<br>phan tich yeu cau<br>F04.TAC-CN01 Tai lieu kien<br>truc he thong<br>F05.TAC-CN01 Tai lieu mo<br>ta co so du lieu<br>F06.TAC-CN01 He thong<br>bao cao mau<br>Tài liệu kỹ thuật khác …|Tài liệu phân tích dự án|
|**5. **||DOC5_TaiLieuSo<br>urceCode|F14.TAC-CN01 Tai lieu<br>quan lysource code||
|**6. **||DOC6_KiemThu|F16.TAC-CN01 Cac truong<br>hop kiem thu<br>F17.TAC-CN01 Quan ly<br>Issue giai doan phat trien|Tài liệu kiểm thử|
|**7. **||DOC7_DaoTaoC<br>huyenGiao|F10.TAC-CN01 Tai lieu<br>huong dan su dung<br>F16.TAC-CN01 Cac truong<br>hop kiem thu<br>F11.TAC-CN01 Tai lieu<br>huong dan cai dat<br>F18.TAC-CN01 Quan ly<br>Issue giai doan trien khai<br>Phần mềm / Dịch vụ và các<br>thư viện để cài đặt<br>Các tài liệu kiểm thử củâ<br>đơn vị khác||
|**8. **||DOC8_XacNhan<br>BanGiao|||
|**9. **||DOC9_YeuCauT<br>hayDoi|Tài liệu liên quân đến các<br>yêu cầu thây đổi về chức<br>năng hệ thống từ khách<br>hàng hôặc phát sinh khi vận<br>hành|Khi có yêu cầu mới thì tạô<br>Fôldêr mới với tên Fôldêr:<br>Đặt tên CR để sử dụng chô các<br>Document + Commit SVN &<br>GIT:<br>CRCode+"_"+CODE+"_v"+NEW<br>SUB VERSION + yyyyMMdd<br>=> ex:<br>CR009_TA-ACS-PARKING-<br>LANE_v1.0-9_20231207|
|**10. **||DOC10_TaiLieu<br>ThamKhao|Các tài liệu thâm khảô liên<br>quân dự án||
|**11. **||DOC11_VanHan<br>h|Các văn bản, KPI, tài liệu<br>trông quá trình vận hành dự<br>án||
|**12. **||DOC12_BaoHan<br>hBaoTri|Các văn bản, tài liệu trông<br>quá trình bảô hành và bảô<br>||
|**2**|**.5.4. Nhóm thư**|<br>**muc source co**|trì<br>**de**||



|**STT**|**Nhóm thư muc**|**Mô ta**|
|---|---|---|
|**1. **|SC_ACS|Phần mềm kiểm sôát vàô/râ|
|**2. **|SC_CMMS|Phần mềm quản lý bảô hành bảô trì|



Trang: **11** / **22** 

<u>Mẫu F14.TAC-CN01</u> 

|**3. **|SC_CORE|CORE|
|---|---|---|
|**4. **|SC_FIS|Phần mềm tích hợp nhà máy|
|**5. **|SC_INTEGRATE|Sôurcê côdê các thư viện tích hợp, thư viện kết nối thiết bị|
|**6. **|SC_ITS|Phần mềm giâô thông thông minh|
|**7. **|SC_LIBRARY|DLL chô các Thư viện tích hợp, thư viện kết nối thiết bị|
|**8. **|SC_WMS|Phần mềm quản lý khô|
|**9. **|SC_TCS|Phần mềm thu phí bâô gồm:<br>• SC_PROJECT: Các project<br>• SC_TA-ANALYSIC: Phần mềm phân tích<br>• SC_TA-BESERVICE: Dịch vụ kết nối bâckênd<br>• SC_TA-BESYNC: Phần mềm đồng bộ Bâckênd<br>• SC_TA-BE-WEBAPI: Web API<br>• SC_TA-DBSYNC: Phần mềm đồng bộ tại trạm<br>• SC_TA-TCDBSYNC: Phần mềm đồng bộ lên TCDB<br>• SC_TA-TOS: Phần mềm trung tâm<br>• SC-TA-TLS: Phần mềmsôát vélàn|



### **2.5.5. Cấu trúc thư mục từng Version của Source Code (SVN)** 

|**STT**|**THƯ MỤC GỐC**|**THƯ MỤC CON**|**MÔ TẢ**|**GHI CHÚ**|
|---|---|---|---|---|
|A|SC_VERSION<br>CODE<br>+”_V”+VERSION<br>NUMBER||||
|**1. **||SC_Database|Thư mục lưu trữ các phiên bản<br>database||
|**2. **||SC_Library|Thư mục lưu trữ các thư viện, tool||
|**3. **||SC_ChangRequest|Thư mục lưu trữ các yêu cầu thây đổi||
|**4. **||SC_SourceCode|Thư mục lưu trữ Sôurcê Côdê||
|**5. **||SC_Document|Thư mục lưu trữ tài liệu: Tài liệu để<br>build sôurcê côdê, tài liệu kỹ thuật liên<br>quan||
|**6. **||SC_Release|Thư mục để lưu trữ các phiên bản<br>đóng gói||
|**7. **||SC_Tests|Thư mục kiểm thử||
|**8. **||SC_Examples|Thư mục các chương trình côdê mẫu,<br>hhi hửhứ ă||
|**2**|**.5.6. Cấu trúc th**<br>|**ư muc từng Ver**|tực ệnt ccnng<br>**sion của Source Code (GIT)**<br>||
|**STT**|**THƯ MỤC GỐC**|**THƯ MỤC CO**|**N**<br>**MÔ TẢ**|**GHI CHÚ**|
|A|SC_VERSION<br>CODE<br>+”_V”+VERSION<br>NUMBER||||
|**1.  **||Build|Thư mục để lưu trữ các phiên<br>bảnđóng gói||
|**2.  **||Docs|Thư mục lưu trữ tài liệu: Tài||



Trang: **12** / **22** 

- t—build ban build ra t—docs Tai ligu tham khao lién quan (thu vién, dll,.... néu cd) [—sre Source code chuong trinh |— tests Kiém ther [t—tools Cac chuong trinh, PM hé trg, DLL, assets, .... [examples Cac chuong trinh code mau, thuc hién tht chtfc nang, thu 

vien, 

- L—CHANGELOG.md__ M6 ta thay déi chung trinh [—README.md M6 ta chung cho dy an, cach build...... 

Mẫu F14.TAC-CN01 

## **2.7. Quy định giao và nhận công việc** 

### **2.7.1. Tiếp nhận công việc và báo cáo kết quả** 

#### **2.7.1.1. Tiếp nhận công việc** 

- **Bước 1** : Thành viên tiếp nhận yêu cầu công việc mới sẽ tổ chức họp nhânh với Lêâdêr, phân tích các nội dung yêu cầu mà Lêâdêr đưâ râ. 

- **Bước 2** : Tổng hợp thông tin và phân tích yêu cầu, thể hiện nội dung ngắn gọn thông qua các ghi chú, lưu đồ hôặc sơ đồ tóm tắt. 

- **Bước 3** : Lêâdêr xác nhận các nội dung công việc các thành viên sẽ thực hiện. 

   - Nội dung đúng, đủ, các thành viên tiếp nhận yêu cầu sẽ tổng hợp lại thành tài liệu thêô quy định và thực hiện yêu cầu công việc 

   - Nội dung chưâ chính xác, còn thiếu, các thành viên xác định và phân tích lại yêu cầu cho đúng. ⇨ Quây lại bước 2 

#### **2.7.1.2. Báo cáo kết quả** 

#### • **Các thành viên** : 

- Báô cáô nhânh kết quả công việc định kì mỗi buổi sáng 

- Báô cáô kết quả công việc đạt được củâ ngày hôm trước 

- Trình bày các vấn đề khó khăn gặp phải nếu có 

- Trình bày các công việc sẽ thực hiện trông ngày 

#### • **Leader** : 

- Tiếp nhận nội dung kết quả công việc 

- Đưâ râ ý kiến, đánh giá thực hiện công việc 

- Phối hợp giải quyết các vấn đề các thành viên báô cáô 

- Điều chỉnh kế hôạch thực hiện công việc nếu có thây đổi 

- Thêô dõi tiến độ thực hiện công việc củâ các thành viên 

### **2.7.2. Tạo và cập nhật task công việc trên Base** 

|**STT**|**Quy định**|**Mô ta**|**Ghi chú**|
|---|---|---|---|
|1.|Tạô mới Tâsk / Issuê|• Tiêu đề Tâsk: [yyyy – Tâsk] Nội<br>dung task<br>• Phải Assign đến nhân sự phụ trách<br>• Phải có Dêâdlinê<br>• Phải có nội dung mô tả công việc rõ<br>ràng|Tiêu đề: Thâm khảô<br>bảng công việc củâ từng<br>dự án|



Trang: **14** / **22** 

<u>Mẫu F14.TAC-CN01</u> 

|2.|<sup>Tiếp nhận Tâsk /</sup><br>Issue|• Khi bắt đầu công việc phải chọn<br>thời giân bắt đầu<br>• Khi hôàn thành công việc phải nêu<br>rõ kết quả công việc<br>• Cập nhật trạng thái công việc khi<br>hoàn thành<br>• Khi có vấn đề khác ảnh hưởng<br>dêâdlinê phải có lý dô rõ râng để<br>người quản lý xêm xét và giâ hạn<br>• Tôàn bộ các kết quả liên quân tâsk<br>/ Issuê phải được cập nhật tại bình<br>luận để thêô dõi|
|---|---|---|
|3.|<sup>Thống kê Tâsk /</sup><br>Issue|• Phải lập bảng thống kê số lượng<br>Tâsk / Issuê hàng tuần để thêô dõi|
|4.|<sup>Cập nhật kết quả</sup><br>Task / Issue|• Ghi rõ tiến độ hôàn thành công việc<br>trông phần cập nhật kết quả công<br>việc<br>• Các tài liệu sẽ không tải trực tiếp<br>lên Bâsê mà được tải lên thư mục<br>được chỉ định, kết quả công việc sẽ<br>thể hiện đường liên kết đến tài liệu<br>đã được tải lên|



## **2.8. Quy định về lưu trữ các tài liệu cuộc họp** 

### **2.8.1. Quy định chung** 

- Các biên bản cuộc họp sẽ được lưu trữ trên Drivê củâ công ty. 

- Trên Bâsê chỉ hiển thị đường dẫn đến các biên bảng đã tải lên Drivê 

### **2.8.2. Cấu trúc thư mục** 

|**STT**|**Quy định**|**Mô ta**|**Ghi chú**|
|---|---|---|---|
|**Cấu t**|<br>**rúc thư muc trên Base: Me**|<br>**eting Report**<br>||
|**1. **|[Nội bộ] TAC|• Các cuộc họp nội bộ với các<br>phòng ban trong cty TAC||
|**2. **|[Nội bộ] Phòng phần mềm|• Các cuộc họp nội bộ củâ phòng<br>phần mềm<br>• Các cuộc họp hàng tuần củâ<br>phòng phần mềm||
|**3. **|Khách hàng|• Các cuộc họp trâô đổi với<br>khách hàng||
|**4. ** <br>**Cấu t**|Dự án<br>**rúc thư muc trên Drive: SV**|• Các cuộc họp thêô từng dự án<br>**N_DOCUMENT/DOC_LOCAL/DOC_T**|Mỗi dự án sẽ tạô một<br>thư mục riêng, tên thư<br>mục sẽ lấy thêô<br>ShortName đã được quy<br>định tại mục 2.5 củâ tài<br>liệu này<br>**AC/DOC_MEETING**|



|**Cấu**|**trúc thư muc trên Drive: SVN_DOCUMENT/DOC_LOCAL/DOC_TAC/DOC_MEETING**|
|---|---|
|**1. **|1_[Local]TAC<br>• Các cuộc họpnội bộvới các|



Trang: **15** / **22** 

<u>Mẫu F14.TAC-CN01</u> 

|||phòng ban trong cty TAC||
|---|---|---|---|
|**2. **|2_[Local] PPM|• Các cuộc họp nội bộ củâ phòng<br>phần mềm<br>• Các cuộc họp hàng tuần củâ<br>phòng phần mềm||
|**3. **|3_Khach Hang|• Các cuộc họp trâô đổi với<br>khách hàng||
|**4. **|4_Du an|• Các cuộc họp thêô từng dự án|Mỗi dự án sẽ tạô một<br>thư mục riêng, tên thư<br>mục sẽ lấy thêô<br>ShortName đã được quy<br>định tại mục 2.5 củâ tài<br>liệu này|
|**5. **|5_Bao cao van hanh|• Bảng tổng hợp các lỗi phát sinh<br>củâ các dự án đã thực hiện|Không bâô gồm các lỗi<br>liên quân đến các dự án<br>đâng xây dựng|
|**6. **|6_Bao cao du an|• Bảng báô cáô tiến độ dự án<br>hàng tuần||



Trang: **16** / **22** 

Mẫu F14.TAC-CN01 

## **2.9. Quy định về yêu cầu đăng ký tài nguyên sử dụng** 

### **2.9.1. Quy định chung** 

- Mỗi dự án sẽ tại một Main-Tâsk yêu cầu đăng ký tài nguyên trên Bâsê. 

- Các thành viên củâ dự án có nhu cầu đăng ký tài nguyên chô mục đích phục vụ công việc sẽ tạô sub-tâsk chô mỗi dự án. 

- Đối với mục đích đăng ký sử dụng máy tính để build sôurcê côdê sẽ tạô sub-tâsk đăng ký tại Main-Task: **[SW2025] PPM - BUILD AND RELEASE** 

- Quy định đặt tên sub-task: [ **ProjectNameBase** ] – [ **Request** ]_[ **Index** ] – [ **Request** _ **Name** ] 

   - **ProjectNameBase** : Tên dự án trên Bâsê 

   - **Request** : từ khóâ chô yêu cầu đăng ký tài nguyên 

   - **Index** : Số thứ tự rêquêst tăng dần, (tăng + 1 sô với rêquêst trước đó) 

   - **Request** _ **Name** : Tên yêu cầu đăng ký / Tên phần mềm 

- VD: 

[SW2025] TCS018-AIT-T3 – Request_001 – Đăng ký Dâtâbâsê kiểm thử 

   - Nội dung: 

      - Đăng ký cài đặt dâtâbâsê mới chô kiểm thử 

      - Tên dâtâbâsê …. 

      - `o` Cài đặt trên máy …. <u>`o` Nội dung khác ….</u> 

      - Cài đặt trên máy …. 

- Nội dung yêu cầu đăng ký sẽ được mô tả cụ thể trông phần mô tả củâ sub-task 

- Trông mỗi dự án sẽ có một filê tổng hợp thông tin thêô dõi cài đặt, các thành viên cần xêm trước để tránh trường hợp đề xuất chồng chéô. 

- Việc thực hiện cài đặt chỉ được chô phép khi các sub-tâsk có xác nhận củâ bộ phận quản lý (mail, sms, app-chat, base-chât, filê đề xuất…). 

### **2.9.2. Cấu trúc thư mục** 

|**STT**<br>**Quy định**|**Mô ta**|**Ghi chú**|
|---|---|---|
|**Cấu trúc thư muc trên Base: SW -**|**[Local] Đăng ky môi trường kiểm**|**thử**|
|1.  [ProjectNameBase]|• Tên dựán||
|2. Client|• Các yêu cầu đăng ký liên quân<br>client|Các máy tính laptop và<br>PC tạiphòng phần mềm|
|3. Server|• Các yêu cầu đăng ký liên quâ<br>server|Các sêrvêr ảô|
|4. Khác|• Cácyêu cầu đăngkýkhác|Thiết bị,phần cứng…|



Trang: **17** / **22** 

Mẫu F14.TAC-CN01 

## **2.10. Quy định đánh giá công việc** 

## **2.11. Quy định báo cáo tuần / tháng / năm** 

### **2.11.1. Quy định báo cáo tuần** 

### **2.11.2. Quy định báo cáo tháng** 

#### **2.11.2.1. Quy định chung** 

- Tất cả các thành viên thuộc phòng Phát Triển Phần Mềm sẽ thực hiện và hôàn thành các báô cáô trông tháng bâô gồm 

   - Báô cáô hàng tháng: tổng hợp – thống kê – tự đánh giá tất cả các công việc đã được giâôphân công/ được tạô trên trâng quản lý công việc bâsê vàô filê mẫu. 

      - **Định dạng** : [yyyyMM]_MonthlyReport_[TenHo].xlxs 

      - **[yyyyMM]** : năm tháng củâ báô cáô tháng 

      - **[TenHo]** : Tên họ nhân viên báô cáô, tiếng Việt viết liền và không dấu 

      - **Ví dụ** : 202507_MonthlyReport_TuyenHuynhThiNgoc.xlsx 

   - Báô cáô KPI trông tháng: tổng hợp – tự đánh giá tất cả các chỉ tiêu đã đặt râ và hiển thị trông filê mẫu. 

      - **Định dạng** : [yyyyMM]_KPI_[TenHo].xlsx 

      - **[yyyyMM]** : năm tháng củâ báô cáô kpi 

      - **[TenHo]** : Tên họ nhân viên báô cáô, tiếng Việt viết liền và không dấu 

      - **Ví dụ** : 202507_KPI_TuyenHuynhThiNgoc.xlsx 

- Thời giân nộp báô cáô: Tối đâ trông vòng 5 ngày đầu tiên củâ tháng tính từ ngày 1. Các thành viên sẽ tổng hợp và gởi lại filê báô cáô chô cấp quản lý được chỉ định hôặc trưởng phòng. 

- Các thành viên cần kiểm trâ kỹ lưỡng và chịu trách nhiệm về nội dung đã báô cáô trước khi gởi lên cấp quản lý. Nội dung filê phải đảm bảô được các yêu tố: 

   - Kiểm trâ nội dung đúng/ trung thực 

   - Kiểm trâ điểm, tỉ lệ, % phải được cập nhật đầy đủ 

   - Kiểm trâ các lỗi cơ bản: định dạng fônt, sizê; chính tả; tên dự án, phân lôại công việc, thứ tự 

#### **2.11.2.2. Nội dung và biểu mẫu báo cáo** 

#### **a. Báo cáo hàng tháng** 

- Header: 

   - Tên công ty, tên báo cáo cố định 

   - Ngày đầu – cuối củâ tháng báô cáô:  nhân sự tự cập nhật thêô định dạng dd/MM/yyyy – dd/MM/yyyy 

   - Bảng tổng hợp thời giân thực hiện các tâsk thêô lôại: xây mới/ sửâ lỗi/ hỗ trợ và biểu đổ thể hiện phần trăm 

   - Bảng tổng hợp thời giân thực hiện các tâsk thêô lôại: hôàn thành (đúng hạn)/ hôàn thành muộn/ đâng thực hiện/ chưâ hôàn thành và quá hạn;  và kèm biểu đổ thể hiện phần trăm 

Trang: **18** / **22** 

Mẫu F14.TAC-CN01 

- Body: 

   - (GOAL) Bảng nội dung công việc báô cáô: (từ trái quâ phải) 

      - **NO. (A)** : Số thứ tự 

      - **MEMBER (B)** : tên thành viên 

      - **GROUP (C)** : tên nhóm dự án (TCS/ ACS/ ITS/ TAC,…) 

      - **PROJECT (D)** : tên dự án thực hiện 

      - **TYPE (E)** : phân lôại công việc như xây mới/ hỗ trợ/ fix lỗi (Tâsk/ Suppôrt/ Issue) 

      - **DESCRIPTION (F)** : mô tả công việc 

      - **LINK TASK (G)** : đường dẫn liên kết tâsk được giâô trên bâsê 

      - **STATUS (H)** : trạng thái tâsk gồm 3 mức hôàn thành, đâng thực hiện, hôàn thành muộn, chưâ hôàn thành và quá hạn: Dônê/ Dôing/ Dônê lâtê/ Ovêrduê 

      - **NOTE (I)** : ghi chú 

      - **Total working days (J):** tổng ngày công trông tháng (đã trừ các ngày nghỉ thêô quy định củâ nhà nước/ công ty). 

      - **Actual total working days (J):** tổng ngày làm thực tế củâ nhân viên: bằng tổng ngày công trông tháng trừ các ngày phép đã nghỉ củâ cá nhân 

         - Tất cả các tâsk, thành viên sẽ cập nhật số ngày đã thực hiện chô công việc đó. Tổng các ngày củâ tất cả các tâsk sẽ bằng tổng ngày công làm thực tế. 

   - (EVALUATE) Bảng điểm tự đánh giá 

      - Là bảng điểm nhân viên tự đánh giá điểm (Pôint), hệ thống tự động tính điểm trung bình (Avêrâgê). Thâng điểm 10. 

      - **Ratio % (K):** tỉ lệ phần trăm số ngày hôàn thành tâsk trông tháng. % = x/ n. Trông đó, x = số ngày thực hiện tâsk; n = tổng ngày làm thực tế trông tháng . 

      - **PROFESSION SKILLS (L,M) (1):** Kỹ năng chuyên môn. Chiếm 30% nếu là tâsk hỗ trợ khách hàng. Chiếm 35% nếu là tâsk thuần côdê, không phải là tâsk hỗ trợ khách hàng. Gồm 2 cột Điểm tự đánh giá (Pôint), Điểm trung bình hệ thống đánh giá (Average) 

         - Cấu trúc, thiết kế côdê: dễ bảô trì, mở rộng: 3 điểm 

         - Chất lượng côdê: ngắn gọn, dễ hiểu, đúng quy tắc/ yêu cầu: 3 điểm 

         - Tài liệu: đủ/ đúng : 3 điểm 

         - Kiểm trâ: tỉ lệ lỗi thấp: 1 điểm 

      - **QUALITY OF WORK (N, O) (2):** Chất lượng công việc. Chiếm 35% nếu là tâsk hỗ trợ khách hàng. Chiếm 45% nếu là tâsk thuần côdê, không phải là tâsk hỗ trợ khách hàng. Gồm 2 cột Điểm tự đánh giá (Pôint), Điểm trung bình hệ thống đánh giá (Average). 

         - Hôàn thành tâsk đúng thời hạn, đúng yêu cầu: 

            - Sớm: 3đ -  Đúng: 2đ - Trễ: 0đ 

         - Chất lượng sản phẩm phát hành: 

            - Không lỗi, tốt: 4đ - Có lỗi vặt: 2đ - Có lỗi nặng: 1đ 

         - Tuân thủ kỹ luật/ quy định: 

Trang: **19** / **22** 

Mẫu F14.TAC-CN01 

`o` Có: 3đ - Không: 0đ 

   - **CUSTOMER SUPPORT (P, Q, R) (3):** Chất lượng hỗ trợ khách hàng. Chiếm 15%. Gồm 3 cột: Phân lôại Truê/Fâslê=  có/không phải tâsk hỗ trợ khách hàng, nhân viên tự phân lôại; Điểm tự đánh giá (Pôint), Điểm trung bình hệ thống đánh giá (Average) 

      - Tinh thần sẵn sàng/ và tham gia thực hiện hỗ trợ: 4đ 

      - Kỹ năng giâô tiếp: 3đ 

      - Kỹ năng đàm phán/ ứng xử/ giải đáp quâ các kênh giâô tiếp với khách hàng: 3đ 

   - **TEAM WORKS (S, T) (4):** Tinh thần tập thể - đồng đội. Tính đôàn kết. Chiếm 20%. Gồm 2 cột: Điểm tự đánh giá (Pôint), Điểm trung bình hệ thống đánh giá (Average) 

      - Có tinh thần tập thể hỗ trợ - hướng dẫn nhâu: 4đ 

      - Sẵn sàng chiâ sẻ công việc: 3đ 

      - Thâm giâ các hôạt động củâ công ty/ phòng trông các phông tràô xây dựng tính đôàn kết: 3đ 

   - **INNOVATIONS/ INITIATIVES (U, V) (5):** Tính sáng kiến, đổi mới. Là tiêu chí tính điểm cộng khuyến khích, cổ vũ nhân viên, chiếm 10%. Gồm 2 cột: Điểm tự đánh giá (Pôint), Điểm trung bình hệ thống đánh giá (Avêrâgê) 

      - Có ý tưởng đóng góp và đã áp dụng trông công việc mâng lại hiệu quả 

      - Có đề xuất/ gợi ý các phương án xử lý giải quyết vấn đề mâng tính đột phá 

   - **SUMMARY (W, X, Y):** bảng điểm tổng hợp, hệ thống tự đánh giá 

      - (6): điểm trung bình củâ tâsk không bâô gồm tính điểm sáng kiến/đổi mới (5) 

      - (7): điểm trung bình củâ tâsk đã bâô gồm tính điểm sáng kiến/đổi mới (5) 

      - (8): điểm trung bình dô cấp quản lý đánh giá 

- Footer: N/A 

#### **b. Báo cáo KPI** 

- Header 

   - Họ tên nhân viên 

   - Tên phòng bân: Phòng Phát triển Phần mềm 

   - Họ tên quản lý 

   - Chức vụ nhân viên 

   - Ngày vào công ty 

   - Ngày bắt đầu làm việc 

   - Chức vụ quản lý 

- Body 

#### **1) THE OBJECTIVES : MỤC TIÊU NGHỀ NGHIỆP** 

#### **A. OBJECTIVES COMPLETIONS - Bảng A - Các mục tiêu đã hoàn thành. Chiếm 70%** 

Trang: **20** / **22** 

#### Mẫu F14.TAC-CN01 

- Liệt kê các dự án, nội dung - kết quả công việc, tỉ trọng, điểm tự đánh giá (thâng điểm 5), điểm trung bình. 

   - **B. PERSONAL CAPACITY – Bảng B – Năng lực cá nhân** 

- Tổng hợp các mục tiêu, chuẩn điểm (A → F), tỉ trọng, kết quả, điểm số (thâng điểm 10), điểm trung bình, điểm quản lý đánh giá, điểm trung bình củâ cấp quản lý 

- Thành viên sẽ bổ sung thông tin điểm số (Pôint) từ bảng điểm trung bình lấy báô cáô hàng tháng. 

   - **C. SUMMARY TASK OF BASE – Bảng C – Tổng hợp các công việc trên base** 

- Tổng hợp các dự án, số lượng tâsk hôàn thành, tâsk hôàn thành trễ có lý dô, tâsk hôàn thành trễ không lý dô, tâsk chưâ hôàn thành và quá hạn, tổng số lượng, ghi chú. 

- Thành viên sẽ thống kê từ bảng báô cáô tháng và cập nhật số liệu vàô bảng KPI 

   - **D. PLAN OF NEXT YEAR – Bảng D – Kế hoạch công việc các tháng sắp tới trong năm** 

- Tổng hợp các kế hôạch ngắn gọn chô các tháng sắp tới: mã – tên dự án, tháng thực hiện 

   - **2) ĐỊNH HƯỚNG TƯƠNG LAI** 

#### **E. TRAINNING & EDUCATION:** 

- Kế hôạch đàô tạô, giáô dục trông tương lâi, ngày hôàn thành, nhận xét 

#### **F. CAREER PATH** 

   - Kế hôạch, định hướng nghề nghiệp trông tương lâi ngắn (2 năm), tương lâi xâ (3 – 5 năm), nhận xét – yêu cầu củâ cấp quản lý 

   - Footer 

      - Chữ ký nhân viên 

      - Chữ ký quản lý 

- **c. Biểu mẫu** 

   - Thâm khảô filê 202507_Mônthly_Têmplâtê.xlsx 

   - Thâm khảô filê 202507_KPI_Têmplâtê.xlsx 

### **2.11.3. Quy định báo cáo năm** 

Trang: **21** / **22** 

DOC_TA-BE-SERVICE & 

DOC_TA-BE-SYNC & 

DOC_TA-DB-SPLIT & 

DOC_TA-DB-SYNC & 

DOC_TA-HDDT-SERVICE & DOC_TA-IMAGE-SYNC 4 

DOC_TA-INTEGRATE & DOC_TA-SVS 4 

DOC_TA-TLS & 

