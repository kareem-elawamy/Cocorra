using Cocorra.DAL.DTOS;
using Cocorra.DAL.DTOS.AdminDto;
using Cocorra.DAL.DTOS.Role;
using Cocorra.DAL.Models;
using Core.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Cocorra.BLL.Services.RolesService
{
    public class RolesService : ResponseHandler, IRolesService
    {
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public RolesService(RoleManager<IdentityRole<Guid>> roleManager, UserManager<ApplicationUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<Response<List<RoleDto>>> GetRolesAsync()
        {
            var roles = await _roleManager.Roles
                .Select(r => new RoleDto { Id = r.Id.ToString(), Name = r.Name! })
                .ToListAsync();
            return Success(roles);
        }

        public async Task<Response<RoleDto>> GetRoleByIdAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return BadRequest<RoleDto>("Role not found");
            return Success(new RoleDto { Id = role.Id.ToString(), Name = role.Name! });
        }

        public async Task<Response<string>> CreateRoleAsync(string roleName)
        {
            var cleanName = roleName.Trim();

            if (string.IsNullOrWhiteSpace(cleanName))
                return BadRequest<string>("Role name cannot be empty");

            if (await _roleManager.RoleExistsAsync(cleanName))
                return BadRequest<string>("Role already exists");

            var result = await _roleManager.CreateAsync(new IdentityRole<Guid>(cleanName));

            if (result.Succeeded) return Success("Role created successfully");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest<string>($"Failed to create role: {errors}");
        }

        public async Task<Response<string>> UpdateRoleAsync(UpdateRoleDto model)
        {
            var role = await _roleManager.FindByIdAsync(model.RoleId);
            if (role == null) return BadRequest<string>("Role not found");

            if (role.Name!.ToUpper() == "ADMIN" || role.Name!.ToUpper() == "USER")
                return BadRequest<string>("Cannot rename system roles (Admin/User).");

            role.Name = model.NewRoleName.Trim();
            var result = await _roleManager.UpdateAsync(role);

            if (result.Succeeded) return Success("Role updated successfully");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest<string>($"Failed to update role: {errors}");
        }

        public async Task<Response<string>> DeleteRoleAsync(string roleId)
        {
            var role = await _roleManager.FindByIdAsync(roleId);
            if (role == null) return BadRequest<string>("Role not found");

            if (role.Name!.ToUpper() == "ADMIN" || role.Name!.ToUpper() == "USER") return BadRequest<string>("Cannot delete Admin role");

            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
            if (usersInRole.Count > 0)
                return BadRequest<string>($"Cannot delete role because {usersInRole.Count} users are assigned to it.");

            var result = await _roleManager.DeleteAsync(role);
            if (result.Succeeded) return Success("Role deleted successfully");

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest<string>($"Failed to delete role: {errors}");
        }

        public async Task<Response<string>> ManageUserRolesAsync(ManageUserRolesDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null) return BadRequest<string>("User not found");

            foreach (var role in model.Roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    return BadRequest<string>($"Role '{role}' does not exist in the system.");
            }

            var currentRoles = await _userManager.GetRolesAsync(user);

            var rolesToAdd = model.Roles.Except(currentRoles).ToList();
            var rolesToRemove = currentRoles.Except(model.Roles).ToList();

            if (!rolesToAdd.Any() && !rolesToRemove.Any())
                return BadRequest<string>("No changes detected.");

            if (rolesToAdd.Any())
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded) return BadRequest<string>("Failed to add roles: " + string.Join(", ", addResult.Errors.Select(e => e.Description)));
            }

            if (rolesToRemove.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
                if (!removeResult.Succeeded) return BadRequest<string>("Failed to remove roles: " + string.Join(", ", removeResult.Errors.Select(e => e.Description)));
            }

            return Success("User roles updated successfully");
        }

        public async Task<Response<List<UserDto>>> GetUsersInRoleAsync(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
                return BadRequest<List<UserDto>>("Role not found");

            var users = await _userManager.GetUsersInRoleAsync(roleName);

            // Mapping كامل للـ DTO
            var usersDto = users.Select(u => new UserDto
            {
                Id = u.Id.ToString(),
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email ?? "",
                Age = u.Age,
                MBTI = u.MBTI ?? "N/A",
                Status = u.Status.ToString(),
                VoicePath = u.VoiceVerificationPath
            }).ToList();

            return Success(usersDto);
        }
    }
}