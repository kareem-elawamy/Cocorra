using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Cocorra.DAL.Models;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; }
    public Guid? ReferenceId { get; set; }

    public bool IsRead { get; set; } = false;
}