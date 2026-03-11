using Cocorra.DAL;
using Cocorra.DAL.Repository.RoomRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Cocorra.API.Hubs
{
    [Authorize]
    public class RoomHub : Hub // غيرنا الاسم هنا
    {
        private readonly IRoomRepository _roomRepo;

        public RoomHub(IRoomRepository roomRepo)
        {
            _roomRepo = roomRepo;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // ممكن هنا في المستقبل نعمل لوجيك لو النت فصل فجأة عن اليوزر
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinRoom(string roomId)
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                throw new HubException("Unauthorized user.");

            if (!Guid.TryParse(roomId, out Guid roomGuid))
                throw new HubException("Invalid Room ID.");

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.status != RoomStatus.Live)
                throw new HubException("Room is not live yet or has ended.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);

            if (participant == null)
                throw new HubException("You are not a member of this room.");
            if (participant.Status == ParticipantStatus.PendingApproval)
                throw new HubException("Your request is still pending approval from the host.");
            if (participant.Status == ParticipantStatus.Kicked || participant.Status == ParticipantStatus.Rejected)
                throw new HubException("You are not allowed to join this room.");

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
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out Guid userId) && Guid.TryParse(roomId, out Guid roomGuid))
            {
                // 1. نجيب اليوزر من الداتابيز
                var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);
                if (participant != null)
                {
                    // 2. حماية التايمر: لو خرج وهو فاتح المايك، نحسب الثواني ونقفل العداد
                    if (participant.IsMuted == false && participant.LastUnmutedAt.HasValue)
                    {
                        var spokenSeconds = (DateTime.UtcNow - participant.LastUnmutedAt.Value).TotalSeconds;
                        participant.TotalSpokenSeconds += spokenSeconds;
                        participant.LastUnmutedAt = null;
                    }

                    // 3. نغير حالته وننزله من المسرح
                    participant.Status = ParticipantStatus.Left; // 👈 ضيفها في الـ Enum لو مش موجودة
                    participant.IsOnStage = false;
                    participant.IsMuted = true;
                    participant.IsHandRaised = false;

                    await _roomRepo.UpdateParticipantAsync(participant);
                    await _roomRepo.SaveChangesAsync();
                }

                // 4. نطلعه من الـ SignalR ونبلغ الباقيين
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("UserLeft", new
                {
                    UserId = userIdString
                });
            }
        }
        // --- دالة مساعدة لاستخراج الـ ID بتاع اليوزر ---
        private Guid GetUserId()
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                throw new HubException("Unauthorized user.");
            return userId;
        }

        // =========================================================
        // 1. رفع الإيد (لليوزر العادي)
        // =========================================================
        public async Task RaiseHand(string roomId)
        {
            var userId = GetUserId();
            var roomGuid = Guid.Parse(roomId);

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);
            if (participant == null) throw new HubException("You are not a member of this room.");

            // لو هو أصلاً على المسرح، ملوش لازمة يرفع إيده
            if (participant.IsOnStage) return;

            participant.IsHandRaised = true;
            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync(); // متنساش تحفظ في الداتابيز

            // بنبعت رسالة لكل اللي في الروم (والفرونت إند بيظهر الإشعار ده للكوتش بس)
            await Clients.Group(roomId).SendAsync("HandRaised", new
            {
                UserId = userId,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }

        // =========================================================
        // 2. الموافقة على الصعود للمسرح (للكوتش فقط)
        // =========================================================
        public async Task ApproveToStage(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = Guid.Parse(roomId);
            var targetGuid = Guid.Parse(targetUserId);

            // 1. نتأكد إن اللي بينادي الدالة دي هو الكوتش فعلاً
            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can approve speakers to the stage.");

            // 2. نتشيك على سعة المسرح (عشان ميزيدش عن 5 مثلاً)
            var stageSpeakers = await _roomRepo.GetStageSpeakersAsync(roomGuid);
            if (stageSpeakers.Count >= room.StageCapacity)
                throw new HubException("Stage is full. Someone must leave the stage first.");

            // 3. نطلع اليوزر المسرح
            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) throw new HubException("User not found in room.");

            participant.IsOnStage = true;
            participant.IsHandRaised = false; // نزل إيده خلاص لأنه طلع
            // ملاحظة: هنسيبه Muted لحد ما هو يفتح المايك بنفسه (أفضل كـ User Experience)

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            // 4. نبلغ كل الروم إن في شخص جديد طلع المسرح
            await Clients.Group(roomId).SendAsync("StageUpdated", new
            {
                UserId = targetGuid,
                IsOnStage = true,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }

        // =========================================================
        // 3. تنزيل شخص للجمهور (للكوتش فقط)
        // =========================================================
        public async Task MoveToAudience(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = Guid.Parse(roomId);
            var targetGuid = Guid.Parse(targetUserId);

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can demote speakers.");

            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) return;

            // 👇👇 الإصلاح هنا (معالجة التايمر قبل التنزيل) 👇👇
            if (participant.IsMuted == false && participant.LastUnmutedAt.HasValue)
            {
                // نحسب الثواني اللي اتكلمها قبل ما الكوتش يطرده
                var spokenSeconds = (DateTime.UtcNow - participant.LastUnmutedAt.Value).TotalSeconds;
                participant.TotalSpokenSeconds += spokenSeconds;
                participant.LastUnmutedAt = null; // تصفير العداد
            }

            // نزله من عالمسرح واقفل مايكه الإجباري
            participant.IsOnStage = false;
            participant.IsMuted = true;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            // 1. نبلغ إن الشخص نزل للجمهور
            await Clients.Group(roomId).SendAsync("StageUpdated", new
            {
                UserId = targetGuid,
                IsOnStage = false,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });

            // 2. 👇👇 الإضافة الجديدة: نبلغ الكل بوضوح إن مايكه اتقفل 👇👇
            await Clients.Group(roomId).SendAsync("MicStatusChanged", new
            {
                UserId = targetGuid,
                IsMuted = true,
                Name = participant.User?.FirstName
            });
        }
        // =========================================================
        // 4. فتح وقفل المايك (للمتحدثين على المسرح فقط) + نظام التايمر
        // =========================================================
        public async Task ToggleMic(string roomId, bool muteStatus)
        {
            var userId = GetUserId();
            var roomGuid = Guid.Parse(roomId);

            // نجيب الروم عشان نعرف الوقت الافتراضي
            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null) return;


            var participant = await _roomRepo.GetParticipantAsync(roomGuid, userId);
            if (participant == null || !participant.IsOnStage) return;

            // 1. حساب الوقت المسموح ليه بالثواني (الوقت الافتراضي + أي وقت إضافي خده)
            var totalAllowedSeconds = (room.DefaultSpeakerDurationMinutes + participant.ExtraMinutesGranted) * 60;
            var remainingSeconds = totalAllowedSeconds - participant.TotalSpokenSeconds;

            // 2. الحماية (لو بيحاول يفتح المايك ووقته خلصان)
            if (muteStatus == false && remainingSeconds <= 0 && userId != room.HostId)
            {
                throw new HubException("Your time is up! The host needs to grant you more time.");
            }
            if (muteStatus == false && remainingSeconds <= 0 && userId != room.HostId)
            {
                throw new HubException("Your time is up! The host needs to grant you more time.");
            }

            // 3. لوجيك تسجيل فتح وقفل المايك
            if (muteStatus == false && participant.IsMuted == true)
            {
                // بيفتح المايك: نسجل لحظة الفتح
                participant.LastUnmutedAt = DateTime.UtcNow;
            }
            else if (muteStatus == true && participant.IsMuted == false)
            {
                // بيقفل المايك: نحسب هو اتكلم قد إيه ونضيفهم للرصيد
                if (participant.LastUnmutedAt.HasValue)
                {
                    var spokenSeconds = (DateTime.UtcNow - participant.LastUnmutedAt.Value).TotalSeconds;
                    participant.TotalSpokenSeconds += spokenSeconds;
                    participant.LastUnmutedAt = null; // نصفر العداد

                    // نحدث الرصيد المتبقي بعد القفل
                    remainingSeconds = totalAllowedSeconds - participant.TotalSpokenSeconds;
                }
            }

            participant.IsMuted = muteStatus;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            // 4. نبلغ الروم كلها إن حالة المايك اتغيرت + نبعت الوقت المتبقي
            await Clients.Group(roomId).SendAsync("MicStatusChanged", new
            {
                UserId = userId,
                IsMuted = muteStatus,
                Name = participant.User?.FirstName,
                RemainingSeconds = Math.Max(0, Math.Round(remainingSeconds)) // بنبعت الثواني المتبقية لفلاتر
            });
        }

        // =========================================================
        // 5. إعطاء وقت إضافي (للكوتش فقط)
        // =========================================================
        public async Task GrantExtraTime(string roomId, string targetUserId, int minutes)
        {
            var hostId = GetUserId();
            var roomGuid = Guid.Parse(roomId);
            var targetGuid = Guid.Parse(targetUserId);

            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can grant extra time.");

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
        // =========================================================
        // 6. الطرد النهائي من الروم (للكوتش فقط)
        // =========================================================
        public async Task KickUser(string roomId, string targetUserId)
        {
            var hostId = GetUserId();
            var roomGuid = Guid.Parse(roomId);
            var targetGuid = Guid.Parse(targetUserId);

            // 1. التأكد من صلاحيات الكوتش
            var room = await _roomRepo.GetByIdAsync(roomGuid);
            if (room == null || room.HostId != hostId)
                throw new HubException("Only the host can kick users.");

            // 2. نجيب اليوزر اللي هينطرد
            var participant = await _roomRepo.GetParticipantAsync(roomGuid, targetGuid);
            if (participant == null) return; // لو مش موجود خلاص

            // 3. نغير حالته في الداتابيز لـ "مطرود"
            participant.Status = ParticipantStatus.Kicked;
            participant.IsOnStage = false;
            participant.IsMuted = true;

            await _roomRepo.UpdateParticipantAsync(participant);
            await _roomRepo.SaveChangesAsync();

            // 4. نبعت رسالة في الـ SignalR للجروب كله إن الشخص ده انطرد
            await Clients.Group(roomId).SendAsync("UserKicked", new
            {
                UserId = targetGuid,
                Name = participant.User?.FirstName + " " + participant.User?.LastName
            });
        }
    }
}