---
name: Quy chuẩn cấu trúc tệp transcript cuộc họp
description: Tệp transcript chỉ gồm mục tóm tắt cốt lõi và kịch bản chi tiết, không tạo bảng Ma trận phân công công việc
type: feedback
---

# Quy chuẩn cấu trúc tệp Transcript cuộc họp / ghi âm

Khi trích xuất và biên soạn tệp `doc/transcript/*.md` từ audio cuộc họp:
1. **Chỉ giữ 2 phần chính**:
   - **Mục 1**: Tóm tắt nội dung & Quyết định kỹ thuật cốt lõi (Executive Summary).
   - **Mục 2**: Kịch bản đối thoại chi tiết theo dòng thời gian (Detailed Transcript).
2. ⛔ **KHÔNG CẦN / CẤM tạo bảng**: `2. Ma trận phân công công việc (Action Items)` (bảng Người phụ trách | Hạng mục | Chi tiết).
3. **Bóc tách theo phân hệ**: Nếu một cuộc họp bàn nhiều phân hệ, bắt buộc tách thành các file riêng cho từng phân hệ (ví dụ VideoWall, ShareData...).

**Why:** User xác nhận transcript chỉ cần tóm tắt quyết định và giữ nguyên lời thoại chi tiết theo dòng thời gian, bảng phân công công việc (Action Items) thừa thãi và không cần thiết.
**How to apply:** Áp dụng cho mọi lần tạo tệp transcript mới hoặc cập nhật transcript cũ trong `doc/transcript/`.
