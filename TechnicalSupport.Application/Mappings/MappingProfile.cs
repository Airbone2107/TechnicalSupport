using AutoMapper;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Features.Authentication.DTOs;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Domain.Entities;

namespace TechnicalSupport.Application.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User Mappings
            CreateMap<ApplicationUser, UserDto>();

            // Status Mappings
            CreateMap<Status, StatusDto>();

            // Comment Mappings
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // Ticket Mappings
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Assignee, opt => opt.MapFrom(src => src.Assignee));
            
            CreateMap<CreateTicketModel, Ticket>();

            // THÊM MAPPING MỚI
            CreateMap<Attachment, AttachmentDto>()
                .ForMember(dest => dest.UploadedByDisplayName, opt => opt.MapFrom(src => src.UploadedBy.DisplayName));

            // Admin Mappings
            CreateMap<ApplicationUser, UserDetailDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore()); // Roles sẽ được map thủ công trong service
             // Thêm các mapping này vào constructor của MappingProfile.cs
            CreateMap<Group, TechnicalSupport.Application.Features.Groups.DTOs.GroupDto>();
            CreateMap<TechnicalSupport.Application.Features.Groups.DTOs.CreateGroupModel, Group>();

        }
    }
} 