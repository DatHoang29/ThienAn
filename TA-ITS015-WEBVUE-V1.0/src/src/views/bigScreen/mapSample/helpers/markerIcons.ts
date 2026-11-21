import L from 'leaflet';
import { useMapStore } from '/@/stores/its/mapStore';
import { useItsMediaServerStore } from '/@/stores/its/itsMediaServer';
import { useBaseConfig } from '/@/stores/baseConfig';
import { BaseConfigTypeEnum, TmsEquipmentStateValueEnum, TmsEquipmentType, TmsWorkUnitType } from '/@/types/enums';
import Image from '/@/assets/toll-road.png';
import { createCustomDivIcon } from './mapViewUtils';

/**
 * Các hàm tạo icon marker hiển thị trên bản đồ.
 * Tách khỏi LeafletMap.vue để component gọn hơn; các giá trị phụ thuộc
 * vào state/props của component được truyền vào qua tham số.
 */

// #region Types

interface RenderMapLike {
    id?: string;
    itemGroups?: any;
    hasUnsavedChanges?: boolean;
    itemDetails: any;
}

// #endregion

// #region Equipment Image Marker (VMS / LCS)

/** Màu viền khung thiết bị theo trạng thái (xanh = hoạt động, đỏ = lỗi/ngưng). */
const VMS_BORDER_ACTIVE = 'var(--el-color-success)';
const VMS_BORDER_INACTIVE = 'var(--el-color-info)';
const VMS_BORDER_FAULT = 'var(--el-color-danger)';

/**
 * Màu đại diện cho 1 nhóm trạng thái thiết bị (giá trị `value` của config
 * `equipment_state`, ví dụ 'active' | 'fault' | 'inactive').
 *
 * Ưu tiên màu do người dùng cấu hình trong `equipment_state_color`; nếu chưa cấu
 * hình thì dùng màu semantic của Element Plus. Hàm được dùng CHUNG cho marker
 * trên bản đồ và bảng chú thích (MapLegend) để 2 nơi không bao giờ lệch màu.
 */
export const resolveEquipmentStateColor = (stateValue?: string | null): string => {
    const configuredColor = String(useBaseConfig().getConfigItemByCode(BaseConfigTypeEnum.TmsEquipmentStateColor, String(stateValue ?? ''))?.value ?? '').trim();
    if (configuredColor) return configuredColor;

    switch (stateValue) {
        case TmsEquipmentStateValueEnum.Active:
            return VMS_BORDER_ACTIVE;
        case TmsEquipmentStateValueEnum.Fault:
            return VMS_BORDER_FAULT;
        default:
            return VMS_BORDER_INACTIVE;
    }
};

// ============================================================================
//  THÔNG SỐ HIỂN THỊ MARKER THIẾT BỊ (chỉnh nhanh kích thước tại đây)
//  - Kích thước khung hiển thị trên bản đồ (px). Ảnh nội dung dùng object-fit:fill
//    nên sẽ lấp đầy đúng khung theo tỉ lệ width x height cấu hình bên dưới.
//  - VMS: bảng chữ (mặc định nội dung 384x64). LCS: đèn làn (mặc định 128x128).
// ============================================================================
const VMS_MARKER_WIDTH = 140; // chiều rộng VMS (ngắn lại)
const VMS_MARKER_HEIGHT = 46; // chiều cao VMS (cao hơn)

const LCS_MARKER_WIDTH = 44; // LCS
const LCS_MARKER_HEIGHT = 44;

const EQUIP_MARKER_RADIUS = 6; // bo góc khung
const EQUIP_DEFAULT_WIDTH = 118; // fallback khi loại thiết bị chưa cấu hình
const EQUIP_DEFAULT_HEIGHT = 46;

// Kích thước icon ĐƠN VỊ (work unit: trạm thu phí, đội tuần tra, ...).
// Trước đây hardcode 32 nên nhỏ hơn icon thiết bị. Tạm thời tăng cứng lên cho cân đối;
// nếu map có cấu hình iconSize chung thì ưu tiên theo cấu hình (giống icon thiết bị).
const UNIT_MARKER_SIZE = 40;

