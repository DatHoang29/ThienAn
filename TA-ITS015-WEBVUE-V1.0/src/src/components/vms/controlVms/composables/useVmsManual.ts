import { VmsActionType, VmsMessageType } from '/@/types/enums/vmsEnum';
import { InformationProcessOutput } from '/@/api-services/vms';
import type { VmsControlDeps } from './types';

interface ManualDeps extends VmsControlDeps {
	// isRunning giữ ở index (dùng nhiều nơi), truyền xuống để displayInformationProcess dùng
	isRunning: (id: any) => boolean;
}

/**
 * Logic biến đổi dữ liệu cho mode manual:
 * - group thiết bị theo hướng
 * - bind message/phrase template vào select
 * - đổ lại thông tin tiến trình đang chạy theo loại điều khiển
 */
export function useVmsManual(deps: ManualDeps) {
	const { state, isRunning } = deps;

	// Nhóm danh sách thiết bị VMS theo hướng đường, sort theo tên trong từng nhóm.
	const buildEquipmentGroupByDirection = (sourceList?: any[]) => {
		const list = sourceList ?? state.equipmentList ?? [];
		const groupByDirection = list.reduce((accumulator: any, currentItem: any) => {
			const valueDirection = currentItem['directionId'];
			if (!valueDirection) return accumulator; // thiếu directionId thì không group được
			const nameDirection = state.directionList.find((item: any) => item.code == valueDirection)?.name;
			if (!nameDirection) return accumulator;
			if (!accumulator[nameDirection]) accumulator[nameDirection] = [];
			accumulator[nameDirection].push(currentItem);
			return accumulator;
		}, {});

		// Sort theo name (asc) cho từng nhóm hướng
		for (const key in groupByDirection) {
			if (Object.prototype.hasOwnProperty.call(groupByDirection, key) && Array.isArray(groupByDirection[key])) {
				groupByDirection[key].sort((a: any, b: any) => {
					const valueA = (a.name ?? '').toUpperCase();
					const valueB = (b.name ?? '').toUpperCase();
					if (valueA < valueB) return -1;
					if (valueA > valueB) return 1;
					return 0;
				});
			}
		}
		state.listData.equipmentListGroupByDirection = groupByDirection;

		// [DEBUG] Kiểm tra dữ liệu nhóm thiết bị theo hướng (manual)
		console.log('[controlVms] buildEquipmentGroupByDirection', {
			equipmentCount: list?.length ?? 0,
			directionCount: state.directionList?.length ?? 0,
			groupKeys: Object.keys(groupByDirection),
		});
	};

	// Bind dữ liệu message/phrase template vào select theo loại (mode manual - giữ nguyên từ vmsInfo).
	const bindVmsDataToSelect = (ctrlType: string) => {
		try {
			state.isLoading = true;
			if (ctrlType == VmsActionType.WordTemplate) {
				state.vmsWordTemplateAll = state.listData.msgTemplateAll?.filter((item: any) => item.type?.toLowerCase() == VmsMessageType.Word)?.map((item: any) => ({ code: item.code, content: item.content1 }));
			} else if (ctrlType == VmsActionType.TextTemplate) {
				state.vmsTextTemplateAll = state.listData.msgTemplateAll?.filter((item: any) => item.type.toLowerCase() == VmsMessageType.Sentence);
			} else if (ctrlType == VmsActionType.ImageTemplate) {
				// no-op
			}
		} catch (error) {
			console.log('Error: ', error);
		} finally {
			state.isLoading = false;
		}
	};

	// Đổ lại thông tin tiến trình đang chạy tương ứng với loại điều khiển (mode manual - giữ nguyên từ vmsInfo).
	const displayInformationProcess = (ctrlType: string) => {
		if (!state.listInformationProcess || state.listInformationProcess == undefined) return;

		const equipmentId = state.itemId;
		const informationProcess = (state.listInformationProcess as Array<InformationProcessOutput>).find(
			(item) => item.controlType == ctrlType && item.equipmentId == equipmentId
		) as InformationProcessOutput;
		if (!informationProcess) return;

		// Gán lại loại điều khiển
		state.ruleForm.controlType = ctrlType;

		// Trạng thái chạy lập lịch
		state.isRunningSchedule = isRunning(state.itemId);

		// Độ ưu tiên + độ sáng
		state.ruleForm.priority = (informationProcess?.priority as number) || 1;
		state.ruleForm.brightness = (informationProcess?.brightness as number) || 100;

		// Reset danh sách ảnh trước khi đổ lại
		state.scheduleImageList = [];

		switch (ctrlType) {
			case VmsActionType.Schedule:
				if (informationProcess?.scheduleList && informationProcess.scheduleList.length > 0) {
					informationProcess.scheduleList.forEach((selectedItem: any) => {
						state.scheduleImageList.push({
							id: selectedItem.id || '',
							preview: selectedItem.url || '',
							timeToLive: selectedItem.timeToLive || 0,
						});
					});
				}
				break;
			case VmsActionType.Time:
				state.ruleForm.urlImage = informationProcess.url;
				state.ruleForm.startTime = new Date(informationProcess.timeStart as any) as any;
				state.ruleForm.endTime = new Date(informationProcess.timeEnd as any) as any;
				break;
		}
	};

	// tra về các hàm để component cha gọi khi cần thiết từ index.vue (controlVms).
	return { buildEquipmentGroupByDirection, bindVmsDataToSelect, displayInformationProcess };
}
