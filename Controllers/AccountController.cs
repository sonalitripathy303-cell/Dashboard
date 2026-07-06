using LoginToDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace LoginToDashboard.Controllers
{
    public class AccountController : Controller
    {
        private readonly string _connString;
        private readonly IConfiguration _configuration;

        // Configuration context gracefully injected via framework constructor engine
        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = _configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool isValidUser = false;

            // Pure ADO.NET Block running custom database execution pipeline
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_ValidateLogin", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Username", model.Username);
                    cmd.Parameters.AddWithValue("@Password", model.Password);

                    try
                    {
                        conn.Open();
                        int result = Convert.ToInt32(cmd.ExecuteScalar());
                        if (result == 1)
                        {
                            isValidUser = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Database connectivity error: " + ex.Message);
                        return View(model);
                    }
                }
            }

            if (isValidUser)
            {
                // 1. Gather configuration properties from the JWT setting domain parameters
                var jwtSection = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSection["SecretKey"] ?? "FallbackSecretKeyIfMissingFromSettings123!";
                var issuer = jwtSection["Issuer"];
                var audience = jwtSection["Audience"];

                // 2. Set Up Claims Identity Payload
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, model.Username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // 3. Cryptographically Sign Token
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(20),
                    signingCredentials: creds
                );

                string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // 4. Save inside a clean, background HttpOnly cookie framework tracking layer
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.AddMinutes(20)
                };
                Response.Cookies.Append("X-JWT-Token", tokenString, cookieOptions);

                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ModelState.AddModelError("", "Invalid Username or Password.");
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            // Erases token cookie properties completely
            Response.Cookies.Delete("X-JWT-Token");
            return RedirectToAction("Login");
        }
    }
}
