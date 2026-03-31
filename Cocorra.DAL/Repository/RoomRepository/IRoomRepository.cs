using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cocorra.DAL.Repository.RoomRepository
{
    public interface IRoomRepository : IGenericRepositoryAsync<Room>
    {
        // 1. دوال القراءة (Read)
        Task<RoomParticipant?> GetParticipantAsync(Guid roomId, Guid userId);
        Task<List<RoomParticipant>> GetRoomParticipantsAsync(Guid roomId);
        Task<List<RoomParticipant>> GetStageSpeakersAsync(Guid roomId);

        // 2. النواقص: دوال التعديل الخاصة بالمشاركين (Write)
        Task AddParticipantAsync(RoomParticipant participant);
        Task UpdateParticipantAsync(RoomParticipant participant);
        Task RemoveParticipantAsync(RoomParticipant participant);
        Task<List<RoomReminder>> GetRemindersByRoomIdAsync(Guid roomId);
        Task RemoveRemindersAsync(IEnumerable<RoomReminder> reminders);
        Task AddNotificationsAsync(IEnumerable<Notification> notifications);

        Task<List<Room>> GetActiveRoomsAsync(int pageNumber = 1, int pageSize = 20); // لنجلب الغرف اللايف والمجدولة بس
        Task<RoomReminder?> GetRoomReminderAsync(Guid roomId, Guid userId);
        Task<int> GetRoomRemindersCountAsync(Guid roomId);
        Task AddRoomReminderAsync(RoomReminder reminder);
        Task RemoveRoomReminderAsync(RoomReminder reminder);
    }
}