/**
 * Thang z-index tập trung cho các camera control UI.
 *
 * Bảo đảm thứ tự lớp đúng: video < controls < overlay < dropdown < dialog < message/tooltip.
 * Dùng cho thuộc tính `:z-index` của `el-dialog`, `popper-class` của dropdown
 * và `customClass` của `ElMessage` để loại bỏ số ma thuật (magic numbers).
 */
export const CameraControlLayer = {
  VIDEO: 0,
  CONTROLS: 5,
  OVERLAY: 10,
  DROPDOWN: 10080,
  DIALOG: 10090,
  MESSAGE: 10100,
} as const;

export type CameraControlLayer = (typeof CameraControlLayer)[keyof typeof CameraControlLayer];
