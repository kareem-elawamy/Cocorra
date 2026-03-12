using Cocorra.DAL;
using Cocorra.DAL.DTOS.RoomDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.RoomRepository;
using Core.Base;
using Microsoft.Extensions.Hosting;

namespace Cocorra.BLL.Services.RoomService;

public class RoomService : ResponseHandler, IRoomService
{
    private readonly IRoomRepository _roomRepo;

    public RoomService(IRoomRepository roomRepo)
    {
        _roomRepo = roomRepo;
    }

    public async Task<Response<Guid>> CreateRoomAsync(CreateRoomDto dto, Guid hostId)
    {
        try
        {
            var status = RoomStatus.Live;
            if (dto.ScheduledStartDate.HasValue && dto.ScheduledStartDate > DateTime.UtcNow)
            {
                status = RoomStatus.Scheduled;
            }

            var room = new Room
            {
                RoomTitle = dto.RoomTitle,
                Description = dto.Description,
                TotalCapacity = dto.TotalCapacity,
                StageCapacity = dto.StageCapacity,
                DefaultSpeakerDurationMinutes = dto.DefaultSpeakerDurationMinutes,
                IsPrivate = dto.IsPrivate,
                SelectionMode = dto.SelectionMode,
                HostId = hostId,
                StartDate = dto.ScheduledStartDate ?? DateTime.UtcNow,
                status = status, // الحالة الجديدة
                CreatedAt = DateTime.UtcNow
            };

            if (status == RoomStatus.Live)
            {
                var hostParticipant = new RoomParticipant
                {
                    UserId = hostId,
                    Status = ParticipantStatus.Active,
                    IsOnStage = true,
                    IsMuted = false,
                    JoinedAt = DateTime.UtcNow,
                    LastUnmutedAt = DateTime.UtcNow
                };
                room.Participants.Add(hostParticipant);
            }

            await _roomRepo.AddAsync(room);
            return Success(room.Id);
        }
        catch (Exception ex)
        {
            return BadRequest<Guid>($"Failed to create room: {ex.Message}");
        }
    }
    public async Task<Response<bool>> JoinRoomAsync(Guid roomId, Guid userId)
    {
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<bool>("Room not found.");

        if (room.status == RoomStatus.Scheduled)
        {
            return BadRequest<bool>("This room has not started yet. You can set a reminder instead.");
        }
        if (room.status == RoomStatus.Ended || room.status == RoomStatus.Cancelled)
        {
            return BadRequest<bool>("This room is no longer available.");
        }

        // 👇 1. التأكد من السعة (Capacity) للناس اللي "جوه" فعلاً
        var allParticipants = await _roomRepo.GetRoomParticipantsAsync(roomId);
        var activeCount = allParticipants.Count(p => p.Status == ParticipantStatus.Active || p.Status == ParticipantStatus.PendingApproval);

        // 👇 2. تشيك اليوزر القديم
        var existingParticipant = allParticipants.FirstOrDefault(p => p.UserId == userId);

        if (existingParticipant != null)
        {
            if (existingParticipant.Status == ParticipantStatus.Active) return Success(true);
            if (existingParticipant.Status == ParticipantStatus.Kicked) return BadRequest<bool>("You are banned from this room.");

            // لو كانت خرجت (Left) وعايزة ترجع:
            // لازم نتأكد إن لسه في مكان قبل ما نرجعها!
            if (activeCount >= room.TotalCapacity) return BadRequest<bool>("Room is full.");

            existingParticipant.Status = room.IsPrivate ? ParticipantStatus.PendingApproval : ParticipantStatus.Active;
            existingParticipant.JoinedAt = DateTime.UtcNow;
            existingParticipant.IsOnStage = false;
            existingParticipant.IsMuted = true;

            await _roomRepo.UpdateParticipantAsync(existingParticipant);
            await _roomRepo.SaveChangesAsync();
            return Success(room.IsPrivate ? false : true, room.IsPrivate ? "Request sent." : "Rejoined successfully.");
        }

        // 👇 3. لو يوزر جديد خالص، نتأكد من العدد برضه
        if (activeCount >= room.TotalCapacity)
        {
            return BadRequest<bool>("Room is full.");
        }

        // 4. ضيف اليوزر
        var newParticipant = new RoomParticipant
        {
            RoomId = roomId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            IsOnStage = false,
            IsMuted = true,
            Status = room.IsPrivate ? ParticipantStatus.PendingApproval : ParticipantStatus.Active
        };

        await _roomRepo.AddParticipantAsync(newParticipant);
        await _roomRepo.SaveChangesAsync();

        return Success(room.IsPrivate ? false : true, room.IsPrivate ? "Request sent, waiting for approval." : "Joined successfully.");
    }
    public async Task<Response<bool>> ApproveUserAsync(Guid roomId, Guid targetUserId, Guid hostId)
    {
        // 1. نجيب الروم عشان نتأكد مين صاحبها
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<bool>("Room not found.");

        // 2. حماية (Security): هل اللي باعت الطلب هو الكوتش فعلاً؟
        if (room.HostId != hostId)
            return BadRequest<bool>("Only the host can approve join requests.");

        // 3. ندور على طلب الانضمام بتاع اليوزر ده
        var participant = await _roomRepo.GetParticipantAsync(roomId, targetUserId);
        if (participant == null)
            return NotFound<bool>("User request not found.");

        if (participant.Status == ParticipantStatus.Active)
            return Success(true, "User is already active in the room.");

        // 4. نغير الحالة لـ Active (مقبول)
        participant.Status = ParticipantStatus.Active;

        await _roomRepo.UpdateParticipantAsync(participant);
        await _roomRepo.SaveChangesAsync();

        return Success(true, "User approved successfully.");
    }
    public async Task<Response<RoomStateDto>> GetRoomStateAsync(Guid roomId, Guid currentUserId)
    {
        // 1. نجيب الروم
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<RoomStateDto>("Room not found.");

        // 2. حماية: نتأكد إن اليوزر اللي بيطلب الداتا دي أصلاً عضو في الروم ومقبول
        var currentParticipant = await _roomRepo.GetParticipantAsync(roomId, currentUserId);
        if (currentParticipant == null || currentParticipant.Status != ParticipantStatus.Active)
        {
            return BadRequest<RoomStateDto>("You are not an active member of this room.");
        }

        // 3. نجيب كل الناس اللي في الروم (والمقبولين بس)
        var participants = await _roomRepo.GetRoomParticipantsAsync(roomId);
        var activeParticipants = participants.Where(p => p.Status == ParticipantStatus.Active).ToList();

        // 4. نجمع الداتا في الـ DTO
        var roomState = new RoomStateDto
        {
            RoomId = room.Id,
            RoomTitle = room.RoomTitle,
            HostId = room.HostId,
            TotalCapacity = room.TotalCapacity,
            StageCapacity = room.StageCapacity,
            Participants = activeParticipants.Select(p => new ParticipantStateDto
            {
                UserId = p.UserId,
                Name = p.User?.FirstName + " " + p.User?.LastName,
                IsOnStage = p.IsOnStage,
                IsMuted = p.IsMuted,
                IsHandRaised = p.IsHandRaised,
                JoinedAt = p.JoinedAt
            }).ToList()
        };

        return Success(roomState);
    }

