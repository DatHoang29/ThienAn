/**
 * fetch-utils — phục vụ SSE stream (text/event-stream).
 *
 * Module này dùng `fetch` + ReadableStream để vừa stream được, vừa giữ đúng các quy ước request của `axios-utils`:
 *
 *   - basePath lấy từ `window.__env__.VITE_API_URL` (giống `serveConfig`)
 *   - `Authorization: Bearer <access-token>` (Local → Session fallback)
 *   - `X-Authorization: Bearer <refresh-token>` khi access token đã hết hạn
 *   - `Accept-Language` theo `themeConfig.globalI18n`
 *   - Đọc lại token mới từ response header `access-token` / `x-access-token`
 *   - 401 ⇒ `clearAccessTokens()`; lỗi khác ⇒ `ElMessage.error` (tắt bằng `customCatch: true`)
 *
 * Ví dụ:
 * ```ts
 * import { fetchSSE, sseJson } from '/@/utils/fetch-utils';
 *
 * const conn = fetchSSE('/api/tms/tmsincident/stream', {
 *     params: { zoneId },
 *     idleTimeout: 60000,   // 60s không có byte nào ⇒ coi như đứt, kết nối lại
 *     reconnect: true,
 *     onMessage: (msg) => {
 *         if (msg.event === 'incident') state.list.unshift(sseJson<IncidentOutput>(msg));
 *     },
 * });
 *
 * onUnmounted(() => conn.close());
 * ```
 *

 */

import { fetchSharedSnapshot } from './shared-snapshot-sse';
import { ElMessage } from 'element-plus';
import { Local, Session } from './storage';
import { accessTokenKey, refreshAccessTokenKey, clearAccessTokens, decryptJWT, getJWTDate, sleep } from './axios-utils';

/** Các HTTP method hỗ trợ cho stream. */
export type FetchMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

/** Trần thời gian chờ giữa 2 lần kết nối lại (ms) — chặn backoff phình vô hạn. */
const MAX_RECONNECT_DELAY = 30000;

/**
 * Một sự kiện SSE đã parse xong theo spec `text/event-stream`.
 * - `event`: giá trị dòng `event:`, mặc định `'message'`.
 * - `data`: các dòng `data:` đã ghép bằng `'\n'`.
 * - `id`: `Last-Event-ID` hiện tại (spec: giữ nguyên qua các event tới khi server đổi).
 * - `retry`: khoảng chờ reconnect (ms) do server chỉ định, nếu có.
 */
export interface SseMessage {
	event: string;
	data: string;
	id?: string;
	retry?: number;
}

/** Hook nội bộ cho parser stream. */
export interface SseStreamHooks {
	/** Gọi mỗi khi nhận được 1 chunk byte (kể cả comment keep-alive) — dùng để reset idle timer. */
	onChunk?: () => void;
	/** Dòng bắt đầu bằng `:` — thường là keep-alive, không phải sự kiện. */
	onComment?: (comment: string) => void;
	/** Server gửi dòng `retry:`. */
	onRetry?: (delayMs: number) => void;
}

/** Tuỳ chọn của {@link fetchSSE}. */
export interface FetchSseOptions {
	/** Share snapshot GET streams across tabs with identical URL and authentication. */
	shareAcrossTabs?: boolean;
	/** Mặc định `'GET'`. Dùng `'POST'` khi cần truyền filter phức tạp qua body. */
	method?: FetchMethod;
	/** Query string; bỏ qua giá trị `undefined`/`null`/`''`, mảng ⇒ lặp key, Date ⇒ ISO. */
	params?: Record<string, any>;
	/** Body: object ⇒ JSON (tự set Content-Type), còn string/FormData/Blob giữ nguyên. Bỏ qua khi method là GET. */
	data?: any;
	/** Header thêm/ghi đè (đặt sau các header mặc định). */
	headers?: Record<string, string>;
	/** `true` ⇒ không gắn Authorization (endpoint public). */
	anonymous?: boolean;
	/** `true` ⇒ không tự hiện `ElMessage.error`, caller tự xử lý qua `onError`. */
	customCatch?: boolean;
	/** Không nhận byte nào trong khoảng này (ms) ⇒ coi như mất kết nối. `0` = tắt. */
	idleTimeout?: number;
	/** Tự kết nối lại khi đứt stream / lỗi mạng / idle timeout. Mặc định `false`. */
	reconnect?: boolean;
	/** Cho phép thử lại HTTP 5xx khi reconnect bật (mặc định tắt). */
	retryServerErrors?: boolean;
	/** Chờ bao lâu trước lần kết nối lại đầu tiên (ms); các lần sau tăng gấp đôi, tối đa 30s. Server gửi `retry:` sẽ ghi đè. */
	reconnectDelay?: number;
	/** Số lần kết nối lại tối đa; `-1` = không giới hạn (mặc định). */
	maxReconnect?: number;
	/** `Last-Event-ID` khởi tạo — cho phép nối tiếp từ event đã xử lý trước đó. */
	lastEventId?: string;
	/** Signal bên ngoài (vd của route/component) — abort sẽ đóng kết nối. */
	signal?: AbortSignal;
	/** Kết nối mở thành công (HTTP 2xx và có body stream). */
	onOpen?: (response: Response) => void;
	/** Nhận 1 sự kiện. Lỗi ném ra trong hàm này được log và bỏ qua, KHÔNG làm sập stream. */
	onMessage?: (message: SseMessage) => void;
	/** Dòng comment keep-alive. */
	onComment?: (comment: string) => void;
	/** Lỗi có thể kết nối lại; `attempt` là lần thử tiếp theo (1-based). */
	onError?: (error: any, attempt: number) => void;
	/** Kết nối đã dừng hẳn (đóng chủ động, lỗi fatal, hoặc hết số lần retry). */
	onClose?: () => void;
}

