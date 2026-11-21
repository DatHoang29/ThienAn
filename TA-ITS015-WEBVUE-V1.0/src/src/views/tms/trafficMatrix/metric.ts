/**
 * Chế độ xem của bảng ma trận theo giờ.
 *
 * DANH SÁCH và NHÃN của chế độ xem nằm ở cấu hình tham số
 * (`BaseConfigTypeEnum.TmsTrafficMatrixMetric`), không hardcode ở đây.
 *
 * Riêng hai MÃ dưới đây phải có trong code vì chúng là hợp đồng ngữ nghĩa với BE: mã nào thì đọc
 * `avgSpeed`, mã nào thì đọc `vehicleCount` của ô. Đây không phải tham số tinh chỉnh được —
 * đổi mã trong DB mà không đổi code thì bảng sẽ hiển thị sai số.
 */
export type TrafficMatrixMetric = string;

export const TrafficMatrixMetricEnum = {
	Speed: 'speed',
	Volume: 'volume',
};
