using System.Threading.Tasks;
using Cocorra.DAL.Models;

namespace Cocorra.DAL.Repository.SupportRepository
{
    public interface ISupportRepository
    {
        Task AddTicketAsync(SupportTicket ticket);
        Task AddReportAsync(Report report);
    }
}
