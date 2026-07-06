using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoginToDashboard.Controllers
{
    public class DashboardController : Controller
    {

        public DashboardController(IConfiguration configuration)
        {
            _connString = configuration.GetConnectionString("DefaultConnection");
        }

        [Authorize]
        public IActionResult Index()
        {
            // 2. Since the JWT is valid, the framework automatically extracts the Username 
            // from the token payload and puts it here!
            ViewBag.Username = User.Identity?.Name ?? "Authenticated User";

            return View();
        }
    }
}
