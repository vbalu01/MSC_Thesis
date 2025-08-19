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

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(string email, string password, string repeatpassword, string fname, string lname)
        {
            if (await _mysql.Users.AnyAsync(u => u.Email == email))
                return BadRequest("Email already exists");
            if(password != repeatpassword)
                return BadRequest("Passwords not matching");

            var salt = PasswordManager.GenerateSalt();
            var hash = PasswordManager.HashPassword(password, salt);
            password = PasswordManager.CombineSaltHash(hash, salt);

            User u = new()
            {
                Email = email,
                Password = password,
                FName = fname,
                LName = lname
            };

            

            _mysql.Users.Add(u);
            await _mysql.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(string email, string password, bool useCookie = false)
        {
            var user = await _mysql.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) 
                return Unauthorized("Invalid credentials");

            if (!PasswordManager.VerifyPassword(password, user.Password))
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

                return RedirectToAction("Index", "Home");
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

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
