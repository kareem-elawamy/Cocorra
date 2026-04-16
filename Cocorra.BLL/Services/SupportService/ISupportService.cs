using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cocorra.BLL.Base;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;
using Cocorra.DAL.Enums;

namespace Cocorra.BLL.Services.SupportService
{
    public interface ISupportService
    {
        Task<Response<string>> SubmitTicketAsync(Guid? userId, SubmitSupportTicketDto dto);
        Task<Response<string>> SubmitReportAsync(Guid reporterId, SubmitReportDto dto);
        Task<Response<List<ReportDetailsDto>>> GetFilteredReportsAsync(ReportCategory? category, string? status);
        Task<Response<string>> UpdateReportStatusAsync(Guid reportId, string newStatus);
        Task<Response<string>> TakeActionOnReportAsync(Guid reportId, TakeReportActionDto dto);
    }
}
