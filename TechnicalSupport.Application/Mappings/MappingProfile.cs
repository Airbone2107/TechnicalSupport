using AutoMapper;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Features.Authentication.DTOs;
using TechnicalSupport.Application.Features.Groups.DTOs;
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
            CreateMap<ApplicationUser, UserDetailDto>(); // Mapping mới

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
            
            // Attachment Mappings
            CreateMap<Attachment, AttachmentDto>()
                .ForMember(dest => dest.UploadedByDisplayName, opt => opt.MapFrom(src => src.UploadedBy.DisplayName));
            
            // Group Mappings
            CreateMap<Group, GroupDto>();
            CreateMap<CreateGroupModel, Group>();
        }
    }
} 