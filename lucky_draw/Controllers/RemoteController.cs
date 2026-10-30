using lucky_draw.Security;
using lucky_draw.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lucky_draw.Controllers
{
    // Đăng nhập dùng chung tại /Account/Login; mật khẩu Remote chỉ có quyền vào trang này
    [Authorize(Roles = AppRoles.AdminOrRemote)]
    public class RemoteController : Controller
    {
        public IActionResult Index()
        {
            // Thiết bị đã đăng nhập từ trước khi có nhật ký cũng được cấp mã thiết bị
            AuditLogger.EnsureDeviceId(HttpContext);
            return View();
        }
    }
}
