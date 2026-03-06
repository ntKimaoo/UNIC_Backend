using BusinessLogic.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BusinessLogic.Services.Interface
{
    public interface IEventService
    {
        Task<EventDetailDto> CreateEventAsync(CreateEventRequest request, string? imageUrl = null);
        Task<EventDetailDto> UpdateEventAsync(UpdateEventRequest request);
        Task<SessionDto> CreateSessionAsync(CreateSessionRequest request);
        Task<EventDetailDto> OpenRegistrationAsync(OpenRegistrationRequest request);
        Task<EventDetailDto> GetEventByIdAsync(int eventId);
        Task<IEnumerable<EventDetailDto>> GetAllEventsAsync(int pageNumber = 1, int pageSize = 10);
        
        Task RegisterForEventAsync(int eventId, string userId);
        Task<(string checkInCode, DateTime expiresAt)> StartEventAsync(int eventId);
        Task CheckInEventAsync(int eventId, string userId, string checkInCode);
        Task CompleteEventAsync(int eventId);
    }
}