// Size CỨNG riêng theo loại đơn vị (work_unit_type). TẠM THỜI hardcode vì work unit KHÔNG
// có cột "Kích thước" trong bảng loại thiết bị. Loại có trong map này sẽ ĐÈ cả icon chung.
// - Trạm thu phí ('7'): to nhất.
const UNIT_TYPE_SIZE: Record<string, number> = {
    [TmsWorkUnitType.TollStation]: 64,
};
// ============================================================================

/**
 * Cấu hình khung marker cho từng loại thiết bị hiển thị bằng hình ảnh nội dung.
 * - width/height: kích thước hiển thị trên bản đồ (px).
 * - radius: bo góc khung.
 */
interface EquipMarkerConfig {
    width: number;
    height: number;
    radius: number;
}

const EQUIP_MARKER_CONFIG: Record<string, EquipMarkerConfig> = {
    [TmsEquipmentType.Vms]: { width: VMS_MARKER_WIDTH, height: VMS_MARKER_HEIGHT, radius: EQUIP_MARKER_RADIUS },
    [TmsEquipmentType.LCS]: { width: LCS_MARKER_WIDTH, height: LCS_MARKER_HEIGHT, radius: EQUIP_MARKER_RADIUS },
};

const EQUIP_DEFAULT_CONFIG: EquipMarkerConfig = { width: EQUIP_DEFAULT_WIDTH, height: EQUIP_DEFAULT_HEIGHT, radius: EQUIP_MARKER_RADIUS };

/** Lấy cấu hình khung theo mã loại thiết bị (fallback về default nếu chưa cấu hình). */
export const getEquipMarkerConfig = (typeCode?: string): EquipMarkerConfig => {
    return (typeCode && EQUIP_MARKER_CONFIG[typeCode]) || EQUIP_DEFAULT_CONFIG;
};

/** Loại thiết bị có được hiển thị dạng marker hình ảnh (VMS/LCS) hay không. */
export const isImageEquipmentType = (typeCode?: string): boolean => {
    return typeCode === TmsEquipmentType.Vms || typeCode === TmsEquipmentType.LCS;
};

/**
 * Dựng markup HTML cho khung thiết bị: hiển thị hình ảnh nội dung hiện tại trong 1
 * khung có viền bo tròn, đổi màu theo trạng thái. Ảnh dùng object-fit:fill để lấp
 * đầy đúng khung theo kích thước width x height cấu hình.
 */
const buildEquipFrameHtml = (options: {
    borderColor: string;
    contentImageUrl: string | undefined;
    width: number;
    height: number;
    radius: number;
}): string => {
    const { borderColor, contentImageUrl, width, height, radius } = options;

    const imgTag = contentImageUrl
        ? `<img src="${contentImageUrl}" onerror="this.style.display='none'" style="width:100%; height:100%; object-fit:fill; display:block;" />`
        : '';

    return `
         <div class="vms-marker-frame" style="width:${width}px; height:${height}px; box-sizing:border-box; background:#0C0F14; border:2px solid ${borderColor}; border-radius:${radius}px; overflow:hidden; box-shadow:0 2px 6px rgba(0,0,0,0.4);">
           ${imgTag}
         </div>
      `;
};

/**
 * Xác định màu viền khung thiết bị (VMS/LCS) theo trạng thái hoạt động:
 * - xanh (success)  : đang hoạt động (active)
 * - đỏ  (danger)    : bị lỗi (fault)
 * - xám (secondary) : không kết nối / ngưng (inactive / default)
 * Ánh xạ equipmentState -> value qua SysConfig giống logic icon thiết bị.
 */
