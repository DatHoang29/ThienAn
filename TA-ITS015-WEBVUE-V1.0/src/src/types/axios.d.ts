/* eslint-disable */
import * as axios from 'axios';

// Extend the axios data return type, you can extend it yourself
declare module 'axios' {
	export interface AxiosResponse<T = any> {
		code: number;
		data: T;
		message: string;
		type?: string;
		[key: string]: T;
	}
}
