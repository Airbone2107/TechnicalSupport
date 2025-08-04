using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Features.Tickets.Abstractions
{
    public interface ITicketService
    {
        Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams);
        Task<TicketDto?> GetTicketByIdAsync(int id);
        Task<TicketDto> CreateTicketAsync(CreateTicketModel model);
        Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model);
        Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model);
        Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model);
        Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model);
        Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId);
        
        // Thêm các phương thức mới
        Task<TicketDto> ClaimTicketAsync(int ticketId);
        Task<TicketDto> RejectFromGroupAsync(int ticketId);
    }
} 