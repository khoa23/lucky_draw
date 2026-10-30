# Chương trình quay số may mắn

Ứng dụng web quay số trúng thưởng cho sự kiện: chiếu màn hình quay lên máy chiếu, điều khiển nút quay bằng điện thoại (Remote), quản lý giải thưởng, import danh sách khách hàng và xuất báo cáo người trúng. Được tối ưu để chạy với danh sách **10 triệu khách hàng** trên máy cấu hình thấp, chạy offline trong mạng nội bộ.

## Hình ảnh

<img width="1920" height="1040" alt="image" src="https://github.com/user-attachments/assets/d90cc761-02d7-4b08-9eb8-e6f32ef3cb1d" />

<img width="1920" height="1080" alt="image" src="https://github.com/user-attachments/assets/75a462de-238d-45b5-9f6c-7eebccd03a22" />

<img width="1920" height="1040" alt="image" src="https://github.com/user-attachments/assets/63603a33-0b38-4656-9815-44f0ee287214" />

<img width="1920" height="1040" alt="image" src="https://github.com/user-attachments/assets/6c6f20ba-e323-4efd-8040-e397b7357406" />

<img width="1920" height="1040" alt="image" src="https://github.com/user-attachments/assets/50116040-5c39-4241-94d6-2ec620bea28e" />

## Tính năng

- **Màn hình quay số** (`/LuckyDraw`): dãy số dừng lần lượt từ trái sang phải, âm thanh, pháo giấy, số giải còn lại ở góc phải, nút bật/tắt tiếng.
- **Remote** (`/Remote`): bấm quay từ điện thoại qua mạng nội bộ (SignalR). Lỗi khi quay (hết giải, chưa chọn giải...) hiện ngay trên điện thoại.
- **Cài đặt** (`/Settings`): chương trình, giải thưởng, số lượng, độ dài dãy số, kéo thả sắp xếp thứ tự giải.
- **Quay tất cả 1 lần**: giải có loại **"Chọn ngẫu nhiên liên tục"** — bấm quay một lần ra toàn bộ số giải còn lại, hiển thị danh sách người trúng (phù hợp giải có số lượng lớn). Các loại khác mỗi lần quay ra 1 người.
- **Import** (`/Home/ImportExcel`): nạp danh sách khách hàng từ `.xlsx`, `.xls` (nhiều sheet) hoặc `.csv`.
- **Khách hàng tham gia** (`/Participants`): số khách đủ điều kiện quay / tổng / đã trúng, danh sách có phân trang và tra cứu theo mã khách hàng (chạy nhanh với 10 triệu dòng).
- **Nhật ký thiết bị Remote**: mỗi lần đăng nhập (thành công/thất bại), thiết bị Remote kết nối/ngắt kết nối và mỗi lần bấm quay được ghi vào `lucky_draw/Logs/remote-yyyyMMdd.log` (giữ 90 ngày): thời điểm, quyền, mã thiết bị, IP, địa chỉ MAC (máy chủ tra theo IP, chỉ có khi cùng mạng LAN), dòng máy, hệ điều hành, trình duyệt, màn hình, múi giờ, ngôn ngữ. Trình duyệt không cho trang web đọc MAC/số serial của thiết bị, nên "mã thiết bị" là mã ngẫu nhiên app lưu trong cookie để nhận ra cùng một thiết bị.
- **Báo cáo** (`/Report`): danh sách người trúng, xuất Excel, xóa kết quả (tự lưu vào lịch sử, màn hình quay cập nhật số giải ngay).
- **Quay số công bằng**: chọn ngẫu nhiên đều tuyệt đối bằng bộ sinh số ngẫu nhiên mật mã; mỗi khách hàng chỉ trúng một lần; nhiều màn hình cùng nhận lệnh Remote vẫn chỉ ra một người trúng.

## Công nghệ

| Thành phần | Công nghệ |
|---|---|
| Backend | ASP.NET Core MVC (.NET 9), SignalR |
| Database | SQL Server, Entity Framework Core 9 (**Code First**, migrations) |
| Giao diện | Razor, Vue 3, Tailwind CSS, axios (thư viện đặt sẵn trong `wwwroot/lib`, không cần Internet) |
| Excel | ExcelDataReader (đọc), ClosedXML (xuất) |

## Yêu cầu

