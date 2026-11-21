<template>
	<div class="sharedata-codeset-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="!state.loading" :close-on-press-escape="!state.loading" :show-close="!state.loading" width="960px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit />
					</el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>

			<el-form v-loading="state.loading" :element-loading-text="$t('lz.label.base.loading')" :model="state.ruleForm" ref="ruleFormRef" label-width="auto"
				label-position="left" autocomplete="off"
				style="max-height: 560px; overflow-y: auto; overflow-x: hidden">
				<el-row :gutter="10">
					<el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item :label="$t('lz.entity.base.code')" prop="code"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.code"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })"
								clearable :disabled="props.operateType === 'edit'" />
						</el-form-item>
					</el-col>
					<el-col :xs="24" :sm="16" :md="16" :lg="16" :xl="16" class="mb20">
						<el-form-item :label="$t('lz.entity.base.name')" prop="name"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' }]">
							<el-input v-model="state.ruleForm.name"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })"
								clearable />
						</el-form-item>
					</el-col>
					<!-- <el-col :xs="24" :sm="8" :md="8" :lg="8" :xl="8" class="mb20">
						<el-form-item label="Phạm vi" prop="scope"
							:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: 'Phạm vi' }), trigger: 'change' }]">
							<el-radio-group v-model="state.ruleForm.scope">
								<el-radio v-for="item in getConfigDataByCode(BaseConfigTypeEnum.ShareScope)" :key="item.code" :value="parseInt(item.code)" class="mr5">{{
									item.name
								}}</el-radio>
							</el-radio-group>
						</el-form-item>
					</el-col> -->
					<el-col :xs="48" :sm="32" :md="32" :lg="32" :xl="32" class="mb20">
						<el-form-item :label="$t('lz.entity.base.remark')">
							<el-input v-model="state.ruleForm.remark"
								:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
								clearable
								type="textarea"/>
						</el-form-item>
					</el-col>

					<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
						<el-form-item label-width="0">
							<div class="w100">
								<div class="cs-toolbar">
									<span class="cs-title">{{ $t('lz.label.sharedataCodeSet.mappingValues') }}</span>
									<el-button type="primary" icon="ele-Plus" size="small" :disabled="valuesLocked" @click="addValueRow">{{ $t('lz.button.sharedataCodeSet.addValue') }}</el-button>
									<span class="rs-note">{{ $t('lz.label.sharedataCodeSet.valueCountLabel', { count: state.ruleForm.values?.length || 0 }) }}</span>
								</div>

								<!-- Hồ sơ ánh xạ đang dùng tham chiếu bộ mã: BE chặn sửa bảng quy đổi / mặc định (ESH-2206); tên, ghi chú, tên hiển thị vẫn sửa được -->
								<el-alert v-if="valuesLocked" type="warning" show-icon :closable="false" class="mb10"
									:title="$t('lz.label.sharedataCodeSet.valuesLockedByMapping', { mappings: state.inUseMappings.join(', ') })" />

								<!-- Hướng dẫn 2 chiều: cùng một bảng, chiều gửi đọc trái sang phải, chiều nhận đọc phải sang trái. -->
								<div class="cs-guide">
									<div class="cs-guide__item cs-guide__item--send">
										<el-tag size="small" type="primary" effect="dark">{{ $t('lz.label.sharedataCodeSet.directionSend') }}</el-tag>
										<span>{{ $t('lz.label.sharedataCodeSet.guideSend') }}</span>
									</div>
									<div class="cs-guide__item cs-guide__item--receive">
										<el-tag size="small" type="success" effect="dark">{{ $t('lz.label.sharedataCodeSet.directionReceive') }}</el-tag>
										<span>{{ $t('lz.label.sharedataCodeSet.guideReceive') }}</span>
									</div>
								</div>

								<vxe-table class="cs-values" :data="tableRows" border size="mini"
									:row-config="{ isHover: true }" :column-config="{ resizable: true }"
									:row-class-name="rowClassName"
									show-header-overflow="title"
									:empty-text="$t('lz.label.base.noData')">
									<!-- Độ rộng cột: # vừa tag "Giá trị mặc định *"; 3 cột nhập cùng min-width để chia đều phần còn lại. -->
									<vxe-column title="#" width="130" align="center">
										<template #default="{ row, rowIndex }">
											<el-tooltip v-if="row[DEFAULT_ROW_FLAG]" :content="$t('lz.label.sharedataCodeSet.defaultValueRowTooltip')" placement="top">
												<el-tag size="small" type="warning" effect="plain">{{ $t('lz.label.sharedataCodeSet.defaultValueRow') }}<span class="cs-req">*</span></el-tag>
											</el-tooltip>
											<span v-else>{{ rowIndex + 1 }}</span>
										</template>
									</vxe-column>
									<vxe-column field="sourceValue" :title="$t('lz.entity.sharedataCodeSet.sourceValue')" min-width="200">
										<template #default="{ row }">
											<el-input v-if="row[DEFAULT_ROW_FLAG]" v-model="state.ruleForm.defaultSourceValue" size="small" clearable :disabled="valuesLocked"
												:class="{ 'cs-dup': defaultErrors.source }"
												:placeholder="$t('lz.label.sharedataCodeSet.defaultSourceValue')">
												<template #prepend>
													<span class="cs-dir cs-dir--receive">{{ $t('lz.label.sharedataCodeSet.whenReceive') }}</span>
												</template>
											</el-input>
											<el-tooltip v-else :disabled="!isDuplicatedSource(row)" :content="$t('lz.label.sharedataCodeSet.duplicatedValue')" placement="top">
												<el-input v-model="row.sourceValue" size="small" :class="{ 'cs-dup': isDuplicatedSource(row) }" :disabled="valuesLocked"
													:placeholder="$t('lz.label.sharedataCodeSet.sourceValue')" />
											</el-tooltip>
										</template>
									</vxe-column>
									<vxe-column title="⇄" width="44" align="center" class-name="cs-arrow" />
									<vxe-column field="partnerValue" :title="$t('lz.entity.sharedataCodeSet.partnerValue')" min-width="200">
										<template #default="{ row }">
											<el-input v-if="row[DEFAULT_ROW_FLAG]" v-model="state.ruleForm.defaultPartnerValue" size="small" clearable :disabled="valuesLocked"
												:class="{ 'cs-dup': defaultErrors.partner }"
												:placeholder="$t('lz.label.sharedataCodeSet.defaultPartnerValue')">
												<template #prepend>
													<span class="cs-dir cs-dir--send">{{ $t('lz.label.sharedataCodeSet.whenSend') }}</span>
												</template>
											</el-input>
											<el-tooltip v-else :disabled="!isDuplicatedPartner(row)" :content="$t('lz.label.sharedataCodeSet.duplicatedValue')" placement="top">
												<el-input v-model="row.partnerValue" size="small" :class="{ 'cs-dup': isDuplicatedPartner(row) }" :disabled="valuesLocked"
													:placeholder="$t('lz.label.sharedataCodeSet.partnerValue')" />
											</el-tooltip>
										</template>
									</vxe-column>
									<vxe-column field="displayName" :title="$t('lz.entity.sharedataCodeSet.displayName')" min-width="200" :show-overflow="false">
										<template #default="{ row }">
											<span v-if="row[DEFAULT_ROW_FLAG]" class="cs-default-hint" :title="$t('lz.label.sharedataCodeSet.defaultRowHint')">{{ $t('lz.label.sharedataCodeSet.defaultRowHint') }}</span>
											<el-input v-else v-model="row.displayName" size="small" :placeholder="$t('lz.label.sharedataCodeSet.displayName')" />
										</template>
									</vxe-column>
									<vxe-column :title="$t('lz.label.base.action')" width="90" align="center">
										<template #default="{ row, rowIndex }">
											<el-button v-if="!row[DEFAULT_ROW_FLAG]" icon="ele-Delete" type="danger" text :disabled="valuesLocked"
												@click="deleteValueRow(rowIndex)" />
										</template>
									</vxe-column>
								</vxe-table>

								<!-- Giá trị mặc định bắt buộc cả hai chiều: màn Ánh xạ coi bộ mã luôn tự lo giá trị mặc định. -->
								<div v-if="defaultErrorMessage" class="cs-err">{{ defaultErrorMessage }}</div>

								<!-- Cảnh báo trùng ngay khi gõ: trùng nội bộ hỏng chiều gửi, trùng đối tác hỏng chiều nhận. -->
								<el-alert v-if="duplicateWarnings.length" type="warning" show-icon :closable="false" class="cs-dup-alert">
									<div v-for="(w, i) in duplicateWarnings" :key="i">{{ w }}</div>
								</el-alert>

								<!-- Thử quy đổi ngay trên form, để người dùng thấy giá trị mặc định áp vào lúc nào. -->
								<div class="cs-tester">
									<span class="cs-title">{{ $t('lz.label.sharedataCodeSet.tester') }}</span>
									<el-radio-group v-model="tester.direction" size="small">
										<el-radio-button value="send">{{ $t('lz.label.sharedataCodeSet.directionSend') }}</el-radio-button>
										<el-radio-button value="receive">{{ $t('lz.label.sharedataCodeSet.directionReceive') }}</el-radio-button>
									</el-radio-group>
									<el-input v-model="tester.input" size="small" clearable style="width: 180px"
										:placeholder="tester.direction === 'send' ? $t('lz.label.sharedataCodeSet.testerSend') : $t('lz.label.sharedataCodeSet.testerReceive')" />
									<template v-if="testResult">
										<span class="cs-tester__arrow">→</span>
										<el-tag size="small" :type="testResult.tagType">{{ testResult.value === '' ? '""' : testResult.value }}</el-tag>
										<span class="rs-note">{{ testResult.note }}</span>
									</template>
								</div>
							</div>
						</el-form-item>
					</el-col>
				</el-row>
			</el-form>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="eshEditCodeSet">
