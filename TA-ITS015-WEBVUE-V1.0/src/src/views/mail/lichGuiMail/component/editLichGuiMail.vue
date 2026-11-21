<template>
	<div class="toll-edit-lich-gui-mail-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="min(1800px, 98vw)"
			top="4vh" class="edit-lich-gui-mail-dialog" :before-close="beforeClose">
			<template #header>
				<div class="mail-dialog-header">
					<el-icon size="14" class="mail-dialog-header__icon"><ele-Message /></el-icon>
					<span class="mail-dialog-header__title">{{ props.title }}</span>
				</div>
			</template>
			<el-form :model="state.editForm" ref="editFormRef" label-position="top" autocomplete="off" class="edit-form"
				:disabled="state.isProcessing">
				<el-splitter class="edit-layout" :class="{ 'edit-layout--stacked': isCompactLayout }">
					<el-splitter-panel size="33%" min="20%" :resizable="!isCompactLayout">
						<div class="pane">
							<el-divider content-position="left" class="divider"><span
									class="grp">{{ $t('lz.label.emailSchedule.groupSchedule') }}</span></el-divider>
							<el-form-item :label="$t('lz.entity.emailSchedule.scheduleName')" prop="name" :rules="[
								{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.emailSchedule.scheduleName') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length128, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.scheduleName'), MaxLength: EntityFieldLength.Length128 }), trigger: 'blur' },
							]">
								<el-input v-model="state.editForm.name"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.scheduleName') })"
									:maxlength="EntityFieldLength.Length128" show-word-limit clearable />
							</el-form-item>
							<el-row :gutter="10">
								<el-col :span="14">
									<el-form-item :label="$t('lz.entity.emailSchedule.scheduleCode')" prop="code"
										:rules="[
											{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.emailSchedule.scheduleCode') }), trigger: 'blur' },
											{ max: EntityFieldLength.Length64, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.scheduleCode'), MaxLength: EntityFieldLength.Length64 }), trigger: 'blur' },
										]">
										<el-input v-model="state.editForm.code"
											:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.scheduleCode') })"
											:maxlength="EntityFieldLength.Length64" clearable />
									</el-form-item>
								</el-col>
								<el-col :span="10">
									<el-form-item :label="$t('lz.entity.base.status')" prop="status">
										<el-radio-group v-model="state.editForm.status">
											<el-radio
												v-for="item in getConfigDataByCode(BaseConfigTypeEnum.SysStatusType)"
												:key="item.code" :value="parseInt(item.code)">
												{{ item.name }}
											</el-radio>
										</el-radio-group>
									</el-form-item>
								</el-col>
							</el-row>

							<el-divider content-position="left" class="divider"><span class="grp">{{
								$t('lz.label.emailSchedule.groupRunTime') }}</span></el-divider>
							<el-row :gutter="10">
								<el-col :span="runColSpan">
									<el-form-item :label="$t('lz.entity.emailSchedule.frequency')" prop="frequency"
										:rules="[
											{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.emailSchedule.frequency') }), trigger: 'change' },
										]">
										<el-select v-model="state.editForm.frequency" class="w100"
											@change="onFrequencyChange">
											<el-option v-for="f in FREQUENCIES" :key="f.code" :label="f.name"
												:value="f.code" />
										</el-select>
									</el-form-item>
								</el-col>
								<!-- Hàng tuần: chọn đúng một thứ -->
								<el-col v-if="state.editForm.frequency === 'week'" :span="runColSpan">
									<el-form-item :label="$t('lz.entity.emailSchedule.dayOfWeek')">
										<el-select v-model="weekDay" class="w100">
											<el-option v-for="d in WEEKDAYS" :key="d" :label="weekdayLabel(d)"
												:value="d" />
										</el-select>
									</el-form-item>
								</el-col>
								<!-- Hàng quý: tháng thứ mấy của quý / Hàng năm: tháng nào -->
								<el-col v-if="needMonthInPeriod" :span="runColSpan">
									<el-form-item :label="state.editForm.frequency === 'quarter'
										? $t('lz.entity.emailSchedule.monthInQuarter')
										: $t('lz.entity.emailSchedule.monthOfYear')">
										<el-select v-model="state.editForm.monthInPeriod" class="w100">
											<el-option v-for="m in monthInPeriodOptions" :key="m.value" :label="m.label"
												:value="m.value" />
										</el-select>
									</el-form-item>
								</el-col>
								<el-col v-if="needDayOfMonth" :span="runColSpan">
									<el-form-item :label="$t('lz.entity.emailSchedule.dayOfMonth')"
										prop="dayOfMonth">
										<el-select v-model="state.editForm.dayOfMonth" class="w100">
											<el-option v-for="d in 31" :key="d" :label="dayOfMonthLabel(d)"
												:value="d" />
										</el-select>
									</el-form-item>
								</el-col>
								<el-col :span="runColSpan">
									<el-form-item :label="$t('lz.entity.emailSchedule.runTime')" prop="runTime">
										<el-time-select v-model="state.runTime" start="00:00" step="00:01" end="23:55"
											class="w100" />
									</el-form-item>
								</el-col>
								<!-- Hàng ngày: bắt buộc chọn ít nhất một thứ; mặc định chọn cả 7 thứ -->
								<el-col v-if="state.editForm.frequency === 'day'" :span="24">
									<el-form-item :label="$t('lz.entity.emailSchedule.daysOfWeek')"
										prop="daysOfWeek" required
										:rules="[{ validator: daysOfWeekValidator, trigger: 'change' }]">
										<el-checkbox-group v-model="dayWeekdays">
											<el-checkbox v-for="d in WEEKDAYS" :key="d" :value="d">{{ weekdayLabel(d)
												}}</el-checkbox>
										</el-checkbox-group>
										<span class="hint ml8">{{ $t('lz.label.emailSchedule.daysOfWeekHint')
											}}</span>
									</el-form-item>
								</el-col>
								<el-col :span="24">
									<div class="hint mb8">{{ scheduleSummary }}</div>
								</el-col>
							</el-row>

							<el-divider content-position="left" class="divider"><span class="grp">{{
								$t('lz.label.base.otherInfo') }}</span></el-divider>
							<el-form-item :label="$t('lz.entity.base.orderNo')" prop="orderNo">
								<el-input-number v-model="state.editForm.orderNo" :min="0" class="w100"
									controls-position="right" />
							</el-form-item>
							<el-form-item :label="$t('lz.entity.base.remark')" prop="remark" :rules="[
								{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.base.remark'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' },
							]">
								<el-input v-model="state.editForm.remark" type="textarea" :rows="2"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.base.remark') })"
									:maxlength="EntityFieldLength.Length256" show-word-limit />
							</el-form-item>
						</div>
					</el-splitter-panel>

					<el-splitter-panel size="42%" min="25%" :resizable="!isCompactLayout">
						<div class="pane" v-loading="state.templateLoading">
							<el-divider content-position="left" class="divider"><span
									class="grp">{{ $t('lz.label.emailSchedule.mailContent') }}</span></el-divider>

							<el-form-item :label="$t('lz.entity.emailSchedule.template')">
								<div class="template-toolbar">
									<MailTemplatePicker ref="pickerRef" v-model="state.editForm.templateCode"
										class="template-select" :disabled="state.templateLoading || state.isProcessing"
										@change="onTemplateChange" />
									<el-button type="primary" plain icon="ele-View" :loading="state.previewing"
										:disabled="state.templateLoading || state.isProcessing" @click="handlePreview">
										{{ $t('lz.label.emailSchedule.previewButton') }}
									</el-button>
								</div>
								<span class="hint">Xem trước nội dung thư với dữ liệu của lịch gửi.</span>
							</el-form-item>
							<el-form-item label="Nội dung thư" prop="content"
								:rules="[{ validator: contentValidator, trigger: 'change' }]">
								<div class="mail-content-editor">
									<JoiEditor :buttons="mailEditorButtons" :upload-image="uploadMailImage"
										:plugins="mailEditorPlugins" :exclude-tools="mailEditorExcludeTools"
										v-if="state.isShowDialog" ref="contentEditorRef" :key="state.editorKey"
										v-model:getHtml="editorHtml" mode="simple" height=60vh
										:allow-upload-image="!!props.baseUrl"
										:disable="state.templateLoading || state.isProcessing"
										:text-drop-mime="MAIL_TAG_MIME" :protect-inserted-tags="true"
										placeholder="Nhập nội dung thư hoặc chọn một template..." />
								</div>
							</el-form-item>
						</div>
					</el-splitter-panel>

					<!-- ═══════════ CỘT PHẢI: NGƯỜI NHẬN VÀ NỘI DUNG ═══════════ -->
					<el-splitter-panel class="recipients-panel" size="25%" min="18%" :resizable="!isCompactLayout">
						<div class="pane">
							<el-divider content-position="left" class="divider"><span class="grp">{{
								$t('lz.label.emailSchedule.groupRecipients') }}</span></el-divider>
							<el-form-item :label="$t('lz.entity.emailSchedule.recipients')" prop="recipients"
								:rules="[
									{ required: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.emailSchedule.recipients') }), trigger: 'blur' },
									{ max: EntityFieldLength.Length1024, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.recipients'), MaxLength: EntityFieldLength.Length1024 }), trigger: 'blur' },
									{ validator: recipientsValidator, trigger: 'blur' },
								]">
								<MailAddressInput v-model="state.editForm.recipients"
									@blur="validateAddress('recipients')"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.recipients') })" />
								<div class="hint">{{ $t('lz.label.emailSchedule.recipientsHint', {
									max:
									MAX_RECIPIENTS }) }}
								</div>
								<!-- Bật / tắt khối CC / BCC. Thu gọn mà còn địa chỉ thì hiện số lượng để không quên -->
								<el-button link type="primary" size="small" class="cc-toggle"
									@click="state.showCcBcc = !state.showCcBcc">
									{{ $t('lz.label.emailSchedule.ccBcc') }}
									<span v-if="!state.showCcBcc && ccBccCount" class="cc-count">({{ ccBccCount
										}})</span>
									<el-icon class="el-icon--right">
										<ele-ArrowUp v-if="state.showCcBcc" />
										<ele-ArrowDown v-else />
									</el-icon>
								</el-button>
							</el-form-item>

							<!-- CC / BCC: mặc định thu gọn (lịch đã có thì mở sẵn). Dùng v-show chứ không v-if để khi thu gọn
							     các ô vẫn được kiểm tra lúc lưu. Giới hạn tính trên TỔNG 3 ô. -->
							<el-collapse-transition>
								<div v-show="state.showCcBcc">
									<el-form-item :label="$t('lz.entity.emailSchedule.cc')" prop="cc" :rules="[
										{ max: EntityFieldLength.Length1024, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.cc'), MaxLength: EntityFieldLength.Length1024 }), trigger: 'blur' },
										{ validator: recipientsValidator, trigger: 'blur' },
									]">
										<MailAddressInput v-model="state.editForm.cc" @blur="validateAddress('cc')"
											:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.cc') })" />
									</el-form-item>
									<el-form-item :label="$t('lz.entity.emailSchedule.bcc')" prop="bcc" :rules="[
										{ max: EntityFieldLength.Length1024, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.bcc'), MaxLength: EntityFieldLength.Length1024 }), trigger: 'blur' },
										{ validator: recipientsValidator, trigger: 'blur' },
									]">
										<MailAddressInput v-model="state.editForm.bcc" @blur="validateAddress('bcc')"
											:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.bcc') })" />
										<div class="hint">{{ $t('lz.label.emailSchedule.bccHint') }}</div>
									</el-form-item>
								</div>
							</el-collapse-transition>

							<!-- Tiêu đề riêng của lịch gửi; đổi template không ghi đè tiêu đề. -->
							<el-form-item :label="$t('lz.entity.emailSchedule.subject')" prop="subject" :rules="[
								{ required: true, whitespace: true, message: $t('lz.validation.base.required', { PropertyName: $t('lz.entity.emailSchedule.subject') }), trigger: 'blur' },
								{ max: EntityFieldLength.Length256, message: $t('lz.validation.base.maxlength', { PropertyName: $t('lz.entity.emailSchedule.subject'), MaxLength: EntityFieldLength.Length256 }), trigger: 'blur' },
							]">
								<MailSubjectInput ref="subjectRef" v-model="state.editForm.subject"
									:disabled="state.isProcessing"
									:placeholder="$t('lz.label.base.pleaseType', { input: $t('lz.entity.emailSchedule.subject') })"
									:maxlength="EntityFieldLength.Length256"
									@blur="editFormRef?.validateField('subject').catch(() => undefined)" />
								<MailTagBar :target="subjectRef" :disabled="state.isProcessing"
									:double-click-insert="true" class="tag-bar" />
								<div class="hint">Nhấp đúp hoặc kéo tag vào tiêu đề. Backspace/Delete xóa nguyên tag.
								</div>
							</el-form-item>
							<el-divider content-position="left" class="divider"><span class="grp">Tag nội dung
									thư</span></el-divider>
							<MailTagBar :target="contentEditorRef"
								:disabled="state.templateLoading || state.isProcessing" class="body-tag-bar" />
							<p class="hint">Bấm hoặc kéo tag vào nội dung thư.</p>

							<el-divider content-position="left" class="divider"><span class="grp">{{
								$t('lz.label.emailSchedule.groupReports') }}</span></el-divider>
							<div class="mb8 tool-row">
								<el-button size="small" type="primary" icon="ele-Plus"
									:disabled="state.editForm.items!.length >= MAX_ITEMS" @click="addItem">
									{{ $t('lz.label.emailSchedule.addReport') }}
								</el-button>
								<span class="hint">{{ $t('lz.label.emailSchedule.maxItemsHint', { max: MAX_ITEMS })
									}}</span>
							</div>
							<el-scrollbar max-height="280px" class="report-list-scroll">
								<div v-for="(row, index) in state.editForm.items" :key="index" class="item-card">
									<div class="item-head">
										<span class="item-no">{{ index + 1 }}</span>
										<el-tree-select v-model="row.reportCode" :data="reportTree" node-key="value"
											filterable :render-after-expand="false" :expand-on-click-node="true"
											:default-expanded-keys="expandedGroupKeys(row.reportCode)" size="small"
											class="item-report" popper-class="mail-report-tree"
											:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.emailSchedule.report') })" />
										<el-button icon="ele-Delete" text type="danger" size="small"
											@click="removeItem(index)" />
									</div>
									<el-row :gutter="6">
										<el-col :span="12">
											<div class="cap">{{ $t('lz.entity.emailSchedule.periodOffset') }}</div>
											<el-select v-model="row.periodOffset" size="small" class="w100"
												:placeholder="$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.emailSchedule.periodOffset') })">
												<el-option v-for="item in getConfigDataByCode(BaseConfigTypeEnum.MailPeriod)"
													:key="item.code" :label="item.name" :value="parseInt(item.code)" />
											</el-select>
										</el-col>
										<el-col :span="12">
											<div class="cap">{{ $t('lz.entity.emailSchedule.format') }}</div>
											<el-select v-model="row.format" size="small" class="w100">
												<el-option label="PDF" value="pdf" />
												<el-option label="Excel" value="xlsx" />
												<el-option label="Word" value="docx" />
											</el-select>
										</el-col>
									</el-row>
									<!-- Báo cáo có kỳ cố định lệch tần suất lịch: KHÔNG chặn, chỉ lưu ý -->
									<div v-if="itemWarnings[index]" class="item-warn">
										<el-icon><ele-WarningFilled /></el-icon>
										<span>{{ itemWarnings[index] }}</span>
									</div>
								</div>
								<el-alert v-if="!state.editForm.items!.length" type="warning" :closable="false"
									:title="$t('lz.label.emailSchedule.noReportWarning')" />
							</el-scrollbar>
						</div>
					</el-splitter-panel>
				</el-splitter>
			</el-form>

			<template #footer>
				<span class="dialog-footer">
					<el-button @click="cancel" :disabled="state.isProcessing">{{ $t('lz.button.base.cancel')
						}}</el-button>
					<el-button type="primary" @click="submit" :loading="state.isProcessing"
						:disabled="state.templateLoading || state.previewing">
						{{ $t('lz.label.emailSchedule.saveSchedule') }}
					</el-button>
				</span>
			</template>
		</el-dialog>
		<el-dialog v-model="state.previewVisible" width="min(1000px, 95vw)" top="4vh" append-to-body destroy-on-close
			@closed="previewRef?.reset()">
			<template #header>
				<div class="mail-dialog-header">
					<el-icon size="14" class="mail-dialog-header__icon"><ele-View /></el-icon>
					<span class="mail-dialog-header__title">{{ $t('lz.label.emailSchedule.previewMail') }}</span>
				</div>
			</template>
			<MailPreview v-if="state.previewVisible" ref="previewRef" :form="previewForm"
				:to="state.editForm.recipients" :cc="state.editForm.cc" />
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tollEditLichGuiMail">
import { computed, nextTick, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { useI18n } from 'vue-i18n';
import { ElMessage, ElMessageBox, ElScrollbar, ElSplitter, ElSplitterPanel } from 'element-plus';
import { useMediaQuery } from '@vueuse/core';
import 'element-plus/theme-chalk/el-splitter.css';
import { getAPI } from '/@/utils/axios-utils';
import { OperateType } from '/@/types/constants/operate';
import { EntityFieldLength, BaseOpConfigGroupCodeEnum, MailOpConfigCodeEnum } from '/@/types/enums/baseEnum';
import { BaseConfigTypeEnum } from '/@/types/enums';
import { useBaseConfig } from '/@/stores/baseConfig';
import { useMailReportCatalog } from '/@/hooks/useMailReportCatalog';
import { emailRegex } from '/@/types/validators';

import { RptEmailScheduleApi, RptEmailTemplateApi } from '/@/api-services/mail';
import type {
	MailUpdateScheduleInput,
	MailPreviewScheduleInput,
	RptEmailTemplateOutput,
	MailReportOutput,
} from '/@/api-services/mail';
import type { PluginOutput } from '/@/api-services/system';

import MailTemplatePicker from './mailTemplatePicker.vue';
import MailTagBar from './mailTagBar.vue';
import MailPreview from './mailPreview.vue';
import MailAddressInput from './mailAddressInput.vue';
import MailSubjectInput from './mailSubjectInput.vue';
import { loadMailTemplates, MAIL_TAG_MIME } from './mailTemplateCache';
import JoiEditor from '/@/components/joditEditor/index.vue';

const uploadMailImage = async (file: File): Promise<string> => {
	const { data } = await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleUploadimagePostForm(file);
	const url = data.result?.url;
	if (!url) throw new Error('Upload ảnh không trả về URL.');
	return new URL(url.replace(/\\/g, '/'), props.baseUrl ?? undefined).href;
};

const mailEditorButtons: string[] = [
	'undo', 'redo', '|',
	'bold', 'italic', 'underline', 'strikethrough', 'superscript', 'subscript', '|',
	'font', 'fontsize', 'brush', 'paragraph', '|',
	'ul', 'ol', 'outdent', 'indent', 'align', '|',
	'cut', 'copy', 'paste', 'selectall', '|',
	'customUploadImage', 'table', 'hr', '|',
	'eraser', 'copyformat', 'fullsize'
];
// Add tool names here to hide them on this screen.
const mailEditorExcludeTools: string[] = [];
const mailEditorPlugins: string[] = [
	'enter', 'backspace', 'delete-command', 'hotkeys', 'size', 'placeholder',
	'wrap-nodes', 'dtd', 'toolbar-navigation', 'clean-html', 'clipboard', 'paste',
	'bold', 'font', 'color', 'format-block', 'ordered-list', 'indent', 'justify',
	'redo-undo', 'link', 'image', 'video', 'file', 'table', 'hr', 'symbols',
	'search', 'preview', 'print', 'copyformat', 'source', 'fullsize', 'about',
	'inline-popup', 'select-cells', 'resize-cells', 'resizer',
];

interface Props {
	title: string;
	operateType: OperateType;
	baseUrl: string | null;
}
const props = defineProps<Props>();
const isCompactLayout = useMediaQuery('(max-width: 1199px)');

interface Emits {
	(e: 'handleQuery'): void;
}
const emits = defineEmits<Emits>();

const { t: $t } = useI18n();
const baseConfig = useBaseConfig();
const getConfigDataByCode = baseConfig.getConfigDataByCode;
const getOpConfigItemByCode = baseConfig.getOpConfigItemByCode;
const { loadMailReports, loadMailReportPlugins, isActiveReport, groupReports, clearMailAddressCache } = useMailReportCatalog();
const editFormRef = ref();
const pickerRef = ref<InstanceType<typeof MailTemplatePicker>>();
const previewRef = ref<InstanceType<typeof MailPreview>>();
const subjectRef = ref<InstanceType<typeof MailSubjectInput>>();
const contentEditorRef = ref<InstanceType<typeof JoiEditor>>();
let dialogRequest = 0;
let templateRequest = 0;


const FREQUENCIES = computed<any[]>(() => getConfigDataByCode(BaseConfigTypeEnum.TollPeriod) ?? []);
/** Kỳ mặc định khi thêm báo cáo — SysConfigData mail_period: 0 = kỳ hiện tại, 1 = kỳ trước. */
const PERIOD_OFFSET_PREVIOUS = 1;

const MAX_ITEMS = computed(() => Number(getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.MailConfig, MailOpConfigCodeEnum.MaxItems)?.value));
const MAX_RECIPIENTS = computed(() => Number(getOpConfigItemByCode(BaseOpConfigGroupCodeEnum.MailConfig, MailOpConfigCodeEnum.MaxRecipients)?.value));

