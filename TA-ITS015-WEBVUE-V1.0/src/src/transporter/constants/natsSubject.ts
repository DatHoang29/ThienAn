// ─── Interface khớp với 1 phần tử trong nats.subjects.json ───────────────────
// Không còn trường "event" — event do transporterEvent.ts quản lý
export interface NatsSubjectConfigItem {
    subject: string;
    mode: 'pubsub' | 'jetstream';
    allowSubscribeOnFe: boolean;
}

export interface NatsSubjectsJson {
    subjects: NatsSubjectConfigItem[];
}

// Map: subject string → config item
// VD: { 'ta.its.status.fms': { mode: 'pubsub', allowSubscribeOnFe: true } }
export type NatsSubjectConfigMap = Record<string, NatsSubjectConfigItem>;

// ─── Loader ───────────────────────────────────────────────────────────────────

let _cachedMap: NatsSubjectConfigMap | null = null;

export async function loadNatsSubjectConfig(): Promise<NatsSubjectConfigMap> {
    if (_cachedMap) return _cachedMap;

    const res = await fetch('Configuration/Nats.subjects.json');
    if (!res.ok) {
        throw new Error(
            `[NATS] Không thể load nats.subjects.json: ${res.status} ${res.statusText}`
        );
    }

    const json: NatsSubjectsJson = await res.json();

    _cachedMap = Object.fromEntries(
        json.subjects.map(item => [item.subject, item])
    );

    console.log('[NATS] Subject config loaded:', Object.keys(_cachedMap));
    return _cachedMap;
}

export function getNatsSubjectConfigMap(): NatsSubjectConfigMap | null {
    return _cachedMap;
}
