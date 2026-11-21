/**
 * Current version: v1.4.0
 * Usage description: https://editor.swagger.io Code generation typescript-axios auxiliary tool library
 * Dependency description: Adapted for axios version: v0.21.4
 * Video tutorial: https://www.bilibili.com/video/BV1EW4y1C71D
 */

import globalAxios, { AxiosInstance } from 'axios';
import { Configuration } from '../api-services/';
import { BaseAPI, BASE_PATH } from '../api-services/base';

import { ElMessage } from 'element-plus';
import { httpErrorMessage } from './httpErrorMessage';
import { Local, Session } from '../utils/storage';

// API server configuration
export const serveConfig = new Configuration({
	basePath: window.__env__.VITE_API_URL,
});

// Token key definition
export const accessTokenKey = 'access-token';
export const refreshAccessTokenKey = `x-${accessTokenKey}`;

// Get token
export const getToken = () => {
	return Local.get(accessTokenKey);
};

// Get request header token
export const getHeader = () => {
	return { authorization: 'Bearer ' + getToken() };
};

// Clear token
export const clearAccessTokens = () => {
	clearTokens();

	// Refresh the browser
	window.location.reload();
};

// Clear token
export const clearTokens = () => {
	Local.remove(accessTokenKey);
	Local.remove(refreshAccessTokenKey);
	Session.clear();
};

// Default axios instance
export const axiosInstance: AxiosInstance = globalAxios;

// Here you can configure more axios options =========================================
axiosInstance.defaults.timeout = 1000 * 60 * 10; // Set timeout, default 10 minutes

// Axios request interceptor
axiosInstance.interceptors.request.use(
	(conf) => {
		// Get local token or session token
		const accessToken = Local.get(accessTokenKey) ? Local.get(accessTokenKey) : Session.get('token');
		if (accessToken) {

			// Add token to request header
			conf.headers!['Authorization'] = `Bearer ${accessToken}`;

			// Check if accessToken is expired
			const jwt: any = decryptJWT(accessToken);
			const exp = getJWTDate(jwt.exp as number);

			// Token has expired
			if (new Date() >= exp) {
				// Get refresh token
				const refreshAccessToken = Local.get(refreshAccessTokenKey);

				// Carry refresh token
				if (refreshAccessToken) {
					conf.headers!['X-Authorization'] = `Bearer ${refreshAccessToken}`;
				}
			}
		}

		// Here you can write request interception code =========================================

		// Get the language set by the frontend
		const globalI18n = Local.get('themeConfig')?.globalI18n;
		if (globalI18n) {
			// Add to request header
			conf.headers!['Accept-Language'] = globalI18n;
		}
		return conf;
	},
	async (error) => {
		if (!error.config?.customCatch) ElMessage.error(httpErrorMessage(error));
		return Promise.reject(error);
	}
);

// Axios response interceptor
axiosInstance.interceptors.response.use(
	(res) => {
		// Get status code and return data
		var status = res.status;
		var serve = res.data;
		// Handle 401
		if (status === 401) {
			clearTokens();
		}

		// Handle unnormalized processing
		if (status >= 400) {
			throw new Error(res.statusText || 'Request Error.');
		}

		// Handle normalized result error
		if (serve && serve.hasOwnProperty('errors') && serve.errors) {
			throw new Error(JSON.stringify(serve.errors || 'Request Error.'));
		}

		// Read response header token information
		var accessToken = res.headers[accessTokenKey];
		var refreshAccessToken = res.headers[refreshAccessTokenKey];

		// Check if it is an invalid token
		if (accessToken === 'invalid_token') {
			clearAccessTokens();
		}
		// Check if there is a refresh token, if so, store it locally
		else if (refreshAccessToken && accessToken && accessToken !== 'invalid_token') {
			Local.set(accessTokenKey, accessToken);
			Local.set(refreshAccessTokenKey, refreshAccessToken);
		}

		// Response interception and custom handling
		if (serve.code === 401) {
			setTimeout(() => {
				clearAccessTokens();
			}, 2000);
		} else if (serve.code === undefined) {
			return Promise.resolve(res);
		} else if (serve.code !== 200) {
			var message;
			// Check if serve.message is an object
			if (serve.message && typeof serve.message == 'object') {
				message = JSON.stringify(serve.message);
			} else {
				message = serve.message;
			}
			// User custom handling exception
			if (!res.config?.customCatch) {
				ElMessage.error(message?.replace(/[\u4e00-\u9fff]/g, ''));

			}
			throw new Error(message?.replace(/[\u4e00-\u9fff]/g, ''));
		}

		return res;
	},
	async (error) => {

		if (error.response?.status === 401) clearAccessTokens();
		// Chỉ truyền chuỗi; object Axios không phải options của ElMessage.
		if (!error.config?.customCatch && !globalAxios.isCancel(error)) {
			let payload = error.response?.data;
			if (payload instanceof Blob) {
				try {
					const text = await payload.text();
					try { payload = JSON.parse(text); } catch { payload = text; }
				} catch { payload = undefined; }
			}
			ElMessage.error(httpErrorMessage(payload, httpErrorMessage(error)));
		}
		return Promise.reject(error);
	}
);

/**
 * Wrap Promise and return [Error, any]
 * @param promise Promise method
 * @param errorExt Custom error message (extension)
 * @returns [Error, any]
 */
export function feature<T, U = Error>(promise: Promise<T>, errorExt?: object): Promise<[U, undefined] | [null, T]> {
	return promise
		.then<[null, T]>((data: T) => [null, data])
		.catch<[U, undefined]>((err: U) => {
			if (errorExt) {
				const parsedError = Object.assign({}, err, errorExt);
				return [parsedError, undefined];
			}

			return [err, undefined];
		});
}

/**
 * Get/create service API instance
 * @param apiType BaseAPI derived type
 * @param configuration Server configuration object
 * @param basePath Server address
 * @param axiosObject axios instance
 * @returns Service API instance
 */
export function getAPI<T>(
	// eslint-disable-next-line no-unused-vars
	apiType: new (configuration?: Configuration, basePath?: string, axiosInstance?: AxiosInstance) => T,
	configuration: Configuration = serveConfig,
	basePath: string = BASE_PATH,
	axiosObject: AxiosInstance = axiosInstance
) {
	return new apiType(configuration, basePath, axiosObject);
}

/**
 * Decrypt JWT token information
 * @param token jwt token string
 * @returns <any>object
 */
export function decryptJWT(token: string): any {
	token = token.replace(/_/g, '/').replace(/-/g, '+');
	var json = decodeURIComponent(escape(window.atob(token.split('.')[1])));
	return JSON.parse(json);
}

/**
 * Convert JWT timestamp to Date
 * @description Mainly for `exp`, `iat`, `nbf`
 * @param timestamp Timestamp
 * @returns Date object
 */
export function getJWTDate(timestamp: number): Date {
	return new Date(timestamp * 1000);
}

/**
 * Implement asynchronous delay
 * @param delay Delay time (milliseconds)
 * @returns
 */
export function sleep(delay: number) {
	return new Promise((resolve) => setTimeout(resolve, delay));
}
