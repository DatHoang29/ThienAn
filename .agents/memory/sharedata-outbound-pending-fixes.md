---
name: sharedata-outbound-pending-fixes
description: "Three agreed fixes for the ShareData outbound worker from the 2026-08-27 review; plan drafted, not yet approved/implemented"
metadata: 
  node_type: memory
  type: project
  originSessionId: 8862bd9e-5d13-42a7-8b51-9079f8a21d37
  modified: 2026-08-27T10:04:11.486Z
---

Review of the staged `DataPublicationService` changes vs live data on `10.10.8.30` (via
`mssql_staging`) on 2026-08-27 found 3 items the user chose to fix. Plan drafted at
`C:\Users\This PC\.claude\plans\applying-knowledge-of-parsed-zebra.md` — user rejected ExitPlanMode,
so **not approved / not implemented yet**.

**(a) Packet-code matching is broken.** `ResolveFilterMode` / `ResolveTopN`
(`DataPublicationService.Validation.cs:64,76`) compare `packet.Code == "103"/"106"/"109"` but real
`ShareDataPacket.Code` values are `"103_vdsData"`, `"106_wimData"`, `"109_etcData"` → never match →
**every packet treated as Snapshot**, `TopN` always null (103/106/109 should be incremental per
25/08 "X ≥ datetime"). Fix: extract numeric prefix (`CodePrefixRegex`) and compare the int; also
add an `OrderNo == number` fallback in `ResolveActivePacket` (`DataPublicationService.cs:852`).

**(e) `{"$each": true, "$as": {...}}` not handled.** `RenderShapeNode` treats `$each`/`$as` as
plain keys → garbage payload. UI generates this shape; active mapping "e-test" uses the older
`data:[{template}]` literal-array form instead. Fix: aggregate mode in `Transform` — when the shape
contains `$each`, render the root once, `$each` expands to one `$as`-rendered element per source
row (single envelope, `data[]` = all rows). No-`$each` shapes keep current per-row behavior.

**(guard) Recursion depth guards missing/inconsistent.** Add caps: `UnaryOpNode.TryEvaluate`
(`.Expression.cs:260`) is missing the `MaxRecursionDepth` check its siblings have; add a `_depth`
guard to the `Parser`; add `MaxXmlDepth` to `PayloadXmlSerializer.WriteValue`; add `MaxShapeDepth`
to `RenderShapeNode` + `WalkShapeDoc`.

Also noted, NOT being fixed now: no partner has `ProtocolProfile = XmlA` so the XML branch is
untested with real data (seed task); `"*1000"` in mapping "e-test" is a malformed DSL expression
(handled safely — ESH-1203 + raw value). Verification: `dotnet build
src/Services/ShareDataWorker/ShareDataWorker.csproj` + `dotnet test tests/test.csproj --filter
FullyQualifiedName~DataPublication` (local DB only).
