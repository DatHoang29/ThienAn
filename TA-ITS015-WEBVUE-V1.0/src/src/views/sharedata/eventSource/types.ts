export interface EventSourceInput {
	code: string;
	name: string;
	subject: string;
	datatypeCode: string;
	description: string;
}

export interface EventSource extends EventSourceInput {
	id: string;
}

// Existing event-source datatype codes and their translations; this screen has no write API yet.
export const DATATYPES = Array.from({ length: 11 }, (_, index) => ({ id: 101 + index }));

export function datatypeLabel(id: string | number, translate: (key: string) => string): string {
	const type = DATATYPES.find(item => String(item.id) === String(id));
	return type ? translate(`lz.label.sharedataEventSource.datatype${type.id}`) : String(id);
}

export function notImplemented(): never {
	throw new Error('lz.message.sharedataEventSource.pendingBackend');
}
