import Dexie, { type EntityTable } from 'dexie';

// SonTH: Define terminology interface
interface ITerminologyiDb {
    id: string;
    value: string;
    lang: string;
}

// SonTH: Define indexedDB
const db = new Dexie('TA-ConfigDb') as Dexie & {
    terminology: EntityTable<
        ITerminologyiDb,
        'id' // primary key "id" (for the typings only)
    >;
};

// SonTH: Define schema
db.version(1).stores({
    terminology: '++id, value, lang' // primary key "id" (for the runtime!)
});

export type { ITerminologyiDb };
export { db as terminologyiDb };