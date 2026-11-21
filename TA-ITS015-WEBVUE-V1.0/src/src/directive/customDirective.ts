import type { App } from 'vue';

/**
 * Button wave directive
 * @directive Default usage: v-waves, e.g. `<div v-waves></div>`
 * @directive With parameter: v-waves=" |light|red|orange|purple|green|teal", e.g. `<div v-waves="'light'"></div>`
 */
export function wavesDirective(app: App) {
	app.directive('waves', {
		mounted(el, binding) {
			el.classList.add('waves-effect');
			binding.value && el.classList.add(`waves-${binding.value}`);
			function setConvertStyle(obj: { [key: string]: unknown }) {
				let style: string = '';
				for (let i in obj) {
					if (obj.hasOwnProperty(i)) style += `${i}:${obj[i]};`;
				}
				return style;
			}
			function onCurrentClick(e: MouseEvent) {
				let elDiv = document.createElement('div');
				elDiv.classList.add('waves-ripple');
				el.appendChild(elDiv);
				// Toạ độ điểm nhấn tính theo chính phần tử gắn v-waves (không dùng layerX/Y vì nó tính theo
				// phần tử con có position, làm gợn sóng lệch khi bấm vào chữ/icon bên trong nút)
				const rect = el.getBoundingClientRect();
				const scaleX = el.offsetWidth ? rect.width / el.offsetWidth : 1;
				const scaleY = el.offsetHeight ? rect.height / el.offsetHeight : 1;
				let styles = {
					left: `${(e.clientX - rect.left) / scaleX - el.clientLeft}px`,
					top: `${(e.clientY - rect.top) / scaleY - el.clientTop}px`,
					opacity: 1,
					transform: `scale(${(el.clientWidth / 100) * 10})`,
					'transition-duration': `750ms`,
					'transition-timing-function': `cubic-bezier(0.250, 0.460, 0.450, 0.940)`,
				};
				elDiv.setAttribute('style', setConvertStyle(styles));
				setTimeout(() => {
					elDiv.setAttribute(
						'style',
						setConvertStyle({
							opacity: 0,
							transform: styles.transform,
							left: styles.left,
							top: styles.top,
						})
					);
					setTimeout(() => {
						elDiv && el.removeChild(elDiv);
					}, 750);
				}, 450);
			}
			el.addEventListener('mousedown', onCurrentClick, false);
		},
		unmounted(el) {
			el.addEventListener('mousedown', () => { });
		},
	});
}

/**
 * Custom drag directive
 * @description Usage: v-drag="[dragDom,dragHeader]", e.g. `<div v-drag="['.drag-container .el-dialog', '.drag-container .el-dialog__header']"></div>`
 * @description dragDom is the element to be dragged, dragHeader is the position of the header to be dragged
 * @link Note: https://github.com/element-plus/element-plus/issues/522
 * @link Reference: https://blog.csdn.net/weixin_46391323/article/details/105228020?utm_medium=distribute.pc_relevant.none-task-blog-baidujs_title-10&spm=1001.2101.3001.4242
 */
export function dragDirective(app: App) {
	app.directive('drag', {
		mounted(el, binding) {
			if (!binding.value) return false;

			const dragDom = document.querySelector(binding.value[0]) as HTMLElement;
			const dragHeader = document.querySelector(binding.value[1]) as HTMLElement;

			dragHeader.onmouseover = () => (dragHeader.style.cursor = `move`);

			function down(e: any, type: string) {
				// Mouse down, calculate the distance of the current element from the visible area
				const disX = type === 'pc' ? e.clientX - dragHeader.offsetLeft : e.touches[0].clientX - dragHeader.offsetLeft;
				const disY = type === 'pc' ? e.clientY - dragHeader.offsetTop : e.touches[0].clientY - dragHeader.offsetTop;

				// Current body width
				const screenWidth = document.body.clientWidth;
				// Visible area height (should be body height, but cannot be obtained in some environments)
				const screenHeight = document.documentElement.clientHeight;

				// Dialog width
				const dragDomWidth = dragDom.offsetWidth;
				// Dialog height
				const dragDomheight = dragDom.offsetHeight;

				const minDragDomLeft = dragDom.offsetLeft;
				const maxDragDomLeft = screenWidth - dragDom.offsetLeft - dragDomWidth;

				const minDragDomTop = dragDom.offsetTop;
				const maxDragDomTop = screenHeight - dragDom.offsetTop - dragDomheight;

				// The obtained value has px, replace with regular expression
				let styL: any = getComputedStyle(dragDom).left;
				let styT: any = getComputedStyle(dragDom).top;

				// Note that in IE, the first obtained value is the component's default 50%, after moving it is assigned as px
				if (styL.includes('%')) {
					styL = +document.body.clientWidth * (+styL.replace(/\%/g, '') / 100);
					styT = +document.body.clientHeight * (+styT.replace(/\%/g, '') / 100);
				} else {
					styL = +styL.replace(/\px/g, '');
					styT = +styT.replace(/\px/g, '');
				}

				return {
					disX,
					disY,
					minDragDomLeft,
					maxDragDomLeft,
					minDragDomTop,
					maxDragDomTop,
					styL,
					styT,
				};
			}

			function move(e: any, type: string, obj: any) {
				let { disX, disY, minDragDomLeft, maxDragDomLeft, minDragDomTop, maxDragDomTop, styL, styT } = obj;

				// Calculate the moving distance through event delegation
				let left = type === 'pc' ? e.clientX - disX : e.touches[0].clientX - disX;
				let top = type === 'pc' ? e.clientY - disY : e.touches[0].clientY - disY;

				// Boundary processing
				if (-left > minDragDomLeft) {
					left = -minDragDomLeft;
				} else if (left > maxDragDomLeft) {
					left = maxDragDomLeft;
				}

				if (-top > minDragDomTop) {
					top = -minDragDomTop;
				} else if (top > maxDragDomTop) {
					top = maxDragDomTop;
				}

				// Move the current element
				dragDom.style.cssText += `;left:${left + styL}px;top:${top + styT}px;`;
			}

			/**
			 * PC
			 * onmousedown triggers when mouse is pressed
			 * onmousemove continuously triggers when mouse is pressed
			 * onmouseup triggers when mouse is released
			 */
			dragHeader.onmousedown = (e) => {
				const obj = down(e, 'pc');
				document.onmousemove = (e) => {
					move(e, 'pc', obj);
				};
				document.onmouseup = () => {
					document.onmousemove = null;
					document.onmouseup = null;
				};
			};

			/**
			 * Mobile
			 * ontouchstart triggers when finger is pressed
			 * ontouchmove triggers when finger is moved
			 * ontouchend triggers when finger is lifted
			 */
			dragHeader.ontouchstart = (e) => {
				const obj = down(e, 'app');
				document.ontouchmove = (e) => {
					move(e, 'app', obj);
				};
				document.ontouchend = () => {
					document.ontouchmove = null;
					document.ontouchend = null;
				};
			};
		},
	});
}

/**
 * Prevent repeated clicks on submit button
 * @directive Default usage: v-reclick, e.g. `<el-button v-reclick></el-button>`
 * @directive With parameter: v-reclick="number", e.g. `<el-button v-reclick="500"></el-button>`
 */
export function reclickDirective(app: App) {
	app.directive('reclick', {
		mounted(el, time) {
			el.addEventListener('click', () => {
				if (!el.disabled) {
					el.disabled = true;
					setTimeout(
						() => {
							el.disabled = false;
						},
						time.value === undefined ? 500 : time.value
					);
				}
			});
		},
		unmounted(el) {
			el.disabled = false;
		},
	});
}