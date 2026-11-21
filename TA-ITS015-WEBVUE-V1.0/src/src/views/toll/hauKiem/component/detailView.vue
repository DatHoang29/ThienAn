<template>
	<div class="detail-view-container">
		<el-dialog v-model="state.isShowDialog" draggable :close-on-click-modal="false" width="min(1520px, 96vw)">
			<!-- HEADER -->
			<template #header>
				<div class="dialog-header">
					<div class="header-left">
						<i class="fa fa-eye"></i>
						<span>{{ $t('lz.button.base.detail') }}</span>

						<el-button
							:type="state.autoSwitch.active ? 'danger' : 'primary'"
							size="small"
							class="auto-btn"
							@click="toggleAutoSwitch"
							:icon="state.autoSwitch.active ? 'ele-VideoPause' : 'ele-VideoPlay'"
						>
						</el-button>
					</div>
				</div>
			</template>

			<!-- BODY -->
			<div class="dialog-body">
				<!-- ===== TOP ===== -->
				<div class="top-section">
					<div class="image-area">
					<div class="image-grid">
						<div class="section-header">
							<span>{{ $t('lz.label.tollHauKiem.directionIn') }}</span>
						</div>
						<div class="section-header">
							<span>{{ $t('lz.label.tollHauKiem.directionOut') }}</span>
						</div>

						<div class="image-section">
							<div class="section-header">
								<div class="info-content">
									{{ state.row?.stationInName }} - {{ state.row?.laneInName }} -
									{{ state.strList[0] && new Date(state.strList[0]).getFullYear() > 1900 ? useDateFormat(state.strList[0], DATE_TIME_FORMAT) : '' }}<br />
									{{ $t('lz.entity.toll.tagId') }}: {{ state.row?.tagId }} - {{ $t('lz.entity.toll.transactionId') }}: {{ state.row?.transactionId }}<br />
									<span class="plate-text">{{ state.row?.plate }}</span>
								</div>
							</div>
							<el-image class="camera-image" :preview-src-list="state.srcList" :initial-index="2" :src="state.srcList[2]" @error="handleImageError(2)" />
						</div>

						<div class="image-section">
							<div class="section-header">
								<div class="info-content">
									{{ state.row?.stationName }} - {{ state.row?.laneName }} -
									{{ state.strList[1] && new Date(state.strList[1]).getFullYear() > 1900 ? useDateFormat(state.strList[1], DATE_TIME_FORMAT) : '' }}<br />
									{{ $t('lz.entity.toll.tagId') }}: {{ state.row?.tagId }} - {{ $t('lz.entity.toll.transactionId') }}: {{ state.row?.transactionId }}<br />
									<span class="plate-text">{{ state.row?.plate }}</span>
								</div>
							</div>
							<el-image class="camera-image" :preview-src-list="state.srcList" :initial-index="3" :src="state.srcList[3]" @error="handleImageError(3)" />
						</div>

						<div class="image-section">
							<el-image class="camera-image" :preview-src-list="state.srcList" :initial-index="0" :src="state.srcList[0]" @error="handleImageError(0)" />
						</div>
						<div class="image-section">
							<el-image class="camera-image" :preview-src-list="state.srcList" :initial-index="1" :src="state.srcList[1]" @error="handleImageError(1)" />
						</div>
					</div>

					<!-- Nút chuyển nhanh lượt hậu kiểm trước / sau (tương đương phím ↑ / ↓), nằm trong khung ảnh, căn giữa 2 bên -->
					<div class="nav-rail">
						<button type="button" class="nav-btn" :disabled="state.currentIndex <= 0" @click="handlePrev">
							<el-icon><ele-ArrowLeftBold /></el-icon>
						</button>
						<button type="button" class="nav-btn" :disabled="state.currentIndex >= state.totalRows - 1" @click="handleNext">
							<el-icon><ele-ArrowRightBold /></el-icon>
						</button>
					</div>
				</div>

					<!-- UPDATE PANEL -->
					<div class="update-panel">
						<div class="panel-title">{{ $t('lz.entity.base.info') }}</div>

						<!-- Các field chỉ đọc lấy từ dòng giao dịch (ListTransactionOutPut), chưa có API cập nhật -->
						<div class="field-grid">
							<div class="field">
								<label>{{ $t('lz.entity.toll.plateLpr') }}</label>
								<el-input :model-value="state.row?.plateLpr" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.toll.price') }}</label>
								<el-input v-model="state.price" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.toll.shift') }}</label>
								<el-input v-model="state.shiftName" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.toll.proccessDate') }}</label>
								<el-input v-model="state.proccessDate" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.toll.reason') }}</label>
								<el-input :model-value="state.row?.reasonName" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.toll.statusChecked') }}</label>
								<el-input :model-value="state.row?.statusCheckedName" size="small" disabled />
							</div>

							<div class="field field--full">
								<label>{{ $t('lz.entity.tms.vehicleType') }}</label>
								<el-input :model-value="state.row?.vehicleTypeName" size="small" disabled />
							</div>

							<div class="field field--full">
								<label>{{ $t('lz.entity.toll.plateEdit') }}</label>
								<el-input v-model="state.plateEdit" size="small" disabled />
							</div>
						</div>

						<div class="panel-title panel-title--sub">{{ $t('lz.label.tollHauKiem.backendInfo') }}</div>

						<div class="field-grid">
							<div class="field">
								<label>{{ $t('lz.entity.tms.licensePlate') }}</label>
								<el-input v-model="state.plate" size="small" disabled />
							</div>

							<div class="field">
								<label>{{ $t('lz.entity.tms.vehicleType') }}</label>
								<el-input :model-value="state.row?.vehicleTypeName" size="small" disabled />
							</div>
						</div>

						<!-- TODO(TOLL): chưa có API TOLL tương ứng - danh mục ca làm việc
						<div class="field">
							<label>{{ $t('lz.entity.acs.shift') }}</label>
							<el-select v-model="state.row.shiftId" filterable clearable class="w100" :placeholder="$t('lz.label.base.selectAll')">
								<el-option v-for="item in state.listData.ShiftList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</div>
						-->

						<!-- TODO(TOLL): chưa có API TOLL tương ứng - danh mục lý do hậu kiểm
						<div class="field">
							<label>{{ $t('lz.entity.acs.reason') }}</label>
							<el-select v-model="state.row.reasonId" filterable clearable class="w100" :placeholder="$t('lz.label.base.selectAll')">
								<el-option v-for="item in state.listData.ReasonList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</div>
						-->

						<!-- TODO(TOLL): không có field trong DTO output - ô ghi chú
						<div class="field">
							<label>{{ $t('lz.entity.base.remark') }}</label>
							<el-input v-model="state.note" size="small" />
						</div>
						-->

						<!-- TODO(TOLL): chưa có API TOLL tương ứng - danh mục trạng thái hậu kiểm
						<div class="field">
							<label>{{ $t('lz.entity.acs.trangThai') }}</label>
							<el-select v-model="state.statusChecked" filterable clearable class="w100" :placeholder="$t('lz.label.base.selectAll')">
								<el-option v-for="item in state.listData.StatusList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</div>
						-->

						<!-- TODO(TOLL): chưa có API TOLL tương ứng - danh mục loại xe
						<div class="field">
							<label>{{ $t('lz.entity.tms.vehicleType') }}</label>
							<el-select v-model="state.vehicleType" filterable clearable class="w100" :placeholder="$t('lz.label.base.selectAll')">
								<el-option v-for="item in state.listData.vehicleTypeList" :key="item.id" :label="item.name" :value="item.id" />
							</el-select>
						</div>
						-->

						<!-- TODO(TOLL): chưa có API TOLL tương ứng - cập nhật giao dịch hậu kiểm
						<el-button type="primary" size="small" class="w100 mt5" @click="handleUpdate">
							{{ $t('lz.button.base.update') }}
						</el-button>
						-->
					</div>
				</div>

				<!-- TODO(TOLL): chưa có API TOLL tương ứng - lịch sử kiểm tra của giao dịch
				<div class="history-toggle">
					<el-checkbox v-model="state.showHistory"> Hiển thị lịch sử kiểm tra </el-checkbox>
				</div>

				<div v-if="state.showHistory" class="history-section">
					<div class="panel-title">Lịch sử kiểm tra</div>

					<el-table :data="state.historyList" border height="200" size="small" highlight-current-row>
						<el-table-column prop="userName" :label="$t('lz.entity.base.updateBy')" width="120" />
						<el-table-column prop="note" :label="$t('lz.entity.base.remark')" />
						<el-table-column :label="$t('lz.entity.base.updateTime')" width="160">
							<template #default="{ row }">
								{{ useDateFormat(row.createDate, DATE_TIME_FORMAT) }}
							</template>
						</el-table-column>
					</el-table>
				</div>
				-->
			</div>

			<!-- FOOTER -->
			<template #footer>
				<el-button @click="closeDialog">
					{{ $t('lz.button.base.close') }}
				</el-button>
			</template>
		</el-dialog>
	</div>
