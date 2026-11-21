# VDS registration and automation-image regression

Run from the repository root:

```powershell
dotnet run --project tests/VdsRegistrationImages/VdsRegistrationImages.csproj
```

These checks use an isolated temporary file store and actual media import code. They cover source/stage selection, raw camera B identity, delayed full, exact bytes/MIME, deadline, corrupt files, replay, restart, tenant isolation, pending-scan rotation and legacy-photo preservation. No application DB, camera, NATS or deployment configuration is loaded.

Automation checks cover frozen configuration snapshots, strict age thresholds for each step, old/disabled/invalid policy, manual/non-VDS exclusion, Ready registration copies for pictures 2/3, immediate camera fallback, immutable per-stage choices, restart and registration/stage store separation. They do not execute SQL workflow transitions or a live CCTV/RTSP client.

Share-access checks use fake Windows identity/config loaders and an actual isolated file store. They cover dedicated VdsConfig keys, domain/UPN handling, async identity scope, concurrency, local paths, credential scope and redaction, lookup failures, cancellation, and every store operation plus full import. Matching UNC shares use the dedicated VDS account; independently configured shares retain host access without inheriting credentials or being rejected before IO. Authentication failures there report the scope mismatch and preserve the original exception. Roots come from host/tenant configuration, never an HTTP path or image name. These regression checks do not log on to Windows or connect to SMB; the separately authorized live probe is in `../VdsImageShareProbe`.

Complete implementation before running the checks once as part of the consolidated build/SFC verification. No change to the test project's dependency graph is needed; the pure policy and file decisions live in VDS Core.
