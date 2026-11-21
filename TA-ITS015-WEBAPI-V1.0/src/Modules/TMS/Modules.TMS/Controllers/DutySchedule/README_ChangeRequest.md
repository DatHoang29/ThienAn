# API Quản lý Thay đổi Lịch trực (Duty Schedule Change Request)

## Tổng quan

Hệ thống cho phép thành viên và đội trưởng tạo yêu cầu thay đổi lịch trực với các loại sau:
- **Swap**: Đổi ca với người khác
- **Absent**: Xin nghỉ
- **Replace**: Thay thế người khác

Mọi thay đổi phải được **phê duyệt** bởi:
- Người tạo lịch trực (CreateUId)
- Hoặc đội trưởng (Leader) của lịch đó

---

## Quy trình Workflow

### 1. Thành viên tạo yêu cầu thay đổi

**Endpoint**: `POST /api/tms/dutyschedule/CreateChangeRequest`

**Request Body**:
```json
{
  "dutyScheduleId": "schedule-id-123",
  "type": "Swap",  // hoặc "Absent", "Replace"
  "targetUserId": "user-id-456",  // Bắt buộc với Swap/Replace
  "reason": "Có việc đột xuất",
  "remark": "Ghi chú thêm"
}
```

**Validation**:
- User phải là thành viên của lịch trực
- Không được có yêu cầu Pending khác cho cùng lịch trực
- Với Swap/Replace: phải có TargetUserId

**Response**: 200 OK

---

### 2. Người phê duyệt xem danh sách yêu cầu chờ duyệt

**Endpoint**: `POST /api/tms/dutyschedule/GetChangeRequests`

**Request Body**:
```json
{
  "viewMode": 2,  // 0: All, 1: MyRequests, 2: PendingApproval
  "status": "Pending",
  "dutyScheduleId": "schedule-id-123"  // optional
}
```

**ViewMode**:
- `All (0)`: Tất cả yêu cầu
- `MyRequests (1)`: Yêu cầu do tôi tạo
- `PendingApproval (2)`: Yêu cầu chờ tôi phê duyệt (lịch tôi tạo hoặc tôi là Leader)

**Response**:
```json
[
  {
    "id": "request-id-789",
    "dutyScheduleId": "schedule-id-123",
    "type": "Swap",
    "requesterId": "user-id-123",
    "targetUserId": "user-id-456",
    "reason": "Có việc đột xuất",
    "status": "Pending",
    "createTime": "2025-11-19T10:30:00",
    "dutySchedule": {
      "name": "Ca trực tháng 11",
      "startTime": "2025-11-20T08:00:00",
      "endTime": "2025-11-20T17:00:00"
    }
  }
]
```

---

### 3. Người phê duyệt duyệt/từ chối yêu cầu

**Endpoint**: `POST /api/tms/dutyschedule/ApproveChangeRequest`

**Request Body**:
```json
{
  "changeRequestId": "request-id-789",
  "status": "Approved",  // hoặc "Rejected"
  "approvalNote": "Đồng ý cho swap"
}
```

**Validation**:
- Chỉ người tạo lịch hoặc Leader mới được duyệt
- Không được tự duyệt yêu cầu của mình
- Chỉ duyệt được yêu cầu Pending

**Khi Approved**:
- **Absent**: Đánh dấu Status = "Absent" trong Detail
- **Swap**: 
  - Nếu TargetUser đã có trong lịch: Swap vai trò
  - Nếu TargetUser chưa có: Thêm mới TargetUser, đánh dấu Requester = "Swapped"
- **Replace**: Đánh dấu TargetUser = "Replaced", Requester = "Active"

**Response**: 200 OK

---

### 4. Người yêu cầu hủy yêu cầu

**Endpoint**: `POST /api/tms/dutyschedule/CancelChangeRequest`

**Request Body**:
```json
{
  "changeRequestId": "request-id-789"
}
```

**Validation**:
- Chỉ người tạo yêu cầu mới được hủy
- Chỉ hủy được yêu cầu Pending

**Response**: 200 OK

---

### 5. Xem chi tiết một yêu cầu

**Endpoint**: `GET /api/tms/dutyschedule/GetChangeRequest/{id}`

