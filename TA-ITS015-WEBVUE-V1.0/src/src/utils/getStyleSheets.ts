import { nextTick } from 'vue';
import * as svg from '@element-plus/icons-vue';
// import local style class name array
import { iconfonntClassList } from '/@/theme/iconfont/font_2298093_rnp72ifj3ba.ts';
import { fontAwesomeClassList } from '/@/theme/font-awesome/font-awesome.ts';

// Get Alibaba font icon
const getAlicdnIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const styles: any = document.styleSheets;
			let sheetsList = [];
			let sheetsIconList = [];
			for (let i = 0; i < styles.length; i++) {
				if (styles[i].href && styles[i].href.indexOf('at.alicdn.com') > -1) {
					sheetsList.push(styles[i]);
				}
			}
			for (let i = 0; i < sheetsList.length; i++) {
				for (let j = 0; j < sheetsList[i].cssRules.length; j++) {
					if (sheetsList[i].cssRules[j].selectorText && sheetsList[i].cssRules[j].selectorText.indexOf('.icon-') > -1) {
						sheetsIconList.push(`${sheetsList[i].cssRules[j].selectorText.substring(1, sheetsList[i].cssRules[j].selectorText.length).replace(/\:\:before/gi, '')}`);
					}
				}
			}
			// Read Alibaba font from local
			if (sheetsIconList.length == 0) {
				for (let i = 0; i < iconfonntClassList.length; i++) {
					sheetsIconList.push(iconfonntClassList[i]);
				}
			}

			if (sheetsIconList.length > 0) resolve(sheetsIconList);
			else reject('No values obtained, please refresh and try again');
		});
	});
};

// Initialize to get CSS styles, get element plus built-in SVG icons, added ele- prefix, usage: ele-Aim
const getElementPlusIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const icons = svg as any;
			const sheetsIconList = [];
			for (const i in icons) {
				sheetsIconList.push(`ele-${icons[i].name}`);
			}
			if (sheetsIconList.length > 0) resolve(sheetsIconList);
			else reject('No values obtained, please refresh and try again');
		});
	});
};

// Initialize to get CSS styles, here using fontawesome icons
const getAwesomeIconfont = () => {
	return new Promise((resolve, reject) => {
		nextTick(() => {
			const styles: any = document.styleSheets;
			let sheetsList = [];
			let sheetsIconList = [];
			for (let i = 0; i < styles.length; i++) {
				if (styles[i].href && styles[i].href.indexOf('netdna.bootstrapcdn.com') > -1) {
					sheetsList.push(styles[i]);
				}
			}
			for (let i = 0; i < sheetsList.length; i++) {
				for (let j = 0; j < sheetsList[i].cssRules.length; j++) {
					if (sheetsList[i].cssRules[j].selectorText && sheetsList[i].cssRules[j].selectorText.indexOf('.fa-') === 0 && sheetsList[i].cssRules[j].selectorText.indexOf(',') === -1) {
						if (/::before/.test(sheetsList[i].cssRules[j].selectorText)) {
							sheetsIconList.push(`${sheetsList[i].cssRules[j].selectorText.substring(1, sheetsList[i].cssRules[j].selectorText.length).replace(/\:\:before/gi, '')}`);
						}
					}
				}
			}
			// Read Alibaba font from local
			if (sheetsIconList.length == 0) {
				for (let i = 0; i < fontAwesomeClassList.length; i++) {
					sheetsIconList.push(fontAwesomeClassList[i]);
				}
			}

			if (sheetsIconList.length > 0) resolve(sheetsIconList.reverse());
			else reject('No values obtained, please refresh and try again');
		});
	});
};

/**
 * Get font icons `document.styleSheets`
 * @method ali Get Alibaba font icon `<i class="iconfont icon-class-name"></i>`
 * @method ele Get element plus built-in icon `<i class="icon-class-name"></i>`
 * @method awe Get fontawesome icon `<i class="fa icon-class-name"></i>`
 */
const initIconfont = {
	// iconfont
	ali: () => {
		return getAlicdnIconfont();
	},
	// element plus
	ele: () => {
		return getElementPlusIconfont();
	},
	// fontawesome
	awe: () => {
		return getAwesomeIconfont();
	},
};

// Export methods
export default initIconfont;
