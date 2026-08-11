---
type: project
created: 2026-08-11
updated: 2026-08-11
---

# Pending Implementation: ShareData Worker (Exclude API)

This is the approved implementation plan to be executed in the next session to close all remaining technical gaps for the ShareData Worker V1.

## 1. Cập nhật các Entities (Cơ sở dữ liệu)

### 1.1 `ShareDataPartner`
Bổ sung các tham số quản lý phiên C2C:
- **`MaxSessions`** (int?): Số phiên tối đa cho phép đồng thời. (Mặc định 1)
- **`SenderName`** (string, length 128): Tên hệ thống định danh trong gói `Initiate` (`datex-Sender-txt`).

### 1.2 `ShareDataSession`
Bổ sung các trường thống kê và duy trì mạng:
- **`LastFredNbr`** (long?): Mã số xác nhận gói FrED cuối cùng (`FrED ConfirmPacket-nbr`, kiểu `ulong`).
- **`TotalBytesSent`** (long?)
- **`TotalBytesRecv`** (long?)
- **`NegotiatedEncoding`** (string, length 16): Encoding rules đã đàm phán thành công.

### 1.3 `CctvDevice`
- Thêm trường **`EquipmentId`** (string, length 36).

## 2. Refactoring Mã nguồn (Worker Logic)

### 2.1 Cập nhật `OutboundService.cs` (Fix Join CCTV)
- Gói `102` (CCTV Image): Chuyển đổi logic Join giữa `CctvDevice` và `TmsEquipment` từ cột `Ip` sang cột `EquipmentId` như khuyến nghị.

### 2.2 Cập nhật `ShareDataEnum.cs`
- Thêm **`RejectSubscriptionReason`**: Chứa các hằng số lý do từ chối khi đối tác gửi gói Subscription (theo ISO 14827).

### 2.3 Cập nhật `C2CService.TcpEngine.cs` & `C2CService.InboundProcessor.cs`
- **MaxSessions**: Sửa code đang fix cứng `1` thành lấy từ cấu hình `partner.MaxSessions ?? 1`.
- **FrED Heartbeat**: Mỗi khi nhận gói Heartbeat, cập nhật `session.LastFredNbr`, `session.TotalBytesRecv`.
