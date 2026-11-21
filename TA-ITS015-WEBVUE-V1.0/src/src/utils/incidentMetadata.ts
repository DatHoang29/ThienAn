export const incidentMetadataFields = [
    ['VehiclePlate', 'lz.entity.tms.licensePlate', ''], ['VehicleSpeed', 'lz.entity.tms.speed', ' km/h'],
    ['AverageSpeed', 'lz.entity.tms.averageSpeed', ' km/h'], ['VehicleLenght', 'lz.entity.tms.vehicleLength', ' m'],
    ['VehicleType', 'lz.entity.tms.vehicleType', ''], ['VehicleColor', 'lz.entity.tms.vehicleColor', ''],
    ['VehicleSize', 'lz.entity.tms.vehicleSize', ''], ['Lane', 'lz.entity.tms.lane', ''],
    ['FromCamera', 'lz.entity.tms.fromCamera', ''], ['ToCamera', 'lz.entity.tms.toCamera', ''],
    ['DistanceMeter', 'lz.entity.tms.distance', ' m'], ['TravelSeconds', 'lz.entity.tms.travelTime', ' s'],
    ['SpeedLimit', 'lz.entity.tms.speedLimit', ' km/h'],
] as const;

export function incidentMetadataRows(metadata?: string | null) {
    let data: Record<string, unknown>;
    try {
        const parsed = JSON.parse(metadata || '{}');
        if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return [];
        data = parsed;
    } catch { return []; }
    return incidentMetadataFields.flatMap(([key, label, unit]) => {
        const value = data[key];
        if ((typeof value !== 'string' && typeof value !== 'number') || value === '' ||
            (typeof value === 'number' && !Number.isFinite(value))) return [];
        return [{ key, label, value: `${value}${unit}` }];
    });
}
