<template>
	<section class="incident-history" :aria-label="$t('lz.label.tmsIncident.historyTitle')">
		<header class="history-toolbar">
			<span>
				{{ loading ? $t('lz.label.base.loading') : $t('lz.label.base.historySummary', { count: entryCount }) }}
			</span>
			<div>
				<el-button :icon="Refresh" text :disabled="loading" :title="$t('lz.button.base.refresh')" :aria-label="$t('lz.button.base.refresh')" @click="loadHistory(true)" />
				<el-button :icon="TopRight" text :title="$t('lz.label.tmsIncident.historyOpenFull')" :aria-label="$t('lz.label.tmsIncident.historyOpenFull')" @click="emits('open-full')" />
			</div>
		</header>

		<div class="history-scroll" :aria-busy="loading">
			<div v-if="error" class="history-error" role="alert">
				{{ error }}
				<el-button text :icon="Refresh" @click="loadHistory(true)">
					{{ $t('lz.button.base.retry') }}
				</el-button>
			</div>
			<div v-if="actorError" class="history-error" role="alert">
				{{ actorError }}
				<el-button text :icon="Refresh" @click="loadHistory(true)">
					{{ $t('lz.button.base.retry') }}
				</el-button>
			</div>

			<el-skeleton v-if="loading" :rows="3" animated />
			<el-timeline v-else-if="entryCount" class="history-timeline">
				<el-timeline-item
					v-for="item in visibleEntries"
					:key="item.key"
					:timestamp="item.date ? useDateFormat(item.date, DateTimeFormatEnum.DateTimeDefault).value : '—'"
					placement="top"
					:type="item.label ? 'warning' : item.incident ? 'primary' : undefined"
					:hollow="!item.incident"
				>
					<div class="history-item-heading">
						<strong>{{ item.title }}</strong>
						<TagInfo v-if="item.statusConfig" :config-item="item.statusConfig" />
						<el-tag v-else-if="item.statusLabel" type="info">
							{{ item.statusLabel }}
						</el-tag>
					</div>
					<p v-if="item.actorLabel" class="history-item-meta" :title="item.actorTooltip">
						{{ item.actorLabel }}
					</p>
					<details v-if="item.hasDetails" class="history-item-details">
						<summary>{{ $t('lz.label.base.detail') }}</summary>
						<dl>
							<template v-if="item.description">
								<dt>{{ $t('lz.entity.base.info') }}</dt>
								<dd>{{ item.description }}</dd>
							</template>
							<template v-if="item.remark">
								<dt>{{ $t('lz.entity.base.remark') }}</dt>
								<dd>{{ item.remark }}</dd>
							</template>
							<template v-if="item.contactLabel">
								<dt>{{ $t('lz.label.base.contact') }}</dt>
								<dd>{{ item.contactLabel }}</dd>
							</template>
						</dl>
					</details>
				</el-timeline-item>
			</el-timeline>
			<p v-else class="history-empty">
				{{ $t('lz.label.tmsIncident.historyEmpty') }}
			</p>

			<div v-if="!loading && entryCount > BATCH_SIZE" class="history-more">
				<span>{{ $t('lz.label.base.visibleEntryCount', { shown: shownCount, total: entryCount }) }}</span>
				<el-button v-if="nextBatchCount" size="small" @click="showMore">
					{{ $t('lz.button.base.showMoreEntries', { count: nextBatchCount }) }}
				</el-button>
			</div>
		</div>
	</section>
</template>

<script setup lang="ts" name="tmsIncidentHistory">
import { computed, onScopeDispose, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import type { TmsWorkExecuteOutput, TmsWorkExecuteHistoryOutput } from '/@/api-services/tms';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useCommonVarsAndFuncs } from '/@/stores/its/commonVarsAndFuncs';
import { useTmsStore } from '/@/stores/tmsStore';
import { BaseConfigTypeEnum, TmsIncidentStateEnum } from '/@/types/enums';
import type { IncidentRow } from '../incidentModel';
import { Refresh, TopRight } from '@element-plus/icons-vue';
import TagInfo from '/@/components/advanced/tag-info.vue';
import { useDateFormat } from '@vueuse/core';
import { DateTimeFormatEnum } from '/@/types/enums/baseEnum';

// Types and constants
interface Props {
	record: IncidentRow;
	version: number;
	active: boolean;
}

