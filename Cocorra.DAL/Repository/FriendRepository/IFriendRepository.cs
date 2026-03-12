using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.GenericRepository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cocorra.DAL.Repository.FriendRepository
{
    public interface IFriendRepository : IGenericRepositoryAsync<FriendRequest>
    {
        Task<FriendRequest?> GetFriendshipRelationAsync(Guid userId1, Guid userId2);
        Task<FriendRequest?> GetPendingRequestAsync(Guid senderId, Guid receiverId);
        Task<List<ApplicationUser>> GetAcceptedFriendsAsync(Guid userId);
    }
}
