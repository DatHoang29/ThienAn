# ShareData — Kiểm thử & Nghiệm thu

Thư mục này chứa **tài liệu kiểm thử và nghiệm thu** của phân hệ Chia sẻ dữ liệu: nhật ký lỗi do QA ghi nhận, biểu mẫu nghiệm thu, ảnh chụp màn hình bằng chứng.

> 🔴 **Tách bạch khỏi `../doc/`.** `doc/` chứa **đặc tả nghiệp vụ** (yêu cầu hệ thống, mapping gói tin, bộ khung ánh xạ); thư mục này chứa **kết quả kiểm thử tại một thời điểm**. ⛔ Không trộn hai loại vào nhau, ⛔ không đưa tài liệu ở đây vào Tier Table nghiệp vụ của `../README.md`.
>
> 🔴 **⛔ Không tra trạng thái task ở đây.** Sổ theo dõi duy nhất là [`../Plan/Sharedata_MasterPlan.md`](../Plan/Sharedata_MasterPlan.md) (rule 19.24). Tài liệu trong thư mục này là **ảnh chụp tại thời điểm kiểm thử**, sẽ lạc hậu dần.

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

## Danh mục

| Tệp | Nội dung | Nguồn gốc |
|---|---|---|
| [`F16-nhat-ky-loi-issue-20260929.md`](F16-nhat-ky-loi-issue-20260929.md) | **28 issue** do TuyenHTN ghi nhận 26/09–29/09/2026, toàn bộ `Chờ phản hồi`. Mỗi issue kèm **nội dung ảnh chụp đã chép thành chữ** — trong đó có **10 mã lỗi thật** mà phần chữ của biểu mẫu không ghi | `F16.TAC-CN01-HNCL_ITS-KICH BAN KIEM THU - Issue - ShareData.pdf` (4 trang) |
| [`images/`](images/) | 33 ảnh chụp màn hình bóc từ PDF, đặt tên theo số issue (`issue-NN.png`, issue có 2 ảnh thì `-a`/`-b`) | bóc từ PDF trên |

📌 Ảnh là **Tier C** (human-only, rule 16) — ⛔ AI không nạp mặc định. Toàn bộ thông tin cần thiết **đã được chép thành chữ** trong tệp `.md`; chỉ mở ảnh khi cần đối chiếu trực quan.

---

## Vì sao phải chép ảnh thành chữ

Phần chữ của biểu mẫu F16 phần lớn chỉ ghi *"thông báo lỗi gây khó hiểu"* mà **không trích câu lỗi thật**. Câu lỗi nằm trong ảnh, và chính nó mới cho biết **phải vá key nào**.

⛔ **Không nhúng base64 vào `.md`**: mô hình đọc ảnh qua kênh hình ảnh, ⛔ không giải mã base64 từ văn bản — nhúng vào chỉ làm phình tệp hàng MB mà **không ai đọc được thêm gì**. Cách đúng là **chép nội dung thành chữ** (tệp `.md` rẻ, đọc là hiểu) và **giữ PNG rời** làm bản gốc đối chiếu.

📌 Quy trình bóc tách chi tiết (lệnh, thư viện, cách gán ảnh ↔ issue) ghi ở mục *Ghi chú chuyển thể* cuối tệp `.md`.

---

## Quy ước cho tài liệu thêm vào sau

- Đặt tên theo **mã biểu mẫu + nội dung + ngày**: `F16-nhat-ky-loi-issue-YYYYMMDD.md`.
- Ảnh đi kèm vào `images/`, đặt tên **theo số issue** để tra ngược được.
- Bản gốc Tier C (PDF/XLSX) để ở [`../_source/`](../_source/), ⛔ không để ở gốc repo.
- Mỗi tệp `.md` mới thêm một dòng vào bảng *Danh mục* ở trên.
