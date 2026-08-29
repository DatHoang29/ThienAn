---
name: sharedata-worker-datapublication
description: "The ShareData outbound worker — its identity, scope boundary, and what was built on branch feat/20260819/sharedata_worker"
metadata: 
  node_type: memory
  type: project
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-08-27T10:03:54.687Z
---

Branch `feat/20260819/sharedata_worker` in `TA-ITS015-WEBAPI-V1.0`. The ShareData **outbound
publication** worker: query DB per packet config → shape per `ShareDataMapping.TargetShapeJson` →
wrap PDU envelope → **write a file locally**. Spec: `DocBusinessThienAn/HữuNghị-ChiLăng/ShareData/
sharedata_plan.md` (merged 19/08 + 25/08 meeting notes).

Live code layout (`src/Services/ShareDataWorker/`, project untracked/new; `ShareDataWorker.Core`
alongside):
- `Infrastructure/Services/DataPublication/DataPublicationService*.cs` (partial: `.cs`,
  `.Validation`, `.Storage`, `.Logging`, `.Alerting`, `.Expression`, `.Xml`)
- `Infrastructure/Workers/DataPublicationWorker.cs` (5s poll → `ProcessBatchSubscriptions`)
- `Core/Interfaces/IDataPublicationService.cs`

Decisions made in this work (2026-08-27):
- **Renamed** `DataExportService` → `DataPublicationService` (+ Worker, Interface, namespace
  `...Services.DataPublication`, folder). Method names keeping "Export" left as-is.
- **Removed the transport abstraction** (`IPacketTransport` / `FileTransport` / `HttpTransport` +
  `HttpTransportTests`). Writes directly via `SaveExportFileAsync` (`NasStorage:BasePath`, default
  `sharedata/send`, path `Out/{partner}/{yyyyMM}/{ddHH}/{datatypeId}/{file}.{json|xml}`).
- **Feature 1** — `$extend.expression` now evaluated: `ShapeExpressionEvaluator` (hand-rolled
  recursive-descent, 0 deps; funcs CONCAT/ISNULL/COALESCE/NULLIF/UPPER/LOWER/LEN/LTRIM/RTRIM/ROUND/
  ABS; `+` numeric-only). Static/runtime failure → alert **ESH-1203** (repurposed meaning) + keep
  raw value.
- **Feature 2** — XML output for `ShareDataPartner.ProtocolProfile == XmlA`: `PayloadXmlSerializer`
  → `.xml` envelope `<pdu><header/><payload><record/></payload></pdu>`; hash over XML bytes.
  `ProtocolProfile.Asn` stays JSON + a log line. B102 protobuf/ASN.1 out of scope (`????` in spec).

**Scope boundary (explicit):** outbound → local file ONLY. NO HTTP transport, NO inbound/receive
pipeline, NO `$each` support yet (see [sharedata-outbound-pending-fixes](sharedata-outbound-pending-fixes)).
Constraint: [do-not-modify-shared-sharedata-entities](do-not-modify-shared-sharedata-entities).
