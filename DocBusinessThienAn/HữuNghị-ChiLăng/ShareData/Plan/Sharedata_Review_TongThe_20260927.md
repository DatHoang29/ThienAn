# Báo cáo rà soát tổng thể phân hệ ShareData — 27/09/2026

> **Phạm vi:** luồng gửi nối đuôi, phát hiện dữ liệu mới tức thì, ghi nhận nhật ký, trạng thái worker giám sát.
> **Nguồn:** biên bản họp [`21-09-2026`](../doc/transcript/21-09-2026-hoan-thien-mapping-va-gui-noi-duoi-sharedata.md)
> (đã xác thực) · đọc trực tiếp mã nguồn `TA-ITS015-WEBAPI-V1.0/src/`.
> **Rà lại lần 2 cuối ngày 27/09** sau đợt refactor worker giám sát.

### Chú giải ký hiệu

| Ký hiệu | Ý nghĩa |
| --- | --- |
| ✅ | Đạt — khớp / đúng / đã hoàn tất |
| ⚠️ | Cần lưu ý — có vấn đề, nhưng chưa phải dừng lại để sửa; ghi nhận rồi xử lý sau |
| ❌ | Không đạt — không khớp / không đúng / không áp dụng được |
| 🔴 | Rủi ro nghiêm trọng — phải xử lý trước khi đi tiếp |
| ⛔ | Cấm tuyệt đối |
| 📌 | Ghi chú bối cảnh — nguồn dữ liệu, ngày đo, thuật ngữ |

> Ký hiệu chỉ nói **mức độ**; trục đánh giá do tiêu đề cột của từng bảng nói rõ.

---

## Kết luận — đọc 30 giây là đủ

✅ **Ba việc bạn chỉ định đều đã làm đúng:**

| Việc | Kết quả đối chiếu code |
| --- | --- |
| Chốt độ dài `LastKey` | ✅ Chốt nằm trong `ExportPage`, chặn **trước khi gửi HTTP**, ghi cảnh báo `ESH-1305`. Giữ độ dài 64 theo chuẩn, ⛔ không nới cột |
| Mốc dấu vết theo đồng hồ CSDL | ✅ `GetDbNow(db)` là nơi duy nhất đọc đồng hồ CSDL, gọi ở **4 điểm**: 2 dạng quét đăng ký, `CommitSuccess`, `ReleaseLock` |
| Không gửi bản ghi xoá mềm | ✅ Đã hoàn nguyên **sạch**: `QueryPacket107` giữ `IsDelete IS NULL`, `incidentState` trả về `State` gốc. ⛔ Không còn mảnh vụn nào (mã trạng thái `Deleted` và bài test tương ứng đều đã gỡ) |

✅ **Ba việc siết thêm cuối ngày 27/09 cũng đã xong:**

| Việc | Kết quả |
| --- | --- |
| Cô lập bảng mất Change Tracking giữa lúc chạy | ✅ Loại đúng bảng hỏng, **giữ tín hiệu cho các bảng lành**, ghi đúng **một** dòng `ESH-1602`, tự thử bật lại theo mốc hẹn **riêng từng bảng** |
| Khe hở chống dội | ✅ `DebounceSec` nay kiểm **trong** lệnh chiếm quyền nguyên tử ⇒ khe hở **đóng hẳn**, ⛔ không còn ảnh chụp bộ nhớ |
| Nâng mốc version giữa nhiều instance | ✅ Lệnh cập nhật có điều kiện — chỉ một bên thắng quyền phát tín hiệu, bên thua rút lui im lặng |

---

## Tóm tắt các trục

