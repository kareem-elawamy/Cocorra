using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocorra.DAL.Models;

public class RoomTopicRequest : BaseEntity
{
    [Required]
    [MaxLength(150)]
    public string TopicTitle { get; set; } = string.Empty; 

    [MaxLength(500)]
    public string? Description { get; set; } 

    public Guid RequesterId { get; set; }
    [ForeignKey(nameof(RequesterId))]
    public virtual ApplicationUser? Requester { get; set; }
    public Guid? TargetCoachId { get; set; }
    [ForeignKey(nameof(TargetCoachId))]
    public virtual ApplicationUser? TargetCoach { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending; 
    public int VotesCount { get; set; } = 0;
}