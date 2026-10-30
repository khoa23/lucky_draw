using System.Security.Cryptography;
using System.Text;

namespace lucky_draw.Security
{
    public static class AppRoles
    {
        public const string Admin = "Admin";   // Toàn quyền: quay số, cài đặt, báo cáo, import
        public const string Remote = "Remote"; // Chỉ dùng Remote để bấm quay
        public const string AdminOrRemote = Admin + "," + Remote;
    }

    // Kiểm tra mật khẩu đăng nhập từ cấu hình (appsettings.json / biến môi trường)
    public class LoginService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginService> _logger;

        public LoginService(IConfiguration configuration, ILogger<LoginService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private string? AdminPassword => _configuration["AdminSettings:Password"];
        private string? RemotePassword => _configuration["RemoteSettings:Password"];

        public bool IsAdminConfigured => !string.IsNullOrEmpty(AdminPassword);

        // Trả về vai trò tương ứng với mật khẩu, null nếu sai
        public string? GetRole(string? password)
        {
            if (string.IsNullOrEmpty(password)) return null;
            if (Matches(password, AdminPassword)) return AppRoles.Admin;
            if (Matches(password, RemotePassword)) return AppRoles.Remote;
            return null;
        }

        public void WarnIfWeakPasswords()
        {
            if (!IsAdminConfigured)
                _logger.LogCritical("Chưa cấu hình AdminSettings:Password => không thể đăng nhập trang quản trị");
            else if (AdminPassword!.Length < 8)
                _logger.LogWarning("Mật khẩu quản trị (AdminSettings:Password) quá ngắn, nên dùng ít nhất 8 ký tự");

            if (!string.IsNullOrEmpty(RemotePassword) && RemotePassword.Length < 6)
                _logger.LogWarning("Mật khẩu Remote (RemoteSettings:Password) quá ngắn, nên dùng ít nhất 6 ký tự");

            if (IsAdminConfigured && AdminPassword == RemotePassword)
                _logger.LogWarning("Mật khẩu quản trị trùng mật khẩu Remote => người dùng Remote sẽ có toàn quyền");
        }

        // So sánh với thời gian cố định để không lộ thông tin qua thời gian phản hồi (timing attack)
        private static bool Matches(string input, string? expected)
        {
            if (string.IsNullOrEmpty(expected)) return false;
            var inputHash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
            return CryptographicOperations.FixedTimeEquals(inputHash, expectedHash);
        }
    }
}
