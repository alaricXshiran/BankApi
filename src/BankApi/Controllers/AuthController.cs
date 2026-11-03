using BankApi.Data;
using BankApi.Dtos;
using BankApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankApi.Services;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
namespace BankApi.Controllers;

[ApiController]
[Route("api/[controller]")]



public class AuthController : ControllerBase
{
    private readonly BankDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    // Creates the JWT after a successful login.
    private readonly TokenService _tokenService;

    public AuthController(
        BankDbContext context,
        IPasswordHasher<User> passwordHasher,
        TokenService tokenService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }
    // POST: api/auth/register
    [HttpPost("register")]
    // Only logged-in staff can create new staff users
    [Authorize]
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
    // POST: api/auth/login
    // Checks the username and password, and returns a JWT if both are correct.
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginRequestDto dto)
    {
        // Normalize the same way as at registration, so "Alaric" finds "alaric".
        var username = dto.Username.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
        {
            return InvalidLogin();
        }

        // Hash what the user typed and compare it with the stored hash.
        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return InvalidLogin();
        }

        // Username and password are correct, so issue a token.
        return Ok(_tokenService.CreateToken(user));
    }

    // One shared response for every failed login.
    
    private ObjectResult InvalidLogin()
    {
        return Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Invalid username or password.");
    }
}