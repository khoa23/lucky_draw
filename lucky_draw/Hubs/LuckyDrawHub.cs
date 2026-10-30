using System.Collections.Concurrent;
using System.Security.Claims;
using lucky_draw.Security;
using lucky_draw.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lucky_draw.Hubs
{
    // Chỉ người đã đăng nhập mới kết nối được; Remote chỉ được ra lệnh quay
    [Authorize(Roles = AppRoles.AdminOrRemote)]
    public class LuckyDrawHub : Hub
    {
        private const int MaxErrorMessageLength = 200;

        // Các kết nối của trang Remote (để ghi nhật ký lúc ngắt kết nối)
        private static readonly ConcurrentDictionary<string, byte> RemoteConnections = new();

        private readonly AuditLogger _audit;

        public LuckyDrawHub(AuditLogger audit)
        {
            _audit = audit;
        }

        private string? CurrentRole => Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        // Trang Remote gọi ngay sau khi kết nối: ghi nhật ký thiết bị
        public async Task RegisterDevice(ClientDeviceInfo info)
        {
            var isNew = RemoteConnections.TryAdd(Context.ConnectionId, 0);
            await _audit.LogAsync(AuditLogs.Remote, AuditEvents.Connect, Context.GetHttpContext(), CurrentRole,
                isNew ? null : "kết nối lại", info);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (RemoteConnections.TryRemove(Context.ConnectionId, out _))
            {
                await _audit.LogAsync(AuditLogs.Remote, AuditEvents.Disconnect, Context.GetHttpContext(), CurrentRole,
                    exception != null ? "mất kết nối: " + exception.Message : null);
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Gửi lệnh "Quay số" từ Remote đến Client.
        // Kèm mã lệnh để nếu mở nhiều màn hình cùng lúc thì tất cả chỉ tạo 1 kết quả và hiển thị giống nhau.
        public async Task TriggerSpin()
        {
            var requestId = Guid.NewGuid().ToString("N");
            await _audit.LogAsync(AuditLogs.Remote, AuditEvents.TriggerSpin, Context.GetHttpContext(), CurrentRole, "mã lệnh " + requestId);
            await Clients.All.SendAsync("ReceiveSpinCommand", requestId);
        }

        // Thông báo trạng thái (ví dụ để Remote biết máy tính đang quay)
        [Authorize(Roles = AppRoles.Admin)]
        public async Task SendStatus(string status)
        {
            if (status != "spinning" && status != "ready") return;
            await Clients.All.SendAsync("ReceiveStatus", status);
        }

        // Báo lỗi quay số (hết giải, chưa chọn giải...) về Remote khi lệnh quay đến từ Remote
        [Authorize(Roles = AppRoles.Admin)]
        public async Task SendSpinError(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.Length > MaxErrorMessageLength) message = message[..MaxErrorMessageLength];
            await Clients.Others.SendAsync("ReceiveSpinError", message);
        }
    }
}
