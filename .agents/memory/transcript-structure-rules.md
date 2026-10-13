---
name: Quy chuẩn cấu trúc tệp transcript cuộc họp
description: Tệp transcript chỉ gồm mục tóm tắt cốt lõi và kịch bản chi tiết, không tạo bảng Ma trận phân công công việc
type: feedback
---

# Quy chuẩn cấu trúc & quy trình nghe tệp Transcript cuộc họp / ghi âm

Khi trích xuất và biên soạn tệp `doc/transcript/*.md` từ audio cuộc họp:
1. **Chỉ giữ 2 phần chính**:
   - **Mục 1**: Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary).
   - **Mục 2**: Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript).
2. ⛔ **KHÔNG CẦN / CẤM tạo bảng**: `2. Ma trận phân công công việc (Action Items)` (bảng Người phụ trách | Hạng mục | Chi tiết).
3. **Bóc tách theo phân hệ**: Nếu một cuộc họp bàn nhiều phân hệ, bắt buộc tách thành các file riêng cho từng phân hệ (ví dụ VideoWall, ShareData...).
4. **Quy trình kỹ thuật khi nghe file Audio gốc (BẮT BUỘC)**:
   - **Cắt nhỏ đoạn audio (`Chunking` $\le 10\text{–}15$ phút/đoạn)**: ⛔ CẤM nạp nguyên file audio dài ($> 15$ phút) trong 1 lần gọi vì gây giảm độ phân giải chú ý ở nửa sau. Bắt buộc cắt thành từng đoạn 10–15 phút (bằng `ffmpeg` hoặc `swift` `AVAssetExportSession`) vào `scratch/`, nghe chi tiết từng giây rồi xóa ngay file cắt tạm.
   - **Tách kênh người nói & Lọc tạp âm bàn bên**: Phân biệt rõ giọng người tham gia cuộc họp chính với tiếng đồng nghiệp ở bàn khác nói chuyện điện thoại/bàn việc riêng gần mic; ⛔ cấm gộp nhầm lời nói nền vào biên bản.
   - **Không đoán từ theo quán tính tài liệu cũ**: Nghe chuẩn từng thuật ngữ kỹ thuật (`Kafka`, `NATS`, `API`, `byte[]`, `Dictionary`, `SystemConfigData`...), không tự ý sửa thuật ngữ nghe được thành từ quen thuộc của tài liệu cũ.

**Why:** Nạp nguyên file dài 42 phút khiến model bị loãng sự chú ý ở đoạn cuối, dễ chép nhầm giọng người ngồi bàn bên cạnh và đoán sai thuật ngữ (`Kafka` thành `NATS`). Cắt đoạn ngắn 10–15 phút giúp phân giải chính xác từng giây.
**How to apply:** Áp dụng cho mọi lần tạo tệp transcript mới hoặc kiểm tra/cập nhật transcript cũ trong `doc/transcript/`.
