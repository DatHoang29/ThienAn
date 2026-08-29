---
name: do-not-modify-shared-sharedata-entities
description: The ShareData worker must not change shared entities/DTOs; it adapts in code instead
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-08-27T10:03:35.463Z
---

In the ShareData worker, do **not** modify these shared types (or add DB columns for them):
`ShareDataPacket`, `ShareDataTable`, `ShareDataSubscription`, `ShareDataMappingItemDto`,
`ShareDataPartner`, `Shared.DTO/Enums/BaseEnums.cs`.

**Why:** the API team owns those entities and is editing them in parallel; the branch reverted its
earlier entity changes to avoid conflict. The DB schema on `10.10.8.30` also lacks the once-planned
new columns (`FilterMode`, `TopN`, `IncrementalColumn`, `LastId`, `ApplyTopN`…).

**How to apply:** the worker adapts to the existing schema with hardcoded helpers in
`DataPublicationService.Validation.cs` — `ResolveFilterMode`, `ResolveTopN`,
`ResolveIncrementalColumn`, `ResolveIncrementalFallbackColumn` (keyed off `packet.Code` /
`table.TableName`). New behavior goes in `src/Services/ShareDataWorker*` only, never in
`src/Modules/ShareData/**` entities. See [sharedata-worker-datapublication](sharedata-worker-datapublication)
and [sharedata-outbound-pending-fixes](sharedata-outbound-pending-fixes).
