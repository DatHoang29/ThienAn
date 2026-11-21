import type { App } from 'vue';
import { authDirective } from '/@/directive/authDirective';
import { wavesDirective, dragDirective, reclickDirective } from '/@/directive/customDirective';

/**
 * Export directive methods: v-xxx
 * @methods authDirective User permission directive, usage: v-auth
 * @methods wavesDirective Button wave directive, usage: v-waves
 * @methods dragDirective Custom drag directive, usage: v-drag
 * @methods reclickDirective Repeated button submit directive, usage: v-reclick
 */
export function directive(app: App) {
	// User permission directive
	authDirective(app);
	// Button wave directive
	wavesDirective(app);
	// // Custom drag directive
	// dragDirective(app);
	// Repeated button submit directive
	reclickDirective(app);
}