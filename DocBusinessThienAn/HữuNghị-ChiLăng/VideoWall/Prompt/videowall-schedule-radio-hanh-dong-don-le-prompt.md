# Fix UI: Radio "Hành động" ở form Lập lịch VideoWall vô nghĩa khi chỉ còn 1 lựa chọn

**Tệp prompt:** `DocBusinessThienAn/HữuNghị-ChiLăng/VideoWall/Prompt/videowall-schedule-radio-hanh-dong-don-le-prompt.md`

## Bối cảnh
`SCHEDULE_ACTIONS` (`src/src/views/videoWall/schedule/component/scheduleOptions.ts`) đang lọc còn
đúng 1 phần tử `activate_scene` ("Chuyển kịch bản") — 2 hành động `close_screens`/`open_screens` bị
ẩn tạm (comment trong file: "TẠM ẨN... BE chưa xử lý — VwScheduleConst.Action.Supported"). Field
"Hành động" trong form Thêm/Sửa Lập lịch (`editSchedule.vue`) vẫn render bằng `el-radio-group` lặp
qua `SCHEDULE_ACTIONS` ⇒ hiện ra đúng 1 radio bấm được, bắt buộc người dùng click vào mới qua được
validate `required` — cảm giác thao tác thừa, không phải lựa chọn thật. Phát hiện trực tiếp khi
người dùng test UI thực tế trên nhánh `fix/20261005-videowall-validation`.

## File cần sửa
`TA-ITS015-WEBVUE-V1.0/src/src/views/videoWall/schedule/component/editSchedule.vue`

## Trước / Sau

### 1. Import (dòng 97 và 106)
TRƯỚC:
```ts
import { reactive, ref } from 'vue';
...
import { SCHEDULE_TYPES, SCHEDULE_TYPE, SCHEDULE_ACTIONS, WEEKDAY_OPTIONS, parseWeekdays, joinWeekdays } from './scheduleOptions';
```
SAU:
```ts
import { reactive, ref, computed } from 'vue';
...
import { SCHEDULE_TYPES, SCHEDULE_TYPE, SCHEDULE_ACTIONS, SCHEDULE_ACTION_LABELS, WEEKDAY_OPTIONS, parseWeekdays, joinWeekdays } from './scheduleOptions';
```

### 2. Thêm computed tra nhãn hiện tại (chèn ngay sau khai báo `state`, trước `validateWeekdays`, dòng ~127)
TRƯỚC:
```ts
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as VwUpdateScheduleInput,
	/** Bản mảng của `ruleForm.weekdays` (CSV) cho checkbox-group. */
	weekdays: [] as number[],
});

const validateWeekdays = (_rule: any, _value: any, callback: (e?: Error) => void) => {
```
SAU:
```ts
const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	ruleForm: {} as VwUpdateScheduleInput,
	/** Bản mảng của `ruleForm.weekdays` (CSV) cho checkbox-group. */
	weekdays: [] as number[],
});

/** Nhãn hành động hiện tại — tra theo danh sách đầy đủ (không lọc) để vẫn hiển thị đúng khi
 *  record cũ đang mang 1 hành động đã bị ẩn khỏi SCHEDULE_ACTIONS. */
const currentActionLabel = computed(() => SCHEDULE_ACTION_LABELS.find((a) => a.value === state.ruleForm.action)?.label ?? '');

const validateWeekdays = (_rule: any, _value: any, callback: (e?: Error) => void) => {
```

### 3. Template field "Hành động" (dòng 56-63)
TRƯỚC:
```html
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.label.base.action')" prop="action"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.action') }), trigger: 'change' }]">
							<el-radio-group v-model="state.ruleForm.action" @change="onActionChange">
								<el-radio v-for="item in SCHEDULE_ACTIONS" :key="item.value" :value="item.value">{{ item.label }}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col>
```
SAU:
```html
					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item :label="$t('lz.label.base.action')" prop="action"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.label.base.action') }), trigger: 'change' }]">
							<el-radio-group v-if="SCHEDULE_ACTIONS.length > 1" v-model="state.ruleForm.action" @change="onActionChange">
								<el-radio v-for="item in SCHEDULE_ACTIONS" :key="item.value" :value="item.value">{{ item.label }}</el-radio>
							</el-radio-group>
							<span v-else>{{ currentActionLabel }}</span>
						</el-form-item>
					</el-col>
```