const resolveEquipBorderColorByStatus = (equipment: any): string => {
    const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
    const listState = getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState);
    const stateEquip = listState.find((s: any) => s.code == equipment?.equipmentState);
    const stateValue = stateEquip ? stateEquip.value : TmsEquipmentStateValueEnum.Inactive;

    return resolveEquipmentStateColor(stateValue);
};

/**
 * Giải nén URL hình ảnh nội dung hiện tại của thiết bị (VMS/LCS) nếu có.
 * Trả về chuỗi rỗng khi đang chỉnh sửa chính nó hoặc chưa có ảnh.
 */
export const resolveVmsContentImageUrl = (feature: any, selectedItemId: any): string => {
    const mapStore = useMapStore();
    if (selectedItemId === feature.properties.id) return '';
    const currentVms = mapStore.vmsCurrentList.find((item: any) => item.equipmentId === feature.properties.id);
    return currentVms ? useItsMediaServerStore().fileUrl(currentVms.url) : '';
};

/**
 * Dựng nhãn tên gắn LIỀN trong icon (position:absolute ngay phía trên khung).
 * Đặt trong icon để nhãn di chuyển CÙNG icon khi kéo-thả (không lệch) và luôn
 * sát icon. Dùng class 'marker-name' để bật/tắt theo mapStore.showIconName.
 */
const buildMarkerLabelHtml = (label?: { name?: string; unsaved?: boolean }): string => {
    if (!label?.name) return '';
    const star = label.unsaved ? '*' : '';
    return `<div class="marker-name" style="position:absolute; left:50%; bottom:calc(100% + 1px); transform:translateX(-50%); font-size:10px; font-weight:600; color:#000; white-space:nowrap; pointer-events:none; text-shadow:1px 1px 2px #fff,-1px -1px 2px #fff,1px -1px 2px #fff,-1px 1px 2px #fff;">${label.name}${star}</div>`;
};

/**
 * Render icon marker cho thiết bị hiển thị bằng hình ảnh (VMS/LCS).
 * Viền chuyển màu xanh khi hoạt động, đỏ khi lỗi/ngưng. Neo ở TÂM để vị trí
 * ổn định khi zoom in/out (giống các marker khác).
 *
 * Kích thước resize theo zoom bằng cách nhân trực tiếp width/height (setIcon lại),
 * KHÔNG dùng CSS transform để tránh lệch vị trí với leaflet-rotate.
 * Nhãn tên nhúng LIỀN trong icon (không dùng tooltip riêng) để kéo-thả không lệch.
 *
 * @param typeCode mã loại thiết bị (TmsEquipmentType.Vms | TmsEquipmentType.LCS)
 * @param scale hệ số phóng theo zoom (mặc định 1)
 * @param label nhãn tên hiển thị kèm (name + cờ chưa lưu)
 */
export const buildEquipmentImageMarkerIcon = (
    feature: any,
    equipment: any,
    selectedItemId: any,
    typeCode?: string,
    label?: { name?: string; unsaved?: boolean }
): L.DivIcon => {
    const cfg = getEquipMarkerConfig(typeCode);
    const contentImageUrl = resolveVmsContentImageUrl(feature, selectedItemId);

    // iconSize CỐ ĐỊNH theo cấu hình (không đổi theo zoom) → Leaflet/leaflet-rotate
    // định vị marker ổn định như các marker thường, KHÔNG bị lệch khi zoom.
    const { width, height } = cfg;

    const frameHtml = buildEquipFrameHtml({
        borderColor: resolveEquipBorderColorByStatus(equipment),
        contentImageUrl,
        width,
        height,
        radius: cfg.radius,
    });

    const labelHtml = buildMarkerLabelHtml(label);

    // Kích thước CỐ ĐỊNH theo cấu hình, KHÔNG scale theo zoom. Marker định vị hoàn
    // toàn bằng iconSize/anchor của Leaflet (giống marker device) nên luôn bám đúng
    // vị trí ở mọi mức zoom, không bị dịch/nhảy.
    const html = `<div style="position:relative; width:${width}px; height:${height}px;">${frameHtml}${labelHtml}</div>`;

    return L.divIcon({
        className: 'custom-div-icon',
        html,
        iconSize: [width, height],
        iconAnchor: [width / 2, height / 2],
        popupAnchor: [0, -height / 2],
    });
};

