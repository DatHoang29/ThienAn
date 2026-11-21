/** Icon theo danh mục, cập nhật tại chỗ; giữ tooltip khi snapshot không đổi. */
import { onMounted, onActivated, onDeactivated, onUnmounted } from 'vue';
import L from 'leaflet';
import { serveConfig } from '/@/utils/axios-utils';
import { useVdsEventSnapshot } from '/@/transporter/hooks/useVdsEventSnapshot';
import { auth } from '/@/utils/authFunction';
import type { EventSnapshotItemOutput } from '/@/api-services/vds';
export function useVdsEventMap(getMap: () => L.Map | null | undefined, open: (item: EventSnapshotItemOutput) => void) {
	const { items } = useVdsEventSnapshot(() => (getMap() ? {} : null));
	let overlay: L.LayerGroup | undefined, currentMap: L.Map | undefined;
	let timer: ReturnType<typeof setInterval> | undefined;
	const markers = new Map<string, { marker: L.Marker; item: EventSnapshotItemOutput; visual: string; label: string }>();
	function icon(raw: string | null | undefined, index: number) {
		const element = document.createElement('span');
		const fallback = () => {
			const theme = getComputedStyle(getMap()?.getContainer() ?? document.documentElement);
			element.replaceChildren();
			element.textContent = '▲';
			element.style.display = 'block';
			element.style.fontSize = '26px';
			element.style.color = theme.getPropertyValue('--el-color-warning').trim();
			element.style.textShadow = '0 0 2px ' + theme.getPropertyValue('--el-text-color-primary').trim();
		};
		fallback();
		if (raw?.trim()) {
			try {
				const url = new URL(raw.trim(), (serveConfig.basePath || window.location.origin).replace(/\/$/, '') + '/');
				if (['http:', 'https:'].includes(url.protocol) || /^data:image\//i.test(raw)) {
					const img = document.createElement('img');
					img.src = url.href;
					img.alt = '';
					img.width = 26;
					img.height = 26;
					img.onerror = fallback;
					element.replaceChildren(img);
				}
			} catch {
				/* Icon không hợp lệ: dùng ký hiệu dự phòng. */
			}
		}
		return L.divIcon({ html: element, className: 'vds-live-event-marker', iconSize: [26, 26], iconAnchor: [-8 - index * 26, 26] });
	}
	function draw() {
		const map = getMap();
		if (currentMap !== map) {
			overlay?.remove();
			overlay = undefined;
			markers.clear();
			currentMap = map ?? undefined;
		}
		if (!map) return;
		overlay ??= L.layerGroup().addTo(map);
		const positions = new Map<string, L.LatLng>();
		map.eachLayer((layer: any) => {
			const feature = layer.feature;
			const equipmentId = feature?.geometry?.type === 'Point' ? feature.properties?.id : layer.options?.id;
			if (equipmentId && layer.getLatLng) positions.set(String(equipmentId), layer.getLatLng());
		});
		const offsets = new Map<string, number>(),
			present = new Set<string>();
		[...items.value]
			.sort((a, b) => String(a.key).localeCompare(String(b.key)))
			.forEach((item) => {
				if (!item.key || !item.equipmentId) return;
				const position = positions.get(item.equipmentId);
				if (!position) return;
				const key = item.key;
				present.add(key);
				const index = offsets.get(item.equipmentId) ?? 0;
				offsets.set(item.equipmentId, index + 1);
				const visual = JSON.stringify([item.icon, index]);
				const label = item.eventTypeName + ' · ' + (item.licensePlate ?? item.equipmentName ?? '');
				let entry = markers.get(key);
				if (!entry) {
					const marker = L.marker(position, { icon: icon(item.icon, index), zIndexOffset: 1000 }).addTo(overlay!);
					entry = { marker, item, visual, label: '' };
					markers.set(key, entry);
					marker.on('click', () => {
						const latest = markers.get(key);
						if (latest && auth('tmsIncident:view')) open(latest.item);
					});
				}
				entry.item = item;
				if (!entry.marker.getLatLng().equals(position)) entry.marker.setLatLng(position);
				if (entry.visual !== visual) {
					entry.marker.setIcon(icon(item.icon, index));
					entry.visual = visual;
				}
				if (entry.label !== label) {
					const text = document.createElement('span');
					text.textContent = label;
					if (entry.marker.getTooltip()) entry.marker.setTooltipContent(text);
					else entry.marker.bindTooltip(text);
					entry.label = label;
				}
			});
		for (const [key, entry] of markers)
			if (!present.has(key)) {
				overlay.removeLayer(entry.marker);
				markers.delete(key);
			}
	}
	const start = () => {
		if (!timer) {
			draw();
			timer = setInterval(draw, 1000);
		}
	};
	const stop = () => {
		clearInterval(timer);
		timer = undefined;
		overlay?.remove();
		overlay = undefined;
		markers.clear();
		currentMap = undefined;
	};
	onMounted(start);
	onActivated(start);
	onDeactivated(stop);
	onUnmounted(stop);
}
