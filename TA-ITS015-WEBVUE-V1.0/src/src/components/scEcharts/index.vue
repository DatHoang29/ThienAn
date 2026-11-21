<template>
	<div ref="scEcharts" :style="{ height: height, width: width }"></div>
</template>

<script>
import * as echarts from 'echarts';
import T from './echarts-theme-T.js';

echarts.registerTheme('T', T);
const unwarp = (obj) => obj && (obj.__v_raw || obj.valueOf() || obj);

export default {
	...echarts,
	name: 'scEcharts',
	props: {
		height: { type: String, default: '100%' },
		width: { type: String, default: '100%' },
		nodata: { type: Boolean, default: false },
		option: {
			type: Object,
			default: () => {},
		},
	},
	data() {
		return {
			isActivat: false,
			myChart: null,
		};
	},
	watch: {
		option: {
			deep: true,
			handler(v) {
				unwarp(this.myChart).setOption(v);
			},
		},
	},
	computed: {
		myOptions: function () {
			return this.option || {};
		},
	},
	activated() {
		if (!this.isActivat) {
			this.$nextTick(() => {
				this.myChart.resize();
			});
		}
	},
	deactivated() {
		this.isActivat = false;
	},
	mounted() {
		this.isActivat = true;
		this.$nextTick(() => {
			this.draw();
		});
	},
	beforeUnmount() {
		if (this.resizeObserver) {
			this.resizeObserver.disconnect();
			this.resizeObserver = null;
		}
		if (this._onWinResize) {
			window.removeEventListener('resize', this._onWinResize);
		}
	},
	methods: {
		draw() {
			let myChart = echarts.init(this.$refs.scEcharts, 'T');
			myChart.setOption(this.myOptions);
			this.myChart = myChart;

			this._onWinResize = () => myChart.resize();
			window.addEventListener('resize', this._onWinResize);

			// Tự resize khi container thay đổi kích thước (vd: tab v-show từ ẩn -> hiện,
			// layout flex settle...) -> tránh chart bị vẽ nhỏ khi khởi tạo lúc container ẩn.
			if (typeof ResizeObserver !== 'undefined') {
				this.resizeObserver = new ResizeObserver(() => {
					if (this.myChart) this.myChart.resize();
				});
				this.resizeObserver.observe(this.$refs.scEcharts);
			}
		},
	},
};
</script>
