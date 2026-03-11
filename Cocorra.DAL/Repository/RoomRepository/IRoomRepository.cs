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
    }
}