<template>
	<div class="sharedata-edit-subscription-container">
		<el-dialog
			v-model="state.isShow"
			draggable
			:close-on-click-modal="!state.loading"
			:close-on-press-escape="!state.loading"
			:show-close="!state.loading"
			width="640px"
			class="sharedata-edit-subscription-dialog"
		>
			<template #header>
				<div style="color: var(--el-color-white)">
					<el-icon size="16"
						style="margin-right: 3px; display: inline; vertical-align: middle"><ele-Plus /></el-icon>
					<span> {{ state.isEdit ? $t('lz.label.sharedataSubscription.title') : $t('lz.label.sharedataSubscription.addTitle') }} </span>
				</div>
			</template>

			<!-- Chiều đăng ký = TAB PANE: tab "Gửi đi" và tab "Nhận về", mỗi tab là một form hoàn chỉnh.
				 Chỉ tab đang mở mới dựng el-form nên trong DOM luôn chỉ có một instance form đang sống,
				 formRef vì thế luôn trỏ đúng form của chiều đang xem. -->
			<el-tabs v-model="directionTab" class="direction-tabs" @tab-change="resolve">
				<el-tab-pane v-for="tab in DIRECTION_TABS" :key="tab.name" :name="tab.name" :label="tab.label" style="height: 400px; overflow-y: auto; overflow-x: hidden"
					:disabled="state.isEdit && directionTab !== tab.name">
					<el-form v-if="directionTab === tab.name" :ref="setFormRef" :model="state.form" :rules="rules"
						label-width="auto" label-position="left" autocomplete="off">
						<el-row :gutter="10">
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataSubscription.partnerId')" prop="partnerId">
									<el-select v-model="state.form.partnerId" :placeholder="$t('lz.label.sharedataSubscription.partnerId')" filterable clearable
										class="w100" :disabled="state.isEdit" @change="resolve">
										<el-option v-for="p in props.partners" :key="p.id" :label="`${p.code} - ${p.name}`"
											:value="p.id" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataSubscription.datatypeId')" prop="datatypeId">
									<el-select v-model="state.form.datatypeId" :placeholder="$t('lz.label.sharedataSubscription.datatypeId')" filterable
										clearable class="w100" :disabled="state.isEdit" @change="onDatatypeChange">
										<el-option v-for="d in packetList"
											:key="d.id" :label="codeNameLabel(d)" :value="d.id">
											<span>{{ codeNameLabel(d) }}</span>
										</el-option>
									</el-select>
								</el-form-item>
							</el-col>


							<!-- Ánh xạ áp dụng được SUY RA từ (đối tác × gói tin × chiều), không lưu trên đăng ký.
								 Ưu tiên hồ sơ riêng của đối tác, không có thì rơi về hồ sơ dùng chung. -->
							<!-- <el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-alert v-if="state.mappingName" type="success" :closable="false">
									<template #title>
										<div class="keys-title">
											{{ $t('lz.label.sharedataSubscription.appliedMapping') }}
											<el-tag size="small" type="success">{{ state.mappingName }}</el-tag>
											<el-tag v-if="state.mappingCode" size="small" effect="plain" class="ml5">{{
												state.mappingCode }}</el-tag>

										</div>
								
									</template>
								</el-alert>
								<el-alert v-else-if="state.mappingLoading" type="info" :closable="false"
									:title="$t('lz.label.sharedataSubscription.resolvingMapping')" />
								<el-alert v-else-if="state.form.partnerId && state.form.datatypeId" type="warning"
									:closable="false"
									:title="$t('lz.label.sharedataSubscription.noActiveMappingWarning')" />
								<el-alert v-else type="info" :closable="false"
									:title="$t('lz.label.sharedataSubscription.selectPartnerAndDatatypeHint')" />
							</el-col> -->

							<!-- Chỉ còn chế độ Định kỳ — ô chọn chế độ và nhóm trường của chế độ Theo sự kiện
								 được giữ lại dạng comment để sau này cần thì mở lại. -->
							<!--
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item label="Chế độ" prop="mode">
									<el-radio-group v-model="state.form.mode">
										<el-radio-button :label="SharedataSubModeEnum.Event">Theo sự kiện</el-radio-button>
										<el-radio-button :label="SharedataSubModeEnum.Periodic">Định kỳ</el-radio-button>
									</el-radio-group>
								</el-form-item>
							</el-col>

							<template v-if="Number(state.form.mode) === SharedataSubModeEnum.Event">
								<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
									<el-form-item label="Giãn cách tối thiểu (giây)">
										<el-input-number v-model="state.form.debounceSec" :min="feConfig.subscriptionDebounce.min" :max="feConfig.subscriptionDebounce.max" :step="1" :precision="0"
											class="w100" controls-position="right" />
									</el-form-item>
								</el-col>
								<el-col :span="24" class="mb20" v-if="!matchedEventSources.length">
									<el-alert type="info" :closable="false"
										title="Chưa có nguồn sự kiện phù hợp. Danh mục nguồn sự kiện do hệ thống cấu hình (xem trang Nguồn sự kiện)." />
								</el-col>
							</template>
							-->
							<template v-if="!isInbound">
								<Scheduler
									ref="schedulerRef"
									v-model:kind="scheduleKind"
									v-model:interval-seconds="state.form.intervalSeconds"
									v-model:start-time="startTime"
									v-model:end-time="endTime"
									v-model:days-of-week="daysOfWeek"
									:interval-min="feConfig.subscriptionInterval.min"
									:interval-max="feConfig.subscriptionInterval.max"
									:allowed-kinds="['continuous', 'daily']"
								>
									<template #header-extra>
										<el-col :xs="24" :sm="24" :md="10" :lg="10" :xl="10" class="mb20">
											<el-form-item>
												<template #label>
													{{ $t('lz.label.sharedataSubscription.sendImmediately') }}
													<el-tooltip effect="dark" placement="top">
														<template #content>
															<div style="max-width: 280px; line-height: 1.5; white-space: normal;">
																{{ $t('lz.label.sharedataSubscription.sendImmediatelyHint') }}
															</div>
														</template>
														<el-icon class="lbl-help"><ele-QuestionFilled /></el-icon>
													</el-tooltip>
												</template>
												<el-checkbox v-model="state.form.sendOnNewData" />
											</el-form-item>
										</el-col>
									</template>
								</Scheduler>
							</template>

							<!-- Chiều nhận là bị động: gói tin do đối tác đẩy vào hộp nhận, không có lịch gửi để cấu hình. -->
							<el-col v-if="isInbound" :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-alert type="info" :closable="false"
									:title="$t('lz.label.sharedataSubscription.inboundPassiveHint')" />
							</el-col>

							<!-- <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item label="Định dạng" prop="format">
									<el-select v-model="state.form.format" placeholder="Chọn định dạng" filterable clearable
										class="w100">
										<el-option :label="$t('lz.label.sharedataSubscription.dataFormat')" :value="SharedataPublishFormatEnum.Data" />
										<el-option :label="$t('lz.label.sharedataSubscription.fileFormat')" :value="SharedataPublishFormatEnum.File" />
									</el-select>
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataSubscription.priority')" prop="priority">
									<el-input-number v-model="state.form.priority" :min="feConfig.subscriptionPriority.min" :max="feConfig.subscriptionPriority.max" :step="1" :precision="0"
										class="w100" controls-position="right" />
								</el-form-item>
							</el-col> -->
							<!-- Ghi chú phải có mặt trên form: nhánh Update của backend gán thẳng
								 entity.Remark = command.Remark, thiếu trường này thì mỗi lần sửa là xoá
								 sạch ghi chú cũ. -->
							<el-col :xs="24" :sm="24" :md="24" :lg="24" :xl="24" class="mb20">
								<el-form-item :label="$t('lz.entity.sharedataPartner.remark')" prop="remark"
									:rules="[{ max: EntityFieldLength.Length256, message: $t('lz.validation.sharedataSubscription.remarkMaxlength', { max: EntityFieldLength.Length256 }), trigger: 'blur' }]">
									<el-input v-model="state.form.remark" :placeholder="$t('lz.label.sharedataSubscription.remark')" type="textarea" :rows="6"
										:maxlength="EntityFieldLength.Length256" show-word-limit clearable />
								</el-form-item>
							</el-col>

							<!-- <el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item prop="guaranteed">
									<template #label>
										Đảm bảo gửi
										<el-tooltip
											content="Gửi lại đến khi đối tác xác nhận — không mất gói (nên bật cho dữ liệu quan trọng như sự cố)">
											<el-icon class="lbl-help"><ele-QuestionFilled /></el-icon>
										</el-tooltip>
									</template>
									<el-switch v-model="state.form.guaranteed" />
								</el-form-item>
							</el-col>
							<el-col :xs="24" :sm="12" :md="12" :lg="12" :xl="12" class="mb20">
								<el-form-item prop="persistent">
									<template #label>
										Tự khôi phục
										<el-tooltip
											content="Tự đăng ký lại sau khi mất/nối lại kết nối (dùng cho luồng chạy dài hạn)">
											<el-icon class="lbl-help"><ele-QuestionFilled /></el-icon>
										</el-tooltip>
									</template>
									<el-switch v-model="state.form.persistent" />
								</el-form-item>
							</el-col> -->
						</el-row>
					</el-form>
				</el-tab-pane>
			</el-tabs>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="handleCancel">{{ $t('lz.button.base.cancel') }}</el-button>
					<el-button type="primary" :loading="state.loading" @click="submit">{{ $t('lz.button.sharedataSubscription.register') }}</el-button>
				</span>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="sharedataEditSubscription">