</template>

<script lang="ts" setup name="tollHauKiemDetail">
import { reactive, onMounted, onBeforeUnmount } from 'vue';
import { useDateFormat } from '@vueuse/core';
import { useI18n } from 'vue-i18n';
import noImage from '/@/assets/img/camera_unavailable.jpg';
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigApi } from '/@/api-services/system';
import { ListTransactionOutPut, HaukiemTransactionApi } from '/@/api-services/toll';
import { DATE_TIME_FORMAT } from '/@/types/formats';
import { useTollCatalog, type TollCatalogItem } from '/@/hooks/useTollCatalog';

/* TODO(TOLL): chưa có API TOLL tương ứng - cập nhật cờ f0 / cập nhật giao dịch / lấy lịch sử hậu kiểm
import { HkTransactionInApi } from '/@/api-services/tcs23HauKiem';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - danh mục trạng thái hậu kiểm / lý do (tcs23QuanLy)
import { MdsStatusApi, MdsReasonApi } from '/@/api-services/tcs23QuanLy';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - danh mục loại xe / ca làm việc (acs13QuanLy)
import { MdsLoaiXeApi, MdsCaApi } from '/@/api-services/acs13QuanLy';
*/
/* TODO(TOLL): chưa có trong TA-ITS015 - enum loại xử lý dùng để lọc danh mục loại xe
import { LoaiXuLyEnum } from '/@/types/enums';
*/
/* TODO(TOLL): chưa có API TOLL tương ứng - thông báo kết quả cập nhật
import { ElMessage } from 'element-plus';
*/

