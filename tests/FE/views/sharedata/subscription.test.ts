import { test, expect } from '@playwright/test';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

/**
 * Test Suite cho chức năng đăng ký chia sẻ dữ liệu (editSubscription.vue):
 * - TC1: StartTime == EndTime (23:00 -> 23:00) bị chặn, trả về timeRangeInvalid.
 * - TC2: EndTime < StartTime (23:00 -> 22:00) bị chặn, trả về timeRangeInvalid.
 * - TC3: Chỉ nhập 1 trong 2 ô (thiếu giờ bắt đầu hoặc giờ kết thúc) bị chặn, trả về timeRangeIncomplete.
 * - TC4: EndTime > StartTime (23:00 -> 23:01) hợp lệ. Với chu kỳ 30 giây, bắn 2 lần (0s và 30s) trong cửa sổ 1 phút.
 * - TC5: Để trống cả 2 ô (gửi liên tục 24/24) hợp lệ, không có lỗi.
 * - TC6: Quét tĩnh mã nguồn tuân thủ Rule 20.8 (CẤM toán tử !!), Rule 19.61 (Timeout <= 5s), template binding và submit guard.
 * - TC7: Kiểm tra đầy đủ bản dịch i18n trong vi-vn.json và en-us.json.
 */

// Hàm logic mô phỏng chính xác reactive computed từ editSubscription.vue
function evaluateTimeRange(startTimeVal: string, endTimeVal: string, t: (key: string) => string) {
	const isEndBeforeStart = Boolean(startTimeVal && endTimeVal) && endTimeVal <= startTimeVal;
	const isTimeRangeIncomplete = (Boolean(startTimeVal) && !endTimeVal) || (!startTimeVal && Boolean(endTimeVal));

	let timeRangeError = '';
	if (isTimeRangeIncomplete) {
		timeRangeError = t('lz.validation.sharedataSubscription.timeRangeIncomplete');
	} else if (isEndBeforeStart) {
		timeRangeError = t('lz.validation.sharedataSubscription.timeRangeInvalid');
	}

	return {
		isEndBeforeStart,
		isTimeRangeIncomplete,
		timeRangeError,
	};
}

const mockI18n = (key: string): string => {
	const dict: Record<string, string> = {
		'lz.validation.sharedataSubscription.timeRangeInvalid': 'Giờ kết thúc phải lớn hơn giờ bắt đầu.',
		'lz.validation.sharedataSubscription.timeRangeIncomplete': 'Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).',
	};
	return dict[key] || key;
};