import { computed, nextTick, reactive, ref } from 'vue';
import type { FormInstance, FormRules } from 'element-plus';
import { ElMessage } from 'element-plus';
import { useI18n } from 'vue-i18n';
import { getAPI } from '/@/utils/axios-utils';
import { ShareDataMappingApi, ShareDataPacketApi, ShareDataSubscriptionApi } from '/@/api-services/shareData';
import type {
	ShareDataMappingOutput,
	ShareDataPartnerOutput,
	ShareDataScheduleDto,
	ShareDataSubscriptionOutput,
	ShareDataUpdateSubscriptionInput,
} from '/@/api-services/shareData';
import { EntityFieldLength, SharedataDirectionEnum, SharedataPublishFormatEnum, SharedataSubModeEnum } from '/@/types/enums';
import { useSharedataFeConfig } from '/@/hooks/useSharedataFeConfig';
import { codeNameLabel } from '/@/utils/sharedata/labelHelper';
import Scheduler, { type DayOfWeek, type ScheduleKind } from '/@/components/scheduler/index.vue';

const { t: $t } = useI18n();

const handleCancel = () => {
	state.isShow = false;
};

interface EventSourceItem {
	id: string;
	name: string;
	subject: string;
	datatypeCode?: string;
}

type SubscriptionForm = ShareDataUpdateSubscriptionInput;

