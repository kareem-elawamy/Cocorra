using System;
using System.Threading.Tasks;
using Cocorra.BLL.Base;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.SupportRepository;

namespace Cocorra.BLL.Services.SupportService
{
    public class SupportService : ResponseHandler, ISupportService
    {
        private readonly ISupportRepository _supportRepo;

        public SupportService(ISupportRepository supportRepo)
        {
            _supportRepo = supportRepo;
        }

        public async Task<Response<string>> SubmitTicketAsync(Guid? userId, SubmitSupportTicketDto dto)
        {
            var ticket = new SupportTicket
            {
                UserId = userId,
                Type = dto.Type,
                Message = dto.Message,
                ContactEmail = dto.ContactEmail,
                Status = "Open"
            };

            await _supportRepo.AddTicketAsync(ticket);

            return Success("Support ticket submitted successfully.");
        }

        public async Task<Response<string>> SubmitReportAsync(Guid reporterId, SubmitReportDto dto)
        {
            var report = new Report
            {
                ReporterId = reporterId,
                ReportedUserId = dto.ReportedUserId,
                ReportedRoomId = dto.ReportedRoomId,
                Category = dto.Category,
                Description = dto.Description,
                Status = "Open"
            };

            await _supportRepo.AddReportAsync(report);

            return Success("Report submitted successfully.");
        }
    }
}