/** Tách ô địa chỉ theo ; , xuống dòng — chữ thường, bỏ trùng. */
const parseEmails = (value?: string | null) =>
	[...new Set((value ?? '').split(/[;,\r\n]+/).map((x) => x.trim().toLowerCase()).filter(Boolean))];

/** Tổng địa chỉ của cả 3 ô (Người nhận + CC + BCC), trùng giữa các ô chỉ tính một — khớp BE. */
const totalRecipients = computed(
	() => new Set([...parseEmails(state.editForm.recipients), ...parseEmails(state.editForm.cc), ...parseEmails(state.editForm.bcc)]).size
);

/**
 * Ô địa chỉ là component riêng (thẻ kiểu Gmail) nên el-form-item không tự bắt sự kiện blur — chạy rule tay.
 * Kiểm tra cả 3 ô vì giới hạn tính trên TỔNG: thêm địa chỉ ở CC có thể làm ô Người nhận vượt giới hạn.
 */
const validateAddress = (prop: 'recipients' | 'cc' | 'bcc') => {
	editFormRef.value?.validateField(prop).catch(() => undefined);
	(['recipients', 'cc', 'bcc'] as const)
		.filter((x) => x !== prop && state.editForm[x])
		.forEach((x) => editFormRef.value?.validateField(x).catch(() => undefined));
};