import { computed, reactive, ref } from 'vue';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataCodeSetApi } from '/@/api-services/shareData';
import { ShareDataUpdateCodeSetInput } from '/@/api-services/shareData';
import { OperateType } from '/@/types/constants';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
const { t: $t } = useI18n();

interface Props {
	/** the type of operation */
	operateType: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();

interface ShareDataCodeSetDefaultValueFields {
	/** Chiều NHẬN: giá trị đối tác gửi sang không khớp dòng nào thì lưu nội bộ bằng giá trị này. */
	defaultSourceValue?: string | null;
	/** Chiều GỬI: giá trị nội bộ không khớp dòng nào thì gửi cho đối tác giá trị này. */
	defaultPartnerValue?: string | null;
}
type EditableCodeSetForm = ShareDataUpdateCodeSetInput & ShareDataCodeSetDefaultValueFields;

const state = reactive({
	isShowDialog: false,
	ruleForm: {} as EditableCodeSetForm,
	loading: false,
	/** Đã bấm lưu ít nhất một lần — chỉ báo lỗi ô bắt buộc từ lúc này, không báo ngay khi mới mở form. */
	submitted: false,
	/** Hồ sơ ánh xạ đang dùng tham chiếu bộ mã (API chi tiết trả về). */
	inUseMappings: [] as string[],
});

/** Khớp BE ESH-2206: khoá bảng quy đổi và giá trị mặc định khi bộ mã nằm trong hồ sơ đang dùng. */
const valuesLocked = computed(() => props.operateType === 'edit' && state.inUseMappings.length > 0);

const DEFAULT_ROW_FLAG = '__isDefaultRow';

const tableRows = computed(() => [...(state.ruleForm.values ?? []), { [DEFAULT_ROW_FLAG]: true }]);

const rowClassName = ({ row }: { row: any }) => (row[DEFAULT_ROW_FLAG] ? 'cs-default-row' : '');

/** So khớp bỏ qua hoa thường và khoảng trắng thừa, giống cách service và BE kiểm tra trùng. */
const normalizeValue = (v?: string | null) => (v ?? '').trim().toLowerCase();

/** Các giá trị đang bị khai báo nhiều lần, tính lại mỗi khi người dùng gõ. */
const duplicatedKeys = computed(() => {
	const sourceCount = new Map<string, number>();
	const partnerCount = new Map<string, number>();

	for (const v of state.ruleForm.values ?? []) {
		const s = normalizeValue(v.sourceValue);
		if (s !== '') sourceCount.set(s, (sourceCount.get(s) ?? 0) + 1);
		const p = normalizeValue(v.partnerValue);
		if (p !== '') partnerCount.set(p, (partnerCount.get(p) ?? 0) + 1);
	}

	const pickDuplicated = (m: Map<string, number>) => new Set([...m].filter(([, c]) => c > 1).map(([k]) => k));
	return { source: pickDuplicated(sourceCount), partner: pickDuplicated(partnerCount) };
});

const isDuplicatedSource = (row: any) => !row[DEFAULT_ROW_FLAG] && duplicatedKeys.value.source.has(normalizeValue(row.sourceValue));
const isDuplicatedPartner = (row: any) => !row[DEFAULT_ROW_FLAG] && duplicatedKeys.value.partner.has(normalizeValue(row.partnerValue));

/** Ô giá trị mặc định còn trống, chỉ tính sau khi đã bấm lưu. */
const defaultErrors = computed(() => ({
	source: state.submitted && (state.ruleForm.defaultSourceValue ?? '').trim() === '',
	partner: state.submitted && (state.ruleForm.defaultPartnerValue ?? '').trim() === '',
}));

const defaultErrorMessage = computed(() => {
	const { source, partner } = defaultErrors.value;
	if (source && partner) return $t('lz.validation.sharedataCodeSet.defaultValuesRequired');
	if (source) return $t('lz.validation.sharedataCodeSet.defaultSourceValueRequired');
	if (partner) return $t('lz.validation.sharedataCodeSet.defaultPartnerValueRequired');
	return '';
});

const duplicateWarnings = computed(() => {
	const messages: string[] = [];
	const { source, partner } = duplicatedKeys.value;
	if (source.size) messages.push($t('lz.message.sharedataCodeSet.duplicatedSourceValue', { values: [...source].join(', ') }));
	if (partner.size) messages.push($t('lz.message.sharedataCodeSet.duplicatedPartnerValue', { values: [...partner].join(', ') }));
	return messages;
});

/**
 * Thử quy đổi — mô phỏng service:
 *  - Gửi : khớp sourceValue -> partnerValue; không khớp -> defaultPartnerValue.
 *  - Nhận: khớp partnerValue -> sourceValue; không khớp -> defaultSourceValue.
 *  - Không có mặc định -> giữ nguyên giá trị gốc.
 * So khớp bỏ qua hoa thường và khoảng trắng thừa, giống màn chạy thử mapping.
 */
const tester = reactive({
	direction: 'send' as 'send' | 'receive',
	input: '',
});

const testResult = computed(() => {
	const raw = (tester.input ?? '').trim();
	if (raw === '') return null;

	const isSend = tester.direction === 'send';
	const key = raw.toLowerCase();
	const hit = (state.ruleForm.values ?? []).find(
		(v) => ((isSend ? v.sourceValue : v.partnerValue) ?? '').trim().toLowerCase() === key,
	);
	if (hit) {
		return { value: ((isSend ? hit.partnerValue : hit.sourceValue) ?? '').trim(), tagType: 'success' as const, note: $t('lz.label.sharedataCodeSet.testerMatch') };
	}

	const fallback = ((isSend ? state.ruleForm.defaultPartnerValue : state.ruleForm.defaultSourceValue) ?? '').trim();
	if (fallback !== '') {
		return { value: fallback, tagType: 'warning' as const, note: $t('lz.label.sharedataCodeSet.testerDefault') };
	}
	return { value: raw, tagType: 'info' as const, note: $t('lz.label.sharedataCodeSet.testerKeep') };
});

// Open dialog
const openDialog = async (row: any) => {
	// Reset validate trước để xoá lỗi từ lần mở trước
	ruleFormRef.value?.resetFields();
	state.submitted = false;
	state.inUseMappings = [];
	state.ruleForm = JSON.parse(JSON.stringify(row));
	state.isShowDialog = true;

	// Danh sách phân trang không trả kèm mảng values nên phải nạp chi tiết theo ID.
	if (row?.id) {
		state.loading = true;
		try {
			const res = await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetByidGet(row.id);
			if (res.data.result) {
				state.ruleForm = JSON.parse(JSON.stringify(res.data.result));
				// Sao chép bộ mã tạo bộ mới, không thừa hưởng khoá của bộ gốc.
				state.inUseMappings = props.operateType === 'edit' ? res.data.result.inUseMappings ?? [] : [];
			}
		} finally {
			state.loading = false;
		}
	}
	if (!state.ruleForm.values) state.ruleForm.values = [];
	state.ruleForm.defaultSourceValue ??= '';
	state.ruleForm.defaultPartnerValue ??= '';
	tester.direction = 'send';
	tester.input = '';
};

// Close dialog
const closeDialog = () => {
	emits('handleQuery');
	state.isShowDialog = false;
};

// Cancel
const cancel = () => {
	state.isShowDialog = false;
};

// Submit
const submit = () => {
	state.submitted = true;
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		if (defaultErrorMessage.value) {
			ElMessage.warning(defaultErrorMessage.value);
			return;
		}

		// Bỏ các dòng để trống hoàn toàn để không lưu rác.
		const values = (state.ruleForm.values ?? []).filter(
			(v) => (v.sourceValue ?? '').trim() !== '' || (v.partnerValue ?? '').trim() !== '',
		);
		if (values.length === 0) {
			ElMessage.warning($t('lz.message.sharedataCodeSet.needAtLeastOneMapping'));
			return;
		}
		if (duplicateWarnings.value.length) {
			ElMessage.warning(duplicateWarnings.value[0]);
			return;
		}

		state.ruleForm.values = values;
		state.ruleForm.defaultSourceValue = (state.ruleForm.defaultSourceValue ?? '').trim();
		state.ruleForm.defaultPartnerValue = (state.ruleForm.defaultPartnerValue ?? '').trim();

		//Edit
		if (props.operateType == 'edit') {
			await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetUpdatePost(state.ruleForm);
		}
		//Copy or Add
		else if (props.operateType == 'copy' || props.operateType == 'add') {
			state.ruleForm.id = '';
			await getAPI(ShareDataCodeSetApi).apiSharedataSharedatacodesetAddPost(state.ruleForm);
		}
		ElMessage.success($t('lz.message.base.saveSuccess'));
		closeDialog();
	});
};

