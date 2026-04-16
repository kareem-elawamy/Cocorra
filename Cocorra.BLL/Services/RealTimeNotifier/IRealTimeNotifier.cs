using System;
using System.Threading.Tasks;

namespace Cocorra.BLL.Services.RealTimeNotifier
{
    /// <summary>
    /// Abstraction for broadcasting real-time events to connected clients.
    /// Decouples the BLL from SignalR hub implementations in the API layer.
    /// </summary>
    public interface IRealTimeNotifier
    {
        /// <summary>
        /// Sends a ForceLogout event to a specific user, causing the client to
        /// disconnect from rooms and clear the session immediately.
        /// </summary>
        Task ForceLogoutAsync(Guid userId, string reason);
    }
}
