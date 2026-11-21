import type { VwControllerOutput } from '/@/api-services/videoWall';
import { vwCtrlCode } from '/@/stores/its/videoWallController';

/** Nút cây bộ điều khiển cho el-cascader (`value: 'id'`, `label: 'label'`). */
export type ControllerNode = { id: string; label: string; children?: ControllerNode[] };

/** Props el-cascader dùng chung; `emitPath: false` ⇒ v-model là id bộ điều khiển (không phải mảng đường dẫn). */
export const CONTROLLER_CASCADER_PROPS = { value: 'id', label: 'label', children: 'children', emitPath: false, expandTrigger: 'hover' as const };

/**
 * Cây bộ điều khiển cha → con theo `parentControllerId`.
 * Gốc = bộ không có cha, cha không còn tồn tại hoặc tự trỏ vào chính nó; bộ nằm trong vòng lặp cha ↔ con (dữ liệu lỗi)
 * cũng được đưa lên gốc để vẫn thấy / chọn được.
 */
export function buildControllerTree(controllers: VwControllerOutput[]): ControllerNode[] {
	const labelOf = (c: VwControllerOutput) => `${vwCtrlCode(c.code)} — ${c.name ?? ''}`;
	const ids = new Set(controllers.map((c) => c.id).filter(Boolean));
	const byParent = new Map<string, VwControllerOutput[]>();
	for (const c of controllers) {
		if (!c.id) continue;
		const pid = c.parentControllerId && ids.has(c.parentControllerId) && c.parentControllerId !== c.id ? c.parentControllerId : '';
		byParent.set(pid, [...(byParent.get(pid) ?? []), c]);
	}
	const seen = new Set<string>();
	const build = (pid: string): ControllerNode[] =>
		(byParent.get(pid) ?? [])
			.filter((c) => !seen.has(c.id!) && seen.add(c.id!))
			.map((c) => {
				const children = build(c.id!);
				return { id: c.id!, label: labelOf(c), ...(children.length ? { children } : {}) };
			});
	const tree = build('');
	for (const c of controllers) if (c.id && !seen.has(c.id)) tree.push({ id: c.id, label: labelOf(c) });
	return tree;
}
