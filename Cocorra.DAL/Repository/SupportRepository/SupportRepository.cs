using System.Threading.Tasks;
using Cocorra.DAL.Data;
using Cocorra.DAL.Models;

namespace Cocorra.DAL.Repository.SupportRepository
{
    public class SupportRepository : ISupportRepository
    {
        private readonly AppDbContext _dbContext;

        public SupportRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddTicketAsync(SupportTicket ticket)
        {
            await _dbContext.SupportTickets.AddAsync(ticket);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddReportAsync(Report report)
        {
            await _dbContext.Reports.AddAsync(report);
            await _dbContext.SaveChangesAsync();
        }
    }
}
