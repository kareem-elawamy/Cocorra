using Cocorra.DAL.DTOS.RoomDto;
using Core.Base;

namespace Cocorra.BLL.Services.RoomService;

public interface IRoomService
{
    Task<Response<Guid>> CreateRoomAsync(CreateRoomDto dto, Guid hostId);
    Task<Response<bool>> JoinRoomAsync(Guid roomId, Guid userId);
    Task<Response<bool>> ApproveUserAsync(Guid roomId, Guid targetUserId, Guid hostId);
    Task<Response<RoomStateDto>> GetRoomStateAsync(Guid roomId, Guid currentUserId);
}
