import { TmsEquipmentApi, TmsEquipmentTypeApi } from '/@/api-services/tms';
import { getAPI } from '/@/utils/axios-utils';

export interface VdsCameraOption {
	id: string;
	name: string;
	code?: string | null;
	kmNumber?: number | null;
	metNumber?: number | null;
}

/** Only VDS cameras can form a journey segment. */
export async function loadVdsCameras(): Promise<VdsCameraOption[]> {
	const [equipmentResponse, typeResponse] = await Promise.all([
		getAPI(TmsEquipmentApi).apiTmsTmsequipmentListGet(),
		getAPI(TmsEquipmentTypeApi).apiTmsTmsequipmenttypeListGet(),
	]);
	const vdsTypes = new Set(
		(typeResponse.data.result ?? [])
			.filter(type => String(type.code ?? '').trim().toUpperCase() === 'VDS')
			.map(type => String(type.id))
	);
	return (equipmentResponse.data.result ?? [])
		.filter(camera => !!camera.id && vdsTypes.has(String(camera.equipmentTypeId)))
		.map(camera => ({
			id: String(camera.id),
			name: camera.name || camera.code || String(camera.id),
			code: camera.code,
			kmNumber: camera.kmNumber,
			metNumber: camera.metNumber,
		}))
		.sort((a, b) => a.name.localeCompare(b.name, 'vi'));
}

export function cameraLabel(camera: VdsCameraOption): string {
	const position = camera.kmNumber == null && camera.metNumber == null
		? ''
		: ` (Km ${camera.kmNumber ?? 0}+${String(camera.metNumber ?? 0).padStart(3, '0')})`;
	return `${camera.name}${position}`;
}