interface Props {
	partners: ShareDataPartnerOutput[];
	defaultPartnerId?: string;
}
const props = defineProps<Props>();
interface Emits { (e: 'handleQuery'): void }
const emit = defineEmits<Emits>();

const formRef = ref<FormInstance>();
/**
 * el-form nằm trong v-for của el-tab-pane nên ref dạng chuỗi sẽ gom thành mảng.
 * Dùng function ref và bỏ qua lần gọi null (pane cũ unmount) để formRef luôn là form của tab đang mở.
 */
const setFormRef = (el: unknown) => {
	if (el) formRef.value = el as FormInstance;
};
const eventSources = ref<EventSourceItem[]>([]);

// Min/max/default lấy từ SysOpConfig nhóm sharedata_config (script 03), thiếu thì dùng SHAREDATA_FE_CONFIG_FALLBACK.
const feConfig = useSharedataFeConfig();
const schedulerRef = ref<InstanceType<typeof Scheduler>>();
const scheduleKind = ref<ScheduleKind>('continuous');
const startTime = ref<string>('06:00');
const endTime = ref<string>('22:00');
const daysOfWeek = ref<DayOfWeek[]>(['MON', 'TUE', 'WED', 'THU', 'FRI']);

function readScheduleInterval(schedule?: ShareDataScheduleDto | null): number | undefined {
	return schedule?.intervalSeconds ?? (schedule as any)?.updateDelaySec ?? undefined;
}

