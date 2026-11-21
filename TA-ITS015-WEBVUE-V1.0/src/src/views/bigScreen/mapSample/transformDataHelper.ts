import { useMapStore } from "../../../stores/its/mapStore";
/**
 * Transforms equipment data into hierarchical structure
 */
export const transformEquipmentData = (equipmentData: any[], equipmentTypeList: any[],
    isIdInGeoJson: (id: string) => boolean
): any[] => {
    // Create groups based on equipment types

    const groupedData = equipmentTypeList.map((type: any) => ({
        id: `type_${type.id}`,
        name: type.name,
        isParent: true,
        children: [] as any[]
    }));
    console.log(groupedData)

    // Populate children for each group
    equipmentData.forEach((equipment: any) => {
        const parentGroup = groupedData.find((group: any) =>
            group.id === `type_${equipment.equipmentTypeId}`
        );

        if (parentGroup) {
            parentGroup.children.push({
                id: equipment.id,
                name: equipment.name,
                equipmentTypeId: equipment.equipmentTypeId,
                isParent: false,
                disabled: isIdInGeoJson(equipment.id)
            });
        }
    });
    return groupedData
        .filter((group: any) => group.children.length > 0)
        .map((group: any) => ({
            ...group,
            children: group.children.sort((a: any, b: any) => a.name.localeCompare(b.name))
        }))
        .sort((a: any, b: any) => a.name.localeCompare(b.name));
};

/**
 * Transforms zones data into hierarchical structure
 */
export const transformZonesData = async (zonesData: any[], isIdInGeoJson: (id: string) => boolean): any[] => {
    //console.log("transformZonesData - zone", zonesData)
    const mapStore = useMapStore();

    // Get list SysConfigData base on tms_zone_type
    const response = await mapStore.fetchSysConfigData('tms_zone_type');
    const groupedZones = zonesData.reduce((acc, zone) => {
        const type = zone.type || 'Other';
        if (!acc[type]) {
            acc[type] = [];
        }
        acc[type].push(zone);
        return acc;
    }, {});

    //console.log("transformZonesData - groupedZones", groupedZones)
    return Object.entries(groupedZones).map(([type, zones]) => {

        const objectReturn: any =
        {
            id: `type_${type}`,
            name: type,
            isParent: true,
            children: (zones as any[])
                .map(zone => ({
                    id: zone.id,
                    name: zone.name,
                    isParent: false,
                    disabled: isIdInGeoJson(zone.id)
                }))
                .sort((a, b) => a.name.localeCompare(b.name))
        }

        const sysConfigData_Zone = response.find((rs: any) => rs.code == type)
        if (sysConfigData_Zone) {
            objectReturn.name = sysConfigData_Zone?.name
        }
        console.log("transformZonesData - objectReturn", objectReturn)
        return objectReturn
    })        //.sort((a, b) => a.name.localeCompare(b.name));
        .sort((a, b) => a.children?.length - b.children?.length);

};

/**
 * Transforms work unit data into hierarchical structure
 */
export const transformWorkUnitData = async (workUnitData: any[], isIdInGeoJson: (id: string) => boolean): any[] => {
    const mapStore = useMapStore();

    // Get list SysConfigData base on tms_zone_type
    const response = await mapStore.fetchSysConfigData('work_unit_type');

    const groupedUnits = workUnitData.reduce((acc, unit) => {
        const type = unit.type || 'Other';
        if (!acc[type]) {
            acc[type] = [];
        }
        acc[type].push(unit);
        return acc;
    }, {});

    return Object.entries(groupedUnits).map(([type, units]) => {
        const objectReturn: any =
        {
            id: `type_${type}`,
            name: type,
            isParent: true,
            children: (units as any[])
                .map(unit => ({
                    id: unit.id,
                    name: unit.name,
                    isParent: false,
                    disabled: isIdInGeoJson(unit.id)
                }))
                .sort((a, b) => a.name.localeCompare(b.name))
        }
        const sysConfigData_WorkUnit = response.find((rs: any) => rs.code == type)
        if (sysConfigData_WorkUnit) {
            objectReturn.name = sysConfigData_WorkUnit?.name
        }
        console.log("transformZonesData - objectReturn", objectReturn)
        return objectReturn

        return objectReturn
    }).sort((a, b) => a.name.localeCompare(b.name));
};


/**
 * Tranform data in component MapControl
 */

