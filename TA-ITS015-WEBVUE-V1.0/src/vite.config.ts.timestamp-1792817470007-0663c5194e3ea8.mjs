// vite.config.ts
import vue from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/@vitejs+plugin-vue@6.0.10_v_65125ceabe076999e9adb5feaac00762/node_modules/@vitejs/plugin-vue/dist/index.mjs";
import { resolve } from "path";
import { defineConfig, loadEnv } from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/vite@5.4.21_@types+node@25._35b74319471954f9bb81f64227ba2706/node_modules/vite/dist/node/index.js";
import vueSetupExtend from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/vite-plugin-vue-setup-exten_e59dd7677e0816c48787882f61d94a56/node_modules/vite-plugin-vue-setup-extend/dist/index.mjs";

// src/utils/build.ts
import importToCDN from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/vite-plugin-cdn-import@1.0._9a5e6d938fd176210ae222e4df4f9284/node_modules/vite-plugin-cdn-import/dist/index.js";
var buildConfig = {
  cdn() {
    return importToCDN({
      prodUrl: "https://unpkg.com/{name}@{version}/{path}",
      modules: [
        // autoComplete('vue'),
        // autoComplete('axios'),
        {
          name: "vue",
          var: "Vue",
          path: "dist/vue.global.js"
        },
        {
          name: "vue-demi",
          var: "VueDemi",
          path: "lib/index.iife.js"
        },
        {
          name: "vue-router",
          var: "VueRouter",
          path: "dist/vue-router.global.js"
        },
        {
          name: "element-plus",
          var: "ElementPlus",
          path: "dist/index.full.js"
        }
        // {
        // 	name: '@element-plus/icons-vue',
        // 	var: 'ElementPlusIconsVue',
        // 	path: 'dist/index.iife.min.js',
        // },
        // {
        // 	name: 'echarts',
        // 	var: 'echarts',
        // 	path: 'dist/echarts.min.js',
        // },
        // {
        // 	name: 'echarts-gl',
        // 	var: 'echarts-gl',
        // 	path: 'dist/echarts-gl.min.js',
        // },
        // {
        // 	name: 'echarts-wordcloud',
        // 	var: 'echarts-wordcloud',
        // 	path: 'dist/echarts-wordcloud.min.js',
        // },
        // {
        // 	name: 'vue-i18n',
        // 	var: 'VueI18n',
        // 	path: 'dist/vue-i18n.global.min.js',
        // },
        // {
        // 	name: 'jsplumb',
        // 	var: 'jsPlumb',
        // 	path: 'dist/js/jsplumb.min.js',
        // },
        // {
        // 	name: 'cropperjs',
        // 	var: 'Cropper',
        // 	path: 'dist/cropper.min.js',
        // },
        // {
        // 	name: 'sortablejs',
        // 	var: 'Sortable',
        // 	path: 'Sortable.min.js',
        // },
        // {
        // 	name: 'qrcodejs2-fixes',
        // 	var: 'QRCode',
        // 	path: 'qrcode.min.js',
        // },
        // {
        // 	name: 'print-js',
        // 	var: 'printJS',
        // 	path: 'dist/print.min.js',
        // },
        // {
        // 	name: '@wangeditor/editor',
        // 	var: 'wangEditor',
        // 	path: 'dist/index.min.js',
        // },
        // {
        // 	name: '@wangeditor/editor-for-vue',
        // 	var: 'WangEditorForVue',
        // 	path: 'dist/index.min.js',
        // },
        // {
        // 	name: 'vue-grid-layout',
        // 	var: 'VueGridLayout',
        // 	path: 'https://cdn.jsdelivr.net/npm/vue-grid-layout@3.0.0-beta1/dist/vue-grid-layout.umd.min.js',
        // },
      ]
    });
  },
  external: [
    "vue",
    // 'axios',
    "vue-router",
    "element-plus"
    // '@element-plus/icons-vue',
    // 'echarts',
    // 'echarts-gl',
    // 'echarts-wordcloud',
    // 'vue-i18n',
    // 'jsplumb',
    // 'cropperjs',
    // 'sortablejs',
    // 'qrcodejs2-fixes',
    // 'print-js',
    // '@wangeditor/editor',
    // '@wangeditor/editor-for-vue',
    // 'vue-grid-layout',
  ]
};