    public async Task<Response<IEnumerable<RoomSummaryDto>>> GetRoomsFeedAsync(Guid currentUserId)
    {
        // 1. نجيب الغرف من الريبو مباشرة
        var activeRooms = await _roomRepo.GetActiveRoomsAsync();
        var resultList = new List<RoomSummaryDto>();

        foreach (var room in activeRooms)
        {
            var dto = new RoomSummaryDto
            {
                Id = room.Id,
                RoomTitle = room.RoomTitle,
                Description = room.Description,
                Status = room.status,
                ScheduledStartDate = room.StartDate,
                IsReminderSetByMe = false,
                ListenersCount = 0,
                HostName = room.Host!.FirstName + " " + room.Host.LastName,
            };

            if (room.status == RoomStatus.Live)
            {
                var participants = await _roomRepo.GetRoomParticipantsAsync(room.Id);
                dto.ListenersCount = participants.Count(p => p.Status == ParticipantStatus.Active);
            }
            else if (room.status == RoomStatus.Scheduled)
            {
                // 👇 نستخدم الريبو بدل الـ DbContext 👇
                var reminder = await _roomRepo.GetRoomReminderAsync(room.Id, currentUserId);
                dto.IsReminderSetByMe = reminder != null;

                dto.ListenersCount = await _roomRepo.GetRoomRemindersCountAsync(room.Id);
            }

            resultList.Add(dto);
        }

        var sortedList = resultList
            .OrderBy(r => r.Status == RoomStatus.Live ? 0 : 1)
            .ThenBy(r => r.ScheduledStartDate)
            .ToList();

        return Success<IEnumerable<RoomSummaryDto>>(sortedList);
    }

