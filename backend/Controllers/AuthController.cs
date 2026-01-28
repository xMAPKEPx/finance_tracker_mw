using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using System.Text;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace backend;
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _context;
    
    private string HashPassword(string password)
    {
        // workFactor 12 — норм для начала, можно 10‑12
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    private bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
    
    public AuthController(IConfiguration config, AppDbContext context)
    {
        _config = config;
        _context = context;
    }
    
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        var exists = await _context.Users.AnyAsync(u => u.Login == request.Login);
        if (exists)
            return BadRequest("Пользователь с таким логином уже существует");

        var user = new User
        {
            Login = request.Login,
            Name = request.Name,
            PasswordHash = HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // СРАЗУ генерим JWT
        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse { Token = token });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .SingleOrDefaultAsync(u => u.Login == request.Login);

        if (user == null)
            return Unauthorized("Неверный логин или пароль");
//
        if (!VerifyPassword(request.Password, user.PasswordHash))
            return Unauthorized("Неверный логин или пароль");

        var token = GenerateJwtToken(user);

        return Ok(new LoginResponse { Token = token });
    }

    
    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(
            int.Parse(_config["Jwt:ExpiresMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    } 
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            // Получаем данные из токена
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name;
            var email = User.FindFirstValue(ClaimTypes.Email);
    
            if (userId == null)
                return Unauthorized();
    
            return Ok(new
            {
                id = userId,
                displayName = userName,
                email = email
            });
        }
}
