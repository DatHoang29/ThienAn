// Phân loại thông điệp Vms
export enum VmsMessageType {
    Word = '0', // Cụm từ
    Sentence = '1' // Câu
}

// Phân loại hình ảnh Vms
export enum VmsImageType {
    Icon = 'icon', // Icon
    Image = 'image' // HÌnh ảnh
}

// Phân loại thao tác Vms
export enum VmsActionType {
    ImageTemplate = 'vms_img_template', // Mẫu hình ảnh có sẵn
    Scheduling = 'vms_scheduling', // Lập lịch
    Timer = 'vms_timer', // Hẹn giờ

    Text = 'text', // Thông điệp tự nhập
    Template = 'template', // Mẫu thông điệp có sẵn
    Image = 'image', // Hình ảnh
    Time = 'timer', // Thời gian
    Schedule = 'schedule', // Lập lịch
    Message = 'message', // Thông điệp
    Phrase = 'phrase', // Cụm từ

}

export enum VmsCurrentType {
    TextManual = '0', // Thông điệp tự nhập
    ImageTemplate = '1', // Mẫu hình ảnh có sẵn
    Scheduling = '2', // Lập lịch
    Timer = '3', // Hẹn giờ
}

// Author: Ngọc Thắng
// Enum vms-message mặc định cho incident
export enum VmsMessageForIncidentEnum {
    VMS_INCIDENT_MESSAGE_ROW1 = 'VMS_INCIDENT_MESSAGE_ROW1',
    VMS_INCIDENT_MESSAGE_ROW2 = 'VMS_INCIDENT_MESSAGE_ROW2',
    VMS_FIRE_MESSAGE_ROW1 = 'VMS_FIRE_MESSAGE_ROW1',
    VMS_FIRE_MESSAGE_ROW2 = 'VMS_FIRE_MESSAGE_ROW2',
    VMS_ACCIDENT_MESSAGE_ROW1 = 'VMS_ACCIDENT_MESSAGE_ROW1',
    VMS_ACCIDENT_MESSAGE_ROW2 = 'VMS_ACCIDENT_MESSAGE_ROW2',
    VMS_TRAFFIC_JAM_MESSAGE_ROW1 = 'VMS_TRAFFIC_JAM_MESSAGE_ROW1',
    VMS_TRAFFIC_JAM_MESSAGE_ROW2 = 'VMS_TRAFFIC_JAM_MESSAGE_ROW2'
}

// Loại thiết bị gửi kèm AddControlInput để BE phân nhánh xử lý (khớp VmsDeviceTypeConst phía BE)
export enum VmsDeviceType {
    Vms = 'vms',
    Lcs = 'lcs',
}

// Kích thước bảng cố định theo loại thiết bị (khớp BE EventDefaultCommandHandler)
export enum VmsSizeEnum {
    Vms = '384x64',
    Lcs = '128x128',
}

// Author: ChuongLP
// Description: Danh sách config-code điều khiển VMS/LCS (đọc từ SysOpConfig, nhóm 'operation_config').
// Dùng cho ràng buộc dữ liệu khi chuẩn bị gửi ra thiết bị thực tế (giới hạn ký tự mỗi dòng...).
// Lưu ý: các key này có thể CHƯA được cấu hình trong DB; khi thiếu sẽ dùng giá trị mặc định (fallback) ở FE.
export enum VmsControlConfigCodeEnum {
    ContentMaxLength = 'vms_content_length_default', // Giới hạn ký tự tối đa mỗi dòng nội dung (row1/row2 + ô text template)
}