/** Số địa chỉ ở CC + BCC — hiện cạnh nút khi khối đang thu gọn. */
const ccBccCount = computed(() => parseEmails(state.editForm.cc).length + parseEmails(state.editForm.bcc).length);

/** Dùng chung cho 3 ô: định dạng của chính ô đó + giới hạn TỔNG 3 ô. Bắt buộc nhập do rule required của từng ô. */
const recipientsValidator = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
	if (!value?.trim()) return callback();

	const emails = parseEmails(value);
	const invalid = emails.filter((x) => !emailRegex.test(x));

	if (!emails.length || invalid.length)
		return callback(new Error($t('lz.validation.emailSchedule.invalidRecipients', { list: invalid.join(', ') || value.trim() })));
	if (totalRecipients.value > MAX_RECIPIENTS.value)
		return callback(new Error($t('lz.validation.emailSchedule.tooManyRecipients', { count: totalRecipients.value, max: MAX_RECIPIENTS.value })));

	callback();
};

/** Khớp RptEmailConst.DefaultTemplateCode của backend. */
const DEFAULT_TEMPLATE = 'tieu-chuan';

/** Nhãn ngày chạy. Khai 31 nghĩa là ngày cuối tháng — job tự dồn, không mất tháng ngắn. */
const dayOfMonthLabel = (day: number) =>
	day === 31
		? $t('lz.label.emailSchedule.lastDayOfMonth')
		: $t('lz.label.emailSchedule.dayNo', { day: String(day).padStart(2, '0') });

