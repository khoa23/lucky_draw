using Microsoft.AspNetCore.SignalR;

namespace lucky_draw.Hubs
{
    public class LuckyDrawHub : Hub
    {
        // Gửi lệnh "Quay số" từ Remote đến Client
        public async Task TriggerSpin()
        {
            await Clients.All.SendAsync("ReceiveSpinCommand");
        }

        // Thông báo trạng thái (ví dụ để Remote biết máy tính đang quay)
        public async Task SendStatus(string status)
        {
            await Clients.All.SendAsync("ReceiveStatus", status);
        }
    }
}
