// Font icon url
const cssCdnUrlList: Array<string> = [
	// Change to import from local, comment out the url below
	// '//at.alicdn.com/t/c/font_2298093_rnp72ifj3ba.css',
	// '//netdna.bootstrapcdn.com/font-awesome/4.7.0/css/font-awesome.min.css',
];
// Third-party js url
const jsCdnUrlList: Array<string> = [];

// Dynamically set font icons
export function setCssCdn() {
	if (cssCdnUrlList.length <= 0) return false;
	cssCdnUrlList.map((v) => {
		let link = document.createElement('link');
		link.rel = 'stylesheet';
		link.href = v;
		link.crossOrigin = 'anonymous';
		document.getElementsByTagName('head')[0].appendChild(link);
	});
}

// Dynamically set third-party js
export function setJsCdn() {
	if (jsCdnUrlList.length <= 0) return false;
	jsCdnUrlList.map((v) => {
		let link = document.createElement('script');
		link.src = v;
		document.body.appendChild(link);
	});
}

/**
 * Batch set font icons and dynamic js
 * @method cssCdn Dynamically set font icons
 * @method jsCdn Dynamically set third-party js
 */
const setIntroduction = {
	// Set css
	cssCdn: () => {
		setCssCdn();
	},
	// Set js
	jsCdn: () => {
		setJsCdn();
	},
};

// Export function methods
export default setIntroduction;