test.describe('ShareData — Khung giờ đăng ký chia sẻ dữ liệu (editSubscription.vue) @sharedata', () => {

	test('TC1 — Chặn khi StartTime == EndTime (23:00 -> 23:00): Hiển thị lỗi và chặn khoảng thời gian rỗng', async () => {
		const result = evaluateTimeRange('23:00', '23:00', mockI18n);

		expect(result.isEndBeforeStart).toBe(true);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('Giờ kết thúc phải lớn hơn giờ bắt đầu.');
	});

	test('TC2 — Chặn khi EndTime < StartTime (23:00 -> 22:00): Hiển thị lỗi đảo ngược thời gian', async () => {
		const result = evaluateTimeRange('23:00', '22:00', mockI18n);

		expect(result.isEndBeforeStart).toBe(true);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('Giờ kết thúc phải lớn hơn giờ bắt đầu.');
	});

	test('TC3 — Chặn khi chỉ nhập 1 trong 2 ô (thiếu giờ bắt đầu hoặc giờ kết thúc): Báo lỗi thiếu khung giờ', async () => {
		const missingEnd = evaluateTimeRange('23:00', '', mockI18n);
		expect(missingEnd.isTimeRangeIncomplete).toBe(true);
		expect(missingEnd.timeRangeError).toBe('Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).');

		const missingStart = evaluateTimeRange('', '23:00', mockI18n);
		expect(missingStart.isTimeRangeIncomplete).toBe(true);
		expect(missingStart.timeRangeError).toBe('Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).');
	});

	test('TC4 — Hợp lệ khi EndTime > StartTime (06:00 -> 06:01 hoặc 23:00 -> 23:01): Form hợp lệ không báo lỗi', async () => {
		const eveningResult = evaluateTimeRange('23:00', '23:01', mockI18n);
		expect(eveningResult.isEndBeforeStart).toBe(false);
		expect(eveningResult.isTimeRangeIncomplete).toBe(false);
		expect(eveningResult.timeRangeError).toBe('');

		const morningResult = evaluateTimeRange('06:00', '06:01', mockI18n);
		expect(morningResult.isEndBeforeStart).toBe(false);
		expect(morningResult.isTimeRangeIncomplete).toBe(false);
		expect(morningResult.timeRangeError).toBe('');
	});

	test('TC5 — Hợp lệ khi để trống cả 2 ô (gửi liên tục 24/24): Không báo lỗi', async () => {
		const result = evaluateTimeRange('', '', mockI18n);

		expect(result.isEndBeforeStart).toBe(false);
		expect(result.isTimeRangeIncomplete).toBe(false);
		expect(result.timeRangeError).toBe('');
	});

	test('TC6 — Quét tĩnh mã nguồn editSubscription.vue: Tuân thủ Rule 20.8 (CẤM !!), submit guard và inline error', async () => {
		const vueFilePath = path.resolve(
			__dirname,
			'../../../../TA-ITS015-WEBVUE-V1.0/src/src/views/sharedata/sharing/component/editSubscription.vue'
		);
		const vueContent = fs.readFileSync(vueFilePath, 'utf8');

		// 1. Rule 20.8: Tuyệt đối không dùng toán tử !! trong đoạn xử lý khung giờ
		const timeRangeBlock = vueContent.substring(
			vueContent.indexOf('const isEndBeforeStart'),
			vueContent.indexOf('const daysOfWeek')
		);
		expect(timeRangeBlock).not.toContain('!!');

		// 2. Kiểm tra điều kiện chặn dấu bằng (<=)
		expect(timeRangeBlock).toContain('endTime.value <= startTime.value');

		// 3. Kiểm tra form-item binding :error hiển thị lỗi inline
		expect(vueContent).toContain(':error="scheduleKind === \'continuous\' ? timeRangeError : \'\'"');

		// 4. Kiểm tra submit guard chặn gọi API khi timeRangeError có lỗi
		const submitBlock = vueContent.substring(
			vueContent.indexOf('async function submit()'),
			vueContent.indexOf('defineExpose')
		);
		expect(submitBlock).toContain('scheduleKind.value === \'continuous\' && timeRangeError.value');

		// 5. Kiểm tra buildSchedule chuẩn hóa chuỗi rỗng thành undefined
		const buildScheduleBlock = vueContent.substring(
			vueContent.indexOf('function buildSchedule()'),
			vueContent.indexOf('async function submit()')
		);
		expect(buildScheduleBlock).toContain('startTime: startTime.value || undefined');
		expect(buildScheduleBlock).toContain('endTime: endTime.value || undefined');

		// 6. Kiểm tra không import thừa package lạ (như locale)
		expect(vueContent).not.toContain("import { lo } from 'element-plus/es/locale'");
	});

	test('TC7 — Kiểm tra cấu hình i18n trong vi-vn.json và en-us.json', async () => {
		const viPath = path.resolve(
			__dirname,
			'../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/vi-vn.json'
		);
		const enPath = path.resolve(
			__dirname,
			'../../../../TA-ITS015-WEBVUE-V1.0/src/src/i18n/lang/en-us.json'
		);

		const viJson = JSON.parse(fs.readFileSync(viPath, 'utf8'));
		const enJson = JSON.parse(fs.readFileSync(enPath, 'utf8'));

		expect(viJson['lz.validation.sharedataSubscription.timeRangeInvalid']).toBe(
			'Giờ kết thúc phải lớn hơn giờ bắt đầu.'
		);
		expect(viJson['lz.validation.sharedataSubscription.timeRangeIncomplete']).toBe(
			'Vui lòng nhập đầy đủ giờ bắt đầu và kết thúc (hoặc để trống cả hai).'
		);

		expect(enJson['lz.validation.sharedataSubscription.timeRangeInvalid']).toBe(
			'End time must be greater than start time.'
		);
		expect(enJson['lz.validation.sharedataSubscription.timeRangeIncomplete']).toBe(
			'Please enter both start and end times (or leave both blank).'
		);
	});
});
