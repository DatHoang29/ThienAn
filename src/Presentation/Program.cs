/*
 * Copyright © Công Ty Cổ Phần Đầu Tư Công Nghệ Thiên An
 * Website: http://tacorp.vn
 */

using Mapster;
using Application.Services;
using Domain.Repositories;
using Infrastructure.Persistence;
using Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các Controller của ứng dụng Web API
builder.Services.AddControllers();

// 2. Đăng ký tầng Infrastructure (SqlSugar Context & Repositories)
builder.Services.AddSingleton<cls_SqlSugarDb>();
builder.Services.AddScoped<Icls_UserRepository, cls_UserRepository>();
builder.Services.AddScoped<Icls_SplitTestRepository, cls_SplitTestRepository>();

// 3. Đăng ký tầng Application (Services)
builder.Services.AddScoped<Icls_UserService, cls_UserService>();
builder.Services.AddScoped<Icls_SplitTestService, cls_SplitTestService>();

// 4. Đăng ký và Cấu hình Mapster Scan từ tầng Application Assembly
var objAppAssembly = typeof(Icls_UserService).Assembly;
TypeAdapterConfig.GlobalSettings.Scan(objAppAssembly);
builder.Services.AddMapster();

// 5. Cấu hình Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cấu hình HTTP Request Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Lab API v1 (DDD)");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Ánh xạ các Controller API
app.MapControllers();

app.Run();
