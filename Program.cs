using RouteCardProcess.Middleware;
using RouteCardProcess.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Dapper repository
builder.Services.AddSingleton<DepartmentRepository>();
builder.Services.AddSingleton<LogInRepository>();
builder.Services.AddSingleton<SetUpTransRepository>();
builder.Services.AddSingleton<MachiningRepository>();

var app = builder.Build();

// Use custom exception middleware
app.UseMiddleware<ExceptionMiddleware>();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
