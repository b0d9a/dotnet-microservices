using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserProfileService _service;

    public UsersController(UserProfileService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _service.GetAllAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _service.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        var profile = new UserProfile
        {
            UserId   = request.UserId,
            Username = request.Username,
            Email    = request.Email,
            FullName = request.FullName,
            Role     = request.Role,
        };

        var created = await _service.CreateAsync(profile);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
    {
        var existing = await _service.GetByIdAsync(id);
        if (existing is null) return NotFound();

        var updates = new List<UpdateDefinition<UserProfile>>();
        if (request.FullName is not null)
            updates.Add(Builders<UserProfile>.Update.Set(u => u.FullName, request.FullName));
        if (request.Email is not null)
            updates.Add(Builders<UserProfile>.Update.Set(u => u.Email, request.Email));
        if (request.Role is not null)
            updates.Add(Builders<UserProfile>.Update.Set(u => u.Role, request.Role));

        if (updates.Count == 0) return BadRequest(new { message = "No fields to update" });

        updates.Add(Builders<UserProfile>.Update.Set(u => u.UpdatedAt, DateTime.UtcNow));

        var combined = Builders<UserProfile>.Update.Combine(updates);
        var updated  = await _service.UpdateAsync(id, combined);

        return updated ? NoContent() : StatusCode(500);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
