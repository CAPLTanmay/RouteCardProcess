using System.Net.Security;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Middleware;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Model.DTOs.PasswordEncryption;
using RouteCardProcess.Repositories;
using RouteCardProcess.Services;
using static RouteCardProcess.Repositories.SetUpTransRepository;

var builder = WebApplication.CreateBuilder(args);

// Add controllers with custom JSON DateTime converter
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonDateTimeConverter());
    });

// Connection string setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<KblApiConfig>(builder.Configuration.GetSection("KblApi"));
builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("EncryptionSettings"));
builder.Services.AddHttpClient<ISapSyncService, SapSyncService>();
builder.Services.AddHttpClient();

var environment = builder.Environment.EnvironmentName;

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var useKblAuth = builder.Configuration.GetValue<bool>("UseKblAuthAPI");
builder.Services.AddSingleton(new KblAuthConfig { UseKblAuthAPI = useKblAuth });
builder.Configuration["UseKblAuth"] = useKblAuth.ToString();
var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];

// Swagger + JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RouteCardProcess API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid JWT token"
    });

    //  API Key Auth
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key required to access this endpoint",
        Name = "X-API-KEY",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

//  Read multiple origins from configuration
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigin").Get<string[]>();

//  Register CORS with multiple origins
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
               .AllowAnyHeader()
              .AllowAnyMethod()
         .AllowCredentials();
    });
});


// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ILogInRepository, LogInRepository>();
builder.Services.AddScoped<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddScoped<ISetUpTransRepository, SetUpTransRepository>();
builder.Services.AddScoped<IMachiningRepository, MachiningRepository>();
builder.Services.AddScoped<IHelperRepository, HelperRepository>();
builder.Services.AddScoped<IRouteCardReportRepository, RouteCardReportRepository>();
builder.Services.AddScoped<IBreakDownRepository, BreakDownRepository>();
builder.Services.AddScoped<IIdleCodeRepository, IdleCodeRepository>();
builder.Services.AddScoped<IMstWorkCenterRepository, MstWorkCenterRepository>();
builder.Services.AddScoped<IExceptionReasonRepository, ExceptionReasonRepository>();
builder.Services.AddScoped<IStdExceptionRepository, StdExceptionRepository>();
builder.Services.AddScoped<IPauseCodeRepository, PauseCodeRepository>();
builder.Services.AddHttpClient<IValidationRepository, ValidationRepository>();
builder.Services.AddScoped<ISystemLoggerRepository, SystemLoggerRepository>();
builder.Services.AddScoped<IOrderTypeRepository, OrderTypeRepository>();
builder.Services.AddScoped<ILossOrderRepository, LossOrderRepository>();
builder.Services.AddScoped<IBreakdownGroupCodeRepository, BreakdownGroupCodeRepository>();
builder.Services.AddScoped<IBreakdownCodeRepository, BreakdownCodeRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IWeeklyReportRepository, WeeklyReportRepository>();
builder.Services.AddScoped<IManualDataRepository, MaualDataRepository>();

// Services
builder.Services.AddScoped<IPasswordSecurityService, PasswordSecurityService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IKblAuthService, KblAuthService>();
builder.Services.AddHttpClient<IKblAuthService, KblAuthService>();
builder.Services.AddSingleton<IUserMessageService, UserMessageService>();

// JWT Auth
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // First, try to read token from cookie
            if (context.Request.Cookies.ContainsKey("AuthToken"))
            {
                context.Token = context.Request.Cookies["AuthToken"];
            }
            //  Fallback to Authorization header (Swagger / Postman)
            else if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var header = context.Request.Headers["Authorization"].ToString();
                if (header.StartsWith("Bearer "))
                    context.Token = header.Substring(7);
            }
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ClockSkew = TimeSpan.Zero,
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

// Rate Limiting Configuration
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("LoginRateLimit", opt =>
    {
        opt.PermitLimit = 5;                    // 5 attempts
        opt.Window = TimeSpan.FromMinutes(1);  // every 1 min
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

builder.Services.AddAuthorization();

//  Configure strong TLS — works on Linux, safely skipped on Windows
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols =
            System.Security.Authentication.SslProtocols.Tls12 |
            System.Security.Authentication.SslProtocols.Tls13;

        // CipherSuitesPolicy is unsupported on Windows Schannel — guard with OS check
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                httpsOptions.OnAuthenticate = (context, sslOptions) =>
                {
                    sslOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
                    {
                        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256,
                        TlsCipherSuite.TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384,
                        TlsCipherSuite.TLS_AES_128_GCM_SHA256,
                        TlsCipherSuite.TLS_AES_256_GCM_SHA384
                    });
                };
            }
            catch (PlatformNotSupportedException)
            {
                Console.WriteLine("CipherSuitesPolicy not supported on this OS — using system defaults.");
            }
        }
        else
        {
            Console.WriteLine("Cipher suite control handled by Windows Schannel. Use PowerShell/Group Policy to enforce strong ciphers.");
        }
    });
});

var app = builder.Build();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] =
        "geolocation=(), microphone=(), camera=(), payment=(), fullscreen=(self)";

    //  Balanced CSP: secure + allows Swagger + Angular
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " + // needed for Swagger/Angular
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data: blob:; " +
        "font-src 'self' data:; " +
        "connect-src 'self' https://* http://localhost:*; " + // allow API calls from Angular dev
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "object-src 'none';";

    await next();
});


// Global exception wrapper first so it catches everything below
app.UseMiddleware<ExceptionMiddleware>();

// Security hardening
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();

// Routing must run before any middleware that inspects endpoint metadata
app.UseRouting();

// CORS must be between UseRouting and UseAuthorization
app.UseCors("DefaultCorsPolicy");

// AuthN: build ClaimsPrincipal from token
app.UseAuthentication();

// Token-level guards (skip for anonymous)
app.UseMiddleware<JwtBlacklistMiddleware>();
app.UseMiddleware<OperatorValidationMiddleware>();

// AuthZ: role/policy checks
app.UseAuthorization();

// Rate limiting (now endpoint metadata is available)
app.UseRateLimiter();

// Swagger, then endpoints
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RouteCardProcess API V1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();

app.Map("/error", (HttpContext context) =>
    Results.Problem("An unexpected error occurred. Please contact your administrator."));

app.Run();
