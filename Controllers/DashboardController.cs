using LoginToDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace LoginToDashboard.Controllers
{
    public class DashboardController : Controller
    {
        private readonly string _connString;

        public DashboardController(IConfiguration configuration)
        {
            _connString = configuration.GetConnectionString("DefaultConnection");//f4
        }

        [Authorize]
        public IActionResult Index()
        {
            // 2. Since the JWT is valid, the framework automatically extracts the Username 
            // from the token payload and puts it here!
            ViewBag.Username = User.Identity?.Name ?? "Authenticated User";//user

            return View();
        }

        // GET: Dashboard/Profile
        [HttpGet]
        public IActionResult Profile()
        {
            // Get the username directly from the validated JWT token claims identity
            string currentUsername = User.Identity?.Name;

            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            ProfileModel profile = new ProfileModel();

            // Pure ADO.NET code block to get user profile details
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                string query = "SELECT UserId, Username FROM Users WHERE Username = @Username";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Username", currentUsername);

                    try
                    {
                        conn.Open();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                profile.UserId = Convert.ToInt32(reader["UserId"]);
                                profile.Username = reader["Username"].ToString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewBag.ErrorMessage = "Error loading profile data: " + ex.Message;
                    }
                }
            }

            return View(profile);
        }
    }
}