const { t: $t } = useI18n();
const { loadTollShifts } = useTollCatalog();
const emit = defineEmits(['change-row']);

const props = defineProps({
	leftListRef: { type: Object, required: false, default: undefined },
});

const state = reactive({
	isShowDialog: false,
	srcList: [noImage, noImage, noImage, noImage] as string[],
	strList: ['', '', '', ''] as any[],
	baseUrl: '',
	plate: '',
	plateEdit: undefined as any,
	price: undefined as any,
	proccessDate: undefined as any,
	shiftName: '',
	shiftList: [] as TollCatalogItem[],
	direction: undefined as any,
	statusChecked: undefined as any,
	statusCheckedName: undefined as any,
	vehicleType: undefined as any,
	note: '',
	row: undefined as any,
	showHistory: false,
	historyList: [] as any[],
	list: [] as any[],
	currentIndex: 0,
	totalRows: 0,
	listData: {
		/* TODO(TOLL): chưa có API TOLL tương ứng - các danh mục trạng thái / loại xe / ca / lý do
		StatusList: [] as any[],
		vehicleTypeList: [] as any[],
		ShiftList: [] as any[],
		ReasonList: [] as any[],
		*/
	},
	autoSwitch: {
		active: false,
		interval: 1000,
		timer: null as any,
		currentIndex: -1,
	},
});

