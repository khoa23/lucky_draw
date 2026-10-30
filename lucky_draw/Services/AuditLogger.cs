using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;

namespace lucky_draw.Services
{
    // Tên file nhật ký: Logs/{tên}-yyyyMMdd.log
    public static class AuditLogs
    {
        public const string Remote = "remote";   // Đăng nhập, thiết bị Remote kết nối/ngắt, bấm quay trên Remote
        public const string Spin = "spin";       // Mỗi lượt quay số (web hoặc Remote) và kết quả
        public const string Report = "report";   // Xóa kết quả ở trang Báo cáo
    }

    public static class AuditEvents
    {
        public const string Login = "Login";
        public const string LoginFailed = "LoginFailed";
        public const string Connect = "Connect";
        public const string Disconnect = "Disconnect";
        public const string TriggerSpin = "TriggerSpin";
        public const string Spin = "Spin";
        public const string DeleteResult = "DeleteResult";
        public const string DeleteAllResults = "DeleteAllResults";
    }

    // Thông tin trang Remote tự gửi lên sau khi kết nối (trình duyệt không cho đọc MAC/serial)
    public class ClientDeviceInfo
    {
        public string? Screen { get; set; }
        public string? TimeZone { get; set; }
        public string? Language { get; set; }
        public string? Model { get; set; }           // Chỉ có trên Chromium qua HTTPS (Client Hints)
        public string? PlatformVersion { get; set; }
    }

    // Ghi nhật ký kiểm soát ra file Logs/{loại}-yyyyMMdd.log (mỗi sự kiện 1 dòng), kèm thông tin thiết bị thực hiện
    public class AuditLogger
    {
        public const string DeviceCookieName = "LuckyDraw.Device";
        private const int RetentionDays = 90;
        private const int MaxFieldLength = 300;    // Thông tin do trình duyệt gửi lên
        private const int MaxDetailLength = 8000;  // Ghi chú do máy chủ tạo (danh sách người trúng/bị xóa)

        private static readonly SemaphoreSlim WriteLock = new(1, 1);

        private readonly string _logDirectory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AuditLogger> _logger;
        private DateOnly _lastCleanup;

        public AuditLogger(IWebHostEnvironment env, IMemoryCache cache, ILogger<AuditLogger> logger)
        {
            _logDirectory = Path.Combine(env.ContentRootPath, "Logs");
            _cache = cache;
            _logger = logger;
        }

        // Mã thiết bị: chuỗi ngẫu nhiên lưu trong cookie lâu dài, dùng để nhận ra cùng 1 thiết bị qua các lần đăng nhập
        public static string? GetDeviceId(HttpContext http) =>
            http.Request.Cookies.TryGetValue(DeviceCookieName, out var id) && IsValidDeviceId(id) ? id : null;

        public static string EnsureDeviceId(HttpContext http)
        {
            var id = GetDeviceId(http);
            if (id != null) return id;

            id = Guid.NewGuid().ToString("N");
            http.Response.Cookies.Append(DeviceCookieName, id, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Strict,
                Secure = http.Request.IsHttps,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddYears(5)
            });
            return id;
        }

        private static bool IsValidDeviceId(string? id) => id is { Length: 32 } && id.All(char.IsAsciiHexDigit);

