using TechnicalSupport.Application.Features.Tickets.DTOs;

namespace TechnicalSupport.Application.Interfaces
{
    public interface ICommentService
    {
        Task<CommentDto?> GetCommentByIdAsync(int commentId);
        Task<CommentDto?> UpdateCommentAsync(int commentId, UpdateCommentModel model, string currentUserId);
        Task<bool> DeleteCommentAsync(int commentId, string currentUserId, bool isAdmin);
    }
} 