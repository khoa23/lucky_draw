namespace lucky_draw.Security
{
    // Thêm các HTTP header bảo mật cho mọi response
    public class SecurityHeadersMiddleware
    {
        // Vue (bản dùng template trong HTML) và tailwindcss.js cần 'unsafe-eval'; các trang dùng script/style inline.
        // CSP vẫn chặn tải script/kết nối tới bất kỳ máy chủ nào khác và chặn nhúng trang vào iframe.
        private const string ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "media-src 'self'; " +
            "connect-src 'self' ws: wss:; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                headers["Content-Security-Policy"] = ContentSecurityPolicy;
                return Task.CompletedTask;
            });
            return _next(context);
        }
    }
}