// ================= INIT =================
onMounted(async () => {
	const rs = await getAPI(SysConfigApi).apiSystemSysconfigFileserverGet('absolute');
	state.baseUrl = rs.data.result as any;

	// Danh mục ca (TollShift) để hiển thị tên ca từ mã ca của giao dịch (ShiftCode = TollShift.ShiftId)
	state.shiftList = await loadTollShifts();

	/* TODO(TOLL): chưa có API TOLL tương ứng - nạp các danh mục trạng thái / ca / lý do / loại xe
	const rsStatus = await getAPI(MdsStatusApi).apiTcs23quanlyMdsstatusListbyconditionGet();
	state.listData.StatusList = rsStatus.data.result as any[];

	const rsShift = await getAPI(MdsCaApi).apiAcs13quanlyMdscaListGet();
	state.listData.ShiftList = rsShift.data.result as any[];

	const rsReason = await getAPI(MdsReasonApi).apiTcs23quanlyMdsreasonListbyconditionGet(undefined, undefined, true);
	state.listData.ReasonList = rsReason.data.result as any[];

	const rsVehicleType = await getAPI(MdsLoaiXeApi).apiAcs13quanlyMdsloaixeListPost({ vehicleType: LoaiXuLyEnum.ThuPhi } as ListLoaiXeInput);
	state.listData.vehicleTypeList = rsVehicleType.data.result as any[];
	*/

	window.addEventListener('keydown', handleKeydown);
});

onBeforeUnmount(() => {
	window.removeEventListener('keydown', handleKeydown);
	stopAutoSwitch();
});

// ================= OPEN =================
// Dữ liệu lấy từ dòng đã tải của grid, không gọi thêm API
const openDialog = (row: any, index?: number) => {
	setRowState(row, index ?? 0);

	// TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu lượt xe đã xem (updatef0)
	/*
	const res = await getAPI(HkTransactionInApi).tcsTcs23haukiemHktransactioninUpdatef0Post({ id: row.transactionId, direction: row.direction });
	if (res) {
		row.f0 = 1;
	}
	*/

	setImageList(row);
	state.isShowDialog = true;
};

// ================= AUTO SWITCH =================
const toggleAutoSwitch = () => {
	state.autoSwitch.active = !state.autoSwitch.active;

	if (state.autoSwitch.active) {
		startAutoSwitch();
	} else {
		stopAutoSwitch();
	}
};

const startAutoSwitch = () => {
	stopAutoSwitch();
	const ref = props.leftListRef;
	const interval = Number(state.autoSwitch.interval) || 1000;

	if (state.autoSwitch.currentIndex === -1) {
		state.autoSwitch.currentIndex = 0;
		ref?.selectRow?.(0);
	}
	state.autoSwitch.timer = setInterval(async () => {
		await handleNext();
	}, interval);
};

const stopAutoSwitch = () => {
	if (state.autoSwitch.timer) {
		clearInterval(state.autoSwitch.timer);
		state.autoSwitch.timer = null;
	}
};

// ================= NAV =================
const getTableData = () => {
	const ref = props.leftListRef;
	if (!ref) return [];
	const raw = ref.getTableData?.();
	return raw?.visibleData || raw?.tableData || raw || [];
};

const moveToRow = (index: number) => {
	const ref = props.leftListRef;
	const tableData = getTableData();
	const row = tableData[index];
	if (!row) return;

	state.currentIndex = index;
	state.autoSwitch.currentIndex = index;
	setRowState(row, index);

	// TODO(TOLL): chưa có API TOLL tương ứng - đánh dấu lượt xe đã xem (updatef0) khi chuyển dòng
	/*
	if (row.f0 != 1) {
		const res = await getAPI(HkTransactionInApi).tcsTcs23haukiemHktransactioninUpdatef0Post({ id: row.transactionId, direction: row.direction });
		if (res) {
			row.f0 = 1;
		}
	}
	*/

	if (typeof ref?.selectRow === 'function') {
		ref.selectRow(index);
		ref.handleScrollToRow?.(index);
	} else {
		ref?.setCurrentRow?.(row);
		ref?.scrollToRow?.(row);
	}

	setImageList(row);
	emit('change-row', row, index);
};

const handleNext = async () => {
	const tableData = getTableData();
	const nextIndex = state.currentIndex + 1;
	if (nextIndex >= tableData.length) return;
	moveToRow(nextIndex);
};

const handlePrev = async () => {
	if (state.currentIndex <= 0) return;
	moveToRow(state.currentIndex - 1);
};