/** Thêm 1 dòng giá trị mới */
const addValueRow = () => {
	if (!state.ruleForm.values) state.ruleForm.values = [];
	state.ruleForm.values.push({ sourceValue: '', partnerValue: '', displayName: '' });
};

/** Xoá 1 dòng giá trị */
const deleteValueRow = (index: number) => {
	state.ruleForm.values?.splice(index, 1);
};

// Export object
defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.cs-toolbar {
	display: flex;
	align-items: center;
	gap: 8px;
	margin-bottom: 5px;
}

.cs-title {
	font-size: 14px;
	font-weight: 600;
	color: var(--el-text-color-regular);
	margin-right: 4px;
}

.rs-note {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

/* Ô nhập trong bảng bám sát viền ô, không để el-form-item chèn thêm lề. */
.cs-values :deep(.el-input__wrapper),
.cs-values :deep(.el-input-number) {
	width: 100%;
}

/* Chừa 2px trên/dưới quanh ô nhập để các dòng không dính sát kẻ ngang.
   Chiều cao dòng của theme vẫn là mức tối thiểu, chỉ nới ra khi ô nhập + padding cao hơn. */
.cs-values.size--mini :deep(.vxe-body--column.col--ellipsis > .vxe-cell),
.cs-values :deep(.vxe-body--column > .vxe-cell) {
	max-height: none;
	padding-top: 2px;
	padding-bottom: 2px;
}

/* Tô nền riêng cho dòng Giá trị mặc định (cố định, không xoá được) để phân biệt
   với các dòng quy đổi có thể thêm/xoá. Cho phép dòng tự tăng chiều cao theo nội dung. */
.cs-values :deep(.cs-default-row) {
	background-color: var(--el-color-warning-light-9);
}

.cs-values :deep(.cs-arrow) {
	color: var(--el-text-color-placeholder);
	font-size: 16px;
}

/* Nhãn "Khi gửi / Khi nhận" gắn đầu ô mặc định, cùng màu với khung hướng dẫn phía trên. */
.cs-values :deep(.el-input-group__prepend) {
	padding: 0 8px;
}

/* Hai nhãn rộng bằng nhau để ô nhập mặc định ở 2 cột bắt đầu thẳng hàng. */
.cs-dir {
	display: inline-block;
	min-width: 56px;
	text-align: center;
	font-size: 12px;
	font-weight: 600;
	white-space: nowrap;
}

.cs-dir--send {
	color: var(--el-color-primary);
}

.cs-dir--receive {
	color: var(--el-color-success);
}

.cs-default-hint {
	display: block;
	font-size: 12px;
	color: var(--el-text-color-secondary);
	white-space: normal;
	word-break: break-word;
	line-height: 1.4;
}

.cs-guide {
	display: flex;
	flex-direction: column;
	gap: 4px;
	margin-bottom: 8px;
	padding: 8px 10px;
	border-radius: 4px;
	background-color: var(--el-fill-color-lighter);
	font-size: 12px;
	color: var(--el-text-color-regular);
	line-height: 1.5;
}

.cs-guide__item {
	display: flex;
	align-items: flex-start;
	gap: 8px;
}

.cs-guide__item .el-tag {
	flex-shrink: 0;
	min-width: 76px;
	justify-content: center;
}

.cs-tester {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 8px;
	margin-top: 10px;
	padding-top: 8px;
	border-top: 1px dashed var(--el-border-color);
}

.cs-dup-alert {
	margin-top: 8px;
	padding: 6px 10px;
}

.cs-dup-alert :deep(.el-alert__content) {
	font-size: 12px;
	line-height: 1.6;
}

/* Ô bị trùng: viền đỏ ngay khi gõ, kèm tooltip giải thích. */
.cs-values :deep(.cs-dup .el-input__wrapper) {
	box-shadow: 0 0 0 1px var(--el-color-danger) inset;
}

.cs-req {
	margin-left: 2px;
	color: var(--el-color-danger);
}

.cs-err {
	margin-top: 6px;
	font-size: 12px;
	line-height: 1.4;
	color: var(--el-color-danger);
}

.cs-tester__arrow {
	color: var(--el-text-color-secondary);
}

.cs-values.size--mini :deep(.cs-default-row .vxe-body--column),
.cs-values :deep(.cs-default-row .vxe-body--column) {
	height: auto;
	min-height: var(--vxe-ui-table-row-height-mini, 32px);
}

/* Gỡ bỏ giới hạn max-height của vxe-cell (kể cả khi vxe-table tự thêm class col--ellipsis hoặc c--tooltip) */
.cs-values.size--mini :deep(.cs-default-row .vxe-body--column.col--ellipsis > .vxe-cell),
.cs-values.size--mini :deep(.cs-default-row .vxe-body--column > .vxe-cell),
.cs-values :deep(.cs-default-row .vxe-cell.c--tooltip),
.cs-values :deep(.cs-default-row .vxe-cell) {
	max-height: none;
	overflow: visible;
	white-space: normal;
	padding-top: 4px;
	padding-bottom: 4px;
}
</style>
