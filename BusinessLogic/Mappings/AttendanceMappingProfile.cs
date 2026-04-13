using AutoMapper;
using BusinessLogic.DTOs;
using DataAccess.Models;

namespace BusinessLogic.Mappings
{
    /// <summary>
    /// AutoMapper profile for Attendance-related mappings
    /// </summary>
    public class AttendanceMappingProfile : Profile
    {
        public AttendanceMappingProfile()
        {
            // Attendance -> AttendanceDetailDto
            CreateMap<Attendance, AttendanceDetailDto>()
                .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.User.Email))
                .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.User.StudentId));

            // Event -> CheckInCodeResponse
            CreateMap<Event, CheckInCodeResponse>()
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.CheckInCode ?? string.Empty))
                .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.CodeExpiresAt ?? DateTime.Now))
                .ForMember(dest => dest.QrContent, opt => opt.MapFrom(src => 
                    $"EVENT:{src.EventId}|CODE:{src.CheckInCode}|EXPIRES:{src.CodeExpiresAt:yyyy-MM-ddTHH:mm:ss}"));
        }
    }
}
