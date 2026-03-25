using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Controllers;

/// <summary>
/// Reverse-proxies /auth/** to AuthService and /users/** to UserService.
/// Auth endpoints are public; user endpoints require a valid JWT.
/// </summary>
[ApiController]
public class ProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration     _config;

    public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config            = config;
    }

    // ── Auth (public) ──────────────────────────────────────────────
    [HttpPost("auth/register")]
    [AllowAnonymous]
    public Task<IActionResult> Register() => ForwardAsync("auth", "auth/register");

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public Task<IActionResult> Login() => ForwardAsync("auth", "auth/login");

    // ── Users (protected) ─────────────────────────────────────────
    [HttpGet("users")]
    [Authorize]
    public Task<IActionResult> GetUsers() => ForwardAsync("users", "users");

    [HttpGet("users/{id}")]
    [Authorize]
    public Task<IActionResult> GetUser(string id) => ForwardAsync("users", $"users/{id}");

    [HttpPost("users")]
    [Authorize]
    public Task<IActionResult> CreateUser() => ForwardAsync("users", "users");

    [HttpPut("users/{id}")]
    [Authorize]
    public Task<IActionResult> UpdateUser(string id) => ForwardAsync("users", $"users/{id}");

    [HttpDelete("users/{id}")]
    [Authorize]
    public Task<IActionResult> DeleteUser(string id) => ForwardAsync("users", $"users/{id}");

    // ── Core proxy logic ──────────────────────────────────────────
    private async Task<IActionResult> ForwardAsync(string clientName, string path)
    {
        var client = _httpClientFactory.CreateClient(clientName);

        // Build query string if present
        var uriBuilder = new UriBuilder(client.BaseAddress! + path)
        {
            Query = Request.QueryString.Value ?? string.Empty,
        };

        using var requestMsg = new HttpRequestMessage(new HttpMethod(Request.Method), uriBuilder.Uri);

        // Forward body
        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            requestMsg.Content = new StreamContent(Request.Body);
            if (Request.ContentType is not null)
                requestMsg.Content.Headers.ContentType =
                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(Request.ContentType);
        }

        // Forward Authorization header so downstream services can validate JWT
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMsg.Headers.TryAddWithoutValidation("Authorization", (string?)authHeader);

        try
        {
            var response = await client.SendAsync(requestMsg);
            var body     = await response.Content.ReadAsStringAsync();

            return new ContentResult
            {
                StatusCode  = (int)response.StatusCode,
                Content     = body,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json",
            };
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { message = "Downstream service unavailable", detail = ex.Message });
        }
    }
}
