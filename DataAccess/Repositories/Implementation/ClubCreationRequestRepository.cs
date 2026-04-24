using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using UNIC.DataAccess.Models;

namespace DataAccess.Repositories
{
    public class ClubCreationRequestRepository : IClubCreationRequestRepository
    {
        private readonly UnicContext _context;

        public ClubCreationRequestRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClubCreationRequest>> GetAllAsync()
        {
            return await _context.ClubCreationRequests
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<ClubCreationRequest?> GetByIdAsync(int id)
        {
            return await _context.ClubCreationRequests
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.RequestId == id);
        }

        public async Task<IEnumerable<ClubCreationRequest>> GetByUserIdAsync(Guid userId)
        {
            return await _context.ClubCreationRequests
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasPendingRequestAsync(Guid userId)
        {
            return await _context.ClubCreationRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "Pending");
        }

        public async Task AddAsync(ClubCreationRequest request)
        {
            await _context.ClubCreationRequests.AddAsync(request);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ClubCreationRequest request)
        {
            _context.ClubCreationRequests.Update(request);
            await _context.SaveChangesAsync();
        }
       
        public async Task DeleteAsync(int id)
        {
            var entity = await _context.ClubCreationRequests.FindAsync(id);

            if (entity != null)
            {
                _context.ClubCreationRequests.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }
    }
}