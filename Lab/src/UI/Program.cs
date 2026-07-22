/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Application.Services;
using FluentValidation;

// 1. Khởi tạo WebApplicationBuilder và kích hoạt Furion Inject Engine (Mục 2.7.3.1)
var builder = WebApplication.CreateBuilder(args).Inject();

// 2. Tích hợp Wolverine làm Command Bus / Mediator trung gian cho dự án (UseWolverine)
builder.Host.UseWolverine();

// 3. Đăng ký Controllers và kích hoạt AddInject() (Mục 2.7.3.1)
builder.Services.AddControllers().AddInject();

// 4. Đăng ký Mvc Filters chuẩn 100% cú pháp Furion (Mục 5.3.5.1)
builder.Services.AddMvcFilter<cls_GlobalExceptionFilter>();
builder.Services.AddMvcFilter<cls_ResourceFilter>();
builder.Services.AddMvcFilter<cls_ResponseHeaderResultFilter>();

// 5. Đăng ký và Cấu hình Mapster Scan từ tầng Application Assembly
var objAppAssembly = typeof(Icls_UserService).Assembly;
TypeAdapterConfig.GlobalSettings.Scan(objAppAssembly);
builder.Services.AddMapster();

// 6. Đăng ký toàn bộ FluentValidation Validators từ tầng Application Assembly
builder.Services.AddValidatorsFromAssembly(objAppAssembly);

// 7. Khởi tạo ứng dụng kết hợp UseDefaultServiceProvider() (Mục 2.7.3.1)
var app = builder.Build().UseDefaultServiceProvider();

app.UseHttpsRedirection();

app.UseAuthorization();

// 8. Kích hoạt Swagger UI & Specification Document tích hợp sẵn của Furion tại đường dẫn /swagger
app.UseInject("swagger");

// 9. Tự động chuyển hướng từ trang gốc / sang trang /swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapControllers();

app.Run();
