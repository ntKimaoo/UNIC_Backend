using API.Services;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Text;
using UNIC.DataAccess.Repositories.Interface;
using UNIC.Presentation.Hubs;

var builder = WebApplication.CreateBuilder(args);

//database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<UnicContext>(options =>
    options.UseSqlServer(connectionString));

//redis
builder.Services.AddStackExchangeRedisCache(redisOptions=>
{
    redisOptions.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFE",
        policy =>
        {
            policy
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
                .SetIsOriginAllowed(_ => true);
        });
});

builder.Services.AddControllers()
    .AddOData(opt => opt
        .Select()
        .Filter()
        .OrderBy()
        .Expand()
        .Count()
        .SetMaxTop(100)
        .AddRouteComponents("api", GetEdmModel()));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IFundRepository, FundRepository>();
builder.Services.AddScoped<IClubFundService, ClubFundService>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<EmailQueueService>();
builder.Services.AddControllers();  
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//signalR
builder.Services.AddSignalR();
//jwt
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Staff"));
});
builder.Services.Configure<AdminSettings>(
    builder.Configuration.GetSection("AdminSettings"));
IEdmModel GetEdmModel()
{
    var odataBuilder = new ODataConventionModelBuilder();
    return odataBuilder.GetEdmModel();
}
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UnicContext>();

    var retry = 0;
    while (!db.Database.CanConnect())
    {
        retry++;
        if (retry > 10)
        {
            throw new Exception("SQL Server is not ready after waiting.");
        }

        Console.WriteLine("Waiting for SQL Server...");
        Thread.Sleep(3000);
    }

    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AllowFE");
app.MapHub<WebRtcHub>("/webrtc");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
