using Cocorra.DAL.Data;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cocorra.DAL.Repository.RoomRepository
{
    public class RoomRepository : GenericRepositoryAsync<Room>, IRoomRepository
    {
        public RoomRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<RoomParticipant?> GetParticipantAsync(Guid roomId, Guid userId)
        {
            return await _dbContext.RoomParticipants
                                 .Include(p => p.User)
                                 .FirstOrDefaultAsync(p => p.RoomId == roomId && p.UserId == userId);
        }

        public async Task<List<RoomParticipant>> GetRoomParticipantsAsync(Guid roomId)
        {
            return await _dbContext.RoomParticipants
                                 .Where(p => p.RoomId == roomId)
                                 .Include(p => p.User)
                                 .OrderByDescending(p => p.IsOnStage)
                                 .ThenBy(p => p.JoinedAt)
                                 .ToListAsync();
        }

        public async Task<List<RoomParticipant>> GetStageSpeakersAsync(Guid roomId)
        {
            return await _dbContext.RoomParticipants
                                 .Where(p => p.RoomId == roomId && p.IsOnStage == true)
                                 .Include(p => p.User)
                                 .ToListAsync();
        }

        // --- تنفيذ النواقص ---

        public async Task AddParticipantAsync(RoomParticipant participant)
        {
            await _dbContext.RoomParticipants.AddAsync(participant);
        }

        public async Task UpdateParticipantAsync(RoomParticipant participant)
        {
            _dbContext.RoomParticipants.Update(participant);
            await Task.CompletedTask;
        }

        public async Task RemoveParticipantAsync(RoomParticipant participant)
        {
            _dbContext.RoomParticipants.Remove(participant);
            await Task.CompletedTask;
        }
    }
}