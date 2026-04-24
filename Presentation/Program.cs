using BusinessLogic.DTOs;
using BusinessLogic.Hubs;
using BusinessLogic.Options;
using BusinessLogic.Services;
using BusinessLogic.Services.Background;
using BusinessLogic.Services.Implementation;
using BusinessLogic.Services.Interface;
using DataAccess.Context;
using DataAccess.Models;
using DataAccess.Repositories;
using DataAccess.Repositories.Implementation;
using DataAccess.Repositories.Interface;
using DataAccess.Seed;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using DataAccess.Interceptors;
using Presentation.Authorization;
using Presentation.Hubs;
using Presentation.Interceptors;
using System;
using System.Text;
using UNIC.BusinessLogic.Services;
using UNIC.BusinessLogic.Services.Implementation;
using UNIC.BusinessLogic.Services.Interface;
using UNIC.DataAccess.Repositories.Implementation;
using UNIC.DataAccess.Repositories.Interface;
using UNIC.DataAccess.Seed;
using UNIC.Presentation.Helpers;
using UNIC.Presentation.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Audit infrastructure (singleton — shared across all requests)
builder.Services.AddSingleton<AuditChannel>();
builder.Services.AddSingleton<AuditInterceptor>();

//database
builder.Services.AddDbContext<UnicContext>((sp, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
});
builder.Services.AddDbContext<MeetingDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("MeetingRoomConnection"));
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
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRecruitmentCampaignRepository, RecruitmentCampaignRepository>();
builder.Services.AddScoped<IRecruitmentCampaignService, RecruitmentCampaignService>();
builder.Services.AddScoped<IClubPostRepository, ClubPostRepository>();
builder.Services.AddScoped<IClubPostService, ClubPostService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IApplicationService, ApplicationService>();
builder.Services.AddScoped<IApplicationRepository, ApplicationRepository>();
builder.Services.AddScoped<IInterviewRepository, InterviewRepository>();
builder.Services.AddScoped<IInterviewService, InterviewService>();
builder.Services.AddScoped<IClubCreationRequestRepository, ClubCreationRequestRepository>();
builder.Services.AddScoped<IClubCreationRequestService, ClubCreationRequestService>();
builder.Services.AddScoped<IAiAnalysisService,AiAnalysisService>();
// Register Cloudinary as a singleton
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var cloudName = config["Cloudinary:CloudName"];
    var apiKey = config["Cloudinary:ApiKey"];
    var apiSecret = config["Cloudinary:ApiSecret"];
    return new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
});
builder.Services.AddSingleton(sp =>
{
    var account = sp.GetRequiredService<CloudinaryDotNet.Account>();
    return new CloudinaryDotNet.Cloudinary(account);
});

// Register Background Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.Configure<BusinessLogic.Options.PayOSOptions>(builder.Configuration.GetSection(BusinessLogic.Options.PayOSOptions.SectionName));
builder.Services.Configure<BusinessLogic.Options.VnPayOptions>(builder.Configuration.GetSection(BusinessLogic.Options.VnPayOptions.SectionName));
builder.Services.AddHttpClient<IPayOSService, PayOSService>((sp, client) =>
{
    var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<BusinessLogic.Options.PayOSOptions>>().Value;
    client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
});
builder.Services.AddScoped<BusinessLogic.Services.Implementation.PaymentGateways.PayOSFundPaymentGateway>();
builder.Services.AddScoped<BusinessLogic.Services.Implementation.PaymentGateways.VnPayFundPaymentGateway>();
builder.Services.AddScoped<BusinessLogic.Services.Interface.IFundPaymentGatewayRegistry>(sp =>
    new BusinessLogic.Services.Implementation.PaymentGateways.FundPaymentGatewayRegistry(
        new BusinessLogic.Services.Interface.IFundPaymentGateway[]
        {
            sp.GetRequiredService<BusinessLogic.Services.Implementation.PaymentGateways.PayOSFundPaymentGateway>(),
            sp.GetRequiredService<BusinessLogic.Services.Implementation.PaymentGateways.VnPayFundPaymentGateway>()
        }));