/** Handle điều khiển kết nối SSE. */
export interface SseConnection {
	/** Đóng kết nối và không kết nối lại nữa. An toàn khi gọi nhiều lần. */
	close(): void;
	/** `true` khi đã yêu cầu đóng. */
	readonly closed: boolean;
	/** Id sự kiện cuối cùng nhận được. */
	readonly lastEventId: string;
	/** Resolve khi stream dừng bình thường; reject khi dừng vì lỗi. */
	readonly done: Promise<void>;
}

/** Tuỳ chọn SSE có response wrapper giống API. Lỗi HTTP vẫn theo fetchSSE. */
export interface FetchSseApiOptions<T> extends Omit<FetchSseOptions, 'onMessage'> {
	/** Chỉ xử lý event này; bỏ qua heartbeat và event khác. */
	event?: string;
	onResult: (result: T, message: SseMessage) => void | Promise<void>;
	/** Lỗi JSON, wrapper hoặc callback; không đóng stream. */
	onResultError?: (error: Error, message: SseMessage) => void;
	/** Chạy sau mỗi event được xử lý, cả thành công và lỗi (vd tắt loading). */
	onSettled?: () => void;
}

/**
 * SSE cho API chuẩn { code, succeeded, result, message, errors }.
 * onResult nhận trực tiếp result; customCatch tắt toast mặc định như axios.
 * Callback async được xử lý tuần tự; lỗi một event không chặn event tiếp theo.
 *
 * fetchSseApi<Snapshot>(url, {
 *     event: 'snapshot',
 *     onResult: result => { rows.value = result.data; },
 *     onSettled: () => { loading.value = false; },
 * });
 */
export function fetchSseApi<T>(url: string, options: FetchSseApiOptions<T>): SseConnection {
	const { event, onResult, onResultError, onSettled, ...streamOptions } = options;
	let errorReported = false;
	let pending = Promise.resolve();
	const connection = fetchSSE(url, {
		...streamOptions,
		onMessage: (message) => {
			// Control failures have their own event names; filtering them hides backend/auth failures.
			if (event && message.event !== event && !['unavailable', 'unauthorized'].includes(message.event)) return;
			pending = pending.then(async () => {
				if (connection.closed || options.signal?.aborted) return;
				try {
					const response = JSON.parse(message.data);
					if (!response || response.code !== 200 || response.succeeded !== true || response.errors) {
						if (response?.code === 401) clearAccessTokens();
						const detail = response?.errors || response?.message || 'Request Error.';
						throw new Error((typeof detail === 'string' ? detail : JSON.stringify(detail)).replace(/[\u4e00-\u9fff]/g, ''));
					}
					if (!Object.prototype.hasOwnProperty.call(response, 'result')) throw new Error('SSE response is missing result.');
					await onResult(response.result as T, message);
					errorReported = false;
				} catch (cause) {
					const error = cause instanceof Error ? cause : new Error(String(cause));
					if (!errorReported) {
						errorReported = true;
						if (!options.customCatch) ElMessage.error(error.message);
						console.warn('[SSE result]', message.event, error.message);
					}
					onResultError?.(error, message);
				} finally {
					onSettled?.();
				}
			}).catch((error) => console.error('[fetchSseApi] Callback failed:', error));
		},
	});
	return connection;
}