// ================= KEY =================
const handleKeydown = (e: KeyboardEvent) => {
	if (!state.isShowDialog) return;

	const tag = (e.target as HTMLElement)?.tagName;
	if (tag === 'INPUT' || tag === 'TEXTAREA') return;

	// ↓ / → : lượt sau; ↑ / ← : lượt trước (khớp 2 nút chuyển nhanh trên form)
	if (e.key === 'ArrowDown' || e.key === 'ArrowRight') {
		e.preventDefault();
		handleNext();
	}
	if (e.key === 'ArrowUp' || e.key === 'ArrowLeft') {
		e.preventDefault();
		handlePrev();
	}
};

// ================= DATA =================
const setRowState = (row: any, index: number) => {
	state.row = row;
	state.currentIndex = index;
	state.totalRows = getTableData().length;
	state.statusChecked = row?.statusChecked;
	state.statusCheckedName = row?.statusCheckedName;
	state.vehicleType = row?.vehicleTypeId;
	state.direction = row?.direction;
};

const setImageList = async (row: any) => {
	const item = { ...row } as ListTransactionOutPut;

	state.strList = [item.transactionDatetimeIn, item.transactionDatetime, item.transactionDatetimeIn, item.transactionDatetime];

	state.plate = item.plate || '';
	state.plateEdit = item.plateEdit || '';
	state.price = item.price != null ? Number(item.price).toLocaleString('vi-VN') : '';
	state.proccessDate = item.proccessDate && new Date(item.proccessDate).getFullYear() > 1900 ? useDateFormat(item.proccessDate, DATE_TIME_FORMAT).value : '';
	state.shiftName = getShiftName(item.shiftId);

	// Ảnh fallback/tạm thời từ file server nếu có
	state.srcList = [
		resolve(item.imagePlateIn), 
		resolve(item.imagePlate), 
		resolve(item.imageLaneIn), 
		resolve(item.imageLane)
	];

	// Gọi API lấy ảnh hậu kiểm (base64) từ TCS
	if (item.imagePlateIn || item.imagePlate || item.imageLaneIn || item.imageLane) {
		try {
			const res = await getAPI(HaukiemTransactionApi).apiTollHaukiemtransactionImagePost({
				imagePlateIn: item.imagePlateIn,
				imagePlate: item.imagePlate,
				imageLaneIn: item.imageLaneIn,
				imageLane: item.imageLane,
			});
			const imgData = res.data?.result;
			if (imgData) {
				state.srcList = [
					imgData.imagePlateIn || resolve(item.imagePlateIn),
					imgData.imagePlate || resolve(item.imagePlate),
					imgData.imageLaneIn || resolve(item.imageLaneIn),
					imgData.imageLane || resolve(item.imageLane),
				];
			}
		} catch (error) {
			console.error('Error fetching transaction images:', error);
		}
	}
};

// TODO(TOLL): chưa có API TOLL tương ứng - cập nhật giao dịch hậu kiểm
/*
const handleUpdate = async () => {
	try {
		const row = state.row;
		const payload = {
			transactionId: row.transactionId,
			direction: state.direction,
			plateEdit: state.plateEdit,
			price: state.price + '',
			note: state.note,
			statusChecked: state.statusChecked,
			vehicleTypeId: state.vehicleType,
			reasonId: state.row.reasonId,
			shiftId: state.row.shiftId,
			proccessDate: state.row.proccessDate,
		};

		await getAPI(HkTransactionInApi).tcsTcs23haukiemHktransactioninUpdatetransactionPut(payload);

		Object.assign(row, {
			plateEdit: state.plateEdit,
			price: state.price,
			note: state.note,
			statusChecked: state.statusChecked,
			vehicleTypeId: state.vehicleType,
			statusCheckedName: state.listData.StatusList?.find((x) => x.id == state.statusChecked)?.name,
			reasonId: state.row.reasonId,
			shiftId: state.row.shiftId,
			proccessDate: state.row.proccessDate,
		});
		await loadHistory(row.transactionId);

		ElMessage.success($t('lz.message.base.saveSuccess'));
	} catch (e) {
		console.error(e);
	}
};
*/