type WorkItem = TmsWorkExecuteOutput | TmsWorkExecuteHistoryOutput;
type NameMap = Record<string, string>;
type OrganizationNameSource = { orgId?: string | null; orgName?: string | null };
type TimelineEntry = {
	key: string;
	title: string;
	date?: Date | string | null;
	user?: string | null;
	organization?: string | null;
	state?: string | null;
	incident: boolean;
	/** Mốc không ứng với trạng thái nào (vd "Xảy ra"): hiện nhãn này làm tiêu đề, không có thẻ trạng thái. */
	label?: string;
	description?: string | null;
	remark?: string | null;
	contacts: string[];
};

const BATCH_SIZE = 20;
const TERMINAL_STATES = [TmsIncidentStateEnum.Finished, TmsIncidentStateEnum.Skipped, TmsIncidentStateEnum.Canceled];
const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
/** Lúc xảy ra sớm hơn lúc đăng ký quá mức này thì nhật ký tách thêm mốc "Xảy ra". */
const OCCURRED_GAP_MS = 60_000;

const props = defineProps<Props>();
const emits = defineEmits<{ 'open-full': [] }>();

// Dependencies
const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const commonVars = useCommonVarsAndFuncs();
const tmsStore = useTmsStore();

// Request state and cached display names
const workItems = ref<WorkItem[]>([]);
const loading = ref(false);
const errorKey = ref('');
const actorErrorKey = ref('');
const namesLoading = ref(false);
const userNames = ref<NameMap>({});
const loadedOrganizationNames = ref<NameMap>({});
const visibleCount = ref(BATCH_SIZE);
let requestId = 0;

// Derived data; translate here so changing the language updates existing entries and errors.
const error = computed(() => (errorKey.value ? $t(errorKey.value) : ''));
const actorError = computed(() => (actorErrorKey.value ? $t(actorErrorKey.value) : ''));
const requestKey = computed(() => [props.record.id, props.record.state, props.version, tmsStore.incidentRevision, tmsStore.catalogRevision].join(':'));

const organizationNames = computed(() => {
	const names: NameMap = {};
	const organizations: OrganizationNameSource[] = commonVars.userOrgList;
	for (const organization of organizations) {
		if (organization.orgId && organization.orgName) {
			names[String(organization.orgId).trim().toLowerCase()] = organization.orgName;
		}
	}
	return { ...names, ...loadedOrganizationNames.value };
});

const entries = computed(() => {
	const milestones = getIncidentMilestones(props.record);
	const work = workItems.value.map(createWorkEntry);
	return [...milestones, ...work].sort((left, right) => timestamp(right.date) - timestamp(left.date));
});

const entryCount = computed(() => entries.value.length);
const shownCount = computed(() => Math.min(visibleCount.value, entryCount.value));
const nextBatchCount = computed(() => Math.min(BATCH_SIZE, entryCount.value - shownCount.value));
const visibleEntries = computed(() =>
	entries.value.slice(0, visibleCount.value).map((item) => ({
		...item,
		title: item.label ?? (item.incident ? getStatusLabel(item) : item.title || $t('lz.label.tmsIncident.historyWorkItem')),
		statusConfig: item.label ? undefined : getStatusConfig(item),
		statusLabel: item.label ? '' : getStatusLabel(item),
		actorLabel: [getUserName(item.user), getOrganizationName(item.organization)].filter(Boolean).join(' · '),
		actorTooltip: [item.user, item.organization].filter(Boolean).join(' · '),
		hasDetails: Boolean(item.description || item.remark || item.contacts.length),
		contactLabel: item.contacts.join(', '),
	}))
);

// Timeline data helpers
function isUserOrOrganizationId(value?: string | null) {
	return UUID_PATTERN.test(value?.trim() ?? '');
}

function timestamp(date?: Date | string | null) {
	const value = date instanceof Date ? date.getTime() : Date.parse(date ?? '');
	return Number.isFinite(value) ? value : 0;
}

function createMilestone(key: string, state: string, date?: Date | null, user?: string | null): TimelineEntry {
	return { key, title: '', state, date, user, incident: true, contacts: [] };
}

