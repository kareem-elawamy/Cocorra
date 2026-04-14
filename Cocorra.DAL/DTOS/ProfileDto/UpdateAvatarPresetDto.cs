using System.ComponentModel.DataAnnotations;

namespace Cocorra.DAL.DTOS.ProfileDto
{
    public class UpdateAvatarPresetDto
    {
        [Required]
        public string AvatarPresetKey { get; set; } = string.Empty;
    }
}