const state = reactive({
	isShowDialog: false,
	isProcessing: false,
	runTime: '08:00',
	editForm: { items: [] } as MailUpdateScheduleInput,
	previewing: false,
	previewVisible: false,
	templateLoading: false,
	loadedTemplateCode: '' as string,
	editorKey: 0,
	/** Hiện 2 ô CC / BCC. */
	showCcBcc: false,
	listData: {
		/** Danh mục báo cáo chưa xoá (kể cả đang tắt) — useMailReportCatalog. */
		reports: [] as MailReportOutput[],
		plugins: [] as PluginOutput[],
	},
});

const needDayOfMonth = computed(() => ['month', 'quarter', 'year'].includes(state.editForm.frequency ?? ''));
const needMonthInPeriod = computed(() => ['quarter', 'year'].includes(state.editForm.frequency ?? ''));
const WEEKDAYS = [1, 2, 3, 4, 5, 6, 7];
const weekdayLabel = (d: number) => $t(`lz.label.emailSchedule.weekday${d}`);

const parseDays = (value?: string | null): number[] =>
	[...new Set((value ?? '').split(',').map((x) => Number(x.trim())).filter((n) => n >= 1 && n <= 7))].sort((a, b) => a - b);
const weekDay = computed<number>({
	get: () => parseDays(state.editForm.daysOfWeek)[0] ?? 1,
	set: (v) => (state.editForm.daysOfWeek = String(v)),
});
const dayWeekdays = computed<number[]>({
	get: () => parseDays(state.editForm.daysOfWeek),
	set: (v) => {
		const days = [...new Set(v)].sort((a, b) => a - b);
		state.editForm.daysOfWeek = days.length === 0 ? null : days.join(',');
	},
});