/**
 * Trả về icon marker theo LOẠI thiết bị (equipmentType) — dùng switch-case để
 * dễ mở rộng thêm cách hiển thị cho các loại thiết bị mới sau này.
 *
 * @param typeCode mã loại thiết bị (equipmentType.code)
 * @param label nhãn tên hiển thị kèm
 * @returns L.DivIcon nếu loại này có marker hiển thị tùy biến (VMS/LCS/...);
 *          null nếu loại này không có marker tùy biến (để dùng icon device mặc định).
 */
export const buildEquipmentMarkerIconByType = (
    feature: any,
    equipment: any,
    selectedItemId: any,
    typeCode?: string,
    label?: { name?: string; unsaved?: boolean }
): L.DivIcon | null => {
    switch (typeCode) {
        // Thiết bị hiển thị bằng hình ảnh nội dung (bảng VMS / đèn làn LCS)
        case TmsEquipmentType.Vms:
        case TmsEquipmentType.LCS:
            return buildEquipmentImageMarkerIcon(feature, equipment, selectedItemId, typeCode, label);

        // TODO: bổ sung case cho các loại thiết bị cần hiển thị tùy biến khác tại đây
        // case TmsEquipmentType.Xxx:
        //     return buildXxxMarkerIcon(feature, equipment, selectedItemId, typeCode, label);

        default:
            return null;
    }
};

/**
 * Dựng nội dung HTML cho popup hover của thiết bị: hiển thị hình ảnh nội dung
 * hiện tại ở kích thước lớn để dễ nhìn. Trả về chuỗi rỗng nếu không có ảnh.
 */
export const buildVmsContentTooltip = (feature: any, selectedItemId: any, typeCode?: string): string => {
    const contentImageUrl = resolveVmsContentImageUrl(feature, selectedItemId);
    if (!contentImageUrl) return '';

    // LCS dùng class riêng để CSS thu nhỏ tooltip lại so với VMS.
    const isLcs = typeCode === TmsEquipmentType.LCS;
    const tooltipClass = isLcs ? 'vms-content-tooltip lcs-content-tooltip' : 'vms-content-tooltip';

    // Khi ảnh KHÔNG load được: ẩn luôn cả popup (không hiển thị tooltip trống/vỡ).
    // Ẩn img + phần tử .leaflet-popup bao ngoài để không còn khung popup thừa.
    const onError = "this.style.display='none'; var p=this.closest('.leaflet-popup'); if(p){p.style.display='none';}";

    return `<div class="${tooltipClass}"><img src="${contentImageUrl}" alt="VMS content" onerror="${onError}" /></div>`;
};

// #endregion

// #region Device Marker Icon

/**
 * Xác định URL icon thiết bị theo trạng thái hoạt động (active/fault/inactive).
 * Ở chế độ cấu hình (isEditMode) luôn trả về icon mặc định của loại thiết bị.
 */
const resolveDeviceIconUrlByStatus = (equipment: any, equipmentType: any, isEditMode: boolean): string | undefined => {
    if (isEditMode) {
        return equipmentType?.defaultIcon || equipmentType?.equipmentType?.defaultIcon;
    }

    const getConfigDataByCode = useBaseConfig().getConfigDataByCode;
    const listState = getConfigDataByCode(BaseConfigTypeEnum.TmsEquipmentState);
    const stateEquip = listState.find((s: any) => s.code == equipment?.equipmentState);
    const stateValue = stateEquip ? stateEquip.value : TmsEquipmentStateValueEnum.Inactive;

    switch (stateValue) {
        case TmsEquipmentStateValueEnum.Active:
            return equipmentType?.activeIcon;
        case TmsEquipmentStateValueEnum.Fault:
            return equipmentType?.faultIcon;
        default:
            return equipmentType?.inactiveIcon;
    }
};

