
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Smart_Xerox_Centre.Data;
using Smart_Xerox_Centre.Models;
using Smart_Xerox_Centre.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Smart_Xerox_Centre.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthController(
        ApplicationDbContext db,
        IPasswordHasher<User> passwordHasher,
        IConfiguration configuration)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    // REGISTER
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("Name, Email and Password are required.");
        }

        if (request.Password.Length < 8)
            return BadRequest("Password must contain at least 8 characters.");

        var tenantExists = await _db.Tenants
            .AnyAsync(x => x.TenantId == request.TenantId);

        if (!tenantExists)
            return BadRequest("Invalid TenantId.");

        var emailExists = await _db.Users.AnyAsync(x =>
            x.TenantId == request.TenantId &&
            x.Email == request.Email);

        if (emailExists)
            return BadRequest("Email already registered.");

        // Find default ShopOwner role
        var role = await _db.Roles.FirstOrDefaultAsync(x =>
            x.RoleName == "ShopOwner" &&
            x.TenantId == null);

        if (role == null)
            return BadRequest("ShopOwner role not found.");

        var user = new User
        {
            TenantId = request.TenantId,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone,
            IsEmailVerified = false,
            IsPhoneVerified = false,
            Status = "Active",
            CreatedAtUtc = DateTime.UtcNow
        };

        // Secure password hashing
        user.PasswordHash = _passwordHasher.HashPassword(
            user, request.Password);

        _db.Users.Add(user);

        await _db.SaveChangesAsync();

        // Assign ShopOwner role
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "Registration successful.",
            userId = user.UserId,
            tenantId = user.TenantId,
            fullName = user.FullName,
            email = user.Email
        });
    }

    // LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _db.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x =>
                x.TenantId == request.TenantId &&
                x.Email == request.Email);

        if (user == null)
            return Unauthorized("Invalid Email or Password.");

        if (user.Status != "Active")
            return Unauthorized("User account is inactive.");

        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (passwordResult == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid Email or Password.");

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(
                user, request.Password);
        }

        user.LastLoginAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var roles = user.UserRoles
            .Select(x => x.Role.RoleName)
            .ToList();

        var expiresAt = DateTime.UtcNow.AddHours(
            Convert.ToDouble(_configuration["Jwt:ExpiryHours"] ?? "2")
        );

        var token = GenerateJwtToken(user, roles, expiresAt);

        return Ok(new AuthResponse
        {
            UserId = user.UserId,
            TenantId = user.TenantId,
            FullName = user.FullName,
            Email = user.Email ?? "",
            Roles = roles,
            Token = token,
            ExpiresAtUtc = expiresAt
        });
    }

    // GET LOGGED-IN USER
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!long.TryParse(userIdValue, out var userId))
            return Unauthorized();

        var user = await _db.Users
            .Include(x => x.UserRoles)
                .ThenInclude(x => x.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);

        if (user == null)
            return NotFound();

        return Ok(new
        {
            user.UserId,
            user.TenantId,
            user.FullName,
            user.Email,
            Roles = user.UserRoles.Select(x => x.Role.RoleName)
        });
    }

    // JWT TOKEN GENERATION
    private string GenerateJwtToken(
        User user,
        List<string> roles,
        DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
            new Claim("TenantId", user.TenantId.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!)
        );

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256
        );

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
