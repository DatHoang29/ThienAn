/** Resolve the displayed S/M/L category independently of configurable catalogue codes. */
export function vehicleCountField(vehicle: { code?: unknown; value?: unknown }): 'short' | 'medium' | 'long' | 'other' | undefined {
    const image = String(vehicle.value ?? '').trim().toLowerCase();
    const images = { 's.png': 'short', 'm.png': 'medium', 'l.png': 'long' } as const;
    if (image in images) return images[image as keyof typeof images];
    const code = String(vehicle.code ?? '').trim().toLowerCase();
    const codes: Record<string, 'short' | 'medium' | 'long' | 'other'> = {
        s: 'short', short: 'short', '1': 'short',
        m: 'medium', medium: 'medium', '3': 'medium',
        l: 'long', long: 'long', '4': 'long',
        other: 'other', '0': 'other',
    };
    return codes[code];
}