    public async Task<Response<string>> StartScheduledRoomAsync(Guid roomId, Guid hostId)
    {
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<string>("Room not found.");

        // 1. حماية: نتأكد إن الكوتش هو صاحب الروم وإنها لسه مابدأتش
        if (room.HostId != hostId)
            return BadRequest<string>("Only the host can start this room.");

        if (room.status == RoomStatus.Live)
            return BadRequest<string>("This room is already live.");

        if (room.status == RoomStatus.Ended || room.status == RoomStatus.Cancelled)
            return BadRequest<string>("This room is no longer available.");

        // 2. تحويل الحالة لـ Live
        room.status = RoomStatus.Live;
        await _roomRepo.UpdateAsync(room); // ده بيعمل Update للروم

        // 3. إضافة الكوتش كأول متحدث على المسرح (زي ما عملنا في الـ Create)
        var hostParticipant = new RoomParticipant
        {
            RoomId = roomId,
            UserId = hostId,
            Status = ParticipantStatus.Active,
            IsOnStage = true,
            IsMuted = false,
            JoinedAt = DateTime.UtcNow,
            LastUnmutedAt = DateTime.UtcNow
        };
        await _roomRepo.AddParticipantAsync(hostParticipant);

        // 4. 🔔 جلب المشتركين وإرسال الإشعارات
        var reminders = await _roomRepo.GetRemindersByRoomIdAsync(roomId);
        if (reminders.Any())
        {
            var notifications = reminders.Select(r => new Notification
            {
                UserId = r.UserId,
                Title = "Room Starting Now! 🎙️",
                Message = $"The room '{room.RoomTitle}' has just started. Join now!",
                Type = NotificationType.RoomReminder,
                ReferenceId = room.Id,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _roomRepo.AddNotificationsAsync(notifications);

            // نقدر نمسح الـ Reminders عشان دورهم انتهى (اختياري بس بينظف الداتابيز)
            await _roomRepo.RemoveRemindersAsync(reminders);
        }

        // نحفظ كل التغييرات دي خبطة واحدة
        await _roomRepo.SaveChangesAsync();

        return Success("Room is now live and notifications have been sent!");
    }
    public async Task<Response<string>> ToggleReminderAsync(Guid roomId, Guid userId)
    {
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<string>("Room not found.");

        if (room.status != RoomStatus.Scheduled)
            return BadRequest<string>("You can only set reminders for scheduled rooms.");

        // 👇 نستخدم الريبو بدل الـ DbContext 👇
        var existingReminder = await _roomRepo.GetRoomReminderAsync(roomId, userId);

        if (existingReminder != null)
        {
            await _roomRepo.RemoveRoomReminderAsync(existingReminder);
            return Success("Reminder removed.");
        }
        else
        {
            var reminder = new RoomReminder
            {
                RoomId = roomId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _roomRepo.AddRoomReminderAsync(reminder);
            return Success("Reminder set successfully.");
        }
    }
}