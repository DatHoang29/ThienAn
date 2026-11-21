/** Preserve QuestDB precision. Zone-less VDS timestamps represent UTC, not browser local time. */
export function vdsTimestamp(value: string | Date): string {
    if (value instanceof Date) return value.toISOString();
    const timestamp = value.trim();
    return /(?:Z|[+-]\d{2}:?\d{2})$/i.test(timestamp) ? timestamp : `${timestamp}Z`;
}