/** Bỏ dấu `/` ở cuối. */
const trimTrailingSlash = (value: string): string => value.replace(/\/+$/, '');

/** Base URL của API server — cùng nguồn cấu hình với `serveConfig` của axios-utils. */
export const getFetchBaseUrl = (): string => trimTrailingSlash(String(window.__env__?.VITE_API_URL ?? ''));

/** Kiểm tra header đã tồn tại (không phân biệt hoa thường). */
const hasHeader = (headers: Record<string, string>, name: string): boolean => Object.keys(headers).some((key) => key.toLowerCase() === name.toLowerCase());

/** Chuẩn hoá 1 giá trị query: Date ⇒ ISO, còn lại ⇒ String. */
const toQueryValue = (value: any): string => (value instanceof Date ? value.toISOString() : String(value));

/**
 * Dựng query string từ object. Bỏ qua `undefined`/`null`/`''`; mảng ⇒ lặp lại key
 * (khớp cách client auto-generated của Swagger serialize mảng).
 */
export const buildQueryString = (params?: Record<string, any>): string => {
	if (!params) return '';
	const search = new URLSearchParams();
	Object.entries(params).forEach(([key, value]) => {
		if (value === undefined || value === null || value === '') return;
		if (Array.isArray(value)) {
			value.filter((item) => item !== undefined && item !== null && item !== '').forEach((item) => search.append(key, toQueryValue(item)));
			return;
		}
		search.append(key, toQueryValue(value));
	});
	const query = search.toString();
	return query ? `?${query}` : '';
};

/** Ghép URL tuyệt đối từ path tương đối + query. URL đã tuyệt đối thì giữ nguyên host. */
export const resolveFetchUrl = (url: string, params?: Record<string, any>): string => {
	const isAbsolute = /^(https?:)?\/\//i.test(url);
	const base = isAbsolute ? '' : getFetchBaseUrl();
	const path = isAbsolute ? url : `/${url.replace(/^\/+/, '')}`;
	return `${base}${path}${buildQueryString(params)}`;
};

/**
 * Dựng header giống axios request interceptor: Authorization, refresh token khi hết hạn,
 * Accept-Language. `extra` đặt cuối nên ghi đè được mặc định.
 */
export const buildFetchHeaders = (extra?: Record<string, string>, anonymous = false): Record<string, string> => {
	const headers: Record<string, string> = {};

	if (!anonymous) {
		const accessToken = Local.get(accessTokenKey) ? Local.get(accessTokenKey) : Session.get('token');
		if (accessToken) {
			headers['Authorization'] = `Bearer ${accessToken}`;
			try {
				// Token hết hạn ⇒ gửi kèm refresh token để backend cấp lại cặp token mới.
				const jwt: any = decryptJWT(accessToken);
				if (new Date() >= getJWTDate(jwt.exp as number)) {
					const refreshAccessToken = Local.get(refreshAccessTokenKey);
					if (refreshAccessToken) headers['X-Authorization'] = `Bearer ${refreshAccessToken}`;
				}
			} catch {
				/* Token không giải mã được ⇒ bỏ qua, để backend tự từ chối. */
			}
		}
	}

	const globalI18n = Local.get('themeConfig')?.globalI18n;
	if (globalI18n) headers['Accept-Language'] = globalI18n;

	return { ...headers, ...(extra ?? {}) };
};

/** Chuẩn bị body cho fetch; object thường ⇒ JSON (fetch không tự set Content-Type như axios). */
const buildRequestBody = (method: FetchMethod, data: any, headers: Record<string, string>): BodyInit | undefined => {
	if (method === 'GET' || data === undefined || data === null) return undefined;
	if (typeof data === 'string' || data instanceof FormData || data instanceof Blob || data instanceof URLSearchParams || data instanceof ArrayBuffer) return data as BodyInit;
	if (!hasHeader(headers, 'content-type')) headers['Content-Type'] = 'application/json';
	return JSON.stringify(data);
};

/**
 * Đọc cặp token mới trả về trong response header — cùng logic axios response interceptor.
 * Cross-origin cần backend expose 2 header này, nếu không `headers.get` luôn trả null.
 */
const syncTokensFromResponse = (response: Response): void => {
	const accessToken = response.headers.get(accessTokenKey);
	const refreshAccessToken = response.headers.get(refreshAccessTokenKey);

	if (accessToken === 'invalid_token') {
		clearAccessTokens();
		return;
	}
	if (accessToken && refreshAccessToken) {
		Local.set(accessTokenKey, accessToken);
		Local.set(refreshAccessTokenKey, refreshAccessToken);
	}
};

