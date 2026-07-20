## Brief overview
Guidelines for managing Expo configuration plugins in the AICC Mobile project.

## Development workflow
- Keep Expo config plugins as simple as possible.
- Avoid splitting single-purpose logic into multiple files; consolidate small logic into the main `index.js` of the plugin directory.
- Avoid using separate files like `withAuthFlag.ts` for simple flag injections; implement such logic directly inside the main `index.js` file of the corresponding platform plugin.
- Maintain all plugin files in pure JavaScript (CommonJS) to avoid `ts-node` or `tsx` compilation issues during `expo prebuild`.

## Coding best practices
- Use native Expo `ConfigPlugin` patterns (e.g., `withDangerousMod`, `withPodfile`).
- Ensure plugins are idempotent.
- Keep implementation logic concise and direct.