const ALL_WEEKDAYS = WEEKDAYS.join(',');

const daysOfWeekValidator = (_rule: unknown, _value: unknown, callback: (error?: Error) => void) => {
	if (state.editForm.frequency === 'day' && parseDays(state.editForm.daysOfWeek).length === 0)
		return callback(new Error($t('lz.validation.emailSchedule.daysOfWeekRequired')));
	callback();
};

const monthInPeriodOptions = computed(() =>
	state.editForm.frequency === 'quarter'
		? [1, 2, 3].map((n) => ({ value: n, label: $t('lz.label.emailSchedule.monthInQuarterNo', { n }) }))
		: Array.from({ length: 12 }, (_, i) => i + 1).map((n) => ({
			value: n,
			label: $t('lz.label.emailSchedule.monthNo', { month: String(n).padStart(2, '0') }),
		}))
);

/** Hàng đầu co giãn theo số ô đang hiện (tần suất + giờ + 0..2 ô tuỳ tần suất). */
const runColSpan = computed(() => {
	const f = state.editForm.frequency;
	const count = 2 + (f === 'week' ? 1 : 0) + (needMonthInPeriod.value ? 1 : 0) + (needDayOfMonth.value ? 1 : 0);
	return 24 / count;
});

/**
 * Đổi tần suất: xoá các trường không thuộc tần suất mới và đặt mặc định cho trường mới hiện ra.
 * Backend cũng dọn (NormalizeRunRule) nhưng dọn ở đây để form hiển thị đúng ngay.
 */
const onFrequencyChange = (f: string) => {
	if (f === 'week') state.editForm.daysOfWeek = String(parseDays(state.editForm.daysOfWeek)[0] ?? 1);
	// Chuyển sang hàng ngày: chọn sẵn cả 7 thứ, người dùng bỏ bớt nếu cần
	else if (f === 'day') state.editForm.daysOfWeek = ALL_WEEKDAYS;
	else state.editForm.daysOfWeek = null;

	if (f === 'quarter' || f === 'year') {
		const max = f === 'quarter' ? 3 : 12;
		const m = state.editForm.monthInPeriod ?? 1;
		state.editForm.monthInPeriod = m >= 1 && m <= max ? m : 1;
	} else {
		state.editForm.monthInPeriod = null;
	}

	if (needDayOfMonth.value && !state.editForm.dayOfMonth) state.editForm.dayOfMonth = 1;
};

const scheduleSummary = computed(() => {
	const f = state.editForm.frequency;
	const time = state.runTime || '08:00';
	const d = state.editForm.dayOfMonth ?? 1;
	const day =
		d >= 31
			? $t('lz.label.emailSchedule.lastDayShort')
			: $t('lz.label.emailSchedule.dayNo', { day: String(d).padStart(2, '0') }).toLowerCase();
	const m = state.editForm.monthInPeriod ?? 1;

	switch (f) {
		case 'day': {
			const days = parseDays(state.editForm.daysOfWeek);
			if (days.length === 0) return $t('lz.validation.emailSchedule.daysOfWeekRequired');
			return days.length === 7
				? $t('lz.label.emailSchedule.summaryDay', { time })
				: $t('lz.label.emailSchedule.summaryDaySome', { time, days: days.map(weekdayLabel).join(', ') });
		}
		case 'week':
			return $t('lz.label.emailSchedule.summaryWeek', { time, day: weekdayLabel(weekDay.value).toLowerCase() });
		case 'month':
			return $t('lz.label.emailSchedule.summaryMonth', { time, day });
		case 'quarter':
			return $t('lz.label.emailSchedule.summaryQuarter', { time, day, n: m });
		case 'year':
			return $t('lz.label.emailSchedule.summaryYear', { time, day, month: String(m).padStart(2, '0') });
		default:
			return '';
	}
});

