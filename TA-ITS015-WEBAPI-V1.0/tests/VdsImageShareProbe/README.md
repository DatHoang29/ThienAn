# Live VDS image-share probe

```powershell
dotnet run --project tests/VdsImageShareProbe/VdsImageShareProbe.csproj -- src/Services/VDS/Services.Vds.Wpf
```

Use the directory containing the active host's appsettings.json and Configuration/Database.json. Credentials are read in memory through the existing settings repository; output contains only configuration-presence flags and numeric error codes. No host, source subscriber or recovery pipeline is started and no SQL data is written.

Optional modes: `config-only` reads the root configuration without SMB authentication; `process-identity` tests host access without the legacy account; `configured-state` checks a unique state child under vds_media_storage_directory/IncidentImageState. DOTNET_ENVIRONMENT JSON overrides and environment variables are loaded; persisted runtime service overrides are not applied. Output includes configured image paths and UNC shares, never username/password/connection strings.

The probe creates a unique `probe-<GUID>` child under configured StateDirectory, calls the actual registration store for lock/register/read/save, then removes only that child. It does not access another incident's state, locks, media or pending frames. Run only when live SQL/SMB access has been authorized.