function getIncidentMilestones(record: IncidentRow): TimelineEntry[] {
	// Đăng ký = lúc sự cố được ghi vào hệ thống (createdDate) bởi createdBy. Trước đây lấy startDate (lúc xảy ra) nên
	// sự cố tạo từ sự kiện cũ hiện "Đăng ký" ở giờ của sự kiện. Thiếu createdDate (dữ liệu cũ) thì vẫn dùng startDate.
	const milestones = [createMilestone('registration', TmsIncidentStateEnum.Registration, record.createdDate || record.startDate, record.createdBy)];

	// Xảy ra trước lúc đăng ký đáng kể (tạo từ sự kiện cũ, người trực khai giờ sớm hơn) ⇒ thêm mốc "Xảy ra" theo
	// startDate. Sự cố VDS tự tạo hai mốc gần như trùng nên không thêm dòng.
	const occurredAt = timestamp(record.startDate);
	const registeredAt = timestamp(record.createdDate);
	if (occurredAt && registeredAt && registeredAt - occurredAt > OCCURRED_GAP_MS) {
		milestones.push({ ...createMilestone('occurred', '', record.startDate), label: $t('lz.label.tmsIncident.historyOccurred') });
	}

	if (record.validatedDate || record.validatedBy) {
		milestones.push(createMilestone('verified', TmsIncidentStateEnum.Verified, record.validatedDate, record.validatedBy));
	}
	if (record.approvedDate || record.approvedBy) {
		milestones.push(createMilestone('approved', TmsIncidentStateEnum.InProgress, record.approvedDate, record.approvedBy));
	}
	if (TERMINAL_STATES.includes(record.state as TmsIncidentStateEnum)) {
		milestones.push(createMilestone('finished', record.state!, record.finishedDate, record.finishedBy));
	}

	return milestones;
}

function formatContacts(contacts: WorkItem['workContactList']): string[] {
	return (contacts ?? [])
		.filter(Boolean)
		.map((contact) => {
			const phone = contact.contactPhone ? `(${contact.contactPhone})` : '';
			return [contact.contactName, phone].filter(Boolean).join(' ');
		})
		.filter(Boolean);
}

function createWorkEntry(item: WorkItem, index: number): TimelineEntry {
	return {
		key: `work:${item.id ?? index}:${index}`,
		title: item.workName || item.code || '',
		date: item.executeDate || item.createTime,
		user: item.executeBy,
		organization: item.executeOrg,
		state: item.state,
		incident: false,
		description: item.description,
		remark: item.remark,
		contacts: formatContacts(item.workContactList),
	};
}

// Display helpers
function getStatusConfig(item: TimelineEntry) {
	const configType = item.incident ? BaseConfigTypeEnum.TmsIncidentStatus : BaseConfigTypeEnum.TmsWorkExecuteStatus;
	const config = baseConfig.getConfigItemByCode(configType, item.state ?? '');
	return config.code ? config : undefined;
}

function getStatusLabel(item: TimelineEntry) {
	const state = item.state ?? '';
	const key = item.incident ? commonVars.getIncidentStateName(state) : commonVars.getWorkExecuteStateName(state);
	return key ? $t(key) : state || $t('lz.label.base.unknown');
}

function resolveActorName(value: string | null | undefined, names: NameMap, fallback: string) {
	const key = value?.trim();
	if (!key) return '';
	if (Object.prototype.hasOwnProperty.call(names, key.toLowerCase())) return names[key.toLowerCase()];
	if (!isUserOrOrganizationId(key)) return key;
	if (namesLoading.value) return $t('lz.label.base.loadingNames');
	return fallback;
}

function getUserName(value?: string | null) {
	return resolveActorName(value, userNames.value, $t('lz.label.base.unknownOperator'));
}

function getOrganizationName(value?: string | null) {
	return resolveActorName(value, organizationNames.value, $t('lz.label.base.unknownOrganization'));
}

// Requests and paging
function isCurrentRequest(request: number, key: string) {
	return request === requestId && key === requestKey.value && props.active;
}

async function loadActorNames(request: number, key: string, force: boolean) {
	const needUsers = entries.value.some((item) => isUserOrOrganizationId(item.user));
	const needOrganizations = entries.value.some((item) => isUserOrOrganizationId(item.organization));
	namesLoading.value = needUsers || needOrganizations;

	// Let the shared store check TTL, even if this component already has names.
	const usersRequest = needUsers ? tmsStore.getHistoryUserNames(force) : Promise.resolve(userNames.value);
	const organizationsRequest = needOrganizations ? tmsStore.getHistoryOrganizationNames(force) : Promise.resolve(loadedOrganizationNames.value);
	const [users, organizations] = await Promise.allSettled([usersRequest, organizationsRequest]);
	if (!isCurrentRequest(request, key)) return;

	userNames.value = users.status === 'fulfilled' ? users.value : {};
	loadedOrganizationNames.value = organizations.status === 'fulfilled' ? organizations.value : {};
	const failed = users.status === 'rejected' || organizations.status === 'rejected';
	actorErrorKey.value = failed ? 'lz.message.base.actorNamesLoadFailed' : '';
	namesLoading.value = false;
}

