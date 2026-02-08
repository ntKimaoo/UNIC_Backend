using AutoMapper;
using BusinessLogic.DTOs;
using DataAccess.Models;
using System.Linq;

namespace BusinessLogic.Mappings
{
    /// <summary>
    /// AutoMapper profile for Event-related mappings
    /// </summary>
    public class EventMappingProfile : Profile
    {
        public EventMappingProfile()
        {
            // Event -> EventDetailDto
            CreateMap<Event, EventDetailDto>()
                .ForMember(dest => dest.Sessions, opt => opt.MapFrom(src => src.EventSchedules));

            // EventSchedule -> SessionDto
            CreateMap<EventSchedule, SessionDto>()
                .ForMember(dest => dest.ScheduleId, opt => opt.MapFrom(src => src.ScheduleId))
                .ForMember(dest => dest.ScheduleName, opt => opt.MapFrom(src => src.ScheduleName))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description));

            // CreateEventRequest -> Event
            CreateMap<CreateEventRequest, Event>()
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "PLANNED"))
                .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.Club, opt => opt.Ignore())
                .ForMember(dest => dest.EventSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.EventImages, opt => opt.Ignore())
                .ForMember(dest => dest.EventBudgets, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore());

            // UpdateEventRequest -> Event
            CreateMap<UpdateEventRequest, Event>()
                .ForMember(dest => dest.EventId, opt => opt.MapFrom(src => src.EventId))
                .ForMember(dest => dest.ClubId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.IsPublic, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Club, opt => opt.Ignore())
                .ForMember(dest => dest.EventSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.EventImages, opt => opt.Ignore())
                .ForMember(dest => dest.EventBudgets, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore());

            // CreateSessionRequest -> EventSchedule
            CreateMap<CreateSessionRequest, EventSchedule>()
                .ForMember(dest => dest.ScheduleId, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduleName, opt => opt.MapFrom(src => src.SessionName))
                .ForMember(dest => dest.Event, opt => opt.Ignore())
                .ForMember(dest => dest.ScheduleDetails, opt => opt.Ignore());
        }
    }
}
