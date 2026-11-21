# Technology Stack

## Build System

**.NET 10** - Target framework with Visual Studio solution (`TAC_WebAPI.sln`)

### Commands (run from `src/` directory)
```bash
dotnet build TAC_WebAPI.sln        # Build entire solution
dotnet run --project TAC_WebAPI    # Run API locally
dotnet publish --project TAC_WebAPI -c Release  # Publish for deployment
Delete-BIN-OBJ-Folders.bat        # Clean all bin/obj directories
```

## Core Framework

- **ASP.NET Core 10** with Startup/Program pattern (not minimal API)
- **Furion 4.9** - Application framework (DI, dynamic API controllers, friendly exceptions, localization)
- **Lamar** - IoC container (replaces default DI)
- **Wolverine 5.x** - Message bus for CQRS (commands/queries dispatched via `MessBus.InvokeAsync`)

## Database & ORM

- **SqlSugar** - Primary ORM with repository pattern (`BaseRepository<T>`)
- **SQL Server** - Primary database
- Soft-delete pattern with diff logging (`InsertWithDiffLogAsync`, `UpdateWithDiffLogAsync`)

## Caching

- **FusionCache** - Multi-level cache with Redis backplane
- **StackExchange.Redis** - Distributed cache backend
- Cache invalidation via `RemoveByPrefixKey` pattern

## Messaging & Real-time

- **NATS.Net** - Real-time event streaming and broadcasting
- **SocketCluster** (ScClient) - WebSocket pub/sub
- **ItsDataTransporter** - Abstraction over messaging channels for device status broadcasting

## Background Jobs

- **Hangfire** with SQL Server storage - Scheduled and recurring tasks
- Dashboard available at `/jobs` endpoint

## Validation & Mapping

- **FluentValidation 12.x** - Request validation in `Validators/` folders
- **Mapster** - Object mapping (via `command.Adapt<T>()`)

## Reporting

- **FastReport** - Report generation (pre-compiled DLLs in `DLLs/`)
- **Carbone** - Template-based document generation
- **Magicodes.IE** - Excel import/export

## HTTP Clients

- **Refit** - Typed HTTP clients for external API integration
- **Flurl** - Fluent URL building and HTTP requests

## Security

- **JWT Bearer** authentication
- **BouncyCastle** - Cryptography utilities
- Key files: `TAKey.key`, `TAKeyData.key`

## Logging

- **NLog** - Structured logging with extensions for .NET logging

## API Documentation

- **Swagger/OpenAPI** via Furion's `AddSpecificationDocuments()`
- Dynamic API controllers auto-discovered from modules
- API versioning via `Microsoft.AspNetCore.Mvc.Versioning`

## Code Style

- C# with nullable enabled and implicit usings
- Global usings defined per module in `GlobalUsings.cs`
- Vietnamese comments in business logic are common
- `[assembly: WolverineModule]` required in each module's GlobalUsings