const packetList = ref<any[]>([]);

const loadPacketList = async () => {
	if (packetList.value.length) return;
	try {
		const res = await getAPI(ShareDataPacketApi).apiSharedataSharedatapacketListGet();
		packetList.value = res.data.result ?? [];
	} catch {
		packetList.value = [];
	}
};

const DAILY_INTERVAL_SEC = 86400;

function effectiveIntervalSeconds(): number {
	if (scheduleKind.value === 'daily') return DAILY_INTERVAL_SEC;
	const limit = feConfig.value.subscriptionInterval;
	const val = Number(state.form.intervalSeconds ?? limit.default);
	if (isNaN(val) || val <= 0) return limit.default;
	if (val > limit.max) return limit.max;
	if (val < limit.min) return limit.min;
	return Math.floor(val);
}

function defaultForm(): SubscriptionForm {
	const limits = feConfig.value;
	return {
		partnerId: props.defaultPartnerId ?? '',
		direction: SharedataDirectionEnum.Outbound as any,
		// Mã loại dữ liệu do danh mục cấu hình `shareData_type` cấp, để trống cho người dùng chọn.
		datatypeId: '',
		mode: SharedataSubModeEnum.Periodic as any,
		sendOnNewData: false,
		format: SharedataPublishFormatEnum.Data as any,
		priority: limits.subscriptionPriority.default,
		// guaranteed: true,
		// persistent: true,
		intervalSeconds: limits.subscriptionInterval.default,
		// dataSourceId: '',
		// mappingProfileId: '',
		eventSourceId: '',
		debounceSec: limits.subscriptionDebounce.default,
		remark: '',
	};
}
const state = reactive({
	isShow: false,
	loading: false,
	/** true = đang sửa bản ghi có sẵn, false = tạo mới. Quyết định khóa field và gọi update hay create. */
	isEdit: false,
	/** ID bản ghi đang sửa. */
	editingId: '',
	form: defaultForm(),

	/* Ánh xạ suy ra từ (đối tác × gói tin × chiều) — chỉ để hiển thị, đăng ký không lưu. */
	mappingLoading: false,
	mappingName: '',
	mappingCode: '',
	/** Tên key phía đối tác, lấy từ hồ sơ ánh xạ để người dùng thấy sẽ gửi/nhận những gì. */
	partnerKeys: [] as string[],
});

/** Nguồn sự kiện khớp loại dữ liệu (fallback: hiện tất cả nếu không có cái nào khớp). */
const matchedEventSources = computed(() => {
	const code = String(state.form.datatypeId);
	const matched = eventSources.value.filter((e) => !e.datatypeCode || e.datatypeCode === code);
	return matched.length ? matched : eventSources.value;
});

/**
 * "Gửi ngay khi có dữ liệu mới" là CỘT RIÊNG trên đăng ký (ShareDataSubscription.SendOnNewData),
 * không còn ghi đè Mode như trước. Bật thì ngoài lịch định kỳ, service tự dò bản ghi mới của gói
 * tin và gửi ngay; tắt thì chỉ gửi theo lịch. Chiều nhận không dùng cờ này nên khối đã ẩn sẵn.
 */

/** Chiều nhận là bị động theo request của đối tác — không có lịch gửi để cấu hình. */
const isInbound = computed(() => Number(state.form.direction) === SharedataDirectionEnum.Inbound);

