using System.Security.Claims;
using LinguaFlow.Api.Dtos;
using LinguaFlow.Api.Models;
using LinguaFlow.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LinguaFlow.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<AppUser> users, TokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest req)
    {
        var user = new AppUser { UserName = req.Email, Email = req.Email, DisplayName = req.DisplayName };
        var result = await users.CreateAsync(user, req.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await users.AddToRoleAsync(user, "User");
        return await BuildResponse(user);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest req)
    {
        var wrong = new { message = "Email hoặc mật khẩu không đúng." };

        var user = await users.FindByEmailAsync(req.Email);
        if (user is null) return Unauthorized(wrong);

        if (await users.IsLockedOutAsync(user))
            return StatusCode(423, new { message = "Tài khoản bị khóa tạm thời do nhập sai nhiều lần. Hãy thử lại sau vài phút." });

        if (!await users.CheckPasswordAsync(user, req.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized(wrong);
        }

        await users.ResetAccessFailedCountAsync(user);
        return await BuildResponse(user);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        id = User.FindFirstValue(ClaimTypes.NameIdentifier),
        email = User.FindFirstValue(ClaimTypes.Email),
        displayName = User.FindFirstValue(ClaimTypes.Name),
        roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value)
    });

    private async Task<AuthResponse> BuildResponse(AppUser user)
    {
        var roles = await users.GetRolesAsync(user);
        var (token, expires) = tokens.Create(user, roles);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expires,
            Email = user.Email ?? "",
            DisplayName = user.DisplayName,
            Roles = roles
        };
    }
}