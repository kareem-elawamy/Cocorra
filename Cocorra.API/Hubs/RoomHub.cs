using Cocorra.BLL.Services.RoomService;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Repository.RoomRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Cocorra.API.Hubs
{
    [Authorize]
    public class RoomHub : Hub
    {
        private readonly IRoomRepository _roomRepo;
        private readonly IRoomService _roomService;

        // Thread-safe mapping: ConnectionId → (UserId, RoomId)
        private static readonly ConcurrentDictionary<string, (Guid UserId, Guid RoomId)> _connections = new();

        public RoomHub(IRoomRepository roomRepo, IRoomService roomService)
        {
            _roomRepo = roomRepo;
            _roomService = roomService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryRemove(Context.ConnectionId, out var mapping))
            {
                try
                {
                    await _roomService.LeaveRoomCleanupAsync(mapping.RoomId, mapping.UserId);

                    var roomIdString = mapping.RoomId.ToString();
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomIdString);
                    await Clients.Group(roomIdString).SendAsync("UserLeft", new
                    {
                        UserId = mapping.UserId
                    });
                }
                catch { }
            }

            await base.OnDisconnectedAsync(exception);
        }

        private Guid GetUserId()
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                throw new HubException("Unauthorized user.");
            return userId;
        }

        private static Guid ParseGuidSafe(string value, string fieldName)
        {
            if (!Guid.TryParse(value, out Guid result))
                throw new HubException($"Invalid {fieldName}.");
            return result;
        }

        public async Task JoinRoom(string roomId)
        {
            var userId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.Status != RoomStatus.Live)
                throw new HubException("Room is not live yet or has ended.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);

            if (participant == null)
                throw new HubException("You are not a member of this room.");
            if (participant.Status == ParticipantStatus.PendingApproval)
                throw new HubException("Your request is still pending approval from the host.");
            if (participant.Status == ParticipantStatus.Kicked || participant.Status == ParticipantStatus.Rejected)
                throw new HubException("You are not allowed to join this room.");

            // Track connection for disconnect cleanup
            _connections[Context.ConnectionId] = (userId, roomGuid);

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            await Clients.Group(roomId).SendAsync("UserJoined", new
            {
                UserId = userId,
                Name = participant.User?.FirstName + " " + participant.User?.LastName,
                IsOnStage = participant.IsOnStage
            });
        }

        public async Task LeaveRoom(string roomId)
        {
            var userId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");

            await _roomService.LeaveRoomCleanupAsync(roomGuid, userId);

            _connections.TryRemove(Context.ConnectionId, out _);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("UserLeft", new
            {
                UserId = userId
            });
        }

        public async Task RaiseHand(string roomId)
        {
            var userId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);
            if (participant == null) throw new HubException("You are not a member of this room.");

            if (participant.IsOnStage) return;

            participant.IsHandRaised = true;
            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("HandRaised", new
            {
                UserId = userId,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }

        public async Task ApproveToStage(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");
            var targetGuid = ParseGuidSafe(targetUserId, "Target User ID");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can approve speakers to the stage.");

            var stageSpeakers = await _roomRepo.GetStageSpeakersAsync(roomGuid);
            if (stageSpeakers.Count >= room.StageCapacity)
                throw new HubException("Stage is full. Someone must leave the stage first.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) throw new HubException("User not found in room.");

            participant.IsOnStage = true;
            participant.IsHandRaised = false;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("StageUpdated", new
            {
                UserId = targetGuid,
                IsOnStage = true,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }

        public async Task MoveToAudience(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");
            var targetGuid = ParseGuidSafe(targetUserId, "Target User ID");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can demote speakers.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) return;

            if (!participant.IsMuted && participant.LastUnmutedAt.HasValue)
            {
                var spokenSeconds = (DateTime.UtcNow - participant.LastUnmutedAt.Value).TotalSeconds;
                participant.TotalSpokenSeconds += spokenSeconds;
                participant.LastUnmutedAt = null;
            }

            participant.IsOnStage = false;
            participant.IsMuted = true;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("StageUpdated", new
            {
                UserId = targetGuid,
                IsOnStage = false,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });

            await Clients.Group(roomId).SendAsync("MicStatusChanged", new
            {
                UserId = targetGuid,
                IsMuted = true,
                Name = participant.User?.FirstName
            });
        }

        public async Task ToggleMic(string roomId, bool muteStatus)
        {
            var userId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null) return;

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);
            if (participant == null || !participant.IsOnStage) return;

            var totalAllowedSeconds = (room.DefaultSpeakerDurationMinutes + participant.ExtraMinutesGranted) * 60;
            var remainingSeconds = totalAllowedSeconds - participant.TotalSpokenSeconds;

            if (muteStatus == false && remainingSeconds <= 0 && userId != room.HostId)
            {
                throw new HubException("Your time is up! The host needs to grant you more time.");
            }

            if (muteStatus == false && participant.IsMuted == true)
            {
                participant.LastUnmutedAt = DateTime.UtcNow;
            }
            else if (muteStatus == true && participant.IsMuted == false)
            {
                if (participant.LastUnmutedAt.HasValue)
                {
                    var spokenSeconds = (DateTime.UtcNow - participant.LastUnmutedAt.Value).TotalSeconds;
                    participant.TotalSpokenSeconds += spokenSeconds;
                    participant.LastUnmutedAt = null;

                    remainingSeconds = totalAllowedSeconds - participant.TotalSpokenSeconds;
                }
            }

            participant.IsMuted = muteStatus;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("MicStatusChanged", new
            {
                UserId = userId,
                IsMuted = muteStatus,
                Name = participant.User?.FirstName,
                RemainingSeconds = Math.Max(0, Math.Round(remainingSeconds))
            });
        }

        public async Task GrantExtraTime(string roomId, string targetUserId, int minutes)
        {
            var hostId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");
            var targetGuid = ParseGuidSafe(targetUserId, "Target User ID");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can grant extra time.");

            if (minutes < 1 || minutes > 30)
                throw new HubException("Extra time must be between 1 and 30 minutes.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null || !participant.IsOnStage) return;

            participant.ExtraMinutesGranted += minutes;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("ExtraTimeGranted", new
            {
                UserId = targetGuid,
                AddedMinutes = minutes,
                Name = participant.User?.FirstName
            });
        }

        public async Task KickUser(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");
            var targetGuid = ParseGuidSafe(targetUserId, "Target User ID");

            if (hostId == targetGuid)
                throw new HubException("The host cannot kick themselves.");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can kick users.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) return;

            participant.Status = ParticipantStatus.Kicked;
            participant.IsOnStage = false;
            participant.IsMuted = true;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            await Clients.Group(roomId).SendAsync("UserKicked", new
            {
                UserId = targetGuid,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }

        public async Task EndRoom(string roomId)
        {
            var hostId = GetUserId();
            var roomGuid = ParseGuidSafe(roomId, "Room ID");

            var result = await _roomService.EndRoomAsync(roomGuid, hostId);

            if (!result.Succeeded)
                throw new HubException(result.Message);

            await Clients.Group(roomId).SendAsync("RoomEnded", new
            {
                RoomId = roomGuid,
                Message = "The host has ended this room."
            });
        }
    }
}