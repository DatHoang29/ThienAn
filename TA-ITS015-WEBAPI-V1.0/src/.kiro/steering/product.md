# Product Overview

## TAC Web API (TAC_WebAPI)

Backend API for the ITS015 Intelligent Transport System (Hà Nội - Cao Lãnh expressway). Provides RESTful services consumed by the Vue frontend for traffic management and monitoring operations.

### Core Modules

- **TMS (Traffic Management System)** - Equipment management, incidents, traffic data, patrol teams, work scheduling, weather
- **VMS (Variable Message Sign)** - Electronic sign control and content management
- **FMS (Fleet Management System)** - Vehicle fleet tracking and equipment history
- **CCTV** - Camera device integration (NX VMS)
- **CfgSystem** - System configuration management
- **DataTransporter** - Real-time data broadcasting via NATS/SocketCluster
- **Integration** - Third-party VMS and CCTV integrations (Chainzones, NxVms)

### Key Capabilities

- Modular architecture with independently deployable feature modules
- CQRS pattern via Wolverine message bus
- Real-time event broadcasting (NATS, SocketCluster)
- Background job scheduling (Hangfire)
- Multi-level caching (Redis + FusionCache)
- Report generation (FastReport, Carbone)
- Swagger/OpenAPI documentation for frontend code generation
- JWT authentication with role-based authorization
- Localization support (Vietnamese default)