/** Bóc thông điệp lỗi từ response không thành công (JSON chuẩn hoá hoặc raw text). */
const extractErrorMessage = async (response: Response): Promise<string> => {
	let text = '';
	try {
		text = await response.text();
	} catch {
		/* Body đã bị huỷ hoặc không đọc được ⇒ dùng status. */
	}
	if (!text) return `${response.status} ${response.statusText || 'Request Error.'}`;

	let message: any = text;
	try {
		const body = JSON.parse(text);
		message = body?.message ?? body?.errors ?? body?.title ?? body?.error ?? text;
	} catch {
		/* Không phải JSON ⇒ giữ raw text. */
	}
	const result = typeof message === 'string' ? message : JSON.stringify(message);
	// Bỏ ký tự Trung Quốc còn sót từ framework backend — giống cách axios interceptor đang làm.
	return result.replace(/[\u4e00-\u9fff]/g, '');
};

/** Lỗi không nên kết nối lại (vd 401, 4xx) — đánh dấu để vòng lặp dừng hẳn. */
const fatalError = (message: string): Error => Object.assign(new Error(message), { sseFatal: true });

/** Phân biệt abort (do close/idle timeout) với lỗi thật. */
const isAbortError = (error: any): boolean => error?.name === 'AbortError' || error?.name === 'TimeoutError' || error?.code === 20;

/**
 * Parser `text/event-stream`: biến ReadableStream thành luồng {@link SseMessage}.
 *
 * Dùng được độc lập với {@link fetchSSE} khi caller tự gọi fetch:
 * ```ts
 * for await (const msg of readSseStream(response.body!)) { ... }
 * ```
 *
 * Chi tiết theo spec: `data:` ghép nhiều dòng bằng `'\n'`; `id:` giữ qua các event kế tiếp;
 * dòng bắt đầu bằng `:` là comment; field lạ bị bỏ qua. Chỉ hỗ trợ ngắt dòng `'\n'`/`'\r\n'`
 * (không hỗ trợ `'\r'` đứng một mình — không server nào dùng, và nếu chấp nhận sẽ cắt sai
 * khi chunk kết thúc đúng giữa cặp `'\r\n'`).
 */
export async function* readSseStream(body: ReadableStream<Uint8Array>, hooks?: SseStreamHooks): AsyncGenerator<SseMessage, void, void> {
	const reader = body.getReader();
	const decoder = new TextDecoder('utf-8');

	let buffer = '';
	let dataLines: string[] = [];
	let eventName = '';
	let eventId: string | undefined;
	let retryDelay: number | undefined;

	/** Dòng trống ⇒ chốt và phát sự kiện đang gom. */
	const takeMessage = (): SseMessage | null => {
		const hasPayload = dataLines.length > 0 || eventName !== '';
		const message: SseMessage | null = hasPayload ? { event: eventName || 'message', data: dataLines.join('\n'), id: eventId, retry: retryDelay } : null;
		dataLines = [];
		eventName = '';
		return message;
	};

	const parseLine = (line: string): SseMessage | null => {
		if (line === '') return takeMessage();
		if (line.startsWith(':')) {
			hooks?.onComment?.(line.slice(1).trim());
			return null;
		}

		const colon = line.indexOf(':');
		const field = colon === -1 ? line : line.slice(0, colon);
		let value = colon === -1 ? '' : line.slice(colon + 1);
		if (value.startsWith(' ')) value = value.slice(1);

		switch (field) {
			case 'data':
				dataLines.push(value);
				break;
			case 'event':
				eventName = value;
				break;
			case 'id':
				if (!value.includes('\u0000')) eventId = value;
				break;
			case 'retry':
				if (/^\d+$/.test(value)) {
					retryDelay = Number(value);
					hooks?.onRetry?.(retryDelay);
				}
				break;
			default:
				break;
		}
		return null;
	};

	const stripCr = (line: string): string => (line.endsWith('\r') ? line.slice(0, -1) : line);

	try {
		for (; ;) {
			const { done, value } = await reader.read();
			if (done) break;

			hooks?.onChunk?.();
			buffer += decoder.decode(value, { stream: true });

			let index: number;
			while ((index = buffer.indexOf('\n')) >= 0) {
				const line = buffer.slice(0, index);
				buffer = buffer.slice(index + 1);
				const message = parseLine(stripCr(line));
				if (message) yield message;
			}
		}

		// Server đóng stream mà không gửi dòng trống cuối ⇒ flush phần còn lại.
		buffer += decoder.decode();
		if (buffer) {
			for (const line of buffer.split('\n')) {
				const message = parseLine(stripCr(line));
				if (message) yield message;
			}
		}
		const tail = takeMessage();
		if (tail) yield tail;
	} finally {
		// cancel() vừa nhả lock vừa giải phóng socket; stream đã lỗi/abort thì bỏ qua.
		try {
			await reader.cancel();
		} catch {
			/* Không cần xử lý. */
		}
	}
}

