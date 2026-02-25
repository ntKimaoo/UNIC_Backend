
using API.Services;
using BusinessLogic.DTOs;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using Presentation.Authorization;
using System;
using System.Text;

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
                .WithOrigins("http://localhost:5173") // FE origin
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
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
builder.Services.AddScoped<IRecruitmentCampaignRepository, RecruitmentCampaignRepository>();
builder.Services.AddScoped<IRecruitmentCampaignService, RecruitmentCampaignService>();
builder.Services.AddScoped<IClubRepository, ClubRepository>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IClubRoleRepository, ClubRoleRepository>();
builder.Services.AddScoped<IClubRoleService, ClubRoleService>();
builder.Services.AddScoped<IClubMemberRepository, ClubMemberRepository>();
builder.Services.AddScoped<IClubMemberService, ClubMemberService>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<EmailQueueService>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
}); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
// Register authorization handler
builder.Services.AddScoped<IAuthorizationHandler, PolicyAuthorizationHandler>();

// Register dynamic policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();

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

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
