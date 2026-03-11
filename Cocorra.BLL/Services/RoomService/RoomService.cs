using Cocorra.DAL;
using Cocorra.DAL.DTOS.RoomDto;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.RoomRepository;
using Core.Base;

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
                StartDate = DateTime.UtcNow,
                status = RoomStatus.Live, 
                CreatedAt = DateTime.UtcNow
            };

            // 2. إضافة الـ Host كأول مشارك في الروم (Owner Logic)
            // الـ Host لازم يدخل الروم أوتوماتيك ويكون على الستيدج
            var hostParticipant = new RoomParticipant
            {
                UserId = hostId,
                Status = ParticipantStatus.Active,
                IsOnStage = true,   // طبعاً صاحب الروم على الستيدج
                IsMuted = false,    // والمايك مفتوح عشان يرحب بالناس
                JoinedAt = DateTime.UtcNow,
                // العلاقة هتتربط لما نضيفها للروم
            };

            // بنضيف المشارك لقائمة مشاركين الروم
            room.Participants.Add(hostParticipant);

            // 3. الحفظ في الداتابيز
            // الـ AddAsync هنا ذكية، هتحفظ الروم وهتحفظ المشارك اللي جواها في نفس الوقت (Transaction)
            await _roomRepo.AddAsync(room);

            // 4. إرجاع الـ ID عشان الفرونت يوجهه لصفحة الروم
            return Success(room.Id);
        }
        catch (Exception ex)
        {
            // ممكن تعمل Logging هنا
            return BadRequest<Guid>($"Failed to create room: {ex.Message}");
        }
    }
    public async Task<Response<bool>> JoinRoomAsync(Guid roomId, Guid userId)
    {
        var room = await _roomRepo.GetByIdAsync(roomId);
        if (room == null) return NotFound<bool>("Room not found.");

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
}