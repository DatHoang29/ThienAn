// Chuẩn hóa các regex để sử dụng chung trong dự án
// Email regex: Cho phép các ký tự chữ, số, và một số ký tự đặc biệt, và phải có địa chỉ email hợp lệ
export const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;

// Phone number regex: Cho phép số điện thoại quốc tế, bắt đầu bằng dấu + theo sau là 1-30 con số
export const phoneRegex = /^(?:\+?\d{1,3})?([0-9]{9,14})$/;


export const recipientsEmailRegex =
    /^([A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,})(\s*[,;]\s*[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,})*$/;