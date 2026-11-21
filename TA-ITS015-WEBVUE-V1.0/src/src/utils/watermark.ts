// Add watermark effect to the page
const setWatermark = (str: string) => {
	const id = '1.23452384164.123412416';
	if (document.getElementById(id) !== null) document.body.removeChild(<HTMLElement>document.getElementById(id));
	const can = document.createElement('canvas');
	can.width = 400;
	can.height = 200;
	const cans = <CanvasRenderingContext2D>can.getContext('2d');
	cans.rotate((-20 * Math.PI) / 180);
	cans.font = '12px Vedana';
	cans.fillStyle = 'rgba(200, 200, 200, 0.30)';
	cans.textBaseline = 'middle';
	cans.fillText(str, can.width / 10, can.height, can.width);
	const div = document.createElement('div');
	div.id = id;
	div.style.pointerEvents = 'none';
	div.style.top = '0px';
	div.style.left = '0px';
	div.style.position = 'fixed';
	div.style.zIndex = '10000000';
	div.style.width = `${document.documentElement.clientWidth}px`;
	div.style.height = `${document.documentElement.clientHeight}px`;
	div.style.background = `url(${can.toDataURL('image/png')}) left top repeat`;
	document.body.appendChild(div);
	return id;
};

/**
 * Add watermark effect to the page
 * @method set Set watermark
 * @method del Remove watermark
 */
const watermark = {
	// Set watermark
	set: (str: string) => {
		let id = setWatermark(str);
		if (document.getElementById(id) === null) id = setWatermark(str);
	},
	// Remove watermark
	del: () => {
		let id = '1.23452384164.123412416';
		if (document.getElementById(id) !== null) document.body.removeChild(<HTMLElement>document.getElementById(id));
	},
};

// Export methods
export default watermark;
