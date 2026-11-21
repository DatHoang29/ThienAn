// Author: KienLT
// Description: Tiện ích cây zone dùng chung cho các tab của trang Zone.
// Created date: 08/10/2026

/** Node cây zone hiển thị ở zoneTreePanel. */
export interface ZoneTreeNode {
	id: string;
	name: string;
	kmText?: string;
	children?: ZoneTreeNode[];
	[key: string]: any;
}

export interface FlatZone {
	id: string;
	pid?: string | null;
	name?: string | null;
	code?: string | null;
	[key: string]: any;
}

const isRootPid = (pid: string | null | undefined, ids: Set<string>) => !pid || pid === '0' || !ids.has(pid);

/** Làm phẳng cây (GetTree trả lồng `children`) thành danh sách, giữ thứ tự duyệt. */
export const flattenZoneTree = <T extends { children?: T[] }>(nodes: T[] | null | undefined): T[] => {
	const result: T[] = [];
	const walk = (list: T[] | null | undefined) => list?.forEach(node => { result.push(node); walk(node.children); });
	walk(nodes);
	return result;
};

/** Map pid → con trực tiếp. */
export const childrenMap = <T extends FlatZone>(zones: T[]) => {
	const ids = new Set(zones.map(z => z.id));
	const map = new Map<string, T[]>();
	for (const zone of zones) {
		if (isRootPid(zone.pid, ids)) continue;
		if (!map.has(zone.pid!)) map.set(zone.pid!, []);
		map.get(zone.pid!)!.push(zone);
	}
	return map;
};

/** Mọi zone con cháu của `id` (không gồm chính nó). */
export const descendantIds = (id: string, children: Map<string, FlatZone[]>): string[] => {
	const result: string[] = [];
	const stack = [...(children.get(id) ?? [])];
	const seen = new Set<string>();
	while (stack.length) {
		const zone = stack.pop()!;
		if (seen.has(zone.id)) continue;
		seen.add(zone.id);
		result.push(zone.id);
		stack.push(...(children.get(zone.id) ?? []));
	}
	return result;
};

/**
 * Dựng cây từ danh sách phẳng. `include` (nếu có): chỉ giữ zone thuộc tập này VÀ tổ tiên của chúng, để kết quả
 * lọc vẫn hiện đúng phân cấp. `decorate` gắn thêm trường hiển thị (kmText…) cho từng node.
 */
export const buildZoneTree = <T extends FlatZone>(
	zones: T[],
	include?: Set<string> | null,
	decorate?: (zone: T) => Partial<ZoneTreeNode>
): ZoneTreeNode[] => {
	const ids = new Set(zones.map(z => z.id));
	const byId = new Map(zones.map(z => [z.id, z]));

	let keep: Set<string> | null = null;
	if (include) {
		keep = new Set<string>();
		for (const id of include) {
			let current: T | undefined = byId.get(id);
			const guard = new Set<string>();
			while (current && !guard.has(current.id)) {
				guard.add(current.id);
				keep.add(current.id);
				current = isRootPid(current.pid, ids) ? undefined : byId.get(current.pid!);
			}
		}
	}

	const children = childrenMap(zones);
	const build = (zone: T, guard: Set<string>): ZoneTreeNode => {
		const next = new Set(guard).add(zone.id);
		const kids = (children.get(zone.id) ?? [])
			.filter(c => !next.has(c.id) && (!keep || keep.has(c.id)))
			.map(c => build(c as T, next));
		// Bỏ `children` gốc (dữ liệu GetTree đã lồng sẵn) — cây con phải dựng lại theo bộ lọc.
		const { children: _original, ...rest } = zone as T & { children?: unknown };
		return {
			...rest,
			id: zone.id,
			name: zone.name || zone.code || zone.id,
			...(decorate?.(zone) ?? {}),
			...(kids.length ? { children: kids } : {}),
		} as ZoneTreeNode;
	};

	return zones
		.filter(z => isRootPid(z.pid, ids) && (!keep || keep.has(z.id)))
		.map(z => build(z, new Set()));
};
