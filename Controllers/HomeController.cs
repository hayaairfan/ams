using Microsoft.AspNetCore.Mvc;
using AttendanceSystem.Business;

namespace AttendanceSystem.Web.Controllers
{
    public class HomeController : Controller // ✅ Must inherit from Controller
    {
        private readonly UserService _userService;

        public HomeController(UserService userService) // ✅ Constructor with DI
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // ✅ If already authenticated, redirect to role-based dashboard
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Index", "Admin");
                else if (User.IsInRole("Teacher"))
                    return RedirectToAction("Index", "Teacher");
                else if (User.IsInRole("Student"))
                    return RedirectToAction("Index", "Student");
                else
                    return RedirectToAction("Index", "Home"); // fallback
            }

            // ❌ Not logged in → show login page
            return View();
        }

        //[HttpPost]
        //public IActionResult Login(string email, string password)
        //{
        //    var user = _userService.Authenticate(email, password);
        //    if (user != null)
        //    {
        //        // Generate JWT token
        //        var token = JwtHelper.GenerateToken(user.Email, user.Role);

        //        // Set in HttpOnly cookie
        //        var cookieOptions = new CookieOptions
        //        {
        //            HttpOnly = true,
        //            Secure = false,
        //            Expires = DateTime.Now.AddMinutes(JwtHelper.ExpireMinutes),
        //            SameSite = SameSiteMode.Strict
        //        };
        //        Response.Cookies.Append("auth_token", token, cookieOptions);

        //        // Redirect based on role (page reload!)
        //        return RedirectToAction("Index", user.Role);
        //    }

        //    // Show error on login page
        //    ViewBag.Error = "Invalid email or password.";
        //    return View();
        //}
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _userService.Authenticate(email, password);
            if (user != null)
            {
                var token = JwtHelper.GenerateToken(user.Email, user.Role);

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    Expires = DateTime.Now.AddMinutes(JwtHelper.ExpireMinutes),
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("auth_token", token, cookieOptions);

                // ✅ NEW: Redirect to change password if required
                if (user.MustChangePassword)
                {
                    return RedirectToAction("ChangePassword", "Home", new { force = true });
                }

                return RedirectToAction("Index", user.Role);
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }
        [HttpGet]
        public IActionResult Logout()
        {
            // ✅ Delete the auth_token cookie
            Response.Cookies.Delete("auth_token");

            // ✅ Redirect to login
            return RedirectToAction("Login", "Home");
        }
        [HttpGet]
        public IActionResult ChangePassword(bool force = false)
        {
            if (Request.Cookies["auth_token"] == null)
                return RedirectToAction("Login");

            if (force)
                ViewBag.Message = "You must change your password on first login.";

            return View();
        }

        [HttpPost]
        public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword, bool force = false)
        {
            var token = Request.Cookies["auth_token"];
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            // ✅ Use YOUR ValidateToken method
            var userClaims = JwtHelper.ValidateToken(token);
            if (userClaims == null)
            {
                ViewBag.Error = "Invalid or expired session.";
                return RedirectToAction("Login");
            }

            var (email, role) = userClaims.Value;

            // Input validation
            if (force && string.IsNullOrEmpty(currentPassword))
            {
                ViewBag.Error = "Current password is required.";
                return View();
            }

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "New password and confirmation are required.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters.";
                return View();
            }

            // Verify current password (if provided)
            if (!force || !string.IsNullOrEmpty(currentPassword))
            {
                if (!_userService.ValidatePassword(email, currentPassword))
                {
                    ViewBag.Error = "Current password is incorrect.";
                    return View();
                }
            }

            // ✅ Update password and clear MustChangePassword flag
            if (_userService.UpdatePassword(email, newPassword))
            {
                // ✅ Optional: Issue a new token (cleaner, but not required)
                var newToken = JwtHelper.GenerateToken(email, role);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    Expires = DateTime.Now.AddMinutes(JwtHelper.ExpireMinutes),
                    SameSite = SameSiteMode.Strict
                };
                Response.Cookies.Append("auth_token", newToken, cookieOptions);

                TempData["Success"] = "Password changed successfully!";
                return RedirectToAction("Index", role);
            }
            else
            {
                ViewBag.Error = "Failed to update password.";
                return View();
            }
        }

        
        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction("Login");
        }
    }
}