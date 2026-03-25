using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthUserService _authUserService;
    private readonly JwtService _jwtService;

    public AuthController(AuthUserService authUserService, JwtService jwtService)
    {
        _authUserService = authUserService;
        _jwtService      = jwtService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _authUserService.UsernameExistsAsync(request.Username))
            return Conflict(new { message = "Username already taken" });

        if (await _authUserService.EmailExistsAsync(request.Email))
            return Conflict(new { message = "Email already registered" });

        var user = new AuthUser
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        await _authUserService.CreateAsync(user);

        if (user.Id is null)
            return StatusCode(500, new { message = "Failed to generate user ID" });

        var token = _jwtService.GenerateToken(user.Id, user.Username, user.Email);
        return CreatedAtAction(nameof(Register), new AuthResponse(token, user.Username, user.Email));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authUserService.GetByUsernameAsync(request.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid username or password" });

        var token = _jwtService.GenerateToken(user.Id!, user.Username, user.Email);
        return Ok(new AuthResponse(token, user.Username, user.Email));
    }
}