/** Parse `data` của sự kiện sang JSON; trả `undefined` nếu không phải JSON hợp lệ. */
export const sseJson = <T = any>(message: SseMessage): T | undefined => {
	if (!message?.data) return undefined;
	try {
		return JSON.parse(message.data) as T;
	} catch {
		return undefined;
	}
};

/**
 * Mở kết nối SSE bằng fetch — "axios cho stream".
 *
 * Trả về ngay (không await) một {@link SseConnection}; nhớ gọi `close()` trong `onUnmounted`.
 * Quy ước reconnect (giống `EventSource`): lỗi mạng / stream đứt / idle timeout thì thử lại,
 * còn HTTP trả về mã lỗi (401, 4xx, 5xx) thì dừng hẳn để tránh bão request và toast lặp.
 */
// Bounded, tab-local diagnostics. Never record tokens, query parameters or payloads.
const sseDiagnosticTab = Math.random().toString(36).slice(2, 10);
let sseDiagnosticSequence = 0;
const sseDiagnosticRows: Record<string, unknown>[] = [];
export function logSseDiagnostic(event: string, details: Record<string, unknown> = {}): void {
 const row = { at: new Date().toISOString(), tab: sseDiagnosticTab, event, ...details };
 sseDiagnosticRows.push(row);
 if (sseDiagnosticRows.length > 200) sseDiagnosticRows.shift();
 if (typeof window !== 'undefined') (window as any).__vdsSseDiagnostics = sseDiagnosticRows;
 console.info('[SSE diagnostic]', JSON.stringify(row));
}

