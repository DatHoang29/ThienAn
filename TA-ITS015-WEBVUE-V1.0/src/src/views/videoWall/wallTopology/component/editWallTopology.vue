<template>
	<div class="videowall-topology-edit-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="560px">
			<template #header>
				<div style="color: #fff">
					<el-icon size="16" style="margin-right: 3px; display: inline; vertical-align: middle"> <ele-Edit /> </el-icon>
					<span> {{ props.title }} </span>
				</div>
			</template>
			<!-- Chỉ thông tin chung; lưới khối / kích thước ô sửa ở tab Bố cục tường -->
			<el-form :model="state.ruleForm" ref="ruleFormRef" label-width="auto" label-position="left" autocomplete="off">
				<el-form-item :label="$t('lz.entity.base.code')" prop="code"
					:rules="[
						{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.code') }), trigger: 'blur' },
						{ max: 64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.code'), MaxLength: 64 }), trigger: 'blur' },
					]">
					<el-input v-model="state.ruleForm.code" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.code') })" clearable />
				</el-form-item>
				<el-form-item :label="$t('lz.entity.base.name')" prop="name"
					:rules="[
						{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.base.name') }), trigger: 'blur' },
						{ max: 128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.name'), MaxLength: 128 }), trigger: 'blur' },
					]">
					<el-input v-model="state.ruleForm.name" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.name') })" clearable />
				</el-form-item>
				<!-- Số ô: tổng cột × hàng của mọi khối phải đúng bằng số này khi sửa lưới ở tab Bố cục tường -->
				<el-form-item :label="$t('lz.entity.videoWall.cellCount')" prop="cellCount"
					:rules="[{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.videoWall.cellCount') }), trigger: 'blur' }]">
					<el-input-number v-model="state.ruleForm.cellCount" :min="1" :max="MAX_CELL_COUNT" :step="1" step-strictly controls-position="right" class="w100"
						:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.videoWall.cellCount') })" />
				</el-form-item>
				<el-form-item :label="$t('lz.entity.base.remark')" prop="remark"
					:rules="[{ max: 256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: 256 }), trigger: 'blur' }]">
					<el-input v-model="state.ruleForm.remark" type="textarea" :rows="3" :placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })" />
				</el-form-item>
				<p class="ewt-hint">
					<el-icon><ele-InfoFilled /></el-icon>{{ props.operateType === 'add' ? $t('lz.label.videoWall.layoutAddHint') : $t('lz.label.videoWall.layoutEditInfoHint') }}
				</p>
			</el-form>
			<template #footer>
				<span class="dialog-footer">
					<el-button @click="state.isShowDialog = false">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :loading="state.saving" @click="submit">{{ $t('lz.button.base.confirm') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="videoWallEditWallTopology">
import { reactive, ref } from 'vue';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { VwWallTopologyApi } from '/@/api-services/videoWall';
import type { VwUpdateWallTopologyInput } from '/@/api-services/videoWall';
import { OperateType } from '/@/types/constants';
import { blockLabel } from '/@/stores/its/videoWallTopology';

const { t: $t } = useI18n();

/** Số ô tối đa của 1 bố cục — khớp BE VwWallProfile.MaxCellCount. */
const MAX_CELL_COUNT = 100;
/** Lưới tối đa 1 khối — khớp BE VwWallProfile.MaxBlockCols / MaxBlockRows. */
const MAX_BLOCK_COLS = 32;
const MAX_BLOCK_ROWS = 16;

/** Lưới cột × hàng = n, gần vuông nhất, cột ≥ hàng (32 ⇒ 8 × 4); không có cặp nào vừa giới hạn khối ⇒ null. */
function gridOf(n: number): { cols: number; rows: number } | null {
	for (let rows = Math.floor(Math.sqrt(n)); rows >= 1; rows--) {
		if (n % rows === 0 && n / rows <= MAX_BLOCK_COLS && rows <= MAX_BLOCK_ROWS) return { cols: n / rows, rows };
	}
	return null;
}

/** Nội suy lưới từ số ô: 1 khối nếu chia được; không thì khối chính + khối phụ 1 hàng cho phần dư (vd 37 ⇒ 6 × 6 + 1 × 1). */
function blocksForCellCount(n: number): { cols: number; rows: number }[] {
	const g = gridOf(n);
	if (g) return [g];
	for (let r = 1; r < n; r++) {
		const main = gridOf(n - r);
		if (main) return [main, { cols: r, rows: 1 }];
	}
	return [{ cols: n, rows: 1 }];
}

interface Props {
	operateType: OperateType;
	title: string;
}
const props = defineProps<Props>();
const emits = defineEmits(['handleQuery']);

const ruleFormRef = ref();
const state = reactive({
	isShowDialog: false,
	saving: false,
	ruleForm: {} as VwUpdateWallTopologyInput,
	/** Lưới gửi kèm nguyên trạng (sửa: lưới đang lưu; nhân bản: lưới bố cục nguồn); thêm mới thì nội suy từ số ô lúc lưu. */
	blocks: [] as { cols: number; rows: number }[],
});

const openDialog = (form: VwUpdateWallTopologyInput, blocks: { cols: number; rows: number }[]) => {
	ruleFormRef.value?.resetFields();
	state.ruleForm = { ...form };
	state.blocks = blocks.map((b) => ({ ...b }));
	state.isShowDialog = true;
};

const submit = () => {
	ruleFormRef.value.validate(async (valid: boolean) => {
		if (!valid) return;
		// Thêm mới ⇒ lưới nội suy từ số ô (tổng ô khớp ngay, tab Bố cục tường không báo lệch); sửa / nhân bản giữ lưới đang có
		const blocks = props.operateType === 'add' ? blocksForCellCount(state.ruleForm.cellCount!) : state.blocks;
		const body: VwUpdateWallTopologyInput = {
			...state.ruleForm,
			code: state.ruleForm.code?.trim(),
			blocks: blocks.map((b, i) => ({ blockNo: i, name: blockLabel(i), cols: b.cols, rows: b.rows })),
		};
		state.saving = true;
		try {
			if (props.operateType === 'edit') await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyUpdatePost(body);
			else await getAPI(VwWallTopologyApi).apiVideowallVwwalltopologyAddPost({ ...body, id: '' });
			ElMessage.success($t('lz.message.base.saveSuccess'));
			state.isShowDialog = false;
			emits('handleQuery');
		} finally {
			state.saving = false;
		}
	});
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss">
.ewt-hint {
	display: flex;
	align-items: flex-start;
	gap: 6px;
	margin: 0;
	font-size: 12px;
	line-height: 1.45;
	color: var(--el-text-color-secondary);
	.el-icon {
		margin-top: 2px;
		flex-shrink: 0;
	}
}
</style>
