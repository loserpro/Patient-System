using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Patient_System.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. 添加跨域服务
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        // 允许前端地址访问（前端默认是8080端口）
        policy.WithOrigins("http://localhost:8086")
              .AllowAnyHeader() // 允许所有请求头（包括Token）
              .AllowAnyMethod() // 允许所有请求方法（GET/POST/PUT/DELETE）
              .AllowCredentials(); // 允许携带Cookie/Token
    });
});



// Add services to the container.

builder.Services.AddControllers();//添加控制器（Web API 必备）

// 基础注册（推荐开发环境）
builder.Services.AddDbContext<AppDbContext>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 配置JWT鉴权
var jwtKey = builder.Configuration["Jwt:Key"] ?? "your_secret_key_123456"; // 配置在appsettings.json
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "your_issuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "your_audience";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 3. 启用跨域（必须在 UseRouting 之后，UseAuthorization 之前）
app.UseCors("CorsPolicy");

// 启用鉴权（在UseAuthorization之前）
app.UseAuthentication();
app.UseAuthorization();

// 强制HTTPS（如果前端用HTTP，可注释这行）
//app.UseHttpsRedirection();


app.MapControllers();

app.Run();
