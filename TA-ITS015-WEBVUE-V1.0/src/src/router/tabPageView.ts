import { defineComponent, h } from 'vue';
import type { Component } from 'vue';

export type TabPageViewLoader = () => Promise<{ default: Component }>;

/** KeepAlive uses the backend route name; each menu needs its own named instance. */
export function createTabPageRouteView(loader: TabPageViewLoader, menu: { id?: string | null; name?: string | null }) {
	const menuId = String(menu.id ?? '');
	const name = menu.name || `managementMenu${menuId}`;
	return async () => {
		const { default: view } = await loader();
		return defineComponent({
			name,
			inheritAttrs: false,
			setup(_props, { attrs }) {
				return () => h(view, { ...attrs, menuId });
			},
		});
	};
}