/** Hai tab của dialog. Tên tab dùng chuỗi, tránh bẫy enum Outbound = 0 bị coi là falsy trong el-tabs. */
const DIRECTION_TABS = computed(() => [
	{ name: 'outbound', label: $t('lz.label.sharedataSubscription.outbound') },
	{ name: 'inbound', label: $t('lz.label.sharedataSubscription.inbound') },
]);

/** Tab đang mở <-> state.form.direction. Chuẩn hoá về đúng một trong hai chiều nên luôn có tab được chọn. */
const directionTab = computed<string>({
	get: () => (isInbound.value ? 'inbound' : 'outbound'),
	set: (v) => {
		state.form.direction = (v === 'inbound' ? SharedataDirectionEnum.Inbound : SharedataDirectionEnum.Outbound) as any;
	},
});

const rules: FormRules = {
	partnerId: [{ required: true, message: $t('lz.validation.sharedataSubscription.partnerRequired'), trigger: 'change' }],
	datatypeId: [{ required: true, message: $t('lz.validation.sharedataSubscription.datatypeRequired'), trigger: 'change' }],
	mode: [{ required: true, message: $t('lz.validation.sharedataSubscription.modeRequired'), trigger: 'change' }],
	intervalSeconds: [
		{
			validator: (_rule: any, value: any, callback: any) => {
				if (scheduleKind.value === 'continuous') {
					if (value == null || value === '') {
						callback(new Error($t('lz.validation.sharedataSubscription.intervalRequired')));
					} else {
						const num = Number(value);
						const { min, max } = feConfig.value.subscriptionInterval;
						if (isNaN(num) || num < min || num > max) {
							callback(new Error($t('lz.validation.sharedataSubscription.intervalRange', { min, max })));
						} else {
							callback();
						}
					}
				} else {
					callback();
				}
			},
			trigger: ['blur', 'change'],
		},
	],
};

/**
 * Suy hồ sơ ánh xạ đang áp dụng theo (đối tác × gói tin × chiều) rồi hiển thị tên hồ sơ và các
 * key phía đối tác. Đăng ký KHÔNG lưu liên kết này — nó được suy lại mỗi lần cần dùng.
 *
 * Chỉ tra hồ sơ CỦA ĐÚNG ĐỐI TÁC: không còn khái niệm hồ sơ dùng chung, vì worker luôn lọc
 * hồ sơ theo PartnerId của đăng ký nên hồ sơ không gắn đối tác sẽ không bao giờ được dùng.
 *
 * Phải gọi hai endpoint: List chỉ trả siêu dữ liệu (nó gán ItemsJson = null và KHÔNG dựng Items),
 * còn GetById mới giải mã ItemsJson thành Items. Lấy key từ List là luôn rỗng.
 *
 * Thứ tự tham số của client sinh từ Swagger: (ID, code, partnerId, datatypeId, direction, isActive).
 */
async function resolve() {
	state.mappingName = '';
	state.mappingCode = '';
	state.partnerKeys = [];
	// state.form.mappingProfileId = '';

	const partnerId = state.form.partnerId;
	const datatypeId = state.form.datatypeId;
	if (!partnerId || !datatypeId) return;

	// Direction của backend là enum số: Outbound = 0, Inbound = 1.
	const direction = Number(state.form.direction) === SharedataDirectionEnum.Inbound
		? SharedataDirectionEnum.Inbound
		: SharedataDirectionEnum.Outbound;

	state.mappingLoading = true;
	try {
		const api = getAPI(ShareDataMappingApi);
		const directionsToTry = [direction, SharedataDirectionEnum.Both];

		let found: ShareDataMappingOutput | undefined;

		// Hồ sơ đúng chiều trước, không có thì tới hồ sơ khai hai chiều.
		for (const dir of directionsToTry) {
			const res = await api.apiSharedataSharedatamappingListGet(
				undefined, undefined, partnerId, datatypeId, dir as any, true
			);
			found = (res.data.result ?? [])[0];
			if (found) break;
		}

		if (!found?.id) return;

		state.mappingName = found.name || found.code || $t('lz.label.sharedataSubscription.unnamed');
		state.mappingCode = found.code ?? '';

		// Lấy chi tiết để có bộ khung đầu ra — endpoint List không trả các cột JSON lớn.
		const detail = await api.apiSharedataSharedatamappingByidGet(found.id);
		// targetShapeJson chưa có trong client sinh từ Swagger nên đọc qua any.
		const shapeText = ((detail.data.result as any)?.targetShapeJson ?? '').toString().trim();
		state.partnerKeys = shapeText ? collectShapeKeys(shapeText, direction) : [];
	} catch {
		// Không tra được ánh xạ thì để trống, không chặn việc mở dialog.
	} finally {
		state.mappingLoading = false;
	}
}

