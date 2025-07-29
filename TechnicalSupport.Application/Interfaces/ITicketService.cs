// TechnicalSupport.Application/Interfaces/ITicketService.cs
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface ITicketService
    {
        Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams, string userId);
        Task<TicketDto?> GetTicketByIdAsync(int id);
        Task<TicketDto> CreateTicketAsync(CreateTicketModel model, string userId);
        Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model);
        Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model, string userId);
        // THÊM METHOD MỚI DƯỚI ĐÂY
        Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model, string currentUserId);
    }
} 