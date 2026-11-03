using BankApi.Data;
using BankApi.Dtos;
using BankApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly BankDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(BankDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequestDto dto)
    {
        var username = dto.Username.Trim().ToLowerInvariant();

        var usernameTaken = await _context.Users.AnyAsync(u => u.Username == username);

        if (usernameTaken)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Username already taken.",
                detail: "Please choose a different username.");
        }

        var user = new User
        {
            Username = username,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            CreatedAt = user.CreatedAt
        };

        return StatusCode(StatusCodes.Status201Created, userDto);
    }
}