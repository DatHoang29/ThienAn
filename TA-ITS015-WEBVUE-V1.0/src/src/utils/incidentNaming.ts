// Mirrors Modules.TMS.Core.Constants.IncidentNaming and the existing manual form format.
export function incidentName(heading: string[], km: number, met: number, dateText: string): string {
    const suffix = `Km${km}+${met} ${dateText}`;
    const title = heading.filter(Boolean).map(value => value.trim()).join(' ');
    const available = Math.max(0, 64 - suffix.length - 1);
    return [title.slice(0, available).trimEnd(), suffix].filter(Boolean).join(' ');
}