/**
 * Thu danh sách key từ bộ khung đầu ra (ShareDataMapping.TargetShapeJson).
 *
 * Phải đọc bộ khung chứ không phải ItemsJson: ItemsJson chỉ chứa trường CÓ luật quy đổi nên
 * không liệt kê đủ, và nó cũng không giữ tên đối tác gọi lẫn vị trí trong gói tin.
 *
 * Chiều GỬI ĐI  → key đối tác nhận là khoá của object bao ngoài {"$field":…}
 * Chiều NHẬN VỀ → quan tâm trường của hệ thống mình, tức giá trị của $field
 */
function collectShapeKeys(shapeText: string, direction: number): string[] {
	let shape: any;
	try {
		shape = JSON.parse(shapeText);
	} catch {
		return [];
	}

	const inbound = direction === SharedataDirectionEnum.Inbound;
	const out: string[] = [];
	const seen = new Set<string>();
	const push = (v?: string | null) => {
		const k = (v ?? '').trim();
		if (k && !seen.has(k)) {
			seen.add(k);
			out.push(k);
		}
	};

	// keyOfParent: tên khoá mà nút hiện tại đang nằm dưới, dùng cho chiều gửi đi.
	const walk = (node: any, keyOfParent: string) => {
		if (Array.isArray(node)) {
			for (const item of node) walk(item, keyOfParent);
			return;
		}
		if (node === null || typeof node !== 'object') return;

		if (Object.prototype.hasOwnProperty.call(node, '$field')) {
			push(inbound ? String(node['$field'] ?? '') : keyOfParent);
			return;
		}
		if (Object.prototype.hasOwnProperty.call(node, '$each')) {
			walk(node['$as'], keyOfParent);
			return;
		}

		for (const key of Object.keys(node)) walk(node[key], key);
	};

	walk(shape, '');
	return out;
}

/** Đổi gói tin: mặc định chế độ Định kỳ rồi tìm lại hồ sơ ánh xạ tương ứng. */
async function onDatatypeChange() {
	state.form.mode = SharedataSubModeEnum.Periodic as any;
	await resolve();
}

/**
 * Nạp các danh mục dùng cho dialog.
 *
 * - Ánh xạ: không tải sẵn toàn bộ; `resolve()` gọi API lọc theo (đối tác × gói tin × chiều)
 *   nên chỉ lấy đúng hồ sơ cần dùng.
 * - Nguồn sự kiện: BACKEND CHƯA CÓ CONTROLLER nên để rỗng, combobox chế độ EVENT
 *   sẽ trống và khối alert trong template đã báo rõ.
 *   TODO(BE): bổ sung ShareDataEventSource/List rồi nạp vào `eventSources`.
 */
async function ensureLists() {
	eventSources.value = [];
	await loadPacketList();
}

/**
 * Mở dialog.
 * Truyền `row` = chế độ sửa: nạp dữ liệu cũ vào form và khóa 3 trường định danh
 * (đối tác, chiều, loại dữ liệu) vì backend cũng không cho đổi.
 */
