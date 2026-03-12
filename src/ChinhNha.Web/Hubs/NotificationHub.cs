using Microsoft.AspNetCore.SignalR;

namespace ChinhNha.Web.Hubs;

/// <summary>
/// SignalR Hub nhận thông báo đơn hàng mới theo thời gian thực.
/// Admin kết nối và nhận thông báo toast khi có đơn mới được đặt.
/// Route: /hubs/notification
/// </summary>
public class NotificationHub : Hub
{
    /// <summary>Admin tham gia nhóm để nhận tất cả thông báo.</summary>
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "AdminGroup");
    }

    /// <summary>Rời nhóm admin (dọn dẹp khi đóng tab).</summary>
    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AdminGroup");
    }
}
