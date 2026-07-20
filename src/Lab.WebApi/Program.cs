/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using System.Reflection;
using Mapster;
using MapsterMapper;
using Lab.WebApi.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các Controller của ứng dụng Web API
builder.Services.AddControllers();

// 2. Đăng ký lớp Singleton quản lý cơ sở dữ liệu SqlSugar
builder.Services.AddSingleton<cls_SqlSugarDb>();

// 3. Đăng ký và Cấu hình Mapster
TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
builder.Services.AddSingleton(TypeAdapterConfig.GlobalSettings);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// 4. Cấu hình Swagger / OpenAPI để tạo giao diện thử nghiệm API trực quan
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab API v1");
        c.RoutePrefix = "swagger"; // Truy cập Swagger tại http://localhost:<port>/swagger
    });
}

app.UseHttpsRedirection();

// Ánh xạ các Controller API
app.MapControllers();

app.Run();
