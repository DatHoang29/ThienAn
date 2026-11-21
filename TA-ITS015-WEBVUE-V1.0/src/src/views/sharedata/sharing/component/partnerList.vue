<template>
	<div class="ptn-list">
		<div class="ptn-list__head">
			<span class="ptn-list__title">{{ $t('lz.label.sharedataPartner.title') }}</span>
			<el-button v-if="canOperate" type="primary" size="small" icon="ele-Plus" v-auth="'sharedataSharing:add'"
				@click="handleAdd">{{ $t('lz.button.sharedataPartner.add') }}</el-button>
		</div>
		<el-scrollbar class="ptn-list__body">
			<div v-for="partner in partners" :key="partner.id" class="ptn-item" :class="{ 'is-selected': partner.id === selectedId }"
				@click="emit('select', partner.id)">
				<div class="ptn-item__row">
					<el-tooltip
						:content="partner.name"
						effect="dark"
						placement="top"
						:show-after="300"
						popper-class="partner-list-tooltip"
					>
						<span class="ptn-item__name">{{ partner.name }}</span>
					</el-tooltip>
					<div v-if="canOperate" class="ptn-item__acts" @click.stop>
					
						<el-tooltip :content="$t('lz.button.base.copy')" placement="top" :auto-close="1000">
							<el-button icon="ele-CopyDocument" text type="warning" v-auth="'sharedataSharing:add'"
								:aria-label="$t('lz.button.base.copy')" @click.stop="handleCopy(partner)"> </el-button>
						</el-tooltip>
						<el-tooltip :content="$t('lz.button.base.edit')" placement="top" :auto-close="1000">
							<el-button icon="ele-Edit" text type="primary" v-auth="'sharedataSharing:update'"
								:aria-label="$t('lz.button.base.edit')" @click.stop="handleEdit(partner)"> </el-button>
						</el-tooltip>
						<el-tooltip
							:content="partner.sessionState === SESSION_CONNECTED ? $t('lz.message.sharedataPartner.disconnectBeforeDelete') : $t('lz.button.base.delete')"
							placement="top" :auto-close="1000">
							<el-button icon="ele-Delete" text type="danger" v-auth="'sharedataSharing:delete'"
								:disabled="partner.sessionState === SESSION_CONNECTED"
								:aria-label="$t('lz.button.base.delete')" @click.stop="handleDelete(partner)"> </el-button>
						</el-tooltip>
							<el-tooltip placement="top" :auto-close="1000">
							<ModifyRecord :icon-only="true" :data="partner" />
						</el-tooltip>
					</div>
				</div>
				<div class="ptn-item__meta">
					<!-- <TagInfo class="ptn-item__state"
						:config-item="getConfigItemByCode(BaseConfigTypeEnum.SharedataSessionState, partner.sessionState)" /> -->
						<span class="ptn-item__code">{{ partner.code }}</span>
				</div>
			</div>
			<el-empty v-if="!partners.length" :description="$t('lz.message.sharedataSharing.noPartner')" :image-size="80" />
		</el-scrollbar>

		<EditPartner ref="editPartnerRef" :title="state.title" :operateType="state.operateType"
			@handleQuery="emit('handleQuery')" />
	</div>
</template>

<script lang="ts" setup name="sharedataPartnerList">
import { defineAsyncComponent, reactive, ref } from 'vue';
import { ElMessageBox } from 'element-plus';
import { useI18n } from 'vue-i18n';
import EditPartner from './editPartner.vue';
import { SessionState } from '/@/api-services/shareData';
import type { ShareDataPartnerOutput, ShareDataSessionOutput } from '/@/api-services/shareData';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { OperateType } from '/@/types/constants';
import ModifyRecord from '/@/components/table/modifyRecord.vue';

const SESSION_CONNECTED = SessionState.NUMBER_1;

const TagInfo = defineAsyncComponent(() => import('/@/components/advanced/tag-info.vue'));
const { t: $t } = useI18n();

interface Props {
	partners: ShareDataPartnerOutput[];
	sessions: ShareDataSessionOutput[];
	selectedId?: string;
	canOperate: boolean;
}
const props = defineProps<Props>();

