using LoginToDashboard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

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

        [HttpGet]
        public IActionResult ItemManagement()
        {
            List<InventoryItem> items = new List<InventoryItem>();

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                string query = "SELECT ItemId, ItemName, InitialQuantity, SoldQuantity, PricePerItem FROM InventoryItems";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            items.Add(new InventoryItem
                            {
                                ItemId = Convert.ToInt32(rdr["ItemId"]),
                                ItemName = rdr["ItemName"].ToString(),
                                InitialQuantity = Convert.ToInt32(rdr["InitialQuantity"]),
                                SoldQuantity = Convert.ToInt32(rdr["SoldQuantity"]),
                                PricePerItem = Convert.ToDecimal(rdr["PricePerItem"])
                            });
                        }
                    }
                }
            }
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddItem(InventoryItem item)
        {
            if (!ModelState.IsValid) return RedirectToAction("ItemManagement");

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                string query = "INSERT INTO InventoryItems (ItemName, InitialQuantity, PricePerItem) VALUES (@Name, @Qty, @Price)";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", item.ItemName);
                    cmd.Parameters.AddWithValue("@Qty", item.InitialQuantity);
                    cmd.Parameters.AddWithValue("@Price", item.PricePerItem);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("ItemManagement");
        }

        // ==========================================
        // FEATURE 3 & 4: SELL ITEMS MANAGEMENT
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SellItem(int itemId, int quantityToSell)
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                string query = "UPDATE InventoryItems SET SoldQuantity = SoldQuantity + @SelQty, SoldDate = GETDATE() WHERE ItemId = @Id AND (InitialQuantity - SoldQuantity) >= @SelQty";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SelQty", quantityToSell);
                    cmd.Parameters.AddWithValue("@Id", itemId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("ItemManagement");
        }

        // ==========================================
        // ADVANCED FEATURE: PRINTABLE SEARCH REPORT
        // ==========================================
        [HttpGet]
        public IActionResult Report(string searchItemName)
        {
            List<ReportRowModel> reportData = new List<ReportRowModel>();
            ViewBag.SearchTerm = searchItemName;

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_GetItemReport", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ItemName", (object)searchItemName ?? DBNull.Value);

                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            reportData.Add(new ReportRowModel
                            {
                                ItemName = rdr["ItemName"].ToString(),
                                SoldDate = rdr["SoldDate"] != DBNull.Value ? Convert.ToDateTime(rdr["SoldDate"]) : null,
                                SoldQuantity = Convert.ToInt32(rdr["SoldQuantity"]),
                                SoldPrice = Convert.ToDecimal(rdr["SoldPrice"]),
                                RemainingQuantity = Convert.ToInt32(rdr["RemainingQuantity"]),
                                RemainingPrice = Convert.ToDecimal(rdr["RemainingPrice"])
                            });
                        }
                    }
                }
            }
            return View(reportData);
        }
    }
}