builder.Services.AddScoped<IFundRepository, FundRepository>();
builder.Services.AddScoped<DataAccess.Repositories.Interface.IFundTypeRepository, DataAccess.Repositories.Implementation.FundTypeRepository>();
builder.Services.AddScoped<IClubPayOSSettingsRepository, ClubPayOSSettingsRepository>();
builder.Services.AddScoped<IClubFundService, ClubFundService>();
builder.Services.AddScoped<IClubRepository, ClubRepository>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IClubRoleRepository, ClubRoleRepository>();
builder.Services.AddScoped<IClubRoleService, ClubRoleService>();
builder.Services.AddScoped<IClubMemberRepository, ClubMemberRepository>();
builder.Services.AddScoped<IClubMemberService, ClubMemberService>();
builder.Services.AddScoped<DataAccess.Repositories.Interface.IReportRepository, DataAccess.Repositories.Implementation.ReportRepository>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IClubPayOSSettingsService, ClubPayOSSettingsService>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddHostedService<RecordOfChangeProcessorService>();
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<EmailQueueService>();
builder.Services.AddHostedService<ImageUploadQueueService>();
builder.Services.AddHostedService<ClubRoleNotificationService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationHubContext, NotificationHubContext>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<BusinessLogic.Services.Background.EventReminderService>();
builder.Services.AddHostedService<BusinessLogic.Services.Background.EventStatusSyncService>();

// Unit of Work and Repositories
builder.Services.AddScoped<DataAccess.Repositories.Interface.IUnitOfWork, DataAccess.Repositories.Implementation.UnitOfWork>();
builder.Services.AddScoped<DataAccess.Repositories.Interface.IEventRepository, DataAccess.Repositories.Implementation.EventRepository>();
builder.Services.AddScoped<DataAccess.Repositories.Interface.IAttendanceRepository, DataAccess.Repositories.Implementation.AttendanceRepository>();
builder.Services.AddScoped<DataAccess.Repositories.Interface.IEventScheduleRepository, DataAccess.Repositories.Implementation.EventScheduleRepository>();

// Business Services
builder.Services.AddScoped<BusinessLogic.Services.Interface.IEventService, BusinessLogic.Services.Implementation.EventService>();
builder.Services.AddScoped<BusinessLogic.Services.Interface.IAttendanceService, BusinessLogic.Services.Implementation.AttendanceService>();
builder.Services.AddScoped<BusinessLogic.Services.Interface.IQRCodeGeneratorService, BusinessLogic.Services.Implementation.QRCodeGeneratorService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<BusinessLogic.Validators.CreateEventRequestValidator>();
builder.Services.Configure<OpenRouterOptions>(builder.Configuration.GetSection("OpenRouter"));
// AutoMapper
builder.Services.AddAutoMapper(typeof(BusinessLogic.Mappings.EventMappingProfile).Assembly);

builder.Services.AddControllers();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    // Luôn serialize DateTime với suffix 'Z' để frontend (UTC+7) parse đúng giờ
    options.JsonSerializerOptions.Converters.Add(new DateTimeUtcJsonConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableDateTimeUtcJsonConverter());
}); ;
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
//signalR
builder.Services.AddSignalR();
//jwt
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key is missing"));
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

builder.Services.AddScoped<IAuthorizationHandler, ClubPolicyOrRoleHandler>();

// Register club-scoped authorization handler
builder.Services.AddScoped<IAuthorizationHandler, ClubMemberAuthorizationHandler>();

// Register event-scoped authorization handler
builder.Services.AddScoped<IAuthorizationHandler, EventPermissionAuthorizationHandler>();

// Register dynamic policy provider
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();

// Configure file upload size limits
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    if (builder.Environment.IsDevelopment())
    {
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    }
});

//builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.None);

IEdmModel GetEdmModel()
{
    var odataBuilder = new ODataConventionModelBuilder();
    return odataBuilder.GetEdmModel();
}
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UnicContext>();
    var meetingDb = scope.ServiceProvider.GetRequiredService<MeetingDbContext>();

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
    meetingDb.Database.Migrate();

    ApplicationDemoSeeder.SeedAsync(db).GetAwaiter().GetResult();

    DatabaseSeeder.SeedData(scope.ServiceProvider);
}

// Configure the HTTP request pipeline.
// Luôn trả JSON khi API lỗi (tránh PARSING_ERROR ở FE - RTK Query cần JSON, không HTML).
app.UseExceptionHandler(appError =>
{
    appError.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
        var ex = feature?.Error;
        await context.Response.WriteAsJsonAsync(new { success = false, message = ex?.Message ?? "Internal server error" });
    });
});

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFE");
app.UseStaticFiles(); // Enable serving files from wwwroot
app.MapHub<WebRtcHub>("/webrtc");
app.MapHub<NotificationHub>("/notifications");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // Tránh 404 khi mở base URL qua ngrok (GET / không có controller).
    app.MapGet("/", () => Results.Redirect("/swagger"));
}

app.Run();
