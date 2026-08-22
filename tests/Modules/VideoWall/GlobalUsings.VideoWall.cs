// ═══════════════════════════════════════════════════════════════════════════════════════════
// Global usings RIÊNG của phân hệ VideoWall.
// File đặt trong tests/Modules/VideoWall/ để cùng bị loại khỏi biên dịch với các file test
// của phân hệ khi Module.VideoWall không còn tồn tại trong repo (xem test.csproj:
// HasVideoWallModule -> Compile Remove="Modules\VideoWall\**\*.cs").
// Đặt ở GlobalUsings.cs gốc sẽ phá build của cả project test khi module bị xoá.
// ═══════════════════════════════════════════════════════════════════════════════════════════

// ─── Entities / Interfaces / Services của Module.VideoWall ───
global using Module.VideoWall.Core.Entities;
global using Module.VideoWall.Core.Interfaces;
global using Module.VideoWall.Infrastructure.Services.Access;
global using Module.VideoWall.Infrastructure.Services.Scene;

// ─── DTO theo từng nhóm nghiệp vụ ───
global using Module.VideoWall.Core.Dto.Controller;
global using Module.VideoWall.Core.Dto.EventRule;
global using Module.VideoWall.Core.Dto.ISAPI;
global using Module.VideoWall.Core.Dto.Scene;
global using Module.VideoWall.Core.Dto.Schedule;
global using Module.VideoWall.Core.Dto.Screen;
global using Module.VideoWall.Core.Dto.SlotPort;
global using Module.VideoWall.Core.Dto.Source;
global using Module.VideoWall.Core.Dto.WindowScene;

// ─── Validators (FluentValidation) dùng cho Negative Test First ───
global using Module.VideoWall.Controllers.Controller.Validators;
global using Module.VideoWall.Controllers.EventRule.Validators;
global using Module.VideoWall.Controllers.Scene.Validators;
global using Module.VideoWall.Controllers.Schedule.Validators;
global using Module.VideoWall.Controllers.Screen.Validators;
global using Module.VideoWall.Controllers.SlotPort.Validators;
global using Module.VideoWall.Controllers.Source.Validators;
global using Module.VideoWall.Controllers.WindowScene.Validators;

// ─── Mock Server giả lập thiết bị Hikvision (nằm ở namespace con nên phải khai báo tường minh) ───
global using Tests.Modules.VideoWall.MockServer;
