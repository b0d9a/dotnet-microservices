using AuthService.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddSingleton<AuthUserService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.Run();
