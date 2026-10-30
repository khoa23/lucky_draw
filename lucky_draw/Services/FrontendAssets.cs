namespace lucky_draw.Services
{
    // Chọn file giao diện nhẹ nhất đang có (kiểm tra 1 lần lúc khởi động, không đọc ổ đĩa mỗi request).
    // Chạy tools/build-frontend.ps1 để tạo các file build sẵn, sau đó khởi động lại ứng dụng.
    public class FrontendAssets
    {
        // CSS Tailwind build sẵn: không phải dịch CSS trong trình duyệt như tailwindcss.js
        public bool HasTailwindCss { get; }

        // Vue bản production: nhỏ và chạy nhanh hơn bản development
        public string VueScript { get; }

        public FrontendAssets(IWebHostEnvironment env)
        {
            HasTailwindCss = File.Exists(Path.Combine(env.WebRootPath, "css", "tailwind.css"));
            VueScript = File.Exists(Path.Combine(env.WebRootPath, "lib", "vue", "vue.global.prod.js"))
                ? "~/lib/vue/vue.global.prod.js"
                : "~/lib/vue/vue.global.js";
        }
    }
}