        // Không bao giờ ném lỗi: ghi nhật ký thất bại không được làm hỏng đăng nhập/quay số/xóa kết quả
        public async Task LogAsync(string logName, string eventName, HttpContext? http, string? role,
            string? detail = null, ClientDeviceInfo? client = null)
        {
            try
            {
                var ip = http?.Connection.RemoteIpAddress;
                if (ip != null && ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
                var userAgent = http?.Request.Headers.UserAgent.ToString();
                var (platform, model, browser) = ParseUserAgent(userAgent);
                if (!string.IsNullOrWhiteSpace(client?.Model)) model = client.Model;
                if (!string.IsNullOrWhiteSpace(client?.PlatformVersion) && platform != null) platform += " (" + client.PlatformVersion + ")";

                var fields = new (string Name, string? Value)[]
                {
                    ("Sự kiện", eventName),
                    ("Quyền", role),
                    ("Thiết bị", http != null ? GetDeviceId(http) : null),
                    ("IP", ip?.ToString()),
                    ("MAC", ip != null ? await GetMacAddressAsync(ip) : null),
                    ("Dòng máy", model),
                    ("Hệ điều hành", platform),
                    ("Trình duyệt", browser),
                    ("Màn hình", client?.Screen),
                    ("Múi giờ", client?.TimeZone),
                    ("Ngôn ngữ", client?.Language),
                    ("Chi tiết", detail),
                    ("User-Agent", userAgent)
                };
                var line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " +
                           string.Join(" | ", fields.Where(f => !string.IsNullOrWhiteSpace(f.Value))
                                                   .Select(f => f.Name + ": " + Clean(f.Value!, f.Name == "Chi tiết" ? MaxDetailLength : MaxFieldLength)));

                await AppendLineAsync(logName, line);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không ghi được nhật ký {Log} ({Event})", logName, eventName);
            }
        }

        private async Task AppendLineAsync(string logName, string line)
        {
            await WriteLock.WaitAsync();
            try
            {
                Directory.CreateDirectory(_logDirectory);
                var today = DateOnly.FromDateTime(DateTime.Now);
                var file = Path.Combine(_logDirectory, $"{logName}-{today:yyyyMMdd}.log");
                await File.AppendAllTextAsync(file, line + Environment.NewLine, Encoding.UTF8);

                if (_lastCleanup != today)
                {
                    _lastCleanup = today;
                    DeleteOldFiles(today);
                }
            }
            finally
            {
                WriteLock.Release();
            }
        }

        private void DeleteOldFiles(DateOnly today)
        {
            foreach (var file in Directory.GetFiles(_logDirectory, "*-*.log"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var datePart = name[(name.LastIndexOf('-') + 1)..];
                if (DateOnly.TryParseExact(datePart, "yyyyMMdd", out var day) && day < today.AddDays(-RetentionDays))
                    File.Delete(file);
            }
        }

        // Ghi trên 1 dòng: bỏ xuống dòng / ký tự "|" (dấu phân cách cột), giới hạn độ dài
        private static string Clean(string value, int maxLength)
        {
            var text = Regex.Replace(value, @"[\r\n\t|]+", " ").Trim();
            return text.Length > maxLength ? text[..maxLength] + "…" : text;
        }

        // ===== Địa chỉ MAC: máy chủ hỏi ARP theo IP của thiết bị (chỉ được khi cùng mạng LAN, máy chủ Windows) =====
        private async Task<string?> GetMacAddressAsync(IPAddress ip)
        {
            if (IPAddress.IsLoopback(ip)) return "(máy chủ)";
            if (ip.AddressFamily != AddressFamily.InterNetwork || !OperatingSystem.IsWindows()) return null;

            return await _cache.GetOrCreateAsync("mac:" + ip, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return Task.Run(() => LookupMac(ip));
            });
        }

        private static string? LookupMac(IPAddress ip)
        {
            var mac = new byte[6];
            uint length = (uint)mac.Length;
            var destination = BitConverter.ToUInt32(ip.GetAddressBytes(), 0);
            if (SendARP(destination, 0, mac, ref length) != 0 || length == 0) return null;
            return string.Join("-", mac.Take((int)length).Select(b => b.ToString("X2")));
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(uint destIp, uint srcIp, byte[] macAddress, ref uint macAddressLength);

        // ===== Hệ điều hành / dòng máy / trình duyệt từ User-Agent =====
        private static (string? Platform, string? Model, string? Browser) ParseUserAgent(string? ua)
        {
            if (string.IsNullOrEmpty(ua)) return (null, null, null);

            string? platform = null, model = null;
            var android = Regex.Match(ua, @"Android ([\d.]+);\s*([^;)]+?)(?: Build/[^;)]*)?\)");
            if (android.Success)
            {
                platform = "Android " + android.Groups[1].Value;
                var m = android.Groups[2].Value.Trim();
                if (m != "K") model = m; // Chrome mới che dòng máy bằng "K"
            }
            else if (Regex.Match(ua, @"(iPhone|iPad|iPod).*?OS ([\d_]+)") is { Success: true } ios)
            {
                platform = "iOS " + ios.Groups[2].Value.Replace('_', '.');
                model = ios.Groups[1].Value;
            }
            else if (Regex.Match(ua, @"Windows NT ([\d.]+)") is { Success: true } win) platform = "Windows NT " + win.Groups[1].Value;
            else if (Regex.Match(ua, @"Mac OS X ([\d_]+)") is { Success: true } mac) platform = "macOS " + mac.Groups[1].Value.Replace('_', '.');
            else if (ua.Contains("Linux")) platform = "Linux";

            string? browser = null;
            foreach (var (pattern, name) in new[]
            {
                (@"Edg[A-Z]*/([\d.]+)", "Edge"), (@"SamsungBrowser/([\d.]+)", "Samsung Internet"), (@"OPR/([\d.]+)", "Opera"),
                (@"CriOS/([\d.]+)", "Chrome"), (@"FxiOS/([\d.]+)", "Firefox"), (@"Firefox/([\d.]+)", "Firefox"),
                (@"Chrome/([\d.]+)", "Chrome"), (@"Version/([\d.]+).*Safari", "Safari")
            })
            {
                var b = Regex.Match(ua, pattern);
                if (b.Success) { browser = name + " " + b.Groups[1].Value.Split('.')[0]; break; }
            }
            return (platform, model, browser);
        }
    }
}
