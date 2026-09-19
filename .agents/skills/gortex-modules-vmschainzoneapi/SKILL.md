---
name: gortex-modules-vmschainzoneapi
description: "Work in the Modules · VMSChainzoneApi area — 1011 symbols across 5 files (92% cohesion)"
---

# Modules · VMSChainzoneApi

1011 symbols | 5 files | 92% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/Services/NetworkConnection.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneApi.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneControl.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneApi.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneControl.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/CfgSystem/Modules.CfgSystem/Controllers/Share/Services/NetworkConnection.cs` | DllImport |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneApi.cs` | sign_file_path, Dfont, Version, sendType, ID, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneControl.cs` | fn_ResetSystem, fn_SendImage_API, VMSChainzoneControl.<init>, vczVMSII, port, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneApi.cs` | LineSpace, totalSteps, fn_SendPicture, rtCode, czPlaySoundFile, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/Chainzone/VMSChainzoneControl.cs` | VMSChainzoneControl.<init>, filePath, chooseTypes, fn_ResetSystem, fn_SendImage_API, ... |

## How to Explore

```
analyze(operation:"communities", id:"community-64")
explore(operation:"context", task:"understand Modules · VMSChainzoneApi", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
