/**
 * Description: Factory dựng Pinia store cho media/file server — phần lõi dùng chung, KHÔNG phụ thuộc nghiệp vụ.
 *   - Nạp địa chỉ server 1 lần, dùng chung (reactive) cho mọi màn hình.
 *   - Ghép đường dẫn file (tương đối/tuyệt đối), cache-buster, ảnh dự phòng khi lỗi.
 * Chỉ phụ thuộc vue + pinia. Cách lấy địa chỉ server truyền từ ngoài vào (loadBaseUrl),
 * nên mang sang dự án khác chỉ cần copy file này và viết store nghiệp vụ riêng.
 *
 * Ví dụ:
 *   export const useXxxMediaServerStore = createMediaServerStore('xxxMediaServer', {
 *       loadBaseUrl: async () => (await api.getFileServer()).data.result,
 *       fallbackImage: noImage,
 *   }, (core) => ({
 *       productImage: (p?: { image?: string | null }) => core.fileUrl(p?.image),
 *   }));
 * Created date: 28/10/2026
 */
import { defineStore } from 'pinia';
import { ref } from 'vue';

export interface MediaServerOptions {
	/** Hàm lấy địa chỉ media/file server (gọi API cấu hình của dự án). */
	loadBaseUrl: () => Promise<string | null | undefined>;
	/** Ảnh dự phòng chung khi không có đường dẫn hoặc ảnh lỗi. */
	fallbackImage?: string;
}

export interface FileUrlOptions {
	/** Ảnh dự phòng riêng cho lần gọi này ('' = không dùng ảnh dự phòng). */
	fallback?: string;
	/** Thêm tham số chống cache: true = token của phiên, hoặc truyền token riêng. */
	bust?: boolean | string | number;
}

/** Đường dẫn đã đầy đủ, không ghép thêm địa chỉ server. */
const ABSOLUTE_URL = /^(https?:|data:|blob:|\/\/)/i;

/** Phần lõi (setup) — dùng được trực tiếp trong defineStore(..., () => {...}) của dự án. */
export function createMediaServerCore(options: MediaServerOptions) {
	const baseUrl = ref('');
	const isLoaded = ref(false);
	/** Token chống cache mặc định của phiên, đổi khi gọi refreshCacheToken(). */
	const cacheToken = ref(Date.now().toString(36));
	let pending: Promise<string> | null = null;

	const normalize = (url?: string | null) => String(url ?? '').trim().replace(/\/+$/, '');

	/** Nạp địa chỉ server (chỉ gọi API 1 lần, các lần sau trả ngay; gọi đồng thời dùng chung 1 request). */
	function ensureLoaded(force = false): Promise<string> {
		if (isLoaded.value && !force) return Promise.resolve(baseUrl.value);
		if (pending) return pending;
		pending = options
			.loadBaseUrl()
			.then((url) => {
				baseUrl.value = normalize(url);
				isLoaded.value = true;
				return baseUrl.value;
			})
			.catch((error) => {
				console.warn('[mediaServer] Không lấy được địa chỉ file server', error);
				return baseUrl.value;
			})
			.finally(() => {
				pending = null;
			});
		return pending;
	}

	/** Gán địa chỉ server có sẵn (vd: lấy từ nguồn khác), bỏ qua gọi API. */
	function setBaseUrl(url?: string | null) {
		baseUrl.value = normalize(url);
		isLoaded.value = true;
	}

	/** Đổi token chống cache (vd: sau khi upload ảnh mới cùng tên). */
	function refreshCacheToken() {
		cacheToken.value = Date.now().toString(36);
	}

	/**
	 * Đường dẫn đầy đủ của file trên server. Chưa có địa chỉ server thì trả ảnh dự phòng
	 * và tự nạp; nạp xong binding reactive tự cập nhật sang ảnh thật.
	 */
	function fileUrl(path?: string | null, opts: FileUrlOptions = {}): string {
		const fallback = opts.fallback ?? options.fallbackImage ?? '';
		const value = String(path ?? '').trim();
		if (!value) return fallback;
		if (ABSOLUTE_URL.test(value)) return value;
		if (!isLoaded.value) {
			void ensureLoaded();
			return fallback;
		}
		const url = `${baseUrl.value}/${value.replace(/^\/+/, '')}`;
		if (!opts.bust) return url;
		const token = opts.bust === true ? cacheToken.value : opts.bust;
		return `${url}${url.includes('?') ? '&' : '?'}t=${token}`;
	}

	/** Gắn vào @error của <img>: ảnh lỗi ⇒ đổi sang ảnh dự phòng, chỉ 1 lần để tránh lặp vô hạn. */
	function onImgError(e: Event, fallback?: string) {
		const img = e.target as HTMLImageElement | null;
		const src = fallback ?? options.fallbackImage;
		if (!img || !src || img.dataset.mediaFallback) return;
		img.dataset.mediaFallback = '1';
		img.src = src;
	}

	return { baseUrl, isLoaded, cacheToken, ensureLoaded, setBaseUrl, refreshCacheToken, fileUrl, onImgError };
}

export type MediaServerCore = ReturnType<typeof createMediaServerCore>;

/** Dựng Pinia store media server; `extend` thêm các hàm nghiệp vụ (ảnh sự kiện, ảnh thiết bị, ...). */
export function createMediaServerStore<E extends Record<string, unknown> = Record<string, never>>(id: string, options: MediaServerOptions, extend?: (core: MediaServerCore) => E) {
	return defineStore(id, () => {
		const core = createMediaServerCore(options);
		return { ...core, ...((extend?.(core) ?? {}) as E) };
	});
}
