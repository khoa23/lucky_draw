using System.Threading.RateLimiting;
using lucky_draw.Data;
using lucky_draw.Security;
using lucky_draw.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình riêng từng máy (mật khẩu, connection string) - không đưa lên git (xem .gitignore).
// Biến môi trường vẫn được ưu tiên cao hơn file này.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// Không để lộ phần mềm máy chủ qua header "Server"
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

// Thêm dịch vụ DbContext, dùng connection string từ appsettings.json
// Pool: tái sử dụng DbContext giữa các request, giảm cấp phát bộ nhớ
builder.Services.AddDbContextPool<LuckyDrawDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
            // Database đang ở compatibility level 100 (không có OPENJSON) => EF sinh "IN (...)" cho Contains()
            sql => sql.UseCompatibilityLevel(100))
        // RewardTypes.Id là tinyint IDENTITY từ database gốc (vài dòng, hiếm khi thêm) => bỏ qua cảnh báo này
        .ConfigureWarnings(w => w.Ignore(SqlServerEventId.ByteIdentityColumnWarning)));

// Thêm các dịch vụ khác
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<LuckyDrawService>();
builder.Services.AddScoped<ParticipantService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<FrontendAssets>();
builder.Services.AddSingleton<LoginService>();
builder.Services.AddSingleton<AuditLogger>();

// ===== Xác thực: cookie đăng nhập, 2 vai trò Admin (toàn quyền) và Remote (chỉ bấm quay) =====
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.Name = "LuckyDraw.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // Chạy HTTPS thì cookie tự gắn cờ Secure
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Events = new CookieAuthenticationEvents
        {
            // API và SignalR trả mã lỗi thay vì chuyển hướng sang trang đăng nhập
            OnRedirectToLogin = context => RespondOrRedirect(context, StatusCodes.Status401Unauthorized),
            OnRedirectToAccessDenied = context => RespondOrRedirect(context, StatusCodes.Status403Forbidden)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Mặc định mọi trang đều cần quyền Admin; trang nào mở rộng hơn thì khai báo riêng
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole(AppRoles.Admin)
        .Build();
});

// Chống brute-force mật khẩu: tối đa 5 lần đăng nhập mỗi phút cho mỗi địa chỉ IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync("Đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 1 phút.", cancellationToken);
    };
});

// Chống CSRF: mọi request POST/PUT/DELETE phải kèm antiforgery token (form hoặc header)
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.Name = "LuckyDraw.Antiforgery";
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Add MVC
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 4 * 1024; // Hub chỉ nhận lệnh ngắn
});

var app = builder.Build();

app.Services.GetRequiredService<LoginService>().WarnIfWeakPasswords();

// Cấu hình middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<lucky_draw.Hubs.LuckyDrawHub>("/luckyHub");
app.MapControllers();

app.Run();

static Task RespondOrRedirect(Microsoft.AspNetCore.Authentication.RedirectContext<CookieAuthenticationOptions> context, int statusCode)
{
    var path = context.Request.Path;
    if (path.StartsWithSegments("/api") || path.StartsWithSegments("/luckyHub"))
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    }
    context.Response.Redirect(context.RedirectUri);
    return Task.CompletedTask;
}