async function loadHistory(force = false) {
	if (!props.active || !props.record.id) return;
	const request = ++requestId;
	const key = requestKey.value;
	const { id, state } = props.record;

	loading.value = true;
	errorKey.value = '';
	actorErrorKey.value = '';
	namesLoading.value = false;
	workItems.value = [];
	visibleCount.value = BATCH_SIZE;

	try {
		const items = await tmsStore.getIncidentHistory(id, state, force);
		if (!isCurrentRequest(request, key)) return;
		workItems.value = items;
		void loadActorNames(request, key, force);
	} catch {
		if (isCurrentRequest(request, key)) errorKey.value = 'lz.message.tmsIncident.historyLoadFailed';
	} finally {
		if (isCurrentRequest(request, key)) loading.value = false;
	}
}

function showMore() {
	visibleCount.value += BATCH_SIZE;
}

function cancelRequests() {
	++requestId;
	loading.value = false;
	namesLoading.value = false;
}

// Lifecycle
watch(
	() => [requestKey.value, props.active],
	() => {
		if (!props.active) {
			cancelRequests();
			return;
		}
		void loadHistory();
	},
	{ immediate: true }
);
onScopeDispose(cancelRequests);
</script>

<style scoped lang="scss">
.incident-history {
	display: flex;
	flex-direction: column;
	flex: 1;
	min-height: 0;
	overflow: hidden;
}
.history-toolbar {
	display: flex;
	align-items: center;
	justify-content: space-between;
	flex-shrink: 0;
	gap: 6px;
	padding-bottom: 6px;
	color: var(--monitor-muted);
	font-size: calc(11px + var(--monitor-panel-font-increment, 0px));
}
.history-toolbar > div {
	display: flex;
	gap: 3px;
}
.history-toolbar .el-button {
	width: 24px;
	min-height: 24px;
	padding: 3px;
}
.history-scroll {
	flex: 1;
	min-height: 0;
	overflow-y: auto;
	scrollbar-width: thin;
	overscroll-behavior: contain;
	padding: 2px 4px 4px 0;
}
.history-timeline {
	margin: 0;
	padding: 0 0 0 4px;
}
.history-timeline :deep(.el-timeline-item) {
	padding-bottom: 14px;
}
.history-timeline :deep(.el-timeline-item__wrapper) {
	padding-left: 23px;
}
.history-timeline :deep(.el-timeline-item__timestamp) {
	font-size: calc(11px + var(--monitor-panel-font-increment, 0px));
	margin-bottom: 5px;
	color: var(--monitor-muted);
}
.history-item-heading {
	display: flex;
	align-items: center;
	flex-wrap: wrap;
	gap: 4px 8px;
}
.history-item-heading strong {
	flex: 1;
	font-size: calc(13px + var(--monitor-panel-font-increment, 0px));
	font-weight: 600;
	overflow-wrap: anywhere;
}
.history-item-meta {
	margin-top: 3px;
	font-size: calc(12px + var(--monitor-panel-font-increment, 0px));
	color: var(--monitor-muted);
	overflow-wrap: anywhere;
}
.history-item-details {
	margin-top: 4px;
	font-size: calc(12px + var(--monitor-panel-font-increment, 0px));
}
.history-item-details summary {
	color: var(--monitor-primary);
	cursor: pointer;
}
.history-item-details dl {
	margin: 5px 0 0;
	padding: 6px 8px;
	background: var(--monitor-subtle);
	border-radius: 4px;
}
.history-item-details dt {
	color: var(--monitor-muted);
	font-size: calc(11px + var(--monitor-panel-font-increment, 0px));
}
.history-item-details dd {
	margin: 1px 0 5px;
	white-space: pre-wrap;
	overflow-wrap: anywhere;
}
.history-more {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 8px;
	padding-top: 6px;
	border-top: 1px solid var(--monitor-border);
	color: var(--monitor-muted);
	font-size: calc(11px + var(--monitor-panel-font-increment, 0px));
}
.history-error {
	margin-bottom: 8px;
	padding: 6px 8px;
	font-size: calc(12px + var(--monitor-panel-font-increment, 0px));
	background: var(--monitor-warning-soft);
	color: var(--monitor-warning);
	border-radius: 4px;
}
.history-empty {
	padding: 16px 0;
	text-align: center;
	color: var(--monitor-muted);
	font-size: calc(13px + var(--monitor-panel-font-increment, 0px));
}
</style>