### 4. `openDialog` — điền sẵn giá trị khi chỉ còn 1 lựa chọn (dòng 140-145)
TRƯỚC:
```ts
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.weekdays = parseWeekdays(state.ruleForm.weekdays);
	state.isShowDialog = true;
};
```
SAU:
```ts
const openDialog = (row: any) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = JSON.parse(JSON.stringify(row));
	// Chỉ còn 1 hành động khả dụng ⇒ điền sẵn, khỏi bắt người dùng bấm vào lựa chọn duy nhất.
	if (!state.ruleForm.action && SCHEDULE_ACTIONS.length === 1) state.ruleForm.action = SCHEDULE_ACTIONS[0].value;
	state.weekdays = parseWeekdays(state.ruleForm.weekdays);
	state.isShowDialog = true;
};
```

## Lý do chọn cách này (không phải xoá hẳn field)
- Tự động phục hồi khi BE mở thêm hành động: khi `SCHEDULE_ACTIONS` có >1 phần tử trở lại (ai đó bỏ
  filter `ENABLED_ACTIONS` trong `scheduleOptions.ts` khi BE đã xử lý `close_screens`/`open_screens`),
  `v-if="SCHEDULE_ACTIONS.length > 1"` tự động hiện lại radio-group bình thường — không cần sửa gì
  thêm ở file này.
- Dùng `SCHEDULE_ACTION_LABELS` (danh sách đầy đủ, không lọc) để tra nhãn hiển thị tĩnh — xử lý đúng
  trường hợp Sửa 1 bản ghi cũ đang mang giá trị `close_screens`/`open_screens` (đã ẩn khỏi lựa chọn
  mới nhưng vẫn phải hiển thị đúng tên, không hiện rỗng) — giống hệt cách `schedule/index.vue` đang
  làm với hàm `actionLabel` (cùng 1 kiểu tra cứu, nhất quán).
- Không đổi field gửi lên BE (`ruleForm.action` vẫn gửi đúng string `'activate_scene'` như cũ), không
  đổi API, không đổi validate rule `required` (giá trị đã được điền sẵn trước khi form hiện ra nên vẫn
  qua validate bình thường).

## Phạm vi ảnh hưởng
Chỉ 1 file FE, thuần hiển thị + 1 dòng set giá trị mặc định lúc mở dialog. Không đụng BE, không đụng
API, không đụng DB. Không cần chạy `npm run build` (rule 13 mục 4 — client dev server hot-reload tự
nạp khi sửa `.vue`).

## Kiểm chứng thủ công sau khi áp dụng
1. VideoWall > Lập lịch > Thêm mới: field "Hành động" hiện chữ tĩnh **"Chuyển kịch bản"** (không phải
   radio bấm được); field "Kịch bản đích" vẫn hiện và bắt buộc chọn như cũ.
2. Lưu thử 1 lịch mới → kiểm tra `VwSchedule.Action` trong DB vẫn lưu đúng `activate_scene` như trước
   khi sửa (không đổi behavior lưu).
3. Mở Sửa 1 lịch đã có sẵn (action = activate_scene) → vẫn hiện đúng "Chuyển kịch bản" dạng chữ tĩnh,
   Lưu lại không đổi hành vi.
4. Nếu có bản ghi cũ mang action khác (ví dụ dữ liệu test cũ `close_screens`): mở Sửa bản ghi đó →
   phải hiện đúng nhãn tương ứng (không rỗng), không cho đổi sang hành động khác — đúng ý đồ: hành
   động đó đã bị ẩn khỏi lựa chọn mới, chỉ hiển thị để biết record cũ đang dùng gì.

## Việc cuối — Cập nhật lại tài liệu gốc
📌 Prompt này không sinh ra từ tài liệu nào (phát hiện trực tiếp khi người dùng test UI thực tế và
phản hồi "cảm giác vô nghĩa", không trích từ MasterPlan/báo cáo nào đã có) ⇒ ⛔ không có mục cập nhật
tài liệu gốc.