async function openDialog(partnerId?: string, direction?: number, row?: ShareDataSubscriptionOutput) {
	state.isEdit = Boolean(row?.id);
	state.editingId = row?.id ?? '';

	if (row) {
		state.form = {
			partnerId: row.partnerId,
			direction: row.direction,
			datatypeId: row.datatypeId,
			// Bản ghi cũ có thể là Single — giao diện chỉ còn Event/Periodic nên quy về Periodic.
			mode: (Number(row.mode) === SharedataSubModeEnum.Event ? SharedataSubModeEnum.Event : SharedataSubModeEnum.Periodic) as any,
			format: row.format,
			priority: row.priority,
			// guaranteed: row.guaranteed,
			// persistent: row.persistent,
			// dataSourceId: row.dataSourceId ?? '',
			// mappingProfileId: row.mappingProfileId ?? '',
			eventSourceId: row.eventSourceId ?? '',
			debounceSec: row.debounceSec ?? feConfig.value.subscriptionDebounce.default,
			sendOnNewData: row.sendOnNewData ?? false,
			// Phải nạp lại remark: backend Update ghi thẳng command.Remark nên bỏ trống là mất dữ liệu cũ.
			remark: row.remark ?? '',
			// Ưu tiên cột IntervalSeconds; bản ghi cũ chỉ có chu kỳ trong ScheduleJson thì lấy tạm.
			intervalSeconds: row.intervalSeconds ?? readScheduleInterval(row.schedule) ?? feConfig.value.subscriptionInterval.default,
		};
		applySchedule(row.schedule);
	} else {
		state.form = {
			...defaultForm(),
			partnerId: partnerId ?? props.defaultPartnerId ?? '',
			direction: (direction ?? SharedataDirectionEnum.Outbound) as any,
		};
		scheduleKind.value = 'continuous';
		startTime.value = '06:00';
		endTime.value = '22:00';
		daysOfWeek.value = ['MON', 'TUE', 'WED', 'THU', 'FRI'];
	}

	await ensureLists();
	await resolve();
	state.isShow = true;
	// Xóa lỗi kiểm tra còn sót từ lần mở trước (ví dụ "Vui lòng chọn loại dữ liệu") trước khi người dùng thao tác.
	await nextTick();
	formRef.value?.clearValidate();
}

/** Đổ lịch gửi đã lưu ngược lại các ô nhập, để form hiện đúng giá trị cũ khi sửa. */
function applySchedule(schedule?: ShareDataScheduleDto | null) {
	scheduleKind.value = (schedule?.kind as 'continuous' | 'daily') ?? 'continuous';
	// Kiểu Hàng ngày dùng startTime làm giờ gửi; kiểu Liên tục dùng cặp startTime/endTime làm khung giờ.
	startTime.value = schedule?.startTime ?? '06:00';
	endTime.value = schedule?.endTime ?? '22:00';
	daysOfWeek.value = (schedule?.daysOfWeek as DayOfWeek[]) ?? ['MON', 'TUE', 'WED', 'THU', 'FRI'];
}

/**
 * Dựng lịch gửi để lưu. Luôn dựng cho cả hai chế độ: khi "Theo sự kiện" bật (mode = Event),
 * lịch vẫn cần thiết vì nó là khung thời gian cho phép gửi.
 */
function buildSchedule(): ShareDataScheduleDto | undefined {
	if (isInbound.value) return undefined;
	// Ghi cùng giá trị chu kỳ vào ScheduleJson và cột IntervalSeconds để hai nơi không lệch.
	const interval = effectiveIntervalSeconds();
	if (scheduleKind.value === 'continuous') {
		return {
			kind: 'continuous',
			intervalSeconds: interval,
			startTime: startTime.value || undefined,
			endTime: endTime.value || undefined,
		};
	}
	// Hàng ngày: 1 lần/ngày vào startTime nên không có endTime, durationMinutes cũng vô nghĩa.
	return {
		kind: 'daily',
		intervalSeconds: interval,
		daysOfWeek: daysOfWeek.value,
		startTime: startTime.value,
	};
}

