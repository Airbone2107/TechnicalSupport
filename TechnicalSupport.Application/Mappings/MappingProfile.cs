using AutoMapper;
using TechnicalSupport.Application.Features.Admin.DTOs;
using TechnicalSupport.Application.Features.Attachments.DTOs;
using TechnicalSupport.Application.Features.Groups.DTOs;
using TechnicalSupport.Application.Features.Permissions.DTOs;
// Thêm using cho ProblemType DTOs
using TechnicalSupport.Application.Features.ProblemTypes.DTOs;
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
            CreateMap<ApplicationUser, UserDetailDto>();

            // Status Mappings
            CreateMap<Status, StatusDto>();
            
            // ProblemType Mappings
            CreateMap<ProblemType, ProblemTypeDto>();
            CreateMap<CreateProblemTypeDto, ProblemType>();
            CreateMap<UpdateProblemTypeDto, ProblemType>();

            // Comment Mappings
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

            // Ticket Mappings
            CreateMap<Ticket, TicketDto>()
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
                .ForMember(dest => dest.Customer, opt => opt.MapFrom(src => src.Customer))
                .ForMember(dest => dest.Assignee, opt => opt.MapFrom(src => src.Assignee))
                .ForMember(dest => dest.Group, opt => opt.MapFrom(src => src.Group))
                .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments)) // Thêm mapping
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments)); // Thêm mapping

            CreateMap<CreateTicketModel, Ticket>();

            // Attachment Mappings
            CreateMap<Attachment, AttachmentDto>()
                .ForMember(dest => dest.UploadedByDisplayName, opt => opt.MapFrom(src => src.UploadedBy.DisplayName));

            // Group Mappings
            CreateMap<Group, GroupDto>();
            CreateMap<CreateGroupModel, Group>();

            // Permission Mappings
            CreateMap<PermissionRequest, PermissionRequestDto>()
                 .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
            CreateMap<CreatePermissionRequestModel, PermissionRequest>();
        }
    }
}