using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SmartHomeWeb.Services;
using Microsoft.EntityFrameworkCore;
using SmartHomeWeb.Models.DBModels;
using SmartHomeWeb.Lib;

namespace SmartHomeWeb.Controllers
{
    [Route("[controller]")]
    public class AuthController : Controller
    {
        private readonly IConfiguration _config;
        private readonly MySQL _mysql;

        public AuthController(IConfiguration config, MySQL mysql)
        {
            _config = config;
            _mysql = mysql;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User model)
        {
            if (await _mysql.Users.AnyAsync(u => u.Email == model.Email))
                return BadRequest("Email already exists");

            var salt = PasswordManager.GenerateSalt();
            var hash = PasswordManager.HashPassword(model.Password, salt);
            model.Password = PasswordManager.CombineSaltHash(hash, salt);

            _mysql.Users.Add(model);
            await _mysql.SaveChangesAsync();

            return Ok("User registered");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User model, [FromQuery] bool useCookie = false)
        {
            var user = await _mysql.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user is null) 
                return Unauthorized("Invalid credentials");

            if (!PasswordManager.VerifyPassword(model.Password, user.Password))
                return Unauthorized("Invalid credentials");

            if (useCookie)
            {
                // ---------------- Cookie Auth ----------------
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Email),
                    new Claim(ClaimTypes.Name, user.FName),
                    new Claim("lname", user.LName)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                return Ok("Logged in with cookie");
            }
            else
            {
                // ---------------- JWT Auth ----------------
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                    new Claim("fname", user.FName),
                    new Claim("lname", user.LName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: "SmartHome",
                    audience: "SmartHome",
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds
                );

                return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged out");
        }
    }
}