async function submit() {
	if (!formRef.value) return;
	if (scheduleKind.value === 'continuous' && schedulerRef.value?.timeRangeError) {
		ElMessage.error(schedulerRef.value.timeRangeError);
		return;
	}
	await formRef.value.validate(async (valid) => {
		if (!valid) return;
		state.loading = true;
		try {
			// BE tự suy lại mappingProfileId / dataSourceId theo (đối tác × gói tin × chiều),
			// giá trị gửi lên chỉ mang tính gợi ý.
			// intervalSeconds ghi đè theo kiểu lịch: Hàng ngày là 1 lần/ngày, không dùng số trong form.
			state.form.intervalSeconds = effectiveIntervalSeconds();
			const payload = { ...state.form, intervalSeconds: state.form.intervalSeconds, schedule: buildSchedule() } as any;
			const api = getAPI(ShareDataSubscriptionApi);

			if (state.isEdit) {
				await api.apiSharedataSharedatasubscriptionUpdatePost({ ...payload, id: state.editingId });
				ElMessage.success($t('lz.message.sharedataSubscription.updateSuccess'));
			} else {
				await api.apiSharedataSharedatasubscriptionAddPost(payload);
				ElMessage.success(Number(state.form.direction) === SharedataDirectionEnum.Inbound ? $t('lz.message.sharedataSubscription.createInboundSuccess') : $t('lz.message.sharedataSubscription.createOutboundSuccess'));
			}

			state.isShow = false;
			emit('handleQuery');
		} catch {
			// Không gọi ElMessage.error tại đây vì axios interceptor (axios-utils.ts)
			// đã hiển thị thông báo lỗi từ response trả về. Gọi thêm sẽ tạo 2 toast lỗi chồng lấn (Issue 2, 22).
		} finally {
			state.loading = false;
		}
	});
}

defineExpose({ openDialog });</script>

<style scoped lang="scss">
.keys-title {
	font-weight: 600;
	margin-bottom: 4px;
}

.keys-wrap {
	display: flex;
	flex-wrap: wrap;
	gap: 4px;
}

.key-tag {
	margin: 0;
}

.keys-empty {
	font-size: 12px;
	color: var(--el-text-color-secondary);
}

.ml5 {
	margin-left: 5px;
}

.lbl-help {
	margin-left: 4px;
	color: var(--el-color-info);
	cursor: help;
	vertical-align: middle;
}

/* Tab chiều (Gửi đi / Nhận về): giữ style tab mặc định như các màn edit khác, chỉ chia đôi thanh tab
   để mỗi chữ nằm giữa nửa của nó. Padding đặt lại 2 bên bằng nhau vì Element Plus bỏ padding-left tab đầu
   và padding-right tab cuối, làm chữ lệch khỏi tâm. */
.direction-tabs {
	:deep(.el-tabs__nav) {
		width: 100%;
	}

	:deep(.el-tabs__header .el-tabs__nav .el-tabs__item) {
		flex: 1;
		padding: 0 20px;
	}
}

/* Khoảng đệm thoáng ở footer modal Tạo/Sửa đăng ký chia sẻ dữ liệu (chỉ áp dụng riêng modal phân hệ ShareData) */
.sharedata-edit-subscription-container {
	:deep(.el-dialog__footer) {
		padding: 12px 20px 20px;
	}

	/* Bỏ khoảng trống giữa thanh tiêu đề và hàng tab. Selector phải nặng hơn rule
	   `.el-overlay .el-overlay-dialog .el-dialog .el-dialog__body` của theme/element.scss. */
	:deep(.el-dialog.sharedata-edit-subscription-dialog .el-dialog__body) {
		padding-top: 0;
	}
}

:deep(.el-dialog.sharedata-edit-subscription-dialog) {
	.el-dialog__footer {
		padding: 12px 20px 20px;
	}
}
</style>