// vite.config.ts
import vueJsx from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/@vitejs+plugin-vue-jsx@5.1._0064d063d4e3d54a6e5288b944048d88/node_modules/@vitejs/plugin-vue-jsx/dist/index.mjs";
import { CodeInspectorPlugin } from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/code-inspector-plugin@1.6.6/node_modules/code-inspector-plugin/dist/index.mjs";
import fs from "fs";
import { visualizer } from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/rollup-plugin-visualizer@7.1.1_rollup@4.64.5/node_modules/rollup-plugin-visualizer/dist/plugin/index.js";
import { webUpdateNotice } from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/@plugin-web-update-notifica_b3033b95242c2fe55f028918b90fc320/node_modules/@plugin-web-update-notification/vite/dist/index.mjs";
import VueDevtools from "file:///D:/TACorp/TA-ITS015-WEBVUE-V1.0/src/node_modules/.pnpm/vite-plugin-vue-devtools@7._b1617303e470b3da811f7403a0e3011b/node_modules/vite-plugin-vue-devtools/dist/vite.mjs";
var __vite_injected_original_dirname = "D:\\TACorp\\TA-ITS015-WEBVUE-V1.0\\src";
var pathResolve = (dir) => {
  return resolve(__vite_injected_original_dirname, ".", dir);
};
var alias = {
  "/@": pathResolve("./src/"),
  "vue-i18n": "vue-i18n/dist/vue-i18n.cjs.js"
};
var tileProxyPlugin = () => ({
  name: "tile-proxy-middleware",
  configureServer(server) {
    server.middlewares.use("/tile-proxy", async (req, res) => {
      try {
        const raw = (req.url || "").replace(/^\//, "");
        const slashIdx = raw.indexOf("/");
        const encodedOrigin = slashIdx === -1 ? raw : raw.slice(0, slashIdx);
        const rest = slashIdx === -1 ? "" : raw.slice(slashIdx);
        const origin = decodeURIComponent(encodedOrigin);
        const targetUrl = origin + rest;
        const upstream = await fetch(targetUrl, {
          headers: {
            "User-Agent": req.headers["user-agent"] || "Mozilla/5.0",
            Accept: req.headers["accept"] || "image/*,*/*"
          }
        });
        res.statusCode = upstream.status;
        res.setHeader("Access-Control-Allow-Origin", "*");
        const contentType = upstream.headers.get("content-type");
        if (contentType) res.setHeader("Content-Type", contentType);
        const cacheControl = upstream.headers.get("cache-control");
        if (cacheControl) res.setHeader("Cache-Control", cacheControl);
        const buf = Buffer.from(await upstream.arrayBuffer());
        res.end(buf);
      } catch (err) {
        console.error("[tile-proxy] error:", err);
        res.statusCode = 502;
        res.end("Tile proxy error");
      }
    });
  }
});
var viteConfig = defineConfig((mode) => {
  console.log("Site running mode:", mode.mode);
  const env = loadEnv(mode.mode, process.cwd());
  fs.writeFileSync("./public/config.js", `window.__env__ = ${JSON.stringify(env, null, 2)} `);
  return {
    plugins: [
      visualizer({ open: false }),
      // Enable visualization analysis page
      CodeInspectorPlugin({
        bundler: "vite",
        hotKeys: ["shiftKey"]
      }),
      vue(),
      vueJsx(),
      VueDevtools(),
      webUpdateNotice({
        versionType: "build_timestamp",
        notificationConfig: {
          placement: "topLeft"
        },
        notificationProps: {
          title: "\u{1F4E2} System Update",
          description: "The system has been updated, please refresh the page!",
          buttonText: "Refresh",
          dismissButtonText: "Ignore"
        }
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
      JSON.parse(env.VITE_OPEN_CDN) ? buildConfig.cdn() : null
    ],
    root: process.cwd(),
    resolve: { alias },
    base: mode.command === "serve" ? "./" : env.VITE_PUBLIC_PATH,
    optimizeDeps: { exclude: ["vue-demi"] },
    server: {
      host: "0.0.0.0",
      port: env.VITE_PORT,
      open: JSON.parse(env.VITE_OPEN),
      hmr: true,
      proxy: {
        "^/[Uu]pload": {
          target: env.VITE_API_URL,
          changeOrigin: true
        }
      }
    },
    build: {
      outDir: "dist",
      chunkSizeWarningLimit: 1500,
      // assetsInlineLimit: 5000, // Assets smaller than this threshold will be inlined as base64 encoding
      sourcemap: false,
      // Whether to generate source map files after build
      extractComments: false,
      // Remove comments
      minify: "terser",
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
      __NEXT_NAME__: JSON.stringify(process.env.npm_package_name)
    }
  };
});
var vite_config_default = viteConfig;
export {
  vite_config_default as default
};
//# sourceMappingURL=data:application/json;base64,ewogICJ2ZXJzaW9uIjogMywKICAic291cmNlcyI6IFsidml0ZS5jb25maWcudHMiLCAic3JjL3V0aWxzL2J1aWxkLnRzIl0sCiAgInNvdXJjZXNDb250ZW50IjogWyJjb25zdCBfX3ZpdGVfaW5qZWN0ZWRfb3JpZ2luYWxfZGlybmFtZSA9IFwiRDpcXFxcVEFDb3JwXFxcXFRBLUlUUzAxNS1XRUJWVUUtVjEuMFxcXFxzcmNcIjtjb25zdCBfX3ZpdGVfaW5qZWN0ZWRfb3JpZ2luYWxfZmlsZW5hbWUgPSBcIkQ6XFxcXFRBQ29ycFxcXFxUQS1JVFMwMTUtV0VCVlVFLVYxLjBcXFxcc3JjXFxcXHZpdGUuY29uZmlnLnRzXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ltcG9ydF9tZXRhX3VybCA9IFwiZmlsZTovLy9EOi9UQUNvcnAvVEEtSVRTMDE1LVdFQlZVRS1WMS4wL3NyYy92aXRlLmNvbmZpZy50c1wiO2ltcG9ydCB2dWUgZnJvbSAnQHZpdGVqcy9wbHVnaW4tdnVlJztcclxuaW1wb3J0IHsgcmVzb2x2ZSB9IGZyb20gJ3BhdGgnO1xyXG5pbXBvcnQgeyBkZWZpbmVDb25maWcsIGxvYWRFbnYsIENvbmZpZ0VudiB9IGZyb20gJ3ZpdGUnO1xyXG5pbXBvcnQgdnVlU2V0dXBFeHRlbmQgZnJvbSAndml0ZS1wbHVnaW4tdnVlLXNldHVwLWV4dGVuZCc7XHJcbmltcG9ydCBjb21wcmVzc2lvbiBmcm9tICd2aXRlLXBsdWdpbi1jb21wcmVzc2lvbjInO1xyXG5pbXBvcnQgeyBidWlsZENvbmZpZyB9IGZyb20gJy4vc3JjL3V0aWxzL2J1aWxkJztcclxuaW1wb3J0IHZ1ZUpzeCBmcm9tICdAdml0ZWpzL3BsdWdpbi12dWUtanN4JztcclxuaW1wb3J0IHsgQ29kZUluc3BlY3RvclBsdWdpbiB9IGZyb20gJ2NvZGUtaW5zcGVjdG9yLXBsdWdpbic7XHJcbmltcG9ydCBmcyBmcm9tICdmcyc7XHJcbmltcG9ydCB7IHZpc3VhbGl6ZXIgfSBmcm9tICdyb2xsdXAtcGx1Z2luLXZpc3VhbGl6ZXInO1xyXG5pbXBvcnQgeyB3ZWJVcGRhdGVOb3RpY2UgfSBmcm9tICdAcGx1Z2luLXdlYi11cGRhdGUtbm90aWZpY2F0aW9uL3ZpdGUnO1xyXG5pbXBvcnQgVnVlRGV2dG9vbHMgZnJvbSAndml0ZS1wbHVnaW4tdnVlLWRldnRvb2xzJztcclxuXHJcbmNvbnN0IHBhdGhSZXNvbHZlID0gKGRpcjogc3RyaW5nKSA9PiB7XHJcblx0cmV0dXJuIHJlc29sdmUoX19kaXJuYW1lLCAnLicsIGRpcik7XHJcbn07XHJcblxyXG5jb25zdCBhbGlhczogUmVjb3JkPHN0cmluZywgc3RyaW5nPiA9IHtcclxuXHQnL0AnOiBwYXRoUmVzb2x2ZSgnLi9zcmMvJyksXHJcblx0J3Z1ZS1pMThuJzogJ3Z1ZS1pMThuL2Rpc3QvdnVlLWkxOG4uY2pzLmpzJyxcclxufTtcclxuXHJcbi8qKlxyXG4gKiBDdXN0b20gVml0ZSBwbHVnaW46IHByb3h5IHRpbGUgYlx1MUVBM24gXHUwMTExXHUxRUQzIFx1MDExMVx1MUVEOW5nIFx1MDExMVx1MUVDMyB0clx1MDBFMW5oIENPUlMuXHJcbiAqIEhcdTFFRDcgdHJcdTFFRTMgQlx1MUVBNFQgS1x1MUVGMiB0aWxlIHNlcnZlciBuXHUwMEUwbyAoaHR0cCBob1x1MUVCN2MgaHR0cHMpLCBraFx1MDBGNG5nIGNcdTFFQTduIGNcdTFFQTV1IGhcdTAwRUNuaCBjXHUxRUU5bmcuXHJcbiAqXHJcbiAqIEZyb250ZW5kIGdcdTFFQ0RpOiAvdGlsZS1wcm94eS97ZW5jb2RlVVJJQ29tcG9uZW50KG9yaWdpbil9L3BhdGguLi5cclxuICogVlx1MDBFRCBkXHUxRUU1OiAvdGlsZS1wcm94eS9odHRwcyUzQSUyRiUyRnRpbGUudGh1bmRlcmZvcmVzdC5jb20vbW9iaWxlLWF0bGFzLzExLzE2MzkvOTYzLnBuZz9hcGlrZXk9eHh4XHJcbiAqIFBsdWdpbiBzXHUxRUJEIGZldGNoIHRpbGUgdFx1MUVFQiBzZXJ2ZXIgZ1x1MUVEMWMgdlx1MDBFMCBzdHJlYW0gdlx1MUVDMSwga1x1MDBFOG0gaGVhZGVyIENPUlMuXHJcbiAqL1xyXG5jb25zdCB0aWxlUHJveHlQbHVnaW4gPSAoKSA9PiAoe1xyXG5cdG5hbWU6ICd0aWxlLXByb3h5LW1pZGRsZXdhcmUnLFxyXG5cdGNvbmZpZ3VyZVNlcnZlcihzZXJ2ZXI6IGFueSkge1xyXG5cdFx0c2VydmVyLm1pZGRsZXdhcmVzLnVzZSgnL3RpbGUtcHJveHknLCBhc3luYyAocmVxOiBhbnksIHJlczogYW55KSA9PiB7XHJcblx0XHRcdHRyeSB7XHJcblx0XHRcdFx0Ly8gcmVxLnVybCBcdTFFREYgXHUwMTExXHUwMEUyeSBcdTAxMTFcdTAwRTMgYlx1MUVDRiB0aVx1MUVDMW4gdFx1MUVEMSAvdGlsZS1wcm94eSBcdTIxOTIgZFx1MUVBMW5nOiAve2VuY29kZWRfb3JpZ2lufS9wYXRoP3F1ZXJ5XHJcblx0XHRcdFx0Y29uc3QgcmF3ID0gKHJlcS51cmwgfHwgJycpLnJlcGxhY2UoL15cXC8vLCAnJyk7XHJcblx0XHRcdFx0Y29uc3Qgc2xhc2hJZHggPSByYXcuaW5kZXhPZignLycpO1xyXG5cdFx0XHRcdGNvbnN0IGVuY29kZWRPcmlnaW4gPSBzbGFzaElkeCA9PT0gLTEgPyByYXcgOiByYXcuc2xpY2UoMCwgc2xhc2hJZHgpO1xyXG5cdFx0XHRcdGNvbnN0IHJlc3QgPSBzbGFzaElkeCA9PT0gLTEgPyAnJyA6IHJhdy5zbGljZShzbGFzaElkeCk7XHJcblx0XHRcdFx0Y29uc3Qgb3JpZ2luID0gZGVjb2RlVVJJQ29tcG9uZW50KGVuY29kZWRPcmlnaW4pO1xyXG5cdFx0XHRcdGNvbnN0IHRhcmdldFVybCA9IG9yaWdpbiArIHJlc3Q7XHJcblxyXG5cdFx0XHRcdGNvbnN0IHVwc3RyZWFtID0gYXdhaXQgZmV0Y2godGFyZ2V0VXJsLCB7XHJcblx0XHRcdFx0XHRoZWFkZXJzOiB7XHJcblx0XHRcdFx0XHRcdCdVc2VyLUFnZW50JzogcmVxLmhlYWRlcnNbJ3VzZXItYWdlbnQnXSB8fCAnTW96aWxsYS81LjAnLFxyXG5cdFx0XHRcdFx0XHRBY2NlcHQ6IHJlcS5oZWFkZXJzWydhY2NlcHQnXSB8fCAnaW1hZ2UvKiwqLyonLFxyXG5cdFx0XHRcdFx0fSxcclxuXHRcdFx0XHR9KTtcclxuXHJcblx0XHRcdFx0cmVzLnN0YXR1c0NvZGUgPSB1cHN0cmVhbS5zdGF0dXM7XHJcblx0XHRcdFx0cmVzLnNldEhlYWRlcignQWNjZXNzLUNvbnRyb2wtQWxsb3ctT3JpZ2luJywgJyonKTtcclxuXHRcdFx0XHRjb25zdCBjb250ZW50VHlwZSA9IHVwc3RyZWFtLmhlYWRlcnMuZ2V0KCdjb250ZW50LXR5cGUnKTtcclxuXHRcdFx0XHRpZiAoY29udGVudFR5cGUpIHJlcy5zZXRIZWFkZXIoJ0NvbnRlbnQtVHlwZScsIGNvbnRlbnRUeXBlKTtcclxuXHRcdFx0XHRjb25zdCBjYWNoZUNvbnRyb2wgPSB1cHN0cmVhbS5oZWFkZXJzLmdldCgnY2FjaGUtY29udHJvbCcpO1xyXG5cdFx0XHRcdGlmIChjYWNoZUNvbnRyb2wpIHJlcy5zZXRIZWFkZXIoJ0NhY2hlLUNvbnRyb2wnLCBjYWNoZUNvbnRyb2wpO1xyXG5cclxuXHRcdFx0XHRjb25zdCBidWYgPSBCdWZmZXIuZnJvbShhd2FpdCB1cHN0cmVhbS5hcnJheUJ1ZmZlcigpKTtcclxuXHRcdFx0XHRyZXMuZW5kKGJ1Zik7XHJcblx0XHRcdH0gY2F0Y2ggKGVycikge1xyXG5cdFx0XHRcdGNvbnNvbGUuZXJyb3IoJ1t0aWxlLXByb3h5XSBlcnJvcjonLCBlcnIpO1xyXG5cdFx0XHRcdHJlcy5zdGF0dXNDb2RlID0gNTAyO1xyXG5cdFx0XHRcdHJlcy5lbmQoJ1RpbGUgcHJveHkgZXJyb3InKTtcclxuXHRcdFx0fVxyXG5cdFx0fSk7XHJcblx0fSxcclxufSk7XHJcblxyXG5jb25zdCB2aXRlQ29uZmlnID0gZGVmaW5lQ29uZmlnKChtb2RlOiBDb25maWdFbnYpID0+IHtcclxuXHRjb25zb2xlLmxvZyhcIlNpdGUgcnVubmluZyBtb2RlOlwiLCBtb2RlLm1vZGUpO1xyXG5cdGNvbnN0IGVudiA9IGxvYWRFbnYobW9kZS5tb2RlLCBwcm9jZXNzLmN3ZCgpKTtcclxuXHRmcy53cml0ZUZpbGVTeW5jKCcuL3B1YmxpYy9jb25maWcuanMnLCBgd2luZG93Ll9fZW52X18gPSAke0pTT04uc3RyaW5naWZ5KGVudiwgbnVsbCwgMil9IGApO1xyXG5cdHJldHVybiB7XHJcblx0XHRwbHVnaW5zOiBbXHJcblx0XHRcdHZpc3VhbGl6ZXIoeyBvcGVuOiBmYWxzZSB9KSwgLy8gRW5hYmxlIHZpc3VhbGl6YXRpb24gYW5hbHlzaXMgcGFnZVxyXG5cdFx0XHRDb2RlSW5zcGVjdG9yUGx1Z2luKHtcclxuXHRcdFx0XHRidW5kbGVyOiAndml0ZScsXHJcblx0XHRcdFx0aG90S2V5czogWydzaGlmdEtleSddLFxyXG5cdFx0XHR9KSxcclxuXHRcdFx0dnVlKCksXHJcblx0XHRcdHZ1ZUpzeCgpLFxyXG5cdFx0XHRWdWVEZXZ0b29scygpLFxyXG5cdFx0XHR3ZWJVcGRhdGVOb3RpY2Uoe1xyXG5cdFx0XHRcdHZlcnNpb25UeXBlOiAnYnVpbGRfdGltZXN0YW1wJyxcclxuXHRcdFx0XHRub3RpZmljYXRpb25Db25maWc6IHtcclxuXHRcdFx0XHRcdHBsYWNlbWVudDogJ3RvcExlZnQnLFxyXG5cdFx0XHRcdH0sXHJcblx0XHRcdFx0bm90aWZpY2F0aW9uUHJvcHM6IHtcclxuXHRcdFx0XHRcdHRpdGxlOiAnXHVEODNEXHVEQ0UyIFN5c3RlbSBVcGRhdGUnLFxyXG5cdFx0XHRcdFx0ZGVzY3JpcHRpb246ICdUaGUgc3lzdGVtIGhhcyBiZWVuIHVwZGF0ZWQsIHBsZWFzZSByZWZyZXNoIHRoZSBwYWdlIScsXHJcblx0XHRcdFx0XHRidXR0b25UZXh0OiAnUmVmcmVzaCcsXHJcblx0XHRcdFx0XHRkaXNtaXNzQnV0dG9uVGV4dDogJ0lnbm9yZScsXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0fSksXHJcblx0XHRcdHZ1ZVNldHVwRXh0ZW5kKCksXHJcblx0XHRcdHRpbGVQcm94eVBsdWdpbigpLFxyXG5cdFx0XHQvLyBjb21wcmVzc2lvbih7XHJcblxyXG5cdFx0XHQvLyBcdGRlbGV0ZU9yaWdpbmFsQXNzZXRzOiBmYWxzZSwgLy8gV2hldGhlciB0byBkZWxldGUgdGhlIG9yaWdpbmFsIGZpbGVzXHJcblx0XHRcdC8vIFx0dGhyZXNob2xkOiA1MTIwLCAvLyBDb21wcmVzcyBmaWxlcyBsYXJnZXIgdGhhbiA1S0IsIHVuaXQgQnl0ZXNcclxuXHRcdFx0Ly8gXHRza2lwSWZMYXJnZXJPckVxdWFsOiB0cnVlLCAvLyBTa2lwIGNvbXByZXNzaW9uIGlmIHRoZSBjb21wcmVzc2VkIGZpbGUgc2l6ZSBpcyBlcXVhbCB0byBvciBsYXJnZXIgdGhhbiB0aGUgb3JpZ2luYWxcclxuXHRcdFx0Ly8gXHQvLyBhbGdvcml0aG06ICdnemlwJywgLy8gQ29tcHJlc3Npb24gYWxnb3JpdGhtLCBvcHRpb25zIFsnZ3ppcCcsICdicm90bGlDb21wcmVzcycsICdkZWZsYXRlJywgJ2RlZmxhdGVSYXcnXVxyXG5cdFx0XHQvLyBcdC8vIGV4Y2x1ZGU6IFsvXFwuKGJyKSQvLCAvXFwuKGd6KSQvXSwgLy8gRXhjbHVkZSBzcGVjaWZpYyBmaWxlc1xyXG5cdFx0XHQvLyB9KSxcclxuXHRcdFx0SlNPTi5wYXJzZShlbnYuVklURV9PUEVOX0NETikgPyBidWlsZENvbmZpZy5jZG4oKSA6IG51bGwsXHJcblx0XHRdLFxyXG5cdFx0cm9vdDogcHJvY2Vzcy5jd2QoKSxcclxuXHRcdHJlc29sdmU6IHsgYWxpYXMgfSxcclxuXHRcdGJhc2U6IG1vZGUuY29tbWFuZCA9PT0gJ3NlcnZlJyA/ICcuLycgOiBlbnYuVklURV9QVUJMSUNfUEFUSCxcclxuXHRcdG9wdGltaXplRGVwczogeyBleGNsdWRlOiBbJ3Z1ZS1kZW1pJ10gfSxcclxuXHRcdHNlcnZlcjoge1xyXG5cdFx0XHRob3N0OiAnMC4wLjAuMCcsXHJcblx0XHRcdHBvcnQ6IGVudi5WSVRFX1BPUlQgYXMgdW5rbm93biBhcyBudW1iZXIsXHJcblx0XHRcdG9wZW46IEpTT04ucGFyc2UoZW52LlZJVEVfT1BFTiksXHJcblx0XHRcdGhtcjogdHJ1ZSxcclxuXHRcdFx0cHJveHk6IHtcclxuXHRcdFx0XHQnXi9bVXVdcGxvYWQnOiB7XHJcblx0XHRcdFx0XHR0YXJnZXQ6IGVudi5WSVRFX0FQSV9VUkwsXHJcblx0XHRcdFx0XHRjaGFuZ2VPcmlnaW46IHRydWUsXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0fSxcclxuXHRcdH0sXHJcblx0XHRidWlsZDoge1xyXG5cdFx0XHRvdXREaXI6ICdkaXN0JyxcclxuXHRcdFx0Y2h1bmtTaXplV2FybmluZ0xpbWl0OiAxNTAwLFxyXG5cdFx0XHQvLyBhc3NldHNJbmxpbmVMaW1pdDogNTAwMCwgLy8gQXNzZXRzIHNtYWxsZXIgdGhhbiB0aGlzIHRocmVzaG9sZCB3aWxsIGJlIGlubGluZWQgYXMgYmFzZTY0IGVuY29kaW5nXHJcblx0XHRcdHNvdXJjZW1hcDogZmFsc2UsIC8vIFdoZXRoZXIgdG8gZ2VuZXJhdGUgc291cmNlIG1hcCBmaWxlcyBhZnRlciBidWlsZFxyXG5cdFx0XHRleHRyYWN0Q29tbWVudHM6IGZhbHNlLCAvLyBSZW1vdmUgY29tbWVudHNcclxuXHRcdFx0bWluaWZ5OiAndGVyc2VyJyxcclxuXHRcdFx0dGVyc2VyT3B0aW9uczoge1xyXG5cdFx0XHRcdGNvbXByZXNzOiB7XHJcblx0XHRcdFx0XHRkcm9wX2NvbnNvbGU6IHRydWUsXHJcblx0XHRcdFx0XHRkcm9wX2RlYnVnZ2VyOiB0cnVlXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0XHRtYW5nbGU6IHtcclxuXHRcdFx0XHRcdHRvcGxldmVsOiB0cnVlXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0XHRmb3JtYXQ6IHtcclxuXHRcdFx0XHRcdGNvbW1lbnRzOiBmYWxzZVxyXG5cdFx0XHRcdH1cclxuXHRcdFx0fVxyXG5cdFx0XHQvLyByb2xsdXBPcHRpb25zOiB7XHJcblx0XHRcdC8vIFx0b3V0cHV0OiB7XHJcblx0XHRcdC8vIFx0XHRjaHVua0ZpbGVOYW1lczogJ2Fzc2V0cy9qcy9bbmFtZV0tW2hhc2hdLmpzJywgLy8gTmFtZSBvZiB0aGUgaW1wb3J0ZWQgZmlsZVxyXG5cdFx0XHQvLyBcdFx0ZW50cnlGaWxlTmFtZXM6ICdhc3NldHMvanMvW25hbWVdLVtoYXNoXS5qcycsIC8vIFBhY2thZ2UgZW50cnkgZmlsZSBuYW1lXHJcblx0XHRcdC8vIFx0XHRhc3NldEZpbGVOYW1lczogJ2Fzc2V0cy9bZXh0XS9bbmFtZV0tW2hhc2hdLltleHRdJywgLy8gUmVzb3VyY2UgZmlsZXMgbGlrZSBmb250cywgaW1hZ2VzLCBldGMuXHJcblx0XHRcdC8vIFx0XHRtYW51YWxDaHVua3MoaWQpIHtcclxuXHRcdFx0Ly8gXHRcdFx0aWYgKGlkLmluY2x1ZGVzKCdub2RlX21vZHVsZXMnKSkge1xyXG5cdFx0XHQvLyBcdFx0XHRcdHJldHVybiBpZC50b1N0cmluZygpLm1hdGNoKC9cXC9ub2RlX21vZHVsZXNcXC8oPyEucG5wbSkoPzxtb2R1bGVOYW1lPlteXFwvXSopXFwvLyk/Lmdyb3VwcyEubW9kdWxlTmFtZSA/PyAndmVuZGVyJztcclxuXHRcdFx0Ly8gXHRcdFx0fVxyXG5cdFx0XHQvLyBcdFx0fSxcclxuXHRcdFx0Ly8gXHR9LFxyXG5cdFx0XHQvLyBcdC4uLihKU09OLnBhcnNlKGVudi5WSVRFX09QRU5fQ0ROKSA/IHsgZXh0ZXJuYWw6IGJ1aWxkQ29uZmlnLmV4dGVybmFsIH0gOiB7fSksXHJcblx0XHRcdC8vIH0sXHJcblx0XHR9LFxyXG5cdFx0Y3NzOiB7XHJcblx0XHRcdHByZXByb2Nlc3Nvck9wdGlvbnM6IHsgY3NzOiB7IGNoYXJzZXQ6IGZhbHNlIH0gfVxyXG5cdFx0fSxcclxuXHRcdGRlZmluZToge1xyXG5cdFx0XHRfX1ZVRV9JMThOX0xFR0FDWV9BUElfXzogSlNPTi5zdHJpbmdpZnkoZmFsc2UpLFxyXG5cdFx0XHRfX1ZVRV9JMThOX0ZVTExfSU5TVEFMTF9fOiBKU09OLnN0cmluZ2lmeShmYWxzZSksXHJcblx0XHRcdF9fSU5UTElGWV9QUk9EX0RFVlRPT0xTX186IEpTT04uc3RyaW5naWZ5KGZhbHNlKSxcclxuXHRcdFx0X19ORVhUX1ZFUlNJT05fXzogSlNPTi5zdHJpbmdpZnkocHJvY2Vzcy5lbnYubnBtX3BhY2thZ2VfdmVyc2lvbiksXHJcblx0XHRcdF9fTkVYVF9OQU1FX186IEpTT04uc3RyaW5naWZ5KHByb2Nlc3MuZW52Lm5wbV9wYWNrYWdlX25hbWUpLFxyXG5cdFx0fSxcclxuXHR9O1xyXG59KTtcclxuXHJcbmV4cG9ydCBkZWZhdWx0IHZpdGVDb25maWc7XHJcbiIsICJjb25zdCBfX3ZpdGVfaW5qZWN0ZWRfb3JpZ2luYWxfZGlybmFtZSA9IFwiRDpcXFxcVEFDb3JwXFxcXFRBLUlUUzAxNS1XRUJWVUUtVjEuMFxcXFxzcmNcXFxcc3JjXFxcXHV0aWxzXCI7Y29uc3QgX192aXRlX2luamVjdGVkX29yaWdpbmFsX2ZpbGVuYW1lID0gXCJEOlxcXFxUQUNvcnBcXFxcVEEtSVRTMDE1LVdFQlZVRS1WMS4wXFxcXHNyY1xcXFxzcmNcXFxcdXRpbHNcXFxcYnVpbGQudHNcIjtjb25zdCBfX3ZpdGVfaW5qZWN0ZWRfb3JpZ2luYWxfaW1wb3J0X21ldGFfdXJsID0gXCJmaWxlOi8vL0Q6L1RBQ29ycC9UQS1JVFMwMTUtV0VCVlVFLVYxLjAvc3JjL3NyYy91dGlscy9idWlsZC50c1wiO2ltcG9ydCBpbXBvcnRUb0NETiBmcm9tICd2aXRlLXBsdWdpbi1jZG4taW1wb3J0JztcclxuXHJcbi8qKlxyXG4gKiBQYWNrYWdpbmcgcmVsYXRlZFxyXG4gKiBOb3RlIHByb2RVcmw6IHdoZXRoZXIgdG8gdXNlIGpzZGVsaXZyIG9yIHVucGtnLiBUaGVpciBwYXRocyBtYXkgYmUgaW5jb25zaXN0ZW50XHJcbiAqIEFydGljbGUgbGluazogaHR0cHM6Ly9ibG9nLmNzZG4ubmV0L3FxXzM0NDUwNzQxL2FydGljbGUvZGV0YWlscy8xMjk3NjY2NzYsIHVzaW5nIGpzZGVsaXZyXHJcbiAqIEBkZXNjcmlwdGlvbiBpbXBvcnRUb0NETiBodHRwczovL2dpdGh1Yi5jb20vbW1mLWZlL3ZpdGUtcGx1Z2luLWNkbi1pbXBvcnRcclxuICogQGRlc2NyaXB0aW9uIGNkbiBvbmxpbmUgaW1wb3J0IGNkbiBhZGRyZXNzIGNvbmZpZ3VyYXRpb24uIHBhdGg6IGh0dHBzOi8vd3d3LmpzZGVsaXZyLmNvbS8gfHwgaHR0cHM6Ly91bnBrZy5jb20vXHJcbiAqIEBkZXNjcmlwdGlvbiBleHRlcm5hbCBmaWx0ZXJzIHBhY2thZ2UgaW1wb3J0cyBkdXJpbmcgcGFja2FnaW5nLiBSZWZlcmVuY2U6IGh0dHBzOi8vcm9sbHVwanMub3JnL2NvbmZpZ3VyYXRpb24tb3B0aW9ucy8jZXh0ZXJuYWxcclxuICovXHJcbmV4cG9ydCBjb25zdCBidWlsZENvbmZpZyA9IHtcclxuXHRjZG4oKSB7XHJcblx0XHRyZXR1cm4gaW1wb3J0VG9DRE4oe1xyXG5cdFx0XHRwcm9kVXJsOiAnaHR0cHM6Ly91bnBrZy5jb20ve25hbWV9QHt2ZXJzaW9ufS97cGF0aH0nLFxyXG5cdFx0XHRtb2R1bGVzOiBbXHJcblx0XHRcdFx0Ly8gYXV0b0NvbXBsZXRlKCd2dWUnKSxcclxuXHRcdFx0XHQvLyBhdXRvQ29tcGxldGUoJ2F4aW9zJyksXHJcblx0XHRcdFx0e1xyXG5cdFx0XHRcdFx0bmFtZTogJ3Z1ZScsXHJcblx0XHRcdFx0XHR2YXI6ICdWdWUnLFxyXG5cdFx0XHRcdFx0cGF0aDogJ2Rpc3QvdnVlLmdsb2JhbC5qcycsXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0XHR7XHJcblx0XHRcdFx0XHRuYW1lOiAndnVlLWRlbWknLFxyXG5cdFx0XHRcdFx0dmFyOiAnVnVlRGVtaScsXHJcblx0XHRcdFx0XHRwYXRoOiAnbGliL2luZGV4LmlpZmUuanMnLFxyXG5cdFx0XHRcdH0sXHJcblx0XHRcdFx0e1xyXG5cdFx0XHRcdFx0bmFtZTogJ3Z1ZS1yb3V0ZXInLFxyXG5cdFx0XHRcdFx0dmFyOiAnVnVlUm91dGVyJyxcclxuXHRcdFx0XHRcdHBhdGg6ICdkaXN0L3Z1ZS1yb3V0ZXIuZ2xvYmFsLmpzJyxcclxuXHRcdFx0XHR9LFxyXG5cdFx0XHRcdHtcclxuXHRcdFx0XHRcdG5hbWU6ICdlbGVtZW50LXBsdXMnLFxyXG5cdFx0XHRcdFx0dmFyOiAnRWxlbWVudFBsdXMnLFxyXG5cdFx0XHRcdFx0cGF0aDogJ2Rpc3QvaW5kZXguZnVsbC5qcycsXHJcblx0XHRcdFx0fSxcclxuXHRcdFx0XHQvLyB7XHJcblx0XHRcdFx0Ly8gXHRuYW1lOiAnQGVsZW1lbnQtcGx1cy9pY29ucy12dWUnLFxyXG5cdFx0XHRcdC8vIFx0dmFyOiAnRWxlbWVudFBsdXNJY29uc1Z1ZScsXHJcblx0XHRcdFx0Ly8gXHRwYXRoOiAnZGlzdC9pbmRleC5paWZlLm1pbi5qcycsXHJcblx0XHRcdFx0Ly8gfSxcclxuXHRcdFx0XHQvLyB7XHJcblx0XHRcdFx0Ly8gXHRuYW1lOiAnZWNoYXJ0cycsXHJcblx0XHRcdFx0Ly8gXHR2YXI6ICdlY2hhcnRzJyxcclxuXHRcdFx0XHQvLyBcdHBhdGg6ICdkaXN0L2VjaGFydHMubWluLmpzJyxcclxuXHRcdFx0XHQvLyB9LFxyXG5cdFx0XHRcdC8vIHtcclxuXHRcdFx0XHQvLyBcdG5hbWU6ICdlY2hhcnRzLWdsJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ2VjaGFydHMtZ2wnLFxyXG5cdFx0XHRcdC8vIFx0cGF0aDogJ2Rpc3QvZWNoYXJ0cy1nbC5taW4uanMnLFxyXG5cdFx0XHRcdC8vIH0sXHJcblx0XHRcdFx0Ly8ge1xyXG5cdFx0XHRcdC8vIFx0bmFtZTogJ2VjaGFydHMtd29yZGNsb3VkJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ2VjaGFydHMtd29yZGNsb3VkJyxcclxuXHRcdFx0XHQvLyBcdHBhdGg6ICdkaXN0L2VjaGFydHMtd29yZGNsb3VkLm1pbi5qcycsXHJcblx0XHRcdFx0Ly8gfSxcclxuXHRcdFx0XHQvLyB7XHJcblx0XHRcdFx0Ly8gXHRuYW1lOiAndnVlLWkxOG4nLFxyXG5cdFx0XHRcdC8vIFx0dmFyOiAnVnVlSTE4bicsXHJcblx0XHRcdFx0Ly8gXHRwYXRoOiAnZGlzdC92dWUtaTE4bi5nbG9iYWwubWluLmpzJyxcclxuXHRcdFx0XHQvLyB9LFxyXG5cdFx0XHRcdC8vIHtcclxuXHRcdFx0XHQvLyBcdG5hbWU6ICdqc3BsdW1iJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ2pzUGx1bWInLFxyXG5cdFx0XHRcdC8vIFx0cGF0aDogJ2Rpc3QvanMvanNwbHVtYi5taW4uanMnLFxyXG5cdFx0XHRcdC8vIH0sXHJcblx0XHRcdFx0Ly8ge1xyXG5cdFx0XHRcdC8vIFx0bmFtZTogJ2Nyb3BwZXJqcycsXHJcblx0XHRcdFx0Ly8gXHR2YXI6ICdDcm9wcGVyJyxcclxuXHRcdFx0XHQvLyBcdHBhdGg6ICdkaXN0L2Nyb3BwZXIubWluLmpzJyxcclxuXHRcdFx0XHQvLyB9LFxyXG5cdFx0XHRcdC8vIHtcclxuXHRcdFx0XHQvLyBcdG5hbWU6ICdzb3J0YWJsZWpzJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ1NvcnRhYmxlJyxcclxuXHRcdFx0XHQvLyBcdHBhdGg6ICdTb3J0YWJsZS5taW4uanMnLFxyXG5cdFx0XHRcdC8vIH0sXHJcblx0XHRcdFx0Ly8ge1xyXG5cdFx0XHRcdC8vIFx0bmFtZTogJ3FyY29kZWpzMi1maXhlcycsXHJcblx0XHRcdFx0Ly8gXHR2YXI6ICdRUkNvZGUnLFxyXG5cdFx0XHRcdC8vIFx0cGF0aDogJ3FyY29kZS5taW4uanMnLFxyXG5cdFx0XHRcdC8vIH0sXHJcblx0XHRcdFx0Ly8ge1xyXG5cdFx0XHRcdC8vIFx0bmFtZTogJ3ByaW50LWpzJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ3ByaW50SlMnLFxyXG5cdFx0XHRcdC8vIFx0cGF0aDogJ2Rpc3QvcHJpbnQubWluLmpzJyxcclxuXHRcdFx0XHQvLyB9LFxyXG5cdFx0XHRcdC8vIHtcclxuXHRcdFx0XHQvLyBcdG5hbWU6ICdAd2FuZ2VkaXRvci9lZGl0b3InLFxyXG5cdFx0XHRcdC8vIFx0dmFyOiAnd2FuZ0VkaXRvcicsXHJcblx0XHRcdFx0Ly8gXHRwYXRoOiAnZGlzdC9pbmRleC5taW4uanMnLFxyXG5cdFx0XHRcdC8vIH0sXHJcblx0XHRcdFx0Ly8ge1xyXG5cdFx0XHRcdC8vIFx0bmFtZTogJ0B3YW5nZWRpdG9yL2VkaXRvci1mb3ItdnVlJyxcclxuXHRcdFx0XHQvLyBcdHZhcjogJ1dhbmdFZGl0b3JGb3JWdWUnLFxyXG5cdFx0XHRcdC8vIFx0cGF0aDogJ2Rpc3QvaW5kZXgubWluLmpzJyxcclxuXHRcdFx0XHQvLyB9LFxyXG5cdFx0XHRcdC8vIHtcclxuXHRcdFx0XHQvLyBcdG5hbWU6ICd2dWUtZ3JpZC1sYXlvdXQnLFxyXG5cdFx0XHRcdC8vIFx0dmFyOiAnVnVlR3JpZExheW91dCcsXHJcblx0XHRcdFx0Ly8gXHRwYXRoOiAnaHR0cHM6Ly9jZG4uanNkZWxpdnIubmV0L25wbS92dWUtZ3JpZC1sYXlvdXRAMy4wLjAtYmV0YTEvZGlzdC92dWUtZ3JpZC1sYXlvdXQudW1kLm1pbi5qcycsXHJcblx0XHRcdFx0Ly8gfSxcclxuXHRcdFx0XSxcclxuXHRcdH0pO1xyXG5cdH0sXHJcblx0ZXh0ZXJuYWw6IFtcclxuXHRcdCd2dWUnLFxyXG5cdFx0Ly8gJ2F4aW9zJyxcclxuXHRcdCd2dWUtcm91dGVyJyxcclxuXHRcdCdlbGVtZW50LXBsdXMnLFxyXG5cdFx0Ly8gJ0BlbGVtZW50LXBsdXMvaWNvbnMtdnVlJyxcclxuXHRcdC8vICdlY2hhcnRzJyxcclxuXHRcdC8vICdlY2hhcnRzLWdsJyxcclxuXHRcdC8vICdlY2hhcnRzLXdvcmRjbG91ZCcsXHJcblx0XHQvLyAndnVlLWkxOG4nLFxyXG5cdFx0Ly8gJ2pzcGx1bWInLFxyXG5cdFx0Ly8gJ2Nyb3BwZXJqcycsXHJcblx0XHQvLyAnc29ydGFibGVqcycsXHJcblx0XHQvLyAncXJjb2RlanMyLWZpeGVzJyxcclxuXHRcdC8vICdwcmludC1qcycsXHJcblx0XHQvLyAnQHdhbmdlZGl0b3IvZWRpdG9yJyxcclxuXHRcdC8vICdAd2FuZ2VkaXRvci9lZGl0b3ItZm9yLXZ1ZScsXHJcblx0XHQvLyAndnVlLWdyaWQtbGF5b3V0JyxcclxuXHRdLFxyXG59O1xyXG4iXSwKICAibWFwcGluZ3MiOiAiO0FBQW1TLE9BQU8sU0FBUztBQUNuVCxTQUFTLGVB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
