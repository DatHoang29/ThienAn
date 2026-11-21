<template>
	<el-popover placement="bottom" :width="props.width" trigger="hover">
		<template #reference>
			<el-text type="primary" class="cursor-default">
				<el-icon class="mr4"><ele-InfoFilled /></el-icon> 	
				<span v-if="!props.iconOnly">
					{{ $t('lz.label.base.detail') }}
				</span>
			</el-text>
		</template>
		<el-descriptions class="modify-record-descriptions" direction="vertical" :column="2" border>
			<el-descriptions-item :span="2" v-auth="stores.userInfos.accountType == '111'">
				<template #label>
					<el-text>
						<el-icon><ele-Key /></el-icon> {{ $t('lz.entity.base.id') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ props.data.id ?? $t('lz.label.base.none') }}</template>
					<el-tag>{{ props.data.id ?? $t('lz.label.base.none') }}</el-tag>
				</el-tooltip>
			</el-descriptions-item>
			<el-descriptions-item>
				<template #label>
					<el-text>
						<el-icon><ele-UserFilled /></el-icon> {{ $t('lz.entity.base.createBy') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ props.data.createUId ?? $t('lz.label.base.none') }}</template>
					<el-tag>{{ props.data.createUId ?? $t('lz.label.base.none') }}</el-tag>
				</el-tooltip>
			</el-descriptions-item>
			<el-descriptions-item>
				<template #label>
					<el-text>
						<el-icon><ele-Calendar /></el-icon> {{ $t('lz.entity.base.createTime') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ useDateFormat(props.data.createTime, DATE_TIME_FORMAT) ?? $t('lz.label.base.none') }}</template>
					<el-tag>{{ useDateFormat(props.data.createTime, DATE_TIME_FORMAT) ?? $t('lz.label.base.none') }}</el-tag>
				</el-tooltip>
			</el-descriptions-item>
			<el-descriptions-item>
				<template #label>
					<el-text>
						<el-icon><ele-UserFilled /></el-icon> {{ $t('lz.entity.base.updateBy') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ props.data.updateUId ?? $t('lz.label.base.none') }}</template>
					<el-tag>{{ props.data.updateUId ?? $t('lz.label.base.none') }}</el-tag>
				</el-tooltip>
			</el-descriptions-item>
			<el-descriptions-item>
				<template #label>
					<el-text>
						<el-icon><ele-Calendar /></el-icon> {{ $t('lz.entity.base.updateTime') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ props.data.updateTime ? useDateFormat(props.data.updateTime, DATE_TIME_FORMAT) : $t('lz.label.base.none') }}</template>
					<el-tag>{{ props.data.updateTime ? useDateFormat(props.data.updateTime, DATE_TIME_FORMAT) : $t('lz.label.base.none') }}</el-tag>
				</el-tooltip>
			</el-descriptions-item>
			<el-descriptions-item :span="2" :rowspan="2">
				<template #label>
					<el-text>
						<el-icon><ele-Tickets /></el-icon> {{ $t('lz.entity.base.remark') }}
					</el-text>
				</template>
				<el-tooltip placement="top" popper-class="modify-record-tooltip">
					<template #content>{{ props.data.remark ?? $t('lz.label.base.none') }}</template>
					<span class="modify-record-remark">{{ props.data.remark ?? $t('lz.label.base.none') }}</span>
				</el-tooltip>
			</el-descriptions-item>
		</el-descriptions>
	</el-popover>
</template>

<script lang="ts" setup>
import { useDateFormat } from '@vueuse/core'
import { useUserInfo } from '/@/stores/userInfo';
const stores = useUserInfo();
import { DATE_TIME_FORMAT } from '/@/types/formats'
const props = withDefaults(
	defineProps<{
		data: ModifyRecord;
		iconOnly?: boolean;
		/** Độ rộng popover (px) — tăng khi nội dung dài (UUID, tên người cập nhật dài). */
		width?: number | string;
	}>(),
	{
		iconOnly: false,
		width: 300,
	}
);
</script>

<style scoped lang="scss">
:global(.modify-record-tooltip) {
	max-width: min(480px, calc(100vw - 24px));
	overflow-wrap: anywhere;
	white-space: pre-wrap;
}

.modify-record-descriptions {
	:deep(.el-descriptions__table) {
		table-layout: fixed;
	}

	:deep(.el-tag) {
		max-width: 100%;
		min-width: 0;
	}

	:deep(.el-tag__content),
	.modify-record-remark {
		display: block;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.cursor-default {
	cursor: pointer;
	display: inline-flex;
	align-items: center;
	gap: 4px;

	.el-icon {
		margin-right: 4px;
	}
}
</style>
