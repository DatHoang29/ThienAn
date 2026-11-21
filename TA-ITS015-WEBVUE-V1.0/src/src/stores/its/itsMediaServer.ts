/**
 * Description: Media/file server của dự án ITS — địa chỉ server dùng chung + các hàm lấy đúng ảnh nghiệp vụ.
 *   Dùng: const media = useItsMediaServerStore();
 *         <img :src="media.eventTypeIcon(type)" @error="media.onEventTypeIconError" />
 *   Thêm loại ảnh mới: viết thêm hàm trong phần extend bên dưới, gọi core.fileUrl(...).
 * Created date: 28/10/2026
 */
import { getAPI } from '/@/utils/axios-utils';
import { SysConfigApi } from '/@/api-services/system';
import type { EquipmentTypeOutput, EventTypeOutput } from '/@/api-services/tms';
import { TmsEquipmentStateValueEnum } from '/@/types/enums';
import { createMediaServerStore, type FileUrlOptions } from '/@/utils/media/createMediaServerStore';
import incidentFallbackIcon from '/@/assets/incident-fallback.svg';

type EventTypeIconSource = Pick<EventTypeOutput, 'url'>;
type EquipmentTypeIconSource = Pick<EquipmentTypeOutput, 'defaultIcon' | 'activeIcon' | 'inactiveIcon' | 'faultIcon'>;

export const useItsMediaServerStore = createMediaServerStore(
	'itsMediaServer',
	{
		loadBaseUrl: async () => (await getAPI(SysConfigApi).apiSystemSysconfigFileserverGet()).data.result,
	},
	(core) => ({
		/** Icon loại sự cố/sự kiện (trường url của danh mục); thiếu/lỗi ⇒ icon sự cố mặc định. */
		eventTypeIcon: (type?: EventTypeIconSource | null, opts: FileUrlOptions = {}) => core.fileUrl(type?.url, { fallback: incidentFallbackIcon, ...opts }),

		/** Gắn vào @error của <img> icon loại sự cố. */
		onEventTypeIconError: (e: Event) => core.onImgError(e, incidentFallbackIcon),

		/**
		 * Icon loại thiết bị theo trạng thái (active/inactive/fault); không truyền trạng thái hoặc
		 * trạng thái chưa có icon riêng ⇒ dùng icon mặc định của loại thiết bị.
		 */
		equipmentTypeIcon: (type?: EquipmentTypeIconSource | null, state?: TmsEquipmentStateValueEnum | string | null, opts: FileUrlOptions = {}) => {
			const byState: Record<string, string | null | undefined> = {
				[TmsEquipmentStateValueEnum.Active]: type?.activeIcon,
				[TmsEquipmentStateValueEnum.Inactive]: type?.inactiveIcon,
				[TmsEquipmentStateValueEnum.Fault]: type?.faultIcon,
			};
			return core.fileUrl((state ? byState[state] : null) || type?.defaultIcon, opts);
		},
	})
);