/**
 * URL đầy đủ icon của 1 thiết bị — CÙNG logic với marker đặt lẻ (buildDeviceMarkerIcon):
 * - Màn cấu hình: icon mặc định của loại thiết bị.
 * - Runtime: icon theo trạng thái (active/fault/inactive), thiếu thì icon mặc định.
 * Ghép địa chỉ file server qua useItsMediaServerStore().fileUrl (reactive: server chưa nạp xong
 * thì trả '' và tự cập nhật khi nạp xong; đường dẫn tuyệt đối giữ nguyên).
 */
export const resolveDeviceIconUrl = (deviceId: string, isEditMode: boolean): string => {
    const mapStore = useMapStore();
    const equipment = mapStore.equipmentList.find((item: any) => String(item.id) === String(deviceId));
    if (!equipment) return '';
    const equipmentType = mapStore.equipmentTypeList.find((item: any) => String(item?.id) === String(equipment.equipmentTypeId));
    const path =
        resolveDeviceIconUrlByStatus(equipment, equipmentType, isEditMode) ||
        equipmentType?.defaultIcon ||
        mapStore.getItemDetails(String(equipment.id), 'device')?.iconUrl;
    return path ? useItsMediaServerStore().fileUrl(path) : '';
};

/**
 * Chọn kích thước icon thiết bị: cấu hình chung của map có ưu tiên cao nhất;
 * khi không đặt/đặt bằng 0 sẽ fallback về `size` của loại thiết bị.
 */
export const resolveEquipmentIconSize = (equipmentType: any, mapIconSize?: unknown): number => {
    const parsePositiveSize = (value: unknown) => {
        const size = Number.parseFloat(String(value ?? ''));
        return Number.isFinite(size) && size > 0 ? size : 0;
    };

    return parsePositiveSize(mapIconSize) || parsePositiveSize(equipmentType?.size) || 32;
};

/**
 * Chọn kích thước icon sự kiện/sự cố: cấu hình chung của map (incidentIconSize) có ưu tiên cao nhất;
 * khi không đặt/đặt bằng 0 sẽ fallback về `size` của loại sự kiện (eventType?.size), cuối cùng là 32px.
 */
export const resolveEventIconSize = (eventType: any, mapIncidentIconSize?: unknown): number => {
    const parsePositiveSize = (value: unknown) => {
        const size = Number.parseFloat(String(value ?? ''));
        return Number.isFinite(size) && size > 0 ? size : 0;
    };

    return parsePositiveSize(mapIncidentIconSize) || parsePositiveSize(eventType?.size) || 32;
};

/**
 * Render icon thiết bị tổng quát theo trạng thái thiết bị.
 * Ở chế độ edit thì luôn dùng icon mặc định.
 * @param options.isEditMode  đang ở màn hình cấu hình map
 * @param options.randomToken cache-buster cho URL ảnh
 * @param options.iconSize kích thước chung của map; để trống/0 sẽ dùng cấu hình loại thiết bị
 */