- [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server (Express trở lên)
- Windows + IIS nếu triển khai chính thức (có thể chạy trực tiếp bằng `dotnet run`)

## Cài đặt lần đầu

### 1. Cấu hình

Đặt chuỗi kết nối và mật khẩu cho từng máy:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=QuaySo;User Id=<tài-khoản>;Password=<mật-khẩu>;TrustServerCertificate=True;"
  },
  "AdminSettings": { "Password": "<mật-khẩu-quản-trị, tối thiểu 8 ký tự>" },
  "RemoteSettings": { "Password": "<mật-khẩu-remote, tối thiểu 6 ký tự>" }
}
```

Có thể dùng biến môi trường thay cho file (ưu tiên cao hơn), ví dụ `AdminSettings__Password`, `ConnectionStrings__DefaultConnection`.

> Chưa đặt `AdminSettings:Password` thì không đăng nhập được trang quản trị (log khi khởi động sẽ báo).

### 2. Tạo database

**Database mới** — chọn một trong hai cách:

```powershell
cd lucky_draw
dotnet ef database update                     # tạo trực tiếp theo chuỗi kết nối đã cấu hình
```

hoặc chạy file `Database/migrations.sql` bằng SSMS / sqlcmd (phù hợp khi cần DBA duyệt).

**Database đã có sẵn đủ cấu trúc hiện tại** (đã chạy hết các migration trước khi gộp thành `InitialCreate`): chạy **một lần** file `Database/mark-initial-create.sql` trên database đó. File chỉ ghi nhận mốc migration, không thay đổi bảng/dữ liệu, tự dừng nếu database chưa đủ cấu trúc, chạy lại nhiều lần vẫn an toàn.

Sau đó tạo chương trình và giải thưởng trong trang **Cài đặt**, hoặc nạp dữ liệu mẫu để chạy thử (chỉ dùng cho database thử, script xóa sạch dữ liệu quay số trước khi nạp):

```powershell
sqlcmd -S .\SQLEXPRESS -d QS_Test -f 65001 -i Database\seed-sample-data.sql
```

### 3. Chạy

```powershell
cd lucky_draw
dotnet run --launch-profile http              # http://<IP-máy>:5156
```

Mở `http://localhost:5156`, đăng nhập bằng mật khẩu quản trị.

## Đăng nhập và phân quyền

Trang đăng nhập dùng chung: `/Account/Login`.

| Mật khẩu | Quyền |
|---|---|
| `AdminSettings:Password` | Toàn quyền: quay số, cài đặt, import, báo cáo |
| `RemoteSettings:Password` | Chỉ vào `/Remote` để bấm quay |

Đăng nhập sai quá 5 lần/phút từ cùng một địa chỉ IP sẽ bị tạm khóa 1 phút.

## Quy trình sử dụng trong sự kiện

1. **Cài đặt**: kiểm tra chương trình, các giải, số lượng và thứ tự giải.
2. **Import**: nạp danh sách khách hàng (tải file mẫu ngay trên trang Import).
3. **Máy chiếu**: mở `/LuckyDraw` (đăng nhập quản trị), bấm F11 để toàn màn hình, chọn giải cần quay.
4. **Điện thoại**: kết nối cùng mạng, mở `http://<IP-máy-chủ>:5156/Remote`, đăng nhập bằng mật khẩu Remote, bấm **QUAY SỐ**.
5. **Báo cáo**: xem và xuất Excel danh sách trúng thưởng. Nếu cần quay lại một giải, xóa người trúng ở đây — số giải còn lại trên màn hình quay tự cập nhật.

## Import dữ liệu lớn

- Import theo file mẫu.
- Dòng đầu tiên của file phải là dòng tiêu đề. Cột được ghép theo **tên tiêu đề** nên thứ tự cột không quan trọng, cột thừa được bỏ qua; thiếu cột bắt buộc thì trang Import báo rõ thiếu cột nào. File theo mẫu cũ vẫn import được.
- Mỗi sheet Excel chứa tối đa khoảng 1 triệu dòng. Với danh sách lớn hơn: **chia thành nhiều sheet** trong cùng file (sheet sau có thể có hoặc không có dòng tiêu đề) hoặc dùng **file `.csv`** (không giới hạn số dòng, nên lưu UTF-8).
- Dung lượng file tối đa 2 GB (đã cấu hình cho cả Kestrel và IIS trong `web.config`).
- Quy trình: xóa bảng tạm → nạp dữ liệu theo lô → chạy procedure `InsertCustomerInfoFromTempTable` và `importdataFromCustomerInfo`. Không đóng trang trong lúc import.