interface Emits {
	(e: 'select', id: string): void;
	(e: 'handleQuery'): void;
	(e: 'delete', row: ShareDataPartnerOutput): void;
}
const emit = defineEmits<Emits>();

const editPartnerRef = ref<InstanceType<typeof EditPartner>>();
const state = reactive({
	title: '',
	operateType: 'add' as OperateType,
});
// Open add dialog
const handleAdd = () => {
	state.title = $t('lz.label.base.titleAdd', { name: $t('lz.label.sharedataPartner.name') });
	state.operateType = 'add';
	editPartnerRef.value?.openDialog({}, 'add');
};

// Open copy dialog
const handleCopy = (row: ShareDataPartnerOutput) => {
	state.title = $t('lz.label.base.titleCopy', { name: $t('lz.label.sharedataPartner.name') });
	state.operateType = 'copy';
	editPartnerRef.value?.openDialog(row, 'copy');
};

// Open edit dialog
const handleEdit = (row: ShareDataPartnerOutput) => {
	state.title = $t('lz.label.base.titleEdit', { name: $t('lz.label.sharedataPartner.name') });
	state.operateType = 'edit';
	editPartnerRef.value?.openDialog(row, 'edit');
};

// Delete
const handleDelete = (row: ShareDataPartnerOutput) => {
	ElMessageBox.confirm(
		$t('lz.message.sharedataPartner.confirmDelete', { name: row.name }),
		$t('lz.message.base.warning'),
		{
			confirmButtonText: $t('lz.button.base.confirm'),
			cancelButtonText: $t('lz.button.base.cancel'),
			type: 'warning',
		}
	)
		.then(() => {
			emit('delete', row);
		})
		.catch(() => { });
};
</script>

<style lang="scss">
/* Tooltips render in <body>: styled here, not in the scoped block */
.el-popper.partner-list-tooltip {
	box-sizing: border-box;
	width: max-content;
	max-width: 28vw;
	line-height: 1.5;
	white-space: pre-line;
	overflow-wrap: anywhere;
}
</style>

<style scoped lang="scss">
.ptn-list {
	display: flex;
	flex-direction: column;
	height: 100%;
}

.ptn-list__head {
	display: flex;
	align-items: center;
	justify-content: space-between;
	padding: 8px 10px;
	border-bottom: 1px solid var(--el-border-color-light);
}

.ptn-list__title {
	font-weight: 600;
	color: var(--el-text-color-primary);
}

.ptn-list__body {
	flex: 1;
}

.ptn-item {
	padding: 8px 10px;
	cursor: pointer;
	border-bottom: 1px solid var(--el-border-color-lighter);
}

.ptn-item:hover {
	background: var(--el-fill-color-light);
}

.ptn-item.is-selected {
	background: var(--el-color-primary-light-9);
	border-left: 3px solid var(--el-color-primary);
}

.ptn-item__row {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 6px;
	min-height: 24px;
}

.ptn-item__name {
	font-weight: 500;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

/* Nút thao tác luôn có trong DOM (giữ thứ tự tab của bàn phím), làm mờ khi không quan tâm
   và rõ hẳn khi hover / đang chọn để danh sách không bị rối. */
.ptn-item__acts {
	display: flex;
	align-items: center;
	gap: 2px;
	flex-shrink: 0;
	opacity: 0.45;
	transition: opacity 0.15s;
}

.ptn-item:hover .ptn-item__acts,
.ptn-item.is-selected .ptn-item__acts,
.ptn-item__acts:focus-within {
	opacity: 1;
}

.ptn-item__acts :deep(.el-button) {
	height: 24px;
	width: 24px;
	padding: 0;
	margin-left: 0;
}

.ptn-item__meta {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-top: 4px;
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

/* Tag trạng thái thu nhỏ cho vừa dòng meta 12px. */
.ptn-item__meta :deep(.ptn-item__state) {
	flex-shrink: 0;
	height: 18px;
	padding: 0 5px;
	font-size: 11px;
	line-height: 18px;
}

.ptn-item__code {
	flex: 1;
	min-width: 0;
	overflow: hidden;
	text-overflow: ellipsis;
	white-space: nowrap;
}

.ptn-item__counters {
	flex-shrink: 0;
	font-variant-numeric: tabular-nums;
}
</style>
