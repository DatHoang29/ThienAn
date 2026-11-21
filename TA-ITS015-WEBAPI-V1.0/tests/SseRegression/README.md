# SSE regression checks

Run from the repository root with the .NET 10 SDK:

```powershell
dotnet run --project tests/SseRegression/SseRegression.csproj
```

The console exits nonzero if a check fails. Checks cover tenant authorization,
shared subscriber cleanup, expected cancellation without first-chance exceptions,
real error propagation, independent heartbeats, and terminal frame completion.
HTTP checks start an isolated loopback server and use synthetic data; no database
or production configuration is loaded.
