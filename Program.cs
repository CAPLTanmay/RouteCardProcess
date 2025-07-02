using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RouteCardProcess.Interfaces;
using RouteCardProcess.Middleware;
using RouteCardProcess.Model.Configurations;
using RouteCardProcess.Model.DTOs.Login;
using RouteCardProcess.Repositories;
using RouteCardProcess.Services;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Connection string setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton(new SqlConnectionFactory(connectionString));
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.Configure<KblApiConfig>(builder.Configuration.GetSection("KblApi"));
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
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Repositories
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ILogInRepository, LogInRepository>();
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

// Services
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IKblAuthService, KblAuthService>();
builder.Services.AddHttpClient<IKblAuthService, KblAuthService>();
builder.Services.AddSingleton<IUserMessageService, UserMessageService>();

// JWT Auth
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

// Production-Only HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseMiddleware<ExceptionMiddleware>();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RouteCardProcess API V1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors("DefaultCorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