/**
 * Dữ liệu gửi sang khung xem trước — CHỈ được gửi lên BE khi bấm nút Xem trước.
 * Kỳ dữ liệu, tên trạm, tên tệp... do BE tính theo đúng luật của job gửi, FE không tự tính
 * (bản tính ở FE trước đây dễ lệch với thư thật).
 */
const previewForm = computed<MailPreviewScheduleInput>(() => {
	const [hh, mm] = (state.runTime || '08:00').split(':').map(Number);
	return {
		templateCode: state.editForm.templateCode,
		name: state.editForm.name,
		subject: state.editForm.subject,
		content: state.editForm.content,
		frequency: state.editForm.frequency,
		dayOfMonth: needDayOfMonth.value ? state.editForm.dayOfMonth : null,
		monthInPeriod: needMonthInPeriod.value ? state.editForm.monthInPeriod : null,
		daysOfWeek: ['day', 'week'].includes(state.editForm.frequency ?? '') ? state.editForm.daysOfWeek : null,
		hourOfDay: hh || 0,
		minuteOfHour: mm || 0,
		items: state.editForm.items ?? [],
	};
});

/**
 * Lựa chọn báo cáo, gom theo nhóm: báo cáo đang bật + báo cáo đã tắt mà lịch đang dùng (hiện mờ, không chọn
 * mới được) — để mở lịch cũ vẫn thấy đúng tên thay vì mã trơn.
 */
const reportGroups = computed(() => {
	const used = new Set((state.editForm.items ?? []).map((x) => x.reportCode).filter(Boolean));
	const options = state.listData.reports.filter((r) => isActiveReport(r) || used.has(r.code));
	return groupReports(options, state.listData.plugins);
});

/* ═════════════ Kỳ dữ liệu theo tần suất ═════════════
 * Kỳ của báo cáo = tần suất lịch (gửi hàng tháng = số liệu tháng). Báo cáo có kỳ CỐ ĐỊNH (periodType, khai trong
 * code BE) mà lệch tần suất vẫn lưu được — chạy kỳ của nó; ở đây chỉ hiện một dòng lưu ý. Cùng luật với
 * RptPeriodResolver.CheckFrequencyMatch ở BE (bản xem trước cũng trả cảnh báo này).
 */
const PERIOD_RANK: Record<string, number> = { day: 1, week: 2, month: 3, quarter: 4, year: 5 };
const PERIOD_NOUN_KEY: Record<string, string> = {
	day: 'periodNounDay',
	week: 'periodNounWeek',
	month: 'periodNounMonth',
	quarter: 'periodNounQuarter',
	year: 'periodNounYear',
};
const periodNoun = (p: string) => $t(`lz.label.emailSchedule.${PERIOD_NOUN_KEY[p]}`);

const periodWarning = (row: { reportCode?: string | null; periodOffset?: number | null }, line: number) => {
	const frequency = state.editForm.frequency ?? '';
	const fixed = state.listData.reports.find((r) => r.code === row.reportCode)?.periodType ?? '';
	const f = PERIOD_RANK[frequency];
	const p = PERIOD_RANK[fixed];
	if (!f || !p || f === p) return '';

	// Chạy dày hơn kỳ nhưng lấy kỳ hiện tại (lũy kế) là cách dùng đúng — mỗi lần gửi số liệu nhiều thêm
	if (f < p && row.periodOffset === 0) return '';

	const params = { line, frequency: periodNoun(frequency), period: periodNoun(fixed) };
	return f < p
		? $t('lz.label.emailSchedule.periodRepeatWarning', params)
		: $t('lz.label.emailSchedule.periodGapWarning', params);
};

/** Lưu ý của từng dòng báo cáo, cùng thứ tự với items. */
const itemWarnings = computed(() => (state.editForm.items ?? []).map((row, i) => periodWarning(row, i + 1)));

/** Khoá nút nhóm trong cây — có tiền tố để không bao giờ trùng mã báo cáo. */
const GROUP_KEY_PREFIX = '__group__:';

interface ReportTreeNode {
	value: string;
	label: string;
	disabled?: boolean;
	children?: ReportTreeNode[];
}

/** Cây cho el-tree-select: nhóm (kèm số báo cáo) → báo cáo. */
const reportTree = computed<ReportTreeNode[]>(() =>
	reportGroups.value.map((g) => ({
		value: GROUP_KEY_PREFIX + g.code,
		label: `${g.name} (${g.reports.length})`,
		children: g.reports.map((r) => ({
			value: r.code!,
			label: r.name || r.code!,
			disabled: !isActiveReport(r),
		})),
	}))
);

/**
 * Nhóm mở sẵn khi xổ ra: chỉ có 1 nhóm thì mở luôn (đỡ một lần bấm); nhiều nhóm thì chỉ mở nhóm chứa
 * báo cáo đang chọn, còn lại thu gọn cho gọn.
 */
const expandedGroupKeys = (reportCode?: string | null) => {
	const groups = reportGroups.value;
	if (groups.length === 1) return [GROUP_KEY_PREFIX + groups[0].code];
	const owner = groups.find((g) => g.reports.some((r) => r.code === reportCode));
	return owner ? [GROUP_KEY_PREFIX + owner.code] : [];
};

const addItem = () => {
	const first = reportGroups.value.flatMap((g) => g.reports).find(isActiveReport);
	if (!first) {
		ElMessage.warning($t('lz.message.emailSchedule.reportListEmpty'));
		return;
	}
	const previous = state.editForm.items?.[state.editForm.items.length - 1];

	// Loại kỳ (periodType) do backend gán theo báo cáo lúc lưu — FE không gửi
	state.editForm.items!.push({
		reportCode: first.code,
		periodOffset: PERIOD_OFFSET_PREVIOUS,
		format: previous?.format ?? 'pdf',
		orderNo: (state.editForm.items?.length ?? 0) + 1,
	});
};

const removeItem = (index: number) => {
	state.editForm.items!.splice(index, 1);
};