export const buildDeviceMarkerIcon = (
    feature: any,
    objectRender: RenderMapLike,
    options: { isEditMode: boolean; randomToken: number | string; iconSize?: number }
): L.DivIcon => {
    const mapStore = useMapStore();

    const itemDetails = objectRender.itemDetails;

    const selectedEquipmentId = feature.properties.id;
    const equipemnt = mapStore.equipmentList.find((equipment: any) => equipment.id == selectedEquipmentId);
    const equipemntType = mapStore.equipmentTypeList.find((equipmentType: any) => equipmentType?.id == equipemnt?.equipmentTypeId);
    const iconSize = resolveEquipmentIconSize(equipemntType, options.iconSize);

    const iconUrlBaseOnStatusEquipment = resolveDeviceIconUrlByStatus(equipemnt, equipemntType, options.isEditMode);

    // Ưu tiên icon theo trạng thái → icon mặc định của loại thiết bị → icon trong itemDetails.
    // Phải có bước icon mặc định: loại thiết bị chưa cấu hình icon cho trạng thái hiện tại thì
    // itemDetails.iconUrl (cũng chọn theo trạng thái) cũng rỗng → rơi về ảnh dự phòng toll-road.
    // Cùng thứ tự với resolveDeviceIconUrl (icon trong block) để 2 nơi luôn ra cùng một hình.
    const iconUrlImage = itemDetails?.iconUrl;
    const iconUrl = iconUrlBaseOnStatusEquipment || equipemntType?.defaultIcon || iconUrlImage;
    // Lật ngang icon theo từng vị trí đặt (feature.properties.flipX)
    const flipX = feature?.properties?.flipX === true;

    if (!iconUrl) {
        return createCustomDivIcon(Image, 'device-marker-icon', iconSize, undefined, flipX);
    }

    const fullIconUrl = useItsMediaServerStore().fileUrl(iconUrl, { bust: options.randomToken });
    return createCustomDivIcon(fullIconUrl, 'device-marker-icon', iconSize, undefined, flipX);
};

// #endregion

// #region Work Unit Marker Icon

/**
 * Render icon cho work unit (đơn vị).
 * @param randomToken cache-buster cho URL ảnh
 */
/**
 * Trích kích thước (px) từ chuỗi styleSetting của cấu hình (vd "size:40px", "width: 48", "40px").
 * Trả 0 nếu không parse được để caller tự fallback.
 */
const parseSizeFromStyleSetting = (style?: string | null): number => {
    if (!style) return 0;
    const text = String(style);
    const match =
        text.match(/(?:size|width|height)\s*:\s*(\d+(?:\.\d+)?)/i) ||
        text.match(/(\d+(?:\.\d+)?)\s*px/i) ||
        text.match(/^\s*(\d+(?:\.\d+)?)\s*$/);
    const value = match ? Number.parseFloat(match[1]) : NaN;
    return Number.isFinite(value) && value > 0 ? value : 0;
};

export const buildUnitMarkerIcon = (objectRender: RenderMapLike, randomToken: number | string, iconSize?: number): L.DivIcon => {
    const mapStore = useMapStore();
    const itemDetails = objectRender.itemDetails;

    // Cấu hình loại đơn vị (work_unit_type) trong SysConfig: chứa icon (value) + kiểu style (styleSetting).
    const unitTypeCode = String(itemDetails?.type ?? '');
    const unitTypeConfig = mapStore.workUnitTypeList.find((m: any) => m.code == unitTypeCode);

    // Kích thước icon đơn vị, ưu tiên:
    //  1) styleSetting của work_unit_type (vd "size:40px") -> chỉnh size trực tiếp trên cấu hình.
    //  2) size cứng theo loại (UNIT_TYPE_SIZE) -> fallback khi chưa đặt styleSetting.
    //  3) icon chung của map (khi > 0).
    //  4) UNIT_MARKER_SIZE mặc định.
    const size =
        parseSizeFromStyleSetting(unitTypeConfig?.styleSetting) ||
        UNIT_TYPE_SIZE[unitTypeCode] ||
        (Number.isFinite(Number(iconSize)) && Number(iconSize) > 0 ? Number(iconSize) : UNIT_MARKER_SIZE);

    // Icon theo loại work_unit_type (nếu có cấu hình trong SysConfig) sẽ ưu tiên hơn icon đã lưu sẵn
    if (unitTypeConfig?.value) {
        itemDetails.iconUrl = unitTypeConfig.value;
    }

    const iconUrlImage = itemDetails?.iconUrl;
    if (!iconUrlImage) {
        return createCustomDivIcon(Image, 'unit-marker-icon', size);
    }

    const fullIconUrl = `${mapStore.baseUrl}/${iconUrlImage}?&t=${randomToken}`;
    return createCustomDivIcon(fullIconUrl, 'unit-marker-icon', size);
};

// #endregion
