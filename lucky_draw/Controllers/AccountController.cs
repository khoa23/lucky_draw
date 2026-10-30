using System.Security.Claims;
using lucky_draw.Security;
using lucky_draw.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace lucky_draw.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly LoginService _loginService;
        private readonly AuditLogger _audit;
        private readonly ILogger<AccountController> _logger;

        public AccountController(LoginService loginService, AuditLogger audit, ILogger<AccountController> logger)
        {
            _loginService = loginService;
            _audit = audit;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            // Cấp mã thiết bị ngay từ trang đăng nhập để nhật ký nhận ra thiết bị (kể cả khi đăng nhập sai)
            AuditLogger.EnsureDeviceId(HttpContext);
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.AdminNotConfigured = !_loginService.IsAdminConfigured;
            return View();
        }

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login(string? password, string? returnUrl)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var role = _loginService.GetRole(password);
            AuditLogger.EnsureDeviceId(HttpContext);
            if (role == null)
            {
                _logger.LogWarning("Đăng nhập thất bại từ {Ip}", ip);
                await _audit.LogAsync(AuditLogs.Remote, AuditEvents.LoginFailed, HttpContext, role: null);
                ViewBag.ReturnUrl = returnUrl;
                ViewBag.AdminNotConfigured = !_loginService.IsAdminConfigured;
                ViewBag.Error = "Mật khẩu không đúng!";
                return View();
            }

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, role),
                new Claim(ClaimTypes.Role, role)
            }, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            _logger.LogInformation("Đăng nhập thành công với quyền {Role} từ {Ip}", role, ip);
            await _audit.LogAsync(AuditLogs.Remote, AuditEvents.Login, HttpContext, role);

            // Chỉ chuyển hướng về địa chỉ nội bộ (chống open redirect)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return role == AppRoles.Remote ? RedirectToAction("Index", "Remote") : RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