const editorHtml = computed({
	get: () => state.editForm.content ?? '',
	set: (html: string) => { state.editForm.content = html; },
});
const hasContent = (html?: string | null) => {
	const body = new DOMParser().parseFromString(html ?? '', 'text/html').body;
	return !!(body.textContent ?? '').replace(/\u00a0/g, ' ').trim() || !!body.querySelector('img, table, hr');
};
const contentValidator = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
	callback(hasContent(value) ? undefined : new Error('Vui lòng nhập nội dung thư'));
};

const loadTemplateContent = async (tpl: RptEmailTemplateOutput): Promise<string> => {
	if (tpl.content != null) return tpl.content;
	const res = await getAPI(RptEmailTemplateApi).apiMailRptemailtemplateBycodeGet(tpl.code ?? undefined);
	if (res.data.result?.content == null) {
		ElMessage.error('Không tải được nội dung template');
		throw new Error('Template không có nội dung');
	}
	return res.data.result.content;
};

const onTemplateChange = async (tpl: RptEmailTemplateOutput, previous?: RptEmailTemplateOutput) => {
	const request = ++templateRequest;
	const session = dialogRequest;
	const oldCode = previous?.code ?? state.loadedTemplateCode;
	const current = () => request === templateRequest && session === dialogRequest && state.isShowDialog;
	state.templateLoading = true;
	try {
		if (hasContent(state.editForm.content)) {
			await ElMessageBox.confirm(`Chọn mẫu "${tpl.name || tpl.code}" sẽ thay nội dung thư hiện tại. Tiếp tục?`, $t('lz.label.base.warning'), {
				confirmButtonText: $t('lz.button.base.confirm'), cancelButtonText: $t('lz.button.base.cancel'), type: 'warning',
			});
		}
		if (!current()) return;
		const content = await loadTemplateContent(tpl);
		if (!current()) return;
		state.editForm.content = content;
		state.loadedTemplateCode = tpl.code ?? '';
		state.editorKey++;
		editFormRef.value?.clearValidate('content');
	} catch {
		if (current()) state.editForm.templateCode = oldCode;
	} finally {
		if (request === templateRequest && session === dialogRequest) state.templateLoading = false;
	}
};

const handlePreview = async () => {
	if (state.previewing || state.templateLoading || state.isProcessing) return;
	const session = dialogRequest;
	state.previewing = true;
	state.previewVisible = true;
	try {
		await nextTick();
		if (session === dialogRequest && state.isShowDialog) await previewRef.value?.preview();
	} finally {
		if (session === dialogRequest) state.previewing = false;
	}
};

const openDialog = async (row: any) => {
	if (state.isProcessing) return;
	const session = ++dialogRequest;
	templateRequest++;
	editFormRef.value?.resetFields();
	state.editForm = JSON.parse(JSON.stringify(row ?? {}));
	if (!Array.isArray(state.editForm.items)) state.editForm.items = [];
	if (!state.editForm.frequency) state.editForm.frequency = 'month';
	state.showCcBcc = !!(state.editForm.cc || state.editForm.bcc);
	if (state.editForm.frequency === 'day' && !parseDays(state.editForm.daysOfWeek).length)
		state.editForm.daysOfWeek = ALL_WEEKDAYS;
	const hasSavedContent = row?.content != null;
	state.runTime = `${String(state.editForm.hourOfDay ?? 8).padStart(2, '0')}:${String(state.editForm.minuteOfHour ?? 0).padStart(2, '0')}`;
	state.loadedTemplateCode = state.editForm.templateCode ?? '';
	state.previewVisible = false;
	state.previewing = false;
	state.templateLoading = true;
	state.editorKey++;
	state.isShowDialog = true;
	try {
		const [templates, reports, plugins] = await Promise.all([loadMailTemplates().catch(() => []), loadMailReports(), loadMailReportPlugins()]);
		if (session !== dialogRequest || !state.isShowDialog) return;
		state.listData.reports = reports;
		state.listData.plugins = plugins;
		let tpl = templates.find((item) => item.code === state.editForm.templateCode);
		if (!state.editForm.templateCode) {
			tpl = templates.find((item) => item.code === DEFAULT_TEMPLATE) ?? templates[0];
			state.editForm.templateCode = tpl?.code ?? null;
		}
		state.loadedTemplateCode = state.editForm.templateCode ?? '';
		// A stored schedule draft takes precedence over the current master template.
		if (!hasSavedContent && state.editForm.templateCode) {
			const content = await loadTemplateContent(tpl ?? { code: state.editForm.templateCode });
			if (session !== dialogRequest || !state.isShowDialog) return;
			state.editForm.content = content;
			state.editorKey++;
		}
		await nextTick();
		editFormRef.value?.clearValidate();
	} catch {
		// API interceptor displays failures; keep any saved content in the form.
	} finally {
		if (session === dialogRequest) state.templateLoading = false;
	}
};

const beforeClose = (done: () => void) => { if (!state.isProcessing) done(); };
const closeDialog = () => { emits('handleQuery'); state.isShowDialog = false; };
const cancel = () => { if (!state.isProcessing) state.isShowDialog = false; };
watch(() => state.isShowDialog, (visible) => {
	if (visible) return;
	dialogRequest++;
	templateRequest++;
	state.previewVisible = false;
	state.previewing = false;
	previewRef.value?.reset();
});
onBeforeUnmount(() => { dialogRequest++; templateRequest++; });

