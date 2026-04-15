using BusinessLogic.DTOs;
using BusinessLogic.Services.Interface;
using DataAccess.Models;
using DataAccess.Repositories.Interface;
using UNIC.DataAccess.Repositories.Interface;

namespace BusinessLogic.Services.Implementation
{
    public class ClubPayOSSettingsService : IClubPayOSSettingsService
    {
        private const string MEMBER_STATUS_ACTIVE = "ACTIVE";

        private readonly IClubMemberRepository _clubMemberRepository;
        private readonly IClubPayOSSettingsRepository _settingsRepository;

        public ClubPayOSSettingsService(
            IClubMemberRepository clubMemberRepository,
            IClubPayOSSettingsRepository settingsRepository)
        {
            _clubMemberRepository = clubMemberRepository;
            _settingsRepository = settingsRepository;
        }

        public async Task<ClubPayOSSettingsResponseDto> GetAsync(Guid currentUserId, int clubId, bool isSystemAdmin)
        {
            await EnsureCanManagePayOSAsync(currentUserId, clubId, isSystemAdmin);
            var s = await _settingsRepository.GetByClubIdAsync(clubId);
            return ToResponse(clubId, s);
        }

        public async Task<ClubPayOSSettingsResponseDto> UpsertAsync(Guid currentUserId, int clubId, bool isSystemAdmin, UpsertClubPayOSSettingsDto dto)
        {
            await EnsureCanManagePayOSAsync(currentUserId, clubId, isSystemAdmin);

            var clientId = (dto.ClientId ?? string.Empty).Trim();
            var apiKey = (dto.ApiKey ?? string.Empty).Trim();
            var checksumKey = (dto.ChecksumKey ?? string.Empty).Trim();

            if (dto.IsEnabled)
            {
                if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(checksumKey))
                    throw new ArgumentException("Bật PayOS thì cần đủ ClientId, ApiKey, ChecksumKey.");
            }

            var settings = new ClubPayOSSettings
            {
                ClubId = clubId,
                ClientId = clientId,
                ApiKey = apiKey,
                ChecksumKey = checksumKey,
                IsEnabled = dto.IsEnabled,
                UpdatedAtUtc = DateTime.UtcNow,
                UpdatedBy = currentUserId
            };

            await _settingsRepository.UpsertAsync(settings);
            return ToResponse(clubId, settings);
        }

        private async Task EnsureCanManagePayOSAsync(Guid userId, int clubId, bool isSystemAdmin)
        {
            if (isSystemAdmin)
                return;

            var member = await _clubMemberRepository.GetMemberAsync(userId, clubId);
            if (member == null)
                throw new UnauthorizedAccessException("Bạn không phải thành viên của club này.");
            if (!string.Equals(member.Status, MEMBER_STATUS_ACTIVE, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Chỉ thành viên đang hoạt động mới được cấu hình PayOS.");
            if (member.ClubRole?.Level != 1)
                throw new UnauthorizedAccessException("Chỉ Club Manager (Level 1) mới được cấu hình PayOS cho CLB.");
        }

        private static ClubPayOSSettingsResponseDto ToResponse(int clubId, ClubPayOSSettings? s)
        {
            var configured = s != null
                             && !string.IsNullOrWhiteSpace(s.ClientId)
                             && !string.IsNullOrWhiteSpace(s.ApiKey)
                             && !string.IsNullOrWhiteSpace(s.ChecksumKey);

            return new ClubPayOSSettingsResponseDto
            {
                ClubId = clubId,
                IsConfigured = configured,
                IsEnabled = s?.IsEnabled ?? false,
                ClientId = string.IsNullOrWhiteSpace(s?.ClientId) ? null : s!.ClientId,
                ApiKeyMasked = Mask(s?.ApiKey),
                ChecksumKeyMasked = Mask(s?.ChecksumKey),
                UpdatedAtUtc = s?.UpdatedAtUtc
            };
        }

        private static string? Mask(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;
            var v = value.Trim();
            if (v.Length <= 6)
                return new string('*', v.Length);
            return $"{v[..2]}***{v[^2..]}";
        }
    }
}

