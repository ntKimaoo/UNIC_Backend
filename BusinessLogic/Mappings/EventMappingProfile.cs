using AutoMapper;
using BusinessLogic.DTOs;
using BusinessLogic.Helpers;
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
                .ForMember(dest => dest.ClubName, opt => opt.MapFrom(src => src.Club != null ? src.Club.ClubName : null))
                .ForMember(dest => dest.Sessions, opt => opt.MapFrom(src => src.EventSchedules))
                .ForMember(dest => dest.CurrentAttendees, opt => opt.MapFrom(src =>
                    src.Attendances != null
                        ? src.Attendances.Count(a =>
                            a.AttendanceStatus == "REGISTERED"
                            || a.AttendanceStatus == "PRESENT"
                            || a.AttendanceStatus == "CHECKED_IN"
                            || a.AttendanceStatus == "ABSENT")
                        : 0))
                .ForMember(dest => dest.RequiresApproval, opt => opt.MapFrom(src => src.RequiresApproval))
                .ForMember(dest => dest.AvailableSlots, opt => opt.MapFrom(src =>
                    src.MaxAttendees.HasValue
                        ? Math.Max(0, src.MaxAttendees.Value - (src.Attendances != null
                            ? src.Attendances.Count(a =>
                                a.AttendanceStatus == "REGISTERED"
                                || a.AttendanceStatus == "PRESENT"
                                || a.AttendanceStatus == "CHECKED_IN"
                                || a.AttendanceStatus == "ABSENT")
                            : 0))
                        : (int?)null))
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                .ForMember(dest => dest.MeetLink, opt => opt.MapFrom(src => src.MeetLink))
                .ForMember(dest => dest.Location, opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.CheckInCode, opt => opt.MapFrom(src => src.CheckInCode))
                .ForMember(dest => dest.CodeExpiresAt, opt => opt.MapFrom(src => src.CodeExpiresAt));

            CreateMap<EventSchedule, SessionDto>()
                .ForMember(dest => dest.ScheduleId,   opt => opt.MapFrom(src => src.ScheduleId))
                .ForMember(dest => dest.ScheduleName, opt => opt.MapFrom(src => src.ScheduleName))
                .ForMember(dest => dest.StartTime,    opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime,      opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.Location,     opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.Description,  opt => opt.MapFrom(src => src.Description));

            // CreateEventRequest -> Event
            CreateMap<CreateEventRequest, Event>()
                .ForMember(dest => dest.EventId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "PLANNED"))
                .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => VnTimeHelper.Now))
                .ForMember(dest => dest.IsOnline, opt => opt.MapFrom(src => src.IsOnline))
                // AvailableSlots = MaxAttendees khi tạo event mới
                .ForMember(dest => dest.AvailableSlots, opt => opt.MapFrom(src => src.MaxAttendees))
                .ForMember(dest => dest.Club, opt => opt.Ignore())
                .ForMember(dest => dest.EventSchedules, opt => opt.Ignore())
                .ForMember(dest => dest.EventImages, opt => opt.Ignore())
                .ForMember(dest => dest.EventBudgets, opt => opt.Ignore())
                .ForMember(dest => dest.Attendances, opt => opt.Ignore())
                .IgnoreAllPropertiesWithAnInaccessibleSetter();

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
                .ForMember(dest => dest.ScheduleId,      opt => opt.Ignore())
                .ForMember(dest => dest.ScheduleName,    opt => opt.MapFrom(src => src.SessionName))
                .ForMember(dest => dest.Location,        opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.Description,     opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SessionType,     opt => opt.MapFrom(src => src.SessionType))
                .ForMember(dest => dest.Event,           opt => opt.Ignore())
                .ForMember(dest => dest.ScheduleDetails, opt => opt.Ignore());

            // EventSchedule -> SessionDto
            CreateMap<EventSchedule, SessionDto>()
                .ForMember(dest => dest.ScheduleId,   opt => opt.MapFrom(src => src.ScheduleId))
                .ForMember(dest => dest.ScheduleName, opt => opt.MapFrom(src => src.ScheduleName))
                .ForMember(dest => dest.StartTime,    opt => opt.MapFrom(src => src.StartTime))
                .ForMember(dest => dest.EndTime,      opt => opt.MapFrom(src => src.EndTime))
                .ForMember(dest => dest.Location,     opt => opt.MapFrom(src => src.Location))
                .ForMember(dest => dest.Description,  opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.SessionType,  opt => opt.MapFrom(src => src.SessionType));
        }
    }
}