// TODO(TOLL): chưa có API TOLL tương ứng - lấy lịch sử kiểm tra của giao dịch
/*
const loadHistory = async (id: any) => {
	try {
		const res = await getAPI(HkTransactionInApi).tcsTcs23haukiemHktransactioninGethistoryPost({ id });
		state.historyList = res?.data?.result || [];
	} catch (err) {
		console.error(err);
		state.historyList = [];
	}
};
*/

// Tên ca dạng "Ca 1: 06:00 - 14:00"; không tìm thấy trong danh mục thì hiển thị mã ca
const getShiftName = (shiftId?: string | null) => {
	if (!shiftId) return '';
	const shift = state.shiftList.find((x) => x.code === shiftId);
	if (!shift) return shiftId;

	const raw = shift.raw as any;
	const range = raw?.startTime && raw?.endTime ? `: ${useDateFormat(raw.startTime, 'HH:mm').value} - ${useDateFormat(raw.endTime, 'HH:mm').value}` : '';
	return `${shift.name}${range}`;
};

// ================= IMAGE =================
const resolve =(path?: string | null) => {
	if (!path) return noImage;
	return `${state.baseUrl}${path}`;
};

const handleImageError = (index: number) => {
	state.srcList[index] = noImage;
};

// ================= CLOSE =================
const closeDialog = () => {
	stopAutoSwitch();
	state.autoSwitch.active = false;
	state.isShowDialog = false;
};

defineExpose({ openDialog });
</script>

<style scoped>
.detail-view-container {
	width: 100%;
}

/* ================= DIALOG ================= */
:deep(.el-dialog) {
	border-radius: 12px;
	overflow: hidden;
	box-shadow: 0 18px 45px rgba(0, 0, 0, 0.16);
}

