import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';
import { defineConfig, loadEnv, ConfigEnv } from 'vite';
import vueSetupExtend from 'vite-plugin-vue-setup-extend';
import compression from 'vite-plugin-compression2';
import { buildConfig } from './src/utils/build';
import vueJsx from '@vitejs/plugin-vue-jsx';
import { CodeInspectorPlugin } from 'code-inspector-plugin';
import fs from 'fs';
import { visualizer } from 'rollup-plugin-visualizer';
import { webUpdateNotice } from '@plugin-web-update-notification/vite';
import VueDevtools from 'vite-plugin-vue-devtools';

const pathResolve = (dir: string) => {
	return resolve(__dirname, '.', dir);
};

const alias: Record<string, string> = {
	'/@': pathResolve('./src/'),
	'vue-i18n': 'vue-i18n/dist/vue-i18n.cjs.js',
};

/**
 * Custom Vite plugin: proxy tile bản đồ động để tránh CORS.
 * Hỗ trợ BẤT KỲ tile server nào (http hoặc https), không cần cấu hình cứng.
 *
 * Frontend gọi: /tile-proxy/{encodeURIComponent(origin)}/path...
 * Ví dụ: /tile-proxy/https%3A%2F%2Ftile.thunderforest.com/mobile-atlas/11/1639/963.png?apikey=xxx
 * Plugin sẽ fetch tile từ server gốc và stream về, kèm header CORS.
 */
const tileProxyPlugin = () => ({
	name: 'tile-proxy-middleware',
	configureServer(server: any) {
		server.middlewares.use('/tile-proxy', async (req: any, res: any) => {
			try {
				// req.url ở đây đã bỏ tiền tố /tile-proxy → dạng: /{encoded_origin}/path?query
				const raw = (req.url || '').replace(/^\//, '');
				const slashIdx = raw.indexOf('/');
				const encodedOrigin = slashIdx === -1 ? raw : raw.slice(0, slashIdx);
				const rest = slashIdx === -1 ? '' : raw.slice(slashIdx);
				const origin = decodeURIComponent(encodedOrigin);
				const targetUrl = origin + rest;

				const upstream = await fetch(targetUrl, {
					headers: {
						'User-Agent': req.headers['user-agent'] || 'Mozilla/5.0',
						Accept: req.headers['accept'] || 'image/*,*/*',
					},
				});

				res.statusCode = upstream.status;
				res.setHeader('Access-Control-Allow-Origin', '*');
				const contentType = upstream.headers.get('content-type');
				if (contentType) res.setHeader('Content-Type', contentType);
				const cacheControl = upstream.headers.get('cache-control');
				if (cacheControl) res.setHeader('Cache-Control', cacheControl);

				const buf = Buffer.from(await upstream.arrayBuffer());
				res.end(buf);
			} catch (err) {
				console.error('[tile-proxy] error:', err);
				res.statusCode = 502;
				res.end('Tile proxy error');
			}
		});
	},
});

const viteConfig = defineConfig((mode: ConfigEnv) => {
	console.log("Site running mode:", mode.mode);
	const env = loadEnv(mode.mode, process.cwd());
	fs.writeFileSync('./public/config.js', `window.__env__ = ${JSON.stringify(env, null, 2)} `);
	return {
		plugins: [
			visualizer({ open: false }), // Enable visualization analysis page
			CodeInspectorPlugin({
				bundler: 'vite',
				hotKeys: ['shiftKey'],
			}),
			vue(),
			vueJsx(),
			VueDevtools(),
			webUpdateNotice({
				versionType: 'build_timestamp',
				notificationConfig: {
					placement: 'topLeft',
				},
				notificationProps: {
					title: '📢 System Update',
					description: 'The system has been updated, please refresh the page!',
					buttonText: 'Refresh',
					dismissButtonText: 'Ignore',
				},
			}),
			vueSetupExtend(),
			tileProxyPlugin(),
			// compression({

			// 	deleteOriginalAssets: false, // Whether to delete the original files
			// 	threshold: 5120, // Compress files larger than 5KB, unit Bytes
			// 	skipIfLargerOrEqual: true, // Skip compression if the compressed file size is equal to or larger than the original
			// 	// algorithm: 'gzip', // Compression algorithm, options ['gzip', 'brotliCompress', 'deflate', 'deflateRaw']
			// 	// exclude: [/\.(br)$/, /\.(gz)$/], // Exclude specific files
			// }),
			JSON.parse(env.VITE_OPEN_CDN) ? buildConfig.cdn() : null,
		],
		root: process.cwd(),
		resolve: { alias },
		base: mode.command === 'serve' ? './' : env.VITE_PUBLIC_PATH,
		optimizeDeps: { exclude: ['vue-demi'] },
		server: {
			host: '0.0.0.0',
			port: env.VITE_PORT as unknown as number,
			open: JSON.parse(env.VITE_OPEN),
			hmr: true,
			proxy: {
				'^/[Uu]pload': {
					target: env.VITE_API_URL,
					changeOrigin: true,
				},
			},
		},
		build: {
			outDir: 'dist',
			chunkSizeWarningLimit: 1500,
			// assetsInlineLimit: 5000, // Assets smaller than this threshold will be inlined as base64 encoding
			sourcemap: false, // Whether to generate source map files after build
			extractComments: false, // Remove comments
			minify: 'terser',
			terserOptions: {
				compress: {
					drop_console: true,
					drop_debugger: true
				},
				mangle: {
					toplevel: true
				},
				format: {
					comments: false
				}
			}
			// rollupOptions: {
			// 	output: {
			// 		chunkFileNames: 'assets/js/[name]-[hash].js', // Name of the imported file
			// 		entryFileNames: 'assets/js/[name]-[hash].js', // Package entry file name
			// 		assetFileNames: 'assets/[ext]/[name]-[hash].[ext]', // Resource files like fonts, images, etc.
			// 		manualChunks(id) {
			// 			if (id.includes('node_modules')) {
			// 				return id.toString().match(/\/node_modules\/(?!.pnpm)(?<moduleName>[^\/]*)\//)?.groups!.moduleName ?? 'vender';
			// 			}
			// 		},
			// 	},
			// 	...(JSON.parse(env.VITE_OPEN_CDN) ? { external: buildConfig.external } : {}),
			// },
		},
		css: {
			preprocessorOptions: { css: { charset: false } }
		},
		define: {
			__VUE_I18N_LEGACY_API__: JSON.stringify(false),
			__VUE_I18N_FULL_INSTALL__: JSON.stringify(false),
			__INTLIFY_PROD_DEVTOOLS__: JSON.stringify(false),
			__NEXT_VERSION__: JSON.stringify(process.env.npm_package_version),
			__NEXT_NAME__: JSON.stringify(process.env.npm_package_name),
		},
	};
});

export default viteConfig;
