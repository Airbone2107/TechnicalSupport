using Microsoft.EntityFrameworkCore;
using TechnicalSupport.Application.Common;
using TechnicalSupport.Application.Features.Tickets.Abstractions;
using TechnicalSupport.Application.Features.Tickets.DTOs;
using TechnicalSupport.Infrastructure.Persistence;

namespace TechnicalSupport.Api.Services
{
    /// <summary>
    /// Decorator for ITicketService to handle transactional test mode.
    /// If the "X-Test-Mode" header is true, all write operations are wrapped in a transaction that is immediately rolled back.
    /// This allows for safe testing of write endpoints against the real database without committing any changes.
    /// </summary>
    public class TransactionalTicketServiceDecorator : ITicketService
    {
        private readonly ITicketService _innerService;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TransactionalTicketServiceDecorator(
            ITicketService innerService,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _innerService = innerService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsTestMode()
        {
            return _httpContextAccessor.HttpContext?.Request.Headers["X-Test-Mode"].ToString().ToLower() == "true";
        }

        // Read operations are passed through directly.
        public Task<PagedResult<TicketDto>> GetTicketsAsync(TicketFilterParams filterParams)
        {
            return _innerService.GetTicketsAsync(filterParams);
        }

        public Task<TicketDto?> GetTicketByIdAsync(int id)
        {
            return _innerService.GetTicketByIdAsync(id);
        }
        
        // Write operations are wrapped in a transaction if in test mode.

        public async Task<TicketDto> CreateTicketAsync(CreateTicketModel model)
        {
            if (!IsTestMode())
            {
                return await _innerService.CreateTicketAsync(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.CreateTicketAsync(model);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<TicketDto?> UpdateTicketStatusAsync(int id, UpdateStatusModel model)
        {
            if (!IsTestMode())
            {
                return await _innerService.UpdateTicketStatusAsync(id, model);
            }
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.UpdateTicketStatusAsync(id, model);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<CommentDto?> AddCommentAsync(int ticketId, AddCommentModel model)
        {
            if (!IsTestMode())
            {
                return await _innerService.AddCommentAsync(ticketId, model);
            }
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.AddCommentAsync(ticketId, model);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<TicketDto?> AssignTicketAsync(int ticketId, AssignTicketModel model)
        {
            if (!IsTestMode())
            {
                return await _innerService.AssignTicketAsync(ticketId, model);
            }
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.AssignTicketAsync(ticketId, model);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<TicketDto?> AssignTicketToGroupAsync(int ticketId, AssignGroupModel model)
        {
            if (!IsTestMode())
            {
                return await _innerService.AssignTicketToGroupAsync(ticketId, model);
            }
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.AssignTicketToGroupAsync(ticketId, model);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<(bool Success, string Message)> DeleteTicketAsync(int ticketId)
        {
            if (!IsTestMode())
            {
                return await _innerService.DeleteTicketAsync(ticketId);
            }
            
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.DeleteTicketAsync(ticketId);
            await transaction.RollbackAsync();
            
            // We return the "success" result from the inner call, even though we rolled back.
            // This simulates a successful deletion for the client.
            return result;
        }

        // Bổ sung các phương thức còn thiếu
        public async Task<TicketDto> ClaimTicketAsync(int ticketId)
        {
            if (!IsTestMode())
            {
                return await _innerService.ClaimTicketAsync(ticketId);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.ClaimTicketAsync(ticketId);
            await transaction.RollbackAsync();
            return result;
        }

        public async Task<TicketDto> RejectFromGroupAsync(int ticketId)
        {
            if (!IsTestMode())
            {
                return await _innerService.RejectFromGroupAsync(ticketId);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            var result = await _innerService.RejectFromGroupAsync(ticketId);
            await transaction.RollbackAsync();
            return result;
        }
    }
} 