using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cocorra.DAL.Data;
using Cocorra.DAL.Enums;
using Cocorra.DAL.Models;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<Report>> GetFilteredReportsAsync(ReportCategory? category, string? status)
        {
            var query = _dbContext.Reports
                .AsNoTracking()
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedRoom)
                    .ThenInclude(room => room!.Host)
                .AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(r => r.Category == category.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(r => r.Status == status);
            }

            return await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Report?> GetReportByIdAsync(Guid reportId)
        {
            return await _dbContext.Reports.FindAsync(reportId);
        }

        public async Task UpdateReportAsync(Report report)
        {
            _dbContext.Reports.Update(report);
            await _dbContext.SaveChangesAsync();
        }
    }
}
