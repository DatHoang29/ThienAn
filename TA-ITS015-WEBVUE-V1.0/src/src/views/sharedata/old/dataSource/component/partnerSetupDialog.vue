<template>
	<el-dialog
		v-model="visible"
		width="580px"
		destroy-on-close
		draggable
		class="partner-setup-dialog"
	>
		<template #header>
			<div class="psd-header">
				<span>{{ $t('lz.title.sharedataDataSource.partnerSetupModal') }}</span>
			</div>
		</template>

		<el-form size="small" label-position="left" label-width="110px" class="psd-form">
			<el-form-item :label="$t('lz.label.sharedataDataSource.partnerSetupTarget')">
				<el-select
					v-model="selectedPartnerId"
					size="small"
					class="w100"
					filterable
					popper-class="partner-setup-select-popper"
					:placeholder="$t('lz.placeholder.sharedataDataSource.selectPartner')"
				>
					<el-option
						v-for="p in candidatePartners"
						:key="p.partnerId"
						:label="partnerOptionLabel(p)"
						:value="p.partnerId"
					/>
				</el-select>
			</el-form-item>

			<el-form-item :label="$t('lz.label.sharedataDataSource.partnerSetupSource')" class="setup-source-form-item">
				<div class="setup-source-wrapper">
					<div class="setup-radio-line">
						<el-radio v-model="sourceType" value="tpl">
							{{ $t('lz.label.sharedataDataSource.partnerSetupFromStandard') }}
							<span v-if="props.templateVersion" class="chip-ver">v{{ props.templateVersion }}</span>
						</el-radio>
					</div>
					<div class="setup-radio-line custom-source-line">
						<el-radio v-model="sourceType" value="other" :disabled="!customPartners.length">
							{{ $t('lz.label.sharedataDataSource.partnerSetupFromPartnerRadio') }}
						</el-radio>
						<el-select
							v-model="selectedCustomPartnerId"
							size="small"
							:disabled="sourceType !== 'other' || !customPartners.length"
							popper-class="partner-setup-select-popper"
							class="setup-other-select"
						>
							<el-option
								v-for="p in customPartners"
								:key="p.partnerId"
								:label="p.partnerCode || p.partnerName"
								:value="p.partnerId"
							/>
						</el-select>
					</div>
				</div>
			</el-form-item>

			<div class="alert info">
				{{ props.alertText || $t('lz.label.sharedataDataSource.partnerSetupAlert') }}
			</div>
		</el-form>

		<template #footer>
			<span class="dialog-footer">
				<el-button @click="visible = false">{{ $t('lz.button.base.cancel') }}</el-button>
				<el-button type="primary" :disabled="!selectedPartnerId" @click="handleConfirm">
					{{ $t('lz.button.sharedataDataSource.createAndOpenTab') }}
				</el-button>
			</span>
		</template>
	</el-dialog>
</template>

<script lang="ts" setup name="sharedataPartnerSetupDialog">
import { computed, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';

const { t: $t } = useI18n();

const props = defineProps<{
	modelValue: boolean;
	packetCode: string;
	partners: Array<{
		partnerId: string;
		partnerCode?: string;
		partnerName: string;
		isCustom: boolean;
	}>;
	templateVersion?: number | string;
	/** Ghi chú cuối hộp thoại; trống = ghi chú của câu truy vấn chiều gửi (dùng lại hộp thoại cho câu ghi chiều nhận). */
	alertText?: string;
}>();

const emit = defineEmits<{
	(e: 'update:modelValue', val: boolean): void;
	(e: 'confirm', payload: { partnerId: string; sourcePartnerId: string }): void;
}>();

const visible = computed({
	get: () => props.modelValue,
	set: (val: boolean) => emit('update:modelValue', val),
});

const selectedPartnerId = ref('');
const sourceType = ref<'tpl' | 'other'>('tpl');
const selectedCustomPartnerId = ref('');

const candidatePartners = computed(() => (props.partners || []).filter((p) => !p.isCustom));
const customPartners = computed(() => (props.partners || []).filter((p) => p.isCustom));

const partnerOptionLabel = (p: { partnerCode?: string; partnerName: string }) => {
	const namePart = p.partnerCode ? `${p.partnerCode} — ${p.partnerName}` : p.partnerName;
	return `${namePart} (${$t('lz.label.sharedataDataSource.inUseTpl')})`;
};

watch(
	() => props.modelValue,
	(isOpen) => {
		if (isOpen) {
			selectedPartnerId.value = candidatePartners.value[0]?.partnerId || '';
			sourceType.value = 'tpl';
			selectedCustomPartnerId.value = customPartners.value[0]?.partnerId || '';
		}
	}
);

const handleConfirm = () => {
	if (!selectedPartnerId.value) return;
	const sourcePartnerId = sourceType.value === 'other' ? (selectedCustomPartnerId.value || 'standard') : 'standard';
	emit('confirm', {
		partnerId: selectedPartnerId.value,
		sourcePartnerId,
	});
	visible.value = false;
};
</script>

<style scoped lang="scss">
:global(.partner-setup-select-popper .el-select-dropdown__item) {
	font-size: 12px;
	height: 28px;
	line-height: 28px;
	padding: 0 10px;
}

.psd-form {
	:deep(.el-select .el-input__inner),
	:deep(.el-select .el-select__input) {
		font-size: 12px;
	}
}

.psd-header {
	display: flex;
	align-items: center;
	font-weight: 600;
	font-size: 15px;
	/* Nền tiêu đề hộp thoại màu xanh: chữ trắng như các hộp thoại khác */
	color: #fff;
}

/* Nút Tạo & mở tab bọc trong span (để có tooltip) nên mất khoảng cách mặc định giữa hai nút */
.dialog-footer {
	display: inline-flex;
	align-items: center;
	gap: 12px;
}

.setup-source-wrapper {
	display: flex;
	flex-direction: column;
	width: 100%;
}

.setup-radio-line {
	display: flex;
	align-items: center;
	min-height: 32px;
}

.custom-source-line {
	gap: 12px;

	.setup-other-select {
		flex: 1;
		max-width: 220px;
	}
}

.chip-ver {
	display: inline-block;
	background: #eaf1f8;
	border: 1px solid #bcd3e8;
	color: #1d4f7c;
	border-radius: 10px;
	padding: 0 7px;
	font-size: 11.5px;
	line-height: 18px;
	margin-left: 6px;
	vertical-align: middle;
}

.alert.info {
	border: 1px solid #bcd3e8;
	background: #eaf1f8;
	color: #1d4f7c;
	border-radius: 4px;
	padding: 8px 12px;
	font-size: 13px;
	line-height: 1.5;
	margin-top: 10px;
	margin-bottom: 4px;
	white-space: pre-line;
	cursor: help;
	transition: background-color 0.2s ease, border-color 0.2s ease;

	&:hover {
		background: #e2eef8;
		border-color: #a4c4e0;
	}
}
</style>