export function fetchSSE(url: string, options: FetchSseOptions = {}): SseConnection {
	const { method = 'GET', idleTimeout = 0, reconnect = false, reconnectDelay = 3000, maxReconnect = -1 } = options;

 const diagnosticId = ++sseDiagnosticSequence;
 const path = url.split('?')[0].split('#')[0];
 const log = (event: string, details: Record<string, unknown> = {}) => logSseDiagnostic(event, { id: diagnosticId, path, ...details });
 log('created');
	let closedByUser = false;
	let finished = false;
	let lastEventId = options.lastEventId ?? '';
	let serverRetryDelay: number | undefined;
	let currentAbort: AbortController | null = null;

	let resolveDone!: () => void;
	let rejectDone!: (reason?: any) => void;
	const done = new Promise<void>((resolve, reject) => {
		resolveDone = resolve;
		rejectDone = reject;
	});
	// Caller thường không await `done` ⇒ gắn handler rỗng để không thành unhandled rejection.
	done.catch(() => { });

	const finish = (error?: any): void => {
		if (finished) return;
		finished = true;
		log('finished', { reason: closedByUser ? 'consumer-closed' : error ? 'error' : 'end' });
		options.signal?.removeEventListener('abort', close);
		options.onClose?.();
		if (error) rejectDone(error);
		else resolveDone();
	};

	const close = (): void => {
		if (closedByUser) return;
		closedByUser = true;
		log('close-requested');
		currentAbort?.abort(options.signal?.reason);
	};

	if (options.signal) {
		if (options.signal.aborted) closedByUser = true;
		else options.signal.addEventListener('abort', close, { once: true });
	}

	const run = async (): Promise<void> => {
		let attempt = 0;

		while (!closedByUser) {
			const abort = new AbortController();
			currentAbort = abort;

   const startedAt = Date.now();
   let phase = 'waiting-headers', messages = 0, lastByteAt = 0;
   let diagnosticTimer: ReturnType<typeof setTimeout> | undefined;
   const report = () => {
    log('progress', { phase, elapsedMs: Date.now() - startedAt, messages, silentMs: lastByteAt ? Date.now() - lastByteAt : null });
    diagnosticTimer = setTimeout(report, 15000);
   };
   diagnosticTimer = setTimeout(report, 15000);
   log('fetch-start', { attempt: attempt + 1 });
			let idleTimer: any = null;
			let idleHit = false;
			const armIdle = (): void => {
				if (!idleTimeout) return;
				if (idleTimer) clearTimeout(idleTimer);
				idleTimer = setTimeout(() => {
					idleHit = true;
					log('idle-timeout', { phase, elapsedMs: Date.now() - startedAt, messages });
					abort.abort('idle-timeout');
				}, idleTimeout);
			};

			let retryError: any = null;

			try {
				const headers = buildFetchHeaders(
					{
						Accept: 'text/event-stream',
						'Cache-Control': 'no-cache',
						...(lastEventId ? { 'Last-Event-ID': lastEventId } : {}),
						...(options.headers ?? {}),
					},
					options.anonymous
				);
				const body = buildRequestBody(method, options.data, headers);

				// Tính cả pha kết nối vào idle timeout: server treo lúc handshake cũng phải bị cắt.
				armIdle();
				const requestUrl = resolveFetchUrl(url, options.params);
				if (!options.shareAcrossTabs || method !== 'GET') {
					const connectionId = 'sse-' + Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('');
					headers['X-SSE-Diagnostic-Id'] = connectionId;
					log('direct-connection', { connectionId });
				}
				const response = options.shareAcrossTabs && method === 'GET'
					? await fetchSharedSnapshot(requestUrl, headers, abort.signal, log)
					: await fetch(requestUrl, { method, headers, body, signal: abort.signal, cache: 'no-store' });

				phase = 'streaming';
				log('headers', { status: response.status, elapsedMs: Date.now() - startedAt });
				syncTokensFromResponse(response);

				if (response.status === 401) {
					// clearAccessTokens() xoá token và reload trang — cùng hành vi với axios interceptor.
					clearAccessTokens();
					throw fatalError('Unauthorized');
				}
				if (!response.ok) {
					const message = await extractErrorMessage(response);
					if (options.retryServerErrors && response.status >= 500) throw new Error(message);
					if (!options.customCatch) ElMessage.error(message);
					throw fatalError(message);
				}
				if (!response.body) throw fatalError('SSE: response không có body stream.');

				attempt = 0; // Mở được kết nối ⇒ reset bộ đếm để lần đứt sau vẫn còn đủ lượt retry.
				options.onOpen?.(response);

				const hooks: SseStreamHooks = {
					onChunk: () => { lastByteAt = Date.now(); armIdle(); },
					onComment: options.onComment,
					onRetry: (delayMs) => (serverRetryDelay = delayMs),
				};

				for await (const message of readSseStream(response.body, hooks)) {
					messages++;
					if (messages === 1 || message.event === 'unavailable') log('event', { name: message.event, elapsedMs: Date.now() - startedAt });
					if (message.id) lastEventId = message.id;
					try {
						options.onMessage?.(message);
					} catch (handlerError) {
						// Lỗi trong handler của caller không được làm sập stream.
						console.error('[fetchSSE] onMessage handler error:', handlerError);
					}
				}

				// Stream kết thúc bình thường (server đóng).
				if (closedByUser || !reconnect) {
					finish();
					return;
				}
			} catch (error: any) {
				log('attempt-ended', { phase, aborted: closedByUser, idleHit, errorName: error?.name });
				if (closedByUser && isAbortError(error)) {
					finish();
					return;
				}

				retryError = idleHit ? new Error(`SSE: không nhận được dữ liệu trong ${idleTimeout}ms.`) : error;
				if (retryError?.sseFatal || !reconnect) {
					finish(retryError);
					return;
				}
				options.onError?.(retryError, attempt + 1);
			} finally {
				if (diagnosticTimer) clearTimeout(diagnosticTimer);
				if (idleTimer) clearTimeout(idleTimer);
				currentAbort = null;
			}

			if (closedByUser) {
				finish();
				return;
			}

			attempt += 1;
			if (maxReconnect >= 0 && attempt > maxReconnect) {
				finish(retryError ?? undefined);
				return;
			}

			// Server chỉ định `retry:` thì tôn trọng đúng con số đó, ngược lại backoff gấp đôi.
			const delay = serverRetryDelay ?? Math.min(reconnectDelay * 2 ** (attempt - 1), MAX_RECONNECT_DELAY);
			await sleep(delay);
		}

		finish();
	};

	run().catch((error) => finish(error));

	return {
		close,
		get closed() {
			return closedByUser;
		},
		get lastEventId() {
			return lastEventId;
		},
		done,
	};
}
