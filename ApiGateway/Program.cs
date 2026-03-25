using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var jwtSecret   = builder.Configuration["JWT_SECRET"]   ?? throw new InvalidOperationException("JWT_SECRET not set");
var jwtIssuer   = builder.Configuration["JWT_ISSUER"]   ?? "kursovaya";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? "kursovaya";
var authUrl     = builder.Configuration["AUTH_SERVICE_URL"] ?? "http://localhost:5001";
var userUrl     = builder.Configuration["USER_SERVICE_URL"] ?? "http://localhost:5002";

// JWT authentication (gateway validates token before proxying)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Named HttpClients for each downstream service
builder.Services.AddHttpClient("auth",  c => c.BaseAddress = new Uri(authUrl + "/"));
builder.Services.AddHttpClient("users", c => c.BaseAddress = new Uri(userUrl + "/"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
