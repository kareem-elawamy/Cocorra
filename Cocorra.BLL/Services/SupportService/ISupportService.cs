using System;
using System.Threading.Tasks;
using Cocorra.BLL.Base;
using Cocorra.DAL.DTOS.ReportDto;
using Cocorra.DAL.DTOS.SupportDto;

namespace Cocorra.BLL.Services.SupportService
{
    public interface ISupportService
    {
        Task<Response<string>> SubmitTicketAsync(Guid? userId, SubmitSupportTicketDto dto);
        Task<Response<string>> SubmitReportAsync(Guid reporterId, SubmitReportDto dto);
    }
}
