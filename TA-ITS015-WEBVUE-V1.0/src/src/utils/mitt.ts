// https://www.npmjs.com/package/mitt
import mitt, { Emitter } from 'mitt';

// Type
const emitter: Emitter<MittType> = mitt<MittType>();

// Export
export default emitter;