| Câu hỏi | Trả lời | Xem |
| --- | --- | --- |
| Đặc tả yêu cầu gì? | Bốn việc: **chặn gửi khi thiếu hồ sơ ánh xạ** · **gửi nối đuôi** theo mốc `LastTime`/`LastKey` · **phát hiện dữ liệu mới tức thì** (không hoãn) · **trường không map để `null`** | [§1](#1-đặc-tả-yêu-cầu-gì) |
| Code làm được tới đâu? | **Đủ cả bốn**, cộng sáu việc siết thêm trong ngày 27/09 | [§2](#2-code-làm-được-tới-đâu) |
| Chưa làm những gì? | **API danh mục trạng thái ánh xạ gói tin theo đối tác** (chưa làm) | [§3](#3-chưa-làm-những-gì) |
| Edge case? | 12 tình huống — **cả 12 đều có lưới chặn** | [§4](#4-edge-case) |

---

## 1. Đặc tả yêu cầu gì

Trích mục *Quyết định kỹ thuật cốt lõi* của biên bản **21/09/2026** (Anh Sơn — Tech Lead, Đạt — Backend
WebAPI/CSDL, Hiếu — Backend Worker). Biên bản đã xác thực, là nguồn sự thật nghiệp vụ.

**1.1. Chặn gửi khi thiếu hồ sơ ánh xạ** — *"Tuyệt đối không cho phép gửi dữ liệu nếu gói tin chưa có cấu
hình ánh xạ hợp lệ."* Hồ sơ ánh xạ gắn bộ ba `[Đối tác] + [Gói tin] + [Chiều In/Out]`, mã tự sinh.

**1.2. Gửi nối đuôi** — *"Lần gửi trước đã xuất dữ liệu đến mốc nào thì lần sau chỉ lấy các bản ghi phát
sinh tiếp sau mốc đó để gửi tiếp."* Bảng lưu mốc tối thiểu `PartnerCode`, `PacketCode`, `LastTime`,
`LastKey`; xử lý trùng mốc bằng khoá phức hợp:

```
WHERE (UpdateTime > LastTime) OR (UpdateTime = LastTime AND Id > LastId)
```

**1.3. Phát hiện dữ liệu mới tức thì — không hoãn.** Chốt cuối: làm ngay trong tuần bằng SQL Server
Change Tracking + NATS, ô chọn `SendOnNewData` bật thật trên giao diện.

**1.4. Kiểu dữ liệu** — *"Trường dữ liệu nào không map thì để trống hoặc giá trị `null` an toàn, không cố
ép format gây lỗi."*

---

## 2. Code làm được tới đâu

| Hạng mục | Khớp đặc tả? | Căn cứ |
| --- | --- | --- |
| Chặn gửi khi thiếu ánh xạ (1.1) | ✅ | Phân giải ra `null` là huỷ kết xuất, không phát gói nào |
| Bảng lưu mốc đúng 4 trường (1.2) | ✅ | `ShareDataLastSend` + index độc nhất `(PartnerCode, PacketCode)` |
| Cursor kép chỉ tiến, không tụt (1.2) | ✅ | Điều kiện đơn điệu nằm ở mệnh đề `WHERE` của lệnh cập nhật mốc |
| Nguyên tử khi tịnh tiến mốc (1.2) | ✅ | Một giao dịch: cập nhật đăng ký có kiểm tra độc quyền → ghi mốc → xác nhận. Mất quyền thì hoàn tác, trang sau không gửi |
| Phân trang có phanh (1.2) | ✅ | 100 dòng/trang, tối đa 20 trang/lượt, 50% thời lượng giữ quyền; có rào chắn cursor `null` |
| Change Tracking + NATS (1.3) | ✅ | Worker quét 1 giây/lần, phát vào **một** subject chung, gói tin nhận diện qua mã trong nội dung bản tin |
| Chỉ bắt Thêm mới và Sửa, bỏ Xoá (1.3) | ✅ | Đúng chủ đích — Change Tracking chỉ làm **tín hiệu kích hoạt**, dữ liệu thật vẫn lấy từ bảng nguồn |
| Trường không map để `null` (1.4) | ✅ | Gói 106: 4 trường tải trọng không nằm trong câu truy vấn nên tự trả `null` |
| **Chốt độ dài `LastKey`** | ✅ | Chặn trước khi gửi, cảnh báo `ESH-1305` |
| **Mốc dấu vết theo đồng hồ CSDL** | ✅ | `GetDbNow(db)`, 4 điểm gọi |
| **Không gửi bản ghi xoá mềm** | ✅ | Giữ bộ lọc ở cả 5 gói nối đuôi, hoàn nguyên sạch |
| **Chống dội ở tầng nguyên tử** | ✅ | `DebounceSec` là một vế trong chính lệnh chiếm quyền, ⛔ không kiểm trước bằng ảnh chụp bộ nhớ. Chỉ luồng theo sự kiện áp vế này; luồng quét định kỳ giữ nguyên câu lệnh |
| **Cô lập bảng mất Change Tracking** | ✅ | Loại đúng bảng hỏng khỏi câu SQL đang áp dụng rồi **thử lại ngay** với bảng lành ⇒ tín hiệu tức thì vẫn chạy cho phần còn tốt. Mốc hẹn thử lại tính **riêng từng bảng** |
| **Nâng mốc version nguyên tử** | ✅ | Nhiều instance cùng thấy version tăng thì chỉ một bên nâng được mốc và thắng quyền phát tín hiệu |

---

## 3. Chưa làm những gì

- **API danh mục trả về trạng thái ánh xạ của từng gói tin theo đối tác**: Chưa làm.

---

## 4. Edge case

| # | Tình huống | Có lưới chặn? | Cách xử và cái giá phải trả |
| --- | --- | --- | --- |
| 1 | Sập giữa hai trang khi gửi nối đuôi | ✅ | Trang đã ghi nhận giữ nguyên, trang dở hoàn tác. Giá: lượt sau làm lại trang dở |
| 2 | Sập sau khi gửi xong nhưng trước khi tịnh tiến mốc | ✅ | Lượt sau gửi lại. Giá: **đối tác nhận trùng** — bên nhận tự chống trùng |
| 3 | Tiến trình bị kết thúc đột ngột khi đang giữ quyền xử lý | ✅ | Quyền tự hết hạn, worker sau nhận lại. Giá: chậm tối đa bằng thời hạn giữ quyền |
| 4 | Worker giám sát khởi động lại | ✅ | Mốc nằm ở bảng `ShareDataTrackVersion` (**đúng 1 dòng toàn hệ thống**) nên tiếp tục từ mốc cũ. Giá: chu kỳ đầu sau khi dừng lâu có thể bắn nhiều tín hiệu; khoảng chống dội của từng đăng ký vẫn tiết chế |
| 5 | NATS mất kết nối | ✅ | Ghi cảnh báo rồi thôi, quét định kỳ gửi bù. Giá: mất tính tức thì tới khi nối lại |
| 6 | Mất kết nối CSDL giữa chừng | ✅ | Giao dịch tự hoàn tác, quyền xử lý treo giống #3 rồi tự hồi phục |
| 7 | Sập khi đang gửi gói bản chụp | ✅ | Không mất gì — chu kỳ sau gửi lại toàn bộ |
| 8 | Mốc version rơi ngoài cửa sổ hợp lệ (giữ 1 ngày) | ✅ | Tự nhảy cóc lên mốc hiện tại, ghi `ESH-1601` kèm khoảng bị bỏ qua. Giá: **quãng đó chỉ được quét định kỳ gửi bù**, không có tín hiệu tức thì |
| 9 | Tác vụ trước chạy lâu, bản tin tín hiệu sau dồn ứ | ✅ **siết lại 27/09** | Khoá độc quyền bỏ qua êm nếu đăng ký đang bận. Khoảng chống dội nay là **một vế trong chính lệnh chiếm quyền**, nên không còn khe hở đua nhau: trước đây nó tính trên ảnh chụp bộ nhớ của mốc gửi gần nhất, worker khác vừa kết xuất xong thì lượt này không thấy và lọt qua |
| 10 | Lần đầu khởi động sau triển khai, chưa bật Change Tracking | ✅ | Worker tự kiểm tra và tự bật; không bật được thì tạm dừng quét, ghi `ESH-1603`. Giá: nên chọn giờ thấp điểm cho lần đầu |
| 11 | **Bảng nguồn bị tắt hoặc xoá Change Tracking giữa lúc đang chạy** | ✅ **siết lại 27/09** | Worker nhận diện đúng bảng hỏng từ bảng hệ thống, loại nó khỏi câu SQL đang áp dụng, rồi **thử lại ngay** với các bảng lành ⇒ chu kỳ đó vẫn hoàn tất, tín hiệu tức thì **vẫn chạy cho phần còn tốt**. Ghi đúng **một** dòng `ESH-1602` cho mỗi lần phát hiện; chu kỳ sau bảng hỏng không còn trong câu SQL nên ⛔ không dội lỗi. Đến hạn thì tự thử bật lại. Giá: bảng hỏng mất tín hiệu tức thì cho tới khi bật lại được — luồng quét định kỳ vẫn gửi bù. 📌 Mốc hẹn thử lại tính **riêng từng bảng**, vì mốc dùng chung sẽ bị bảng hỏng muộn đẩy lùi |
| 12 | Bản ghi nguồn bị **xoá mềm** | ✅ *(theo quyết định)* | Bộ lọc `IsDelete IS NULL` giữ nguyên ⇒ ⛔ không gửi bản ghi đã xoá. Giá: **đối tác giữ bản ghi đã xoá** — đã chấp nhận, xem [Phụ lục B](#phụ-lục-b-quyết-định-đã-chốt-và-phương-án-bị-bác) |

---

# Phụ lục A. Cơ chế hoạt động (tham khảo)

**A.1. Luồng gửi nối đuôi.** Quét đăng ký đến hạn → giành quyền xử lý độc quyền bằng một lệnh cập nhật
nguyên tử → phân giải gói tin và hồ sơ ánh xạ (thiếu ánh xạ là huỷ ngay) → với nhóm nối đuôi thì đọc mốc
đã gửi → vòng lặp trang: trích xuất, ánh xạ, ghi tệp và gửi, ghi nhận trong một giao dịch, tịnh tiến mốc
→ cuối phiên nhả quyền trong khối `finally`.

**A.2. Luồng phát hiện dữ liệu mới.** Worker quét 1 giây/lần, so mốc version của CSDL với mốc đã xử lý.
Version không tăng thì thoát ngay, ⛔ không tốn thêm truy vấn nào. Có tăng thì truy vấn danh sách bảng đổi
(gộp toàn bộ bảng nguồn trong **một** câu `UNION ALL`, chỉ lọc Thêm mới và Sửa), ánh xạ ra danh sách mã
gói, phát vào một subject chung.

📌 **Danh sách bảng nguồn nạp động từ cấu hình** `appsettings.json` khoá `ShareDataTracker:TablePacketMap`.
Câu SQL truy vấn thay đổi được sinh **một lần lúc khởi tạo** worker từ danh sách đó. Khi một bảng mất Change Tracking
giữa lúc chạy, worker giữ thêm một câu SQL **đang áp dụng** chỉ gồm các bảng lành để tiếp tục phục vụ phần còn tốt (xem A.4).

**A.3. Ghi nhận nhật ký sự cố.**

| Tầng | Dành cho |
| --- | --- |
| `ShareDataActivityLog` | Nhật ký truyền nhận nghiệp vụ. **Kiêm** sự cố hạ tầng Change Tracking, phân biệt bằng nhóm mã `ESH-16xx` ở cột `Remark`, tự dọn sau 7 ngày |
| `ILogger` | Tranh chấp tài nguyên bình thường của khoá độc quyền. ⛔ Không lưu CSDL làm rác cảnh báo |

Ba mã hạ tầng: `ESH-1601` mốc version không hợp lệ đã tự nhảy cóc · `ESH-1602` truy vấn lỗi vì lý do khác
version · `ESH-1603` chưa bật Change Tracking, tạm dừng quét.

**A.4. Trạng thái worker giám sát.** Bảng `ShareDataTrackVersion` chỉ còn **một cột nghiệp vụ duy nhất
`LastVersion`**, và **đúng một dòng cho toàn hệ thống** — ⛔ không còn khoá theo máy hay tiến trình. Nhiều
instance cùng thấy version tăng thì chỉ **một** bên nâng được mốc bằng lệnh cập nhật có điều kiện và thắng
quyền phát tín hiệu; bên thua rút lui im lặng.

Hai trạng thái còn lại nằm ở **RAM có chủ đích**, ⛔ không thêm cột CSDL: danh sách **bảng đang bị cô lập
kèm mốc hẹn thử lại riêng từng bảng**, và **câu SQL đang áp dụng** (chỉ gồm bảng lành). Lý do: cả hai suy
ra được từ bảng hệ thống của SQL Server bất cứ lúc nào, khởi động lại thì chu kỳ đầu tự phát hiện lại ⇒
⛔ không có gì cần bảo toàn; lưu xuống CSDL chỉ là nhân bản dữ liệu và **mất** thông tin *máy nào* phát hiện.

**A.5. Đồng nhất hệ quy chiếu thời gian.** Mọi mốc ghi vào hoặc so sánh với cột CSDL đều đọc từ đồng hồ
CSDL qua `GetDbNow(db)` — 4 điểm gọi. Lệnh nâng mốc version của worker giám sát cũng lấy mốc dấu vết bằng
hàm thời gian của CSDL, ⛔ không lấy đồng hồ máy ứng dụng. Lý do: chạy nhiều worker trên nhiều máy, mỗi máy lệch đồng hồ một
chút là cửa sổ chống dội và cửa sổ độc quyền lệch theo đúng mức đó. ⛔ Không dùng đồng hồ máy ứng dụng.

---

# Phụ lục B. Quyết định đã chốt và phương án bị bác

| Phương án | Vì sao bác |
| --- | --- |
| **Gửi bản ghi xoá mềm sang đối tác** (bỏ bộ lọc ở gói 107, `incidentState` trả mã "đã xoá") | ⛔ **Chủ dự án chốt 27/09/2026: không làm.** Miễn `IsDelete` có giá trị thì không lấy. Đã áp rồi hoàn nguyên sạch. Tương lai nếu đối tác chính thức yêu cầu thì mới thống nhất bộ mã trạng thái rồi bật lại. 📌 Chỉ gói 107 mới khả thi vì đặc tả có mệnh đề *"giá trị cũ sẽ update trạng thái"* và có trường `incidentState`; 103/104/106/109 ⛔ không có trường trạng thái nào nên bỏ lọc là đối tác cộng dữ liệu rác vào số liệu đo |
| **Đưa trạng thái cô lập bảng lỗi xuống CSDL để dùng chung** | Nguồn sự thật vốn đã ở bảng hệ thống của SQL Server ⇒ lưu thêm là **nhân bản dữ liệu**, rồi phải lo hai bản lệch nhau. Khởi động lại không mất gì vì chu kỳ đầu tự phát hiện lại. Dùng chung sẽ **mất** thông tin *máy nào* phát hiện (hữu ích khi chỉ vài máy gặp lỗi quyền hoặc kết nối riêng) |
| **Dùng một mốc hẹn thử lại chung cho mọi bảng bị cô lập** | Mốc chung bị gán lại mỗi lần cô lập thêm bảng ⇒ bảng A hỏng lúc t=0 (hẹn t+5 phút), bảng B hỏng lúc t=2 phút thì mốc bị đẩy thành t+7 phút, bảng A **mất lượt**. Hỏng liên tiếp thì **không bảng nào** được thử lại. Cần mốc hẹn **riêng từng bảng** |
| **Nới cột `LastKey` lên 128** | Khoá bản ghi hiện là khoá chính bảng nguồn, mà cột đó cũng 64 ⇒ nới là phá quy ước và che vấn đề thật. Chọn **thêm chốt chặn `ESH-1305`** tại `ExportPage` trước khi gửi |
| **Tạo bảng riêng hoặc dùng AlertLog để lưu sự cố Change Tracking** | Dùng lại cột `Remark` của `ShareDataActivityLog` (phân loại mã `ESH-16xx`) tận dụng cấu trúc có sẵn, không phải đổi schema CSDL hay giao diện, đồng thời giữ `ShareDataAlertLog` đúng vai trò dành riêng cho lỗi nghiệp vụ cần người can thiệp |
