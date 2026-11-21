<template>
	<article class="dashboard-card" v-auth="'cctvDashboardHome:view'">
		<header>
			<div class="header-main">
				<div class="card-icon">
					<SvgIcon name="ele-DataBoard" />
				</div>
				<div>
					<h3>{{ dashboard.name }}</h3>
					<p>{{ dashboard.remark }}</p>
				</div>
			</div>
			<div v-if="dashboard.id !== 'all-cameras'" class="card-actions">
				<el-dropdown trigger="click" @command="onActionCommand">
					<el-button :aria-label="t('lz.label.base.action')" size="small" text>
						<i class="fa fa-ellipsis-v"></i>
					</el-button>
					<template #dropdown>
						<el-dropdown-menu>
							<el-dropdown-item command="edit" v-auth="'cctvDashboardHome:update'">
								<el-icon><ele-Edit /></el-icon> {{
									t('lz.button.base.edit') }}</el-dropdown-item>
							<el-dropdown-item command="duplicate" v-auth="'cctvDashboardHome:add'">
								<el-icon><ele-CopyDocument /></el-icon> {{
									t('lz.button.base.copy') }}</el-dropdown-item>
							<el-dropdown-item command="delete" v-auth="'cctvDashboardHome:delete'">
								<el-icon><ele-Delete /></el-icon> {{
									t('lz.button.base.delete') }}</el-dropdown-item>
						</el-dropdown-menu>
					</template>
				</el-dropdown>
			</div>
		</header>
		<div class="tags">
			<span v-for="tag in dashboard.tags" :key="tag">{{ tag }}</span>
		</div>
		<footer>
			<div>
				<strong>{{ t('lz.label.cctvDashboard.cameraCount', { count: dashboard.cameraCount ?? 0 }) }}</strong>
				<span>{{ t('lz.label.cctvDashboard.updatedAt', { date: dashboard.updatedAt }) }}</span>
			</div>
			<div>
				<el-button type="primary" @click="$emit('view', dashboard.id)" v-auth="'cctvDashboardHome:view'">
					{{ t('lz.button.base.view') }}
				</el-button>
			</div>
		</footer>
	</article>
</template>

<script setup lang="ts" name="cctvDashboardCard">
import type { DashboardCCTVOutput } from '/@/views/cctv/dashboard/types';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();

const emit = defineEmits<{
	(e: 'view', id: string): void;
	(e: 'edit', id: string): void;
	(e: 'duplicate', id: string): void;
	(e: 'delete', id: string): void;
}>();

const props = defineProps<{
	dashboard: DashboardCCTVOutput;
}>();

const onActionCommand = (command: string | number | object) => {
	if (typeof command !== 'string') return;
	if (command === 'edit') emit('edit', props.dashboard.id);
	if (command === 'duplicate') emit('duplicate', props.dashboard.id);
	if (command === 'delete') emit('delete', props.dashboard.id);
};
</script>

<style scoped lang="scss">
.dashboard-card {
	min-height: 180px;
	border: 1px solid #e5e7eb;
	border-radius: 8px;
	background: #ffffff;
	padding: 18px;
	display: flex;
	flex-direction: column;
	gap: 16px;

	header {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 12px;
	}

	.header-main {
		display: flex;
		gap: 14px;
		min-width: 0;
	}

	h3 {
		margin: 0;
		color: #101828;
		font-size: 18px;
		line-height: 24px;
	}

	p {
		margin: 5px 0 0;
		color: #667085;
		font-size: 13px;
		line-height: 18px;
	}

	footer {
		border-top: 1px solid #e5e7eb;
		padding-top: 16px;
		margin-top: auto;
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 12px;

		strong,
		span {
			display: block;
		}

		strong {
			color: #344054;
			font-size: 14px;
		}

		span {
			color: #667085;
			font-size: 12px;
			margin-top: 6px;
		}
	}
}

.card-actions {
	margin-left: auto;
}

.card-icon {
	width: 44px;
	height: 44px;
	min-width: 44px;
	border-radius: 10px;
	background: #eef4ff;
	color: #2563eb;
	display: grid;
	place-items: center;
}

.tags {
	display: flex;
	flex-wrap: wrap;
	gap: 8px;

	span {
		border-radius: 999px;
		border: 1px solid #d0d5dd;
		padding: 5px 12px;
		color: #344054;
		font-size: 12px;
	}
}
</style>