:deep(.el-dialog__header) {
	margin: 0;
	padding: 14px 18px;
	background: linear-gradient(135deg, #3e86bd, #378ace);
	border-bottom: 1px solid #87abc4;
}

:deep(.el-dialog__body) {
	padding: 16px;
	background: linear-gradient(to bottom, #f7fbfe, #edf3f8);
}

:deep(.el-dialog__footer) {
	padding: 12px 18px;
	background: #edf3f8;
	border-top: 1px solid #d8e4ee;
}

/* ================= HEADER ================= */
.dialog-header {
	display: flex;
	justify-content: space-between;
	align-items: center;
	color: #fff;
	font-weight: 700;
	font-size: 16px;
}

.header-left {
	display: flex;
	align-items: center;
	gap: 8px;
}

.dialog-header :deep(.el-button) {
	border-radius: 8px;
}

/* ================= BODY ================= */
.dialog-body {
	display: flex;
	flex-direction: column;
	gap: 16px;
}

/* ================= TOP SECTION ================= */
.top-section {
	position: relative;
	display: flex;
	gap: 18px;
}

/* ================= NAV PREV / NEXT ================= */
/* Khung chứa lưới ảnh + 2 nút chuyển; nút nằm trong khung ảnh */
.image-area {
	position: relative;
	flex: 2;
	min-width: 0;
	/* Chừa lề 2 bên cho nút chuyển, nút nằm ngoài khung ảnh */
	padding: 0 60px;
}

/* Lớp phủ căn 2 nút ở giữa hai bên, chỉ tính phần 2 hàng ảnh (bỏ hàng tiêu đề Chiều vào / Chiều ra) */
.nav-rail {
	position: absolute;
	left: 0;
	right: 0;
	top: calc(var(--image-head-height) + var(--image-grid-gap));
	bottom: 0;
	z-index: 10;
	display: flex;
	align-items: center;
	justify-content: space-between;
	/* Nút nằm trong lề 60px bên ngoài khung ảnh (nút 48px + 6px mỗi bên) */
	padding: 0 6px;
	pointer-events: none;
}

/* Nút màu chủ đạo + viền trắng: nổi rõ trên cả ảnh tối lẫn ảnh sáng */
.nav-btn {
	pointer-events: auto;
	flex-shrink: 0;
	width: 48px;
	height: 48px;
	padding: 0;
	display: flex;
	align-items: center;
	justify-content: center;
	border: 3px solid #fff;
	border-radius: 50%;
	background: #378ace;
	color: #fff;
	font-size: 20px;
	cursor: pointer;
	box-shadow: 0 4px 14px rgba(0, 0, 0, 0.45);
	transition: background 0.2s, transform 0.2s, opacity 0.2s;
}

.nav-btn:hover:not(:disabled) {
	background: #2b6fa5;
	transform: scale(1.08);
}

.nav-btn:disabled {
	opacity: 0.55;
	background: #6b7f90;
	cursor: not-allowed;
}

/* ================= IMAGE GRID ================= */
.image-area {
	--image-head-height: 34px;
	--image-grid-gap: 12px;
}

.image-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	gap: var(--image-grid-gap);
}

/* Hàng tiêu đề Chiều vào / Chiều ra cao cố định để căn nút giữa hai hàng ảnh */
.image-grid > .section-header {
	height: var(--image-head-height);
	padding-block: 0;
	border-radius: 8px;
}

.image-section {
	background: #fff;
	border: 1px solid #d9e6f0;
	border-radius: 12px;
	overflow: hidden;
	box-shadow: 0 6px 18px rgba(25, 42, 70, 0.06);
	display: flex;
	flex-direction: column;
	height: 270px;
}

.section-header {
	background: linear-gradient(180deg, #3e86bd, #378ace);
	color: #fff;
	padding: 8px 10px;
	font-size: 12px;
	font-weight: 600;
	display: flex;
	align-items: center;
	justify-content: center;
	text-align: center;
}

.section-header .info-content {
	width: 100%;
	line-height: 1.4;
	word-break: break-word;
}

.camera-image {
	flex: 1;
	width: 100%;
	background: #f8fbfe;
}

.plate-text {
	font-weight: bold;
	background: rgba(255, 255, 255, 0.24);
	padding: 2px 6px;
	border-radius: 6px;
	color: crimson;
}

/* ================= UPDATE PANEL ================= */
.update-panel {
	flex: 1;
	background: #fff;
	border: 1px solid #d9e6f0;
	border-radius: 12px;
	padding: 18px;
	box-shadow: 0 6px 18px rgba(25, 42, 70, 0.06);
}

.panel-title {
	font-size: 15px;
	font-weight: 700;
	color: #356e9d;
	margin-bottom: 16px;
	padding-bottom: 10px;
	border-bottom: 2px solid #edf4fa;
}

/* ================= FORM ================= */
.field {
	margin-bottom: 12px;
	display: flex;
	flex-direction: column;
}

.field label {
	font-size: 13px;
	font-weight: 600;
	color: #335b7e;
	margin-bottom: 6px;
}

.field-row {
	display: flex;
	gap: 14px;
}

.field-grid {
	display: grid;
	grid-template-columns: 1fr 1fr;
	column-gap: 12px;
}

.field-grid .field {
	min-width: 0;
}

.field--full {
	grid-column: 1 / -1;
}

.panel-title--sub {
	margin-top: 8px;
}

.w100 {
	width: 100%;
}

.mt5 {
	margin-top: 10px;
}

/* ================= BUTTONS ================= */
:deep(.el-button) {
	border-radius: 8px;
	font-weight: 500;
}

:deep(.el-button:hover) {
	transform: translateY(-1px);
	transition: 0.2s;
}

/* ================= HISTORY ================= */
.history-toggle {
	padding: 10px 14px;
	background: #fff;
	border-radius: 10px;
	border: 1px solid #d9e6f0;
}

.history-section {
	background: #fff;
	border: 1px solid #d9e6f0;
	border-radius: 12px;
	padding: 16px;
	box-shadow: 0 6px 18px rgba(25, 42, 70, 0.06);
}

/* ================= TABLE ================= */
:deep(.el-table) {
	border-radius: 10px;
	overflow: hidden;
}

:deep(.el-table th.el-table__cell) {
	background: linear-gradient(180deg, #3e86bd, #378ace);
	color: #fff;
	font-weight: 600;
}

:deep(.el-table__row:hover td) {
	background: #f1f8ff !important;
}

/* ================= INPUT ================= */
:deep(.el-input__wrapper),
:deep(.el-select__wrapper) {
	border-radius: 8px;
}

/* ================= FOOTER ================= */
:deep(.el-dialog__footer .el-button) {
	min-width: 110px;
}

/* hide old */
.header-right {
	display: none;
}
</style>