const submit = async () => {
	if (state.isProcessing || state.templateLoading || state.previewing) return;
	state.isProcessing = true;
	try {
		if (!(await editFormRef.value?.validate().catch(() => false))) return;
		if (!state.editForm.items?.length) {
			ElMessage.warning($t('lz.message.emailSchedule.itemsRequired'));
			return;
		}
		const missingReport = state.editForm.items.findIndex((item) => !item.reportCode);
		if (missingReport >= 0) {
			ElMessage.warning($t('lz.message.emailSchedule.lineNoReport', { line: missingReport + 1 }));
			return;
		}
		// BE nhận int — dòng chưa chọn kỳ (dữ liệu cũ null) phải chặn ở đây, không để API trả lỗi đọc JSON
		const missingOffset = state.editForm.items.findIndex((item) => item.periodOffset == null);
		if (missingOffset >= 0) {
			ElMessage.warning(`#${missingOffset + 1}: ${$t('lz.label.base.pleaseSelect', { input: $t('lz.entity.emailSchedule.periodOffset') })}`);
			return;
		}
		// Use only fields in the current API contract, including this schedule's edited HTML.
		const payload: MailUpdateScheduleInput = {
			...previewForm.value,
			id: state.editForm.id,
			code: state.editForm.code,
			recipients: state.editForm.recipients,
			cc: state.editForm.cc,
			bcc: state.editForm.bcc,
			status: state.editForm.status,
			orderNo: state.editForm.orderNo,
			remark: state.editForm.remark,
			items: state.editForm.items.map((item, index) => ({
				reportCode: item.reportCode,
				periodOffset: item.periodOffset!,
				format: item.format,
				orderNo: index + 1,
			})),
		};
		if (props.operateType === 'edit') {
			await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleUpdatePost(payload);
		} else if (props.operateType === 'copy' || props.operateType === 'add') {
			payload.id = undefined;
			await getAPI(RptEmailScheduleApi).apiMailRptemailscheduleAddPost(payload);
		} else return;
		clearMailAddressCache();
		ElMessage.success($t('lz.message.base.processSuccess'));
		closeDialog();
	} catch {
		// Keep the draft open when the API fails.
	} finally {
		state.isProcessing = false;
	}
};

defineExpose({ openDialog });
</script>

<style scoped lang="scss" src="../../styles/mailEditForm.scss"></style>

<style scoped lang="scss">
.mb8 {
	margin-bottom: 8px;
}

.ml8 {
	margin-left: 8px;
}

.tool-row {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
}



/* ── Thẻ báo cáo đính kèm ── */
.item-card {
	padding: 8px 10px 10px;
	margin-bottom: 8px;
	border: 1px solid var(--el-border-color-lighter);
	border-radius: 6px;
	background: var(--el-fill-color-lighter);
}

.item-head {
	display: flex;
	align-items: center;
	gap: 6px;
	margin-bottom: 6px;
}

.item-no {
	flex-shrink: 0;
	width: 20px;
	height: 20px;
	border-radius: 50%;
	background: var(--el-color-primary-light-8);
	color: var(--el-color-primary);
	font-size: 12px;
	font-weight: 600;
	display: flex;
	align-items: center;
	justify-content: center;
}

.item-report {
	flex: 1;
	min-width: 0;
}

.item-warn {
	display: flex;
	align-items: flex-start;
	gap: 4px;
	margin-top: 6px;
	font-size: 12px;
	line-height: 1.5;
	color: var(--el-color-warning-dark-2);

	.el-icon {
		margin-top: 2px;
		flex-shrink: 0;
	}
}

.cc-toggle {
	margin-top: 2px;

	.cc-count {
		margin-left: 4px;
		color: var(--el-text-color-secondary);
	}
}

.cap {
	font-size: 11px;
	color: var(--el-text-color-secondary);
	margin: 4px 0 2px;
}

.ro {
	height: 24px;
	line-height: 24px;
	font-size: 12px;
	color: var(--el-text-color-regular);
}

.template-toolbar {
	display: flex;
	align-items: center;
	gap: 8px;
	flex-wrap: wrap;
	width: 100%;
	margin-bottom: 6px;
}

.template-select {
	flex: 1 1 200px;
	min-width: 0;
}

/* Only the report list scrolls; the recipient fields stay in place. */
.edit-layout :deep(.recipients-panel) {
	overflow: clip;
}

.edit-layout :deep(.recipients-panel .pane) {
	display: flex;
	flex-direction: column;
	min-height: 0;
	overflow: clip;
}

.edit-layout :deep(.recipients-panel .pane > *) {
	flex-shrink: 0;
}

.edit-layout :deep(.recipients-panel .report-list-scroll) {
	flex: 1 1 0;
	min-height: 0;
	max-height: 280px;
}

.edit-layout--stacked :deep(.recipients-panel .pane) {
	max-height: none;
}

.edit-layout--stacked :deep(.recipients-panel .report-list-scroll) {
	flex: 0 1 auto;
}

.report-list-scroll :deep(.el-scrollbar__wrap) {
	overscroll-behavior-y: contain;
}

.report-list-scroll :deep(.el-scrollbar__view) {
	padding-right: 10px;
}


/* ── Danh sách tag: gom vào một vùng nền nhạt, chip bo tròn ── */
.tag-bar,
.body-tag-bar {
	box-sizing: border-box;
	width: 100%;
	gap: 6px;
	padding: 8px;
	border: 1px dashed var(--el-border-color);
	border-radius: 4px;
	background: var(--el-fill-color-lighter);
}

.tag-bar {
	margin-top: 8px;
}

.tag-bar :deep(.tag-chip),
.body-tag-bar :deep(.tag-chip) {
	border-radius: 12px;
	padding: 0 10px;
	background: var(--el-bg-color);
	transition: background-color 0.2s, border-color 0.2s;
}

.tag-bar :deep(.tag-chip:hover),
.body-tag-bar :deep(.tag-chip:hover) {
	border-color: var(--el-color-primary);
	background: var(--el-color-primary-light-9);
}

.tag-bar :deep(.hint),
.body-tag-bar :deep(.hint) {
	display: none;
}

.body-tag-bar + .hint {
	margin: 4px 0 0;
}

/* Cột phải hẹp: tiêu đề nhóm cách khối phía trên rõ hơn */
.recipients-panel .divider:not(:first-child) {
	margin-top: 18px;
}

/* theme/element.scss đặt break-all cho mọi alert nên chữ bị ngắt giữa từ */
.recipients-panel :deep(.el-alert__title) {
	word-break: normal;
	overflow-wrap: break-word;
	line-height: 1.5;
}
</style>

<!-- Popper của el-tree-select được teleport ra body nên style scoped không áp được -->
<style lang="scss">
.mail-report-tree {
	.el-tree-node__content {
		height: 30px;
	}

	/* Nút nhóm: chữ đậm để phân biệt với báo cáo */
	.el-tree>.el-tree-node>.el-tree-node__content {
		font-weight: 600;
	}
}
</style>
