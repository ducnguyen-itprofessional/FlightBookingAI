
using FlightBookingAI.Web.Models;
using FlightBookingAI.Web.ViewModels;
using System;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Cryptography;

namespace FlightBookingAI.Web.Controllers
{
    public class AccountController : Controller
    {
        private FlightBookingAIEntities db =
            new FlightBookingAIEntities();

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLowerInvariant();

            if (db.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("Email",
                    "Email này đã được đăng ký.");
                return View(model);
            }

            var user = new User
            {
                FullName = model.FullName.Trim(),
                Email = email,
                Phone = model.Phone,
                PasswordHash = HashPassword(model.Password),
                Role = "Customer",
                CreatedAt = DateTime.Now
            };

            db.Users.Add(user);
            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Đăng ký thành công! Vui lòng đăng nhập.";

            return RedirectToAction("Login");
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
                return View(model);

            string email = model.Email.Trim().ToLowerInvariant();

            var user = db.Users
                .FirstOrDefault(u => u.Email == email);

            if (user == null ||
                !VerifyPassword(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("",
                    "Email hoặc mật khẩu không chính xác.");

                return View(model);
            }

            FormsAuthentication.SetAuthCookie(user.Email, model.RememberMe);

            // Chỉ chuyển hướng đến đường dẫn nội bộ hợp lệ
            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Đăng nhập thông thường thì về trang chủ
            return RedirectToAction("Index", "Home");
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        // GET: Account/Welcome
        [HttpGet]
        public ActionResult Welcome()
        {
            return View();
        }

        // Băm mật khẩu bằng PBKDF2
        private static string HashPassword(string password)
        {
            byte[] salt = new byte[16];

            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            const int iterations = 100000;

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                return "PBKDF2-SHA256$" +
                    iterations + "$" +
                    Convert.ToBase64String(salt) + "$" +
                    Convert.ToBase64String(hash);
            }
        }

        // Kiểm tra mật khẩu
        private static bool VerifyPassword(
            string password, string storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
                return false;

            string[] parts = storedHash.Split('$');

            if (parts.Length != 4 ||
                parts[0] != "PBKDF2-SHA256")
                return false;

            int iterations;

            if (!int.TryParse(parts[1], out iterations) ||
                iterations < 100000 || iterations > 1000000)
                return false;

            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);

                if (salt.Length != 16 || expected.Length != 32)
                    return false;

                using (var pbkdf2 = new Rfc2898DeriveBytes(
                    password, salt, iterations, HashAlgorithmName.SHA256))
                {
                    byte[] actual = pbkdf2.GetBytes(expected.Length);

                    int difference = 0;

                    for (int i = 0; i < actual.Length; i++)
                    {
                        difference |= actual[i] ^ expected[i];
                    }

                    return difference == 0;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                db.Dispose();

            base.Dispose(disposing);
        }
    }
}
