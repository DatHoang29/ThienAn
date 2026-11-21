import type { MenuOutput } from '/@/api-services/system/models/menu-output';
import type { Router } from 'vue-router';
import { useMenuTabs } from '/@/stores/menuTabs';

/** Keep tab configuration out of router records, sidebar menus and tagsView. */
export function splitMenuTree(menus: MenuOutput[]) {
	const tabsByMenuId: Record<string, MenuOutput[]> = {};
	const copyNode = (node: MenuOutput): MenuOutput => ({
		...node,
		meta: node.meta ? { ...node.meta } : undefined,
		children: node.children?.map(copyNode),
	});
	const visit = (nodes: MenuOutput[]): MenuOutput[] => nodes
		.filter((node) => node.type !== 'tab' && node.type !== 'btn' && (node.status == null || Number(node.status) === 1))
		.map((node) => {
			if (node.id) {
				tabsByMenuId[node.id] = (node.children ?? [])
					.filter((child) => child.type === 'tab')
					.map(copyNode);
			}
			return { ...node, meta: node.meta ? { ...node.meta } : undefined, children: visit(node.children ?? []) };
		});
	return { routes: visit(menus), tabsByMenuId };
}

export function visibleMenuTabs(tabs: MenuOutput[], hasPermission: (permission: string) => boolean): MenuOutput[] {
	return tabs
		.filter((tab) => tab.id && tab.type === 'tab' && (tab.status == null || Number(tab.status) === 1)
			&& !tab.meta?.isHide && (!tab.permission || hasPermission(tab.permission)))
		.slice()
		.sort((a, b) => (a.orderNo ?? 0) - (b.orderNo ?? 0) || String(a.id).localeCompare(String(b.id)));
}

export function findMenuNode<T extends { id?: string | null; children?: T[] | null }>(nodes: T[], id: string): T | undefined {
	for (const node of nodes) {
		if (node.id === id) return node;
		const child = findMenuNode(node.children ?? [], id);
		if (child) return child;
	}
}

/** A tab has no URL and cannot be selected as a role's default route. */
export function excludeTabMenus<T extends { type?: string | null; children?: T[] | null }>(nodes: T[]): T[] {
	return nodes.filter((node) => node.type !== 'tab').map((node) => ({
		...node,
		children: node.children ? excludeTabMenus(node.children) : node.children,
	}));
}

const normalizeViewPath = (path: string) => path.trim()
	.replace(/^(?:\/src\/views\/|\/@\/views\/|\.\.\/views\/|\/views\/)/, '')
	.replace(/^\/+/, '').replace(/\.(vue|tsx)$/, '');

/** Resolve a compiled view by exact path; reject missing and ambiguous components. */
export function resolveViewModule<T>(modules: Record<string, T>, component?: string | null): T | undefined {
	const path = normalizeViewPath(component ?? '');
	if (!path || path.startsWith('system/old/') || path.startsWith('cfgSystem/old/') || path.startsWith('tms/old/') || path.startsWith('report/tms/old/') || path.startsWith('vms/old/') || path.startsWith('report/lcs/old/') || path.startsWith('report/vms/old/') || path.startsWith('report/fms/old/') || path.startsWith('sharedata/old/') || path.split('/').some((part) => !part || part === '.' || part === '..')) return;
	const keys = Object.keys(modules).filter((key) => normalizeViewPath(key) === path);
	return keys.length === 1 ? modules[keys[0]] : undefined;
}

/** Nơi mở 1 trang chức năng: menu thường ⇒ `path`; tab của menu nhóm tabPage ⇒ `path` của menu nhóm + ID menu nhóm + tab. */
export interface MenuPageTarget {
	path: string;
	menuId?: string;
	tab?: string;
}

/** Tìm trang chức năng theo `component` trong cây menu BE (bản gốc, còn node `tab`); không được cấp menu ⇒ undefined. */
export function findMenuPage(menus: MenuOutput[], component: string): MenuPageTarget | undefined {
	const target = normalizeViewPath(component);
	const find = (items: MenuOutput[], parent?: MenuOutput): MenuPageTarget | undefined => {
		for (const item of items ?? []) {
			if (item.component && normalizeViewPath(item.component) === target) {
				if (item.type !== 'tab') return item.path ? { path: item.path } : undefined;
				return parent?.path && parent.id ? { path: parent.path, menuId: parent.id, tab: item.name || (item.id as string) } : undefined;
			}
			const child = find(item.children ?? [], item);
			if (child) return child;
		}
	};
	return find(menus);
}

/** Mở trang tìm được bằng findMenuPage: là tab ⇒ ghi yêu cầu chọn tab (tabPage tự chọn) rồi điều hướng tới menu nhóm. */
export function openMenuPage(router: Router, target: MenuPageTarget) {
	if (target.menuId && target.tab) useMenuTabs().requestTab(target.menuId, target.tab);
	return router.push(target.path);
}