**Response**:
```json
{
  "id": "request-id-789",
  "dutyScheduleId": "schedule-id-123",
  "type": "Swap",
  "requesterId": "user-id-123",
  "targetUserId": "user-id-456",
  "reason": "Có việc đột xuất",
  "status": "Approved",
  "approverId": "admin-id-999",
  "approvedTime": "2025-11-19T11:00:00",
  "approvalNote": "Đồng ý cho swap",
  "dutySchedule": { ... },
  "createTime": "2025-11-19T10:30:00"
}
```

---

## Các trạng thái (Status)

| Status | Mô tả |
|--------|-------|
| `Pending` | Chờ phê duyệt |
| `Approved` | Đã phê duyệt |
| `Rejected` | Bị từ chối |
| `Cancelled` | Đã hủy (bởi người yêu cầu) |

---

## Các loại yêu cầu (Type)

| Type | Mô tả | Yêu cầu TargetUserId |
|------|-------|---------------------|
| `Swap` | Đổi ca với người khác | Có |
| `Absent` | Xin nghỉ | Không |
| `Replace` | Thay thế người khác | Có |

---

## Quyền phê duyệt

Người có quyền phê duyệt:
1. **Người tạo lịch trực** (CreateUId của TmsDutySchedule)
2. **Đội trưởng** (Role = "Leader" trong TmsDutyScheduleDetail)

**Lưu ý**: Không được tự phê duyệt yêu cầu của mình

---

## Ví dụ Use Cases

### Use Case 1: Thành viên xin nghỉ

```
1. User A tạo yêu cầu Absent cho lịch trực ngày 20/11
2. Leader xem danh sách yêu cầu chờ duyệt
3. Leader phê duyệt
4. Hệ thống đánh dấu User A = "Absent" trong Detail
```

### Use Case 2: Đổi ca

```
1. User A muốn đổi ca với User B
2. User A tạo yêu cầu Swap (targetUserId = User B)
3. Người tạo lịch phê duyệt
4. Hệ thống swap vai trò giữa User A và User B
```

### Use Case 3: Thay thế

```
1. User A muốn thay thế User B (vì User B có việc)
2. User A tạo yêu cầu Replace (targetUserId = User B)
3. Leader phê duyệt
4. Hệ thống đánh dấu User B = "Replaced", User A = "Active"
```

---

## Database Schema

### TmsDutyScheduleChangeRequest

| Field | Type | Description |
|-------|------|-------------|
| ID | string | Primary key |
| DutyScheduleId | string | FK → TmsDutySchedule |
| Type | string | Swap/Absent/Replace |
| RequesterId | string | Người yêu cầu |
| TargetUserId | string? | Người được swap/thay thế |
| Reason | string? | Lý do |
| Status | string | Pending/Approved/Rejected/Cancelled |
| ApproverId | string? | Người phê duyệt |
| ApprovedTime | DateTime? | Thời gian phê duyệt |
| ApprovalNote | string? | Ghi chú phê duyệt |
| CreateTime | DateTime | Thời gian tạo |
| CreateUId | string | Người tạo |

---

## Frontend Integration

### 1. Trang danh sách lịch trực
- Hiển thị button "Tạo yêu cầu thay đổi" cho mỗi lịch
- User chọn loại: Swap/Absent/Replace

### 2. Trang yêu cầu của tôi
- Gọi API với `viewMode = MyRequests`
- Hiển thị trạng thái, cho phép hủy nếu Pending

### 3. Trang phê duyệt (dành cho Leader/Creator)
- Gọi API với `viewMode = PendingApproval`
- Hiển thị button Approve/Reject

### 4. Notification
- Thông báo khi có yêu cầu mới chờ duyệt
- Thông báo khi yêu cầu được duyệt/từ chối

---

## Lưu ý quan trọng

1. **Transaction**: Tất cả thao tác phê duyệt đều dùng transaction để đảm bảo tính toàn vẹn
2. **Cache**: Sau mỗi thao tác, cache của DutySchedule được clear
3. **Validation**: Kiểm tra kỹ quyền trước khi cho phép tạo/phê duyệt
4. **Audit**: Mọi thay đổi đều được log với CreateUId, CreateTime, UpdateTime
