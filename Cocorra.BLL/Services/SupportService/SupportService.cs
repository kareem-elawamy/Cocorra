using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocorra.BLL.Base;
using Cocorra.BLL.Services.Upload;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.NotificationRepository;
using Cocorra.DAL.Repository.SupportRepository;
using Microsoft.AspNetCore.Identity;

namespace Cocorra.BLL.Services.SupportService
{
    public class SupportService : ResponseHandler, ISupportService
    {
        private readonly ISupportRepository _supportRepo;
        private readonly IUploadImage _uploadImage;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepo;

        public SupportService(
            ISupportRepository supportRepo,
            IUploadImage uploadImage,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepo)
        {
            _supportRepo = supportRepo;
            _uploadImage = uploadImage;
            _userManager = userManager;
            _notificationRepo = notificationRepo;
        }

        public async Task<Response<string>> SubmitTicketAsync(Guid? userId, SubmitSupportTicketDto dto)
        {
            string? screenshotPath = null;
            if (dto.Screenshot != null && dto.Screenshot.Length > 0)
            {
                screenshotPath = await _uploadImage.SaveImageAsync(dto.Screenshot);
            }

            var ticket = new SupportTicket
            {
                UserId = userId,
                Type = dto.Type,
                Message = dto.Message,
                ContactEmail = dto.ContactEmail,
                ScreenshotPath = screenshotPath,
                Status = "Open"
            };

            await _supportRepo.AddTicketAsync(ticket);

            return Success("Support ticket submitted successfully.");
        }

        public async Task<Response<string>> SubmitReportAsync(Guid reporterId, SubmitReportDto dto)
        {
            if (dto.ReportedUserId == null && dto.ReportedRoomId == null)
                return BadRequest<string>("You must specify a user or a room to report.");

            string? screenshotPath = null;
            if (dto.Screenshot != null && dto.Screenshot.Length > 0)
            {
                screenshotPath = await _uploadImage.SaveImageAsync(dto.Screenshot);
            }

            var report = new Report
            {
                ReporterId = reporterId,
                ReportedUserId = dto.ReportedUserId,
                ReportedRoomId = dto.ReportedRoomId,
                Category = dto.Category,
                Description = dto.Description,
                ScreenshotPath = screenshotPath,
                Status = "Open"
            };

            await _supportRepo.AddReportAsync(report);

            return Success("Report submitted successfully.");
        }

        public async Task<Response<List<ReportDetailsDto>>> GetFilteredReportsAsync(ReportCategory? category, string? status)
        {
            var reports = await _supportRepo.GetFilteredReportsAsync(category, status);

            var result = reports.Select(r => new ReportDetailsDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                ReporterName = r.Reporter != null
                    ? $"{r.Reporter.FirstName} {r.Reporter.LastName}"
                    : "Unknown",
                ReportedUserId = r.ReportedUserId,
                ReportedUserName = r.ReportedUser != null
                    ? $"{r.ReportedUser.FirstName} {r.ReportedUser.LastName}"
                    : null,
                ReportedRoomId = r.ReportedRoomId,
                ReportedRoomName = r.ReportedRoom?.RoomTitle,
                ReportedRoomHostName = r.ReportedRoom?.Host != null
                    ? $"{r.ReportedRoom.Host.FirstName} {r.ReportedRoom.Host.LastName}"
                    : null,
                ReportedRoomCreatedAt = r.ReportedRoom?.CreatedAt,
                Category = (int)r.Category,
                CategoryName = r.Category.ToString(),
                Description = r.Description,
                ScreenshotPath = r.ScreenshotPath,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            }).ToList();

            return Success(result);
        }

        public async Task<Response<string>> UpdateReportStatusAsync(Guid reportId, string newStatus)
        {
            var report = await _supportRepo.GetReportByIdAsync(reportId);
            if (report == null) return NotFound<string>("Report not found.");

            report.Status = newStatus;
            report.UpdatedAt = DateTime.UtcNow;

            await _supportRepo.UpdateReportAsync(report);

            return Success($"Report status updated to '{newStatus}'.");
        }

        public async Task<Response<string>> TakeActionOnReportAsync(Guid reportId, TakeReportActionDto dto)
        {
            var report = await _supportRepo.GetReportByIdAsync(reportId);
            if (report == null) return NotFound<string>("Report not found.");

            switch (dto.Action)
            {
                case AdminReportAction.WarnUser:
                    if (report.ReportedUserId == null)
                        return BadRequest<string>("This report has no reported user to warn.");

                    var warning = new Notification
                    {
                        UserId = report.ReportedUserId.Value,
                        Title = "Admin Warning",
                        Message = dto.AdminNote ?? "You have received a warning from the admin team for violating community guidelines.",
                        Type = NotificationType.AdminWarning,
                        ReferenceId = report.Id,
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _notificationRepo.AddAsync(warning);

                    report.Status = "Resolved";
                    break;

                case AdminReportAction.Mute24h:
                    if (report.ReportedUserId == null)
                        return BadRequest<string>("This report has no reported user to mute.");

                    var muteUser = await _userManager.FindByIdAsync(report.ReportedUserId.Value.ToString());
                    if (muteUser == null) return NotFound<string>("Reported user not found.");

                    await _userManager.SetLockoutEnabledAsync(muteUser, true);
                    await _userManager.SetLockoutEndDateAsync(muteUser, DateTimeOffset.UtcNow.AddHours(24));

                    report.Status = "Resolved";
                    break;

                case AdminReportAction.BanUser:
                    if (report.ReportedUserId == null)
                        return BadRequest<string>("This report has no reported user to ban.");

                    var banUser = await _userManager.FindByIdAsync(report.ReportedUserId.Value.ToString());
                    if (banUser == null) return NotFound<string>("Reported user not found.");

                    await _userManager.SetLockoutEnabledAsync(banUser, true);
                    await _userManager.SetLockoutEndDateAsync(banUser, DateTimeOffset.UtcNow.AddYears(100));

                    report.Status = "Resolved";
                    break;

                case AdminReportAction.RejectReport:
                    report.Status = "Rejected";
                    break;

                default:
                    return BadRequest<string>("Invalid action.");
            }

            report.UpdatedAt = DateTime.UtcNow;
            await _supportRepo.UpdateReportAsync(report);

            return Success($"Action '{dto.Action}' applied successfully.");
        }
    }
}