## Tối ưu giao diện cho máy cấu hình thấp (khuyên dùng)

Mặc định các trang dùng `tailwindcss.js` (dịch CSS ngay trong trình duyệt) và Vue bản development. Chạy script sau **một lần** để tạo file CSS tĩnh và Vue bản production, giúp trang tải nhanh và nhẹ hơn:

```powershell
cd lucky_draw
powershell -ExecutionPolicy Bypass -File tools\build-frontend.ps1
```

Khởi động lại ứng dụng để dùng file mới (ứng dụng tự nhận, chưa chạy script thì vẫn dùng file cũ). Chạy lại script mỗi khi sửa class Tailwind trong các view. Nên commit `wwwroot/css/tailwind.css` và `wwwroot/lib/vue/vue.global.prod.js` để máy triển khai không phải tải lại.

## Thay đổi cấu trúc database (Code First)

Cấu trúc database được quản lý trong `lucky_draw/Data/LuckyDrawDbContext.cs` và thư mục `lucky_draw/Migrations`.

```powershell
cd lucky_draw
# 1. Sửa model / LuckyDrawDbContext
dotnet ef migrations add TenThayDoi                                  # 2. tạo migration
dotnet ef database update                                            # 3a. áp dụng trên máy dev
dotnet ef migrations script --idempotent -o ..\Database\migrations.sql   # 3b. xuất SQL cho máy chủ
```

## Bảo mật

- Toàn bộ trang và API yêu cầu đăng nhập; Remote chỉ có quyền gửi lệnh quay.
- Chống CSRF (antiforgery token), giới hạn số lần đăng nhập, cookie `HttpOnly` + `SameSite=Strict`, các HTTP header bảo mật (CSP, `X-Frame-Options`...).
- Ghi log các thao tác quan trọng: đăng nhập, import, xóa kết quả, xóa/sửa giải.
- Không dùng tài khoản `sa` cho ứng dụng: tạo tài khoản SQL riêng chỉ có quyền trên database quay số.
- Không đưa mật khẩu vào `appsettings.json` khi commit; dùng `appsettings.Local.json` hoặc biến môi trường.

## Cấu trúc thư mục

```
lucky_draw/
├── Controllers/      Trang và API (Account, LuckyDraw, LuckyDrawApi, Remote, Report, Settings, Home)
├── Data/             LuckyDrawDbContext - cấu hình database
├── Hubs/             SignalR hub cho Remote
├── Migrations/       EF Core migrations
├── Models/           Các bảng dữ liệu
├── Security/         Đăng nhập, header bảo mật
├── Services/         Quay số, import Excel
├── Views/            Giao diện Razor
├── tools/            Script build giao diện (Tailwind CSS, Vue production)
└── wwwroot/          Thư viện JS/CSS, font, âm thanh, hình ảnh
Database/
├── migrations.sql              Tạo database mới (idempotent)
├── mark-initial-create.sql     Ghi nhận mốc migration cho database đã có đủ cấu trúc
└── seed-sample-data.sql        Dữ liệu mẫu để chạy thử (xóa dữ liệu cũ trước khi nạp)
```

## Xử lý sự cố

| Hiện tượng | Nguyên nhân / cách xử lý |
|---|---|
| Không đăng nhập được, trang báo chưa cấu hình mật khẩu | Đặt `AdminSettings:Password` (xem mục Cấu hình). |
| "Đăng nhập sai quá nhiều lần" | Chờ 1 phút rồi thử lại. |
| Điện thoại vào được cổng HTTP nhưng không vào được HTTPS `:7261` | Chứng chỉ phát triển chỉ cấp cho `localhost` và chỉ được tin cậy trên máy dev. Dùng HTTP trong mạng nội bộ khi thử nghiệm, hoặc gắn chứng chỉ hợp lệ trên IIS. |
| Remote báo "Màn hình chưa sẵn sàng" | Màn hình `/LuckyDraw` chưa mở, chưa đăng nhập quản trị hoặc chưa chọn giải. |
| Build báo file `lucky_draw.exe` đang bị khóa | Ứng dụng đang chạy — dừng ứng dụng rồi build lại. |
| `dotnet ef` không tìm thấy | Chạy `dotnet tool restore` ở thư mục gốc repo. |
