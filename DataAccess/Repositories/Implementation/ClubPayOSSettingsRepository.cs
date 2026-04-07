using DataAccess.Models;
using DataAccess.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories.Implementation
{
    public class ClubPayOSSettingsRepository : IClubPayOSSettingsRepository
    {
        private readonly UnicContext _context;

        public ClubPayOSSettingsRepository(UnicContext context)
        {
            _context = context;
        }

        public async Task<ClubPayOSSettings?> GetByClubIdAsync(int clubId)
        {
            return await _context.ClubPayOSSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ClubId == clubId);
        }

        public async Task UpsertAsync(ClubPayOSSettings settings)
        {
            var existing = await _context.ClubPayOSSettings
                .FirstOrDefaultAsync(x => x.ClubId == settings.ClubId);

            if (existing == null)
            {
                await _context.ClubPayOSSettings.AddAsync(settings);
            }
            else
            {
                existing.ClientId = settings.ClientId;
                existing.ApiKey = settings.ApiKey;
                existing.ChecksumKey = settings.ChecksumKey;
                existing.IsEnabled = settings.IsEnabled;
                existing.UpdatedAtUtc = settings.UpdatedAtUtc;
                existing.UpdatedBy = settings.UpdatedBy;
                _context.ClubPayOSSettings.Update(existing);
            }

            await _context.SaveChangesAsync();
        }
    }
}

