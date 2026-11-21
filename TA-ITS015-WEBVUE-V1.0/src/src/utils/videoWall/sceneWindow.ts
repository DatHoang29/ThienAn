
import type { VwWindowSceneOutput } from '/@/api-services/videoWall';
import { SceneWindowVisible } from '/@/api-services/videoWall';

export type EditWindow = VwWindowSceneOutput & {
	/** Khối chứa cửa sổ; x / y / w / h tính trong khối. */
	blockNo: number;
	x: number;
	y: number;
	w: number;
	h: number;
	zIndex: number;
	/** Khoá thao tác trên canvas — chỉ ở FE. */
	locked?: boolean;
};

/** Chuẩn bị bản ghi BE để đưa lên canvas: toạ độ rỗng coi là 0, khối rỗng coi là khối đầu tiên. */
export const toEditWindow = (w: VwWindowSceneOutput): EditWindow => ({
	...w,
	blockNo: w.blockNo ?? 0,
	x: w.x ?? 0,
	y: w.y ?? 0,
	w: w.w ?? 0,
	h: w.h ?? 0,
	zIndex: w.zIndex ?? 1,
	locked: false,
});

/** Làm tròn về bội số gần nhất của 1 ô. */
export const snapToCell = (value: number, panel: number) => Math.round(value / panel) * panel;

/** Làm tròn vị trí khi nhập số, kẹp trong [0, max] như khi kéo thả. */
export const snapPosition = (value: number, panel: number, max: number) => Math.max(0, Math.min(snapToCell(value, panel), max));

/** Làm tròn kích thước khi nhập số: nhỏ nhất 1 ô; vượt max (mép khối) thì lùi về số ô lớn nhất còn nằm trong khối. */
export const snapSize = (value: number, panel: number, max: number) => {
	const snapped = Math.max(panel, snapToCell(value, panel));
	return snapped <= max ? snapped : Math.max(panel, Math.floor(max / panel) * panel);
};

/** Cửa sổ có đang hiển thị không (BE rỗng = hiện). */
export const isWindowVisible = (w: Pick<VwWindowSceneOutput, 'visible'>) => w.visible !== SceneWindowVisible.NUMBER_0;
