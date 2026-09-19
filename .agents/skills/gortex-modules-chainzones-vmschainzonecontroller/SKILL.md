---
name: gortex-modules-chainzones-vmschainzonecontroller
description: "Work in the Modules/Chainzones · VMSChainzoneController area — 684 symbols across 2 files (93% cohesion)"
---

# Modules/Chainzones · VMSChainzoneController

684 symbols | 2 files | 93% cohesion

## When to Use

Use this skill when working on files in:
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/VMSChainzoneController.cs`
- `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/VMSChainzoneController.cs`

## Key Files

| File | Symbols |
|------|---------|
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzone.ITS/Modules.Chainzones/Controllers/VMSChainzoneController.cs` | outTemp, color, numFiles, CZ_COLOR_AMBER, HeadTailMoveSpeed, ... |
| `TA-ITS015-WEBAPI-V1.0/src/Modules/Chainzones/Modules.Chainzones/Controllers/VMSChainzoneController.cs` | signIp, FileName, UpdateStatusFunc, sysState, read, ... |

## How to Explore

```
analyze(operation:"communities", id:"community-59")
explore(operation:"context", task:"understand Modules/Chainzones · VMSChainzoneController", format:"gcx")
```

_`format: "gcx"` returns the [GCX1 compact wire format](../../docs/wire-format.md) — round-trippable, ~27% fewer tokens than JSON. Drop it for JSON output; agents using `@gortex/wire` or the Go `github.com/gortexhq/gcx-go` package decode either._
