using Cocorra.DAL.Data;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using Cocorra.DAL.Repository.RoomRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Repository.FriendRepository;

public class FriendRepository : GenericRepositoryAsync<FriendRequest>, IFriendRepository
{
    public FriendRepository(AppDbContext dbContext) : base(dbContext)
    {

    }
    public async Task<FriendRequest?> GetFriendshipRelationAsync(Guid userId1, Guid userId2)
    {
        return await _dbContext.FriendRequests
            .FirstOrDefaultAsync(f =>
                (f.SenderId == userId1 && f.ReceiverId == userId2) ||
                (f.SenderId == userId2 && f.ReceiverId == userId1));
    }

    public async Task<FriendRequest?> GetPendingRequestAsync(Guid senderId, Guid receiverId)
    {
        return await _dbContext.FriendRequests
            .FirstOrDefaultAsync(f => f.SenderId == senderId && f.ReceiverId == receiverId && f.Status == FriendRequestStatus.Pending);
    }
}
