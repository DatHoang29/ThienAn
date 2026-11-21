// Global usings cho toàn bộ Services.Vds.
// Chỉ khai những namespace mà GẦN NHƯ MỌI file đều dùng — thêm vào đây phải cân nhắc,
// vì global using nhiều quá thì đọc một file lẻ không còn biết type đến từ đâu.

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;

// ORM. Mọi repository đều cần.
global using SqlSugar;

// Entity của bảng QuestDB (Modules.VDS.Core). Namespace viết hoa lạ là "Vds" chứ không phải "VDS".
global using Modules.VDS.Core.Entities;

// Cầu dao QuestDB dùng chung với WebAPI. Đi qua ba chỗ: Services.Vds/Shared/Persistence (03/09/2026 →
// Modules.VDS.Core) → Shared.Utility/Resilience (23/09/2026, vì lớp này không có gì của VDS). Type dùng
// tên trần ở nhiều repository nên khai global.
global using Shared.Utility.Resilience;

// Entity Tms* trên SQL Server (Modules.TMS.Core): TmsEquipment cho danh mục camera, TmsIncident…
global using Modules.TMS.Core.Entities;

// Logging: ILogger<T>. Backend là ZLogger, cấu hình ở Configuration/Logging.json.
global using Microsoft.Extensions.Logging;


// Mở internals cho project test: một số lớp gọi HTTP nhận thẳng HttpClient qua constructor internal để
// test bơm handler giả (IncidentAutomationApiClient) — cách đó không cần dựng IHttpClientFactory thật.
// Constructor dành cho DI vẫn là bản nhận IHttpClientFactory.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Services.Vds.Tests")]
