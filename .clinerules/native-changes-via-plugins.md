## Brief overview
- Project-specific rule for Expo React Native projects with generated native folders.
- Native Android/iOS changes should be reproducible through Expo config plugins instead of manual edits whenever possible.

## Native folder change policy
- If a change is needed inside the `android/` or `ios/` folders, prefer writing or updating an Expo config plugin under the project `plugins/` directory.
- Avoid one-off manual edits in generated native files because `expo prebuild --clean` can delete or overwrite them.
- Manual edits are acceptable only for temporary diagnosis or for files that cannot reasonably be managed by Expo config plugins; document those exceptions clearly.

## Plugin implementation guidance
- Keep config plugins idempotent: running prebuild multiple times must not duplicate manifest entries, Gradle lines, imports, permissions, or native code blocks.
- Use platform-specific mods where appropriate:
  - Android: `withAndroidManifest`, `withProjectBuildGradle`, `withAppBuildGradle`, `withDangerousMod`.
  - iOS: `withInfoPlist`, `withEntitlementsPlist`, `withAppDelegate`, `withPodfile`, `withDangerousMod`.
- Register custom plugins in `app.json` / `app.config.js` so native changes are reapplied automatically during prebuild.

## Gradle and build troubleshooting
- When Android build fails, identify the first real Gradle error above the generic `BUILD FAILED` / `exited with non-zero code: 1` message.
- Prioritize errors containing `FAILURE`, `Caused by`, `Execution failed for task`, `Could not resolve`, `compileSdkVersion`, `Manifest merger failed`, or Android Gradle Plugin version checks.
- For Expo SDK 54 / React Native 0.81.x Android builds, verify Gradle wrapper compatibility before changing app code or dependencies.

## Communication style
- Explain the root cause directly and in Vietnamese when the user asks Vietnamese questions.
- Provide exact file paths, full corrected snippets, and clean rebuild commands when fixing native build issues.